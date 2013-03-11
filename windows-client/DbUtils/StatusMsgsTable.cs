using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Languages;
using windows_client.Model;

namespace windows_client.DbUtils
{
    class StatusMsgsTable
    {
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

        public static List<StatusMessage> GetAllStatusMsgs()
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetAllStatusMsgs(context).ToList<StatusMessage>();
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
                catch (Exception e)
                {
                    Debug.WriteLine("StatusMsgsTable :: Exception while deleting all status msgs : " + e.StackTrace);
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
                catch (Exception e)
                {
                    Debug.WriteLine("StatusMsgsTable :: Exception while deleting status : " + e.StackTrace);
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
    }
}
