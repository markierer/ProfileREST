# ProfileREST
Example: A business application creates pdf files and put them into a sync folder. The metadata is within the file name.
The Profile.DocumentService monitors the sync folder, parses and sends files to the PRO.FILE WebApp Server.
PRO.FILE is a PDM/PLM system from Revalize (Procad)


## appsettings.json
The following appSettings must be configured.
```javascript
{
  "appSettings": {
    "syncAuto": "true", // true: Monitor the sync folder with FileSystemWatcher / false: Read the files in the sync folder just once
    "syncPath": "\\\\fileServer\\ProfileDir\\export", // Path to sync folder
    "syncPathSuccessed": "\\\\fileServer\\ProfileDir\\successful", // Copy location for successfully uploaded files / empty: Files are deleted after an upload
    "syncPathFailed": "\\\\fileServer\\ProfileDir\\failed", // Copy location for files that failed to upload / empty: Files are deleted after a failed attempt
    "syncFileExtension": "pdf", // File extension to monitor
    "profileBaseUri": "http://{0}/procad/profile/api/{1}/", // Basic Uri to PRO.FILE WebApp Server
    "profileServer": "profileServer", // Server name of the PRO.FILE WebApp Server
    "profileMandant": "dbName", // Database name of the PRO.FILE WebApp Server
    "profileUserName": "procad", // PRO.FILE username
    "profilePassword": "1234", // PRO.FILE Passwort Base64 coded
    "profileTempPath": "C:\temp", // Temporary path to download the files
    "profileIdentNo": "IDNR", // Neutral name of the document ID
    "profileVersion": "2", // Try version 1 or 2 for different authentication strategies
    "fileNameSeparator": "@@", // Separator in file name
    "fileNameRename": "fileNameElement3", // File name element becomes new file name before upload / empty: file name remains
    "docBase": "/Document/" // docBase path (standard)
  }
}
```
Assign the file name elements to the document properties.
Example: NY@@23@@4@@100000@@70000@@999.95@@USD@@2015-02-30T00_00_00.pdf
```javascript
{
  "fileNameElement": {
    "fileNameElement0": "docMandant",
    "fileNameElement1": "docType",
    "fileNameElement2": "docSAPDocTypeId",
    "fileNameElement3": "docNumber",
    "fileNameElement4": "docSAPBPCode",
    "fileNameElement5": "docValue",
    "fileNameElement6": "docCurrency",
    "fileNameElement7": "docDate"
  }
}
```
The document types are configured here.
If a docFixProperty = docType, a document type is assigned via the docType ID.
```javascript
{
  "docType": {
    "23": "docKundenangebot/",
    "17": "docKundenauftrag/",
    "15": "docLieferung/",
    "203": "docAnzahlungsrechnung/",
    "13": "docAusgangsrechnung/",
    "14": "docAusgangsgutschrift/",
    "-1": "docProformarechnung/",
    "18": "docEingangsrechnung/",
    "22": "docLieferantenbestellung/"
  }
}
```
The docFixProperty are configured here.
These are fields from the base of /Document/
A single docFixProperty must be equal to docType.
File name elements can be assigned (fileNameElement). Otherwise the entry will be interpreted as text.

If a value "Date" contains a date, a date is expected and _ is replaced by :.
Attention: The date must be in the following format yyyy-mm-ddThh:mi:ss.mmm (no spaces), ISO8601 (MSSQL convert code 126)
```javascript
{
  "docFixProperty": {
    "docFixProperty0": "docType",
    "docFixProperty1": "docSAPBPCode"
  },
  "docFixValue": {
    "docFixValue0": "fileNameElement1",
    "docFixValue1": "fileNameElement4"
  }
}
```
The docVarProperty are configured here.
These are fields from the variable root /Document/docType/
File name elements can be assigned (fileNameElement). Otherwise the entry will be interpreted as text.

If a value "Date" contains a date, a date is expected and _ is replaced by :.
Attention: The date must be in the following format yyyy-mm-ddThh:mi:ss.mmm (no spaces), ISO8601 (MSSQL convert code 126)
```javascript
{
  "docVarProperty": {
    "docVarProperty0": "docMandant",
    "docVarProperty1": "docNumber",
    "docVarProperty2": "docValue",
    "docVarProperty3": "docCurrency",
    "docVarProperty4": "docDate",
    "docVarProperty5": "docSAPObjectType",
    "docVarProperty6": "docSAPDocTypeId",
    "docVarProperty7": "docRemarks"
  },
  "docVarValue": {
    "docVarValue0": "fileNameElement0",
    "docVarValue1": "fileNameElement3",
    "docVarValue2": "fileNameElement5",
    "docVarValue3": "fileNameElement6",
    "docVarValue4": "fileNameElement7",
    "docVarValue5": "fileNameElement1",
    "docVarValue6": "fileNameElement2",
    "docVarValue7": "Von SAP automatisch hochgeladen"
  }
}
```
The search query is configured here.
Before a document is created and a file is uploaded, it can be checked whether the document already exists.
In this case the document will be updated. Attention: The query must deliver unequivocally results!
```javascript
{
  "queryElementLeft": {
    "queryElementLeft0": "docFixProperty0",
    "queryElementLeft1": "docVarProperty0",
    "queryElementLeft2": "docVarProperty1"
  },
  "queryElementOperator": {
    "queryElementOperator0": "=",
    "queryElementOperator1": "=",
    "queryElementOperator2": "="
  },
  "queryElementRight": {
    "queryElementRight0": "docFixValue0",
    "queryElementRight1": "docVarValue0",
    "queryElementRight2": "docVarValue1"
  }
}
```
