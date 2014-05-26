using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.utils;

namespace windows_client.Mqtt
{
    class IpManager
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile IpManager instance = null;

        byte count = 0;
        public static IpManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new IpManager();
                        }
                    }
                }
                return instance;
            }
        }

        private string[] ProductionIps = 
        {   
            "54.251.135.51",
            "54.251.142.252",
            "54.251.180.2",
            "54.251.144.227",
            "54.251.144.219",
            "54.251.180.5",
            "54.251.180.3",
            "54.251.180.0",
            "54.251.150.240",
            "54.251.150.109",
            "54.251.145.59",
            "54.251.144.144",
            "54.251.180.1",
            "54.251.180.4",
            "54.251.144.159",
            "54.251.180.6",
            "54.251.144.159",
            "54.251.180.6"
       };

        Random _random = new Random();
        public string GetIp()
        {
            bool mqttDmqttToggle = true;
            App.appSettings.TryGetValue<bool>(App.MQTT_DMQTT_SETTING, out mqttDmqttToggle);
            string ip = string.Empty;

            if (AccountUtils.IsProd)
            {
                if (count < 5 && mqttDmqttToggle)
                {
                    ip = ProductionIps[_random.Next(ProductionIps.Length)];
                    count++;
                }
                else
                {
                    ip = AccountUtils.MQTT_HOST;
                    count = 0;
                }
            }
            else
                ip = AccountUtils.MQTT_HOST;

            return ip;
        }


        public void ResetIp()
        {
            count = 0;
        }
    }
}
