using System;
using mqtttest.Client;

namespace windows_client.Mqtt
{
    public class DisconnectCB : Callback
    {

        private bool reconnect;
        private HikeMqttManager hikeMqttManager;

        public DisconnectCB(bool reconnect, HikeMqttManager hikeMqttManager)
        {
            this.reconnect = reconnect;
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onSuccess()
        {

            if (hikeMqttManager.mqttConnection != null)
            {
                //				mqttConnection.listener(CallbackConnection.DEFAULT_LISTENER);
            }

            hikeMqttManager.setConnectionStatus(HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            if (reconnect)
            {
                hikeMqttManager.connect();
            }
        }

        public void onFailure(Exception value)
        {
            hikeMqttManager.setConnectionStatus(HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
            if (hikeMqttManager.mqttConnection != null)
            {

            }

            if (reconnect)
            {
                hikeMqttManager.connect();
            }
        }
    }
}
