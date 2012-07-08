using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using windows_client.Model;
using System.Linq;
using System.Data.Linq;
using System.Collections.Generic;
using windows_client.View;

namespace windows_client.DbUtils
{
    public class ConversationTableUtils
    {
        /* This function gets all the conversations shown on the message list page*/
        public static List<Conversation> getAllConversations()
        {
            Func<HikeDataContext, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
            ((HikeDataContext hdc) =>
                from o in hdc.conversations
                select o);

            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<Conversation> res = DbCompiledQueries.GetAllConversations(context).ToList<Conversation>();
                if (res==null || res.Count() == 0)
                    return null;
                res.Sort();
                res.Reverse();
                return res;
            }           
        }

        public static void addConversation(ConvMessage convMessage)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
            Conversation conv = new Conversation(convMessage.Msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : convMessage.Msisdn,  (contactInfo != null) ? contactInfo.OnHike:!convMessage.IsSms);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                try
                {
                    context.conversations.InsertOnSubmit(conv);
                    context.SubmitChanges();
                }
                catch (Exception)
                {
                }
            }          
        }


        public static void deleteAllConversations()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(context.GetTable<Conversation>());
                context.SubmitChanges();
            }
        }

        public static void deleteConversation(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<Conversation>>
            ((HikeDataContext hdc, string _msisdn) =>
                from o in hdc.conversations
                where o.Msisdn == _msisdn
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(DbCompiledQueries.GetConvForMsisdn(context, msisdn));
                context.SubmitChanges();
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            Func<HikeDataContext, string, IQueryable<Conversation>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<Conversation>>
             ((HikeDataContext hdc, string ms) =>
                 from o in hdc.conversations
                 where o.Msisdn == ms
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<Conversation> res = DbCompiledQueries.GetConvForMsisdn(context, msisdn).ToList<Conversation>();
                if (res == null || res.Count<Conversation>() == 0)
                    return;
                for (int i = 0; i < res.Count;i++ )
                {
                    Conversation conv = res[i];
                    conv.OnHike = (bool)joined;
                }
                context.SubmitChanges();
            }
        }
    }
}
