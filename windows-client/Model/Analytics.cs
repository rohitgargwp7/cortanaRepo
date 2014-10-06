using System;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using CommonLibrary.Constants;
using windows_client.Misc;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;
using windows_client.utils;

namespace windows_client.Model
{
    public class Analytics
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile Analytics instance = null;

        public static Analytics Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Analytics();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// Send click event analytics packet to server
        /// </summary>
        /// <param name="key">analytics key</param>
        public static void SendClickEvent(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            JObject metadataObject = new JObject();
            metadataObject.Add(HikeConstants.AnalyticsKeys.EVENT_TYPE, HikeConstants.AnalyticsKeys.EVENT_TYPE_CLICK);
            metadataObject.Add(HikeConstants.AnalyticsKeys.EVENT_KEY, key);

            JObject dataObj = new JObject();
            dataObj.Add(ServerJsonKeys.METADATA, metadataObject);
            dataObj.Add(ServerJsonKeys.TAG, ServerJsonKeys.TAG_MOBILE);
            dataObj.Add(ServerJsonKeys.SUB_TYPE, ServerJsonKeys.ST_UI_EVENT);

            JObject analyticObj = new JObject();
            analyticObj.Add(ServerJsonKeys.DATA, dataObj);
            analyticObj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.LOG_EVENT);

            if (HikeInstantiation.HikePubSubInstance != null)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticObj);
        }

        /// <summary>
        /// Send analytics packet to server
        /// </summary>
        /// <param name="eventType">type of event</param>
        /// <param name="key">analytics key</param>
        public static void SendAnalyticsEvent(string eventType, string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(eventType))
                return;

            JObject metadataObject = new JObject();
            metadataObject.Add(HikeConstants.AnalyticsKeys.EVENT_KEY, key);

            JObject dataObj = new JObject();
            dataObj.Add(ServerJsonKeys.METADATA, metadataObject);
            dataObj.Add(ServerJsonKeys.TAG, ServerJsonKeys.TAG_MOBILE);
            dataObj.Add(ServerJsonKeys.SUB_TYPE, eventType);

            JObject analyticObj = new JObject();
            analyticObj.Add(ServerJsonKeys.DATA, dataObj);
            analyticObj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.LOG_EVENT);

            if (HikeInstantiation.HikePubSubInstance != null)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticObj);
        }

        /// <summary>
        /// Send analytics packet to server
        /// </summary>
        /// <param name="eventType">type of event</param>
        /// <param name="key">analytics key</param>
        /// <param name="value">value to be sent</param>
        public static void SendAnalyticsEvent(string eventType, string key, JToken value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            JObject analyticsJson = new JObject();
            analyticsJson.Add(key, value);

            JObject data = new JObject();
            data.Add(ServerJsonKeys.METADATA, analyticsJson);
            data.Add(ServerJsonKeys.SUB_TYPE, eventType);
            data[ServerJsonKeys.TAG] = ServerJsonKeys.TAG_MOBILE;

            JObject jsonObj = new JObject();
            jsonObj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.LOG_EVENT);
            jsonObj.Add(ServerJsonKeys.DATA, data);

            if (HikeInstantiation.HikePubSubInstance != null)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, jsonObj);
        }

        public static void SendAnalyticsEvent(string subtype, string eventType, string eventKey, string msisdn)
        {
            if (String.IsNullOrEmpty(subtype) || String.IsNullOrEmpty(eventType) || String.IsNullOrEmpty(eventKey) || String.IsNullOrEmpty(msisdn))
                return;

            JObject metadata = new JObject();
            metadata.Add(HikeConstants.AnalyticsKeys.EVENT_TYPE, eventType);
            metadata.Add(HikeConstants.AnalyticsKeys.EVENT_KEY, eventKey);
            metadata.Add(HikeConstants.NokiaHere.CONTEXT, msisdn);

            long ts = TimeUtils.getCurrentTimeStamp();

            JObject data = new JObject();
            data.Add(ServerJsonKeys.SUB_TYPE, subtype);
            data.Add(HikeConstants.AnalyticsKeys.CLIENT_TIMESTAMP, ts);
            data.Add(ServerJsonKeys.METADATA, metadata);
            data.Add(ServerJsonKeys.TAG, ServerJsonKeys.TAG_MOBILE);
            data.Add(ServerJsonKeys.MESSAGE_ID, ts);

            JObject analyticsJson = new JObject();
            analyticsJson.Add(ServerJsonKeys.DATA,data);
            analyticsJson.Add(ServerJsonKeys.TYPE, ServerJsonKeys.LOG_EVENT);

            if (HikeInstantiation.HikePubSubInstance != null)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticsJson);
        }
    }
}
