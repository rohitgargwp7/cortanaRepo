using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    public class PhotoClass : INotifyPropertyChanged
    {
        Picture _pic;
        bool _isSelected;

        public BitmapImage Thumbnail
        {
            get
            {
                return AddMoreImage ? new BitmapImage(new Uri("/View/images/add.png", UriKind.RelativeOrAbsolute)) : App.ViewModel.GetMftImageCache(_pic);
            }
        }
        public BitmapImage ImageSource
        {
            get
            {
                BitmapImage _image = new BitmapImage();

                if (_pic != null)
                {
                    int toWidth = UI_Utils.Instance.GetMaxToWidthForImage(_pic.Height, _pic.Width);
                    if (toWidth != 0)
                        _image.DecodePixelWidth = toWidth;
                    _image.SetSource(_pic.GetImage());
                }
                return _image;
            }
        }
        public DateTime TimeStamp { get; set; }
        public string Title { get; set; }
        public Picture Pic
        {
            get
            {
                return _pic;
            }
        }

        //default constructor added so that it can be serialized and deserialized by phone application service 
        public PhotoClass()
        {
        }
        public PhotoClass(Picture pic)
        {
            _pic = pic;
        }

        public bool AddMoreImage
        {
            get;
            set;
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("ThumbnailBorder");
                }
            }
        }

        public SolidColorBrush ThumbnailBorder
        {
            get
            {
                return IsSelected ? UI_Utils.Instance.HikeBlue : UI_Utils.Instance.Transparent;
            }
        }

        #region InotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Photo Model :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }
        #endregion
    }
}
