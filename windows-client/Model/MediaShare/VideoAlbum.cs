﻿using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Model
{
    /// <summary>
    /// It inherits List VideoItem. It clubs videos into respective albums.
    /// </summary>
    public class VideoAlbum : List<VideoItem>
    {
        private string _albumName;
        private byte[] _thumbBytes;
        private BitmapImage _thumbImage;

        public VideoAlbum(string albumName, byte[] thumbnail)
        {
            _albumName = albumName;
            _thumbBytes = thumbnail;
        }

        public Byte[] ThumbBytes
        {
            get
            {
                return _thumbBytes;
            }
            set
            {
                _thumbBytes = value;
            }
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
                    _thumbImage = UI_Utils.Instance.createImageFromBytes(_thumbBytes);
                
                return _thumbImage;
            }
        }

    }
}
