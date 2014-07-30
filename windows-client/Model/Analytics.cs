using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using windows_client.Misc;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;

namespace windows_client.Model
{
    public class Analytics
    {
        //co = conversations screen
        public static readonly string DELETE_ALL_CHATS = "coDelAll";
        public static readonly string COMPOSE = "coCom";
        public static readonly string GROUP_CHAT = "coGrp";
        public static readonly string ADD_FAVS_FROM_FAV_REQUEST = "coATFReq";
        public static readonly string ADD_FAVS_CONTEXT_MENU_CONVLIST = "coATFCM";
        public static readonly string REMOVE_FAVS_CONTEXT_MENU_CONVLIST = "coRFFCM";

        //pr = profile screen
        public static readonly string FREE_SMS = "prFrS";
        public static readonly string INVITE = "prInv";
        public static readonly string SETTINGS = "prSet";
        public static readonly string HELP = "prHlp";
        public static readonly string EDIT_PROFILE = "prEdtPr";
        public static readonly string REWARDS = "prRew";

        //st = settingsScreen

        public static readonly string PREFERENCES = "stPref";
        public static readonly string NOTIFICATIONS = "stNot";
        public static readonly string ACCOUNT = "stPriv";
        public static readonly string BLOCKLIST = "stBlk";

        //in = invite Screen
        public static readonly string INVITE_SOCIAL = "inSo";
        public static readonly string INVITE_EMAIL = "inEm";
        public static readonly string INVITE_MESSAGE = "inMsg";

        //inu = invite users
        public static readonly string ADD_FAVS_INVITE_USERS = "inuATF";

        //su = select user
        public static readonly string REFRESH_CONTACTS = "suRC";

        //ct = chat thread
        public static readonly string GROUP_INFO = "ctGI";
        public static readonly string ADD_TO_FAVS_APP_BAR_CHATTHREAD = "ctATFAB";
        public static readonly string REMOVE_FAVS_CONTEXT_MENU_CHATTHREAD = "ctRFFAB";
        public static readonly string SEE_LARGE_PROFILE_PIC = "ctLPP"; //chat thread large profile pic
        public static readonly string SEE_LARGE_PROFILE_PIC_FROM_USERPROFILE = "upLPP"; //chat thread large profile pic

        //hp = help
        public static readonly string FAQS = "hpFAQ";
        public static readonly string CONTACT_US = "hpCU";
        public static readonly string LEGAL = "hpLgl";
        public static readonly string WALKTHROUGH = "hpWt";
        public static readonly string RATE_APP = "hpRA";

        //gi = group info
        public static readonly string INVITE_SMS_PARTICIPANTS = "giInv";
        public static readonly string ADD_FAVS_CONTEXT_MENU_GROUP_INFO = "giATFCM";
        public static readonly string REMOVE_FAVS_CONTEXT_MENU_GROUP_INFO = "giRFFCM";

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
            metadataObject.Add(HikeConstants.EVENT_TYPE, HikeConstants.EVENT_TYPE_CLICK);
            metadataObject.Add(HikeConstants.EVENT_KEY, key);

            JObject dataObj = new JObject();
            dataObj.Add(HikeConstants.METADATA, metadataObject);
            dataObj.Add(HikeConstants.TAG, HikeConstants.TAG_MOBILE);
            dataObj.Add(HikeConstants.SUB_TYPE, HikeConstants.ST_UI_EVENT);

            JObject analyticObj = new JObject();
            analyticObj.Add(HikeConstants.DATA, dataObj);
            analyticObj.Add(HikeConstants.TYPE, HikeConstants.LOG_EVENT);

            if (App.HikePubSubInstance != null)
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticObj);
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
            metadataObject.Add(HikeConstants.EVENT_KEY, key);

            JObject dataObj = new JObject();
            dataObj.Add(HikeConstants.METADATA, metadataObject);
            dataObj.Add(HikeConstants.TAG, HikeConstants.TAG_MOBILE);
            dataObj.Add(HikeConstants.SUB_TYPE, eventType);

            JObject analyticObj = new JObject();
            analyticObj.Add(HikeConstants.DATA, dataObj);
            analyticObj.Add(HikeConstants.TYPE, HikeConstants.LOG_EVENT);

            if (App.HikePubSubInstance != null)
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticObj);
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
            data.Add(HikeConstants.METADATA, analyticsJson);
            data.Add(HikeConstants.SUB_TYPE, eventType);
            data[HikeConstants.TAG] = HikeConstants.TAG_MOBILE;

            JObject jsonObj = new JObject();
            jsonObj.Add(HikeConstants.TYPE, HikeConstants.LOG_EVENT);
            jsonObj.Add(HikeConstants.DATA, data);

            if (App.HikePubSubInstance != null)
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, jsonObj);
        }
    }
}
