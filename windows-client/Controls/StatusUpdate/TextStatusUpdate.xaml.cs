using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System;
using System.Windows.Media;
using windows_client.Model;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        private long timestamp;

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
                    if (value != true) //read status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                        statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
                    }
                    else
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                        statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
                    }
                }
            }
        }

        public TextStatusUpdate(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline,
            EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap)
            : base(userName, userImage, sm.Msisdn, sm.ServerId)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = sm.Message;
            this.timestamp = sm.Timestamp;
            this.IsUnread = sm.IsUnread;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (statusBubbleImageTap != null)
            {
                this.userProfileImage.Tap += statusBubbleImageTap;
            }
            if (sm.IsUnread)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
            }
            else
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
            }
            if (sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
            {
                statusTypeImage.Source = UI_Utils.Instance.FriendRequestImage;
            }
            else
            {
                statusTypeImage.Source = UI_Utils.Instance.TextStatusImage;
            }
            if (isShowOnTimeline)
            {
                this.userProfileImage.Source = this.UserImage;
                this.userProfileImage.MinHeight = 69;
            }
            else
            {
                this.userProfileImage.Source = UI_Utils.Instance.TextStatusImage;
                this.userProfileImage.MaxHeight = 60;
                statusTypeImage.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        void timestampTxtBlk_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
