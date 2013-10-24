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
    public class FileTransferManager
    {
        private const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        int MaxBlockSize;
        const int WifiBuffer =  1048576;
        const int MobileBuffer =  102400;
        const int _defaultBlockSize = 1024;
        const int _noOfParallelRequest = 20;

        private static volatile FileTransferManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        Queue<HikeFileInfo> PendingTasks = new Queue<HikeFileInfo>();

        public Dictionary<string, HikeFileInfo> TaskMap = new Dictionary<string, HikeFileInfo>();

        public static FileTransferManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new FileTransferManager();
                    }
                }
                return instance;
            }
        }

        public FileTransferManager()
        {
            MaxBlockSize = WifiBuffer;
            ThreadPool.SetMaxThreads(_noOfParallelRequest, _noOfParallelRequest);
        }

        public void AddFileToUploadDownloadTask(string msisdn, string key, string fileName, string contentType, byte[] fileBytes, bool isDownload)
        {
            HikeFileInfo fInfo = new HikeFileInfo(msisdn, key, fileBytes, fileName, contentType, isDownload);

            PendingTasks.Enqueue(fInfo);
            TaskMap.Add(fInfo.SessionId, fInfo);
            SaveTaskData(fInfo);
            StartTask();
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public void ResumeTask(string key)
        {
            HikeFileInfo fInfo = null;
            if (TaskMap.TryGetValue(key, out fInfo))
            {
                fInfo.FileState = HikeFileState.STARTED;

                if (!PendingTasks.Contains(fInfo))
                    PendingTasks.Enqueue(fInfo);

                SaveTaskData(fInfo);
                StartTask();

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fInfo,true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
            }
        }

        public void StartTask()
        {
            if (PendingTasks.Count > 0)
            {
                HikeFileInfo fileInfo = PendingTasks.Dequeue();

                if (fileInfo.FileState == HikeFileState.CANCELED)
                {
                    TaskMap.Remove(fileInfo.SessionId);
                    DeleteTaskData(fileInfo.SessionId);
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1)
                {
                    fileInfo.FileState = HikeFileState.COMPLETED;
                    SaveTaskData(fileInfo);
                 
                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else if (fileInfo.FileState != HikeFileState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || fileInfo.FileState != HikeFileState.PAUSED))
                {
                    if (!BeginUploadDownload(fileInfo))
                        PendingTasks.Enqueue(fileInfo);
                }
            }

            if (PendingTasks.Count > 0)
                StartTask();
        }

        private Boolean BeginUploadDownload(HikeFileInfo fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                fileInfo.FileState = HikeFileState.FAILED;
                SaveTaskData(fileInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);

                return true;
            }
            else
            {
                if (fileInfo.IsDownload)
                    return ThreadPool.QueueUserWorkItem(BeginDownloadGetRequest, fileInfo);
                else
                    return ThreadPool.QueueUserWorkItem(BeginUploadGetRequest, fileInfo);
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
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        var fileNames = store.GetFileNames(FILE_TRANSFER_DIRECTORY_NAME + "\\*");

                        foreach (var fileName in fileNames)
                        {
                            HikeFileInfo fileInfo = new HikeFileInfo();
                            using (var file = store.OpenFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    fileInfo.Read(reader);
                                    reader.Close();
                                }
                            }

                            TaskMap.Add(fileName, fileInfo);
                            PendingTasks.Enqueue(fileInfo);
                        }
                    }

                    if (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && PendingTasks.Count > 0)
                        StartTask();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Load Uploads From File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void PauseTask(string id)
        {
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = HikeFileState.MANUAL_PAUSED;
                SaveTaskData(fInfo);
            }
        }

        public void CancelTask(string id)
        {
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = HikeFileState.CANCELED;
                SaveTaskData(fInfo);
            }
        }

        public void DeleteTask(string id)
        {
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
                fInfo.FileState = HikeFileState.CANCELED;

            DeleteTaskData(id);
        }

        public void DeleteTaskData(string id)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
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

        void SaveTaskData(HikeFileInfo fileInfo)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + fileInfo.SessionId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME);

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

        #region Download Http Request

        void BeginDownloadGetRequest(object obj)
        {
            var fileInfo = obj as HikeFileInfo;

            var req = HttpWebRequest.Create(new Uri(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + fileInfo.FileName)) as HttpWebRequest;
            req .AllowReadStreamBuffering = false;
            req.Method = "GET";
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
            req.Headers["Range"] = string.Format("bytes={0}-", fileInfo.CurrentHeaderPosition);

            req.BeginGetResponse(DownloadGetResponseCallback, new object[] { req, fileInfo });
        }

        void DownloadGetResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            var myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            Stream responseStream = null;
            HttpStatusCode responseCode = HttpStatusCode.NotFound;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                responseCode = response.StatusCode;
                responseStream = response.GetResponseStream();
            }
            catch (Exception e)
            {
                Debug.WriteLine("BackgroundUploader ::  GetRequestCallback :  GetRequestCallback , Exception : " + e.StackTrace);
                responseStream = null;

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
                var fileInfo = vars[1] as HikeFileInfo;

                if (response != null)
                {
                    if (fileInfo.FileBytes == null)
                    {
                        fileInfo.TotalBytes = (int)response.ContentLength;
                        fileInfo.FileBytes = new byte[fileInfo.TotalBytes];
                    }

                    fileInfo.FileState = HikeFileState.STARTED;

                    SaveTaskData(fileInfo);

                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }

                ProcessDownloadGetResponse(responseStream, responseCode, fileInfo);
            }
        }

        void ProcessDownloadGetResponse(Stream responseStream, HttpStatusCode responseCode, HikeFileInfo fileInfo)
        {
            if (fileInfo.FileState == HikeFileState.CANCELED)
            {
                FileTransferManager.Instance.DeleteTaskData(fileInfo.SessionId);
            }
            else if (responseCode == HttpStatusCode.PartialContent || responseCode == HttpStatusCode.OK)
            {
                byte[] newBytes = null;
                using (BinaryReader br = new BinaryReader(responseStream))
                {
                    while (fileInfo.BytesTransfered != fileInfo.TotalBytes && fileInfo.FileState == HikeFileState.STARTED)
                    {
                        newBytes = br.ReadBytes(fileInfo.BlockSize);

                        if (newBytes.Length == 0)
                            break;

                        Array.Copy(newBytes, 0, fileInfo.FileBytes, fileInfo.CurrentHeaderPosition, newBytes.Length);
                        fileInfo.CurrentHeaderPosition += newBytes.Length;

                        var newSize = (fileInfo.AttemptNumber + fileInfo.AttemptNumber) * _defaultBlockSize;

                        if (newSize <= MaxBlockSize)
                        {
                            fileInfo.AttemptNumber += fileInfo.AttemptNumber;
                            fileInfo.BlockSize = fileInfo.AttemptNumber * _defaultBlockSize;
                        }

                        SaveTaskData(fileInfo);

                        if (UpdateTaskStatusOnUI != null)
                            UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, false));
                    }

                    if (fileInfo.FileState == HikeFileState.CANCELED)
                    {
                        FileTransferManager.Instance.DeleteTaskData(fileInfo.SessionId);
                    } 
                    else if (fileInfo.BytesTransfered == fileInfo.TotalBytes)
                    {
                        fileInfo.FileState = HikeFileState.COMPLETED;

                        SaveTaskData(fileInfo);

                        if (UpdateTaskStatusOnUI != null)
                            UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, false));
                    }
                    else if (newBytes.Length == 0)
                    {
                        fileInfo.FileState = HikeFileState.PAUSED;
                        SaveTaskData(fileInfo);

                        if (!PendingTasks.Contains(fileInfo))
                        {
                            PendingTasks.Enqueue(fileInfo);
                            StartTask();
                        }

                        if (UpdateTaskStatusOnUI != null)
                            UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                        App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                    }
                }
            }
            else if (responseCode == HttpStatusCode.BadRequest)
            {
                fileInfo.FileState = HikeFileState.FAILED;
                SaveTaskData(fileInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
            }
            else
            {
                fileInfo.FileState = HikeFileState.PAUSED;
                SaveTaskData(fileInfo);

                if (!PendingTasks.Contains(fileInfo))
                {
                    PendingTasks.Enqueue(fileInfo);

                    StartTask();
                }

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
            }
        }


        #endregion

        #region Upload Http Request

        string _boundary = "----------V2ymHFg03ehbqgZCaKO6jy";

        void BeginUploadGetRequest(object obj)
        {
            var fileInfo =  obj as HikeFileInfo;

            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
            
            AccountUtils.addToken(req);
            
            req.Method = "GET";
            
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = fileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = fileInfo.SessionId;
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();

            req.BeginGetResponse(UploadGetResponseCallback, new object[] { req, fileInfo });
        }

        void UploadGetResponseCallback(IAsyncResult result)
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
                var fileInfo = vars[1] as HikeFileInfo;
                ProcessUploadGetResponse(data, responseCode, fileInfo);
            }
        }

        void ProcessUploadGetResponse(string data, HttpStatusCode responseCode, HikeFileInfo fileInfo)
        {
            int index = 0;
            if (responseCode == HttpStatusCode.OK)
            {
                index = Convert.ToInt32(data);

                if (fileInfo.FileBytes.Length - 1 == index)
                {
                    fileInfo.FileState = HikeFileState.COMPLETED;
                    SaveTaskData(fileInfo);

                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo,true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else
                {
                    fileInfo.CurrentHeaderPosition = index + 1;
                    fileInfo.FileState = HikeFileState.STARTED;

                    SaveTaskData(fileInfo);

                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo,true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                    
                    BeginUploadPostRequest(fileInfo);
                }
            }
            else if (responseCode == HttpStatusCode.NotFound)
            {
                // fresh upload
                fileInfo.CurrentHeaderPosition = index;
                fileInfo.FileState = HikeFileState.STARTED;
                SaveTaskData(fileInfo);

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo,true));

                BeginUploadPostRequest(fileInfo);
            }
        }

        void BeginUploadPostRequest(HikeFileInfo fileInfo)
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

            req.BeginGetRequestStream(UploadPostRequestCallback, new object[] { req, bytesToUpload, fileInfo });
        }

        byte[] getMultiPartBytes(byte[] data, Dictionary<string, string> param, HikeFileInfo fileInfo)
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

        String getBoundaryMessage(Dictionary<string, string> param, HikeFileInfo fileInfo)
        {
            String res = "--" + _boundary + "\r\n";

            var keys = param.Keys;

            foreach (var keyValue in param)
                res += "Content-Disposition: form-data; name=\"" + keyValue.Key + "\"\r\n" + "\r\n" + keyValue.Value + "\r\n" + "--" + _boundary + "\r\n";

            res += "Content-Disposition: form-data; name=\"file\"; filename=\"" + fileInfo.FileName + "\"\r\n" + "Content-Type: " + fileInfo.ContentType + "\r\n\r\n";

            return res;
        }

        void UploadPostRequestCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            JObject data = new JObject();
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            byte[] dataBytes = (byte[])vars[1];
            postStream.Write(dataBytes, 0, dataBytes.Length);
            postStream.Close();
            postStream.Close();
            var fileInfo = vars[2] as HikeFileInfo;
            req.BeginGetResponse(UploadPostResponseCallback, new object[] { req, fileInfo });
        }

        void UploadPostResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];

            //Deployment.Current.Dispatcher.BeginInvoke(() =>
            //{
            //    try
            //    {
            //        var netInterface = myHttpWebRequest.GetCurrentNetworkInterface();
            //        MaxBlockSize = (netInterface.InterfaceSubtype == NetworkInterfaceSubType.Cellular_EDGE 
            //            || netInterface.InterfaceSubtype == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;

            //        System.Diagnostics.Debug.WriteLine(netInterface.InterfaceType.ToString());
            //    }
            //    catch (NetworkException networkException)
            //    {
            //        if (networkException.NetworkErrorCode == NetworkError.WebRequestAlreadyFinished)
            //        {
            //            System.Diagnostics.Debug.WriteLine("Cannot call GetCurrentNetworkInterface if the webrequest is already complete");
            //        }
            //    }
            //}); 
            
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
                var fileInfo = vars[1] as HikeFileInfo;
                ProcessUploadPostResponse(data, responseCode, fileInfo);
            }
        }

        void ProcessUploadPostResponse(string data, HttpStatusCode code, HikeFileInfo fileInfo)
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

                    if (fileInfo.FileState == HikeFileState.STARTED)
                    {
                        fileInfo.FileState = HikeFileState.COMPLETED;

                        if (UpdateTaskStatusOnUI != null)
                            UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));
                     
                        App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                    }

                    SaveTaskData(fileInfo);
                }
            }
            else if (code == HttpStatusCode.Created)
            {
                if (fileInfo.FileState == HikeFileState.CANCELED)
                {
                    FileTransferManager.Instance.DeleteTaskData(fileInfo.SessionId);
                }
                else
                {
                    fileInfo.CurrentHeaderPosition += fileInfo.BlockSize;

                    if (fileInfo.FileState == HikeFileState.STARTED)
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

                    SaveTaskData(fileInfo);

                    if (fileInfo.FileState == HikeFileState.STARTED || (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && fileInfo.FileState != HikeFileState.MANUAL_PAUSED))
                        BeginUploadPostRequest(fileInfo);
                   
                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, false));
                }
            }
            else if (code == HttpStatusCode.BadRequest)
            {
                fileInfo.FileState = HikeFileState.FAILED;
                SaveTaskData(fileInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo,true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
            }
            else
            {
                //app suspension and disconnected case
                fileInfo.FileState = HikeFileState.PAUSED;
                SaveTaskData(fileInfo);

                if (!PendingTasks.Contains(fileInfo))
                {
                    PendingTasks.Enqueue(fileInfo);

                    StartTask();
                }

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
            }
        }

        #endregion

        public event EventHandler<TaskCompletedArgs> UpdateTaskStatusOnUI;
    }

    public class TaskCompletedArgs : EventArgs
    {
        public TaskCompletedArgs(HikeFileInfo fileInfo, bool isStateChanged)
        {
            FileInfo = fileInfo;
            IsStateChanged = isStateChanged;
        }

        public HikeFileInfo FileInfo { get; private set; }
        public bool IsStateChanged { get; private set; }
    }
}
