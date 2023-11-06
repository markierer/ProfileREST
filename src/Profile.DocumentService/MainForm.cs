using Microsoft.Extensions.Configuration;
using Profile.WebApp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Windows.Forms;

namespace Profile.DocumentService
{
    /// <summary>
    /// MainForm
    /// </summary>
    public partial class MainForm : Form
    {
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(typeof(MainForm));

        private ProfileWebApp _profile;
        private BlockingCollection<string> _queue;
        private BackgroundWorker _bg;
        private FileSystemWatcher _watcher;

        private string _syncAuto;
        private string _syncPath;
        private string _syncPathSuccessed;
        private string _syncPathFailed;
        private string _syncFileExtension;
        private string _profileBaseUri;
        private string _profileServer;
        private string _profileMandant;
        private string _profileUserName;
        private string _profilePassword;
        private string _profileTempPath;
        private string _profileIdentNo;
        private string _profileVersion;
        private string _fileNameSeparator;
        private string _fileNameRename;
        private string _docBase;

        private Dictionary<string, string> _fileNameElement;
        private Dictionary<string, string> _docType;
        private Dictionary<string, string> _docFixProperty;
        private Dictionary<string, string> _docFixValue;
        private Dictionary<string, string> _docVarProperty;
        private Dictionary<string, string> _docVarValue;
        private Dictionary<string, string> _queryElementLeft;
        private Dictionary<string, string> _queryElementOperator;
        private Dictionary<string, string> _queryElementRight;


        /// <summary>
        /// MainForm
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }


        /// <summary>
        /// OnLoad
        /// </summary>
        /// <param name="e">EventArgs</param>
        protected override void OnLoad(EventArgs e)
        {
            // Change to tray mode
            this.Visible = false;
            this.ShowInTaskbar = false;

            base.OnLoad(e);

            try
            {
                // Load app.config
                LoadAppConfig();

                // Create Objects
                CreateObjects();

                // First scan folder and send existing files
                SendFilesByFolderScan();

                // Then send files continuously
                if (_syncAuto.Equals("true"))
                {
                    // BackgroundWorker
                    StartBG();

                    // FileSystemWatcher
                    StartFSW(_syncPath, _syncFileExtension);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format("OnLoad failed with error {0}", ex.Message));
                ExitApp();
            }
        }


        /// <summary>
        /// trayMenuItemExit_Click
        /// </summary>
        /// <param name="sender">object</param>
        /// <param name="e">EventArgs</param>
        private void trayMenuItemExit_Click(object sender, EventArgs e)
        {
            ExitApp();
        }


