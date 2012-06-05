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
                //clear phone num & other prefs
            }
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_UNKNOWNREASON);
        }

        public void onSuccess()
        {
            hikeMqttManager.setConnectionStatus(windows_client.Mqtt.HikeMqttManager.MQTTConnectionStatus.CONNECTED);

        }
    }
}
