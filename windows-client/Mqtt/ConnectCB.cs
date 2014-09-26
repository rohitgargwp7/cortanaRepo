using System;
using mqtttest.Client;
using System.Windows;
using windows_client.DbUtils;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Reactive;
using windows_client.utils;

namespace windows_client.Mqtt
{
    public class ConnectCB : Callback
    {
        private HikeMqttManager hikeMqttManager;

        public ConnectCB(HikeMqttManager hikeMqttManager)
        {
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onFailure(Exception value)
        {
            if ((value is ConnectionException) && ((ConnectionException)value).getCode().Equals(finalmqtt.Msg.ConnAckMessage.ConnectionStatus.BAD_USERNAME_OR_PASSWORD))
            {
                bool isPresent = false;

                if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.IS_DB_CREATED))
                    isPresent = true;
                HikeInstantiation.ClearAppSettings();
                if (isPresent)

                    HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.IS_DB_CREATED, true);
                NetworkManager.turnOffNetworkManager = true; // stop network manager
                HikeInstantiation.MqttManagerInstance.disconnectFromBroker(false);
                MiscDBUtil.clearDatabase();

                HttpNotificationChannel pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
                if (pushChannel != null)
                {
                    if (pushChannel.IsShellTileBound)
                        pushChannel.UnbindToShellTile();
                    if (pushChannel.IsShellToastBound)
                        pushChannel.UnbindToShellToast();
                    pushChannel.Close();
                }
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.BAD_USER_PASS, null);
            }
            else if ((value is ConnectionException) && ((ConnectionException)value).getCode().Equals(finalmqtt.Msg.ConnAckMessage.ConnectionStatus.SERVER_UNAVAILABLE))
            {
                Random rnd = new Random();
                int nextscheduleTime = rnd.Next(HikeConstants.SERVER_UNAVAILABLE_MAX_CONNECT_TIME) + 1;//in minutes
                hikeMqttManager.ScheduleConnect(nextscheduleTime * 60);
            }
            else if (hikeMqttManager.connectionStatus != HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET)
            {
                hikeMqttManager.ScheduleConnect(5);
            }
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
        }

        public void onSuccess()
        {
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED);
        }
    }
}
