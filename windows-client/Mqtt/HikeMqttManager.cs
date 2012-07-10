using System;
using System.Runtime.CompilerServices;
using finalmqtt.Client;
using windows_client.utils;
using System.Collections.Generic;
using windows_client.Model;
using windows_client.DbUtils;
using finalmqtt.Msg;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Phone.Reactive;

namespace windows_client.Mqtt
{
    //    public class HikeMqttManager : Listener
    public class HikeMqttManager : Listener, HikePubSub.Listener
    {
        private static NLog.Logger logger;
        public MqttConnection mqttConnection;
        private HikePubSub pubSub;
        // constants used to define MQTT connection status
        public enum MQTTConnectionStatus
        {
            INITIAL, // initial status
            CONNECTING, // attempting to connect
            CONNECTED, // connected
            NOTCONNECTED_WAITINGFORINTERNET, // can't connect because the phone
            // does not have Internet access
            NOTCONNECTED_USERDISCONNECT, // user has explicitly requested
            // disconnection
            NOTCONNECTED_DATADISABLED, // can't connect because the user
            // has disabled data access
            NOTCONNECTED_UNKNOWNREASON // failed to connect for some reason
        }

        public HikeMqttManager()
        {
            logger = NLog.LogManager.GetCurrentClassLogger();
            pubSub = App.HikePubSubInstance;
            pubSub.addListener(HikePubSub.MQTT_PUBLISH, this);
        }

        // status of MQTT client connection
        public volatile MQTTConnectionStatus connectionStatus = MQTTConnectionStatus.INITIAL;

        /************************************************************************/
        /* VARIABLES used to configure MQTT connection */
        /************************************************************************/

        // taken from preferences
        // host name of the server we're receiving push notifications from
        private String brokerHostName = AccountUtils.HOST;


        // defaults - this sample uses very basic defaults for it's interactions
        // with message brokers
        private int brokerPortNumber = 1883;

        //        private HikeMqttPersistence persistence = null;

        /*
         * how often should the app ping the server to keep the connection alive?
         * 
         * too frequently - and you waste battery life too infrequently - and you wont notice if you lose your connection until the next unsuccessfull attempt to ping // // it's a
         * trade-off between how time-sensitive the data is that your // app is handling, vs the acceptable impact on battery life // // it is perhaps also worth bearing in mind the
         * network's support for // long running, idle connections. Ideally, to keep a connection open // you want to use a keep alive value that is less than the period of // time
         * after which a network operator will kill an idle connection
         */
        private short keepAliveSeconds = HikeConstants.KEEP_ALIVE;

        private String clientId;

        private String topic;

        private String password;

        private String uid;

        private IScheduler scheduler = Scheduler.NewThread;


        private Dictionary<Int32, HikePacket> mqttIdToPacket;


        public HikePacket getPacketIfUnsent(int mqttId)
        {
            HikePacket packet;
            mqttIdToPacket.TryGetValue(mqttId, out packet);
            return packet;
        }


        private bool init()
        {
            password = (string)App.appSettings[App.TOKEN_SETTING];
            uid = topic = (string)App.appSettings[App.UID_SETTING];
            clientId = (string)App.appSettings[App.MSISDN_SETTING];
            return !(String.IsNullOrEmpty(password) || String.IsNullOrEmpty(clientId) || String.IsNullOrEmpty(topic));
        }

        public void setConnectionStatus(MQTTConnectionStatus connectionStatus)
        {
            this.connectionStatus = connectionStatus;
        }

        /*
 * Terminates a connection to the message broker.
 */
        //synchronized
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void disconnectFromBroker(bool reconnect)
        {
            try
            {
                if (mqttConnection != null)
                {
                    mqttConnection.disconnect(new DisconnectCB(reconnect, this));
                    mqttConnection = null;
                }

                setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            }
            catch (Exception e)
            {
            }
        }


        //synchronized
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void connectToBroker()
        {
            if (connectionStatus == MQTTConnectionStatus.CONNECTING)
            {
                return;
            }

            if (mqttConnection == null)
            {
                init();
                mqttConnection = new MqttConnection(clientId, brokerHostName, brokerPortNumber, uid, password, new ConnectCB(this));
                mqttConnection.MqttListener = this;
            }

            try
            {
                // try to connect
                setConnectionStatus(MQTTConnectionStatus.CONNECTING);
                mqttConnection.connect();
            }
            catch (Exception e)
            {
                /* couldn't connect, schedule a ping even earlier? */
            }
        }

        public bool isConnected()
        {
            return (mqttConnection != null) && (MQTTConnectionStatus.CONNECTED == connectionStatus);
        }

        private void unsubscribeFromTopics(string[] topics)
        {
            if (!isConnected())
            {
                return;
            }

            try
            {
                for (int i = 0; i < topics.Length; i++)
                {
                    mqttConnection.unsubscribe(topics[i], null);
                }
            }
            catch (ArgumentException e)
            {
                //			Log.e("HikeMqttManager", "IllegalArgument trying to unsubscribe", e);
            }
        }

