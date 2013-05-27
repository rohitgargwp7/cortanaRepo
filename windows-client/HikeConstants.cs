using System;
using windows_client.utils;

namespace windows_client
{
    public class HikeConstants
    {
        public static readonly string MESSAGE = "msg";
        public static readonly string UI_TOPIC = "/u";
        public static readonly string APP_TOPIC = "/a";
        public static readonly string SERVICE_TOPIC = "/s";
        public static readonly string PUBLISH_TOPIC = "/p";

        public static readonly string TYPE = "t";
        public static readonly string DATA = "d";
        public static readonly string TO = "to";
        public static readonly string FROM = "f";

        public static readonly string HIKE_MESSAGE = "hm";
        public static readonly string SMS_MESSAGE = "sm";
        public static readonly string TIMESTAMP = "ts";
        public static readonly string MESSAGE_ID = "i";
        public static readonly string METADATA = "md";
        public static readonly string METADATA_DND = "dnd";
        public static readonly string ANALYTICS_EVENT = "ae";
        public static readonly string LOG_EVENT = "le";//for analytics
        public static readonly string FILE_SYSTEM_VERSION = "File_System_Version";

        public static readonly string CRITICAL = "critical";
        public static readonly string LATEST = "latest";
        public static readonly string APP_ID = "appID";


        public static readonly string SOUND_PREF = "soundPref";
        public static readonly string VIBRATE_PREF = "vibratePref";
        public static readonly string HIKEBOT = "TD-HIKE";

        public static readonly string DONE = "Done";
        public static readonly string PIN_ERROR = "PinError";
        public static readonly string ADDRESS_BOOK_ERROR = "AddressBookError";
        public static readonly string CHANGE_NUMBER = "ChangeNumber";

        public static readonly string FILE_NAME = "fn";
        public static readonly string FILE_RESPONSE_DATA = "data";
        public static readonly string FILE_KEY = "fk";
        public static readonly string FILE_THUMBNAIL = "tn";
        public static readonly string FILE_CONTENT_TYPE = "ct";
        public static readonly string FILES_DATA = "files";

        public static readonly string LATITUDE = "lat";
        public static readonly string LONGITUDE = "long";
        public static readonly string ZOOM_LEVEL = "zoom";
        public static readonly string LOCATION_ADDRESS = "add";

        public static readonly string FILE_TRANSFER_LOCATION = "TransferredFiles";
        public static readonly string FILES_BYTE_LOCATION = FILE_TRANSFER_LOCATION + "/FileBytes";
        public static readonly string FILES_THUMBNAILS = FILE_TRANSFER_LOCATION + "/Thumbnails";
        public static readonly string FILES_ATTACHMENT = FILE_TRANSFER_LOCATION + "/Attachments";
        public static readonly string TEMP_VIDEO_RECORDED = FILE_TRANSFER_LOCATION + "/TempVideo";
        public static readonly string TEMP_VIDEO_NAME = "CameraMovie.mp4";


        public static readonly string SHARED_FILE_LOCATION = "/shared/transfers";
        public static readonly string FILE_TRANSFER_BASE_URL = AccountUtils.FILE_TRANSFER_BASE + "/user/ft";
        public static readonly string FILE_TRANSFER_COPY_BASE_URL = "http://hike.in/f";

        public static readonly string pushNotificationChannelName = "HikeApp";

        public static readonly int ATTACHMENT_THUMBNAIL_MAX_HEIGHT = 180;
        public static readonly int ATTACHMENT_THUMBNAIL_MAX_WIDTH = 180;
        public static readonly int ATTACHMENT_MAX_HEIGHT = 800;
        public static readonly int ATTACHMENT_MAX_WIDTH = 800;
        public static readonly int MAX_EMOTICON_SUPPORTED = 20;
        public static readonly int VIBRATE_DURATION = 700;
        public static readonly int MAX_AUDIO_RECORDTIME_SUPPORTED = 360; // 6 minutes
        public static readonly int RECURSIVE_PING_INTERVAL = 270;//seconds
        public static readonly int LOCATION_THUMBNAIL_MAX_HEIGHT = 220;
        public static readonly int LOCATION_THUMBNAIL_MAX_WIDTH = 220;
        public static readonly int PROFILE_PICS_SIZE = 640; //image which are uploaded on servers
        public static readonly int MAX_THUMBNAILSIZE = 4800;

        public static readonly int CHECK_FOR_UPDATE_TIME = 48;//hours on prod and minuts on staging

