﻿using System;
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
        public static void addConversationMessages(ConvMessage conv, bool createEntry)
        {
        }

        public static void deleteMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.messages
                where o.MessageId == id
                select o);
            App.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(q(App.HikeDataContext, msgId));
            App.HikeDataContext.SubmitChanges();
        }

        public static void updateMsgStatus(long msgID, int val)
        {
        }

        /* This function gets all the conversations shown on the message list page*/
        public static List<Conversation> getConversations()
        {
            Func<HikeDataContext, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
            ((HikeDataContext hdc) =>
                from o in hdc.conversations
                select o);
            if (q(App.HikeDataContext).Count<Conversation>() == 0)
                return null;
            List<Conversation> conversations = q(App.HikeDataContext).ToList<Conversation>();
            conversations.Sort();
            conversations.Reverse();
            return conversations;
        }
    }
}
