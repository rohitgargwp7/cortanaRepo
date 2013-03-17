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
        private bool _showOnTimeline = false;

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

        public TextStatusUpdate(string userName, BitmapImage userImage, string msisdn, string serverId, string textOrLocationName, long timestamp,
            bool isUnread, StatusMessage.StatusType statusType, EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap)
            : base(userName, userImage, msisdn, serverId)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = textOrLocationName;
            this.timestamp = timestamp;
            this.IsUnread = isUnread;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (statusBubbleImageTap != null)
            {
                this.userProfileImage.Tap += statusBubbleImageTap;
            }
            if (isUnread)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiBoldFont;
            }
            else
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
                statusTextTxtBlk.FontFamily = UI_Utils.Instance.SemiLightFont;
            }
            if (statusType == StatusMessage.StatusType.IS_NOW_FRIEND)
            {
                statusTypeImage.Source = UI_Utils.Instance.FriendRequestImage;
            }
            else
            {
                statusTypeImage.Source = UI_Utils.Instance.TextStatusImage;
            }
            if (_showOnTimeline)
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
