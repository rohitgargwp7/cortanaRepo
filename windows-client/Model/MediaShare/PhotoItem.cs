using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    [DataContract]
    public class PhotoItem : INotifyPropertyChanged
    {
        Picture _pic;
        bool _isSelected;

        public BitmapImage Thumbnail
        {
            get
            {
                return AddMoreImage ? new BitmapImage(new Uri("/View/images/add.png", UriKind.RelativeOrAbsolute)) : App.ViewModel.GetMftImageCache(Pic);
            }
        }
        public BitmapImage ImageSource
        {
            get
            {
                BitmapImage _image = new BitmapImage();

                if (Pic != null)
                {
                    int toWidth = UI_Utils.Instance.GetMaxToWidthForImage(Pic.Height, Pic.Width);
                    if (toWidth != 0)
                        _image.DecodePixelWidth = toWidth;
                    _image.SetSource(Pic.GetImage());
                }
                return _image;
            }
        }

        [DataMember]
        public DateTime TimeStamp { get; set; }

        [DataMember]
        public string Title { get; set; }
        public Picture Pic
        {
            get
            {
                if (_pic == null)
                {
                    //this will trigger only in case of tombstoning
                    MediaLibrary lib = new MediaLibrary();
                    var list = lib.Pictures.Where(x => x.Date == TimeStamp);
                    if (list != null && list.Count() > 0)
                    {
                        _pic = list.FirstOrDefault();
                    }
                }
                return _pic;
            }
        }

        public PhotoItem(Picture pic)
        {
            _pic = pic;
        }
        [DataMember]
        public bool AddMoreImage
        {
            get;
            set;
        }
        [DataMember]
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
                return IsSelected ? UI_Utils.Instance.PhoneThemeColor : UI_Utils.Instance.Transparent;
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
