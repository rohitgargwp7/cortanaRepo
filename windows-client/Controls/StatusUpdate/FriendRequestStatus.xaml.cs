using System;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using windows_client.Model;

namespace windows_client.Controls.StatusUpdate
{
    public partial class FriendRequestStatus : StatusUpdateBox
    {
        private ConversationListObject convObj;

        public FriendRequestStatus(string userName, BitmapImage userImage, string msisdn,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage, msisdn, string.Empty)
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

        public FriendRequestStatus(ConversationListObject c,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap,
            EventHandler<System.Windows.Input.GestureEventArgs> bubbleTap)
            : base(c, string.Empty)
        {
            InitializeComponent();
            convObj = c;
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, c.NameToShow);
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
            this.Tap += bubbleTap;
        }

        public void update(ConversationListObject c)
        {
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, c.NameToShow);
            this.UserImage = c.AvatarImage;
        }
    }
}
