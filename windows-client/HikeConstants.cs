using System;

namespace windows_client
{
    public class HikeConstants
    {
        public static readonly String MESSAGE = "msg";
        public static readonly String UI_TOPIC = "/u";
        public static readonly String APP_TOPIC = "/a";
        public static readonly String SERVICE_TOPIC = "/s";
        public static readonly String PUBLISH_TOPIC = "/p";

        public static readonly String TYPE = "t";
        public static readonly String DATA = "d";
        public static readonly String TO = "to";
        public static readonly String FROM = "f";

        public static readonly String HIKE_MESSAGE = "hm";
        public static readonly String SMS_MESSAGE = "sm";
        public static readonly String TIMESTAMP = "ts";
        public static readonly String MESSAGE_ID = "i";
        public static readonly String METADATA = "md";
        public static readonly String METADATA_DND = "dnd";
        public static readonly String ANALYTICS_EVENT = "ae";

        public static readonly String SOUND_PREF = "soundPref";
        public static readonly String VIBRATE_PREF = "vibratePref";
        public static readonly String HIKEBOT = "TD-HIKE";

        public static readonly String DONE = "Done";
        public static readonly String PIN_ERROR = "PinError";
        public static readonly String ADDRESS_BOOK_ERROR = "AddressBookError";
        public static readonly String CHANGE_NUMBER = "ChangeNumber";
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
        public static readonly String ADAPTER_NAME = "hikeadapter";

        /* constants for defining what to do after checking for updates*/
        public static readonly int UPDATE_AVAILABLE = 2;
        public static readonly int CRITICAL_UPDATE = 1;
        public static readonly int NO_UPDATE = 0;

        public static class Extras
	    {
            public static readonly string ANIMATED_ONCE = "animatedOnce";
        }

    }
}
