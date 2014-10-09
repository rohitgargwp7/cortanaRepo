using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using CommonLibrary.Model;
using Microsoft.Phone.Data.Linq;
using CommonLibrary.Lib;
using CommonLibrary.Constants;

namespace CommonLibrary.DbUtils
{
    class StatusMsgsTable
    {
        public static string LAST_STATUS_FILENAME = "_Last_Status";
        public static string UNREAD_COUNT_FILE = "unreadCountFile";
        public static object readWriteLock = new object();
        private static object refreshLock = new object();

        /// <summary>
        /// Add single status msg
        /// </summary>
        /// <param name="sm"></param>
        public static bool InsertStatusMsg(StatusMessage sm, bool checkAlreadyExists)
        {
            try
            {
                using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
                {
                    if (checkAlreadyExists)
                    {
                        IQueryable<StatusMessage> sts = DbCompiledQueries.GetStatusMsgForServerId(context, sm.ServerId);
                        StatusMessage sMsg = sts.FirstOrDefault();
                        if (sMsg != null)
                            return false;
                    }
                    context.statusMessage.InsertOnSubmit(sm);
                    context.SubmitChanges();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("MessagesTableUtils :: addMessage : submit changes, Exception : " + e.StackTrace);
                return false;
            }
            return true;
        }

        public static void DeleteAllStatusMsgs()
        {

            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
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

            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
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

            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                StatusMessage smsg = DbCompiledQueries.GetStatusMsgForStatusId(context, sm.StatusId).FirstOrDefault();
                if (smsg != null)
                {
                    smsg.MsgId = sm.MsgId;
                    context.SubmitChanges();
                }
            }
        }

        public static void DeleteLastStatusFile()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
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

        public static void DeleteUnreadCountFile()
        {
            lock (refreshLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (store.FileExists(UNREAD_COUNT_FILE))
                        {
                            store.DeleteFile(UNREAD_COUNT_FILE);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StatusMsgTable :: DeleteRefreshCountFile : delete file, Exception : " + ex.StackTrace);
                    }
                }
            }
        }
    }
}
