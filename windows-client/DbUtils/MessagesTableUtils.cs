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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsisdn(context, msisdn).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        /* This queries messages table and get the last message for given msisdn*/
        public static ConvMessage getLastMessageForMsisdn(string msisdn)
        {
            List<ConvMessage> res;
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                res = DbCompiledQueries.GetMessagesForMsisdn(context, msisdn).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res.Last();
            }

        }

        public static List<ConvMessage> getAllMessages()
        {
            List<ConvMessage> res;
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                res = DbCompiledQueries.GetAllMessages(context).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res.ToList();
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

        public static void updateMsgStatus(long msgID, int val)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsgId(context, msgID).ToList<ConvMessage>();
                if (res.Count == 1)
                {
                    ConvMessage message = res.First();
                    message.MessageStatus = (ConvMessage.State)val;
                    context.SubmitChanges();
                }
                else
                {
                    // show some logs and errors
                }
            }

        }

        public static void updateAllMsgStatus(long[] ids, int status)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsgId(context, ids[i]).ToList<ConvMessage>();
                    if (res.Count == 1)
                    {
                        ConvMessage message = res.First();
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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(DbCompiledQueries.GetMessagesForMsgId(context, msgId));
                context.SubmitChanges();
            }
        }

        public static void deleteAllMessagesForMsisdn(string msisdn)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(DbCompiledQueries.GetMessagesForMsisdn(context, msisdn));
                context.SubmitChanges();
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
