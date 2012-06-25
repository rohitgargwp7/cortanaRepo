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
                if (q(context).Count<Conversation>() == 0)
                    return null;
                List<Conversation> conversations = q(context).ToList<Conversation>();
                conversations.Sort();
                conversations.Reverse();
                return conversations;
            }           
        }

        public static void addConversation(ConvMessage convMessage)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
            Conversation conv = new Conversation(convMessage.Msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : null,  (contactInfo != null) ? contactInfo.OnHike:false);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.conversations.InsertOnSubmit(conv);
                context.SubmitChanges();
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
                context.conversations.DeleteAllOnSubmit<Conversation>(q(context, msisdn));
                context.SubmitChanges();
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            Conversation cInfo = null;
            Func<HikeDataContext, string, IQueryable<Conversation>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<Conversation>>
             ((HikeDataContext hdc, string ms) =>
                 from o in hdc.conversations
                 where o.Msisdn == ms
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                if (q(context, msisdn).Count<Conversation>() == 1)
                {
                    cInfo = q(context, msisdn).ToList<Conversation>().First<Conversation>();
                    cInfo.OnHike = (bool)joined;
                    context.SubmitChanges();
                }
            }
        }
    }
}
