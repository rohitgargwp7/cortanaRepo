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
        public static readonly string FILE_TRANSFER_LOCATION = "TransferredFiles";
        public static readonly string FILES_BYTE_LOCATION = FILE_TRANSFER_LOCATION + "/FileBytes";
        public static readonly string FILES_THUMBNAILS = FILE_TRANSFER_LOCATION + "/Thumbnails";
        public static readonly string FILES_ATTACHMENT = FILE_TRANSFER_LOCATION + "/Attachments";
        public static readonly string FILES_MESSAGE_PREFIX = "I sent you a file. To view go to " + HikeConstants.FILE_TRANSFER_BASE_URL + "/";

        public static readonly string SHARED_FILE_LOCATION = "/shared/transfers";
        public static readonly string FILE_TRANSFER_BASE_URL = AccountUtils.BASE + "/user/ft";


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

        /* constants for defining what to do after checking for updates*/
        public static readonly int UPDATE_AVAILABLE = 2;
        public static readonly int CRITICAL_UPDATE = 1;
        public static readonly int NO_UPDATE = 0;
        public static string ALL_INVITEE = "ai";
        public static object ALL_INVITEE_JOINED = "aij";
        public static string TOTAL_CREDITS_PER_MONTH = "tc";

        public static string GROUP_PARTICIPANT_SEPARATOR = ",";
        public static string MSISDN="msisdn";
        public static string NAME = "name";
        public static string NEW_USER="nu";
        public static string DND_NUMBERS = "dndnumbers";
        public static string START_NEW_GROUP = "start_new_group";
        public static string EXISTING_GROUP_MEMBERS = "existing_group_members";
        public static string IS_EXISTING_GROUP = "is_existing_group";
        public static string GROUP_ID_FROM_CHATTHREAD = "groupIdFromChatThreadPage";
        public static string GROUP_NAME_FROM_CHATTHREAD = "groupNameFromChatThreadPage";
        public static string GROUP_CHAT = "groupChat";
        public static string IS_NEW_INSTALLATION = "is_new_installation";
        public static string MY_PROFILE_PIC = "my_profile_pic";

        public static string USER_JOINED = " has joined the group chat";
        public static string USER_LEFT = " has left the group chat";
        public static string GROUP_CHAT_END = "This group chat has end.";

        /* NAVIGATION CONSTANTS*/
        public static string OBJ_FROM_SELECTUSER_PAGE = "objFromSelectUserPage";
        public static string OBJ_FROM_CONVERSATIONS_PAGE = "objFromConversationPage";
        public static string FORWARD_MSG = "forwardedText";

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

            public static readonly string GROUP_CHAT_JOIN = "gcj";
            public static readonly string GROUP_CHAT_LEAVE = "gcl";
            public static readonly string GROUP_CHAT_END = "gce";
            public static readonly string GROUP_CHAT_NAME = "gcn";
            public static readonly string REQUEST_ACCOUNT_INFO = "rai";
            public static string ACCOUNT_INFO = "ai";
        }
    }
}
