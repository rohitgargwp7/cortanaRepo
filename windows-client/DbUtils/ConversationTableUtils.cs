using System;
using windows_client.Model;
using System.Linq;
using System.Collections.Generic;

namespace windows_client.DbUtils
{
    public class ConversationTableUtils
    {
        /* This function gets all the conversations shown on the message list page*/
        public static List<Conversation> getAllConversations()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                var q = from o in context.conversations select o;
                return q.ToList<Conversation>();
            }           
        }

        public static void addConversation(ConvMessage convMessage)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
            Conversation conv = new Conversation(convMessage.Msisdn,(contactInfo != null) ? contactInfo.OnHike:!convMessage.IsSms);
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {              
                    context.conversations.InsertOnSubmit(conv);
                    context.SubmitChanges();
            }          
        }

        public static void deleteAllConversations()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(context.GetTable<Conversation>());
                context.SubmitChanges();
            }
        }

        public static void deleteConversation(string msisdn)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<Conversation>(DbCompiledQueries.GetConvForMsisdn(context, msisdn));
                context.SubmitChanges();
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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
