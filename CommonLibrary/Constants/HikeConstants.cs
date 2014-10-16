using System;

namespace CommonLibrary.Constants
{
    public class HikeConstants
    {
        #region All Uncategorized constants
        public static readonly string PushNotificationChannelName = "HikeApp";

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

        public static readonly int STARTING_BASE_YEAR = 1600; //file time is ticks starting from jan 1 1601 so adding 1600 years

        public static readonly string CAMERA_FRONT = "Front";
        public static readonly string CAMERA_BACK = "Back";

        public const string FTUE_TEAMHIKE_MSISDN = "+hike+";
        public const string FTUE_HIKEBOT_MSISDN = "+hike1+";
        public const string FTUE_GAMING_MSISDN = "+hike2+";
        public const string FTUE_HIKE_DAILY_MSISDN = "+hike3+";
        public const string FTUE_HIKE_SUPPORT_MSISDN = "+hike4+";

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
        public static readonly int BackgroundExecutionTime = 45000;

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
            public static readonly string TOAST_FOR_STATUS = "Posted a status";
        }

        public static class Extras
        {
            public static readonly string ANIMATED_ONCE = "animatedOnce";
            public static readonly string SEND_BOT = "sendbot";
        }

        public static class DBStrings
        {
            public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
            public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
            public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
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
    }
}
