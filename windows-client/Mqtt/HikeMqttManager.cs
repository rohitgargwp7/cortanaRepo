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
using System.Runtime.CompilerServices;
using finalmqtt.Client;
using mqtttest.Client;
using windows_client.utils;
using System.Collections.Generic;
using windows_client.Model;
using windows_client.DbUtils;
using finalmqtt.Msg;
using System.Net.NetworkInformation;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

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
            pubSub.addListener(HikePubSub.WS_RECEIVED, this);
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

        private Dictionary<Int32, HikePacket> mqttIdToPacket;


        public HikePacket getPacketIfUnsent(int mqttId)
        {
            HikePacket packet;
            mqttIdToPacket.TryGetValue(mqttId, out packet);
            return packet;
        }


        private bool init()
        {
            password = (string)IsolatedStorageSettings.ApplicationSettings[App.TOKEN_SETTING];
            uid = topic = (string)IsolatedStorageSettings.ApplicationSettings[App.UID_SETTING];
            clientId = (string)IsolatedStorageSettings.ApplicationSettings[App.MSISDN_SETTING];
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
                mqttConnection = new MqttConnection(clientId, brokerHostName, brokerPortNumber, uid, password, new ConnectCB(this));
                mqttConnection.Listener = this;
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
            //            NetworkInterface.GetIsNetworkAvailable();

            return (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType !=
                 Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.None);
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
            mqttConnection.ping(new PingCB(this));
        }

        public void reconnect()
        {
            if (this.connectionStatus == MQTTConnectionStatus.CONNECTING)
                return;

            if (mqttConnection != null)
            {
                mqttConnection.disconnect(new DisconnectCB(true, this));
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
                        MiscDBUtils.addSentMessage(packet);
                    }
                    catch (Exception e)
                    {
                        //                        Log.e("HikeMqttManager", "Unable to persist message");
                    }
                }

                this.connect();
                return;
            }
            PublishCB pbCB = new PublishCB(packet, this);


            mqttConnection.publish(this.topic + HikeConstants.PUBLISH_TOPIC,
                    packet.Message, (QoS)qos == 0 ? QoS.AT_MOST_ONCE : QoS.AT_LEAST_ONCE,
                    pbCB);
        }


        private Topic[] getTopics()
        {
            //		bool appConnected = mHikeService.appIsConnected();
            bool appConnected = true;
            List<Topic> topics = new List<Topic>(2 + (appConnected ? 0 : 1));
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
            //		Log.d("HikeMqttManager", "mqtt connected");
            setConnectionStatus(MQTTConnectionStatus.CONNECTED);

            subscribeToTopics(getTopics());

            /* Accesses the persistence object from the main handler thread */

            //TODO make it async
            List<HikePacket> packets = MiscDBUtils.getAllSentMessages();
            for (int i = 0; i < packets.Count; i++)
            {
                //					Log.d("HikeMqttManager", "resending message " + new String(hikePacket.getMessage()));
                send(packets[i], 1);
            }
        }

        public void onDisconnected()
        {
            setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
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
                logger.Info("WebSocketPublisher", "Invalid JSON message: " + msg + ", Exception : " + e);
                return;
            }
            string type = (string)jsonObj[HikeConstants.TYPE];
            string msisdn = (string)jsonObj[HikeConstants.FROM];

           
        }

        public void onEventReceived(string type, object obj)
        {
            if (type == HikePubSub.MQTT_PUBLISH) // signifies msg is received through web sockets.
            {
                
                //onMessage(message);
            }
        }
    }
}
