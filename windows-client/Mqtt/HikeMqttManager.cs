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
using System.Runtime.CompilerServices;
using finalmqtt.Client;
using mqtttest.Client;
using windows_client.utils;
using System.Collections.Generic;
using windows_client.Model;
using windows_client.DbUtils;
using finalmqtt.Msg;

namespace windows_client.Mqtt
{
    //    public class HikeMqttManager : Listener
    public class HikeMqttManager
    {
        public MqttConnection mqttConnection;
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



        // status of MQTT client connection
        public MQTTConnectionStatus connectionStatus = MQTTConnectionStatus.INITIAL;

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
            return false;

            //settings = this.mHikeService.getSharedPreferences(HikeMessengerApp.ACCOUNT_SETTINGS, 0);
            //password = settings.getString(HikeMessengerApp.TOKEN_SETTING, null);
            //topic = uid = settings.getString(HikeMessengerApp.UID_SETTING, null);
            //clientId = settings.getString(HikeMessengerApp.MSISDN_SETTING, null);
            //Log.d("HikeMqttManager", "clientId is " + clientId);
            //return !TextUtils.isEmpty(topic) && !TextUtils.isEmpty(clientId) && !TextUtils.isEmpty(password);
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
                //			mqttConnection.listener(this);
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
            return (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.NetworkInterfaceType !=
                 Microsoft.Phone.Net.NetworkInformation.NetworkInterfaceType.None);
        }

        /*
         * Send a request to the message broker to be sent messages published with the specified topic name. Wildcards are allowed.
         */
        //TODO Define class Topics and use that
        private void subscribeToTopics(String[] topics)
        {

            if (isConnected() == false)
            {
                // quick sanity check - don't try and subscribe if we
                // don't have a connection
                return;
            }

            for (int i = 0; i < topics.Length; i++)
            {
                mqttConnection.subscribe(topics[i], new SubscribeCB(this));
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

            //if (this.mHikeService.isUserOnline())
            //{
            //    Log.d("HikeMqttManager", "netconnection valid, try to connect");
            //    // set the status to show we're trying to connect
            //    connectToBroker();
            //}
            //else
            //{
            //    // we can't do anything now because we don't have a working
            //    // data connection
            //    setConnectionStatus(MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET);
            //}
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
                        HikeDbUtils.addSentMessage(packet);
                    }
                    catch (Exception e)
                    {
                        //                        Log.e("HikeMqttManager", "Unable to persist message");
                    }
                }

                this.connect();
                return;
            }
            PublishCB pbCB = new PublishCB(packet);


            mqttConnection.publish(this.topic + HikeConstants.PUBLISH_TOPIC,
                    packet.Message, (QoS)qos == 0 ? QoS.AT_MOST_ONCE : QoS.AT_LEAST_ONCE,
                    pbCB);
        }


    }
}
