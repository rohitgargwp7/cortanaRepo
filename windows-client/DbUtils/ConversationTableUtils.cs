using System;
using windows_client.Model;
using System.Linq;
using System.Collections.Generic;
using windows_client.utils;
using windows_client.View;
using System.Data.Linq;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;

namespace windows_client.DbUtils
{
    public class ConversationTableUtils
    {
        public static string CONVERSATIONS_DIRECTORY = "CONVERSATIONS";
        private static object lockObj = new object();
        /* This function gets all the conversations shown on the message list page*/
        public static List<ConversationListObject> getAllConversations()
        {
            byte[] data = null;
            List<ConversationListObject> convList = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(CONVERSATIONS_DIRECTORY))
                    return null;
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                if (files == null)
                    return null;
                convList = new List<ConversationListObject>(files.Length);
                foreach (string fileName in files)
                {
                    using (var file = store.OpenFile(CONVERSATIONS_DIRECTORY + "\\" + fileName, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            ConversationListObject co = new ConversationListObject();
                            co.Read(reader);
                            convList.Add(co);
                        }
                    }
                }
            }
            convList.Sort();
            return convList;
            /*
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring+";Max Buffer Size=1024"))
            {
                var q = from o in DbCompiledQueries.chatsDbContext.conversations select o;
                //var q = from o in DbCompiledQueries.chatsDbContext.conversations orderby o.TimeStamp descending select o;
                return q.ToList();
            }
             * */
        }

        public static ConversationListObject addGroupConversation(ConvMessage convMessage, string groupName)
        {
            /*
            * Msisdn : GroupId
            * Contactname : GroupOwner
            */
            ConversationListObject obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message,
                true, convMessage.Timestamp, null, convMessage.MessageStatus, convMessage.MessageId);

            /*If ABCD join grp chat convObj should show D joined grp chat as D is last in sorted order*/
            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
            {
                string[] vals = Utils.splitUserJoinedMessage(convMessage.Message);
                if (vals == null || vals.Length == 0)
                    return null;
                string[] vars = vals[vals.Length - 1].Split(':');
                GroupParticipant gp = Utils.getGroupParticipant(null, vars[0], obj.Msisdn); // get last element of group in sorted order.
                string text = HikeConstants.USER_JOINED_GROUP_CHAT;
                if (vars[1] == "0")
                    text = HikeConstants.USER_INVITED;
                obj.LastMessage = gp.FirstName + text;
                if (PhoneApplicationService.Current.State.ContainsKey("GC_" + convMessage.Msisdn))
                {
                    obj.IsFirstMsg = true;
                    PhoneApplicationService.Current.State.Remove("GC_" + convMessage.Msisdn);
                }

            }
            string msisdn = obj.Msisdn.Replace(":", "_");
            //saveConvObject(obj, msisdn);
            saveNewConv(obj);
            return obj;
        }

        public static ConversationListObject addConversation(ConvMessage convMessage, bool isNewGroup)
        {
            ConversationListObject obj = null;
            if (isNewGroup)
            {
                string groupName = convMessage.Msisdn;
                if (PhoneApplicationService.Current.State.ContainsKey(convMessage.Msisdn))
                {
                    groupName = (string)PhoneApplicationService.Current.State[convMessage.Msisdn];
                    PhoneApplicationService.Current.State.Remove(convMessage.Msisdn);
                }
                obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message, true, convMessage.Timestamp, null, convMessage.MessageStatus, convMessage.MessageId);
            }
            else
            {
                ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
                byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(convMessage.Msisdn);
                obj = new ConversationListObject(convMessage.Msisdn, contactInfo == null ? null : contactInfo.Name, convMessage.Message,
                    contactInfo == null ? !convMessage.IsSms : contactInfo.OnHike, convMessage.Timestamp, avatar, convMessage.MessageStatus, convMessage.MessageId);
            }

            /*If ABCD join grp chat convObj should show D joined grp chat as D is last in sorted order*/
            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
            {
                string[] vals = Utils.splitUserJoinedMessage(convMessage.Message);
                if (vals == null)
                    return null;
                string[] vars = vals[vals.Length - 1].Split(':');
                GroupParticipant gp = Utils.getGroupParticipant(null, vars[0], convMessage.Msisdn);
                string text = HikeConstants.USER_JOINED_GROUP_CHAT;
                if (vars[1] == "0")
                    text = HikeConstants.USER_INVITED;
                obj.LastMessage = gp.FirstName + text;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
            {
                obj.LastMessage = obj.NameToShow + HikeConstants.USER_OPTED_IN_MSG;
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
            {
                obj.LastMessage = convMessage.Message;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
            {
                obj.LastMessage = string.Format(HikeConstants.DND_USER, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED)
            {
                obj.LastMessage = string.Format(HikeConstants.USER_JOINED_HIKE, obj.NameToShow);
                convMessage.Message = obj.LastMessage;
            }
            if (PhoneApplicationService.Current.State.ContainsKey("GC_" + convMessage.Msisdn)) // this is to store firstMsg logic
            {
                obj.IsFirstMsg = true;
                PhoneApplicationService.Current.State.Remove("GC_" + convMessage.Msisdn);
                Debug.WriteLine("Phone Application Service : GC_{0} removed.", convMessage.Msisdn);
            }
            else
                obj.IsFirstMsg = false;

            Stopwatch st1 = Stopwatch.StartNew();
            MessagesTableUtils.addMessage(convMessage);
            obj.LastMsgId = convMessage.MessageId;
            st1.Stop();
            long msec1 = st1.ElapsedMilliseconds;
            Debug.WriteLine("Time to add chat msg : {0}", msec1);

            Stopwatch st = Stopwatch.StartNew();
            saveNewConv(obj);
            //saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to write conversation to iso storage {0}", msec);

            return obj;
        }

        public static void deleteAllConversations()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + "Convs");
            }
        }

        public static void deleteConversation(string msisdn)
        {
            msisdn = msisdn.Replace(":", "_");
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(CONVERSATIONS_DIRECTORY + "\\" + msisdn))
                        store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + msisdn);
                }
                catch
                {
                }
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                ConversationListObject obj = App.ViewModel.ConvMap[msisdn];
                obj.IsOnhike = joined;
                //saveConvObject(obj, msisdn);
                saveConvObjectList();
            }
        }

        public static void updateConversation(ConversationListObject obj)
        {
            //saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
            saveConvObjectList();
        }

        public static bool updateGroupName(string grpId, string groupName)
        {
            if (!App.ViewModel.ConvMap.ContainsKey(grpId))
                return false;
            ConversationListObject obj = App.ViewModel.ConvMap[grpId];
            obj.ContactName = groupName;
            string msisdn = grpId.Replace(":", "_");
            //saveConvObject(obj, msisdn);
            saveConvObjectList();
            return true;
        }

        internal static void updateConversation(List<ContactInfo> cn)
        {
            saveConvObjectList();
            return;
            for (int i = 0; i < cn.Count; i++)
            {
                if (App.ViewModel.ConvMap.ContainsKey(cn[i].Msisdn))
                {
                    ConversationListObject obj = App.ViewModel.ConvMap[cn[i].Msisdn]; //update UI
                    obj.ContactName = cn[i].Name;
                    obj.IsOnhike = cn[i].OnHike;
                    //saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
                }
            }
        }

        public static void updateLastMsgStatus(long id, string msisdn, int status)
        {
            if (msisdn == null)
                return;
            ConversationListObject obj = null;
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                obj = App.ViewModel.ConvMap[msisdn];
                if (obj.LastMsgId != id)
                    return;
                if (obj.MessageStatus != ConvMessage.State.UNKNOWN) // no D,R for notification msg so dont update
                {
                    obj.MessageStatus = (ConvMessage.State)status;
                    //saveConvObject(obj, msisdn.Replace(":", "_"));
                    saveConvObjectList();
                }
            }
        }

        /// <summary>
        /// Object is serialized using protobuf and is stored in isolated storage file
        /// </summary>
        /// <param name="obj"></param>
        public static void saveConvObject(ConversationListObject obj, string msisdn)
        {
            lock (lockObj)
            {
                string FileName = CONVERSATIONS_DIRECTORY + "\\" + msisdn;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            obj.Write(writer);
                        }
                    }
                }
            }
        }

        public static void saveConvObjectList()
        {
            int convs = 0;
            Stopwatch st = Stopwatch.StartNew();
            Dictionary<string, ConversationListObject> convMap = App.ViewModel.ConvMap;
            lock (lockObj)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    string FileName = CONVERSATIONS_DIRECTORY + "\\" + "Convs";
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            if (convMap != null && convMap.Count > 0)
                            {
                                writer.Write(convMap.Count);
                                foreach (ConversationListObject item in convMap.Values)
                                {
                                    item.Write(writer);
                                    convs++;
                                }
                            }
                        }
                    }
                }
            }
            st.Stop();
            long mSec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to save {0} conversations : {1}", convs, mSec);
        }

        public static void saveNewConv(ConversationListObject obj)
        {
            int convs = 0;
            Stopwatch st = Stopwatch.StartNew();
            Dictionary<string, ConversationListObject> convMap = App.ViewModel.ConvMap;
            lock (lockObj)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    string FileName = CONVERSATIONS_DIRECTORY + "\\" + "Convs";
                    using (var file = store.OpenFile(FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (var writer = new BinaryWriter(file))
                        {
                            int count = (convMap == null ? 0 : convMap.Count) + 1;
                            writer.Write(count);
                            obj.Write(writer);
                            if (convMap != null && convMap.Count > 0)
                            {                               
                                foreach (ConversationListObject item in convMap.Values)
                                {
                                    item.Write(writer);
                                    convs++;
                                }
                            }
                        }
                    }
                }
            }
            st.Stop();
            long mSec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to save {0} conversations : {1}", convs, mSec);
        }

        public static List<ConversationListObject> getAllConvs()
        {
            List<ConversationListObject> convList = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(CONVERSATIONS_DIRECTORY) || !store.FileExists(CONVERSATIONS_DIRECTORY + "\\" + "Convs"))
                    return null;
                using (var file = store.OpenFile(CONVERSATIONS_DIRECTORY + "\\" + "Convs", FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(file))
                    {
                        int count = reader.ReadInt32();
                        if (count > 0)
                        {
                            convList = new List<ConversationListObject>(count);
                            for (int i = 0; i < count; i++)
                            {
                                ConversationListObject item = new ConversationListObject();
                                item.Read(reader);
                                convList.Add(item);
                            }
                            convList.Sort();
                            return convList;
                        }
                        return null;
                    }
                }

            }
        }


        /* Handle old versions*/
        public static void deleteAllConversationsOld()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY + "\\*");
                if (files == null)
                    return;
                foreach (string fileName in files)
                {
                    try
                    {
                        if (fileName == CONVERSATIONS_DIRECTORY + "\\" + "Convs")
                            continue;
                        store.DeleteFile(CONVERSATIONS_DIRECTORY + "\\" + fileName);
                    }
                    catch
                    {
                        Debug.WriteLine("File {0} does not exist.", CONVERSATIONS_DIRECTORY + "\\" + fileName);
                    }
                }
            }
        }
    }
}