        private bool isUserOnline()
        {
            return NetworkInterface.GetIsNetworkAvailable();
            //return (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType !=
            //     Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.None);
        }

        /*
         * Send a request to the message broker to be sent messages published with the specified topic name. Wildcards are allowed.
         */
        //TODO Define class Topics and use that
        private void subscribeToTopics(Topic[] topics)
        {

            if (isConnected() == false)
            {
                // quick sanity check - don't try and subscribe if we
                // don't have a connection
                return;
            }

            for (int i = 0; i < topics.Length; i++)
            {
                mqttConnection.subscribe(topics[i].Name, topics[i].qos, new SubscribeCB(this));
            }
        }

        /*
         * Checks if the MQTT client thinks it has an active connection
         */

        public MQTTConnectionStatus getConnectionStatus()
        {
            return connectionStatus;
        }

        public void ping()
        {
            if (mqttConnection != null)
            {
                mqttConnection.ping(new PingCB(this));
            }
            else
            {
                connect();
            }
        }

        public void reconnect()
        {
            if (this.connectionStatus == MQTTConnectionStatus.CONNECTING)
                return;

            if (mqttConnection != null)
            {
                mqttConnection.disconnect(new DisconnectCB(false, this));
                mqttConnection = null;
            }
            setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            connect();
        }

        public void connect()
        {
            if (isConnected())
            {
                return;
            }

            if (isUserOnline())
            {
                connectToBroker();
            }
            else
            {
                setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET);
                scheduler.Schedule(ping, TimeSpan.FromSeconds(10));
            }
        }

        public void send(HikePacket packet, int qos)
        {
            if (!isConnected())
            {
                /* only care about failures for messages we care about. */
                if (qos > 0)
                {
                    try
                    {
                        MqttDBUtils.addSentMessage(packet);
                    }
                    catch (Exception e)
                    {
                    }
                }

                this.connect();
                return;
            }
            PublishCB pbCB = new PublishCB(packet, this);
            String tempString = Encoding.UTF8.GetString(packet.Message, 0, packet.Message.Length);

            mqttConnection.publish(this.topic + HikeConstants.PUBLISH_TOPIC,
                    packet.Message, (QoS)qos == 0 ? QoS.AT_MOST_ONCE : QoS.AT_LEAST_ONCE,
                    pbCB);
        }


        private Topic[] getTopics()
        {
            bool appConnected = true;
            List<Topic> topics = new List<Topic>();
            topics.Add(new Topic(this.topic + HikeConstants.APP_TOPIC, QoS.AT_LEAST_ONCE));
            topics.Add(new Topic(this.topic + HikeConstants.SERVICE_TOPIC, QoS.AT_LEAST_ONCE));

            /* only subscribe to UI events if the app is currently connected */
            if (appConnected)
            {
                topics.Add(new Topic(this.topic + HikeConstants.UI_TOPIC, QoS.AT_LEAST_ONCE));
            }

            return topics.ToArray();
        }


        public void onConnected()
        {
            setConnectionStatus(MQTTConnectionStatus.CONNECTED);
            subscribeToTopics(getTopics());
            scheduler.Schedule(ping, TimeSpan.FromMinutes(10));

            /* Accesses the persistence object from the main handler thread */

            //TODO make it async
            List<HikePacket> packets = MqttDBUtils.getAllSentMessages();
            if (packets == null)
                return;
            for (int i = 0; i < packets.Count; i++)
            {
                send(packets[i], 1);
            }
        }

        public void onDisconnected()
        {
            setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            mqttConnection = null;
            connect();
        }

        public void onPublish(String topic, byte[] body)
        {
            String receivedMessage = Encoding.UTF8.GetString(body, 0, body.Length);
            JObject jsonObj = JObject.Parse(receivedMessage);

            JToken type;
            jsonObj.TryGetValue(HikeConstants.TYPE, out type);
            pubSub.publish(HikePubSub.WS_RECEIVED, receivedMessage);
        }

        public void onEventReceived(string type, object obj)
        {
            if (type == HikePubSub.MQTT_PUBLISH) // signifies msg is received through web sockets.
            {
                JObject json = (JObject)obj;
                JToken data;
                json.TryGetValue(HikeConstants.TYPE, out data);
                string objType = data.ToString();
                json.TryGetValue("d", out data);
                JObject dataObj;
                int msgId;

                if (objType == NetworkManager.MESSAGE)
                {
                    dataObj = JObject.FromObject(data);
                    JToken messageIdToken;
                    dataObj.TryGetValue("i", out messageIdToken);
                    msgId = Convert.ToInt32(messageIdToken.ToString());
                }
                else
                {
                    msgId = -1;
                }
                String msgToPublish = json.ToString(Newtonsoft.Json.Formatting.None);
                byte[] byteData = Encoding.UTF8.GetBytes(msgToPublish);
                HikePacket packet = new HikePacket(msgId, byteData, TimeUtils.getCurrentTimeStamp());
                send(packet, 1);
            }
        }

    }
}
