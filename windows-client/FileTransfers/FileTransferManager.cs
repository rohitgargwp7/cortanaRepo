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
        public static int MaxQueueCount = 4;

        private static volatile FileTransferManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        public Queue<IFileInfo> PendingTasks = new Queue<IFileInfo>();

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

        public bool DownloadFile(string msisdn, string key, string fileName, string contentType)
        {
            if (PendingTasks.Count >= MaxQueueCount)
                return false;

            if (TaskMap.ContainsKey(key))
                ResumeTask(key);
            else
            {
                FileDownloader fInfo = new FileDownloader(msisdn, key, fileName, contentType);

                PendingTasks.Enqueue(fInfo);
                TaskMap.Add(fInfo.Id, fInfo);
                SaveTaskData(fInfo);

                StartTask();
            }

            return true;
        }

        public bool UploadFile(string msisdn, string key, string fileName, string contentType, int size)
        {
            if (TaskMap.ContainsKey(key))
            {
                ResumeTask(key);
                return true;
            }
            else
            {
                FileUploader fInfo = new FileUploader(msisdn, key, size, fileName, contentType);

                PendingTasks.Enqueue(fInfo);
                TaskMap.Add(fInfo.Id, fInfo);

                if (PendingTasks.Count >= MaxQueueCount)
                {
                    FailTask(fInfo);
                    return false;
                }

                SaveTaskData(fInfo);
                StartTask();
                return true;
            }
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            FileUploader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
            FileDownloader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public void ResumeTask(string key)
        {
            IFileInfo fInfo = null;
            if (TaskMap.TryGetValue(key, out fInfo))
            {
                fInfo.FileState = FileTransferState.STARTED;

                if (!PendingTasks.Contains(fInfo))
                    PendingTasks.Enqueue(fInfo);

                SaveTaskData(fInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);

                StartTask();
            }
        }

        public void StartTask()
        {
            if (PendingTasks.Count > 0)
            {
                IFileInfo fileInfo = PendingTasks.Dequeue();

                if (fileInfo.FileState == FileTransferState.CANCELED)
                {
                    TaskMap.Remove(fileInfo.Id);
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
                else if (fileInfo is FileUploader && fileInfo.FileState != FileTransferState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || fileInfo.FileState != FileTransferState.PAUSED))
                {
                    if (!BeginThreadTask(fileInfo))
                        PendingTasks.Enqueue(fileInfo);
                }
                else if (fileInfo is FileDownloader && fileInfo.FileState != FileTransferState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING) || fileInfo.FileState != FileTransferState.PAUSED))
                {
                    if (!BeginThreadTask(fileInfo))
                        PendingTasks.Enqueue(fileInfo);
                }
            }

            if (PendingTasks.Count > 0)
                StartTask();
        }

        void FailTask(IFileInfo fInfo)
        {
            fInfo.FileState = FileTransferState.FAILED;
            
            SaveTaskData(fInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fInfo, true));

            App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
        }

        private Boolean BeginThreadTask(IFileInfo fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                fileInfo.FileState = FileTransferState.FAILED;
                SaveTaskData(fileInfo);

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

        public void PopulatePreviousUploads()
        {
            PopulateUploads();
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
                            FileUploader fileInfo = new FileUploader();
                            using (var file = store.OpenFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                            FileDownloader fileInfo = new FileDownloader();

                            using (var file = store.OpenFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
            }
        }

        public void CancelTask(string id)
        {
            IFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                SaveTaskData(fInfo);
            }
        }

        public void DeleteTask(string id)
        {
            IFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                fInfo.Delete();
            }
        }

        void SaveTaskData(IFileInfo fileInfo)
        {
            fileInfo.Save();
        }

        public event EventHandler<FileTransferSatatusChangedEventArgs> UpdateTaskStatusOnUI;
    }
}
