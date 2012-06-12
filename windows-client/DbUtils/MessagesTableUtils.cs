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
using System.Linq;
using windows_client.Model;
using System.Data.Linq;
using System.Collections.Generic;
using Microsoft.Phone.Controls;
using windows_client.View;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {

        /* This is shown on chat thread screen*/
        public static List<ConvMessage> getMessagesForMsisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string myMsisdn) =>
                from o in hdc.messages
                where o.Msisdn == myMsisdn
                select o);
            return q(App.HikeDataContextInstance, msisdn).Count<ConvMessage>() == 0 ? null :
                q(App.HikeDataContextInstance, msisdn).ToList<ConvMessage>();
        }


        /* This queries messages table and get the last message for given msisdn*/
        public static ConvMessage getLastMessageForMsisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string myMsisdn) =>
                from o in hdc.messages
                where o.Msisdn == myMsisdn
                select o);
            return q(App.HikeDataContextInstance, msisdn).Count<ConvMessage>() == 0 ? null :
                q(App.HikeDataContextInstance, msisdn).Last<ConvMessage>();
        }


        public static List<ConvMessage> getAllMessages()
        {
            Func<HikeDataContext, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ConvMessage>>
            ((HikeDataContext hdc) =>
                from o in hdc.messages
                select o);
            return q(App.HikeDataContextInstance).Count<ConvMessage>() == 0 ? null :
                q(App.HikeDataContextInstance).ToList<ConvMessage>();
        }

        /* Adds a chat message to message Table.*/
        public static void addMessage(ConvMessage convMessage)
        {
            App.HikeDataContextInstance.messages.InsertOnSubmit(convMessage);
            App.HikeDataContextInstance.SubmitChanges();
            long msgId = convMessage.MessageId;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var currentPage = ((App)Application.Current).RootFrame.Content as ChatThread;
                if (currentPage != null)
                    currentPage.MsgMap.Add(msgId, convMessage);
            });
        }

        public static void addChatMessage(ConvMessage convMsg, bool isNewConversation)
        {
            if (isNewConversation)
            {
                ConversationTableUtils.addConversation(convMsg);
            }
            addMessage(convMsg);
        }
        public static void addChatMessage(ConvMessage convMsg)
        {
            if (!MessageList.ConvMap.ContainsKey(convMsg.Msisdn))
            {
                ConversationTableUtils.addConversation(convMsg);
            }
            addMessage(convMsg);
        }

        /// <summary>
        /// Update message status in db. 
        /// </summary>
        /// <param name="msgID">messageID from which msg is searched in db.</param>
        /// <param name="val">New value of Message state</param>
        public static void updateMsgStatus(long msgID, int val)
        {
            ConvMessage message;
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
             CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
             ((HikeDataContext hdc, long m) =>
                 from o in hdc.messages
                 where o.MessageId == msgID
                 select o);
            if (q(App.HikeDataContextInstance, msgID).Count<ConvMessage>() == 1)
            {
                message = q(App.HikeDataContextInstance, msgID).ToList<ConvMessage>().First<ConvMessage>();
                message.MessageStatus = (ConvMessage.State)val;
                App.HikeDataContextInstance.SubmitChanges();
            }
        }
    }
}
