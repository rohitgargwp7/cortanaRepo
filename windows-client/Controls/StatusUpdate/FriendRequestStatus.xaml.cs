using System;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using windows_client.Model;
using windows_client.utils;

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
            string firstName = Utils.GetFirstName(userName);
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, firstName);
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
            string firstName = Utils.GetFirstName(c.NameToShow);
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, firstName);
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
            string firstName = Utils.GetFirstName(c.NameToShow);
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, firstName);
            this.UserImage = c.AvatarImage;
        }
    }
}
