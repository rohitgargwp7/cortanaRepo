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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, msisdn).Count<ConvMessage>() == 0 ? null :
                q(context, msisdn).ToList<ConvMessage>();
            }           
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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, msisdn).Count<ConvMessage>() == 0 ? null :
              q(context, msisdn).Last<ConvMessage>();
            }
          
        }


        public static List<ConvMessage> getAllMessages()
        {
            Func<HikeDataContext, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ConvMessage>>
            ((HikeDataContext hdc) =>
                from o in hdc.messages
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context).Count<ConvMessage>() == 0 ? null :
               q(context).ToList<ConvMessage>();
            }
           
        }

        /* Adds a chat message to message Table.*/
        public static void addMessage(ConvMessage convMessage)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.InsertOnSubmit(convMessage);
                context.SubmitChanges();
                long msgId = convMessage.MessageId;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var currentPage = ((App)Application.Current).RootFrame.Content as ChatThread;
                    if (currentPage != null)
                    {
                        if (convMessage.IsSent)
                            currentPage.MsgMap.Add(msgId, convMessage);
                        else
                            currentPage.IncomingMessages.Add(convMessage);
                    }
                });
            }           
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
            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn))
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
            ConvMessage message = null;
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
             CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
             ((HikeDataContext hdc, long m) =>
                 from o in hdc.messages
                 where o.MessageId == m
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                if (q(context, msgID).Count<ConvMessage>() == 1)
                {
                    message = q(context, msgID).ToList<ConvMessage>().First<ConvMessage>();
                    message.MessageStatus = (ConvMessage.State)val;
                    context.SubmitChanges();
                }
            }
            
        }
        public static void updateAllMsgStatus(long[] ids, int status)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    ConvMessage message = new ConvMessage();
                    Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
                     CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
                     ((HikeDataContext hdc, long m) =>
                         from o in hdc.messages
                         where o.MessageId == m
                         select o);
                    if (q(context, ids[i]).Count<ConvMessage>() == 1)
                    {
                        message = q(context, ids[i]).ToList<ConvMessage>().First<ConvMessage>();
                        message.MessageStatus = (ConvMessage.State)status;
                    }
                }
                context.SubmitChanges();
            }
        }


        public static void deleteAllMessages()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.SubmitChanges();
            }
        }

        public static void deleteMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.messages
                where o.MessageId == id
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(q(context, msgId));
                context.SubmitChanges();
            }
        }

        public static void deleteAllMessagesForMsisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string myMsisdn) =>
                from o in hdc.messages
                where o.Msisdn == myMsisdn
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(q(context, msisdn));
                context.SubmitChanges();
            }   
        }

        public bool wasMessageReceived(ConvMessage conv)
        {
            Func<HikeDataContext, long, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long timestamp, string m) =>
                from cm in hdc.messages
                //                where cm.Msisdn == m && cm.ConversationId == conversationId && cm.Timestamp == timestamp
                where cm.Msisdn == m && cm.Timestamp == timestamp
                select cm);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, conv.Timestamp, conv.Message).Count() != 0;
            }
        }



        public static void addMessages(List<ConvMessage> messages)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.InsertAllOnSubmit(messages);
                context.SubmitChanges();
            }
        }

    }
}
