using System;
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
        }

        public void onSuccess()
        {
            //Log
        }


    }
}
