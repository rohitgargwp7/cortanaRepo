using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;

namespace windows_client.DbUtils
{
    class StatusMsgsTable
    {
        /// <summary>
        /// Add single status msg
        /// </summary>
        /// <param name="sm"></param>
        public static void AddStatusMsg(StatusMessage sm)
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

        public static List<StatusMessage> GetAllStatusMsgs()
        {
            List<StatusMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetAllStatusMsgs(context).ToList<StatusMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }
    }
}