        public static readonly int TYPING_NOTIFICATION_AUTOHIDE = 20; //seconds
        public static readonly int MAX_CHATBUBBLE_SIZE = 1400;//chars

        public static readonly int ANALYTICS_POST_TIME = 12;//hours on prod and minutes on staging
        public static readonly string ANALYTICS_OBJECT_FILE = "eventsFile";
        public static readonly string ANALYTICS_OBJECT_DIRECTORY = "analytics";

        //Chat bubbles
        public static readonly int CHATBUBBLE_LANDSCAPE_WIDTH = 510;
        public static readonly int CHATBUBBLE_PORTRAIT_WIDTH = 330;

        //file for sharing info with background agent
        public static readonly string BACKGROUND_AGENT_FILE = "token";
        public static readonly string BACKGROUND_AGENT_DIRECTORY = "ba";

        private static readonly string TERMS_AND_CONDITIONS_WHITE = "http://hike.in/terms/wp7";
        private static readonly string FAQS_LINK_WHITE = "http://hike.in/help/wp7/";
        private static readonly string TERMS_AND_CONDITIONS_BLACK = "http://hike.in/terms/wp7/black.html";
        private static readonly string FAQS_LINK_BLACK = "http://get.hike.in/help/wp7/black.html";
        //private static readonly string CONTACT_US_LINK = "http://support.hike.in";
        public static readonly string UPDATE_URL = AccountUtils.IsProd ? "http://get.hike.in/updates/wp8" : "http://staging.im.hike.in:8080/updates/wp8";
        public static readonly string SYSTEM_HEALTH_LINK = "http://twitter.com/hikestatus/";

        //for device info
        public static readonly string DEVICE_TYPE = "devicetype";//The OS
        public static readonly string DEVICE_ID = "deviceid";//A unique ID of the device
        public static readonly string DEVICE_TOKEN = "devicetoken";// A unique ID of the device 
        public static readonly string DEVICE_VERSION = "deviceversion";//The current OS version
        public static readonly string APP_VERSION = "app_version";//The app version
        public static readonly string APPVERSION = "appversion";//The app version
        public static readonly string INVITE_TOKEN_KEY = "invite_token";//The referral token
        public static readonly string PUSH_CHANNEL_CN = "*.hike.in";//The PUSH CN
        public static readonly string FAVORITES = "favorites";
        public static readonly string PENDING = "pending";
        public static readonly string REQUEST_PENDING = "requestpending";
        public static readonly string FULL_VIEW_IMAGE_PREFIX = "_fullView";


        //CS prefix for contactsharing
        public static readonly string CS_PHONE_NUMBERS = "phone_numbers";
        public static readonly string CS_NAME = "name";
        public static readonly string CS_EMAILS = "emails";
        public static readonly string CS_ADDRESSES = "addresses";
        public static readonly string CS_HOME_KEY = "Home";
        public static readonly string CS_WORK_KEY = "Work";
        public static readonly string CS_MOBILE_KEY = "Mobile";
        public static readonly string CS_OTHERS_KEY = "Others";

        // keys for application info in analytics, update and signup
        public static readonly string OS_NAME = "_os";
        public static readonly string OS_VERSION = "_os_version";
        public static readonly string DEVICE_TYPE_KEY = "dev_type";


        public static string FAQS_LINK
        {
            get
            {
                if (Utils.isDarkTheme())
                {
                    return FAQS_LINK_BLACK;
                }
                return FAQS_LINK_WHITE;
            }
        }

        public static string TERMS_LINK
        {
            get
            {
                if (Utils.isDarkTheme())
                {
                    return TERMS_AND_CONDITIONS_BLACK;
                }
                return TERMS_AND_CONDITIONS_WHITE;
            }
        }

        /* how long to wait between sending publish and receiving an acknowledgement */

        public static readonly long MESSAGE_DELIVERY_TIMEOUT = 5 * 1000;

        /* how long to wait for a ping confirmation */
        public static readonly long PING_TIMEOUT = 5 * 1000;

        /* how long to wait to resend message. This should significantly greathr than PING_TIMEOUT */
        public static readonly long MESSAGE_RETRY_INTERVAL = 15 * 1000;

        /* quiet period of no changes before actually updating the db */
        public static readonly long CONTACT_UPDATE_TIMEOUT = 10 * 1000;

        /* how often to ping the server */
        public static readonly short KEEP_ALIVE = 10 * 60; /* 10 minutes */

