﻿using System;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;

namespace windows_client.Controls.StatusUpdate
{
    public partial class FriendRequestStatus : StatusUpdateBox
    {
        public FriendRequestStatus(string userName, BitmapImage userImage, string msisdn,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.seeUpdatesTxtBlk.Text = userName + "has added"; //string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);
            if (yesTap != null)
            {
                var glYes = GestureService.GetGestureListener(this.yesBtn);
                glYes.Tap += yesTap;
            }
            if (noTap != null)
            {
                var glNo = GestureService.GetGestureListener(this.noBtn);
                glNo.Tap += noTap;
            }
        }
    }
}
