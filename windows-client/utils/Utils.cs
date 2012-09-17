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

namespace windows_client.utils
{
    public class Utils
    {
        public class Group<T> : IEnumerable<T>
        {
            public Group(string name, List<T> items)
            {
                this.Title = name;
                this.Items = items;
            }

            public override bool Equals(object obj)
            {
                Group<T> that = obj as Group<T>;

                return (that != null) && (this.Title.Equals(that.Title));
            }
            public override int GetHashCode()
            {
                return this.Title.GetHashCode();
            }
            public string Title
            {
                get;
                set;
            }

            public List<T> Items
            {
                get;
                set;
            }
            public bool HasItems
            {
                get
                {
                    return (Items == null || Items.Count == 0) ? false : true;
                }
            }

            /// <summary>
            /// This is used to colour the tiles - greying out those that have no entries
            /// </summary>
            public Brush GroupBackgroundBrush
            {
                get
                {
                    return (SolidColorBrush)Application.Current.Resources[(HasItems) ? "PhoneAccentBrush" : "PhoneChromeBrush"];
                }
            }
            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return this.Items.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.Items.GetEnumerator();
            }

            #endregion
        }

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
        public static GroupParticipant getGroupParticipant(string defaultName, string msisdn,string grpId)
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
                        if (l[i].HasLeft)
                            l[i].HasLeft = false;
                        return l[i];
                    }
                }
            }
            ContactInfo cInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
            GroupParticipant gp = new GroupParticipant(grpId,cInfo != null ? getFirstName(cInfo.Name) : defaultName, msisdn, cInfo != null ? cInfo.OnHike : true);
            if (groupCache.ContainsKey(grpId))
            {
                groupCache[grpId].Add(gp);
                return gp;
            }
            
            List<GroupParticipant> ll = new List<GroupParticipant>();
            ll.Add(gp);
            groupCache.Add(grpId, ll);
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
            appSettings.Save();
        }

        public static bool isGroupConversation(string msisdn)
        {
            return !msisdn.StartsWith("+");
        }

        public static string defaultGroupName(string grpId)
        {
            
            List<GroupParticipant> groupParticipants = null;
            Utils.GroupCache.TryGetValue(grpId,out groupParticipants);
            if (groupParticipants == null || groupParticipants.Count == 0) // this should not happen as at this point cache should be populated
                return "GROUP";
           
            switch (groupParticipants.Count)
            {
                case 1:
                    return Utils.getFirstName(groupParticipants[0].Name);
                case 2:
                    return Utils.getFirstName(groupParticipants[0].Name) + " and "
                    + Utils.getFirstName(groupParticipants[1].Name);
                default:
                    return Utils.getFirstName(groupParticipants[0].Name) + " and "
                    + (groupParticipants.Count) + " others";
            }
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

        public static ConvMessage [] splitUserJoinedMessage(ConvMessage convMessage)
        {
            string[] names= null;
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
            for (; i < names.Length-1; i++)
            {
                c[i] = new ConvMessage(names[i] + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
                c[i].GrpParticipantState = convMessage.GrpParticipantState;
            }
            names[i] = names[i].Trim();
            int idx = names[i].IndexOf(" ");
            c[i] = new ConvMessage(names[i].Substring(0,idx) + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
            c[i].GrpParticipantState = convMessage.GrpParticipantState;
            return c;
        }

        public static string getFirstName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            name = name.Trim();
            int idx = name.IndexOf(" ");
            if (idx != -1)
                return name.Substring(0, idx);
            else
                return name;
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
                        int count = groupCache !=null? groupCache.Count:0;
                        writer.Write(count);
                        if(count !=0)
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

    }
}
