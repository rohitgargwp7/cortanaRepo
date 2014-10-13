using CommonLibrary.Constants;
using System;
using windows_client.utils;

namespace windows_client
{
    public class HikeConstants
    {
        #region All Uncategorized constants
        public static readonly string PushNotificationChannelName = "HikeApp";
        public static readonly string VoipBackgroundTaskName = "HikeVoipBackgroundAgent";

        public static readonly string TEMP_VIDEO_NAME = "CameraMovie.mp4";
        public static readonly string HikeDirectoryPath = @"C:/Data/Users/Public/Pictures/hike";
        public static readonly string HikeDirectoryName = "hike";
        public static readonly string ValidVideoDirectoryPath = "C:\\Data\\Users\\Public\\Pictures\\";

        public static readonly int ATTACHMENT_THUMBNAIL_MAX_HEIGHT = 180;
        public static readonly int ATTACHMENT_THUMBNAIL_MAX_WIDTH = 180;
        public static readonly int ATTACHMENT_MAX_HEIGHT = 800;
        public static readonly int ATTACHMENT_MAX_WIDTH = 800;
        public static readonly int MAX_EMOTICON_SUPPORTED = 50;
        public static readonly int VIBRATE_DURATION = 200;
        public static readonly int MAX_AUDIO_RECORDTIME_SUPPORTED = 360; // 6 minutes
        public static readonly int LOCATION_THUMBNAIL_MAX_HEIGHT = 220;
        public static readonly int LOCATION_THUMBNAIL_MAX_WIDTH = 220;
        public static readonly int PROFILE_PICS_SIZE = 640; //image which are uploaded on servers
        public static readonly int MAX_THUMBNAILSIZE = 4800;

        public static readonly byte MAX_IMAGES_SHARE = 15;
        public static readonly int STATUS_INITIAL_FETCH_COUNT = 31;
        public static readonly int STATUS_SUBSEQUENT_FETCH_COUNT = 21;

        public static readonly int TYPING_NOTIFICATION_AUTOHIDE = 6; //seconds
        public static readonly int SEND_START_TYPING_TIMER = 4; //seconds
        public static readonly int MAX_CHATBUBBLE_SIZE = 1400;//chars
        public static readonly int STARTING_BASE_YEAR = 1600; //file time is ticks starting from jan 1 1601 so adding 1600 years

        //Chat bubbles height and width
        public static readonly int CHATBUBBLE_LANDSCAPE_WIDTH = 510;
        public static readonly int CHATBUBBLE_LANDSCAPE_MINWIDTH = 170;
        public static readonly int CHATBUBBLE_PORTRAIT_WIDTH = 330;
        public static readonly int CHATBUBBLE_PORTRAIT_MINWIDTH = 110;

        public static readonly string PUSH_CHANNEL_CN = "*.hike.in";
        

        public static readonly string MICROSOFT_MAP_SERVICE_APPLICATION_ID = "b4703e38-092f-4144-826a-3e3d41f50714";
        public static readonly string MICROSOFT_MAP_SERVICE_AUTHENTICATION_TOKEN = "CjsOsVAhJ0GPdjiP12KwvA";

        public static readonly string CAMERA_FRONT = "Front";
        public static readonly string CAMERA_BACK = "Back";

        public const string FTUE_TEAMHIKE_MSISDN = "+hike+";
        public const string FTUE_HIKEBOT_MSISDN = "+hike1+";
        public const string FTUE_GAMING_MSISDN = "+hike2+";
        public const string FTUE_HIKE_DAILY_MSISDN = "+hike3+";
        public const string FTUE_HIKE_SUPPORT_MSISDN = "+hike4+";

        public static readonly int HIDDEN_MODE_RESET_TIMER = 300;

        public static readonly string PINID = "pinId";
        public static readonly string READPIN = "readPin";
        public static readonly string UNREADPINS = "unreadpins";

        /* how often to ping the server */
        public static readonly short KEEP_ALIVE = 10 * 60; /* 10 minutes */
        public static readonly int SERVER_UNAVAILABLE_MAX_CONNECT_TIME = 9; /* 9 minutes */

        public static char[] DELIMITERS = new char[] { ':' };

        public static readonly string INDIA_COUNTRY_CODE = "+91";

