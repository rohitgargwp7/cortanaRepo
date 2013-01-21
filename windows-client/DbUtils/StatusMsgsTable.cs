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

        public static List<StatusMessage> InsertStatusMsgs(string msisdn, JObject data)
        {
            if (data == null)
                return null;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                List<StatusMessage> list = new List<StatusMessage>(2);
                JToken val;
                if (data.TryGetValue(HikeConstants.PIC_UPDATE, out val))
                {
                    try
                    {
                        list.Add(new StatusMessage(msisdn, AppResources.PicUpdate_StatusTxt, StatusMessage.StatusType.PHOTO_UPDATE));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception while inserting Pic Update msg : " + e.StackTrace);
                    }
                }
                val = null;
                if (data.TryGetValue(HikeConstants.TEXT_UPDATE, out val))
                {
                    try
                    {
                        JObject valObj = val.ToObject<JObject>();
                        list.Add(new StatusMessage(msisdn, valObj["data"].ToString(), StatusMessage.StatusType.TEXT_UPDATE));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception while inserting Text Update msg : " + e.StackTrace);
                    }
                }
                context.statusMessage.InsertAllOnSubmit(list);
                context.SubmitChanges();
                return list;
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
