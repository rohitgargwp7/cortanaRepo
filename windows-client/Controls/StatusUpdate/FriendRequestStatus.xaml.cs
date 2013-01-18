using System;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;

namespace windows_client.Controls.StatusUpdate
{
    public partial class FriendRequestStatus : StatusUpdateBox
    {
        //public FriendRequestStatus(string userName, BitmapImage userImage)
        //    : base(userName, userImage)
        //{
        //    InitializeComponent();
        //    this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);
        //}

        public FriendRequestStatus(string userName, BitmapImage userImage, 
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage, StatusType.FRIEND_REQUEST)
        {
            InitializeComponent();
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);

            var glYes = GestureService.GetGestureListener(this.yesBtn);
            glYes.Tap += yesTap;
            var glNo = GestureService.GetGestureListener(this.noBtn);
            glNo.Tap += noTap;
        }
    }
}
