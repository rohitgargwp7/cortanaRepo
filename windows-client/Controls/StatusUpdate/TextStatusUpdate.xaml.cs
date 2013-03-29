using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System;
using System.Windows.Media;
using windows_client.Model;
using System.Windows;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        private long timestamp;
        private int moodId = -1;
        private bool isShowOnTimeline;
        private static Thickness timelineStatusLayoutMargin = new Thickness(0, 12, 0, 12);
        private static Thickness userProfileStatusLayoutMargin = new Thickness(0, 0, 0, 0);

        private static Thickness timelineStatusTypeMargin = new Thickness(0, 8, 0, 0);
        private static Thickness userProfileStatusTypeMargin = new Thickness(12, 34, 0, 0);

        private static Thickness timelineStatusTextMargin = new Thickness(20, 0, 5, 0);
        private static Thickness userProfileStatusTextMargin = new Thickness(18, 0, 5, 0);

        //private static Thickness timelineTimestampMargin = new Thickness(12, 23, 0, 0);
        //private static Thickness userProfileTimestampMargin = new Thickness(12, 34, 0, 0);

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
                        //        statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
                    }
                    else
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                        //        statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
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
            this.Loaded += TextStatusUpdate_Loaded;
            this.isShowOnTimeline = isShowOnTimeline;
            if (statusBubbleImageTap != null)
            {
                this.statusTypeImage.Tap += statusBubbleImageTap;
            }
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
                this.statusTypeImage.Source = this.UserImage;
                this.statusTypeImage.MaxWidth = 69;
                statusTextTxtBlk.MaxWidth = 300;
                this.LayoutRoot.Margin = timelineStatusLayoutMargin;
                this.statusTypeImage.Margin = timelineStatusTypeMargin;
                this.statusTextTxtBlk.Margin = timelineStatusTextMargin;
                this.timestampTxtBlk.Margin = timelineStatusTextMargin;
            }
            else
            {
                userNameTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                statusTypeImage.Width = 40;
                statusTextTxtBlk.MaxWidth = 380;
                this.LayoutRoot.Margin = userProfileStatusLayoutMargin;
                this.statusTypeImage.Margin = userProfileStatusTypeMargin;
                this.statusTextTxtBlk.Margin = userProfileStatusTextMargin;
                this.timestampTxtBlk.Margin = userProfileStatusTextMargin;
            }
            if (sm.MoodId > 0)
            {
                statusTypeImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
                moodId = sm.MoodId;
            }
            else if(!isShowOnTimeline)
            {
                if (sm.Status_Type == StatusMessage.StatusType.TEXT_UPDATE)
                {
                    statusTypeImage.Source = UI_Utils.Instance.TextStatusImage;
                }
                else
                {
                    statusTypeImage.Source = UI_Utils.Instance.FriendRequestImage;
                }
            }
        }

        void TextStatusUpdate_Loaded(object sender, RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            //if there is no mood. refresh user image on loading CC
            if (moodId == -1 && isShowOnTimeline)
            {
                this.statusTypeImage.Source = UI_Utils.Instance.GetBitmapImage(this.Msisdn);
            }
        }
    }
}
