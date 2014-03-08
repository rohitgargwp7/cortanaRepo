﻿using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.Model
{
    public class AlbumClass : List<PhotoClass>
    {
        private string _albumName;
        private Picture _albumPicture;

        public AlbumClass(string albumName)
        {
            _albumName = albumName;
        }

        public Picture AlbumPicture
        {
            get
            {
                return _albumPicture;
            }
            set
            {
                _albumPicture = value;
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
                return App.ViewModel.GetMftImageCache(_albumPicture);
            }
        }

    }
}