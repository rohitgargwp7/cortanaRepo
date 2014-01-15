using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using windows_client.DbUtils;
using Newtonsoft.Json.Linq;
using System.IO;
using windows_client.Misc;
using windows_client.utils;
using System.IO.IsolatedStorage;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Net.Http;

namespace windows_client.FileTransfers
{
    public class FileUploader : FileInfoBase
    {
        static object readWriteLock = new object();
        string _boundary = "----------V2ymHFg03ehbqgZCaKO6jy";
        public string Id { get; set; }
        public JObject SuccessObj { get; set; }

        public FileUploader()
            : base()
        {
        }

        public FileUploader(string msisdn, string messageId, string fileName, string contentType, int size)
            : base(msisdn, messageId, fileName, contentType, size)
        {
            Id = Guid.NewGuid().ToString();

            Save();
        }

        public override void Write(BinaryWriter writer)
        {
            if (Id == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Id);

            if (Msisdn == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Msisdn);

            if (MessageId == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(MessageId);

            writer.Write(CurrentHeaderPosition);

            if (SuccessObj == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(SuccessObj.ToString(Newtonsoft.Json.Formatting.None));

            if (FileName == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(FileName);

            if (ContentType == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(ContentType);

            writer.Write((int)FileState);

            writer.Write(TotalBytes);
        }

        public override void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Id == "*@N@*")
                Id = null;

            count = reader.ReadInt32();
            Msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Msisdn == "*@N@*")
                Msisdn = null;

            count = reader.ReadInt32();
            MessageId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (MessageId == "*@N@*")
                MessageId = null;

            CurrentHeaderPosition = reader.ReadInt32();

            count = reader.ReadInt32();
            var str = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (str == "*@N@*")
                SuccessObj = null;
            else
                SuccessObj = JObject.Parse(str);

            count = reader.ReadInt32();
            FileName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (FileName == "*@N@*")
                FileName = null;

            count = reader.ReadInt32();
            ContentType = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (ContentType == "*@N@*")
                ContentType = null;

            FileState = (FileTransferState)reader.ReadInt32();

            if (App.appSettings.Contains(App.AUTO_RESUME_SETTING) && FileState == FileTransferState.STARTED)
                FileState = FileTransferState.PAUSED;

            TotalBytes = reader.ReadInt32();
        }

        public override void Save()
        {
            lock (readWriteLock)
            {
                if (FileState == FileTransferState.CANCELED)
                    return;

                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + MessageId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME);

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FileUploader :: Save Upload Status To IS, Exception : " + ex.StackTrace);
                }
            }
        }