        public static readonly string MOOD_TOD_SEPARATOR = ":";

        public static readonly string MY_PROFILE_PIC = "my_profile_pic";

        public static readonly string UNREAD_UPDATES = "urUp";
        public static readonly string UNREAD_FRIEND_REQUESTS = "urFr";
        public static readonly string REFRESH_BAR = "refBar";

        

        public static readonly string AVATAR = "avatar";
        public static readonly string STICKER_ID = "stId";
        public static readonly string CATEGORY_ID = "catId";

        public static readonly string VIDEO_SIZE = "videoSize";
        public static readonly string VIDEO_THUMBNAIL = "videoThumbnail";
        public static readonly string MAX_VIDEO_PLAYING_TIME = "maxPlayingTime";
        public static readonly string IS_PRIMARY_CAM = "isPrimaryCam";
        public static readonly string VIDEO_RESOLUTION = "videoResolution";
        public static readonly string VIDEO_FRAME_BYTES = "videoFrameBytes";

        public static readonly int MAX_GROUP_MEMBER_SIZE = 100;
        

        #endregion

        public static class ToastConstants
        {
            public static readonly string TOAST_FOR_HIDDEN_MODE = "You have a new notification";
            public static readonly string TOAST_FOR_MESSAGE = "Sent you a message";
            public static readonly string TOAST_FOR_STICKER = "Sent you a sticker";
            public static readonly string TOAST_FOR_PHOTO = "Sent you a photo";
            public static readonly string TOAST_FOR_AUDIO = "Sent you an audio";
            public static readonly string TOAST_FOR_VIDEO = "Sent you a video";
            public static readonly string TOAST_FOR_CONTACT = "Sent you a contact";
            public static readonly string TOAST_FOR_LOCATION = "Sent you a location";
            public static readonly string TOAST_FOR_FILE = "Sent you a file";
            public static readonly string TOAST_FOR_PIN = "Has posted a pin";
        }

        public static class Extras
        {
            public static readonly string ANIMATED_ONCE = "animatedOnce";
            public static readonly string SEND_BOT = "sendbot";
        }

        public static class NokiaHere
        {
            public static readonly string SEARCH = "search";
            public static readonly string CONTEXT = "context";
            public static readonly string LOCATION = "location";
            public static readonly string ADDRESS = "address";
            public static readonly string TEXT = "text";
            public static readonly string RESULTS = "results";
            public static readonly string ITEMS = "items";
            public static readonly string PLACE_TYPE = "urn:nlp-types:place";
            public static readonly string POSITION = "position";
            public static readonly string CGEN_GPS = "gps";
            public static readonly string CGEN_MAP = "map";
        }

        public static class DBStrings
        {
            public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
            public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
            public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        }

        public static class NavigationKeys
        {
            public static readonly string GROUP_NAME = "groupName";
            public static readonly string HAS_CUSTOM_IMAGE = "hasCustomImage";
            public static readonly string NEW_GROUP_ID = "newGroupId";

            public static readonly string START_NEW_GROUP = "start_new_group";
            public static readonly string EXISTING_GROUP_MEMBERS = "existing_group_members";
            public static readonly string IS_EXISTING_GROUP = "is_existing_group";
            public static readonly string GROUP_ID_FROM_CHATTHREAD = "groupIdFromChatThreadPage";
            public static readonly string GROUP_NAME_FROM_CHATTHREAD = "groupNameFromChatThreadPage";
            public static readonly string GROUP_CHAT = "groupChat";
            public static readonly string SHARE_CONTACT = "shareContact";
            public static readonly string COUNTRY_SELECTED = "country_selected";

            public static readonly string IMAGE_TO_DISPLAY = "imageToDisplay";
            public static readonly string STATUS_IMAGE_TO_DISPLAY = "statusToDisplay";
            public static readonly string FROM_SOCIAL_PAGE = "fromSocialPage";
            public static readonly string SOCIAL_STATE = "socialState";
            public static readonly string SOCIAL = "Social_Request";

            public static readonly string PLAYER_TIMER = "playerTimer";
            public static readonly string MULTIPLE_IMAGES = "multipleimages";
            public static readonly string LOCATION_MAP_COORDINATE = "locationMapCoordinate";
            public static readonly string SET_PROFILE_PIC = "setProfilePic";

