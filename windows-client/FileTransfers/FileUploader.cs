using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using System.ComponentModel;
using windows_client.DbUtils;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading;
using System.Net;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using System.Windows;

namespace windows_client.FileTransfers
{
    public class FileUploader
    {
        private const string UPLOAD_DIRECTORY_NAME = "FileUpload";
        int MaxBlockSize;
        const int WifiBuffer =  1048576;
        const int MobileBuffer =  102400;
        const int _defaultBlockSize = 1024;
        const int _noOfParallelRequest = 20;

        private static volatile FileUploader instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        Queue<UploadFileInfo> Sources = new Queue<UploadFileInfo>();

        public Dictionary<string, UploadFileInfo> UploadMap = new Dictionary<string, UploadFileInfo>();

        public static FileUploader Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FileUploader();
                    }
                }
                return instance;
            }
        }

        public FileUploader()
        {
            MaxBlockSize = WifiBuffer;
            ThreadPool.SetMaxThreads(_noOfParallelRequest, _noOfParallelRequest);
        }

        public void Upload(string msisdn, string key, string fileName, string contentType, byte[] fileBytes)
        {
            UploadFileInfo fInfo = new UploadFileInfo(msisdn, key, fileBytes, fileName, contentType);

            Sources.Enqueue(fInfo);
            UploadMap.Add(fInfo.SessionId, fInfo);
            SaveUploadData(fInfo);
            StartUpload();
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public void ResumeUpload(string key)
        {
            UploadFileInfo fInfo = null;
            if (UploadMap.TryGetValue(key, out fInfo))
            {
                fInfo.FileState = UploadFileState.STARTED;

                if (!Sources.Contains(fInfo))
                    Sources.Enqueue(fInfo);

                SaveUploadData(fInfo);
                StartUpload();

                if (UpdateFileUploadStatusOnUI != null)
                    UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fInfo,true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
            }
        }

        public void StartUpload()
        {
            if (Sources.Count > 0)
            {
                UploadFileInfo fileInfo = Sources.Dequeue();

                if (fileInfo.FileState == UploadFileState.CANCELED)
                {
                    UploadMap.Remove(fileInfo.SessionId);
                    DeleteUploadData(fileInfo.SessionId);
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1)
                {
                    fileInfo.FileState = UploadFileState.COMPLETED;
                    SaveUploadData(fileInfo);
                 
                    if (UpdateFileUploadStatusOnUI != null)
                        UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo, true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else if (fileInfo.FileState != UploadFileState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || fileInfo.FileState != UploadFileState.PAUSED))
                {
                    if (!Upload(fileInfo))
                        Sources.Enqueue(fileInfo);
                }
            }

            if (Sources.Count > 0)
                StartUpload();
        }

        private Boolean Upload(UploadFileInfo fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                fileInfo.FileState = UploadFileState.FAILED;
                SaveUploadData(fileInfo);

                if (UpdateFileUploadStatusOnUI != null)
                    UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                
                return true;
            }
            else
            {
                return ThreadPool.QueueUserWorkItem(BeginGetRequest, fileInfo);
            }
        }

        public void PopulatePreviousUploads()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(UPLOAD_DIRECTORY_NAME))
                            return;

                        var fileNames = store.GetFileNames(UPLOAD_DIRECTORY_NAME + "\\*");

                        foreach (var fileName in fileNames)
                        {
                            UploadFileInfo fileInfo = new UploadFileInfo();
                            using (var file = store.OpenFile(UPLOAD_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    fileInfo.Read(reader);
                                    reader.Close();
                                }
                            }

                            UploadMap.Add(fileName, fileInfo);
                            Sources.Enqueue(fileInfo);
                        }
                    }

                    if (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && Sources.Count > 0)
                        StartUpload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Load Uploads From File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void PauseUploadTask(string id)
        {
            UploadFileInfo fInfo;

            if (UploadMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = UploadFileState.MANUAL_PAUSED;
                SaveUploadData(fInfo);
            }
        }

        public void CancelUploadTask(string id)
        {
            UploadFileInfo fInfo;

            if (UploadMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = UploadFileState.CANCELED;
                SaveUploadData(fInfo);
            }
        }

        public void DeleteUploadTask(string id)
        {
            UploadFileInfo fInfo;

            if (UploadMap.TryGetValue(id, out fInfo))
                fInfo.FileState = UploadFileState.CANCELED;

            DeleteUploadData(id);
        }

        public void DeleteUploadData(string id)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = UPLOAD_DIRECTORY_NAME + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(UPLOAD_DIRECTORY_NAME))
                            return;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Delete Upload From IS, Exception : " + ex.StackTrace);
                }
            }
        }

        void SaveUploadData(UploadFileInfo fileInfo)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = UPLOAD_DIRECTORY_NAME + "\\" + fileInfo.SessionId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(UPLOAD_DIRECTORY_NAME))
                            store.CreateDirectory(UPLOAD_DIRECTORY_NAME);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                fileInfo.Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Save Upload Status To IS, Exception : " + ex.StackTrace);
                }
            }
        }

        #region Upload Http Request

        string _boundary = "----------V2ymHFg03ehbqgZCaKO6jy";

        void BeginGetRequest(object obj)
        {
            var fileInfo =  obj as UploadFileInfo;

            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
            
            AccountUtils.addToken(req);
            
            req.Method = "GET";
            
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = fileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = fileInfo.SessionId;
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();

            req.BeginGetResponse(GetResponseCallback, new object[] { req, fileInfo });
        }

        void GetResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            var myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            string data = null;
            HttpStatusCode responseCode = HttpStatusCode.NotFound;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                responseCode = response.StatusCode;
                Stream responseStream = response.GetResponseStream();
                using (var reader = new StreamReader(responseStream))
                {
                    data = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("BackgroundUploader ::  GetRequestCallback :  GetRequestCallback , Exception : " + e.StackTrace);
                data = null;

                var webException = e as WebException;
                if (webException != null)
                {
                    HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                    if (webResponse != null)
                        responseCode = webResponse.StatusCode;
                    else
                        responseCode = HttpStatusCode.RequestTimeout;
                }
            }
            finally
            {
                var fileInfo = vars[1] as UploadFileInfo;
                ProcessGetResponse(data, responseCode, fileInfo);
            }
        }

        void ProcessGetResponse(string data, HttpStatusCode responseCode, UploadFileInfo fileInfo)
        {
            int index = 0;
            if (responseCode == HttpStatusCode.OK)
            {
                index = Convert.ToInt32(data);

                if (fileInfo.FileBytes.Length - 1 == index)
                {
                    fileInfo.FileState = UploadFileState.COMPLETED;
                    SaveUploadData(fileInfo);

                    if (UpdateFileUploadStatusOnUI != null)
                        UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo,true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else
                {
                    fileInfo.CurrentHeaderPosition = index + 1;
                    fileInfo.FileState = UploadFileState.STARTED;

                    SaveUploadData(fileInfo);

                    if (UpdateFileUploadStatusOnUI != null)
                        UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo,true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                    
                    BeginPostRequest(fileInfo);
                }
            }
            else if (responseCode == HttpStatusCode.NotFound)
            {
                // fresh upload
                fileInfo.CurrentHeaderPosition = index;
                fileInfo.FileState = UploadFileState.STARTED;
                SaveUploadData(fileInfo);

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);

                if (UpdateFileUploadStatusOnUI != null)
                    UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo,true));

                BeginPostRequest(fileInfo);
            }
        }

        void BeginPostRequest(UploadFileInfo fileInfo)
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
           
            AccountUtils.addToken(req);

            req.Method = "POST";
            
            req.ContentType = string.Format("multipart/form-data; boundary={0}", _boundary);
            
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = fileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = fileInfo.SessionId;

            var bytesLeft = fileInfo.FileBytes.Length - fileInfo.CurrentHeaderPosition;
            fileInfo.BlockSize = bytesLeft >= fileInfo.BlockSize ? fileInfo.BlockSize : bytesLeft;

            var endPosition = fileInfo.CurrentHeaderPosition + fileInfo.BlockSize;
            endPosition -= 1;

            req.Headers["X-CONTENT-RANGE"] = string.Format("bytes {0}-{1}/{2}", fileInfo.CurrentHeaderPosition, endPosition, fileInfo.FileBytes.Length);

            var partialDataBytes = new byte[endPosition - fileInfo.CurrentHeaderPosition + 1];
            Array.Copy(fileInfo.FileBytes, fileInfo.CurrentHeaderPosition, partialDataBytes, 0, endPosition - fileInfo.CurrentHeaderPosition + 1);

            var param = new Dictionary<string, string>();
            param.Add("Cookie", req.Headers["Cookie"]);
            param.Add("X-SESSION-ID", req.Headers["X-SESSION-ID"]);
            param.Add("X-CONTENT-RANGE",req.Headers["X-CONTENT-RANGE"]);
            var bytesToUpload = getMultiPartBytes(partialDataBytes, param, fileInfo);

            req.BeginGetRequestStream(PostRequestCallback, new object[] { req, bytesToUpload, fileInfo });
        }

        byte[] getMultiPartBytes(byte[] data, Dictionary<string, string> param, UploadFileInfo fileInfo)
        {
            String boundaryMessage = getBoundaryMessage(param, fileInfo);
            String endBoundary = "\r\n--" + _boundary + "--\r\n";

            var bos = new MemoryStream();
            
            var msg = Encoding.UTF8.GetBytes(boundaryMessage);
            bos.Write(msg, 0, msg.Length);

            bos.Write(data, 0, data.Length);

            msg = Encoding.UTF8.GetBytes(endBoundary);
            bos.Write(msg, 0, msg.Length);
            
            bos.Close();

            return bos.ToArray();
        }

        String getBoundaryMessage(Dictionary<string, string> param, UploadFileInfo fileInfo)
        {
            String res = "--" + _boundary + "\r\n";

            var keys = param.Keys;

            foreach (var keyValue in param)
                res += "Content-Disposition: form-data; name=\"" + keyValue.Key + "\"\r\n" + "\r\n" + keyValue.Value + "\r\n" + "--" + _boundary + "\r\n";

            res += "Content-Disposition: form-data; name=\"file\"; filename=\"" + fileInfo.FileName + "\"\r\n" + "Content-Type: " + fileInfo.ContentType + "\r\n\r\n";

            return res;
        }

        void PostRequestCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            JObject data = new JObject();
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            byte[] dataBytes = (byte[])vars[1];
            postStream.Write(dataBytes, 0, dataBytes.Length);
            postStream.Close();
            postStream.Close();
            var fileInfo = vars[2] as UploadFileInfo;
            req.BeginGetResponse(PostResponseCallback, new object[] { req, fileInfo });
        }

        void PostResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    var netInterface = myHttpWebRequest.GetCurrentNetworkInterface();
                    MaxBlockSize = (netInterface.InterfaceSubtype == NetworkInterfaceSubType.Cellular_EDGE 
                        || netInterface.InterfaceSubtype == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;

                    System.Diagnostics.Debug.WriteLine(netInterface.InterfaceType.ToString());
                }
                catch (NetworkException networkException)
                {
                    if (networkException.NetworkErrorCode == NetworkError.WebRequestAlreadyFinished)
                    {
                        System.Diagnostics.Debug.WriteLine("Cannot call GetCurrentNetworkInterface if the webrequest is already complete");
                    }
                }
            }); 
            
            HttpWebResponse response = null;

            string data = null;

            HttpStatusCode responseCode =  HttpStatusCode.NotFound;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                responseCode = response.StatusCode;
                Stream responseStream = response.GetResponseStream();
                using (var reader = new StreamReader(responseStream))
                {
                    data = reader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("BackgroundUploader ::  UploadResponseCallback :  UploadResponseCallback , Exception : " + e.StackTrace);
                data = null;

                var webException = e as WebException;
                if (webException != null)
                {
                    HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                    if (webResponse != null)
                        responseCode = webResponse.StatusCode;
                    else
                        responseCode = HttpStatusCode.RequestTimeout;
                }
            }
            finally
            {
                var fileInfo = vars[1] as UploadFileInfo;
                ProcessPostResponse(data, responseCode, fileInfo);
            }
        }

        void ProcessPostResponse(string data, HttpStatusCode code, UploadFileInfo fileInfo)
        {
            JObject jObject = null;

            if (code == HttpStatusCode.OK)
            {
                if (!String.IsNullOrEmpty(data))
                    jObject = JObject.Parse(data);

                if (jObject != null)
                {
                    fileInfo.SuccessObj = jObject;
                    fileInfo.CurrentHeaderPosition = fileInfo.TotalBytes;

                    if (fileInfo.FileState == UploadFileState.STARTED)
                    {
                        fileInfo.FileState = UploadFileState.COMPLETED;

                        if (UpdateFileUploadStatusOnUI != null)
                            UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo, true));
                     
                        App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                    }

                    SaveUploadData(fileInfo);
                }
            }
            else if (code == HttpStatusCode.Created)
            {
                if (fileInfo.FileState == UploadFileState.CANCELED)
                {
                    FileUploader.Instance.DeleteUploadData(fileInfo.SessionId);
                }
                else
                {
                    fileInfo.CurrentHeaderPosition += fileInfo.BlockSize;

                    if (fileInfo.FileState == UploadFileState.STARTED)
                    {
                        var newSize = (fileInfo.AttemptNumber + fileInfo.AttemptNumber) * _defaultBlockSize;

                        if (newSize <= MaxBlockSize)
                        {
                            fileInfo.AttemptNumber += fileInfo.AttemptNumber;
                            fileInfo.BlockSize = fileInfo.AttemptNumber * _defaultBlockSize;
                        }
                    }
                    else
                    {
                        fileInfo.AttemptNumber = 1;
                        fileInfo.BlockSize = _defaultBlockSize;
                    }

                    SaveUploadData(fileInfo);

                    if (fileInfo.FileState == UploadFileState.STARTED || (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && fileInfo.FileState != UploadFileState.MANUAL_PAUSED))
                        BeginPostRequest(fileInfo);
                   
                    if (UpdateFileUploadStatusOnUI != null)
                        UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo, false));
                }
            }
            else if (code == HttpStatusCode.BadRequest)
            {
                fileInfo.FileState = UploadFileState.FAILED;
                SaveUploadData(fileInfo);

                if (UpdateFileUploadStatusOnUI != null)
                    UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo,true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
            }
            else
            {
                //app suspension and disconnected case
                fileInfo.FileState = UploadFileState.PAUSED;
                SaveUploadData(fileInfo);

                if (!Sources.Contains(fileInfo))
                {
                    Sources.Enqueue(fileInfo);

                    StartUpload();
                }

                if (UpdateFileUploadStatusOnUI != null)
                    UpdateFileUploadStatusOnUI(null, new UploadCompletedArgs(fileInfo, true));
            }
        }

        #endregion

        public event EventHandler<UploadCompletedArgs> UpdateFileUploadStatusOnUI;
    }

    public class UploadCompletedArgs : EventArgs
    {
        public UploadCompletedArgs(UploadFileInfo fileInfo, bool isStateChanged)
        {
            FileInfo = fileInfo;
            IsStateChanged = isStateChanged;
        }

        public UploadFileInfo FileInfo { get; private set; }
        public bool IsStateChanged { get; private set; }
    }
}