        public override void Delete()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + MessageId;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                            return;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FileUploader :: Delete Upload From IS, Exception : " + ex.StackTrace);
                }
            }
        }

        public override void Start(object obj)
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;

            if (!App.appSettings.Contains(App.UID_SETTING))
            {
                Delete();
                return;
            }

            AccountUtils.AddToken(req);
            req.Method = "GET";

            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = FileName;
            req.Headers["X-SESSION-ID"] = Id;
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();

            req.BeginGetResponse(UploadGetResponseCallback, new object[] { req });
        }

        public async override void CheckIfComplete()
        {
            if (SuccessObj != null)
            {
                var jData = SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                var fileKey = jData[HikeConstants.FILE_KEY].ToString();
                var result = await CheckForCRC(fileKey);

                if (result)
                {
                    FileState = FileTransferState.COMPLETED;
                    Save();
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                    return;
                }
            }

            FileState = FileTransferState.FAILED;
            Delete();
            OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
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
                Logging.LogWriter.Instance.WriteToLog("FileUploader ::  UploadGetResponseCallback :  UploadGetResponseCallback , Exception : " + e.StackTrace);
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
                else
                    responseCode = HttpStatusCode.RequestTimeout;
            }
            finally
            {
                ProcessUploadGetResponse(data, responseCode);
            }
        }

        async void ProcessUploadGetResponse(string data, HttpStatusCode responseCode)
        {
            if (FileState == FileTransferState.CANCELED)
            {
                // if state was cancelled before download began delete the data
                Delete();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                return;
            }

            int index = 0;
            if (responseCode == HttpStatusCode.OK && !String.IsNullOrEmpty(data) && Int32.TryParse(data, out index))
            {
                if (TotalBytes - 1 == index)
                {
                    // if the task was paused on the last chunk. This condition will be triggered on resume
                    // check for successful upload
                    if (SuccessObj != null)
                    {
                        var jData = SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                        var fileKey = jData[HikeConstants.FILE_KEY].ToString();
                        var result = await CheckForCRC(fileKey);

                        if (result)
                        {
                            FileState = FileTransferState.COMPLETED;
                            Save();
                            OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                            return;
                        }
                    }

                    // if not uploaded succssfully, need to start from begining
                    // mark as failed and delete the data to restart the upload next time.
                    FileState = FileTransferState.FAILED;
                    Delete();
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                }
                else
                {
                    // resume upload from last position and update state

                    CurrentHeaderPosition = index + 1;
                    FileState = FileTransferState.STARTED;
                    Save();
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                    BeginUploadPostRequest();
                }
            }
            else if (responseCode == HttpStatusCode.NotFound)
            {
                // fresh upload

                // to be safe create new GUID
                Id = Guid.NewGuid().ToString();

                CurrentHeaderPosition = index;
                FileState = FileTransferState.STARTED;
                Save();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                BeginUploadPostRequest();
            }
            else
            {
                FileState = FileTransferState.FAILED;
                Save();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
            }
        }

        void BeginUploadPostRequest()
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;

            if (!App.appSettings.Contains(App.UID_SETTING))
            {
                Delete();
                return;
            }

            AccountUtils.AddToken(req);

            req.Method = "POST";
            req.ContentType = string.Format("multipart/form-data; boundary={0}", _boundary);
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = FileName;
            req.Headers["X-SESSION-ID"] = Id;

            var bytesLeft = TotalBytes - CurrentHeaderPosition;
            BlockSize = bytesLeft >= BlockSize ? BlockSize : bytesLeft;

            var endPosition = CurrentHeaderPosition + BlockSize;
            endPosition -= 1;

            req.Headers["X-CONTENT-RANGE"] = string.Format("bytes {0}-{1}/{2}", CurrentHeaderPosition, endPosition, TotalBytes);

            var partialDataBytes = new byte[endPosition - CurrentHeaderPosition + 1];

            partialDataBytes = ReadChunkFromIsolatedStorage(CurrentHeaderPosition, endPosition - CurrentHeaderPosition + 1);

            if (partialDataBytes == null)
            {
                FileState = FileTransferState.FAILED;
                Delete();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                return;
            }

            var param = new Dictionary<string, string>();
            param.Add("Cookie", req.Headers["Cookie"]);
            param.Add("X-SESSION-ID", req.Headers["X-SESSION-ID"]);
            param.Add("X-CONTENT-RANGE", req.Headers["X-CONTENT-RANGE"]);
            var bytesToUpload = getMultiPartBytes(partialDataBytes, param);

            req.BeginGetRequestStream(UploadPostRequestCallback, new object[] { req, bytesToUpload });
        }

        byte[] getMultiPartBytes(byte[] data, Dictionary<string, string> param)
        {
            String boundaryMessage = getBoundaryMessage(param);
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

        String getBoundaryMessage(Dictionary<string, string> param)
        {
            String res = "--" + _boundary + "\r\n";

            var keys = param.Keys;

            foreach (var keyValue in param)
                res += "Content-Disposition: form-data; name=\"" + keyValue.Key + "\"\r\n" + "\r\n" + keyValue.Value + "\r\n" + "--" + _boundary + "\r\n";

            // keep ct empty since we are not sure about content type for images and video
            var ct = ContentType.Contains(HikeConstants.IMAGE) || ContentType.Contains(HikeConstants.VIDEO) ? "" : ContentType;

            res += "Content-Disposition: form-data; name=\"file\"; filename=\"" + FileName + "\"\r\n" + "Content-Type: " + ct + "\r\n\r\n";

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
            req.BeginGetResponse(UploadPostResponseCallback, new object[] { req });
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

            //        Logging.LogWriter.Instance.WriteToLog(netInterface.InterfaceType.ToString());
            //    }
            //    catch (NetworkException networkException)
            //    {
            //        if (networkException.NetworkErrorCode == NetworkError.WebRequestAlreadyFinished)
            //        {
            //            Logging.LogWriter.Instance.WriteToLog("Cannot call GetCurrentNetworkInterface if the webrequest is already complete");
            //        }
            //    }
            //}); 

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
                Logging.LogWriter.Instance.WriteToLog("FileUploader ::  UploadPostResponseCallback :  UploadPostResponseCallback , Exception : " + e.StackTrace);
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
                ProcessUploadPostResponse(data, responseCode);
            }
        }

        void ProcessUploadPostResponse(string data, HttpStatusCode code)
        {
            if (FileState == FileTransferState.CANCELED)
            {
                Delete();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                return;
            } 

            JObject jObject = null;

            if (code == HttpStatusCode.OK)
            {
                if (!String.IsNullOrEmpty(data))
                    jObject = JObject.Parse(data);

                if (jObject != null)
                {
                    SuccessObj = jObject;
                    CurrentHeaderPosition = TotalBytes;
                    Save();
                    
                    // if state is started then mark it as complete
                    // else update file state with respective state
                    if (FileState == FileTransferState.STARTED)
                        CheckIfComplete();   
                    else
                        OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                }
            }
            else if (code == HttpStatusCode.Created)
            {
                CurrentHeaderPosition += BlockSize;

                if (FileState == FileTransferState.STARTED)
                {
                    var newSize = (ChunkFactor + ChunkFactor) * DefaultBlockSize;

                    if (newSize <= MaxBlockSize)
                    {
                        ChunkFactor += ChunkFactor;
                        BlockSize = ChunkFactor * DefaultBlockSize;
                    }
                    else
                    {
                        ChunkFactor /= 2;
                        BlockSize = MaxBlockSize;
                    }
                }
                else
                {
                    ChunkFactor = 1;
                    BlockSize = DefaultBlockSize;
                }

                Save();

                // If state is started - dont update file state as its done before
                // Else update file state with new state
                if (FileState == FileTransferState.STARTED)
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, false));
                else
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));

                if (FileState == FileTransferState.STARTED || (!App.appSettings.Contains(App.AUTO_RESUME_SETTING) && FileState != FileTransferState.MANUAL_PAUSED))
                    BeginUploadPostRequest();
            }
            else if (code == HttpStatusCode.BadRequest) // server error during upload
            {
                FileState = FileTransferState.FAILED;
                Delete();
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
            }
            else
            {
                if (ShouldRetry())
                {
                    Start(null);
                }
                else // Bad Network and retry timed out
                {
                    FileState = FileTransferState.FAILED;
                    Save();
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                }
            }
        }

        byte[] ReadChunkFromIsolatedStorage(int position, int size)
        {
            string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn.Replace(":", "_") + "/" + MessageId;
            string fileDirectory = filePath.Substring(0, filePath.LastIndexOf("/"));
            byte[] bytes = null;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.DirectoryExists(fileDirectory))
                    return null;

                if (!myIsolatedStorage.FileExists(filePath))
                    return null;

                using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.Open, myIsolatedStorage))
                {
                    bytes = new byte[size];

                    fileStream.Seek(position, SeekOrigin.Begin);

                    using (BinaryReader reader = new BinaryReader(fileStream))
                    {
                        reader.Read(bytes, 0, size);
                    }
                }
            }

            return bytes;
        }

        protected override void OnStatusChanged(FileTransferSatatusChangedEventArgs e)
        {
            // Call the base class event invocation method. 
            base.OnStatusChanged(e);
        }
    }
}
