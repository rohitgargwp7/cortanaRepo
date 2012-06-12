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
        public static void deleteMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.messages
                where o.MessageId == id
                select o);
            App.HikeDataContextInstance.messages.DeleteAllOnSubmit<ConvMessage>(q(App.HikeDataContextInstance, msgId));
            App.HikeDataContextInstance.SubmitChanges();
        }

        /* This function gets all the conversations shown on the message list page*/
        public static List<Conversation> getAllConversations()
        {
            Func<HikeDataContext, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
            ((HikeDataContext hdc) =>
                from o in hdc.conversations
                select o);
            if (q(App.HikeDataContextInstance).Count<Conversation>() == 0)
                return null;
            List<Conversation> conversations = q(App.HikeDataContextInstance).ToList<Conversation>();
            conversations.Sort();
            conversations.Reverse();
            return conversations;
        }

        public static void addConversation(ConvMessage convMessage)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
            Conversation conv = new Conversation(convMessage.Msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : null,  (contactInfo != null) ? contactInfo.OnHike:false);
            App.HikeDataContextInstance.conversations.InsertOnSubmit(conv);
            App.HikeDataContextInstance.SubmitChanges();
        }

    }
}
