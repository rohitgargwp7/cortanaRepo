using System;
using mqtttest.Client;

namespace windows_client.Mqtt
{
    public class PingCB : Callback
    {
        private HikeMqttManager hikeMqttManager;

        public PingCB(HikeMqttManager hikeMqttManager)
        {
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onFailure(Exception value)
        {
            if (hikeMqttManager.connectionStatus == windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED)
            {
                hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            }
            hikeMqttManager.disconnectFromBroker(true);
        }

        public void onSuccess()
        {
            //Log
        }

    }
}
