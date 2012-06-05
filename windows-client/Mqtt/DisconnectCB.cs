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
                hikeMqttManager.connectToBroker();
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
                hikeMqttManager.connectToBroker();
            }
        }
    }
}
