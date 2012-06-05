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
            hikeMqttManager.connect();
        }

        public void onSuccess()
        {
            //Log
        }

    }
}
