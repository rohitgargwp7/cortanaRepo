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
        private static readonly IpManager instance = new IpManager();

        private IpManager()
        {
            string[] iplist = null;
            if (App.appSettings.TryGetValue(App.IP_LIST, out iplist) && iplist != null && iplist.Length > 0)
            {
                ProductionIps = iplist;
            }
        }

        byte count = 0;
        public static IpManager Instance
        {
            get
            {
                return instance;
            }
        }

        private string[] ProductionIps = 
        {   
           "54.251.180.0",
           "54.251.180.1",
           "54.251.180.2",
           "54.251.180.3",
           "54.251.180.4",
           "54.251.180.5",
           "54.251.180.6",
           "54.251.180.7"
       };

        Random _random = new Random();

        /// <summary>
        /// returns ip and if ip fails 5 times then it returns domain name
        /// </summary>
        /// <returns></returns>
        public string GetIp()
        {
            string ip = string.Empty;
            if (AccountUtils.IsProd)
            {
                if (count < 5)
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
