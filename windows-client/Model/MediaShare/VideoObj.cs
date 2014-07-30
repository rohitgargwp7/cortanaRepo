using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model.Video
{
    class VideoType
    {
        string _filePath;
        string _fileName;
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


        public VideoType(string fileName ,string filePath, byte[] thumbnail)
        {
            _fileName = fileName;
            _filePath = filePath;
            _thumbnail = thumbnail;
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
