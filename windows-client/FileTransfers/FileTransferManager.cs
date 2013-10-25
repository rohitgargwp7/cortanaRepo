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
            DownloadFileInfo.MaxBlockSize = WifiBuffer;
            UploadFileInfo.MaxBlockSize = WifiBuffer;
            ThreadPool.SetMaxThreads(_noOfParallelRequest, _noOfParallelRequest);
        }

        public void DownloadFile(string msisdn, string key, string fileName, string contentType)
        {
            DownloadFileInfo fInfo = new DownloadFileInfo(msisdn, key, fileName, contentType);

            PendingTasks.Enqueue(fInfo);
            TaskMap.Add(fInfo.Id, fInfo);
            SaveTaskData(fInfo);
            StartTask();
        }

        public void UploadFile(string msisdn, string key, string fileName, string contentType, byte[] fileBytes)
        {
            UploadFileInfo fInfo = new UploadFileInfo(msisdn, key, fileBytes, fileName, contentType);

            PendingTasks.Enqueue(fInfo);
            TaskMap.Add(fInfo.Id, fInfo);
            SaveTaskData(fInfo);
            StartTask();
        }

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            UploadFileInfo.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
            DownloadFileInfo.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
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
                else if (fileInfo.FileState != HikeFileState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) || fileInfo.FileState != HikeFileState.PAUSED))
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
                if (fileInfo is DownloadFileInfo)
                {
                    FileDownloader downloader = new FileDownloader();
                    downloader.DownloadStatusChanged += downloader_DownloadStatusChanged;
                    return ThreadPool.QueueUserWorkItem(downloader.BeginDownloadGetRequest, fileInfo);
                }
                else
                {
                    FileUploader uploader = new FileUploader();
                    uploader.UploadStatusChanged += uploader_UploadStatusChanged;
                    return ThreadPool.QueueUserWorkItem(uploader.BeginUploadGetRequest, fileInfo);
                }
            }
        }

        void uploader_UploadStatusChanged(object sender, TaskCompletedArgs e)
        {
            SaveTaskData(e.FileInfo);

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, new TaskCompletedArgs(e.FileInfo, e.IsStateChanged));

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
                UpdateTaskStatusOnUI(null, new TaskCompletedArgs(e.FileInfo, e.IsStateChanged));

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
            
            if (!App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && PendingTasks.Count > 0)
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
                            UploadFileInfo fileInfo = new UploadFileInfo();
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
                            DownloadFileInfo fileInfo = new DownloadFileInfo();

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
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + fileInfo.Id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME);

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