        /// <summary>
        /// ExitApp
        /// </summary>
        public void ExitApp()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
            if (_bg != null)
            {
                _bg.CancelAsync();
                _logger.Info("OnExit waiting on BackgroundWorker before exit...");
                while (_bg.IsBusy)
                {
                    Application.DoEvents();
                    Thread.Sleep(100);
                }
            }
            _logger.Info("OnExit Application exit");
            Application.Exit();
        }


        /// <summary>
        /// LoadAppConfig
        /// </summary>
        public void LoadAppConfig()
        {
            // app.config AppSettings
            var appSettings = Program.Configuration.GetSection("appSettings").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _syncAuto = appSettings["syncAuto"];
            _syncPath = appSettings["syncPath"];
            _syncPathSuccessed = appSettings["syncPathSuccessed"];
            _syncPathFailed = appSettings["syncPathFailed"];
            _syncFileExtension = appSettings["syncFileExtension"];
            _profileBaseUri = appSettings["profileBaseUri"];
            _profileServer = appSettings["profileServer"];
            _profileMandant = appSettings["profileMandant"];
            _profileTempPath = appSettings["profileTempPath"];
            _profileIdentNo = appSettings["profileIdentNo"];
            _profileUserName = appSettings["profileUserName"];
            _profilePassword = appSettings["profilePassword"];
            _profileVersion = appSettings["profileVersion"];
            _fileNameSeparator = appSettings["fileNameSeparator"];
            _fileNameRename = appSettings["fileNameRename"];
            _docBase = appSettings["docBase"];

            _fileNameElement = Program.Configuration.GetSection("fileNameElement").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _docType = Program.Configuration.GetSection("docType").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _docFixProperty = Program.Configuration.GetSection("docFixProperty").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _docFixValue = Program.Configuration.GetSection("docFixValue").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _docVarProperty = Program.Configuration.GetSection("docVarProperty").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _docVarValue = Program.Configuration.GetSection("docVarValue").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _queryElementLeft = Program.Configuration.GetSection("queryElementLeft").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _queryElementOperator = Program.Configuration.GetSection("queryElementOperator").GetChildren().ToDictionary(x => x.Key, x => x.Value);
            _queryElementRight = Program.Configuration.GetSection("queryElementRight").GetChildren().ToDictionary(x => x.Key, x => x.Value);
        }


        /// <summary>
        /// CreateObjects
        /// </summary>
        public void CreateObjects()
        {
            if (_profileVersion.Equals("2"))
                _profile = new ProfileWebApp2(_profileBaseUri, _profileServer, _profileMandant, _profileUserName, _profilePassword, _profileTempPath, _profileIdentNo);
            else
                _profile = new ProfileWebApp(_profileBaseUri, _profileServer, _profileMandant, _profileUserName, _profilePassword, _profileTempPath, _profileIdentNo);
            _queue = new BlockingCollection<string>();
        }


        /// <summary>
        /// StartBG (BackgroundWorker)
        /// </summary>
        public void StartBG()
        {
            _bg = new BackgroundWorker();
            _bg.WorkerSupportsCancellation = true;
            _bg.DoWork += (a, b) => SendFilesByBG();
            _bg.RunWorkerCompleted += (a, b) =>
            {
                if (b.Error != null)
                {
                    _logger.Error(String.Format("BackgroundWorker failed with error {0}", b.Error.Message));
                    throw b.Error;
                }
                else
                {
                    _logger.Info("BackgroundWorker finished successfully");
                }
            };
            _bg.RunWorkerAsync();
        }


        /// <summary>
        /// SendFilesByBG (BackgroundWorker)
        /// </summary>
        public void SendFilesByBG()
        {
            while (!_bg.CancellationPending)
            {
                SendFilesToProfile();
                Thread.Sleep(100);
            }
        }


        /// <summary>
        /// SendFilesByFolderScan
        /// </summary>
        public void SendFilesByFolderScan()
        {
            DirectoryInfo d = new(_syncPath);
            foreach (var file in d.GetFiles("*." + _syncFileExtension))
            {
                try
                {
                    // Add full path to queue for upload
                    _queue.TryAdd(file.FullName);
                    _logger.Info(String.Format("SendFilesFolderScan {0} with path {1} added to queue", file.Name, file.FullName));
                }
                catch (InvalidOperationException ex)
                {
                    _logger.Error(String.Format("SendFilesFolderScan {0} with path {1} failed with error {2}", file.Name, file.FullName, ex.Message));
                    throw;
                }
            }

            SendFilesToProfile();
        }


        /// <summary>
        /// SendFilesToProfile
        /// </summary>
        public void SendFilesToProfile()
        {
            int id = 0;

            try
            {
                while (_queue.TryTake(out string? fullPath))
                {
                    id = 0;
                    if (File.Exists(fullPath))
                    {
                        // Split filename and check number of elements
                        string[] fnValues = Path.GetFileNameWithoutExtension(fullPath).Split(new[] { _fileNameSeparator }, StringSplitOptions.None);
                        if (fnValues.Length == _fileNameElement.Count)
                        {
                            // Fill Dictionary with values from filename
                            var fnElements = new Dictionary<string, string>();
                            for (int i = 0; i < fnValues.Length; ++i)
                            {
                                fnElements.Add(_fileNameElement.ElementAt(i).Key, fnValues[i]);
                            }

                            // Create Dictionary with values to fill document
                            var docContent = CreateDocumentContent(fnElements);

                            // Create List with values to find document
                            var docQuery = CreateDocumentQuery(fnElements);

                            // Get Document by query
                            id = _profile.GetDocumentByQuery(docQuery);
                            if (id == 0) // create new
                            {
                                string link = _profile.CreateDocument(docContent);
                                id = ProfileWebApp.GetIdFromLink(link);
                            }
                            else if (id == -1) // error: exist more than one
                            {
                                // Move file to failed folder or delete it
                                _logger.Warn(String.Format("SendFilesToProfile filename length {0} is not equal fileNameElement.Count {1}", fnValues.Length, _fileNameElement.Count));
                                if (!string.IsNullOrEmpty(_syncPathFailed))
                                {
                                    string newPath = Path.Combine(_syncPathFailed, Path.GetFileName(fullPath));
                                    if (File.Exists(newPath))
                                    {
                                        File.Delete(newPath);
                                    }
                                    File.Move(fullPath, newPath);
                                    _logger.Warn(String.Format("SendFilesToProfile file {0} moved to failed folder", fullPath));
                                }
                                else
                                {
                                    File.Delete(fullPath);
                                    _logger.Warn(String.Format("SendFilesToProfile file {0} deleted", fullPath));
                                }
                                break;
                            }
                            else // exists
                            {
                                _profile.ChangeDocument(id, docContent);
                            }

                            // Upload file
                            fnElements.TryGetValue(_fileNameRename, out string? newFileName);
                            _profile.UploadFileById(id, fullPath, newFileName);

                            // Move file to successed folder or delete it
                            if (!string.IsNullOrEmpty(_syncPathSuccessed))
                            {
                                string newPath = Path.Combine(_syncPathSuccessed, Path.GetFileName(fullPath));
                                if (File.Exists(newPath))
                                {
                                    File.Delete(newPath);
                                }
                                File.Move(fullPath, newPath);
                                _logger.Info(String.Format("SendFilesToProfile file {0} moved to successed folder", fullPath));
                            }
                            else
                            {
                                File.Delete(fullPath);
                                _logger.Info(String.Format("SendFilesToProfile file {0} deleted", fullPath));
                            }
                        }
                        else
                        {
                            // Move file to failed folder or delete it
                            _logger.Warn(String.Format("SendFilesToProfile filename length {0} is not equal fileNameElement.Count {1}", fnValues.Length, _fileNameElement.Count));
                            if (!string.IsNullOrEmpty(_syncPathFailed))
                            {
                                string newPath = Path.Combine(_syncPathFailed, Path.GetFileName(fullPath));
                                if (File.Exists(newPath))
                                {
                                    File.Delete(newPath);
                                }
                                File.Move(fullPath, newPath);
                                _logger.Warn(String.Format("SendFilesToProfile file {0} moved to failed folder", fullPath));
                            }
                            else
                            {
                                File.Delete(fullPath);
                                _logger.Warn(String.Format("SendFilesToProfile file {0} deleted", fullPath));
                            }
                        }
                    }
                    else
                    {
                        // File not exists
                        _logger.Warn(String.Format("SendFilesToProfile file {0} not exists", fullPath));
                    }
                    Thread.Sleep(100);
                }
                while (_queue.IsCompleted)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format("SendFilesToProfile failed with error {0}", ex.Message));
                log4net.LogManager.GetLogger("EmailLogger").Error(String.Format("SendFilesToProfile failed with error {0} ID: " + id, ex.Message));
                throw;
            }
        }


        /// <summary>
        /// CreateDocumentContent
        /// </summary>
        /// <param name="fnElements">Filename elements</param>
        /// <returns>Document content</returns>
        public Dictionary<string, string> CreateDocumentContent(Dictionary<string, string> fnElements)
        {
            var docContent = new Dictionary<string, string>();
            string docType = GetDocType(fnElements);

            // Fix document properties (one of elements is docType)
            for (int i = 0; i < _docFixProperty.Count; ++i)
            {
                string docFixProperty = _docFixProperty.ElementAt(i).Value;
                string docFixValue = _docFixValue.ElementAt(i).Value;
                string key = _docBase + docFixProperty;
                string value = string.Empty;

                if (docFixValue.StartsWith("fileNameElement"))
                {
                    fnElements.TryGetValue(docFixValue, out value);

                    // Document type
                    if (docFixProperty.Equals("docType"))
                    {
                        value = _docBase + docType;
                    }

                    // Date type
                    if (docFixProperty.Contains("Date"))
                    {
                        value = value.Replace("_", ":");
                    }
                }
                else
                {
                    value = docFixValue;
                }

                docContent.Add(key, value);
            }

            // Variable document properties
            for (int i = 0; i < _docVarProperty.Count; ++i)
            {
                string docVarProperty = _docVarProperty.ElementAt(i).Value;
                string docVarValue = _docVarValue.ElementAt(i).Value;
                string key = _docBase + docType + docVarProperty;
                string value = string.Empty;

                if (docVarValue.StartsWith("fileNameElement"))
                {
                    fnElements.TryGetValue(docVarValue, out value);

                    // Date type
                    if (docVarProperty.Contains("Date"))
                    {
                        value = value.Replace("_", ":");
                    }
                }
                else
                {
                    value = docVarValue;
                }

                docContent.Add(key, value);
            }

            return docContent;
        }


        /// <summary>
        /// CreateDocumentQuery
        /// </summary>
        /// <param name="fnElements">Filename elements</param>
        /// <returns>Search query</returns>
        public List<string> CreateDocumentQuery(Dictionary<string, string> fnElements)
        {
            var docQuery = new List<string>();
            string docType = GetDocType(fnElements);
            bool isDocType = false;
            bool isDateType = false;

            // Concat query
            for (int i = 0; i < _queryElementLeft.Count; ++i)
            {
                string queryElementLeft = _queryElementLeft.ElementAt(i).Value;
                string queryElementOperator = _queryElementOperator.ElementAt(i).Value;
                string queryElementRight = _queryElementRight.ElementAt(i).Value;
                string left = string.Empty;
                string right = string.Empty;

                // Left side of query
                if (queryElementLeft.StartsWith("docFixProperty"))
                {
                    string docFixProperty = _docFixProperty[queryElementLeft];
                    left = _docBase + docFixProperty;

                    // Document type
                    if (docFixProperty.Equals("docType"))
                    {
                        isDocType = true;
                    }

                    // Date type
                    if (docFixProperty.Contains("Date"))
                    {
                        isDateType = true;
                    }
                }
                else if (queryElementLeft.StartsWith("docVarProperty"))
                {
                    string docVarProperty = _docVarProperty[queryElementLeft];
                    left = _docBase + docType + docVarProperty;

                    // Date type
                    if (docVarProperty.Contains("Date"))
                    {
                        isDateType = true;
                    }
                }

                // Right side of query
                if (queryElementRight.StartsWith("docFixValue"))
                {
                    string docFixValue = _docFixValue[queryElementRight];
                    if (docFixValue.StartsWith("fileNameElement"))
                    {
                        fnElements.TryGetValue(docFixValue, out right);

                        // Document type
                        if (isDocType)
                        {
                            right = _docBase + docType;
                            isDocType = false;
                        }

                        // Date type
                        if (isDateType)
                        {
                            right = right.Replace("_", ":");
                            isDateType = false;
                        }
                    }
                    else
                    {
                        right = docFixValue;
                    }

                }
                else if (queryElementRight.StartsWith("docVarValue"))
                {
                    string docVarValue = _docVarValue[queryElementRight];
                    if (docVarValue.StartsWith("fileNameElement"))
                    {
                        fnElements.TryGetValue(docVarValue, out right);

                        // Date type
                        if (isDateType)
                        {
                            right = right.Replace("_", ":");
                            isDateType = false;
                        }
                    }
                    else
                    {
                        right = docVarValue;
                    }
                }

                docQuery.Add(String.Format("'{0}'{1}'{2}'", left, queryElementOperator, right));
            }

            return docQuery;
        }


        /// <summary>
        /// GetDocType
        /// </summary>
        /// <param name="fnElements">Filename elements</param>
        /// <returns>Document type</returns>
        public string GetDocType(Dictionary<string, string> fnElements)
        {
            string docType = string.Empty;

            // Fix document properties (one of the elements is docType)
            for (int i = 0; i < _docFixProperty.Count; ++i)
            {
                string docFixProperty = _docFixProperty.ElementAt(i).Value;
                string docFixValue = _docFixValue.ElementAt(i).Value;

                if (docFixProperty.Equals("docType") && docFixValue.StartsWith("fileNameElement"))
                {
                    fnElements.TryGetValue(docFixValue, out string? value);
                    docType = _docType[value];
                    break;
                }
            }

            return docType;
        }


        /// <summary>
        /// StartFSW (FileSystemWatcher)
        /// </summary>
        /// <param name="path">Path to watch</param>
        /// <param name="extension">File extension filter</param>
        public void StartFSW(string path, string extension = "*")
        {

            // Create a new FileSystemWatcher
            _watcher = new FileSystemWatcher();
            _watcher.Error += (a, b) =>
            {
                Exception ex = b.GetException();
                if (ex != null)
                {
                    _logger.Error(String.Format("FileSystemWatcher failed with error {0}", ex.Message));
                    throw ex;
                }
            };

            // Set path to watch
            _watcher.Path = path;

            // Watch both files and subdirectories
            _watcher.IncludeSubdirectories = true;

            // Watch for all changes specified in the NotifyFilters
            // enumeration.
            _watcher.NotifyFilter =
                //NotifyFilters.Attributes |
                //NotifyFilters.CreationTime |
                //NotifyFilters.DirectoryName |
                //NotifyFilters.FileName |
                //NotifyFilters.LastAccess |
                NotifyFilters.LastWrite; // |
                                         //NotifyFilters.Security |
                                         //NotifyFilters.Size;

            // Watch files
            _watcher.Filter = "*." + extension;

            // Add event handlers
            _watcher.Changed += (a, b) => OnChanged(a, b);
            _watcher.Created += (a, b) => OnChanged(a, b);
            //watcher.Deleted += (a, b) => OnChanged(a, b);
            //watcher.Renamed += (a, b) => OnRenamed(a, b);

            //Start monitoring
            _watcher.EnableRaisingEvents = true;
        }


        /// <summary>
        /// OnChanged
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                // Add full path to queue for upload
                _queue.TryAdd(e.FullPath);
                _logger.Info(String.Format("OnChanged {0} with path {1} by event {2} added to queue", e.Name, e.FullPath, e.ChangeType));
            }
            catch (InvalidOperationException ex)
            {
                _logger.Error(String.Format("OnChanged {0} with path {1} by event {2} failed with error {3}", e.Name, e.FullPath, e.ChangeType, ex.Message));
                throw;
            }
        }


        /// <summary>
        /// OnRenamed
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void OnRenamed(object source, RenamedEventArgs e)
        {
            _logger.Info(String.Format("OnRenamed {0} renamed to {1}", e.OldFullPath, e.FullPath));
        }
    }
}
