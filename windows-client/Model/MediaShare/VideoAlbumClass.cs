﻿using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Model.Video;
using windows_client.utils;

namespace windows_client.Model
{
    public class VideoAlbumClass : List<VideoClass>
    {
        private string _albumName;
        private byte[] _thumbBytes;
        private BitmapImage _thumbImage;

        public VideoAlbumClass(string albumName, byte[] thumbnail)
        {
            _albumName = albumName;
            _thumbBytes = thumbnail;
        }


        public string AlbumName
        {
            get
            {
                return _albumName;
            }
        }

        public BitmapImage Image
        {
            get
            {
                if (_thumbImage == null)
                {
                    _thumbImage = UI_Utils.Instance.createImageFromBytes(_thumbBytes);
                }
                return _thumbImage;
            }
        }

    }
}
