using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using windows_client.utils;

namespace windows_client.FileTransfers
{
    class FileUploader
    {
        string _boundary = "----------V2ymHFg03ehbqgZCaKO6jy";
        const int _defaultBlockSize = 1024;

        public event EventHandler<TaskCompletedArgs> UploadStatusChanged;

        public void BeginUploadGetRequest(object obj)
        {
            var fileInfo = obj as HikeFileInfo;

            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;

            AccountUtils.addToken(req);

            req.Method = "GET";

            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = fileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = fileInfo.Id;
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
                Debug.WriteLine("FileUploader ::  UploadGetResponseCallback :  UploadGetResponseCallback , Exception : " + e.StackTrace);
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

                    if (UploadStatusChanged != null)
                        UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
                }
                else
                {
                    fileInfo.CurrentHeaderPosition = index + 1;
                    fileInfo.FileState = HikeFileState.STARTED;

                    if (UploadStatusChanged != null)
                        UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));

                    BeginUploadPostRequest(fileInfo);
                }
            }
            else if (responseCode == HttpStatusCode.NotFound)
            {
                // fresh upload
                fileInfo.CurrentHeaderPosition = index;
                fileInfo.FileState = HikeFileState.STARTED;

                if (UploadStatusChanged != null)
                    UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));

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
            req.Headers["X-SESSION-ID"] = fileInfo.Id;

            var bytesLeft = fileInfo.FileBytes.Length - fileInfo.CurrentHeaderPosition;
            (fileInfo as UploadFileInfo).BlockSize = bytesLeft >= (fileInfo as UploadFileInfo).BlockSize ? (fileInfo as UploadFileInfo).BlockSize : bytesLeft;

            var endPosition = fileInfo.CurrentHeaderPosition + (fileInfo as UploadFileInfo).BlockSize;
            endPosition -= 1;

            req.Headers["X-CONTENT-RANGE"] = string.Format("bytes {0}-{1}/{2}", fileInfo.CurrentHeaderPosition, endPosition, fileInfo.FileBytes.Length);

            var partialDataBytes = new byte[endPosition - fileInfo.CurrentHeaderPosition + 1];
            Array.Copy(fileInfo.FileBytes, fileInfo.CurrentHeaderPosition, partialDataBytes, 0, endPosition - fileInfo.CurrentHeaderPosition + 1);

            var param = new Dictionary<string, string>();
            param.Add("Cookie", req.Headers["Cookie"]);
            param.Add("X-SESSION-ID", req.Headers["X-SESSION-ID"]);
            param.Add("X-CONTENT-RANGE", req.Headers["X-CONTENT-RANGE"]);
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
                Debug.WriteLine("FileUploader ::  UploadPostResponseCallback :  UploadPostResponseCallback , Exception : " + e.StackTrace);
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
                    (fileInfo as UploadFileInfo).SuccessObj = jObject;
                    fileInfo.CurrentHeaderPosition = fileInfo.TotalBytes;

                    var stateUpdated = false;

                    if (fileInfo.FileState == HikeFileState.STARTED)
                    {
                        fileInfo.FileState = HikeFileState.COMPLETED;
                        stateUpdated = true;
                    }

                    if (UploadStatusChanged != null)
                        UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, stateUpdated));
                }
            }
            else if (code == HttpStatusCode.Created)
            {
                if (fileInfo.FileState == HikeFileState.CANCELED)
                {
                    FileTransferManager.Instance.DeleteTaskData(fileInfo.Id);
                }
                else
                {
                    fileInfo.CurrentHeaderPosition += (fileInfo as UploadFileInfo).BlockSize;

                    if (fileInfo.FileState == HikeFileState.STARTED)
                    {
                        var newSize = ((fileInfo as UploadFileInfo).AttemptNumber + (fileInfo as UploadFileInfo).AttemptNumber) * _defaultBlockSize;

                        if (newSize <= UploadFileInfo.MaxBlockSize)
                        {
                            (fileInfo as UploadFileInfo).AttemptNumber += (fileInfo as UploadFileInfo).AttemptNumber;
                            (fileInfo as UploadFileInfo).BlockSize = (fileInfo as UploadFileInfo).AttemptNumber * _defaultBlockSize;
                        }
                    }
                    else
                    {
                        (fileInfo as UploadFileInfo).AttemptNumber = 1;
                        (fileInfo as UploadFileInfo).BlockSize = _defaultBlockSize;
                    }

                    if (fileInfo.FileState == HikeFileState.STARTED || (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && fileInfo.FileState != HikeFileState.MANUAL_PAUSED))
                        BeginUploadPostRequest(fileInfo);

                    if (UploadStatusChanged != null)
                        UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, false));
                }
            }
            else if (code == HttpStatusCode.BadRequest)
            {
                fileInfo.FileState = HikeFileState.FAILED;

                if (UploadStatusChanged != null)
                    UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
            }
            else
            {
                //app suspension and disconnected case
                fileInfo.FileState = HikeFileState.PAUSED;

                if (UploadStatusChanged != null)
                    UploadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
            }
        }
    }
}
