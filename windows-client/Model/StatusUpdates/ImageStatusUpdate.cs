using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using windows_client.utils;

namespace windows_client.Model
{
    public class ImageStatus : BaseStatusUpdate
    {
        public ImageStatus(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline, BitmapImage statusImageBitmap)
            : base(userName, userImage, sm.Msisdn, sm.ServerId, isShowOnTimeline)
        {
            Text = AppResources.StatusUpdate_Photo;
            Timestamp = sm.Timestamp;
            IsUnread = sm.IsUnread;
            IsShowOnTimeline = isShowOnTimeline;
            if (statusImageBitmap != null)
                StatusImage = statusImageBitmap;
        }

        public SolidColorBrush StatusTextForeground
        {
            get
            {
                if (IsShowOnTimeline)
                {
                    if (IsUnread != true) //read status
                        return UI_Utils.Instance.StatusTextForeground;
                    else
                        return (SolidColorBrush)App.Current.Resources["HikeBlueHeader"];
                }
                else
                    return UI_Utils.Instance.StatusTextForeground;
            }
        }

        public override BitmapImage UserImage
        {
            get
            {
                if (IsShowOnTimeline)
                    return base.UserImage;
                else
                    return UI_Utils.Instance.ProfilePicStatusImage;
            }
        }

        private BitmapImage _statusImage;
        public BitmapImage StatusImage
        {
            get
            {
                return _statusImage;
            }
            set
            {
                if (value != _statusImage)
                {
                    _statusImage = value;
                    NotifyPropertyChanged("StatusImage");
                }
            }
        }
    }
}
