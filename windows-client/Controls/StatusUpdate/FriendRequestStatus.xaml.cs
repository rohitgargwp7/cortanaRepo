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

        public FriendRequestStatus(string userName, BitmapImage userImage, string msisdn, string mappedStatusId,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage, msisdn, mappedStatusId)
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

        public FriendRequestStatus(ConversationListObject c, string mappedStatusId,
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(c, mappedStatusId)
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
        }

        public void update(ConversationListObject c)
        {
            this.seeUpdatesTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, c.NameToShow);
            this.UserImage = c.AvatarImage;
        }
    }
}
