using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using System.ComponentModel;

namespace windows_client.FileTransfers
{
    public static class FileUploader
    {
        private static void LoadReq()
        {
            if (Sources.Count > 0)
            {
                FileInfo fileInfo = Sources.Dequeue();

                if (!Download(fileInfo))
                    Sources.Enqueue(fileInfo);
            }
        }

        static BackgroundWorker _loadWorker;

        public static void Load(ConvMessage convMessage, byte[] fileBytes = null)
        {
            convMessage.FileAttachment.FileState = Attachment.AttachmentState.FAILED_OR_NOT_STARTED;

            FileInfo fileInfo = new FileInfo(convMessage, fileBytes);
            Sources.Enqueue(fileInfo);

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
                if (isUploadSuccess)
                    fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                else
                    fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);

                UploadServices.Remove(client);
            }
        }

        static void dClient_UploadComplete(object sender, UploadCompletedArgs e)
        {
            var fileInfo = e.UserState as FileInfo;
            fileInfo.ConvMessage.ProgressBarValue = 100;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
            RemoveFromUploaderService(sender as BackgroundUploader, true);
        }

        static Queue<FileInfo> Sources = new Queue<FileInfo>();

        private static BackgroundUploaderService UploadServices = new BackgroundUploaderService();
    }
}
