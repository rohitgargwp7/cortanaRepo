﻿using System;
using windows_client.utils;

namespace windows_client
{
    public class    HikeConstants
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
        public static readonly string SUB_TYPE = "st";
        public static readonly string SUBTYPE_NEW_JOIN = "nu";
        public static readonly string SUBTYPE_REJOIN = "ru";
        public static readonly string TAG = "tag";
        public static readonly string UPGRADE = "upgrade";

        public static readonly string ADD_STICKER = "addStk";
        public static readonly string ADD_CATEGORY = "addCat";
        public static readonly string REMOVE_STICKER = "remStk";
        public static readonly string REMOVE_CATEGORY = "remCat";

        public static readonly string HIKE_MESSAGE = "hm";
        public static readonly string SMS_MESSAGE = "sm";
        public static readonly string TIMESTAMP = "ts";
        public static readonly string MESSAGE_ID = "i";
        public static readonly string INVITE_LIST = "list";
        public static readonly string METADATA = "md";
        public static readonly string METADATA_DND = "dnd";
        public static readonly string ANALYTICS_EVENT = "ae";
        public static readonly string LOG_EVENT = "le";//for analytics
        public static readonly string FILE_SYSTEM_VERSION = "File_System_Version";
        public static readonly string REQUEST_SERVER_TIME = "rsts";
        public static readonly string STATUS = "st";

        public static readonly string PUSH = "push";
        public static readonly string CRITICAL = "critical";
        public static readonly string LATEST = "latest";
        public static readonly string APP_ID = "appID";

        public static readonly string LAST_NOTIFICATION_TIME = "lastNotTime";
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
        public static readonly string FILE_SIZE = "fs";
        public static readonly string FILE_THUMBNAIL = "tn";
        public static readonly string FILE_CONTENT_TYPE = "ct";
        public static readonly string FILES_DATA = "files";
        public static readonly string FILE_PLAY_TIME = "pt";
        public static readonly string MD5_ORIGINAL = "md5_original";

        public static readonly string LATITUDE = "lat";
        public static readonly string LONGITUDE = "long";
        public static readonly string ZOOM_LEVEL = "zoom";
        public static readonly string LOCATION_ADDRESS = "add";
        public static readonly string LOCATION_TITLE = "title";
        public static readonly string LOCATION_FILENAME = "Location";
        public static readonly string LOCATION_CONTENT_TYPE = "hikemap/location";

        public static readonly string FILE_TRANSFER_LOCATION = "TransferredFiles";
        public static readonly string FILE_TRANSFER_TEMP_LOCATION = "TempTransferredFiles";
        public static readonly string FILES_BYTE_LOCATION = FILE_TRANSFER_LOCATION + "/FileBytes";
        public static readonly string FILES_THUMBNAILS = FILE_TRANSFER_LOCATION + "/Thumbnails";
        public static readonly string FILES_ATTACHMENT = FILE_TRANSFER_LOCATION + "/Attachments";
        public static readonly string TEMP_VIDEO_RECORDED = FILE_TRANSFER_LOCATION + "/TempVideo";
        public static readonly string TEMP_VIDEO_NAME = "CameraMovie.mp4";

        public static readonly string SHARED_FILE_LOCATION = "/shared/transfers";
        public static readonly string FILE_TRANSFER_BASE_URL = AccountUtils.FILE_TRANSFER_BASE + "/user/ft";
        public static readonly string FILE_TRANSFER_COPY_BASE_URL = "http://hike.in/f";
        public static readonly string PARTIAL_FILE_TRANSFER_BASE_URL = AccountUtils.FILE_TRANSFER_BASE + "/user/pft/";

        public static readonly string pushNotificationChannelName = "HikeApp";

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
       
        public const byte MAX_IMAGES_SHARE = 15;
        public const int STATUS_INITIAL_FETCH_COUNT = 31;
        public const int STATUS_SUBSEQUENT_FETCH_COUNT = 21; 

