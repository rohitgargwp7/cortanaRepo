using System.IO.IsolatedStorage;
using System.Runtime.Serialization;
using windows_client.Misc;
using System.IO;
using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    [DataContract]
    public class Attachment : IBinarySerializable
    {
        private int _fileSize;
        private string _fileKey;
        private string _fileName;
        private string _contentType;
        private byte[] _thumbnail;
        private volatile AttachmentState _fileState;

        public enum AttachmentState
        {
            FAILED = 0,  /* message sent to server */
            STARTED, /* message could not be sent, manually retry */
            COMPLETED, /* message received by server */
            CANCELED,
            PAUSED,
            MANUAL_PAUSED,
            NOT_STARTED
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
        public int FileSize
        {
            get
            {
                return _fileSize;
            }
            set
            {
                if (_fileSize != value)
                {
                    _fileSize = value;
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

        BitmapImage _thumbnailImage;
        public BitmapImage ThumbnailImage
        {
            get
            {
                if (_thumbnailImage == null)
                {
                    if (Thumbnail != null)
                        _thumbnailImage = UI_Utils.Instance.createImageFromBytes(Thumbnail);

                    //todo handle default thumbnail
                }

                return _thumbnailImage;
            }
        }

        //newly received messages
        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes, string contentType, AttachmentState attachmentState, int fileSize)
        {
            this.FileName = fileName;
            this.FileKey = fileKey;
            this.Thumbnail = thumbnailBytes;
            this.ContentType = contentType;
            this.FileState = attachmentState;
            this.FileSize = fileSize;
        }

        public Attachment(string fileName, byte[] thumbnailBytes, AttachmentState attachmentState, int fileSize)
        {
            this.FileName = fileName;
            this.Thumbnail = thumbnailBytes;
            this.FileState = attachmentState;
            this.FileSize = fileSize;
        }

        public Attachment()
        {
            // TODO: Complete member initialization
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
            writer.Write(FileSize);
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
            try
            {
                _fileSize = reader.ReadInt32();
            }
            catch
            {
                _fileSize = 0;
            }
        }
    }
}
