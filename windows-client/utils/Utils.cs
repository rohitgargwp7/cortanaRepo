using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.DbUtils;
using System;
using System.Diagnostics;
using System;
using System.Windows.Media;
using System.Windows;
using System.IO;
using Microsoft.Phone.Tasks;

namespace windows_client.utils
{
    public class Utils
    {

        private static Dictionary<string, List<GroupParticipant>> groupCache = null;

        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        public static Dictionary<string, List<GroupParticipant>> GroupCache
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

        public static GroupParticipant getGroupParticipant(string defaultName, string msisdn, string grpId)
        {
            if (grpId == null)
                return null;

            if (groupCache == null)
            {
                groupCache = new Dictionary<string, List<GroupParticipant>>();
            }
            if (groupCache.ContainsKey(grpId))
            {
                List<GroupParticipant> l = groupCache[grpId];
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].Msisdn == msisdn)
                    {
                        return l[i];
                    }
                }
            }
            ContactInfo cInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
            GroupParticipant gp = new GroupParticipant(grpId, cInfo != null ? cInfo.Name : string.IsNullOrWhiteSpace(defaultName) ? msisdn : defaultName, msisdn, cInfo != null ? cInfo.OnHike : true);
            if (groupCache.ContainsKey(grpId))
            {
                groupCache[grpId].Add(gp);
                App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
                return gp;
            }

            List<GroupParticipant> ll = new List<GroupParticipant>();
            ll.Add(gp);
            groupCache.Add(grpId, ll);
            App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            return gp;
        }

        public static void savedAccountCredentials(JObject obj)
        {
            App.MSISDN = (string)obj["msisdn"];
            AccountUtils.Token = (string)obj["token"];
            appSettings[App.MSISDN_SETTING] = App.MSISDN;
            appSettings[App.UID_SETTING] = (string)obj["uid"];
            appSettings[App.TOKEN_SETTING] = (string)obj["token"];
            appSettings[App.SMS_SETTING] = (int)obj[NetworkManager.SMS_CREDITS];
            appSettings[App.IS_PUSH_ENABLED] = (bool)true;
            appSettings[App.VIBRATE_PREF] = (bool)true;
            appSettings[App.LAST_UPDATE_CHECK_TIME] = (long)-1;
            appSettings[App.LAST_ANALYTICS_POST_TIME] = (long)TimeUtils.getCurrentTimeStamp();
            appSettings.Save();
        }

        public static bool isGroupConversation(string msisdn)
        {
            return !msisdn.StartsWith("+");
        }

        public static string defaultGroupName(string grpId)
        {

            List<GroupParticipant> groupParticipants = null;
            Utils.GroupCache.TryGetValue(grpId, out groupParticipants);
            if (groupParticipants == null || groupParticipants.Count == 0) // this should not happen as at this point cache should be populated
                return "GROUP";
            List<GroupParticipant> activeMembers = GetActiveGroupParticiants(grpId);
            if (activeMembers == null || groupParticipants.Count == 0)
                return "GROUP";
            switch (activeMembers.Count)
            {
                case 1:
                    return activeMembers[0].FirstName;
                case 2:
                    return activeMembers[0].FirstName + " and "
                    + activeMembers[1].FirstName;
                default:
                    return activeMembers[0].FirstName + " and "
                    + (activeMembers.Count - 1) + " others";
            }
        }

        public static List<GroupParticipant> GetActiveGroupParticiants(string groupId)
        {
            if (!Utils.GroupCache.ContainsKey(groupId) || Utils.GroupCache[groupId] == null)
                return null;
            List<GroupParticipant> activeGroupMembers = new List<GroupParticipant>(Utils.GroupCache[groupId].Count);
            for (int i = 0; i < Utils.GroupCache[groupId].Count; i++)
            {
                if (!Utils.GroupCache[groupId][i].HasLeft)
                    activeGroupMembers.Add(Utils.GroupCache[groupId][i]);
            }
            return activeGroupMembers;
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
        /**
       * Requests the server to send an account info packet
       */
        public static void requestAccountInfo()
        {
            Debug.WriteLine("Utils", "Requesting account info");
            JObject requestAccountInfo = new JObject();
            try
            {
                requestAccountInfo.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.REQUEST_ACCOUNT_INFO);
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, requestAccountInfo);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Utils", "Invalid JSON", e);
            }
        }

        public static ConvMessage[] splitUserJoinedMessage(ConvMessage convMessage)
        {
            string[] names = null;
            ConvMessage[] c = null;

            if (convMessage.Message.IndexOf(',') == -1) // only one name in message ex "abc joined the group chat"
            {
                int spaceIndex = convMessage.Message.IndexOf(" ");

                ConvMessage cm = new ConvMessage(convMessage.Message.Substring(0, spaceIndex) + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
                cm.GrpParticipantState = convMessage.GrpParticipantState;
                c = new ConvMessage[1];
                c[0] = cm;
                return c;
            }

            else
                names = convMessage.Message.Split(','); // ex : "a,b joined the group chat"

            c = new ConvMessage[names.Length];
            int i = 0;
            for (; i < names.Length - 1; i++)
            {
                c[i] = new ConvMessage(names[i] + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
                c[i].GrpParticipantState = convMessage.GrpParticipantState;
            }
            names[i] = names[i].Trim();
            int idx = names[i].IndexOf(" ");
            c[i] = new ConvMessage(names[i].Substring(0, idx) + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
            c[i].GrpParticipantState = convMessage.GrpParticipantState;
            return c;
        }

        public static bool isDarkTheme()
        {
            return ((Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"] == Visibility.Visible);
        }

        public void SerializeGroupCache()
        {
            string fileName = "GroupCacheFile";
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (var writer = new BinaryWriter(file))
                    {
                        int count = groupCache != null ? groupCache.Count : 0;
                        writer.Write(count);
                        if (count != 0)
                        {
                            foreach (string key in groupCache.Keys)
                            {
                                writer.Write(key);
                                List<GroupParticipant> l = groupCache[key];
                                int lcount = l != null ? l.Count : 0;
                                writer.Write(lcount);
                            }
                        }
                    }
                }
            }

        }

        public void DeSerializeGroupCache()
        {
            string fileName = "GroupCacheFile";
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        int count = reader.ReadInt32();
                    }
                }
            }
        }

        public static string[] splitUserJoinedMessage(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return null;
            char[] delimiters = new char[] { ',' };
            return msg.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
        }

        public static void TellAFriend()
        {

        }

        /***
         * returns 
         * -1 if v1 < v2
         * 1 is v1>v2
         * 0 if v1=v2
         * */
        public static int compareVersion(string version1, string version2)
        {
            string[] version1_parts = version1.Split('.');
            string[] version2_parts = version2.Split('.');
            int i;
            int min = version1_parts.Length < version2_parts.Length ? version1_parts.Length : version2_parts.Length;
            for (i = 0; i < min && version1_parts[i] == version2_parts[i]; i++) ;

            int v1, v2;
            if (version1_parts.Length == version2_parts.Length)
            {
                if (i == version2_parts.Length)
                    return 0;
                v1 = Convert.ToInt32(version1_parts[i]);
                v2 = Convert.ToInt32(version2_parts[i]);
            }
            else if (version1_parts.Length > version2_parts.Length)
            {
                v2 = 0;
                v1 = Convert.ToInt32(version1_parts[i]);
            }
            else
            {
                v1 = 0;
                v2 = Convert.ToInt32(version2_parts[i]);
            }
            if (v1 > v2)
                return 1;
            return -1;

        }

        public static bool isCriticalUpdatePending()
        {
            try
            {
                string lastCriticalVersion = "";
                App.appSettings.TryGetValue<string>(App.LAST_CRITICAL_VERSION, out lastCriticalVersion);
                if (String.IsNullOrEmpty(lastCriticalVersion))
                    return false;
                string currentVersion = Utils.GetVersion();
                return compareVersion(lastCriticalVersion, currentVersion) == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static string GetVersion()
        {
            Uri manifest = new Uri("WMAppManifest.xml", UriKind.Relative);
            var si = Application.GetResourceStream(manifest);
            if (si != null)
            {
                using (StreamReader sr = new StreamReader(si.Stream))
                {
                    bool haveApp = false;
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (!haveApp)
                        {
                            int i = line.IndexOf("AppPlatformVersion=\"", StringComparison.InvariantCulture);
                            if (i >= 0)
                            {
                                haveApp = true;
                                line = line.Substring(i + 20);
                                int z = line.IndexOf("\"");
                                if (z >= 0)
                                {
                                    // if you're interested in the app plat version at all                        
                                    // AppPlatformVersion = line.Substring(0, z);                      
                                }
                            }
                        }

                        int y = line.IndexOf("Version=\"", StringComparison.InvariantCulture);
                        if (y >= 0)
                        {
                            int z = line.IndexOf("\"", y + 9, StringComparison.InvariantCulture);
                            if (z >= 0)
                            {
                                // We have the version, no need to read on.                      
                                return line.Substring(y + 9, z - y - 9);
                            }
                        }
                    }
                }
            }

            return "Unknown";
        }
    }
}
