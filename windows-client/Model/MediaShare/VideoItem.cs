using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    [DataContract]
    public class VideoItem
    {
        string _filePath;
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

        public VideoItem(string filePath, byte[] thumbnail,int videoDuration,int videoSize)
        {
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
