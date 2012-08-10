using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Collections.Generic;

namespace windows_client.utils
{
    public class Utils
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

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
    }
}
