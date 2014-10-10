using System;
using mqtttest.Client;
using CommonLibrary.DbUtils;
using Microsoft.Phone.Notification;
using CommonLibrary.utils;
using CommonLibrary.Constants;
using CommonLibrary.Lib;
using CommonLibrary.Misc;

namespace CommonLibrary.Mqtt
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

                if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.IS_DB_CREATED))
                    isPresent = true;

                HikeInstantiation.ClearAppSettings();
                
                if (isPresent)
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.IS_DB_CREATED, true);

                NetworkManager.turnOffNetworkManager = true; // stop network manager
                HikeInstantiation.MqttManagerInstance.DisconnectFromBroker(false);
                MiscDBUtil.clearDatabase();
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

            hikeMqttManager.setConnectionStatus(CommonLibrary.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
        }

        public void onSuccess()
        {
            hikeMqttManager.setConnectionStatus(CommonLibrary.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED);
        }
    }
}
