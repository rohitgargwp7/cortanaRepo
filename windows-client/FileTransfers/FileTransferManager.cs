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
        private const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        private const string FILE_TRANSFER_UPLOAD_DIRECTORY_NAME = "Upload";
        private const string FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME = "Download";
        const int WifiBuffer =  1048576;
        const int MobileBuffer =  102400;
        const int _noOfParallelRequest = 20;

        private static volatile FileTransferManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        Queue<HikeFileInfo> PendingTasks = new Queue<HikeFileInfo>();

        public Dictionary<string, HikeFileInfo> TaskMap = new Dictionary<string, HikeFileInfo>();

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
            ThreadPool.SetMaxThreads(_noOfParallelRequest, _noOfParallelRequest);
        }

        public void DownloadFile(string msisdn, string key, string fileName, string contentType)
        {
            FileDownloader fInfo = new FileDownloader(msisdn, key, fileName, contentType);

            PendingTasks.Enqueue(fInfo);
            TaskMap.Add(fInfo.Id, fInfo);
            SaveTaskData(fInfo);
            StartTask();
        }

        public void UploadFile(string msisdn, string key, string fileName, string contentType, byte[] fileBytes)
        {
            FileUploader fInfo = new FileUploader(msisdn, key, fileBytes, fileName, contentType);

            PendingTasks.Enqueue(fInfo);
            TaskMap.Add(fInfo.Id, fInfo);
            SaveTaskData(fInfo);
            StartTask();
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            FileUploader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
            FileDownloader.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public void ResumeTask(string key)
        {
            HikeFileInfo fInfo = null;
            if (TaskMap.TryGetValue(key, out fInfo))
            {
                fInfo.FileState = HikeFileState.STARTED;

                if (!PendingTasks.Contains(fInfo))
                    PendingTasks.Enqueue(fInfo);

                SaveTaskData(fInfo);
                StartTask();

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fInfo,true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
            }
        }

        public void StartTask()
        {
            if (PendingTasks.Count > 0)
            {
                HikeFileInfo fileInfo = PendingTasks.Dequeue();

                if (fileInfo.FileState == HikeFileState.CANCELED)
                {
                    TaskMap.Remove(fileInfo.Id);
                    DeleteTaskData(fileInfo.Id);
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1)
                {
                    fileInfo.FileState = HikeFileState.COMPLETED;
                    SaveTaskData(fileInfo);
                 
                    if (UpdateTaskStatusOnUI != null)
                        UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                    App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);
                }
                else if (fileInfo is FileUploader && fileInfo.FileState != HikeFileState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || fileInfo.FileState != HikeFileState.PAUSED))
                {
                    if (!BeginUploadDownload(fileInfo))
                        PendingTasks.Enqueue(fileInfo);
                }
                else if (fileInfo is FileDownloader && fileInfo.FileState != HikeFileState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING) || fileInfo.FileState != HikeFileState.PAUSED))
                {
                    if (!BeginUploadDownload(fileInfo))
                        PendingTasks.Enqueue(fileInfo);
                }
            }

            if (PendingTasks.Count > 0)
                StartTask();
        }

        private Boolean BeginUploadDownload(HikeFileInfo fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                fileInfo.FileState = HikeFileState.FAILED;
                SaveTaskData(fileInfo);

                if (UpdateTaskStatusOnUI != null)
                    UpdateTaskStatusOnUI(null, new TaskCompletedArgs(fileInfo, true));

                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fileInfo);

                return true;
            }
            else
            {
                if (fileInfo is FileDownloader)
                    return ThreadPool.QueueUserWorkItem((fileInfo as FileDownloader).BeginDownloadGetRequest, null);
                else
                    return ThreadPool.QueueUserWorkItem((fileInfo as FileUploader).BeginUploadGetRequest, null);
            }
        }

        void uploader_UploadStatusChanged(object sender, TaskCompletedArgs e)
        {
            SaveTaskData(e.FileInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, e);

            if (e.IsStateChanged)
                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, e.FileInfo);

            if (e.FileInfo.FileState == HikeFileState.PAUSED && !PendingTasks.Contains(e.FileInfo))
            {
                PendingTasks.Enqueue(e.FileInfo);

                StartTask();
            }
        }

        void downloader_DownloadStatusChanged(object sender, TaskCompletedArgs e)
        {
            SaveTaskData(e.FileInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, e);

            if (e.IsStateChanged)
                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, e.FileInfo);

            if (e.FileInfo.FileState == HikeFileState.PAUSED && !PendingTasks.Contains(e.FileInfo))
            {
                PendingTasks.Enqueue(e.FileInfo);

                StartTask();
            }
        }

        public void PopulatePreviousUploads()
        {
            PopulateUploads();
            PopulateDownloads();

            if ((!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || !App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING)) && PendingTasks.Count > 0)
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
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = HikeFileState.MANUAL_PAUSED;
                SaveTaskData(fInfo);
            }
        }

        public void CancelTask(string id)
        {
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = HikeFileState.CANCELED;
                SaveTaskData(fInfo);
            }
        }

        public void DeleteTask(string id)
        {
            HikeFileInfo fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
                fInfo.FileState = HikeFileState.CANCELED;

            DeleteTaskData(id);
        }

        public void DeleteTaskData(string id)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
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

        void SaveTaskData(HikeFileInfo fileInfo)
        {
            fileInfo.Save();
        }

        public event EventHandler<TaskCompletedArgs> UpdateTaskStatusOnUI;
    }

    public class TaskCompletedArgs : EventArgs
    {
        public TaskCompletedArgs(HikeFileInfo fileInfo, bool isStateChanged)
        {
            FileInfo = fileInfo;
            IsStateChanged = isStateChanged;
        }

        public HikeFileInfo FileInfo { get; private set; }
        public bool IsStateChanged { get; private set; }
    }
}
