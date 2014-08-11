using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Controls;
using System.Windows;
using System.ComponentModel;
using System.Windows.Media;
using windows_client.utils;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace windows_client.ServerTips
{
    class TipInfo : TipInfoBase, INotifyPropertyChanged
    {
        public static readonly string CHATPAGE = "cp";
        public static readonly string MAINPAGE = "mp";
        public static readonly string TOP = "tp";
        public static readonly string BOTTOM = "bm";

        public ToolTipsUC TipControl;
        public TipTypes TipType;
        public string Location { get; set; }
        public string Position { get; set; }
        public bool IsSilent { get; set; }
        public TipState State { get; set; }

        public TipInfo(string type, string header, string body, string id)
            : base(type, header, body, id)
        {
            InitializeTipInfo();
        }

        public TipInfo(TipInfoBase tempObj)
            : base(tempObj.Type, tempObj.HeadText, tempObj.BodyText, tempObj.TipId)
        {
            InitializeTipInfo();
        }

        public void InitializeTipInfo()
        {
            switch (base.Type)
            {
                case HikeConstants.STICKER_TIPS:
                    {
                        TipType = TipTypes.STICKERS;
                        IntializeStickersTip();
                        break;
                    }
                case HikeConstants.PROFILE_TIPS:
                    {
                        TipType = TipTypes.PROFILE;

                        IntializeProfileTip();

                        break;
                    }
                case HikeConstants.ATTACHMENT_TIPS:
                    {
                        TipType = TipTypes.ATTACHMENTS;

                        IntializeAttachmentTip();

                        break;
                    }
                case HikeConstants.INFORMATIONAL_TIPS:
                    {
                        TipType = TipTypes.INFORMATIONAL;

                        IntializeInformationalTip();

                        break;
                    }
                case HikeConstants.FAVOURITE_TIPS:
                    {
                        TipType = TipTypes.FAVOURITES;

                        IntializeFavouriteTip();

                        break;
                    }
                case HikeConstants.THEME_TIPS:
                    {
                        TipType = TipTypes.THEMES;

                        InitializeThemeTip();

                        break;
                    }
                case HikeConstants.INVITATION_TIPS:
                    {
                        TipType = TipTypes.INVITE_FRIENDS;

                        IntitializeInviteFriendsTip();

                        break;
                    }
                case HikeConstants.STATUS_UPDATE_TIPS:
                    {
                        TipType = TipTypes.STATUS_UPDATE;

                        IntializeStatusUpdateTip();

                        break;
                    }
            };
        }

        private void IntializeStealthTip()
        {
        }

        void CloseTip(object sender, EventArgs e)
        {
            TipControl.Visibility = Visibility.Collapsed;
            State = TipState.COMPLETED;
        }

        void OpenProfile(object sender, EventArgs e)
        {
            State = TipState.COMPLETED;
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        void OpenFavourites(object sender, EventArgs e)
        {
            State = TipState.COMPLETED;
            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
        }
        private void IntializeStickersTip()
        {
            Location = CHATPAGE;
            Position = BOTTOM;
            TipControl = new ToolTipsUC();
            TipControl.TipText = "This is Sticker Tip";
            TipControl.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
            TipControl.RightIconClicked -= CloseTip;
            TipControl.RightIconClicked += CloseTip;
            /*
                Tip on the bottom of the chat screen
                Image: Arrow to the “sticker” icon.
                Header/body text set from server.
                On tapping the tip, do nothing.
                Silent only - No push
             */

        }

        private void IntializeProfileTip()
        {
            Location = MAINPAGE;
            Position = TOP;
            TipControl = new ToolTipsUC();
            TipControl.TipText = "This is Profile Tip";
            TipControl.FullTipTapped -= OpenProfile;
            TipControl.FullTipTapped += OpenProfile;
            /*
                Tip on the top of the main screen. 
                Image: Profile Pic icon
                Header/body text set from server.
                On tapping the tip, open the Profile screen.
                On clicking push notification, open main screen where the tip is visible

             */
        }

        private void IntializeAttachmentTip()
        {
            Location = CHATPAGE;
            Position = BOTTOM;
            TipControl = new ToolTipsUC();
            TipControl.TipText = "This is Attachment Tip";
            TipControl.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
            TipControl.RightIconClicked -= CloseTip;
            TipControl.RightIconClicked += CloseTip;
            /*
                
                ITip on the top of the chat screen. 
                Image: Arrow to the “attachments” icon.
                Header/body text set from server.
                On tapping the tip, do nothing.
                Silent only - No push
             */
        }

        private void IntializeInformationalTip()
        {
            Location = MAINPAGE;
            Position = TOP;
            TipControl = new ToolTipsUC();
            TipControl.TipText = "This is Informational Tip";
            TipControl.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
            TipControl.RightIconClicked -= CloseTip;
            TipControl.RightIconClicked += CloseTip;
            /*
                Tip on the top of the main screen. 
                Image: Informational icon.
                Header/body text set from server.
                On tapping the tip, do nothing.
                On clicking push notification, open main screen where the tip is visible.

            */
        }

        private void IntializeFavouriteTip()
        {
            Location = MAINPAGE;
            Position = TOP;
            TipControl = new ToolTipsUC();
            TipControl.TipText = "This is Favourite Tip";
            TipControl.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
            TipControl.FullTipTapped -= OpenFavourites;
            TipControl.FullTipTapped += OpenFavourites;
            /*
                Tip on the top of the main screen. 
                Image: Favorites icon.
                Header/body text set from server.
                On tapping the tip, open the Favorites screen.
                On clicking push notification, open main screen where the tip is visible.

            */
        }

        private void InitializeThemeTip()
        {
            /*
                Tip on the top of the chat screen. 
                Image: Arrow to the “chat themes” icon.
                Header/body text set from server.
                On tapping the tip, do nothing.
                Silent only - No push

            */
        }

        private void IntitializeInviteFriendsTip()
        {
            /*
                Tip on the top of the main screen. 
                Image: Invite icon.
                Header/body text set from server.
                On tapping the tip, open the Invite Friends via SMS screen. (Ideally scroll to the Add Favorites section, based on feasibility)
                On clicking push notification, open main screen where the tip is visible.

            */
        }

        private void IntializeStatusUpdateTip()
        {
            /*

                Tip on the top of the main screen. 
                Image: Status Update icon
                Header/body text set from server.
                On tapping the tip, open the Profile screen.
                On clicking push notification, open main screen where the tip is visible.

            */
        }

    }
}
