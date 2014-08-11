using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model.Video
{
    public class VideoClass
    {
        string _filePath;
        string _fileName;
        int _duration;
        int _size;
        byte[] _thumbnail;

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
        public byte[] Thumbnail
        {
            get
            {
                return _thumbnail;
            }
            set
            {
                if (value != _thumbnail)
                    _thumbnail = value;
            }
        }

        public DateTime TimeStamp { get; set; }

        public VideoClass(string fileName, string filePath, byte[] thumbnail,int videoDuration,int videoSize)
        {
            _fileName = fileName;
            _filePath = filePath;
            _thumbnail = thumbnail;
            _duration = videoDuration;
            _size = videoSize;
        }


        public BitmapImage ThumbnailImage
        {
            get
            {
                return UI_Utils.Instance.createImageFromBytes(_thumbnail);
            }
        }
    }
}
