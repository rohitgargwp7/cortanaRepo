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
        const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        const string FILE_TRANSFER_UPLOAD_DIRECTORY_NAME = "Upload";
        const string FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME = "Download";
        const int WifiBuffer =  1048576;
        const int MobileBuffer =  102400;
        const int NoOfParallelRequest = 20;
        public static int MaxQueueCount = 10;

        private static volatile FileTransferManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        public Queue<IFileInfo> PendingTasks = new Queue<IFileInfo>();

        /// <summary>
        /// only started and completed transfers will be present in TaskMap. 
        /// Once transfer is completed, the entry will be removed from DBUtils.
        /// </summary>
        public Dictionary<string, IFileInfo> TaskMap = new Dictionary<string, IFileInfo>();

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
            FileDownloader.MaxBlockSize = WifiBuffer;
            FileUploader.MaxBlockSize = WifiBuffer;
            ThreadPool.SetMaxThreads(NoOfParallelRequest, NoOfParallelRequest);
        }

        public bool GetAttachmentStatus(string id, bool isSent, out IFileInfo fileInfo)
        {
            fileInfo = null;

            if (!IsTransferOld(id, isSent))
                return false;

            string fileName = null;

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                    return false;

                if (isSent)
                {
                    if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                        return false;

                    fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + id;

                    if (!store.FileExists(fileName))
                        return false;

                    fileInfo = new FileUploader();
                }
                else
                {
                    if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME))
                        return false;

                    fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + id;

                    if (!store.FileExists(fileName))
                        return false;

                    fileInfo = new FileDownloader();
                }

                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BinaryReader reader = new BinaryReader(file))
                    {
                        fileInfo.Read(reader);
                        reader.Close();
                    }
                }
            }

            return true;
        }

        public bool GetAttachmentStatus(string id, out IFileInfo fileInfo)
        {
            fileInfo = null;

            string fileName = null;

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                    return false;

                if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                    return false;

                fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + id;

                if (store.FileExists(fileName))
                    fileInfo = new FileUploader();
                else
                {
                    fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + id;

                    if (store.FileExists(fileName))
                        fileInfo = new FileDownloader();
                    else
                        return false;
                }

                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (BinaryReader reader = new BinaryReader(file))
                    {
                        fileInfo.Read(reader);
                        reader.Close();
                    }
                }
            }

            return true;
        }

        bool IsTransferOld(string id, bool isSent)
        {
            string fileName = isSent ?
                FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + id :
                FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + id;

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(fileName))
                    return true;
            }

            return false;
        }

        public bool DownloadFile(string msisdn, string messageId, string fileName, string contentType)
        {
            if (PendingTasks.Count >= MaxQueueCount)
                return false;

            var isResumed = ResumeTask(messageId, false);

            if (!isResumed)
            {
                FileDownloader fInfo = new FileDownloader(msisdn, messageId, fileName, contentType);

                PendingTasks.Enqueue(fInfo);
                SaveTaskData(fInfo);

                StartTask();
            }

            return true;
        }

        public bool UploadFile(string msisdn, string messageId, string fileName, string contentType, int size)
        {
            var isResumed = ResumeTask(messageId, true);

            if (!isResumed)
            {
                FileUploader fInfo = new FileUploader(msisdn, messageId, size, fileName, contentType);

                SaveTaskData(fInfo);

                if (PendingTasks.Count >= MaxQueueCount)
                {
                    FailTask(fInfo);
                    return false;
                }

                PendingTasks.Enqueue(fInfo);
                StartTask();
            }

            return true;
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            FileUploader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
            FileDownloader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public bool ResumeTask(string key, bool isSent)
        {
            IFileInfo fInfo = null;
         
            if (GetAttachmentStatus(key, isSent, out fInfo) && !TaskMap.ContainsKey(key))
            {
                if (fInfo.FileState != FileTransferState.COMPLETED)
                    fInfo.FileState = FileTransferState.STARTED;

                if (!PendingTasks.Contains(fInfo))
                    PendingTasks.Enqueue(fInfo);

                SaveTaskData(fInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);

                StartTask();

                return true;
            }

            return false;
        }

        public void StartTask()
        {
            if (PendingTasks.Count > 0)
            {
                IFileInfo fileInfo = PendingTasks.Dequeue();

                if (fileInfo.FileState == FileTransferState.CANCELED)
                {
                    TaskMap.Remove(fileInfo.MessageId);
                    fileInfo.Delete();
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1 && fileInfo.FileState == FileTransferState.STARTED)
                {
                    fileInfo.FileState = FileTransferState.COMPLETED;
                    SaveTaskData(fileInfo);

                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fileInfo, true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else
                {
                    var shouldTransfer = fileInfo is FileUploader ? !App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) : !App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING);

                    if (fileInfo.FileState != FileTransferState.MANUAL_PAUSED && (shouldTransfer || fileInfo.FileState != FileTransferState.PAUSED))
                    {
                        if (BeginThreadTask(fileInfo))
                        {
                            fileInfo.FileState = FileTransferState.STARTED;
                            TaskMap.Add(fileInfo.MessageId, fileInfo);
                            SaveTaskData(fileInfo);

                            if (UpdateTaskStatusOnUI != null)
                                UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fileInfo, true));

                            App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                        }
                        else
                            PendingTasks.Enqueue(fileInfo);
                    }
                    else if (fileInfo.FileState != FileTransferState.STARTED)
                        TaskMap.Remove(fileInfo.MessageId);
                }
            }

            if (PendingTasks.Count > 0)
                StartTask();
        }

        public bool IsBusy()
        {
            return TaskMap.Keys.Count > 0;
        }

        void FailTask(IFileInfo fInfo)
        {
            fInfo.FileState = FileTransferState.FAILED;
            
            SaveTaskData(fInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fInfo, true));

            App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
        }

        Boolean BeginThreadTask(IFileInfo fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                fileInfo.FileState = FileTransferState.FAILED;
                SaveTaskData(fileInfo);
                TaskMap.Remove(fileInfo.MessageId);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);

                return true;
            }
            else
            {
                fileInfo.StatusChanged -= File_StatusChanged;
                fileInfo.StatusChanged += File_StatusChanged;
                return ThreadPool.QueueUserWorkItem(fileInfo.Start, null);
            }
        }

        void File_StatusChanged(object sender, FileTransferSatatusChangedEventArgs e)
        {
            SaveTaskData(e.FileInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, e);

            if (e.IsStateChanged)
                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, e.FileInfo);

            if (e.FileInfo.FileState == FileTransferState.PAUSED && !PendingTasks.Contains(e.FileInfo))
            {
                PendingTasks.Enqueue(e.FileInfo);

                StartTask();
            }
        }

        public void PopulatePreviousTasks()
        {
            if (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING))
                PopulateUploads();

            if (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING))
                PopulateDownloads();

            if (PendingTasks.Count > 0)
                StartTask();
        }

        void PopulateUploads()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                            return;

                        var fileNames = store.GetFileNames(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\*");

                        foreach (var fileName in fileNames)
                        {
                            if (!TaskMap.ContainsKey(fileName))
                            {
                                FileUploader fileInfo = new FileUploader();
                                using (var file = store.OpenFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    using (BinaryReader reader = new BinaryReader(file))
                                    {
                                        fileInfo.Read(reader);
                                        reader.Close();
                                    }
                                }

                                PendingTasks.Enqueue(fileInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileTransferManager :: Load Uploads From File, Exception : " + ex.StackTrace);
                }
            }
        }

        void PopulateDownloads()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME))
                            return;

                        var fileNames = store.GetFileNames(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\*");

                        foreach (var fileName in fileNames)
                        {
                            if (!TaskMap.ContainsKey(fileName))
                            {
                                FileDownloader fileInfo = new FileDownloader();

                                using (var file = store.OpenFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    using (BinaryReader reader = new BinaryReader(file))
                                    {
                                        fileInfo.Read(reader);
                                        reader.Close();
                                    }
                                }

                                PendingTasks.Enqueue(fileInfo);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileTransferManager :: Load Downloads From File, Exception : " + ex.StackTrace);
                }
            }
        }

        public void PauseTask(string id)
        {
            IFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.MANUAL_PAUSED;
                SaveTaskData(fInfo);
                TaskMap.Remove(id);
            }
        }

        public void CancelTask(string id)
        {
            IFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                fInfo.Delete();
                TaskMap.Remove(id);
            }
        }

        public void DeleteTask(string id)
        {
            IFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                fInfo.Delete();
                TaskMap.Remove(id);
            }
        }

        void SaveTaskData(IFileInfo fileInfo)
        {
            fileInfo.Save();
        }

        public event EventHandler<FileTransferSatatusChangedEventArgs> UpdateTaskStatusOnUI;
    }
}
