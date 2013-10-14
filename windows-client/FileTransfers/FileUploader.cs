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
using Microsoft.Phone.Net.NetworkInformation;

namespace windows_client.FileTransfers
{
    public class FileUploader
    {
        private void LoadReq()
        {
            if (Sources.Count > 0)
            {
                FileInfo fileInfo = Sources.Dequeue();

                if (fileInfo.FileState == Attachment.AttachmentState.CANCELED)
                {
                    UploadMap.Remove(fileInfo.Id);
                    DeleteUpload(fileInfo.Id);
                    fileInfo.ConvMessage = null;
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.FileState == Attachment.AttachmentState.COMPLETED)
                {
                    if (fileInfo.ConvMessage != null)
                        MarkedFileInfoAsUploaded(fileInfo);
                }
                else if (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING) || (fileInfo.FileState != Attachment.AttachmentState.PAUSED && fileInfo.FileState != Attachment.AttachmentState.MANUAL_PAUSED))
                {
                    if (!Download(fileInfo))
                        Sources.Enqueue(fileInfo);
                }
                else
                    Sources.Enqueue(fileInfo);
            }
        }

        private void MarkedFileInfoAsUploaded(FileInfo fileInfo)
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
            DeleteUploadBackUpOnComplete(fileInfo);

            fileInfo.ConvMessage = null;
            fileInfo = null;
        }

        BackgroundWorker _loadWorker;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile FileUploader instance = null;
        
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

        public void Load(ConvMessage convMessage, byte[] fileBytes = null)
        {
            var id = convMessage.Msisdn + "___" + convMessage.MessageId;
            FileInfo fInfo = null;

            if (UploadMap.TryGetValue(id, out fInfo))
            {
                fInfo.ConvMessage = convMessage;

                if (fInfo.FileState == Attachment.AttachmentState.COMPLETED)
                    MarkedFileInfoAsUploaded(fInfo);

                if (fInfo.FileState == Attachment.AttachmentState.PAUSED || fInfo.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
                {
                    fInfo.FileState = Attachment.AttachmentState.STARTED;
                    Sources.Enqueue(fInfo);
                    SaveUploadStatus(fInfo);
                    StartUpload();
                }
            }
            else
            {
                fInfo = new FileInfo(convMessage, fileBytes);

                if (fInfo.FileState != Attachment.AttachmentState.STARTED)
                {
                    Sources.Enqueue(fInfo);
                    UploadMap.Add(fInfo.Id, fInfo);
                    SaveUploadStatus(fInfo);
                    StartUpload();
                }
            }
        }

        private void StartUpload()
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

        private Boolean Download(FileInfo fileInfo)
        {
            if (UploadServices.Count >= 5 || !NetworkInterface.GetIsNetworkAvailable())
            {
                return false;
            }
            else
            {
                BackgroundUploader dClient = new BackgroundUploader();
                dClient.UploadComplete += dClient_UploadComplete;
                dClient.UploadFailed += dClient_UploadFailed;

                dClient.OpenReadAsync(fileInfo);
                UploadServices.Add(dClient);

                return true;
            }
        }

        void dClient_UploadFailed(object sender, EventArgs e)
        {
            RemoveFromUploaderService(sender as BackgroundUploader, false);
        }

        private void RemoveFromUploaderService(BackgroundUploader client, Boolean isUploadSuccess)
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

                    MiscDBUtil.saveAttachmentObject(fileInfo.ConvMessage.FileAttachment, fileInfo.ConvMessage.Msisdn, fileInfo.ConvMessage.MessageId);

                    UploadMap.Remove(fileInfo.Id);
                    DeleteUploadBackUpOnComplete(fileInfo);
                }

                fileInfo.ConvMessage = null;
                fileInfo = null;

                UploadServices.Remove(client);
            }
        }

        void dClient_UploadComplete(object sender, UploadCompletedArgs e)
        {
            var fileInfo = e.UserState as FileInfo;

            if (fileInfo.ConvMessage != null)
            {
                fileInfo.ConvMessage.ProgressBarValue = 100;
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, fileInfo.ConvMessage.serialize(true));
            }

            RemoveFromUploaderService(sender as BackgroundUploader, true);
        }

        Queue<FileInfo> Sources = new Queue<FileInfo>();
        Dictionary<string, FileInfo> UploadMap = new Dictionary<string, FileInfo>();

        private BackgroundUploaderService UploadServices = new BackgroundUploaderService();

        private const string UPLOAD_DIRECTORY_NAME = "FileUpload";
        private static object readWriteLock = new object();

        public void ResumeAllUploads()
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
                            FileInfo fileInfo = new FileInfo();
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

                    if (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING) && Sources.Count > 0)
                        StartUpload();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileUploader :: Save Uploads To File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void PauseUploadTask(string msisdn, long id)
        {
            var fileInfoId = msisdn + "___" + id;
            FileInfo fInfo;

            if (UploadMap.TryGetValue(fileInfoId, out fInfo))
            {
                fInfo.FileState = Attachment.AttachmentState.MANUAL_PAUSED;
                SaveUploadStatus(fInfo);
            }
        }

        public void CancelUploadTask(string msisdn, long id)
        {
            var fileInfoId = msisdn + "___" + id;
            FileInfo fInfo;

            if (UploadMap.TryGetValue(fileInfoId, out fInfo))
            {
                fInfo.FileState = Attachment.AttachmentState.CANCELED;
                SaveUploadStatus(fInfo);
            }
        }

        public void DeleteUploadTask(string msisdn, long id)
        {
            DeleteUploadTask(msisdn + "___" + id);
        }
        
        public void DeleteUploadTask(string id)
        {
            FileInfo fInfo;

            if (UploadMap.TryGetValue(id, out fInfo))
                fInfo.FileState = Attachment.AttachmentState.CANCELED;

            DeleteUpload(id);
        }

        public void DeleteUpload(string id)
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

        
        public void DeleteUploadBackUpOnComplete(FileInfo fileInfo)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = UPLOAD_DIRECTORY_NAME + "\\" + fileInfo.Id;
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

        public async void SaveUploadStatus(FileInfo fileInfo)
        {
            await Task.Delay(1000);

            lock (readWriteLock)
            {
                try
                {
                    string fileName = UPLOAD_DIRECTORY_NAME + "\\" + fileInfo.Id;
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
    }
}
