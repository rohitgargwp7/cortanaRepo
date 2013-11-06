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
using System.Windows;
using windows_client.Languages;
using System.Threading;
using System.Net.Http;

namespace windows_client.FileTransfers
{
    public class FileDownloader : FileInfoBase
    {
        static object readWriteLock = new object();

        public FileDownloader()
            : base()
        {
        }

        public FileDownloader(string msisdn, string messageId, string fileName, string contentType, int size)
            : base(msisdn, messageId, fileName, contentType, size)
        {
        }

        public override void Write(BinaryWriter writer)
        {
            if (Msisdn == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(Msisdn);

            if (MessageId == null)
                writer.WriteStringBytes("*@N@*");
            else
                writer.WriteStringBytes(MessageId);

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

        public override void Read(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            Msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (Msisdn == "*@N@*")
                Msisdn = null;

            count = reader.ReadInt32();
            MessageId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (MessageId == "*@N@*")
                MessageId = null;

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

            if (App.appSettings.Contains(App.AUTO_RESUME_SETTING) && FileState == FileTransferState.STARTED)
                FileState = FileTransferState.PAUSED;

            TotalBytes = reader.ReadInt32();
        }

        public override void Save()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + MessageId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
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

        public override void Delete()
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FILE_TRANSFER_DIRECTORY_NAME + "\\" + FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME + "\\" + MessageId;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
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

        public override void Start(object obj)
        {
            var req = HttpWebRequest.Create(new Uri(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + FileName)) as HttpWebRequest;
            req.AllowReadStreamBuffering = false;
            req.Method = "GET";
            req.Headers[HttpRequestHeader.IfModifiedSince] = DateTime.UtcNow.ToString();
            req.Headers["Range"] = string.Format("bytes={0}-", CurrentHeaderPosition);

            req.BeginGetResponse(DownloadGetResponseCallback, new object[] { req });
        }

        public async override void CheckIfComplete()
        {
            var result = await CheckForCRC(FileName);

            if (result)
            {
                FileState = FileTransferState.COMPLETED;
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                Save();
            }
            else
            {
                FileState = FileTransferState.FAILED;
                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                Delete();
            }
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
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                }

                ProcessDownloadGetResponse(responseStream, responseCode);
            }
        }

        void ProcessDownloadGetResponse(Stream responseStream, HttpStatusCode responseCode)
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
                        newBytes = br.ReadBytes(BlockSize);
                        if (newBytes.Length == 0)
                            break;

                        WriteChunkToIsolatedStorage(newBytes, CurrentHeaderPosition);
                        CurrentHeaderPosition += newBytes.Length;

                        var newSize = (ChunkFactor + ChunkFactor) * DefaultBlockSize;

                        if (newSize <= MaxBlockSize)
                        {
                            ChunkFactor += ChunkFactor;
                            BlockSize = ChunkFactor * DefaultBlockSize;
                        }
                        else
                        {
                            ChunkFactor /= 2;
                            BlockSize = MaxBlockSize;
                        }

                        OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, false));
                    }

                    if (FileState == FileTransferState.CANCELED)
                    {
                        Delete();
                    }
                    else if (BytesTransfered == TotalBytes - 1)
                    {
                        CheckIfComplete();
                    }
                    else if (newBytes.Length == 0)
                    {
                        if (ShouldRetry())
                        {
                            Start(null);
                        }
                        else
                        {
                            FileState = FileTransferState.FAILED;
                            OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                            Save();
                        }
                    }
                }
            }
            else if (responseCode == HttpStatusCode.BadRequest)
            {
                FileState = FileTransferState.FAILED;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                });

                OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                Delete();
            }
            else
            {
                if (ShouldRetry())
                {
                    Start(null);
                }
                else
                {
                    FileState = FileTransferState.FAILED;
                    OnStatusChanged(new FileTransferSatatusChangedEventArgs(this, true));
                    Save();
                }
            }
        }

        void WriteChunkToIsolatedStorage(byte[] bytes, int position)
        {
            string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn.Replace(":", "_") + "/" + MessageId;
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

        protected override void OnStatusChanged(FileTransferSatatusChangedEventArgs e)
        {
            // Call the base class event invocation method. 
            base.OnStatusChanged(e);
        }
    }
}