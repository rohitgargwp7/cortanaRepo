using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using windows_client.DbUtils;
using windows_client.utils;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace windows_client.FileTransfers
{
    public abstract class FileInfoBase
    {
        protected const string FILE_TRANSFER_DIRECTORY_NAME = "FileTransfer";
        protected const string FILE_TRANSFER_UPLOAD_DIRECTORY_NAME = "Upload";
        protected const string FILE_TRANSFER_DOWNLOAD_DIRECTORY_NAME = "Download";
        protected const int DefaultBlockSize = 1024;
        public static int MaxBlockSize;
        protected int BlockSize = 1024;
        protected int ChunkFactor = 1;
        
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
        public string MessageId { get; set; }
        public int CurrentHeaderPosition { get; set; }
        public string ContentType { get; set; }
        public string FileName { get; set; }
        public string Msisdn { get; set; }
        public FileTransferState FileState { get; set; }

        public FileInfoBase()
        {
        }

        public FileInfoBase(string msisdn, string messageId, string fileName, string contentType, int size)
        {
            Msisdn = msisdn;
            MessageId = messageId;
            TotalBytes = size;
            FileName = fileName;
            ContentType = contentType;
            FileState = FileTransferState.NOT_STARTED;
        }

        public event EventHandler<FileTransferSatatusChangedEventArgs> StatusChanged;

        protected virtual void OnStatusChanged(FileTransferSatatusChangedEventArgs e)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            EventHandler<FileTransferSatatusChangedEventArgs> handler = StatusChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public abstract void Write(BinaryWriter writer);
        public abstract void Read(BinaryReader reader);
        public abstract void Save();
        public abstract void Delete();
        public abstract void Start(object obj);
        public abstract void CheckIfComplete();

        bool retry = true;
        short retryAttempts = 0;
        const short MAX_RETRY_ATTEMPTS = 5;
        int reconnectTime = 0;
        const int MAX_RECONNECT_TIME = 30; // in seconds

        protected bool ShouldRetry()
        {
            if (retry && retryAttempts < MAX_RETRY_ATTEMPTS)
            {
                // make first attempt within first 5 seconds
                if (reconnectTime == 0)
                {
                    Random random = new Random();
                    reconnectTime = random.Next(5) + 1;
                }
                else
                {
                    reconnectTime *= 2;
                }

                reconnectTime = reconnectTime > MAX_RECONNECT_TIME ? MAX_RECONNECT_TIME : reconnectTime;

                try
                {
                    Thread.Sleep(reconnectTime * 1000);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                retryAttempts++;

                return true;
            }
            else
                return false;
        }

        protected async Task<bool> CheckForCRC(string key)
        {
            if (TotalBytes > HikeConstants.FILE_MAX_SIZE)
                return true;

            string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn.Replace(":", "_") + "/" + MessageId;
            byte[] bytes;
            MiscDBUtil.readFileFromIsolatedStorage(filePath, out bytes);

            if (bytes == null || bytes.Length == 0)
                return false;

            String md5 = string.Empty;
            try
            {
                md5 = MD5CryptoServiceProvider.GetMd5String(bytes);
            }
            catch
            {
                return false;
            }

            string result = String.Empty;

            if (this is FileDownloader)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, new Uri(HikeConstants.FILE_TRANSFER_BASE_URL + "/" + key));

                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    result = response.Headers.GetValues("Etag").First().ToString();
                }
                catch
                {
                    result = String.Empty;
                }
            }
            else
            {
                try
                {
                    var jData = (this as FileUploader).SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                    result = jData[HikeConstants.MD5_ORIGINAL].ToString();
                }
                catch
                {
                    result = String.Empty;
                }
            }

            return md5 == result;
        }
    }
}
