using System;
using mqtttest.Client;
using System.Windows;
using windows_client.DbUtils;
using Microsoft.Phone.Notification;
using Microsoft.Phone.Reactive;

namespace windows_client.Mqtt
{
    public class ConnectCB : Callback
    {
        private HikeMqttManager hikeMqttManager;
        private IScheduler scheduler = Scheduler.NewThread;


        public ConnectCB(HikeMqttManager hikeMqttManager)
        {
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onFailure(Exception value)
        {
            if ((value is ConnectionException) && ((ConnectionException)value).getCode().Equals(finalmqtt.Msg.ConnAckMessage.ConnectionStatus.BAD_USERNAME_OR_PASSWORD))
            {
                bool isPresent = false;
                if (App.appSettings.Contains(App.IS_DB_CREATED))
                    isPresent = true;
                App.ClearAppSettings();
                if (isPresent)
                    App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
                NetworkManager.turnOffNetworkManager = true; // stop network manager
                App.MqttManagerInstance.disconnectFromBroker(false);
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
                App.HikePubSubInstance.publish(HikePubSub.BAD_USER_PASS, null);
            }
            else if(hikeMqttManager.connectionStatus != HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET)
            {
                scheduler.Schedule(hikeMqttManager.connect, TimeSpan.FromSeconds(5));
            }
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
        }

        public void onSuccess()
        {
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED);
        }
    }
}