            public static readonly string OBJ_FROM_SELECTUSER_PAGE = "objFromSelectUserPage";
            public static readonly string OBJ_FROM_CONVERSATIONS_PAGE = "objFromConversationPage";
            public static readonly string OBJ_FROM_STATUSPAGE = "objFromStatusPage";

            /// <summary>
            /// Use key whenever relaunching chat thread from chat thread page to clear back stack
            /// </summary>
            public static readonly string IS_CHAT_RELAUNCH = "isChatRelaunch";
            public static readonly string FORWARD_MSG = "forwardedText";
            public static readonly string AUDIO_RECORDED_DURATION = "audioRecordedDuration";
            public static readonly string AUDIO_RECORDED = "audioRecorded";
            public static readonly string CONTACT_SELECTED = "contactSelected";
            public static readonly string VIDEO_RECORDED = "videoRecorded";
            public static readonly string VIDEO_SHARED = "videoShared";
            public static readonly string SHARED_LOCATION = "sharedLocation";

            public static readonly string OBJ_FROM_BLOCKED_LIST = "objFrmBlckList";
            public static readonly string PAGE_TO_NAVIGATE_TO = "pageToNavigateTo";
            public static readonly string VIEW_MORE_MESSAGE_OBJ = "viewMoreMsg";
            public static readonly string USERINFO_FROM_CONVERSATION_PAGE = "userInfoFromConvPage";
            public static readonly string USERINFO_FROM_CHATTHREAD_PAGE = "userInfoFromChatThread";
            public static readonly string USERINFO_FROM_GROUPCHAT_PAGE = "userInfoFromGroupChatThread";
            public static readonly string USERINFO_FROM_PROFILE = "userInfoFromProfile";
            public static readonly string USERINFO_FROM_TIMELINE = "usrInfoFromTimeLine";

            public static readonly string GO_TO_CONV_VIEW = "goToConvView";
            public static readonly string LAUNCH_FROM_PUSH_MSISDN = "launchFromPushMsisdn";
            public static readonly string GC_PIN = "pin";
            public static readonly string PROFILE_NAME_CHANGED = "ProfileNameChanged";
            public static readonly string IS_PIC_DOWNLOADED = "isPicDownloaded";

            public static readonly string LOCATION_SEARCH = "locationSearch";
            public static readonly string LOCATION_SELECTED_INDEX = "locationSelectedPlace";
            public static readonly string LOCATION_PLACE_SEARCH_RESULT = "locationPlaceSearchResult";

            public static readonly string VIDEO_SIZE = "videoSize";
            public static readonly string VIDEO_THUMBNAIL = "videoThumbnail";
            public static readonly string MAX_VIDEO_PLAYING_TIME = "maxPlayingTime";
            public static readonly string IS_PRIMARY_CAM = "isPrimaryCam";
            public static readonly string VIDEO_RESOLUTION = "videoResolution";
            public static readonly string VIDEO_FRAME_BYTES = "videoFrameBytes";
        }

        public static class ServerTips
        {
            public static readonly string CHAT_SCREEN_TIP_ID = "chtScrTipId";
            public static readonly string CONV_PAGE_TIP_ID = "cnvPgTipId";

            public const string STICKER_TIPS = "stk";
            public const string PROFILE_TIPS = "pp";
            public const string ATTACHMENT_TIPS = "ft";
            public const string INFORMATIONAL_TIPS = "info";
            public const string FAVOURITE_TIPS = "fav";
            public const string THEME_TIPS = "theme";
            public const string INVITATION_TIPS = "inv";
            public const string STATUS_UPDATE_TIPS = "stts";


        }
        
        public static class EmailConversation
        {
            public static readonly int EMAIL_LIMIT = 60 * 1024; //60 KB
            public static readonly int CHAT_FETCH_LIMIT = 500; //how many chats to be fetched from DB to avoid multiple DB call
            public static readonly string CONV_MSG_DISP_FMT = "{0}- {1}: {2}"; //datetime- name: msg (format to display conversation)
            public static readonly string SYS_MSG_DISP_FMT = "{0}- {1}"; //datetime- msg (format to display system msgs like changed chat theme etc)
        }
    }
}
