using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace windows_client.Model
{
    public class Analytics
    {
        //coSc = conversations screen

        public static readonly string DELETE_ALL_CHATS = "coDelAll";
        public static readonly string COMPOSE = "coCom";

        //prSc = profile screen
        public static readonly string FREE_SMS = "prFrS";
        public static readonly string INVITE = "prInv";
        public static readonly string SETTINGS = "prSet";
        public static readonly string HELP = "prHlp";
        public static readonly string EDIT_PROFILE = "prEdtPr";

        private Dictionary<string, int> eventMap = new Dictionary<string, int>();

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
            JObject eventsData = new JObject();
            eventsData["tag"] = "mob";
            foreach(KeyValuePair<string,int> entry in eventMap)
            {
                if (entry.Value != 0)
                {
                    eventsData[entry.Key] = entry.Value;
                }
            }
            JObject serializedJson = new JObject();
            serializedJson[HikeConstants.TYPE] = HikeConstants.LOG_EVENT;
            serializedJson[HikeConstants.DATA] = eventsData;
            return serializedJson;
        }

    }
}
