﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;
using windows_client.DbUtils;
using Newtonsoft.Json.Linq;
using System.IO;
using windows_client.Misc;
using windows_client.utils;
using System.IO.IsolatedStorage;
using System.Net;
using System.Diagnostics;

namespace windows_client.FileTransfers
{
    public class FileDownloader : IFileInfo
    {
        const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        const string FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME = "Download";
        const int DefaultBlockSize = 1024;

        static object readWriteLock = new object();
       
        public static int MaxBlockSize;

        int BlockSize = 1024;
        int AttemptFactor = 1;
        
        public int BytesTransfered
        {
            get
            {
                return CurrentHeaderPosition == 0 ? 0 : CurrentHeaderPosition - 1;
            }
        }
        public double PercentageTransfer
        {
            get
            {
                return TotalBytes == 0 ? 0 : ((double)BytesTransfered / TotalBytes) * 100;
            }
        }
        public int TotalBytes { get; set; }
        public string Id { get; set; }
        public int CurrentHeaderPosition { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string Msisdn { get; set; }
        public FileTransferState FileState { get; set; }

        public FileDownloader()
        {
        }

        public FileDownloader(string msisdn, string key, string fileName, string contentType)
        {
            Msisdn = msisdn;
            Id = key;
            FileName = fileName;
            ContentType = contentType;
            FileState = FileTransferState.NOT_STARTED;
        }

        public void Write(BinaryWriter writer)
        {
            if (Msisdn == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Msisdn);

            if (Id == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Id);

            writer.Write(CurrentHeaderPosition);

            if (FileName == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(FileName);

            if (ContentType == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(ContentType);

            writer.Write((int)FileState);

            writer.Write(TotalBytes);
        }

        public void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Msisdn == "*@N@*")
                Msisdn = null;

            count = reader.ReadInt32();
            Id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Id == "*@N@*")
                Id = null;

            CurrentHeaderPosition = reader.ReadInt32();

            count = reader.ReadInt32();
            FileName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (FileName == "*@N@*")
                FileName = null;

            count = reader.ReadInt32();
            ContentType = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (ContentType == "*@N@*")
                ContentType = null;

            FileState = (FileTransferState)reader.ReadInt32();

            if (App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && FileState == FileTransferState.STARTED)
                FileState = FileTransferState.PAUSED;

            TotalBytes = reader.ReadInt32();
        }

        public void Save()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + Id;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME);

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME))
                            store.CreateDirectory(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME);

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                Write(writer);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileDownloader :: Save Download Status To IS, Exception : " + ex.StackTrace);
                }
            }
        }

        public void Delete()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + Id;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME))
                            return;

                        if (!store.DirectoryExists(FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME))
                            return;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("FileDownloader :: Delete Download From IS, Exception : " + ex.StackTrace);
                }
            }
        }

        public void Start(object obj)
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + FileName)) as HttpWebRequest;
            req.AllowReadStreamBuffering = false;
            req.Method = "GET";
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
            req.Headers["Range"] = string.Format("bytes={0}-", CurrentHeaderPosition);

            req.BeginGetResponse(DownloadGetResponseCallback, new object[] { req });
        }

        void DownloadGetResponseCallback(IAsyncResult result)
        {
            object[] vars = (object[])result.AsyncState;

            var myHttpWebRequest = (HttpWebRequest)vars[0];
            HttpWebResponse response = null;
            Stream responseStream = null;
            HttpStatusCode responseCode = HttpStatusCode.NotFound;

            try
            {
                response = (HttpWebResponse)myHttpWebRequest.EndGetResponse(result);
                responseCode = response.StatusCode;
                responseStream = response.GetResponseStream();
            }
            catch (Exception e)
            {
                Debug.WriteLine("FileDownloader ::  DownloadGetResponseCallback :  DownloadGetResponseCallback , Exception : " + e.StackTrace);
                responseStream = null;

                var webException = e as WebException;
                if (webException != null)
                {
                    HttpWebResponse webResponse = webException.Response as HttpWebResponse;
                    if (webResponse != null)
                        responseCode = webResponse.StatusCode;
                    else
                        responseCode = HttpStatusCode.RequestTimeout;
                }
            }
            finally
            {
                if (response != null)
                {
                    if (TotalBytes == 0)
                        TotalBytes = (int)response.ContentLength;

                    FileState = FileTransferState.STARTED;

                    if (StatusChanged != null)
                        StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, true));
                }

                ProcessDownloadGetResponse(responseStream, responseCode);
            }
        }

        public void WriteChunkToIsolatedStorage(byte[] bytes, int position)
        {
            string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn.Replace(":", "_") + "/" + Id;
            string fileDirectory = filePath.Substring(0, filePath.LastIndexOf("/"));
            if (bytes != null)
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myIsolatedStorage.DirectoryExists(fileDirectory))
                        myIsolatedStorage.CreateDirectory(fileDirectory);

                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.OpenOrCreate, myIsolatedStorage))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
                        {
                            writer.Seek(position, SeekOrigin.Begin);
                            writer.Write(bytes, 0, bytes.Length);
                        }
                    }
                }
            }
        }

        async void ProcessDownloadGetResponse(Stream responseStream, HttpStatusCode responseCode)
        {
            if (FileState == FileTransferState.CANCELED)
            {
                Delete();
            }
            else if (responseStream != null && (responseCode == HttpStatusCode.PartialContent || responseCode == HttpStatusCode.OK))
            {
                byte[] newBytes = null;
                using (BinaryReader br = new BinaryReader(responseStream))
                {
                    while (BytesTransfered != TotalBytes && FileState == FileTransferState.STARTED)
                    {
                        await Task.Delay(100);

                        newBytes = br.ReadBytes(BlockSize);
                        if (newBytes.Length == 0)
                            break;

                        WriteChunkToIsolatedStorage(newBytes, CurrentHeaderPosition);
                        CurrentHeaderPosition += newBytes.Length;

                        var newSize = (AttemptFactor + AttemptFactor) * DefaultBlockSize;

                        if (newSize <= MaxBlockSize)
                        {
                            AttemptFactor += AttemptFactor;
                            BlockSize = AttemptFactor * DefaultBlockSize;
                        }
                        else
                        {
                            AttemptFactor /= 2;
                            BlockSize = MaxBlockSize;
                        }

                        if (StatusChanged != null)
                            StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, false));
                    }

                    if (FileState == FileTransferState.CANCELED)
                    {
                        Delete();
                    }
                    else if (BytesTransfered == TotalBytes - 1)
                    {
                        FileState = FileTransferState.COMPLETED;

                        if (StatusChanged != null)
                            StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, true));
                    }
                    else if (newBytes.Length == 0)
                    {
                        FileState = FileTransferState.PAUSED;

                        if (StatusChanged != null)
                            StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, true));
                    }
                }
            }
            else if (responseCode == HttpStatusCode.BadRequest)
            {
                FileState = FileTransferState.FAILED;

                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                //});

                if (StatusChanged != null)
                    StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, true));
            }
            else
            {
                if (App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING))
                    FileState = FileTransferState.FAILED;
                else
                    FileState = FileTransferState.PAUSED;

                if (StatusChanged != null)
                    StatusChanged(this, new FileTransferSatatusChangedEventArgs(this, true));
            }
        }

        public event EventHandler<FileTransferSatatusChangedEventArgs> StatusChanged;
    }
}