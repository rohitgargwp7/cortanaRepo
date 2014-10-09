using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Constants
{
    public static class AnalyticsKeys
    {
        //Analytic Events
        public static readonly string STICKER_TIP_TAP_EVENT = "StickerTClick";
        public static readonly string ATTACHMENT_TIP_TAP_EVENT = "AttachmentTClick";
        public static readonly string INFORMATIONAL_TIP_TAP_EVENT = "InformationalTClick";
        public static readonly string THEME_TIP_TAP_EVENT = "ChatThemeTClick";
        public static readonly string STATUS_TIP_TAP_EVENT = "atomicStatusTClick";
        public static readonly string INVITE_TIP_TAP_EVENT = "atomicAppInvFreeSmsTClick";
        public static readonly string FAVOURITE_TIP_TAP_EVENT = "atomicFavTClick";
        public static readonly string PROFILE_PIC_TIP_TAP_EVENT = "atomicProPicTClick";

        public static readonly string INVITE_FRIENDS_FROM_POPUP_FREE_SMS = "inviteFriendsFromPopupFreeSMS";
        public static readonly string INVITE_FRIENDS_FROM_POPUP_REWARDS = "inviteFriendsFromPopupRewards";
        public static readonly string INVITE_SMS_SCREEN_FROM_INVITE = "inviteSMSScreenFromInvite";
        public static readonly string INVITE_SMS_SCREEN_FROM_CREDIT = "inviteSMSScreenFromCredit";
        public static readonly string SELECT_ALL_INVITE = "selectAllInvite";
        public static readonly string START_HIKING = "startHiking";
        public static readonly string FTUE_TUTORIAL_STICKER_VIEWED = "ftueTutorialStickerViewed";
        public static readonly string FTUE_TUTORIAL_CBG_VIEWED = "ftueTutorialCbgViewed";
        public static readonly string FTUE_SET_PROFILE_IMAGE = "ftueSetProfileImage";
        public static readonly string FTUE_CARD_SEE_ALL_CLICKED = "ftueCardSeeAllClicked";
        public static readonly string FTUE_CARD_START_CHAT_CLICKED = "ftueCardStartChatClicked";
        public static readonly string FTUE_CARD_LAST_SEEN_CLICKED = "ftueCardLastSeenClicked";
        public static readonly string FTUE_CARD_GROUP_CHAT_CLICKED = "ftueCardGroupChatClicked";
        public static readonly string FTUE_CARD_PROFILE_PIC_CLICKED = "ftueCardProfilePicClicked";
        public static readonly string FTUE_CARD_POST_STATUS_CLICKED = "ftueCardPostStatusClicked";
        public static readonly string FTUE_CARD_INVITE_CLICKED = "ftueCardInviteClicked";
        public static readonly string DARK_MODE_CLICKED = "darkModeClicked";
        public static readonly string NEW_CHAT_FROM_TOP_BAR = "newChatFromTopBar";

        public static readonly string EVENT_TYPE = "et";
        public static readonly string EVENT_KEY = "ek";
        public static readonly string EVENT_TYPE_CLICK = "click";

        public static readonly string PRO_TIPS_DISMISSED = "tip_id";
        public static readonly string FWD_TO_MULTIPLE = "fwdToMul";
        public static readonly string NULL_PUSH_TOKEN = "npt";
        public static readonly string EXCEPTION_PUSH_TOKEN = "expt";

        public static readonly string ANALYTICS_TAP_HI_WHILE_TIP = "quickSetupClick";
        public static readonly string ANALYTICS_HIDDEN_MODE_PASSWORD_CONFIRMATION = "stlthFtueDone";
        public static readonly string ANALYTICS_TAP_HI_WHILE_NO_TIP = "stlthFtueTap";
        public static readonly string ANALYTICS_INIT_RESET_HIDDEN_MODE = "resetStlthInit";
        public static readonly string ANALYTICS_PWD_CHANGE_HIDDEN_MODE = "changepassStlthSucc";
        public static readonly string ANALYTICS_ENTER_TO_SEND = "entr_2_snd";

        public static readonly string ANALYTICS_EMAIL = "email";
        public static readonly string ANALYTICS_EMAIL_MENU = "menu";
        public static readonly string ANALYTICS_EMAIL_LONGPRESS = "lpress";

    }
}
