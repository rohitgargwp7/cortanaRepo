using System.IO.IsolatedStorage;
using ProtoBuf;

namespace windows_client.Model
{
    [ProtoContract]

    public class Attachment
    {
        private string _fileKey;
        private string _fileName;
        private string _contentType;
        private byte[] _thumbnail;
        private AttachmentState _fileState;


        public enum AttachmentState
        {
            FAILED_OR_NOT_STARTED = 0,  /* message sent to server */
            STARTED, /* message could not be sent, manually retry */
            COMPLETED, /* message received by server */
            CANCELED
        }

        [ProtoMember(1)]
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

        [ProtoMember(2)]
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

        [ProtoMember(3)]
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

        [ProtoMember(4)]
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

        [ProtoMember(5)]
        public AttachmentState FileState
        {
            get
            {
                return _fileState;
            }
            set
            {
                if (_fileState != value)
                {
                    _fileState = value;
                }
            }

        }

        //newly received messages
        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes, string contentType, AttachmentState attachmentState)
        {
            this.FileName = fileName;
            this.FileKey = fileKey;
            this.Thumbnail = thumbnailBytes;
            this.ContentType = contentType;
            this.FileState = attachmentState;
        }

        //public Attachment(string fileName, string fileKey, string contentType)
        //{
        //    this.FileName = fileName;
        //    this.FileKey = fileKey;
        //    this.ContentType = contentType;
        //}


        public Attachment(string fileName, byte[] thumbnailBytes, AttachmentState attachmentState)
        {
            this.FileName = fileName;
            this.Thumbnail = thumbnailBytes;
            this.FileState = attachmentState;
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
