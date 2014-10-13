using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Constants
{
    public static class ServerJsonKeys
    {
        public static readonly string MESSAGE = "msg";
        public static readonly string MESSAGES = "msgs";
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
        public static readonly string CLIENT_TIMESTAMP = "cts";
        public static readonly string STATUS_DATA = "data";

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
        public static readonly string REQUEST_SERVER_TIME = "rsts";
        public static readonly string STATUS = "st";
        public static readonly string LONG_MESSAGE = "lm";

        public static readonly string PUSH = "push";
        public static readonly string CRITICAL = "critical";

        public static readonly string FILE_NAME = "fn";
        public static readonly string FILE_RESPONSE_DATA = "data";
        public static readonly string FILE_KEY = "fk";
        public static readonly string FILE_SIZE = "fs";
        public static readonly string FILE_THUMBNAIL = "tn";
        public static readonly string SOURCE = "source";
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
        public static readonly string FILE_TYPE_AUDIO = "audio/voice";
        public static readonly string FILE_TYPE_VIDEO = "video/mp4";

        public static readonly string DEVICE_ID = "deviceid";//A unique ID of the device 
        public static readonly string DEVICE_VERSION = "deviceversion";//The current OS version
        public static readonly string APP_VERSION = "app_version";//The app version
        public static readonly string APPVERSION = "appversion";//The app version
        public static readonly string INVITE_TOKEN = "invite_token";
        public static readonly string INVITE_TOKEN_KEY = "invite_token";//The referral token
        public static readonly string FAVORITES = "favorites";
        public static readonly string PENDING = "pending";
        public static readonly string REQUEST_PENDING = "requestpending";

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

        public static readonly string TAG_MOBILE = "wp8";
        public static readonly string VERSION = "version";

        public static readonly string TOTAL_CREDITS_PER_MONTH = "tc";

        public static readonly string ALL_INVITEE = "ai";
        public static readonly string ALL_INVITEE_JOINED = "aij";
        public static readonly string ACCOUNT = "account";
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

        public static readonly string NO_TOKEN = "notoken";
        public static readonly string INVALID_TOKEN = "invalidtoken";
        public static readonly string FAILURE = "failure";
        public static readonly string SUCCESS = "success";

        public static readonly string PRO_TIP_ID = "i";
        public static readonly string PRO_TIP_HEADER = "h";
        public static readonly string PRO_TIP_TEXT = "t";
        public static readonly string PRO_TIP_IMAGE = "img";
        public static readonly string PRO_TIP_TIME = "wt";

        public static readonly string BACKGROUND_ID = "bg_id";
        public static readonly string HAS_CUSTOM_BACKGROUND = "custom";
        public static readonly string CHAT_BACKGROUND_ARRAY = "cbgs";

        public static readonly string STEALTH = "stlth";
        public static readonly string RESET = "reset";
        public static readonly string CHAT_ENABLED = "en";
        public static readonly string CHAT_DISABLED = "di";
        public static readonly string HIDDEN_MODE_ENABLED = "enabled";
        public static readonly string HIDDEN_MODE_TYPE = "ts";

        public static readonly string PREVIEW = "preview";

        public static readonly string NEW_USER = "nu";
        public static readonly string DND_NUMBERS = "dndnumbers";

        public static readonly string OK = "ok";
        public static readonly string STAT = "stat";
        public static readonly string FAIL = "fail";
        public static readonly string REWARDS_TOKEN = "reward_token";
        public static readonly string IP_KEY = "ip";
        public static readonly string SHOW_REWARDS = "show_rewards";
        public static readonly string REWARDS_VALUE = "tt";

        public static readonly string LOCALE = "locale";
        public static readonly string PUSH_SU = "pushsu";

        public static readonly string ICON = "icon";
        public static readonly string LASTSEEN = "ls";
        public static readonly string LASTSEENONOFF = "lastseen";
        public static readonly string JUSTOPENED = "justOpened";

        public static readonly string ENABLE_PUSH_BATCH_SU = "enablepushbatchingforsu";

        public static readonly string REQUEST_DISPLAY_PIC = "rdp";
        public static readonly string MSISDN = "msisdn";
        public static readonly string MSISDNS = "msisdns";
        public static readonly string NAME = "name";

        public static readonly string FREE_INVITE_POPUP_TITLE = "free_invite_popup_title";
        public static readonly string FREE_INVITE_POPUP_TEXT = "free_invite_popup_text";

        public static readonly string GC_PIN = "pin";

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
    }
}