        public static readonly int CHECK_FOR_UPDATE_TIME = 48;//hours on prod and minuts on staging

        public static readonly int TYPING_NOTIFICATION_AUTOHIDE = 6; //seconds
        public static readonly int SEND_START_TYPING_TIMER = 4; //seconds
        public static readonly int MAX_CHATBUBBLE_SIZE = 1400;//chars

        public static readonly int ANALYTICS_POST_TIME = 12;//hours on prod and minutes on staging
        public static readonly string ANALYTICS_OBJECT_FILE = "eventsFile";
        public static readonly string ANALYTICS_OBJECT_DIRECTORY = "analytics";

        //Chat bubbles
        public static readonly int CHATBUBBLE_LANDSCAPE_WIDTH = 510;
        public static readonly int CHATBUBBLE_LANDSCAPE_MINWIDTH = 170;
        public static readonly int CHATBUBBLE_PORTRAIT_WIDTH = 330;
        public static readonly int CHATBUBBLE_PORTRAIT_MINWIDTH = 110;

        //file for sharing info with background agent
        public static readonly string BACKGROUND_AGENT_FILE = "token";
        public static readonly string BACKGROUND_AGENT_DIRECTORY = "ba";

        private static readonly string TERMS_AND_CONDITIONS_WHITE = "http://hike.in/terms/wp8";
        private static readonly string FAQS_LINK_WHITE = "http://get.hike.in/help/wp8/index.html";
        //private static readonly string CONTACT_US_LINK = "http://support.hike.in";
        public static readonly string UPDATE_URL = AccountUtils.IsProd ? "http://get.hike.in/updates/wp8" : "http://staging.im.hike.in:8080/updates/wp8";
        public static readonly string SYSTEM_HEALTH_LINK = "http://twitter.com/hikestatus/";
        public static readonly string STICKER_URL = AccountUtils.IsProd ? "http://hike.in/s/" : "http://staging.im.hike.in/s/";
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

        public static readonly string NO_SMS = "nosms";
        public static readonly string ST_UI_EVENT = "uiEvent";
        public static readonly string ST_CONFIG_EVENT = "config";
        public static readonly string ST_NETWORK_EVENT = "nw";

        public static readonly string COUNT = "c";
        public static readonly string FORCE_SMS_MESSAGE = "m";

        public static readonly string MICROSOFT_MAP_SERVICE_APPLICATION_ID = "b4703e38-092f-4144-826a-3e3d41f50714";
        public static readonly string MICROSOFT_MAP_SERVICE_AUTHENTICATION_TOKEN = "CjsOsVAhJ0GPdjiP12KwvA";

        public static readonly string CAMERA_FRONT = "Front";
        public static readonly string CAMERA_BACK = "Back";

        public const string FTUE_TEAMHIKE_MSISDN = "+hike+";
        public const string FTUE_HIKEBOT_MSISDN = "+hike1+";
        public const string FTUE_GAMING_MSISDN = "+hike2+";
        public const string FTUE_HIKE_DAILY_MSISDN = "+hike3+";
        public const string FTUE_HIKE_SUPPORT_MSISDN = "+hike4+";

        public static readonly string VERSION = "version";
        public static readonly string BLACK_THEME = "black_theme";

        public static string FAQS_LINK
        {
            get
            {
                return FAQS_LINK_WHITE;
            }
        }

