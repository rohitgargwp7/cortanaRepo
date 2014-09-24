using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    /// <summary>
    /// It inherits list PhotoItem.
    /// </summary>
    public class PhotoAlbum : List<PhotoItem>
    {
        private string _albumName;
        private Picture _albumPicture;

        public PhotoAlbum(string albumName)
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
                return HikeInstantiation.ViewModel.GetMftImageCache(_albumPicture);
            }
        }

    }
}
