using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.DbUtils;
using System;

namespace windows_client.utils
{
    public class Utils
    {
        private static Dictionary<string, GroupParticipant> groupCache = null;
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static Dictionary<string, GroupParticipant> GroupCache
        {
            get
            {
                return groupCache;
            }
            set
            {
                if (value != groupCache)
                    groupCache = value;
            }
        }
        public static GroupParticipant getGroupParticipant(string name,string msisdn)
        {
            if (groupCache.ContainsKey(msisdn))
                return groupCache[msisdn];
            ContactInfo cInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
            GroupParticipant gp =  new GroupParticipant(cInfo != null?cInfo.Name:name,msisdn,cInfo != null?cInfo.OnHike:true);
            groupCache.Add(msisdn, gp);
            // App.appSettings[App.GROUPS_CACHE] = groupCache; Doing this while app is closing
            return gp;
        }

        public static void savedAccountCredentials(JObject obj)
        {
            AccountUtils.Token = (string)obj["token"];
            appSettings[App.MSISDN_SETTING] = (string)obj["msisdn"];
            appSettings[App.UID_SETTING] = (string)obj["uid"];
            appSettings[App.TOKEN_SETTING] = (string)obj["token"];
            appSettings[App.SMS_SETTING] = (int)obj[NetworkManager.SMS_CREDITS];
            appSettings.Save();
        }

        public static bool isGroupConversation(string msisdn)
        {
            return !msisdn.StartsWith("+");
        }

        public static string defaultGroupName(List<GroupMembers> participantList)
        {
            if (participantList == null || participantList.Count == 0)
            {
                return "Group";
            }
            List<GroupMembers> groupParticipants = new List<GroupMembers>();
            for (int i = 0; i < participantList.Count; i++)
            {
                if (!participantList[i].HasLeft)
                {
                    groupParticipants.Add(participantList[i]);
                }
            }
            //groupParticipants.Sort();   // TODO IMPLEMENT SORT

            switch (groupParticipants.Count)
            {
                case 0:
                    return "";
                case 1:
                    return groupParticipants[0].Name;
                case 2:
                    return groupParticipants[0].Name + " and "
                    + groupParticipants[1].Name;
                default:
                    return groupParticipants[0].Name + " and "
                    + (groupParticipants.Count - 1) + " others";
            }
        }

        public static List<GroupMembers> getGroupMemberList(JObject jsonObject)
        {
            if (jsonObject == null)
                return null;
            JArray array = (JArray)jsonObject[HikeConstants.DATA];
            List<GroupMembers> gmList = new List<GroupMembers>(array.Count);
            for (int i = 0; i < array.Count; i++)
            {
                JObject nameMsisdn = (JObject)array[i];
                string contactNum = (string)nameMsisdn[HikeConstants.MSISDN];
                string contactName = (string)nameMsisdn[HikeConstants.NAME];
                GroupMembers gm = new GroupMembers((string)jsonObject[HikeConstants.TO], contactNum, contactName);
                gmList.Add(gm);
            }
            return gmList;
        }

        public static int CompareByName<T>(T a, T b)
        {
            string name1 = a.ToString();
            string name2 = b.ToString();
            if (String.IsNullOrEmpty(name1))
            {
                if (String.IsNullOrEmpty(name2))
                {
                    return 0;
                }
                //b is greater
                return -1;
            }
            else
            {
                if (String.IsNullOrEmpty(name2))
                {
                    //a is greater
                    return 1;
                }
            }
            if (name1.StartsWith("+"))
            {
                if (name2.StartsWith("+"))
                {
                    return name1.CompareTo(name2);
                }
                return -1;
            }
            else
            {
                if (name2.StartsWith("+"))
                {
                    return 1;
                }
                return name1.CompareTo(name2);
            }
        }
    }
}
