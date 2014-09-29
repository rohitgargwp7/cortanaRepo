using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
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
            dataObj.Add(HikeConstants.ServerJsonKeys.METADATA, metadataObject);
            dataObj.Add(HikeConstants.ServerJsonKeys.TAG, HikeConstants.ServerJsonKeys.TAG_MOBILE);
            dataObj.Add(HikeConstants.ServerJsonKeys.SUB_TYPE, HikeConstants.ServerJsonKeys.ST_UI_EVENT);

            JObject analyticObj = new JObject();
            analyticObj.Add(HikeConstants.ServerJsonKeys.DATA, dataObj);
            analyticObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.LOG_EVENT);

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
            dataObj.Add(HikeConstants.ServerJsonKeys.METADATA, metadataObject);
            dataObj.Add(HikeConstants.ServerJsonKeys.TAG, HikeConstants.ServerJsonKeys.TAG_MOBILE);
            dataObj.Add(HikeConstants.ServerJsonKeys.SUB_TYPE, eventType);

            JObject analyticObj = new JObject();
            analyticObj.Add(HikeConstants.ServerJsonKeys.DATA, dataObj);
            analyticObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.LOG_EVENT);

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
            data.Add(HikeConstants.ServerJsonKeys.METADATA, analyticsJson);
            data.Add(HikeConstants.ServerJsonKeys.SUB_TYPE, eventType);
            data[HikeConstants.ServerJsonKeys.TAG] = HikeConstants.ServerJsonKeys.TAG_MOBILE;

            JObject jsonObj = new JObject();
            jsonObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.LOG_EVENT);
            jsonObj.Add(HikeConstants.ServerJsonKeys.DATA, data);

            if (HikeInstantiation.HikePubSubInstance != null)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, jsonObj);
        }
    }
}
