using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        private long timestamp;
        public TextStatusUpdate(string userName, BitmapImage userImage, string msisdn, string textOrLocationName, long timestamp,
            EventHandler<GestureEventArgs> statusBubbleImageTap)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = textOrLocationName;
            this.timestamp = timestamp;
            if (Utils.isDarkTheme())
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextBlackTheme;
            }
            else
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextWhiteTheme;
            }
            if (statusBubbleImageTap != null)
            {
                    var gl = GestureService.GetGestureListener(this.userProfileImage);
                    gl.Tap += statusBubbleImageTap;
            }
        }

        void timestampTxtBlk_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
