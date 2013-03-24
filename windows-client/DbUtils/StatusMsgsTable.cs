using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Languages;
using windows_client.Model;
using windows_client.Misc;
using Microsoft.Phone.Data.Linq;

namespace windows_client.DbUtils
{
    class StatusMsgsTable
    {
        public const int MessagesDb_Latest_Version = 1;
        public static string LAST_STATUS_FILENAME = "_Last_Status";
        public static object readWriteLock = new object();

        /// <summary>
        /// Add single status msg
        /// </summary>
        /// <param name="sm"></param>
        public static void InsertStatusMsg(StatusMessage sm)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.statusMessage.InsertOnSubmit(sm);
                context.SubmitChanges();
            }
        }

        /// <summary>
        /// Add list of msgs
        /// </summary>
        /// <param name="smList"></param>
        public static void AddStatusMsg(List<StatusMessage> smList)
        {
            if (smList == null)
                return;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.statusMessage.InsertAllOnSubmit(smList);
                context.SubmitChanges();
            }
        }

        public static List<StatusMessage> GetStatusMsgsForMsisdn(string msisdn)
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetStatusMsgsForMsisdn(context, msisdn).ToList<StatusMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        public static List<StatusMessage> GetUnReadStatusMsgsForMsisdn(string msisdn, int count)
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetUnReadStatusMsgsForMsisdn(context, msisdn, count).ToList<StatusMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        public static List<StatusMessage> GetAllStatusMsgsForTimeline()
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetAllStatusMsgsForTimeline(context).ToList<StatusMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        public static List<StatusMessage> GetUnReadStatusMsgs(int count)
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetUnReadStatusMsgs(context, count).ToList<StatusMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        public static void DeleteAllStatusMsgs()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                try
                {

                    context.statusMessage.DeleteAllOnSubmit<StatusMessage>(context.GetTable<StatusMessage>());
                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StatusMsgsTable :: DeleteAllStatusMsgs : DeleteAllStatusMsgs, Exception : " + ex.StackTrace);
                }
            }
        }

        public static long DeleteStatusMsg(string id)
        {
            if (string.IsNullOrEmpty(id))
                return -1;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                try
                {
                    List<StatusMessage> smEn = DbCompiledQueries.GetStatusMsgForServerId(context, id).ToList();
                    context.statusMessage.DeleteAllOnSubmit<StatusMessage>(smEn);
                    context.SubmitChanges();
                    StatusMessage sm = smEn.FirstOrDefault<StatusMessage>();
                    return sm.MsgId;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StatusMsgsTable :: DeleteStatusMsg : DeleteStatusMsg, Exception : " + ex.StackTrace);
                    return -1;
                }
            }
        }

        public static void UpdateMsgId(StatusMessage sm)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                StatusMessage smsg = DbCompiledQueries.GetStatusMsgForStatusId(context, sm.StatusId).FirstOrDefault();
                if (smsg != null)
                {
                    smsg.MsgId = sm.MsgId;
                    context.SubmitChanges();
                }
            }
        }

        public static void SaveLastStatusMessage(string message, int moodId)
        {
            if (!string.IsNullOrEmpty(message))
            {
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        string fileName = LAST_STATUS_FILENAME;
                        try
                        {
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("StatusMsgTable :: SaveLastStatusMessage: delete file, Exception : " + ex.StackTrace);
                        }
                        try
                        {
                            using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    writer.Seek(0, SeekOrigin.Begin);
                                    writer.WriteStringBytes(message);
                                    writer.Write(moodId);
                                    writer.Flush();
                                    writer.Close();
                                }
                                file.Close();
                                file.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("StatusMsgTable :: SaveLastStatusMessage : write file, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        public static string GetLastStatusMessage(out int moodId)
        {
            string message = null;
            int retMoodId = -1;
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    try
                    {
                        if (store.FileExists(LAST_STATUS_FILENAME))
                        {

                            using (var file = store.OpenFile(LAST_STATUS_FILENAME, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    int count = reader.ReadInt32();
                                    message = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    retMoodId = reader.ReadInt32();
                                    reader.Close();
                                }
                                file.Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UserTableUtils :: GetContactsFromFile : read file, Exception : " + ex.StackTrace);
                    }
                }
            }
            moodId = retMoodId;
            return message;
        }

        public static void DeleteLastStatusFile()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    try
                    {
                        if (store.FileExists(LAST_STATUS_FILENAME))
                        {
                            store.DeleteFile(LAST_STATUS_FILENAME);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StatusMsgTable :: DeleteLastStatusFile : delete file, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        public static void MessagesDbUpdateToLatestVersion()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                DatabaseSchemaUpdater schemaUpdater = context.CreateDatabaseSchemaUpdater();
                // get current database schema version
                // if not changed the version is 0 by default
                int version = schemaUpdater.DatabaseSchemaVersion;

                // if current version of database schema is old
                if (version == 0)
                {
                    // add a status messages table to chats db  
                    schemaUpdater.AddTable<StatusMessage>();
                    
                    // IMPORTANT: update database schema version before calling Execute
                    schemaUpdater.DatabaseSchemaVersion = MessagesDb_Latest_Version;
                    try
                    {
                        // execute changes to database schema
                        schemaUpdater.Execute();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
    }
}
