using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.FileTransfers
{
    class FileDownloader
    {
        const int _defaultBlockSize = 1024;

        public event EventHandler<TaskCompletedArgs> DownloadStatusChanged;

        public void BeginDownloadGetRequest(object obj)
        {
            var fileInfo = obj as HikeFileInfo;

            var req = HttpWebRequest.Create(new Uri(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + fileInfo.FileName)) as HttpWebRequest;
            req.AllowReadStreamBuffering = false;
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
                Debug.WriteLine("FileDownloader ::  DownloadGetResponseCallback :  DownloadGetResponseCallback , Exception : " + e.StackTrace);
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

                    if (DownloadStatusChanged != null)
                        DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
                }

                ProcessDownloadGetResponse(responseStream, responseCode, fileInfo);
            }
        }

        void ProcessDownloadGetResponse(Stream responseStream, HttpStatusCode responseCode, HikeFileInfo fileInfo)
        {
            if (fileInfo.FileState == HikeFileState.CANCELED)
            {
                FileTransferManager.Instance.DeleteTaskData(fileInfo.Id);
            }
            else if (responseCode == HttpStatusCode.PartialContent || responseCode == HttpStatusCode.OK)
            {
                byte[] newBytes = null;
                using (BinaryReader br = new BinaryReader(responseStream))
                {
                    while (fileInfo.BytesTransfered != fileInfo.TotalBytes && fileInfo.FileState == HikeFileState.STARTED)
                    {
                        newBytes = br.ReadBytes((fileInfo as DownloadFileInfo).BlockSize);

                        if (newBytes.Length == 0)
                            break;

                        Array.Copy(newBytes, 0, fileInfo.FileBytes, fileInfo.CurrentHeaderPosition, newBytes.Length);
                        fileInfo.CurrentHeaderPosition += newBytes.Length;

                        var newSize = ((fileInfo as DownloadFileInfo).AttemptNumber + (fileInfo as DownloadFileInfo).AttemptNumber) * _defaultBlockSize;

                        if (newSize <= DownloadFileInfo.MaxBlockSize)
                        {
                            (fileInfo as DownloadFileInfo).AttemptNumber += (fileInfo as DownloadFileInfo).AttemptNumber;
                            (fileInfo as DownloadFileInfo).BlockSize = (fileInfo as DownloadFileInfo).AttemptNumber * _defaultBlockSize;
                        }

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, false));
                    }

                    if (fileInfo.FileState == HikeFileState.CANCELED)
                    {
                        FileTransferManager.Instance.DeleteTaskData(fileInfo.Id);
                    }
                    else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1)
                    {
                        fileInfo.FileState = HikeFileState.COMPLETED;

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
                    }
                    else if (newBytes.Length == 0)
                    {
                        fileInfo.FileState = HikeFileState.PAUSED;

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
                    }
                }
            }
            else if (responseCode == HttpStatusCode.BadRequest)
            {
                fileInfo.FileState = HikeFileState.FAILED;

                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                //});

                if (DownloadStatusChanged != null)
                    DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
            }
            else
            {
                fileInfo.FileState = HikeFileState.PAUSED;

                if (DownloadStatusChanged != null)
                    DownloadStatusChanged(this, new TaskCompletedArgs(fileInfo, true));
            }
        }
    }
}
