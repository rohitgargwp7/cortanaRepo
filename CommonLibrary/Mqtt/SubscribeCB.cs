using System;
using mqtttest.Client;

namespace CommonLibrary.Mqtt
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
