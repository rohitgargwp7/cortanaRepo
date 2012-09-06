using System.IO.IsolatedStorage;

namespace windows_client.Model
{
    public class Attachment
    {
        private string _fileKey;
        private string _fileName;
        private string _contentType;
        private byte[] _thumbnail;

        public string FileKey
        {
            get
            {
                return _fileKey;
            }
            set
            {
                if (_fileKey != value)
                {
                    _fileKey = value;
                }
            }
        }

        public string FileName
        {
            get
            {
                return _fileName;
            }
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                }
            }
        }

        public string ContentType
        {
            get
            {
                return _contentType;
            }
            set
            {
                if (_contentType != value)
                {
                    _contentType = value;
                }
            }
        }

        public byte[] Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            set
            {
                if (_thumbnail != value)
                {
                    _thumbnail = value;
                }
            }
        }

        //newly received messages
        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes, string contentType)
        {
            this.FileName = fileName;
            this.FileKey = fileKey;
            this.Thumbnail = thumbnailBytes;
            this.ContentType = contentType;
        }

        public Attachment(string fileName, byte[] thumbnailBytes)
        {
            this.FileName = fileName;
            this.Thumbnail = thumbnailBytes;
        }

        public static string[] getAttachmentFiles(string msisdn)
        {
            string filePath = HikeConstants.FILE_TRANSFER_LOCATION + "/" + "Attachments" + "/" + msisdn + "/*";
            string[] fileNames = null;
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    fileNames = myIsolatedStorage.GetFileNames(filePath);
                }
            }
            return fileNames;
        }
    }
}
