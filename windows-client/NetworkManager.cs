using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using windows_client.Model;

namespace windows_client
{
    public class NetworkManager : HikePubSub.Listener
    {
        /* message read by recipient */
        public static readonly string MESSAGE_READ = "mr";

        public static readonly string MESSAGE = "m";

        public static readonly string SMS_CREDITS = "sc";

        public static readonly string DELIVERY_REPORT = "dr";

        public static readonly string SERVER_REPORT = "sr";

        public static readonly string USER_JOINED = "uj";

        public static readonly string USER_LEFT = "ul";

        public static readonly string START_TYPING = "st";

        public static readonly string END_TYPING = "et";

        public static readonly string INVITE = "i";

        public static readonly string ICON = "ic";

        private HikePubSub pubSub;

        private static NLog.Logger logger;
        private static volatile NetworkManager instance;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private NetworkManager()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            pubSub = App.HikePubSubInstance;
            pubSub.addListener(HikePubSub.WS_RECEIVED, this);
        }

        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new NetworkManager();
                    }
                }

                return instance;
            }
        }

        private void onMessage(string msg)
        {
            JObject jsonObj = null;
            try
            {
                jsonObj = JObject.Parse(msg);
            }
            catch (JsonReaderException e)
            {
                logger.Info("WebSocketPublisher", "Invalid JSON message: " + msg +", Exception : "+e);
                return;
            }
            string type = (string)jsonObj[HikeConstants.TYPE];
            string msisdn = (string)jsonObj[HikeConstants.FROM];

            if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    ConvMessage convMessage = new ConvMessage(jsonObj);
                    this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED_FROM_SENDER, convMessage);
                }
                catch (Exception e)
                {
                    logger.Info("NETWORK MANAGER", "Invalid JSON", e);
                }
            }
            else if (START_TYPING == type) /* Start Typing event received*/
            {
                this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, msisdn);
            }
            else if (END_TYPING == type) /* End Typing event received */
            {
                this.pubSub.publish(HikePubSub.END_TYPING_CONVERSATION, msisdn);
            }
            else if (SMS_CREDITS == type) /* SMS CREDITS */
            {
                int sms_credits = Int32.Parse((string)jsonObj[HikeConstants.DATA]);
                this.pubSub.publish(HikePubSub.SMS_CREDIT_CHANGED, sms_credits);
            }
            else if (SERVER_REPORT == type) /* Represents Server has received the msg you sent */
            {
                string id = (string)jsonObj[HikeConstants.DATA];
                long msgID;
                try
                {
                    msgID = long.Parse(id);
                }
                catch (FormatException e)
                {
                    logger.Info("NETWORK MANAGER", "Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                }
                this.pubSub.publish(HikePubSub.SERVER_RECEIVED_MSG, msgID);
            }
            else if (DELIVERY_REPORT == type) // this handles the case when msg with msgId is recieved by the recipient but is unread
            {
                string id = (string)jsonObj[HikeConstants.DATA];
                long msgID;
                try
                {
                    msgID = Int64.Parse(id);
                }
                catch (FormatException e)
                {
                    logger.Info("NETWORK MANAGER", "Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                }
                logger.Info("NETWORK MANAGER", "Delivery report received for msgid : " + msgID + "	;	REPORT : DELIVERED");
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED, msgID);
            }
            else if (MESSAGE_READ == type) // Message read by recipient
            {
                JArray msgIds = (JArray)jsonObj["d"];
                if (msgIds == null)
                {
                    logger.Info("NETWORK MANAGER", "Update Error : Message id Array is empty or null . Check problem");
                    return;
                }

                long[] ids = new long[msgIds.Count];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = Int64.Parse(msgIds[i].ToString());
                }
                logger.Info("NETWORK MANAGER", "Delivery report received : " + "	;	REPORT : DELIVERED READ");
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED_READ, ids);
            }
            else if ((USER_JOINED == type) || (USER_LEFT == type))
            {
                string uMsisdn = (string)jsonObj[HikeConstants.DATA];
                bool joined = USER_JOINED == type;
                this.pubSub.publish(joined ? HikePubSub.USER_JOINED : HikePubSub.USER_LEFT, uMsisdn);
            }
            /*else if ((ICON.equals(type)))
            {
                IconCacheManager.getInstance().clearIconForMSISDN(msisdn);
            }*/
            else
            {
                logger.Info("WebSocketPublisher", "Unknown Type:" + type);
            }
        }

        public void onEventReceived(string type, object obj)
        {
            if (type == HikePubSub.WS_RECEIVED) // signifies msg is received through web sockets.
            {
                string message = (string)obj;
                onMessage(message);
            }
        }
    }
}
