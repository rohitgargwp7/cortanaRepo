using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using windows_client.Misc;
using System.IO;

namespace windows_client.Model
{
    [DataContract]
    public class Attachment : IBinarySerializable
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        public Attachment(string fileName, byte[] thumbnailBytes, AttachmentState attachmentState)
        {
            this.FileName = fileName;
            this.Thumbnail = thumbnailBytes;
            this.FileState = attachmentState;
        }

        public Attachment()
        {
            // TODO: Complete member initialization
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

        public void Write(BinaryWriter writer)
        {
            writer.WriteString(FileKey);
            writer.WriteString(FileName);
            writer.WriteString(ContentType);
            writer.Write(_thumbnail != null ?_thumbnail.Length:0);
            if(_thumbnail != null)
                writer.Write(Thumbnail);
            writer.Write((int)FileState);
        }

        public void Read(BinaryReader reader)
        {
            _fileKey = reader.ReadString();
            _fileName = reader.ReadString();
            _contentType = reader.ReadString();
            int count = reader.ReadInt32();
            if (count != 0)
                _thumbnail = reader.ReadBytes(count);
            else
                _thumbnail = null;
            _fileState = (AttachmentState)reader.ReadInt32();
        }
    }
}
