using CommonLibrary.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Utils
{
    public class ConnectionUtility
    {
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
                    return HikeConstants.ServerUrls.ProductionUrls.MQTT_HOST;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.MQTT_HOST;
                else
                    return HikeConstants.ServerUrls.StagingUrls.MQTT_HOST;
            }
        }

        public static int MQTT_PORT
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return HikeConstants.ServerUrls.ProductionUrls.MQTT_PORT;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.MQTT_PORT;
                else
                    return HikeConstants.ServerUrls.StagingUrls.MQTT_PORT;
            }
        }

        #endregion

        public static string FILE_TRANSFER_BASE
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return String.Format("http://{0}:{1}/v1", HikeConstants.ServerUrls.ProductionUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return String.Format("http://{0}:{1}/v1", HikeConstants.ServerUrls.DevUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
                else
                    return String.Format("http://{0}:{1}/v1", HikeConstants.ServerUrls.StagingUrls.FILE_TRANSFER_HOST, Convert.ToString(PORT));
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
                    return HikeConstants.ServerUrls.ProductionUrls.HOST;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.HOST;
                else
                    return HikeConstants.ServerUrls.StagingUrls.HOST;
            }
        }

        public static int PORT
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return HikeConstants.ServerUrls.ProductionUrls.PORT;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.PORT;
                else
                    return HikeConstants.ServerUrls.StagingUrls.PORT;
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
                    return HikeConstants.ServerUrls.ProductionUrls.UPDATE_URL;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.UPDATE_URL;
                else
                    return HikeConstants.ServerUrls.StagingUrls.UPDATE_URL;
            }
        }

        public static string GetStickerUrl
        {
            get
            {
                if (AppEnvironment == DebugEnvironment.PRODUCTION)
                    return HikeConstants.ServerUrls.ProductionUrls.STICKER_URL;
                else if (AppEnvironment == DebugEnvironment.DEV)
                    return HikeConstants.ServerUrls.DevUrls.STICKER_URL;
                else
                    return HikeConstants.ServerUrls.StagingUrls.STICKER_URL;
            }
        }
    }
}
