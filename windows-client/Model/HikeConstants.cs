﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace windows_client.Model
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

	    public static readonly string SOUND_PREF = "soundPref";
	    public static readonly string VIBRATE_PREF = "vibratePref";
	    public static readonly string HIKEBOT = "TD-HIKE";
	
	    public static readonly string DONE = "Done";
	    public static readonly string PIN_ERROR = "PinError";
	    public static readonly string ADDRESS_BOOK_ERROR = "AddressBookError";
	    public static readonly string CHANGE_NUMBER = "ChangeNumber"; 
	    /* how long to wait between sending publish and receiving an acknowledgement */
	    public static readonly long MESSAGE_DELIVERY_TIMEOUT = 5*1000;

	    /* how long to wait for a ping confirmation */
	    public static readonly long PING_TIMEOUT = 5*1000;

	    /* how long to wait to resend message. This should significantly greathr than PING_TIMEOUT */
	    public static readonly long MESSAGE_RETRY_INTERVAL = 15*1000;

	    /* quiet period of no changes before actually updating the db */
	    public static readonly long CONTACT_UPDATE_TIMEOUT = 10*1000;

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
	
	    public static class Extras
	    {
		    public static readonly string MSISDN = "msisdn";
		    public static readonly string ID = "id";
		    public static readonly string NAME = "name";
		    public static readonly string INVITE = "invite";
		    public static readonly string MSG = "msg";
		    public static readonly string PREF = "pref";
		    public static readonly string EDIT = "edit";
		    public static readonly string IMAGE_PATH ="image-path";
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
    }
}
