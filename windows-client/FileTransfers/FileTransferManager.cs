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
        static int MaxQueueCount = 30;

        private static volatile FileTransferManager instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static object readWriteLock = new object();

        Queue<FileInfoBase> PendingTasks = new Queue<FileInfoBase>();

        /// <summary>
        /// only started and completed transfers will be present in TaskMap. 
        /// Once transfer is completed, the entry will be removed from DBUtils.
        /// </summary>
        public Dictionary<string, FileInfoBase> TaskMap = new Dictionary<string, FileInfoBase>();

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

        public void ChangeMaxUploadBuffer(NetworkInterfaceSubType type)
        {
            FileInfoBase.MaxBlockSize = (type == NetworkInterfaceSubType.Cellular_EDGE || type == NetworkInterfaceSubType.Cellular_3G) ? MobileBuffer : WifiBuffer;
        }

        public bool GetAttachmentStatus(string id, bool isSent, out FileInfoBase fileInfo)
        {
            fileInfo = null;

            if (!DoesTransferExist(id, isSent))
                return false;

            string fileName = null;

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
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

        bool DoesTransferExist(string id, bool isSent)
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

        public bool DownloadFile(string msisdn, string messageId, string fileName, string contentType, int size)
        {
            if (!DoesTransferExist(messageId, false))
            {
                FileDownloader fInfo = new FileDownloader(msisdn, messageId, fileName, contentType, size);
                PendingTasks.Enqueue(fInfo);
                StartTask();
            }
            else
                return ResumeTask(messageId, false);

            return true;
        }

        public bool UploadFile(string msisdn, string messageId, string fileName, string contentType, int size)
        {
            if (!DoesTransferExist(messageId, false))
            {
                FileUploader fInfo = new FileUploader(msisdn, messageId, fileName, contentType, size);
                if (!IsTransferPossible())
                {
                    FailTask(fInfo);
                    return false;
                }

                PendingTasks.Enqueue(fInfo);
                StartTask();
            }
            else
                return ResumeTask(messageId, false);

            return true;
        }

        public bool ResumeTask(string key, bool isSent)
        {
            if (!IsTransferPossible())
                return false;

            FileInfoBase fInfo = null;
         
            if (GetAttachmentStatus(key, isSent, out fInfo) && !TaskMap.ContainsKey(key))
            {
                // if file was cancelled, prevent resume
                if (fInfo.FileState == FileTransferState.CANCELED)
                    return false;

                if (fInfo.FileState != FileTransferState.COMPLETED)
                    fInfo.FileState = FileTransferState.STARTED;

                if (!PendingTasks.Contains(fInfo))
                    PendingTasks.Enqueue(fInfo);

                StartTask();

                return true;
            }

            return false;
        }

        public bool IsTransferPossible()
        {
            if (PendingTasks.Count >= MaxQueueCount)
                return false;

            return true;
        }

        public async void StartTask()
        {
            if (PendingTasks.Count > 0)
            {
                FileInfoBase fileInfo = PendingTasks.Dequeue();

                if (fileInfo.FileState == FileTransferState.CANCELED)
                {
                    // should not reach here as only ongoing tasks can be cancelled and not pending tasks
                    // but keeping a fail check as a preventive measure
                    TaskMap.Remove(fileInfo.MessageId);
                    fileInfo.Delete();
                    NotifyUI(new FileTransferSatatusChangedEventArgs(fileInfo, true));
                    fileInfo = null;
                    return;
                }
                else if (fileInfo.BytesTransfered == fileInfo.TotalBytes - 1 && fileInfo.FileState == FileTransferState.STARTED)
                {
                    fileInfo.StatusChanged -= File_StatusChanged;
                    fileInfo.StatusChanged += File_StatusChanged;
                    fileInfo.CheckIfComplete();
                }
                else if (fileInfo.FileState != FileTransferState.MANUAL_PAUSED && (!App.appSettings.Contains(App.AUTO_RESUME_SETTING) || fileInfo.FileState != FileTransferState.PAUSED))
                {
                    if (!TaskMap.ContainsKey(fileInfo.MessageId))
                    {
                        //If in progress add to map else queue it to pending task
                        if (BeginThreadTask(fileInfo))
                        {
                            fileInfo.FileState = FileTransferState.STARTED;
                            TaskMap.Add(fileInfo.MessageId, fileInfo);
                        }
                        else
                            PendingTasks.Enqueue(fileInfo);
                    }
                }
                else
                    TaskMap.Remove(fileInfo.MessageId); // stale state file. Remove from taskmap to prevent any possible error
            }

            if (PendingTasks.Count > 0)
                StartTask();
        }

        public bool IsBusy()
        {
            return TaskMap.Keys.Count > 0;
        }

        void FailTask(FileInfoBase fInfo)
        {
            fInfo.FileState = FileTransferState.FAILED;

            fInfo.Save();

            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, new FileTransferSatatusChangedEventArgs(fInfo, true));

            App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, fInfo);
        }

        Boolean BeginThreadTask(FileInfoBase fileInfo)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                FailTask(fileInfo);
                TaskMap.Remove(fileInfo.MessageId);
                return true;
            }
            else
            {
                fileInfo.StatusChanged -= File_StatusChanged;
                fileInfo.StatusChanged += File_StatusChanged;
                return ThreadPool.QueueUserWorkItem(fileInfo.Start);
            }
        }

        public void PopulatePreviousTasks()
        {
            if (!App.appSettings.Contains(App.AUTO_RESUME_SETTING))
            {
                PopulateUploads();
                PopulateDownloads();
             
                if (PendingTasks.Count > 0)
                    StartTask();
            }
        }

        void PopulateUploads()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
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

                                if (fileInfo.FileState != FileTransferState.MANUAL_PAUSED && fileInfo.FileState != FileTransferState.FAILED)
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
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
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

                                if (fileInfo.FileState != FileTransferState.MANUAL_PAUSED && fileInfo.FileState != FileTransferState.FAILED)
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

        public bool PauseTask(string id)
        {
            FileInfoBase fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                //prevent pause action for cancelled task
                if (fInfo.FileState == FileTransferState.CANCELED)
                    return false;

                fInfo.FileState = FileTransferState.MANUAL_PAUSED;
                TaskMap.Remove(id);
                return true;
            }
            else
                return false;
        }

        public bool CancelTask(string id)
        {
            FileInfoBase fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                TaskMap.Remove(id);

                // User can cancel only ongoing file transfers and not those which are paused.
                // Hence they will always be in TaskMap Dict
                // Only change the state. Data will be deleted and UI will be notified by their respective threads

                return true;
            }
            else
                return false;
        }

        public void DeleteTask(string id)
        {
            FileInfoBase fInfo;

            if (TaskMap.TryGetValue(id, out fInfo))
            {
                fInfo.FileState = FileTransferState.CANCELED;
                TaskMap.Remove(id);

                // User can delete an ongoing file transfers by deleting the message or the conversation list.
                // They will always be in TaskMap Dict
                // Only change the state. Data will be deleted by their respective threads
            }
            else
            {
                // If they are not in TaskMap Dict. But in Pending queue.
                var taskList = PendingTasks.Count > 0 ? PendingTasks.Where(t => t.MessageId == id) : null;
                if (taskList != null && taskList.Count() > 0)
                {
                    // Only change the state. Data will be deleted by their respective threads
                    fInfo = taskList.First();
                    fInfo.FileState = FileTransferState.CANCELED;
                }
                else
                {
                    // remove file from both upload and download directory if file was not transfering when it was deleted.
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        if (store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME))
                        {
                            if (store.FileExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + id))
                                store.DeleteFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + id);
                        }

                        if (store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME))
                        {
                            if (store.FileExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + id))
                                store.DeleteFile(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_UPLOAD_DIRECTORY_NAME + "\\" + id);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all file transfers. In case of unlink or delete
        /// </summary>
        public void ClearTasks()
        {
            //Cancel all files and delete them. Dont rely on other threads for deletion.

            foreach (var key in TaskMap.Keys)
            {
                TaskMap[key].FileState = FileTransferState.CANCELED;
                TaskMap[key].Delete();
            }

            TaskMap.Clear();

            foreach (var task in PendingTasks)
            {
                task.FileState = FileTransferState.CANCELED;
                task.Delete();
            }
            
            PendingTasks.Clear();
        }

        void File_StatusChanged(object sender, FileTransferSatatusChangedEventArgs e)
        {
            NotifyUI(e);
        }

        private void NotifyUI(FileTransferSatatusChangedEventArgs e)
        {
            if (UpdateTaskStatusOnUI != null)
                UpdateTaskStatusOnUI(null, e);

            if (e.IsStateChanged)
                App.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, e.FileInfo);

            if (e.FileInfo.FileState != FileTransferState.STARTED && e.FileInfo.FileState != FileTransferState.COMPLETED)
                TaskMap.Remove(e.FileInfo.MessageId);

            if (e.FileInfo.FileState == FileTransferState.PAUSED && !PendingTasks.Contains(e.FileInfo))
            {
                PendingTasks.Enqueue(e.FileInfo);
                StartTask();
            }
        }

        public event EventHandler<FileTransferSatatusChangedEventArgs> UpdateTaskStatusOnUI;
    }
}
