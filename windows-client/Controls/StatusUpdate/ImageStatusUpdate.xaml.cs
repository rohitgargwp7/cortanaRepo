﻿using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using System;
using System.Windows.Media;
using windows_client.Model;
using System.Windows;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        private BitmapImage _statusImageSource;
        private long timestamp;
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
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline,
            BitmapImage statusImageBitmap, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, sm.Msisdn, sm.ServerId, isShowOnTimeline)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestamp = sm.Timestamp;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(sm.Timestamp);
            this.Loaded += ImageStatusUpdate_Loaded;
            this.IsUnread = sm.IsUnread;
            this.isShowOnTimeline = isShowOnTimeline;
            if (statusImageBitmap != null)
                this.StatusImage = statusImageBitmap;
            if (imageTap != null)
                this.userProfileImage.Tap += imageTap;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (isShowOnTimeline)
            {
                this.userProfileImage.Source = this.UserImage;
                this.userProfileImage.Height = 69;
                if (sm.IsUnread)
                {
                    statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                }
            }
            else
            {
                this.userProfileImage.Margin = UI_Utils.Instance.StatusImageMargin;
                userProfileImage.Source = UI_Utils.Instance.ProfilePicStatusImage;
            
                if (sm.MoodId < 1)
                    userNameTxtBlk.Visibility = System.Windows.Visibility.Collapsed;

                this.userProfileImage.MaxHeight = 40;
            }
        }

        void ImageStatusUpdate_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (isShowOnTimeline)
                this.userProfileImage.Source = UI_Utils.Instance.GetBitmapImage(this.Msisdn == App.MSISDN ? HikeConstants.MY_PROFILE_PIC : this.Msisdn);
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
