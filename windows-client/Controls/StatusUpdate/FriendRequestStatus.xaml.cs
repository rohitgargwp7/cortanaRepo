using System;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;

namespace windows_client.Controls.StatusUpdate
{
    public partial class FriendRequestStatus : StatusUpdateBox
    {
        public FriendRequestStatus(string userName, BitmapImage userImage,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage)
        {
            InitializeComponent();
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);
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
