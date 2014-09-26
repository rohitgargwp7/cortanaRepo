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

            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.IP_LIST, out iplist) && iplist != null && iplist.Length > 0)
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
        /// fetch mqtt ip and port to connect
        /// </summary>
        /// <param name="ip"> returns ip and if ip fails 5 times then it returns domain name </param>
        /// <param name="port">returns port 8080 and if it fails to connect then returns 5222</param>
        public void GetIpAndPort(out string ip, out  int port)
        {
            ip = string.Empty;
            port = AccountUtils.MQTT_PORT;
            if (AccountUtils.AppEnvironment == AccountUtils.DebugEnvironment.PRODUCTION)
            {
                //try for port 8080 once and if it fails then fallback to xmpp (5222)
                if (count > 0)//todo:check for wifi
                    port = HikeConstants.ServerUrls.ProductionUrls.MQTT_PRODUCTION_XMPP_PORT;
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
        }

        public void ResetIp()
        {
            count = 0;
        }
    }
}
