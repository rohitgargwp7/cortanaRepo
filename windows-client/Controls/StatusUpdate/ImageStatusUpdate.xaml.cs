using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using System;
using System.Windows.Media;

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
                    }
                    else //read status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                    }
                }
            }
        }


        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, long statusId, BitmapImage statusImageBitmap, long timestamp,
            bool isUnread, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, msisdn, statusId)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (statusImageBitmap != null)
                this.StatusImage = statusImageBitmap;
            if (imageTap != null)
                this.userProfileImage.Tap += imageTap;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (isUnread)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
            }
            else
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            }
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, long statusId, BitmapImage statusImageBitmap,
            string updateText, long timestamp, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, msisdn, statusId)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = updateText;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            this.StatusImage = statusImageBitmap;
            if (imageTap != null)
                this.userProfileImage.Tap += imageTap;
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