        public static string TERMS_LINK
        {
            get
            {
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
        public static readonly string INDIA_COUNTRY_CODE = "+91";

        public static readonly string NO_TOKEN = "notoken";
        public static readonly string INVALID_TOKEN = "invalidtoken";
        public static readonly string FAILURE = "failure";
        public static readonly string SUCCESS = "success";

        public static readonly string PRO_TIP_ID = "i";
        public static readonly string PRO_TIP_HEADER = "h";
        public static readonly string PRO_TIP_TEXT = "t";
        public static readonly string PRO_TIP_IMAGE = "img";
        public static readonly string PRO_TIP_TIME = "wt";
        public static readonly Int64 DEFAULT_PRO_TIP_TIME = 300;

        public static readonly string BACKGROUND_ID = "bg_id";
        public static readonly string HAS_CUSTOM_BACKGROUND = "custom";

        public static string MOOD_TOD_SEPARATOR = ":";
        public static string GROUP_PARTICIPANT_SEPARATOR = ",";
        public static string REQUEST_DISPLAY_PIC = "rdp";
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
        public static string PLAYER_TIMER = "playerTimer";
        public static string MULTIPLE_IMAGES = "multipleimages";
        public static string LOCATION_MAP_COORDINATE = "locationMapCoordinate";
        public static string LOCATION_DEVICE_COORDINATE = "locationDeviceCoordinate";
        public static string LOCATION_SEARCH = "locationSearch";
        public static string LOCATION_ZOOM_LEVEL = "locationZoomLevel";
        public static string LOCATION_SELECTED_INDEX = "locationSelectedPlace";
        public static string LOCATION_PLACE_SEARCH_RESULT = "locationPlaceSearchResult";
        public static string SET_PROFILE_PIC = "setProfilePic";

        /* NAVIGATION CONSTANTS*/
        public static string OBJ_FROM_SELECTUSER_PAGE = "objFromSelectUserPage";
        public static string OBJ_FROM_CONVERSATIONS_PAGE = "objFromConversationPage";
        public static string OBJ_FROM_STATUSPAGE = "objFromStatusPage";
        public static string FORWARD_MSG = "forwardedText";
        public static string AUDIO_RECORDED_DURATION = "audioRecordedDuration";
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
        public static readonly string LAUNCH_FROM_UPGRADEPAGE = "launchFromUpgradePage";
        public static readonly string VIEW_MORE_MESSAGE_OBJ = "viewMoreMsg";
        public static string USERINFO_FROM_CONVERSATION_PAGE = "userInfoFromConvPage";
        public static string USERINFO_FROM_CHATTHREAD_PAGE = "userInfoFromChatThread";
        public static string USERINFO_FROM_GROUPCHAT_PAGE = "userInfoFromGroupChatThread";
        public static string USERINFO_FROM_PROFILE = "userInfoFromProfile";
        public static string USERINFO_FROM_TIMELINE = "usrInfoFromTimeLine";
        public static string UNREAD_UPDATES = "urUp";
        public static string UNREAD_FRIEND_REQUESTS = "urFr";
        public static string REFRESH_BAR = "refBar";
        public static string CHAT_BACKGROUND_ARRAY = "cbgs";
        public static string CHAT_FTUE = "cftue";
        public static string SHOW_CHAT_FTUE = "showcftue";
        public static string GO_TO_CONV_VIEW = "goToConvView";

        public static readonly string LAUNCH_FROM_PUSH_MSISDN = "launchFromPushMsisdn";

        public static string PHONE_ADDRESS_BOOK = "phoneAddressBook";
        public static string PROFILE_NAME_CHANGED = "ProfileNameChanged";

        public static string IS_PIC_DOWNLOADED = "isPicDownloaded";

        /* FILE BASED CONSTANTS*/
        public static readonly string LOCATION = "location";
        public static readonly string VIDEO = "video";
        public static readonly string AUDIO = "audio";
        public static readonly string IMAGE = "image";
        public static readonly string CONTACT = "contact";
        public static readonly string UNKNOWN_FILE = "file";
        public static readonly string CT_CONTACT = "contact/share";
        public static readonly string POKE = "poke";

        public static readonly string OK = "ok";
        public static readonly string STAT = "stat";
        public static readonly string FAIL = "fail";
        public static readonly string REWARDS_TOKEN = "reward_token";
        public static readonly string IP_KEY = "ip";
        public static readonly string SHOW_REWARDS = "show_rewards";
        public static readonly string REWARDS_VALUE = "tt";

        public static readonly string LOCALE = "locale";
        public static readonly string STAGING_SERVER = "stagingServer";

        public static readonly string ENABLE_PUSH_BATCH_SU = "enablepushbatchingforsu";
        public static readonly string PUSH_SU = "pushsu";
        public static readonly string PUSH_CBG = "pushcbg";
        public static readonly string STICKER_ID = "stId";
        public static readonly string CATEGORY_ID = "catId";

        public static readonly string ICON = "icon";
        public static readonly string LASTSEEN = "ls";
        public static readonly string LASTSEENONOFF = "lastseen";
        public static readonly string JUSTOPENED = "justOpened";

        public static readonly string VIDEO_SIZE = "videoSize";
        public static readonly string VIDEO_THUMBNAIL = "videoThumbnail";
        public static readonly string MAX_VIDEO_PLAYING_TIME = "maxPlayingTime";
        public static readonly string IS_PRIMARY_CAM = "isPrimaryCam";
        public static readonly string VIDEO_RESOLUTION = "videoResolution";
        public static readonly string VIDEO_FRAME_BYTES = "videoFrameBytes";

        public static readonly int FILE_MAX_SIZE = 26214400;//in bytes
        public static readonly int APP_MIN_FREE_SIZE = 20971520;
        public static readonly int MAX_GROUP_MEMBER_SIZE = 100;

        public static readonly string FREE_INVITE_POPUP_TITLE = "free_invite_popup_title";
        public static readonly string FREE_INVITE_POPUP_TEXT = "free_invite_popup_text";
        public static readonly string SHOW_FREE_INVITES = "show_free_invites";
        public static readonly string INVITE_POPUP_UNIQUEID = "invite_popup_uniqueid";
        public static readonly string SHOW_POPUP = "show_popup";

        #region ANALYTICS EVENTS KEYS

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
        public static readonly string TAG_MOBILE = "wp8";

        public static readonly string PRO_TIPS_DISMISSED = "tip_id";
        public static readonly string ENTER_TO_SEND = "entr_2_snd";
        public static readonly string FWD_TO_MULTIPLE = "fwdToMul";
        public static readonly string NULL_PUSH_TOKEN = "npt";
        public static readonly string EXCEPTION_PUSH_TOKEN = "expt";

        #endregion

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
            public static readonly string SEND_BOT = "sendbot";
        }

        public static class MqttMessageTypes
        {
            public static readonly string BLOCK_INTERNATIONAL_USER = "bis";
            public static readonly string GROUP_CHAT_JOIN = "gcj";
            public static readonly string GROUP_CHAT_JOIN_NEW = "gcj_new";
            public static readonly string GROUP_CHAT_LEAVE = "gcl";
            public static readonly string GROUP_CHAT_END = "gce";
            public static readonly string GROUP_CHAT_NAME = "gcn";
            public static readonly string GROUP_OWNER_CHANGED = "goc";
            public static readonly string DND_USER_IN_GROUP = "dugc";

            public static readonly string ACCOUNT_INFO = "ai";
            public static readonly string ACCOUNT_CONFIG = "ac";
            public static readonly string REQUEST_ACCOUNT_INFO = "rai";
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
            public static readonly string PRO_TIPS = "pt";
            public static readonly string CHAT_BACKGROUNDS = "cbg";
            public static readonly string APP_INFO = "app";
            public static readonly string FORCE_SMS = "fsms";
            public static readonly string APP_UPDATE = "update";

            public static readonly string MSISDN_KEYWORD = "msisdn";
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
            public static readonly string TIME_DIFF_EPOCH = "serverEpochTime";
            public static readonly string REMOVE_EMMA = "removeEmma";
            public static readonly string NEW_UPDATE_AVAILABLE = "New_Update_Available";
            public static readonly string LAST_SELECTED_STICKER_CATEGORY = "lastSelectedStickerCategory";
            public static readonly string LAST_SELECTED_EMOTICON_CATEGORY = "lastSelectedEmoticonCategory";
            public static readonly string LAST_USER_JOIN_TIMESTAMP = "lastUjTs";

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
    }
}
