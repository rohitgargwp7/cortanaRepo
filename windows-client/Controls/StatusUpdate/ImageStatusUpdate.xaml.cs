﻿using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using System;
using System.Windows.Media;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        private BitmapImage _statusImageSource;

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
                    else //unread status
                    {
                        statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
                    }
                }
            }
        }


        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, BitmapImage statusImageBitmap, long timestamp,
            bool isRead, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (statusImageBitmap != null)
                this.StatusImage = statusImageBitmap;
            if (imageTap != null)
                this.userProfileImage.Tap += imageTap;
            statusTextTxtBlk.Foreground = UI_Utils.Instance.StatusTextForeground;
            if (!isRead)
            {
                statusTextTxtBlk.Foreground = UI_Utils.Instance.PhoneThemeColor;
            }
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, BitmapImage statusImageBitmap, string updateText,
            long timestamp, EventHandler<System.Windows.Input.GestureEventArgs> imageTap)
            : base(userName, userImage, msisdn)
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