        /* how often to ping after a failure */
        public static readonly int RECONNECT_TIME = 10; /* 10 seconds */

        public static readonly int HIKE_SYSTEM_NOTIFICATION = 0;
        public static readonly string ADAPTER_NAME = "hikeadapter";

        public static char[] DELIMITERS = new char[] { ':' };

        /* constants for defining what to do after checking for updates*/
        public static readonly int UPDATE_AVAILABLE = 2;
        public static readonly int CRITICAL_UPDATE = 1;
        public static readonly int NO_UPDATE = 0;
        public static readonly string ALL_INVITEE = "ai";
        public static readonly string ALL_INVITEE_JOINED = "aij";
        public static readonly string TOTAL_CREDITS_PER_MONTH = "tc";
        public static readonly string ACCOUNTS = "accounts";
        public static readonly string TWITTER = "twitter";
        public static readonly string FACEBOOK = "fb";
        public static readonly string TEXT_UPDATE_MSG = "msg";
        public static readonly string STATUS_ID = "statusid";
        public static readonly string PROFILE_UPDATE = "profile";
        public static readonly string THUMBNAIL = "tn";
        public static readonly string PROFILE_PIC_ID = "ppid";
        public static readonly string MOOD = "mood";
        public static readonly string TIME_OF_DAY = "timeofday";

        public static string MOOD_TOD_SEPARATOR = ":";
        public static string GROUP_PARTICIPANT_SEPARATOR = ",";
        public static string MSISDN = "msisdn";
        public static string MSISDNS = "msisdns";
        public static string NAME = "name";
        public static string NEW_USER = "nu";
        public static string DND_NUMBERS = "dndnumbers";
        public static string START_NEW_GROUP = "start_new_group";
        public static string EXISTING_GROUP_MEMBERS = "existing_group_members";
        public static string IS_EXISTING_GROUP = "is_existing_group";
        public static string GROUP_ID_FROM_CHATTHREAD = "groupIdFromChatThreadPage";
        public static string GROUP_NAME_FROM_CHATTHREAD = "groupNameFromChatThreadPage";
        public static string GROUP_CHAT = "groupChat";
        public static string SHARE_CONTACT = "shareContact";
        public static string IS_NEW_INSTALLATION = "is_new_installation";
        public static string MY_PROFILE_PIC = "my_profile_pic";
        public static string COUNTRY_SELECTED = "country_selected";
        public static string IMAGE_TO_DISPLAY = "imageToDisplay";
        public static string STATUS_IMAGE_TO_DISPLAY = "statusToDisplay";
        public static string FROM_SOCIAL_PAGE = "fromSocialPage";
        public static string SOCIAL_STATE = "socialState";
        public static string SOCIAL = "Social_Request";
        public static string SHOW_GROUP_CHAT_OVERLAY = "sgcol";

        /* NAVIGATION CONSTANTS*/
        public static string OBJ_FROM_SELECTUSER_PAGE = "objFromSelectUserPage";
        public static string OBJ_FROM_CONVERSATIONS_PAGE = "objFromConversationPage";
        public static string OBJ_FROM_STATUSPAGE = "objFromStatusPage";
        public static string FORWARD_MSG = "forwardedText";
        public static string AUDIO_RECORDED = "audioRecorded";
        public static string CONTACT_SELECTED = "contactSelected";
        public static string VIDEO_RECORDED = "videoRecorded";
        public static string SHARED_LOCATION = "sharedLocation";
        public static string DND = "dnd";
        public static string INVITE_TOKEN = "invite_token";
        public static string FB_LOGGED_IN = "FbLoggedIn";
        public static string TW_LOGGED_IN = "TwLoggedIn";
        public static string ACCOUNT = "account";
        public static string OBJ_FROM_BLOCKED_LIST = "objFrmBlckList";
        public static readonly string PAGE_TO_NAVIGATE_TO = "pageToNavigateTo";

        public static string USERINFO_FROM_CHATTHREAD_PAGE = "userInfoFromChatThread";
        public static string USERINFO_FROM_GROUPCHAT_PAGE = "userInfoFromGroupChatThread";
        public static string USERINFO_FROM_PROFILE = "userInfoFromProfile";
        public static string USERINFO_FROM_TIMELINE = "usrInfoFromTimeLine";
        public static string UNREAD_UPDATES = "urUp";
        public static string UNREAD_FRIEND_REQUESTS = "urFr";
        public static string REFRESH_BAR = "refBar";

        public static string CLOSE_FRIENDS_NUX = "closeFriends";

