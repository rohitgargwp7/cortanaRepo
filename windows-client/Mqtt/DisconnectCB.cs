using System;
using mqtttest.Client;
using System.Diagnostics;

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
            Debug.WriteLine("Disconnect callback success called");
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
            Debug.WriteLine("Disconnect callback failure called");

            if (reconnect)
            {
                hikeMqttManager.connect();
            }
        }
    }
}
