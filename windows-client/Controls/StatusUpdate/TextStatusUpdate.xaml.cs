using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System;
using System.Windows.Media;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        private long timestamp;

        public override bool IsRead
        {
            get
            {
                return base.IsRead;
            }
            set
            {
                if (value != base.IsRead)
                {
                    base.IsRead = value;
                    if (value == true) //read status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                    }
                    else
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                    }
                }
            }
        }

        public TextStatusUpdate(string userName, BitmapImage userImage, string msisdn, string textOrLocationName, long timestamp,
            bool isRead, EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = textOrLocationName;
            this.timestamp = timestamp;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (statusBubbleImageTap != null)
            {
                this.userProfileImage.Tap += statusBubbleImageTap;
            }
            if (!isRead)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
            }
        }

        void timestampTxtBlk_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
