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
        private static MySerializer ser = new MySerializer();
        private static object lockObj = new object();
        /* This function gets all the conversations shown on the message list page*/
        public static List<ConversationListObject> getAllConversations()
        {
            byte[] data = null;
            List<ConversationListObject> convList = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY+"\\*");
                convList = new List<ConversationListObject>(files.Length);
                foreach (string fileName in files)
                {
                    using (IsolatedStorageFileStream isfs = store.OpenFile(CONVERSATIONS_DIRECTORY+"\\" + fileName, FileMode.Open, FileAccess.Read))
                    {
                        data = new byte[isfs.Length];
                        // Read the entire file and then close it
                        isfs.Read(data, 0, data.Length);
                        isfs.Close();
                        using (var ms = new MemoryStream(data))
                        {
                            ConversationListObject co = (ConversationListObject)ser.Deserialize(ms, null, typeof(ConversationListObject));
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
                true, convMessage.Timestamp, null, convMessage.MessageStatus);

            string msisdn = obj.Msisdn.Replace(":", "_");
            saveConvObject(obj, msisdn);
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    context.conversations.InsertOnSubmit(obj);
            //    context.SubmitChanges();
            //}
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
                obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message, true, convMessage.Timestamp, null, ConvMessage.State.SENT_UNCONFIRMED);
            }
            else
            {
                ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
                byte [] avatar = MiscDBUtil.getThumbNailForMsisdn(convMessage.Msisdn);
                obj = new ConversationListObject(convMessage.Msisdn, contactInfo == null ? null : contactInfo.Name, convMessage.Message,
                    contactInfo == null ? !convMessage.IsSms : contactInfo.OnHike, convMessage.Timestamp, avatar, convMessage.MessageStatus);
            }

            Stopwatch st = Stopwatch.StartNew();
            saveConvObject(obj, obj.Msisdn.Replace(":","_"));
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to write conversation to iso storage {0}", msec);

            //App.WriteToIsoStorageSettings("CONV::" + convMessage.Msisdn, obj);
            //st.Reset(); st.Start();
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    context.conversations.InsertOnSubmit(obj);
            //    context.SubmitChanges();
            //}
            //st.Stop();
            //msec = st.ElapsedMilliseconds;
            //Debug.WriteLine("Time to write conversation to DB {0}", msec);
            return obj;
        }

        public static void deleteAllConversations()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(CONVERSATIONS_DIRECTORY+"\\*");
                foreach (string fileName in files)
                {
                    try
                    {
                        store.DeleteFile(CONVERSATIONS_DIRECTORY+"\\" + fileName);
                    }
                    catch
                    {
                        Debug.WriteLine("File {0} does not exist.", CONVERSATIONS_DIRECTORY+"\\" + fileName);
                    }
                }
            }
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    context.conversations.DeleteAllOnSubmit<ConversationListObject>(context.GetTable<ConversationListObject>());
            //    MessagesTableUtils.SubmitWithConflictResolve(context);
            //}
        }

        public static void deleteConversation(string msisdn)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(CONVERSATIONS_DIRECTORY+"\\" + msisdn))
                        store.DeleteFile(CONVERSATIONS_DIRECTORY+"\\" + msisdn);
                }
                catch
                {
                }
            }
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    context.conversations.DeleteAllOnSubmit<ConversationListObject>(DbCompiledQueries.GetConvForMsisdn(context, msisdn));
            //    MessagesTableUtils.SubmitWithConflictResolve(context);
            //}
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            if (ConversationsList.ConvMap.ContainsKey(msisdn))
            {
                ConversationListObject obj = ConversationsList.ConvMap[msisdn];
                obj.IsOnhike = joined;
                saveConvObject(obj, msisdn);
            }
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    List<ConversationListObject> res = DbCompiledQueries.GetConvForMsisdn(context, msisdn).ToList<ConversationListObject>();
            //    if (res == null || res.Count<ConversationListObject>() == 0)
            //        return;
            //    for (int i = 0; i < res.Count; i++)
            //    {
            //        ConversationListObject conv = res[i];
            //        conv.IsOnhike = (bool)joined;
            //    }
            //    MessagesTableUtils.SubmitWithConflictResolve(context);
            //}
        }

        public static void updateConversation(ConversationListObject obj)
        {
            saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
            //lock (lockObj)
            //{
            //    using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + "; Max Buffer Size = 2048"))
            //    {
            //        ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, obj.Msisdn).FirstOrDefault();
            //        if (cObj.ContactName != obj.ContactName)
            //            cObj.ContactName = obj.ContactName;
            //        cObj.MessageStatus = obj.MessageStatus;
            //        cObj.LastMessage = obj.LastMessage;
            //        cObj.TimeStamp = obj.TimeStamp;
            //        MessagesTableUtils.SubmitWithConflictResolve(context);
            //    }
            //}
        }
        public static bool updateGroupName(string grpId, string groupName)
        {
            if (!ConversationsList.ConvMap.ContainsKey(grpId))
                return false;
            ConversationListObject obj = ConversationsList.ConvMap[grpId];
            obj.ContactName = groupName;
            string msisdn = grpId.Replace(":","_");
            saveConvObject(obj, msisdn);
            return true;

            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, grpId).FirstOrDefault();
            //    if (cObj == null)
            //        return false; ;
            //    if (cObj.ContactName != groupName)
            //    {
            //        cObj.ContactName = groupName;
            //        MessagesTableUtils.SubmitWithConflictResolve(context);
            //    }
            //    else
            //        return false;
            //}
            //return true;
        }
        internal static void updateConversation(List<ContactInfo> cn)
        {
            for (int i = 0; i < cn.Count; i++)
            {
                if (ConversationsList.ConvMap.ContainsKey(cn[i].Msisdn))
                {
                    ConversationListObject obj = ConversationsList.ConvMap[cn[i].Msisdn]; //update UI
                    obj.ContactName = cn[i].Name;
                    saveConvObject(obj, obj.Msisdn.Replace(":", "_"));
                }
            }

            //bool shouldSubmit = false;
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    for (int i = 0; i < cn.Count; i++)
            //    {
            //        if (ConversationsList.ConvMap.ContainsKey(cn[i].Msisdn))
            //        {
            //            ConversationListObject obj = ConversationsList.ConvMap[cn[i].Msisdn]; //update UI
            //            obj.ContactName = cn[i].Name;

            //            ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, obj.Msisdn).FirstOrDefault();
            //            if (cObj.ContactName != cn[i].Name)
            //            {
            //                cObj.ContactName = cn[i].Name;
            //                shouldSubmit = true;
            //            }
            //        }
            //    }
            //    if (shouldSubmit)
            //    {
            //        MessagesTableUtils.SubmitWithConflictResolve(context);
            //    }
            //}
        }

        public static void updateLastMsgStatus(string msisdn, int status)
        {
            ConversationListObject obj = null;
            if (ConversationsList.ConvMap.ContainsKey(msisdn))
            {
                obj = ConversationsList.ConvMap[msisdn];
                obj.MessageStatus = (ConvMessage.State)status;
                saveConvObject(obj, msisdn.Replace(":", "_"));
            }
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{
            //    ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, msisdn).FirstOrDefault<ConversationListObject>();
            //    if (cObj == null)
            //        return;
            //    cObj.MessageStatus = (ConvMessage.State)status;
            //    MessagesTableUtils.SubmitWithConflictResolve(context);
            //}
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
                    using (FileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, store))
                    {
                        byte[] raw = null;
                        using (var ms = new MemoryStream())
                        {
                            ser.Serialize(ms, obj);
                            raw = ms.ToArray();
                        }
                        stream.Write(raw, 0, raw.Length);
                    }
                }
            }
        }

        public static void saveConvObjectList(List<ConversationListObject> cObjList)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
            {
                for (int i = 0; i < cObjList.Count; i++)
                {
                    string FileName = CONVERSATIONS_DIRECTORY+"\\" + cObjList[i].Msisdn;
                    using (FileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, store))
                    {
                        byte[] raw = null;
                        using (var ms = new MemoryStream())
                        {
                            ser.Serialize(ms, cObjList[i]);
                            raw = ms.ToArray();
                        }
                        stream.Write(raw, 0, raw.Length);
                    }
                }
            }
        }
    }
}
