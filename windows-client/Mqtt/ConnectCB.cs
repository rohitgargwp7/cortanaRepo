﻿using System;
using mqtttest.Client;

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
                bool isPresent =false;
                if(App.appSettings.Contains(App.IS_DB_CREATED))
                    isPresent = true;
                App.ClearAppSettings();
                if(isPresent)
                    App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            }
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
        }

        public void onSuccess()
        {
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED);

        }
    }
}
