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
    public class FileDownloader : HikeFileInfo
    {
        private const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        private const string FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME = "Download";
        private static object readWriteLock = new object();
       
        public static int MaxBlockSize;

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
        public int BlockSize = 1024;
        public int AttemptNumber = 1;
        public string Id { get; set; }
        public int CurrentHeaderPosition { get; set; }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string Msisdn { get; set; }
        public HikeFileState FileState { get; set; }

        public FileDownloader()
        {
        }

        public FileDownloader(string msisdn, string key, string fileName, string contentType)
        {
            Msisdn = msisdn;
            Id = key;
            FileName = fileName;
            ContentType = contentType;
            FileState = HikeFileState.NOT_STARTED;
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

            writer.Write(FileBytes != null ? FileBytes.Length : 0);
            if (FileBytes != null)
                writer.Write(FileBytes);

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

            FileState = (HikeFileState)reader.ReadInt32();

            if (App.appSettings.Contains(App.AUTO_UPLOAD_SETTING) && FileState == HikeFileState.STARTED)
                FileState = HikeFileState.PAUSED;

            count = reader.ReadInt32();
            FileBytes = count != 0 ? reader.ReadBytes(count) : FileBytes = null;

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

        const int _defaultBlockSize = 1024;

        public event EventHandler<TaskCompletedArgs> DownloadStatusChanged;

        public void BeginDownloadGetRequest(object obj)
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
                    if (FileBytes == null)
                    {
                        TotalBytes = (int)response.ContentLength;
                        FileBytes = new byte[TotalBytes];
                    }

                    FileState = HikeFileState.STARTED;

                    if (DownloadStatusChanged != null)
                        DownloadStatusChanged(this, new TaskCompletedArgs(this, true));
                }

                ProcessDownloadGetResponse(responseStream, responseCode);
            }
        }

        void ProcessDownloadGetResponse(Stream responseStream, HttpStatusCode responseCode)
        {
            if (FileState == HikeFileState.CANCELED)
            {
                FileTransferManager.Instance.DeleteTaskData(Id);
            }
            else if (responseCode == HttpStatusCode.PartialContent || responseCode == HttpStatusCode.OK)
            {
                byte[] newBytes = null;
                using (BinaryReader br = new BinaryReader(responseStream))
                {
                    while (BytesTransfered != TotalBytes && FileState == HikeFileState.STARTED)
                    {
                        newBytes = br.ReadBytes(BlockSize);

                        if (newBytes.Length == 0)
                            break;

                        Array.Copy(newBytes, 0, FileBytes, CurrentHeaderPosition, newBytes.Length);
                        CurrentHeaderPosition += newBytes.Length;

                        var newSize = (AttemptNumber + AttemptNumber) * _defaultBlockSize;

                        if (newSize <= MaxBlockSize)
                        {
                            AttemptNumber += AttemptNumber;
                            BlockSize = AttemptNumber * _defaultBlockSize;
                        }

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(this, false));
                    }

                    if (FileState == HikeFileState.CANCELED)
                    {
                        FileTransferManager.Instance.DeleteTaskData(Id);
                    }
                    else if (BytesTransfered == TotalBytes - 1)
                    {
                        FileState = HikeFileState.COMPLETED;

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(this, true));
                    }
                    else if (newBytes.Length == 0)
                    {
                        FileState = HikeFileState.PAUSED;

                        if (DownloadStatusChanged != null)
                            DownloadStatusChanged(this, new TaskCompletedArgs(this, true));
                    }
                }
            }
            else if (responseCode == HttpStatusCode.BadRequest)
            {
                FileState = HikeFileState.FAILED;

                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                //    MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                //});

                if (DownloadStatusChanged != null)
                    DownloadStatusChanged(this, new TaskCompletedArgs(this, true));
            }
            else
            {
                if (App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING))
                    FileState = HikeFileState.FAILED;
                else
                    FileState = HikeFileState.PAUSED;

                if (DownloadStatusChanged != null)
                    DownloadStatusChanged(this, new TaskCompletedArgs(this, true));
            }
        }
    }
}