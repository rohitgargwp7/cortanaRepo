﻿using windows_client.utils;
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
            if (statusBubbleImageTap != null)
            {
                this.userProfileImage.Tap += statusBubbleImageTap;
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
            if (sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
            {
                statusTypeImage.Source = UI_Utils.Instance.FriendRequestImage;
            }
            else
            {
                statusTypeImage.Source = UI_Utils.Instance.TextStatusImage;
            }
            if (sm.MoodId > 0)
            {
                statusTypeImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
            }
            if (isShowOnTimeline)
            {
                this.userProfileImage.Source = this.UserImage;
                this.userProfileImage.Height = 69;
                statusTypeImage.Width = 31;
                statusTextTxtBlk.MaxWidth = 300;
            }
            else
            {
                userNameTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                this.userProfileImage.Visibility = System.Windows.Visibility.Collapsed;
                statusTypeImage.Width = 35;
                statusTextTxtBlk.MaxWidth = 380;
            }
        }

        void timestampTxtBlk_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