        public static string PHONE_ADDRESS_BOOK = "phoneAddressBook";
        public static string PROFILE_NAME_CHANGED = "ProfileNameChanged";

        /* FILE BASED CONSTANTS*/
        public static readonly string LOCATION = "location";
        public static readonly string VIDEO = "video";
        public static readonly string AUDIO = "audio";
        public static readonly string IMAGE = "image";
        public static readonly string CONTACT = "contact";
        public static readonly string CT_CONTACT = "contact/share";
        public static readonly string POKE = "poke";

        public static readonly string OK = "ok";
        public static readonly string STAT = "stat";
        public static readonly string FAIL = "fail";
        public static readonly string REWARDS_TOKEN = "reward_token";
        public static readonly string SHOW_REWARDS = "show_rewards";
        public static readonly string REWARDS_VALUE = "tt";

        public static readonly string LOCALE = "locale";
        public static readonly string STAGING_SERVER = "stagingServer";

        public static readonly string ENABLE_PUSH_BATCH_SU = "enablepushbatchingforsu";
        public static readonly string PUSH_SU = "pushsu";
        public static readonly string STICKER_ID = "stickerid";

        public static class Extras
        {
            public static readonly string ANIMATED_ONCE = "animatedOnce";
            public static readonly string MSISDN = "msisdn";
            public static readonly string ID = "id";
            public static readonly string NAME = "name";
            public static readonly string INVITE = "invite";
            public static readonly string MSG = "msg";
            public static readonly string PREF = "pref";
            public static readonly string EDIT = "edit";
            public static readonly string IMAGE_PATH = "image-path";
            public static readonly string SCALE = "scale";
            public static readonly string OUTPUT_X = "outputX";
            public static readonly string OUTPUT_Y = "outputY";
            public static readonly string ASPECT_X = "aspectX";
            public static readonly string ASPECT_Y = "aspectY";
            public static readonly string DATA = "data";
            public static readonly string RETURN_DATA = "return-data";
            public static readonly string BITMAP = "bitmap";
            public static readonly string CIRCLE_CROP = "circleCrop";
            public static readonly string SCALE_UP = "scaleUpIfNeeded";
            public static readonly string UPDATE_AVAILABLE = "updateAvailable";
            public static readonly string KEEP_MESSAGE = "keepMessage";
        }

        public static class MqttMessageTypes
        {
            public static readonly string BLOCK_INTERNATIONAL_USER = "bis";
            public static readonly string GROUP_CHAT_JOIN = "gcj";
            public static readonly string GROUP_CHAT_JOIN_NEW = "gcj_new";
            public static readonly string GROUP_CHAT_LEAVE = "gcl";
            public static readonly string GROUP_CHAT_END = "gce";
            public static readonly string GROUP_CHAT_NAME = "gcn";
            public static readonly string REQUEST_ACCOUNT_INFO = "rai";
            public static readonly string DND_USER_IN_GROUP = "dugc";

            public static readonly string ACCOUNT_INFO = "ai";
            public static readonly string ACCOUNT_CONFIG = "ac";
            public static readonly string GROUP_USER_JOINED_OR_WAITING = "gujow";
            public static readonly string USER_OPT_IN = "uo";
            public static readonly string USER_JOIN = "uj";
            public static readonly string SMS_USER = "sms_user";
            public static readonly string HIKE_USER = "hike_user";
            public static readonly string ADD_FAVOURITE = "af";
            public static readonly string REMOVE_FAVOURITE = "rf";
            public static readonly string POSTPONE_FRIEND_REQUEST = "pf";
            public static readonly string REWARDS = "rewards";
            public static readonly string STATUS_UPDATE = "su";
            public static readonly string DELETE_STATUS_UPDATE = "dsu";
            public static readonly string GROUP_DISPLAY_PIC = "dp";

        }

        public static class AppSettings
        {
            public static readonly string PAGE_STATE = "page_State";
            public static readonly string FB_USER_ID = "FbUserId";
            public static readonly string FB_ACCESS_TOKEN = "FbAccessToken";
            public static readonly string TWITTER_TOKEN = "TwToken";
            public static readonly string TWITTER_TOKEN_SECRET = "TwTokenSecret";
            public static readonly string CONTACTS_TO_SHOW = "ContactsToShow";
            public static readonly string NEW_UPDATE = "New_Update";
            public static readonly string APP_LAUNCH_COUNT = "App_Launch_Count";
        }
    }
}
