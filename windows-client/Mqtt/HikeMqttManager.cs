using System;
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
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;
using System.Diagnostics;

namespace windows_client.Mqtt
{
    //    public class HikeMqttManager : Listener
    public class HikeMqttManager : Listener, HikePubSub.Listener
    {
        public volatile MqttConnection mqttConnection;
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
            //logger = NLog.LogManager.GetCurrentClassLogger();
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


        private String brokerHostName = AccountUtils.MQTT_HOST;


        // defaults - this sample uses very basic defaults for it's interactions
        // with message brokers
        private int brokerPortNumber = AccountUtils.MQTT_PORT;

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

        private volatile bool disconnectCalled = false;

        public HikePacket getPacketIfUnsent(int mqttId)
        {
            HikePacket packet;
            mqttIdToPacket.TryGetValue(mqttId, out packet);
            return packet;
        }


        private bool init()
        {
            App.appSettings.TryGetValue<string>(App.TOKEN_SETTING, out password);
            App.appSettings.TryGetValue<string>(App.UID_SETTING, out topic);
            App.appSettings.TryGetValue<string>(App.MSISDN_SETTING, out clientId);
            uid = topic;
            if (!String.IsNullOrEmpty(clientId))
                clientId += ":1";
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
        //        [MethodImpl(MethodImplOptions.Synchronized)]
        public void disconnectFromBroker(bool reconnect)
        {
            try
            {
                if (mqttConnection != null)
                {
                    disconnectCalled = !reconnect;
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
        //[MethodImpl(MethodImplOptions.Synchronized)]
        public void connectToBroker()
        {
            if (isConnected() || isConnecting())
            {
                return;
            }

            if (mqttConnection == null)
            {
                if (!init())
                {
                    return;
                }
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

        public bool isConnecting()
        {
            return (mqttConnection != null) && (MQTTConnectionStatus.CONNECTING == connectionStatus);
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
            try
            {
                if (disconnectCalled == false)
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
            }
            catch (Exception)
            {
                //connect();
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

        private static Border b = new Border();

        public void connect()
        {
            try
            {
                if (isConnected() || isConnecting())
                {
                    return;
                }
                b.Height = 4;
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    connectInBackground();
                };
                bw.RunWorkerAsync();
            }
            catch (Exception)
            {
                connectInBackground();
            }
        }

        private void connectAgain()
        {
            if (!isConnected() && !isConnecting() && connectionStatus != MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET)
            { 
                connect();
            }        
        }

        private static object lockObj = new object();


        private void connectInBackground()
        {
            lock (lockObj)
            {
                disconnectCalled = false;
                if (isConnected() || isConnecting())
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
                    //scheduler.Schedule(ping, TimeSpan.FromSeconds(10));
                }
            }
        }

        //this is called when messages are sent 1 by 1
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
            PublishCB pbCB = null;
            if (qos > 0)
                pbCB = new PublishCB(packet, this, qos, false);
            mqttConnection.publish(this.topic + HikeConstants.PUBLISH_TOPIC,
                    packet.Message, (QoS)qos == 0 ? QoS.AT_MOST_ONCE : QoS.AT_LEAST_ONCE,
                    pbCB);
        }

        //this is called to send unsent messages. They all are sent in a single thread
        public void sendAllUnsentMessages(List<HikePacket> packets)
        {
            if (!isConnected())
            {
                this.connect();
                return;
            }
            byte[][] messagesToSend = new byte[packets.Count][];
            PublishCB[] messageCallbacks = new PublishCB[packets.Count];
            for (int i = 0; i < packets.Count; i++)
            {
                messageCallbacks[i] = new PublishCB(packets[i], this, 1, true);
                messagesToSend[i] = packets[i].Message;
            }
            mqttConnection.publish(this.topic + HikeConstants.PUBLISH_TOPIC,
                    messagesToSend, QoS.AT_LEAST_ONCE,
                    messageCallbacks);
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

        void recursivePingSchedule(Action<TimeSpan> action)
        {
            action(TimeSpan.FromSeconds(HikeConstants.RECURSIVE_PING_INTERVAL));
            ping();
        }

        private volatile bool isRecursivePingScheduled = false;

        public void onConnected()
        {
            if (isConnected())
            {
                return;
            }
            setConnectionStatus(MQTTConnectionStatus.CONNECTED);
            subscribeToTopics(getTopics());
            if (!isRecursivePingScheduled)
                scheduler.Schedule(new Action<Action<TimeSpan>>(recursivePingSchedule), TimeSpan.FromSeconds(HikeConstants.RECURSIVE_PING_INTERVAL));

            /* Accesses the persistence object from the main handler thread */

            //TODO make it async
            List<HikePacket> packets = MqttDBUtils.getAllSentMessages();

            if (packets == null)
                return;
            Debug.WriteLine("MQTT MANAGER:: NUmber os unsent messages" + packets.Count);
            sendAllUnsentMessages(packets);
        }

        public void onDisconnected()
        {
            setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            mqttConnection = null;
            if (!disconnectCalled)
                disconnectFromBroker(true);
        }

        public void onPublish(String topic, byte[] body)
        {
            String receivedMessage = Encoding.UTF8.GetString(body, 0, body.Length);
            NetworkManager.Instance.onMessage(receivedMessage);
        }

        public void onEventReceived(string type, object obj)
        {
            if (type == HikePubSub.MQTT_PUBLISH) // signifies msg is received through web sockets.
            {
                if (obj is object[])
                {
                    object[] vals = (object[])obj;
                    JObject json = (JObject)vals[0];
                    int qos = (int)vals[1];
                    mqttPublishToServer(json, qos);
                }
                else
                {
                    JObject json = (JObject)obj;
                    mqttPublishToServer(json);
                }
            }
        }

        public void mqttPublishToServer(JObject json)
        {
            mqttPublishToServer(json, 1);
        }


        public void mqttPublishToServer(JObject json, int qos)
        {
            JToken data;
            json.TryGetValue(HikeConstants.TYPE, out data);
            string objType = data.ToString();
            json.TryGetValue("d", out data);
            JObject dataObj;
            long msgId;

            if (objType == NetworkManager.MESSAGE || objType == NetworkManager.INVITE)
            {
                dataObj = JObject.FromObject(data);
                JToken messageIdToken;
                dataObj.TryGetValue("i", out messageIdToken);
                msgId = Convert.ToInt64(messageIdToken.ToString());
            }
            else
            {
                msgId = -1;
            }
            String msgToPublish = json.ToString(Newtonsoft.Json.Formatting.None);
            byte[] byteData = Encoding.UTF8.GetBytes(msgToPublish);
            HikePacket packet = new HikePacket(msgId, byteData, TimeUtils.getCurrentTimeTicks());
            send(packet, qos);
        }

    }
}
