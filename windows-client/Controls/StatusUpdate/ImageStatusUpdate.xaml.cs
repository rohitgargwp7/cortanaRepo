using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using System;
using System.Windows.Media;
using windows_client.Model;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        private BitmapImage _statusImageSource;

        public override bool IsUnread
        {
            get
            {
                return base.IsUnread;
            }
            set
            {
                if (value != base.IsUnread)
                {
                    base.IsUnread = value;
                    if (value == true) //unread status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                        //statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
                    }
                    else //read status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                        //statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
                    }
                }
            }
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline,
            BitmapImage statusImageBitmap, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, sm.Msisdn, sm.ServerId)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(sm.Timestamp);
            this.IsUnread = sm.IsUnread;
            if (statusImageBitmap != null)
                this.StatusImage = statusImageBitmap;
            if (imageTap != null)
                this.userProfileImage.Tap += imageTap;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (sm.IsUnread)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                //statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
            }
            else
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                //statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
            }
            if (isShowOnTimeline)
            {
                this.userProfileImage.Source = this.UserImage;
                this.userProfileImage.Height = 69;
                statusTypeImage.Source = UI_Utils.Instance.ProfilePicStatusImage;
                statusTypeImage.Width = 31;
            }
            else
            {
                statusTypeImage.Width = 35;
                if (sm.MoodId > 0) //For profile pic update. Mood id won't be received. Kept this for future.
                {
                    this.userProfileImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
                    this.userProfileImage.MaxHeight = 60;
                    statusTypeImage.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    this.userProfileImage.Visibility = System.Windows.Visibility.Collapsed;
                    statusTypeImage.Source = UI_Utils.Instance.ProfilePicStatusImage;
                    statusTypeImage.Visibility = System.Windows.Visibility.Visible;
                    userNameTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        public BitmapImage StatusImage
        {
            get
            {
                return _statusImageSource;
            }
            set
            {
                if (value != _statusImageSource)
                {
                    this.statusImage.Source = _statusImageSource = value;
                }
            }
        }
    }
}
