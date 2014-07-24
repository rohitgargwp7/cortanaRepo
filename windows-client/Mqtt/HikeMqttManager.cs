﻿using System;
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
        public bool IsAppStarted = true; // false for resume
        private const int API_VERSION = 2;
        private const bool AUTO_SUBSCRIBE = true;
        //Bug# 3833 - There are some changes in initialization of static objects in .Net 4. So, removing static for now.
        //Later, on we should be using singleton so, static won't be required
        private object lockObj = new object(); //TODO - Madhur Garg make this class singleton

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

        private volatile bool disconnectExplicitly = false;

        private bool init()
        {
            App.appSettings.TryGetValue<string>(App.TOKEN_SETTING, out password);
            App.appSettings.TryGetValue<string>(App.UID_SETTING, out topic);
            App.appSettings.TryGetValue<string>(App.MSISDN_SETTING, out clientId);
            uid = topic;
            if (!String.IsNullOrEmpty(clientId))
                clientId += string.Format(":{0}:{1}", API_VERSION, AUTO_SUBSCRIBE);//: Api version : Auto subscribe(true/false)
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
                disconnectExplicitly = !reconnect;
                setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);

                Debug.WriteLine("Disconnect from Broker Called");
                if (mqttConnection != null)
                    mqttConnection.disconnect();

            }
            catch (Exception ex)
            {
                Debug.WriteLine("HIkeMqttManager ::  disconnectFromBroker : disconnectFromBroker, Exception : " + ex.StackTrace);
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
                lock (lockObj)
                {
                    if (mqttConnection == null)
                    {
                        if (!init())
                        {
                            return;
                        }
                        mqttConnection = new MqttConnection(clientId, uid, password, new ConnectCB(this), this);
                    }
                }
            }

            try
            {
                // try to connect
                setConnectionStatus(MQTTConnectionStatus.CONNECTING);
                mqttConnection.connect(IpManager.Instance.GetIp(), brokerPortNumber);
            }
            catch (Exception ex)
            {
                /* couldn't connect, schedule a ping even earlier? */
                Debug.WriteLine("HIkeMqttManager ::  connectToBroker : connectToBroker, Exception : " + ex.StackTrace);
            }

        }

        public bool isConnected()
        {
            return (MQTTConnectionStatus.CONNECTED == connectionStatus);
        }

        public bool isConnecting()
        {
            return (MQTTConnectionStatus.CONNECTING == connectionStatus);
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
                    if (mqttConnection != null)
                        mqttConnection.unsubscribe(topics[i], null);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HIkeMqttManager ::  unsubscribeFromTopics : unsubscribeFromTopics, Exception : " + ex.StackTrace);
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
            List<string> listTopics = new List<string>();
            List<QoS> listQos = new List<QoS>();
            for (int i = 0; i < topics.Length; i++)
            {
                listTopics.Add(topics[i].Name);
                listQos.Add(topics[i].qos);
            }
            if (mqttConnection != null)
                mqttConnection.subscribe(listTopics, listQos, new SubscribeCB(this));

        }

        /*
         * Checks if the MQTT client thinks it has an active connection
         */

        public MQTTConnectionStatus getConnectionStatus()
        {
            return connectionStatus;
        }

        private Border b = new Border();

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
            catch (Exception ex)
            {
                Debug.WriteLine("HIkeMqttManager ::  connect : connect, Exception : " + ex.StackTrace);
                connectInBackground();
            }
        }

        private void connectInBackground()
        {
            lock (lockObj)
            {
                disconnectExplicitly = false;
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine("HIkeMqttManager ::  send : send, Exception : " + ex.StackTrace);
                    }
                }
                this.connect();
                return;
            }
            PublishCB pbCB = null;
            if (qos > 0)
                pbCB = new PublishCB(packet, this, qos, false);
            if (mqttConnection != null)
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
            if (mqttConnection != null)
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

        public void onConnected()
        {
            Debug.WriteLine("SUCCESS: MQTT CONNECTED");
            if (isConnected())
            {
                return;
            }
            setConnectionStatus(MQTTConnectionStatus.CONNECTED);

            /* Accesses the persistence object from the main handler thread */

            //TODO make it async
            List<HikePacket> packets = MqttDBUtils.getAllSentMessages();

            sendAppFGStatusToServer();

            if (packets != null)
            {
                Debug.WriteLine("MQTT MANAGER:: NUmber os unsent messages" + packets.Count);
                sendAllUnsentMessages(packets);
            }
            PushHelper.Instance.ClearTile();
        }

        public void onDisconnected()
        {
            Debug.WriteLine("OnDisconnected Called");
            if (mqttConnection != null)
            {
                mqttConnection.MqttListener = null;
                mqttConnection = null;
            }
            IpManager.Instance.ResetIp();
            if (!disconnectExplicitly)
            {
                setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
                connect();
            }
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
            json.TryGetValue(HikeConstants.DATA, out data);
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

        private void sendAppFGStatusToServer()
        {

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.APP_INFO);
            obj.Add(HikeConstants.TIMESTAMP, TimeUtils.getCurrentTimeStamp());
            obj.Add(HikeConstants.STATUS, "fg");
            JObject data = new JObject();

            if (IsAppStarted)
                data.Add(HikeConstants.JUSTOPENED, true);
            else
                data.Add(HikeConstants.JUSTOPENED, false);

            obj.Add(HikeConstants.DATA, data);

            Object[] objArr = new object[2];
            objArr[0] = obj;
            objArr[1] = 0;

            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, objArr);
        }

        IDisposable scheduledConnect = null;

        public void ScheduleConnect(int seconds)
        {
            if (scheduledConnect != null)
                scheduledConnect.Dispose();
            scheduledConnect = scheduler.Schedule(connect, TimeSpan.FromSeconds(seconds));
        }
    }
}
