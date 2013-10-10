using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using System.ComponentModel;
using System.Linq;
using windows_client.DbUtils;
using System.IO.IsolatedStorage;
using System.IO;

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
                    if (fileInfo.ConvMessage != null)
                        MarkedFileInfoAsUploaded(fileInfo);
                }
                else
                {
                    if (!Download(fileInfo))
                        Sources.Enqueue(fileInfo);
                }
            }
        }

        private static void MarkedFileInfoAsUploaded(FileInfo fileInfo)
        {
            fileInfo.ConvMessage.Message = fileInfo.Message;
            fileInfo.ConvMessage.FileAttachment.FileKey = fileInfo.FileKey; 
            
            fileInfo.ConvMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
            fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
            MiscDBUtil.saveAttachmentObject(fileInfo.ConvMessage.FileAttachment, fileInfo.ConvMessage.Msisdn, fileInfo.ConvMessage.MessageId);

            fileInfo.ConvMessage.ProgressBarValue = 100;

            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
            fileInfo.ConvMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);

            UploadMap.Remove(fileInfo.Id);
            SaveAllUploads();

            fileInfo.ConvMessage = null;
            fileInfo = null;
        }

        static BackgroundWorker _loadWorker;

        public static void Load(ConvMessage convMessage, byte[] fileBytes = null)
        {
            var id = convMessage.Msisdn + convMessage.MessageId;
            FileInfo fInfo = null;

            if (UploadMap.TryGetValue(id, out fInfo))
            {
                fInfo.ConvMessage = convMessage;

                if (fInfo.FileState == Attachment.AttachmentState.COMPLETED)
                    MarkedFileInfoAsUploaded(fInfo);
            }
            else
            {
                fInfo = new FileInfo(convMessage, fileBytes);

                if (fInfo.FileState != Attachment.AttachmentState.STARTED)
                {
                    Sources.Enqueue(fInfo);
                    UploadMap.Add(fInfo.Id, fInfo);
                    SaveAllUploads();
                    StartUpload();
                }
            }
        }

        private static void StartUpload()
        {
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

                UploadMap.Remove(fileInfo.Id);
                SaveAllUploads();

                fileInfo.ConvMessage = null;
                fileInfo = null;

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
        static Dictionary<string, FileInfo> UploadMap = new Dictionary<string, FileInfo>();

        private static BackgroundUploaderService UploadServices = new BackgroundUploaderService();

        private static string UPLOAD_DIRECTORY_NAME = "FileUpload";
        private static object readWriteLock = new object();

        public static async void ResumeAllUploads()
        {
            await Task.Delay(500);

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
                            FileInfo fileInfo = new FileInfo();
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
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

                    if (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING) && Sources.Count > 0)
                        StartUpload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Save Uploads To File, Exception : " + ex.StackTrace);
                }
            }
        }

        public static async void SaveAllUploads()
        {
            await Task.Delay(100);

            lock (readWriteLock)
            {
                try
                {
                    string fileName = "";
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(UPLOAD_DIRECTORY_NAME))
                            store.CreateDirectory(UPLOAD_DIRECTORY_NAME);

                        foreach (var keyValue in UploadMap)
                        {
                            fileName = UPLOAD_DIRECTORY_NAME + "\\" + keyValue.Key;
                            
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);

                            using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.Write))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    writer.Seek(0, SeekOrigin.Begin);
                                    keyValue.Value.Write(writer);
                                    writer.Flush();
                                    writer.Close();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Save Uploads To File, Exception : " + ex.StackTrace);
                }
            }

        }
    }
}
