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
                    if (isShowOnTimeline)
                    {
                        if (value != true) //read status
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
        }

        public TextStatusUpdate(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline,
            EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap)
            : base(userName, userImage, sm.Msisdn, sm.ServerId, isShowOnTimeline)
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
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (isShowOnTimeline)
            {
                this.statusTypeImage.Source = this.UserImage;
                this.statusTypeImage.MaxWidth = 69;
                statusTextTxtBlk.MaxWidth = 300;
                this.LayoutRoot.Margin = UI_Utils.Instance.TimelineStatusLayoutMargin;
                this.statusTypeImage.Margin = UI_Utils.Instance.TimelineStatusTypeMargin;
                this.statusTextTxtBlk.Margin = UI_Utils.Instance.TimelineStatusTextMargin;
                this.timestampTxtBlk.Margin = UI_Utils.Instance.TimelineStatusTextMargin;
                if (sm.IsUnread)
                {
                    statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                }
            }
            else
            {
                userNameTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                statusTypeImage.Width = 40;
                statusTextTxtBlk.MaxWidth = 380;
                this.LayoutRoot.Margin = UI_Utils.Instance.UserProfileStatusLayoutMargin;
                this.statusTypeImage.Margin = UI_Utils.Instance.UserProfileStatusTypeMargin;
                this.statusTextTxtBlk.Margin = UI_Utils.Instance.UserProfileStatusTextMargin;
                this.timestampTxtBlk.Margin = UI_Utils.Instance.UserProfileStatusTextMargin;
            }
            if (sm.MoodId > 0)
            {
                if (isShowOnTimeline)
                {
                    if (MoodsInitialiser.Instance.IsValidMoodId(sm.MoodId))
                        statusTypeImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
                }
                else
                {
                    statusTypeImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
                }
                moodId = sm.MoodId;
            }
            else if (!isShowOnTimeline)
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
