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
    public class SubscribeCB : Callback
    {
        private HikeMqttManager hikeMqttManager;

        public SubscribeCB(HikeMqttManager hikeMqttManager)
        {
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onFailure(Exception value)
        {
            hikeMqttManager.ping();
        }

        public void onSuccess()
        {
            //Log
        }


    }
}
