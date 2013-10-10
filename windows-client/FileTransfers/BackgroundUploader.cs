using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using windows_client.utils;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using windows_client.DbUtils;
using System.Text;
using System.Collections.Generic; 

namespace windows_client.FileTransfers
{
    public class BackgroundUploader
    {
        const string _boundary = "----------V2ymHFg03ehbqgZCaKO6jy";
        int _blockSize = 1024;
        
        public void OpenReadAsync(FileInfo fileInfo)
        {
            IsBusy = true;
            FileInfo = fileInfo;

            GetFileBytesUploaded();
        }

        private void GetFileBytesUploaded()
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
            
            AccountUtils.addToken(req);
            
            req.Method = "GET";
            
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = FileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = FileInfo.FileName;
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();

            req.BeginGetResponse(GetRequestCallback, new object[] { req });
        }

        public void GetRequestCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            var myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            string data = null;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
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
            }
            finally
            {
                UpdateStatusAfterGetResponse(data);
            }
        }

        private void UpdateStatusAfterGetResponse(string data)
        {
            int index = 0;
            if (!String.IsNullOrEmpty(data))
            {
                index = Convert.ToInt32(data);

                if (FileInfo.FileBytes.Length - 1 == index)
                    NotifyComplete(FileInfo);
                else
                {
                    FileInfo.BytesTransferred = index;
                    FileInfo.CurrentHeaderPosition = index + 1;
                    FileInfo.FileState = Attachment.AttachmentState.STARTED;

                    if (FileInfo.ConvMessage != null)
                    {
                        FileInfo.ConvMessage.ProgressBarValue = FileInfo.PercentageTransfer;
                        FileInfo.ConvMessage.FileAttachment.FileState = Attachment.AttachmentState.STARTED;
                    }

                    UploadFileBytes();
                }
            }
            else
            {
                // fresh upload
                FileInfo.CurrentHeaderPosition = FileInfo.BytesTransferred = index;
                FileInfo.FileState = Attachment.AttachmentState.STARTED;

                if (FileInfo.ConvMessage != null)
                {
                    FileInfo.ConvMessage.ProgressBarValue = FileInfo.PercentageTransfer;
                    FileInfo.ConvMessage.FileAttachment.FileState = Attachment.AttachmentState.STARTED;
                }

                UploadFileBytes();
            }
        }

        private void UploadFileBytes()
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.PARTIAL_FILE_TRANSFER_BASE_URL)) as HttpWebRequest;
           
            AccountUtils.addToken(req);

            req.Method = "POST";
            
            req.ContentType = string.Format("multipart/form-data; boundary={0}", _boundary);
            
            req.Headers["Connection"] = "Keep-Alive";
            req.Headers["Content-Name"] = FileInfo.FileName;
            req.Headers["X-Thumbnail-Required"] = "0";
            req.Headers["X-SESSION-ID"] = FileInfo.FileName;

            var endPosition = (FileInfo.CurrentHeaderPosition + _blockSize) < FileInfo.FileBytes.Length ? (FileInfo.CurrentHeaderPosition + _blockSize) : FileInfo.FileBytes.Length;
            endPosition -= 1;

            req.Headers["X-CONTENT-RANGE"] = string.Format("bytes {0}-{1}/{2}", FileInfo.CurrentHeaderPosition, endPosition, FileInfo.FileBytes.Length);

            var partialDataBytes = new byte[endPosition - FileInfo.CurrentHeaderPosition + 1];
            Array.Copy(FileInfo.FileBytes, FileInfo.CurrentHeaderPosition, partialDataBytes, 0, endPosition - FileInfo.CurrentHeaderPosition + 1);

            var param = new Dictionary<string, string>();
            param.Add("Cookie", req.Headers["Cookie"]);
            param.Add("X-SESSION-ID", req.Headers["X-SESSION-ID"]);
            param.Add("X-CONTENT-RANGE",req.Headers["X-CONTENT-RANGE"]);
            var bytesToUpload = getMultiPartBytes(partialDataBytes, param);

            req.BeginGetRequestStream(BeginUploadPostRequest, new object[] { req, bytesToUpload});
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

            res += "Content-Disposition: form-data; name=\"file\"; filename=\"" + FileInfo.FileName + "\"\r\n" + "Content-Type: " + FileInfo.ContentType + "\r\n\r\n";

            return res;
        }

        public void BeginUploadPostRequest(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;
            JObject data = new JObject();
            HttpWebRequest req = vars[0] as HttpWebRequest;
            Stream postStream = req.EndGetRequestStream(result);
            byte[] dataBytes = (byte[])vars[1];
            postStream.Write(dataBytes, 0, dataBytes.Length);
            postStream.Close();
            postStream.Close();
            req.BeginGetResponse(UploadResponseCallback, new object[] { req });
        }

        public void UploadResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            HttpWebRequest myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            string data = null;
            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
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
            }
            finally
            {
                UpdateStatusAfterPartialUpload(data);
            }
        }

        private void UpdateStatusAfterPartialUpload(string data)
        {
            JObject jObject = null;

            try
            {
                if (!String.IsNullOrEmpty(data))
                    jObject = JObject.Parse(data);
            }
            finally
            {
                if (data != null)
                {
                    if (jObject != null)
                    {
                        FileInfo.SuccessObj = jObject;
                        NotifyComplete(FileInfo);
                    }
                    else
                    {
                        FileInfo.BytesTransferred += _blockSize - 1;
                        FileInfo.CurrentHeaderPosition += _blockSize;

                        if (FileInfo.ConvMessage != null)
                        {
                            FileInfo.ConvMessage.ProgressBarValue = FileInfo.PercentageTransfer;
                        }

                        UploadFileBytes();
                    }
                }
                else
                {
                    FileInfo.SuccessObj = null;
                    NotifyComplete(FileInfo);
                }
            }
        }

        public void NotifyComplete(FileInfo fileInfo)
        {
            var convMessage = fileInfo.ConvMessage;

            if (fileInfo.SuccessObj != null && fileInfo.FileState != Attachment.AttachmentState.CANCELED
                && fileInfo.FileState != Attachment.AttachmentState.FAILED_OR_NOT_STARTED)
            {
                JObject data = fileInfo.SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                fileInfo.FileKey = data[HikeConstants.FILE_KEY].ToString();

                if (fileInfo.ContentType.Contains(HikeConstants.IMAGE))
                {
                    fileInfo.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileInfo.FileKey;
                }
                else if (fileInfo.ContentType.Contains(HikeConstants.AUDIO))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileInfo.FileKey;
                }
                else if (fileInfo.ContentType.Contains(HikeConstants.VIDEO))
                {
                    fileInfo.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileInfo.FileKey;
                }
                else if (fileInfo.ContentType.Contains(HikeConstants.CT_CONTACT))
                {
                    fileInfo.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.ContactTransfer_Text) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileInfo.FileKey;
                }

                if (convMessage != null)
                {
                    convMessage.ProgressBarValue = 100;
                    //DO NOT Update message text in db. We sent the below line, but we save content type as message.
                    //here message status should be updated in db, as on event listener message state should be unknown

                    convMessage.Message = fileInfo.Message;
                    convMessage.FileAttachment.FileKey = fileInfo.FileKey;
                    convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                    convMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                }

                FileInfo.FileState = Attachment.AttachmentState.COMPLETED;

                if (UploadComplete != null)
                    UploadComplete(this, new UploadCompletedArgs(FileInfo));
            }
            else
            {
                if (convMessage != null)
                {
                    convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                    convMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                    NetworkManager.updateDB(null, convMessage.MessageId, (int)ConvMessage.State.SENT_FAILED);
                }

                FileInfo.FileState = Attachment.AttachmentState.FAILED_OR_NOT_STARTED;

                if (UploadFailed != null)
                    UploadFailed(this, new UploadCompletedArgs(FileInfo));
            }
        }

        public FileInfo FileInfo { get; private set; }
        public Boolean IsBusy { get; private set; }

        public event EventHandler<UploadCompletedArgs> UploadComplete;
        public event EventHandler<EventArgs> UploadFailed;
    }

    public class UploadCompletedArgs : EventArgs
    {
        public UploadCompletedArgs(FileInfo userState)
        {
            UserState = userState;
        }

        public FileInfo UserState { get; private set; }
    }

}