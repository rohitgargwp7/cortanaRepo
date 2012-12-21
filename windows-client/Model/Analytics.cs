using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using windows_client.Misc;
using System.IO;
using System.IO.IsolatedStorage;

namespace windows_client.Model
{
    public class Analytics : IBinarySerializable
    {
        //co = conversations screen
        public static readonly string DELETE_ALL_CHATS = "coDelAll";
        public static readonly string COMPOSE = "coCom";
        public static readonly string GROUP_CHAT = "coGrp";

        //pr = profile screen
        public static readonly string FREE_SMS = "prFrS";
        public static readonly string INVITE = "prInv";
        public static readonly string PRIVACY = "prPrvc";
        public static readonly string SETTINGS = "prSet";
        public static readonly string HELP = "prHlp";
        public static readonly string EDIT_PROFILE = "prEdtPr";

        //in = invite Screen
        public static readonly string INVITE_SOCIAL = "inSo";
        public static readonly string INVITE_EMAIL = "inEm";
        public static readonly string INVITE_MESSAGE = "inMsg";

        //su = select user
        public static readonly string REFRESH_CONTACTS = "suRC";

        //ct = chat thread
        public static readonly string GROUP_INFO = "ctGI";

        //hp = help
        public static readonly string FAQS = "hpFAQ";
        public static readonly string CONTACT_US = "hpCU";
        public static readonly string LEGAL = "hpLgl";
        public static readonly string WALKTHROUGH = "hpWt";
        public static readonly string RATE_APP = "hpRA";

        //gi = group info
        public static readonly string INVITE_SMS_PARTICIPANTS = "giInv";


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
            eventsData["tag"] = "wp7";
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
            catch
            {
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
                catch
                {
                }
            }
        }

        public void clearObject() //call after publish
        {
            if(eventMap != null)
                eventMap.Clear();
        }

        public void saveObject()
        {
            string filePath = HikeConstants.ANALYTICS_OBJECT_DIRECTORY + "/" + HikeConstants.ANALYTICS_OBJECT_FILE;
            if (eventMap != null && eventMap.Count > 0)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
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
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
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
    }
}
