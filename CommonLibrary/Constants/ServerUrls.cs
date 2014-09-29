using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Constants
{
    public static class ServerUrls
    {
        public static readonly string APP_ENVIRONMENT_SETTING = "appEnv";
        public static readonly string TERMS_AND_CONDITIONS = "http://hike.in/terms/wp8";
        public static readonly string FAQS_LINK = "http://get.hike.in/help/wp8/index.html";
        public static readonly string CONTACT_US_EMAIL = "support@hike.in";
        public static readonly string SYSTEM_HEALTH_LINK = "http://twitter.com/hikestatus/";

        public static class ProductionUrls
        {
            public static readonly string HOST = "api.im.hike.in";
            public static readonly int PORT = 80;
            public static readonly string MQTT_HOST = "mqtt.im.hike.in";
            public static readonly int MQTT_PRODUCTION_XMPP_PORT = 5222;
            public static readonly int MQTT_PORT = 8080;
            public static readonly string FILE_TRANSFER_HOST = "ft.im.hike.in";
            public static readonly string UPDATE_URL = "http://get.hike.in/updates/wp8";
            public static readonly string STICKER_URL = "http://hike.in/s/";
        }

        public static class DevUrls
        {
            public static readonly string HOST = "staging2.im.hike.in";
            public static readonly int PORT = 8080;
            public static readonly string MQTT_HOST = "staging2.im.hike.in";
            public static readonly int MQTT_PORT = 1883;
            public static readonly string FILE_TRANSFER_HOST = "staging2.im.hike.in";
            public static readonly string UPDATE_URL = "http://staging2.im.hike.in:8080/updates/wp8";
            public static readonly string STICKER_URL = "http://staging2.im.hike.in/s/";
        }

        public static class StagingUrls
        {
            public static readonly string HOST = "staging.im.hike.in";
            public static readonly int PORT = 8080;
            public static readonly string MQTT_HOST = "staging.im.hike.in";
            public static readonly int MQTT_PORT = 1883;
            public static readonly string FILE_TRANSFER_HOST = "staging.im.hike.in";
            public static readonly string UPDATE_URL = "http://staging.im.hike.in:8080/updates/wp8";
            public static readonly string STICKER_URL = "http://staging.im.hike.in/s/";
        }

        #region Environment enum

        public enum DebugEnvironment
        {
            STAGING,
            DEV,
            PRODUCTION
        }

        #endregion

        private static DebugEnvironment _appEnvironment;

        public static DebugEnvironment AppEnvironment
        {
            get
            {
                if (HikeInitManager.IsMarketplace)
                    return DebugEnvironment.PRODUCTION;
                else
                    return _appEnvironment;
            }
            set
            {
                _appEnvironment = value;
            }
        }

        #region MQTT RELATED

        public static string MQTT_HOST
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.MQTT_HOST;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.MQTT_HOST;
                else
                    return ServerUrls.StagingUrls.MQTT_HOST;
            }
        }

        public static int MQTT_PORT
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.MQTT_PORT;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.MQTT_PORT;
                else
                    return ServerUrls.StagingUrls.MQTT_PORT;
            }
        }

        #endregion

        public static string FILE_TRANSFER_BASE
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return String.Format("http://{0}:{1}/v1", ServerUrls.ProductionUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return String.Format("http://{0}:{1}/v1", ServerUrls.DevUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
                else
                    return String.Format("http://{0}:{1}/v1", ServerUrls.StagingUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
            }
        }

        public static string FILE_TRANSFER_BASE_URL
        {
            get
            {
                return FILE_TRANSFER_BASE + "/user/ft";
            }
        }

        public static string PARTIAL_FILE_TRANSFER_BASE_URL
        {
            get
            {
                return FILE_TRANSFER_BASE + "/user/pft/";
            }
        }

        public static string HOST
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.HOST;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.HOST;
                else
                    return ServerUrls.StagingUrls.HOST;
            }
        }

        public static int PORT
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.PORT;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.PORT;
                else
                    return ServerUrls.StagingUrls.PORT;
            }
        }

        public static string BASE
        {
            get
            {
                return "http://" + HOST + ":" + Convert.ToString(PORT) + "/v1";
            }
        }

        public static string AVATAR_BASE
        {
            get
            {
                return "http://" + HOST + ":" + Convert.ToString(PORT);
            }
        }

        public static string GetUpdateUrl
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.UPDATE_URL;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.UPDATE_URL;
                else
                    return ServerUrls.StagingUrls.UPDATE_URL;
            }
        }

        public static string GetStickerUrl
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return ServerUrls.ProductionUrls.STICKER_URL;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return ServerUrls.DevUrls.STICKER_URL;
                else
                    return ServerUrls.StagingUrls.STICKER_URL;
            }
        }
    }
}
