using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model.Video
{
    [DataContract]
    public class VideoClass
    {
        string _filePath;
        string _fileName;
        int _duration;
        int _size;
        byte[] _thumbnailBytes;

        [DataMember]
        public string FilePath
        {
            get
            {
                return _filePath;
            }
            set
            {
                if (value != _filePath)
                    _filePath = value;
            }
        }

        [DataMember]
        public int Duration
        {
            get
            {
                return _duration;
            }
            set
            {
                if (value != _duration)
                    _duration = value;
            }
        }

        [DataMember]
        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (value != _size)
                    _size = value;
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
                if (value != _fileName)
                    _fileName = value;
            }
        }

        [DataMember]
        public byte[] ThumbnailBytes
        {
            get
            {
                return _thumbnailBytes;
            }
            set
            {
                if (value != _thumbnailBytes)
                    _thumbnailBytes = value;
            }
        }

        [DataMember]
        public DateTime TimeStamp { get; set; }

        public VideoClass(string fileName, string filePath, byte[] thumbnail,int videoDuration,int videoSize)
        {
            _fileName = fileName;
            _filePath = filePath;
            _thumbnailBytes = thumbnail;
            _duration = videoDuration;
            _size = videoSize;
        }


        BitmapImage _thumbnailImage;
        public BitmapImage ThumbnailImage
        {
            get
            {
                if (_thumbnailImage == null)
                    _thumbnailImage = UI_Utils.Instance.createImageFromBytes(_thumbnailBytes);
                return _thumbnailImage;
            }
        }
    }
}
