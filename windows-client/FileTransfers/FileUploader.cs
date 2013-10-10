using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using System.ComponentModel;
using System.Linq;
using windows_client.DbUtils;

namespace windows_client.FileTransfers
{
    public static class FileUploader
    {
        private static void LoadReq()
        {
            if (Sources.Count > 0)
            {
                FileInfo fileInfo = Sources.Dequeue();

                if (fileInfo.FileState == Attachment.AttachmentState.COMPLETED)
                {
                    if (fileInfo.ConvMessage == null)
                    {
                        Sources.Enqueue(fileInfo);
                    }
                    else
                    {
                        fileInfo.ConvMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                        fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                        MiscDBUtil.saveAttachmentObject(fileInfo.ConvMessage.FileAttachment, fileInfo.ConvMessage.Msisdn, fileInfo.ConvMessage.MessageId);

                        fileInfo.ConvMessage.ProgressBarValue = 100;

                        App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
                        fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED); 
                        
                        fileInfo.ConvMessage = null;
                        fileInfo = null;
                    }
                }
                else
                {
                    if (!Download(fileInfo))
                        Sources.Enqueue(fileInfo);
                }
            }
        }

        static BackgroundWorker _loadWorker;

        public static void Load(ConvMessage convMessage, byte[] fileBytes = null)
        {
            convMessage.FileAttachment.FileState = Attachment.AttachmentState.FAILED_OR_NOT_STARTED;

            FileInfo fileInfo = new FileInfo(convMessage, fileBytes);

            var list = SourceList.Where(s => s.Id == fileInfo.Id);
            if (list.Count() > 0)
            {
                var fInfo = list.First() as FileInfo;
                fInfo.ConvMessage = fileInfo.ConvMessage;
                fileInfo.FileState = fInfo.FileState;

                if (fInfo.FileState == Attachment.AttachmentState.COMPLETED)
                {
                    fileInfo.ConvMessage.Message = fileInfo.Message;
                    fileInfo.ConvMessage.FileAttachment.FileKey = fileInfo.FileKey;

                    fileInfo.ConvMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                    fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                    MiscDBUtil.saveAttachmentObject(fileInfo.ConvMessage.FileAttachment, fileInfo.ConvMessage.Msisdn, fileInfo.ConvMessage.MessageId);

                    fileInfo.ConvMessage.ProgressBarValue = 100;

                    App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
                    fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);

                    SourceList.Remove(fInfo);
                    fInfo.ConvMessage = null;
                    fInfo = null;

                    fileInfo.ConvMessage = null;
                    fileInfo = null;
                    return;
                }
            }


            Sources.Enqueue(fileInfo);
            SourceList.Add(fileInfo);
            if (_loadWorker == null)
            {
                _loadWorker = new BackgroundWorker();
                _loadWorker.DoWork += delegate
                {
                    LoadReq();
                };

                _loadWorker.RunWorkerCompleted += delegate
                {
                    if (Sources.Count > 0)
                    {
                        if (!_loadWorker.IsBusy)
                            _loadWorker.RunWorkerAsync();
                    }
                    else
                        _loadWorker = null;
                };

                if (!_loadWorker.IsBusy)
                    _loadWorker.RunWorkerAsync();
            }
        }

        private static Boolean Download(FileInfo imgInfo)
        {
            if (UploadServices.Count >= 5)
            {
                return false;
            }
            else
            {
                BackgroundUploader dClient = new BackgroundUploader();
                dClient.UploadComplete += dClient_UploadComplete;
                dClient.UploadFailed += dClient_UploadFailed;

                dClient.OpenReadAsync(imgInfo);
                UploadServices.Add(dClient);

                return true;
            }
        }

        static void dClient_UploadFailed(object sender, EventArgs e)
        {
            RemoveFromUploaderService(sender as BackgroundUploader, false);
        }

        private static void RemoveFromUploaderService(BackgroundUploader client, Boolean isUploadSuccess)
        {
            if (client != null)
            {
                client.UploadFailed -= dClient_UploadFailed;
                client.UploadComplete -= dClient_UploadComplete;

                FileInfo fileInfo = client.FileInfo as FileInfo;
                if (fileInfo.ConvMessage != null)
                {
                    if (isUploadSuccess)
                        fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                    else
                        fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                }

                fileInfo.ConvMessage = null;
                fileInfo = null;

                SourceList.Remove(fileInfo);

                UploadServices.Remove(client);
            }
        }

        static void dClient_UploadComplete(object sender, UploadCompletedArgs e)
        {
            var fileInfo = e.UserState as FileInfo;
            
            if (fileInfo.ConvMessage != null)
                fileInfo.ConvMessage.ProgressBarValue = 100;

            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
            RemoveFromUploaderService(sender as BackgroundUploader, true);
        }

        static Queue<FileInfo> Sources = new Queue<FileInfo>();
        static List<FileInfo> SourceList = new List<FileInfo>();

        private static BackgroundUploaderService UploadServices = new BackgroundUploaderService();
    }
}
