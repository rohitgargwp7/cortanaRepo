﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using windows_client.Misc;
using System.IO;
using System.IO.IsolatedStorage;
using System.Diagnostics;

namespace windows_client.Model
{
    public class Analytics : IBinarySerializable
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

        //pro Tips
        public static readonly string PRO_TIPS_DISMISSED = "tip_id";
        public static readonly string ENTER_TO_SEND = "enter_2_send";

        private Dictionary<string, int> eventMap = null;

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
                        {
                            instance = new Analytics();
                            instance.readObject();
                        }
                    }
                }
                return instance;
            }
        }

        private Analytics()
        {
            eventMap = new Dictionary<string, int>();
        }

        public void addEvent(string eventKey)
        {
            int currentValue = 0;
            if (eventMap.ContainsKey(eventKey))
            {
                currentValue = eventMap[eventKey];
            }
            eventMap[eventKey] = currentValue + 1;
        }

        public JObject serialize()
        {
            if (eventMap == null || eventMap.Count == 0)
                return null;
            JObject eventsData = new JObject();
            eventsData[HikeConstants.TAG] = utils.Utils.IsWP8 ? "wp8" : "wp7";
            foreach (KeyValuePair<string, int> entry in eventMap)
            {
                if (entry.Value > 0)
                {
                    eventsData[entry.Key] = entry.Value;
                }
            }
            JObject serializedJson = new JObject();
            serializedJson[HikeConstants.TYPE] = HikeConstants.LOG_EVENT;
            serializedJson[HikeConstants.DATA] = eventsData;
            return serializedJson;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(eventMap.Count);
            foreach (KeyValuePair<string, int> entry in eventMap)
            {
                writer.WriteString(entry.Key);
                writer.Write(entry.Value);
            }
        }

        public void Read(BinaryReader reader)
        {
            int count = 0;
            try // here end of stream error cound come
            {
                count = reader.ReadInt32();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Analytics :: Read : read Count, Exception : " + ex.StackTrace);
            }
            string key;
            int value = -1;
            /*
             * exception shud be handled for each element rather than complete set.
             * reason : there could be problem in 1 or 2 entries and hence all other entries should be recoreded
             * in the map.
             */
            for (int i = 0; i < count; i++)
            {
                try
                {
                    key = reader.ReadString();
                    value = reader.ReadInt32();
                    if (!String.IsNullOrEmpty(key) && value > 0)
                        eventMap[key] = value;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Analytics :: Read : read item, Exception : " + ex.StackTrace);
                }
            }
        }

        public void clearObject() //call after publish
        {
            if (eventMap != null)
                eventMap.Clear();
        }

        public void saveObject()
        {
            string filePath = HikeConstants.ANALYTICS_OBJECT_DIRECTORY + "/" + HikeConstants.ANALYTICS_OBJECT_FILE;
            if (eventMap != null && eventMap.Count > 0)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(HikeConstants.ANALYTICS_OBJECT_DIRECTORY))
                    {
                        store.CreateDirectory(HikeConstants.ANALYTICS_OBJECT_DIRECTORY);
                    }
                    using (var file = store.OpenFile(filePath, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            this.Write(writer);
                        }
                    }
                }
            }
        }

        private void readObject()
        {
            string filePath = HikeConstants.ANALYTICS_OBJECT_DIRECTORY + "/" + HikeConstants.ANALYTICS_OBJECT_FILE;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(HikeConstants.ANALYTICS_OBJECT_DIRECTORY) || !store.FileExists(filePath))
                {
                    return;
                }
                using (var file = store.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        this.Read(reader);
                    }
                }
            }
        }

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
            dataObj.Add(HikeConstants.SUB_TYPE, HikeConstants.UI_EVENT);

            JObject analyticObj = new JObject();
            analyticObj.Add(HikeConstants.DATA, dataObj);
            analyticObj.Add(HikeConstants.TYPE, HikeConstants.LOG_EVENT);

            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, analyticObj);
        }
    }
}
