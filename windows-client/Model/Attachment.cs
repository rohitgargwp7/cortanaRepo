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
        private volatile AttachemntSource _fileSource;

        public enum AttachmentState
        {
            /// <summary>
            /// File transfer failed.
            /// </summary>
            FAILED = 0,

            /// <summary>
            /// File transfer started.
            /// </summary>
            STARTED,
            
            /// <summary>
            /// File transfer completed.
            /// </summary>
            COMPLETED,
            
            /// <summary>
            /// File transfer was cancelled.
            /// </summary>
            CANCELED,

            /// <summary>
            /// File transfer was paused. 
            /// This will be automatically retried based on user settings.
            /// </summary>
            PAUSED,

            /// <summary>
            /// File transfer was manually paused. 
            /// This will be automatically retried till user taps to retry.
            /// </summary>
            MANUAL_PAUSED,

            /// <summary>
            /// File transfer has not started.
            /// </summary>
            NOT_STARTED
        }

        public enum AttachemntSource
        {
            /// <summary>
            /// The file attachment was recorded from camera.
            /// </summary>
            CAMERA,

            /// <summary>
            /// The file attachment was selected from gallery.
            /// </summary>
            GALLERY,

            /// <summary>
            /// The file attachment was forwarded.
            /// </summary>
            FORWARDED,

            /// <summary>
            /// The file attachment was generated using other means.
            /// </summary>
            OTHER
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

        [DataMember]
        public AttachemntSource FileSource
        {
            get
            {
                return _fileSource;
            }
            set
            {
                if (_fileSource != value)
                    _fileSource = value;
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
                    else
                    {
                        if (ContentType.Contains(HikeConstants.VIDEO))
                            return UI_Utils.Instance.Video_Default;
                        else if (ContentType.Contains(HikeConstants.IMAGE))
                            return UI_Utils.Instance.Image_Default;
                    }
                }

                return _thumbnailImage;
            }
        }

        //newly received messages
        public Attachment(string fileName, string fileKey, byte[] thumbnailBytes, string contentType, AttachmentState attachmentState, AttachemntSource source, int fileSize)
        {
            FileName = fileName;
            FileKey = fileKey;
            Thumbnail = thumbnailBytes;
            ContentType = contentType;
            FileState = attachmentState;
            FileSize = fileSize;
            FileSource = source;
        }

        public Attachment(string fileName, byte[] thumbnailBytes, AttachmentState attachmentState, AttachemntSource source, int fileSize)
        {
            FileName = fileName;
            Thumbnail = thumbnailBytes;
            FileState = attachmentState;
            FileSize = fileSize;
            FileSource = source;
        }

        public Attachment()
        {
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
            writer.Write((int)FileSource);
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

            try
            {
                _fileSource = (AttachemntSource)reader.ReadInt32();
            }
            catch
            {
                _fileSource = AttachemntSource.CAMERA;
            }
        }

        public static string GetAttachmentSource(AttachemntSource source)
        {
            switch(source)
            {
                case AttachemntSource.CAMERA: return "rec";
                case AttachemntSource.GALLERY: return "gal";
                case AttachemntSource.FORWARDED: return "fwd";
                case AttachemntSource.OTHER: return "oth";
                default: return String.Empty;
            }
        }
    }
}
