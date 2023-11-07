using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Web;

namespace Profile.WebApp;


/// <summary>
/// ProfileWebApp2
/// Uses AuthenticationHeaderValue for login
/// </summary>
public class ProfileWebApp2 : ProfileWebApp
{
    /// <summary>
    /// ProfileWebApp2
    /// </summary>
    /// <param name="baseUri"></param>
    /// <param name="server"></param>
    /// <param name="mandant"></param>
    /// <param name="userName">Username</param>
    /// <param name="password">Base64 encoded password</param>
    /// <param name="tempPath"></param>
    /// <param name="identNo"></param>
    public ProfileWebApp2(string baseUri, string server, string mandant, string userName, string password, string tempPath, string identNo) 
        : base(baseUri, server, mandant, userName, password, tempPath, identNo)
    {
        _logger.Info(string.Format("ProfileWebApp2 initialized by user {0} with password {1}", _userName, _password));
    }

    /// <summary>
    /// GetDocumentById
    /// </summary>
    /// <param name="id">Document Id</param>
    /// <param name="selfLink">selfLink</param>
    /// <param name="fileLink">fileLink</param>
    /// <returns>Document Id</returns>
    public override int GetDocumentById(int id, out string selfLink, out string fileLink)
    {
        try
        {
            selfLink = string.Empty;
            fileLink = string.Empty;

            _logger.Info(string.Format("GetDocumentById {0} ...", id.ToString()));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            using HttpResponseMessage response = client.GetAsync(string.Format("objects/document/{0}", id)).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("GetDocumentById failed with http status code {0} and error {1}", response.StatusCode, e.Message));

                _ = int.TryParse(response.StatusCode.ToString(), out int code);
                if (code < 500)
                {
                    return 0;
                }
                else
                {
                    throw;
                }
            }

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);
            JObject header = jsonObject["Header"] as JObject;

            selfLink = header["Links"].First(t => t["Type"].Value<string>() == "self").Value<string>("Href");
            fileLink = header["Links"].First(t => t["Type"].Value<string>() == "file").Value<string>("Href");

            _logger.Info(string.Format("GetDocumentById succeeded with selfLink {0}", selfLink));
            _logger.Info(string.Format("GetDocumentById succeeded with fileLink {0}", fileLink));

            return id;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("GetDocumentById failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// GetDocumentByLink
    /// </summary>
    /// <param name="link">selfLink</param>
    /// <returns>Document Id</returns>
    public override int GetDocumentByLink(string link)
    {
        try
        {
            _logger.Info(string.Format("GetDocumentByLink {0} ...", link));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            using HttpResponseMessage response = client.GetAsync(link).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("GetDocumentByLink failed with http status code {0} and error {1}", response.StatusCode, e.Message));

                _ = int.TryParse(response.StatusCode.ToString(), out int code);
                if (code < 500)
                {
                    return 0;
                }
                else
                {
                    throw;
                }
            }

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);

            int id = jsonObject["Values"][string.Format("/Document/{0}", _identNo)].Value<int>();

            _logger.Info(string.Format("GetDocumentByLink succeeded with id {0}", id.ToString()));

            return id;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("GetDocumentByLink failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// GetDocumentByQuery
    /// </summary>
    /// <param name="list">List with query conditions</param>
    /// <returns>Document Id</returns>
    public override int GetDocumentByQuery(List<string> list)
    {
        try
        {
            _logger.Info(string.Format("GetDocumentByQuery with first condition {0} ...", list.First()));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["query"] = list[0];
            for (int i = 1; i < list.Count; ++i)
            {
                query.Add("query", list[i]);
            }

            using HttpResponseMessage response = client.GetAsync("objects/document?" + query).Result;
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception e)
            {
                _logger.Error(string.Format("GetDocumentByQuery failed with http status code {0} and error {1}", response.StatusCode, e.Message));

                _ = int.TryParse(response.StatusCode.ToString(), out int code);
                if (code < 500)
                {
                    return 0;
                }
                else
                {
                    throw;
                }
            }

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);
            JArray objects = jsonObject["Objects"] as JArray;

            if (objects.Count != 1)
            {
                if (objects.Count == 0)
                {
                    _logger.Error("GetDocumentByQuery failed with no document found");
                    return 0;
                }
                else
                {
                    _logger.Error("GetDocumentByQuery failed with more than one document found");
                    return -1;
                }
            }

            var profileObject = objects[0];

            int id = profileObject["Values"][string.Format("/Document/{0}", _identNo)].Value<int>();

            _logger.Info(string.Format("GetDocumentByQuery succeeded with id {0}", id.ToString()));

            return id;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("GetDocumentByQuery failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// DownloadFileById
    /// </summary>
    /// <param name="id">Document Id</param>
    public override void DownloadFileById(int id)
    {
        try
        {
            _logger.Info(string.Format("DownloadFileById {0} ...", id.ToString()));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            using HttpResponseMessage response = client.GetAsync(string.Format("objects/document/{0}/file", id),
                HttpCompletionOption.ResponseHeadersRead).Result; //is needed for downloading large files (the headers are returned as soon as possible, therefore: the timeout is no problem)
            response.EnsureSuccessStatusCode();

            using Stream resultStream = response.Content.ReadAsStreamAsync().Result;
            string fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            string fullPath = Path.Combine(_tempPath, fileName);

            _logger.Info(string.Format("DownloadFileById succeeded with fullPath {0}", fullPath));

            using FileStream fileStream = File.Create(fullPath);
            resultStream.CopyTo(fileStream);
            Process.Start(fileStream.Name); //open file with windows explorer
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("DownloadFileById failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// DownloadFileByLink
    /// </summary>
    /// <param name="link">fileLink</param>
    public override void DownloadFileByLink(string link)
    {
        try
        {
            _logger.Info(string.Format("DownloadFileByLink {0} ...", link));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            using HttpResponseMessage response = client.GetAsync(link,
                HttpCompletionOption.ResponseHeadersRead).Result; //is needed for downloading large files (the headers are returned as soon as possible, therefore: the timeout is no problem)
            response.EnsureSuccessStatusCode();

            using Stream resultStream = response.Content.ReadAsStreamAsync().Result;
            string fileName = response.Content.Headers.ContentDisposition.FileName.Trim('"');
            fileName = Path.Combine(_tempPath, fileName);

            _logger.Info(string.Format("DownloadFileByLink succeeded with filename {0}", fileName));

            using FileStream fileStream = File.Create(fileName);
            resultStream.CopyTo(fileStream);
            Process.Start(fileStream.Name); //open file with windows explorer
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("DownloadFileByLink failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// CreateDocument
    /// </summary>
    /// <param name="list">Dictionary with properties</param>
    /// <returns>selfLink</returns>
    public override string? CreateDocument(Dictionary<string, string> list)
    {
        try
        {
            var first = list.First();
            _logger.Info(String.Format("CreateDocument with first pair<{0}><{1}> ...", first.Key, first.Value));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            JObject content = CreateJObjectContent(list);
            var stringContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            //using HttpResponseMessage response = client.PostAsJsonAsync("objects/document", content).Result;
            using HttpResponseMessage response = client.PostAsync("objects/document", stringContent).Result;
            response.EnsureSuccessStatusCode();

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);
            JObject header = jsonObject["Header"] as JObject;

            string selfLink = header["Links"].First(t => t["Type"].Value<string>() == "self").Value<string>("Href");

            _logger.Info(string.Format("CreateDocument succeeded with selfLink {0}", selfLink));

            return selfLink;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("CreateDocument failed with error {0}", ex.Message));
            throw;
        }
    }

    /// <summary>
    /// ChangeDocument
    /// </summary>
    /// <param name="id">Document Id</param>
    /// <param name="list">Dictionary with properties</param>
    /// <returns>selfLink</returns>
    public override string? ChangeDocument(int id, Dictionary<string, string> list)
    {
        try
        {
            var first = list.First();
            _logger.Info(String.Format("ChangeDocument with first pair<{0}><{1}> ...", first.Key, first.Value));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            JObject content = CreateJObjectContent(list);
            var stringContent = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");

            //using HttpResponseMessage response = client.PutAsJsonAsync(string.Format("objects/document/{0}", id), content).Result;
            using HttpResponseMessage response = client.PutAsync(string.Format("objects/document/{0}", id), stringContent).Result;
            response.EnsureSuccessStatusCode();

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);
            JObject header = jsonObject["Header"] as JObject;

            string selfLink = header["Links"].First(t => t["Type"].Value<string>() == "self").Value<string>("Href");

            _logger.Info(string.Format("ChangeDocument succeeded with selfLink {0}", selfLink));

            return selfLink;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("ChangeDocument failed with error {0}", ex.Message));
            throw;
        }
    }


    /// <summary>
    /// UploadFileById
    /// </summary>
    /// <param name="id">Document Id</param>
    /// <param name="fullPath">Full path to file</param>
    /// <param name="newFileName">New filename to rename the file</param>
    /// <returns>fileLink</returns>
    public override string? UploadFileById(int id, string fullPath, string? newFileName = null)
    {
        try
        {
            _logger.Info(string.Format("UploadFileById with id {0} and fullPath {1} ...", id, fullPath));

            Uri baseUri = new(string.Format(_baseUri, _server, _mandant));

            using HttpClient client = new();
            client.BaseAddress = baseUri;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.ConnectionClose = true;

            var authenticationString = $"{_userName}:{Base64Decode(_password)}";
            client.DefaultRequestHeaders.Add("Authorization", $"Basic {Base64Encode(authenticationString)}");

            using MultipartFormDataContent content = new();
            string fileName;
            if (!string.IsNullOrEmpty(newFileName))
            {
                fileName = MakeValidFileName(newFileName) + Path.GetExtension(fullPath);
            }
            else
            {
                fileName = Path.GetFileName(fullPath);
            }

            using ByteArrayContent fileContent = new(File.ReadAllBytes(fullPath));
            content.Add(fileContent, "content", "\"" + fileName + "\"");

            using HttpResponseMessage response = client.PostAsync(string.Format("objects/document/{0}/file", id), content).Result;
            response.EnsureSuccessStatusCode();

            string resultContent = response.Content.ReadAsStringAsync().Result;

            JObject jsonObject = JObject.Parse(resultContent);
            JObject header = jsonObject["Header"] as JObject;

            string fileLink = header["Links"].First(t => t["Type"].Value<string>() == "file").Value<string>("Href");

            _logger.Info(string.Format("UploadFileById succeeded with fileLink {0}", fileLink));

            return fileLink;
        }
        catch (Exception ex)
        {
            _logger.Error(string.Format("UploadFileById failed with error {0}", ex.Message));
            throw;
        }
    }
}
