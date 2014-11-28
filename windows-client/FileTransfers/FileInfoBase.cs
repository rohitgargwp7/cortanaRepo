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
        protected int DefaultBlockSize = 1024;
        public static int MaxBlockSize;
        static readonly int MAX_RECONNECT_TIME = 20; // in seconds
        public static readonly int HTTP_TIMEOUT = 55000; // in ms
        protected int BlockSize = 1024;
        protected int ChunkFactor = 1;
        protected short MaxRetryAttempts = 10;

        bool _retry = true;
        short _retryAttempts = 0;
        int _reconnectTime = 0;

        ManualResetEvent _sleep = new ManualResetEvent(false);

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

        string _filePath = null;
        public string FilePath
        {
            get
            {
                if (String.IsNullOrEmpty(_filePath))
                    _filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + Msisdn.Replace(":", "_") + "/" + MessageId;

                return _filePath;
            }
        }

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
        
        public void ResetRetryOnNetworkChanged()
        {
            _sleep.Set();
        }

        protected void ResetRetryOnSuccess()
        {
            _retryAttempts = 0;
            _reconnectTime = 0;
        }

        protected bool ShouldRetry()
        {
            if (_retry && _retryAttempts < MaxRetryAttempts)
            {
                // make first attempt within first 5 seconds
                if (_reconnectTime == 0)
                {
                    Random random = new Random();
                    _reconnectTime = random.Next(5) + 1;
                }
                else
                {
                    _reconnectTime *= 2;
                }

                _reconnectTime = _reconnectTime > MAX_RECONNECT_TIME ? MAX_RECONNECT_TIME : _reconnectTime;

                try
                {
                    _sleep.WaitOne(TimeSpan.FromMilliseconds(_reconnectTime * 1000));
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                _retryAttempts++;

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Check for CRC
        /// </summary>
        /// <param name="key">file key for which head call needs to be made</param>
        /// <returns>true if md5 matches else false</returns>
        protected async Task<bool> CheckForCRC(string key)
        {
            if (TotalBytes > HikeConstants.FILE_MAX_SIZE)
                return true;

            // Calculate md5 by parts
            String md5 = Utils.GetMD5Hash(FilePath);

            string result = String.Empty;

            if (this is FileDownloader)
            {
                try
                {
                    HttpClient httpClient = new HttpClient();

                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, new Uri(AccountUtils.FILE_TRANSFER_BASE_URL + "/" + key));
                    request.Headers.Add(HikeConstants.IfModifiedSince, DateTime.UtcNow.ToString());

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

            return md5.Equals(result, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
