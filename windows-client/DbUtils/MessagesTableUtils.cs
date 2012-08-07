using System.Windows;
using System.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.View;
using System;
using windows_client.utils;
using System.Data.Linq;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {

        /* This is shown on chat thread screen*/
        public static List<ConvMessage> getMessagesForMsisdn(string msisdn)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsisdn(context, msisdn).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        /* This queries messages table and get the last message for given msisdn*/
        public static ConvMessage getLastMessageForMsisdn(string msisdn)
        {
            List<ConvMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetMessagesForMsisdn(context, msisdn).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res.Last();
            }

        }

        public static List<ConvMessage> getAllMessages()
        {
            List<ConvMessage> res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetAllMessages(context).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res.ToList();
            }

        }

        /* Adds a chat message to message Table.*/
        public static void addMessage(ConvMessage convMessage)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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
                            currentPage.OutgoingMsgsMap.Add(msgId, convMessage);
                        else
                            currentPage.IncomingMessages.Add(convMessage);
                    }
                });
            }
        }

        public static ConversationListObject addChatMessage(ConvMessage convMsg)
        {
            ConversationListObject obj = null;
            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn))
            {
                obj = ConversationTableUtils.addConversation(convMsg);
                ConversationsList.ConvMap.Add(convMsg.Msisdn, obj);
            }
            else
            {
                obj = ConversationsList.ConvMap[convMsg.Msisdn];
                obj.LastMessage = convMsg.Message;
                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                ConversationTableUtils.updateConversation(obj);
            }
            addMessage(convMsg);
            return obj;
        }

        public static void updateMsgStatus(long msgID, int val)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConvMessage message = DbCompiledQueries.GetMessagesForMsgId(context, msgID).FirstOrDefault<ConvMessage>();
                if (message != null)
                {
                    message.MessageStatus = (ConvMessage.State)val;
                    SubmitWithConflictResolve(context);
                }
                else
                {
                    // show some logs and errors
                }
            }

        }

        public static string updateAllMsgStatus(long[] ids, int status)
        {
            string msisdn = null;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsgId(context, ids[i]).ToList<ConvMessage>();
                    if (res.Count == 1)
                    {
                        ConvMessage message = res.First();
                        message.MessageStatus = (ConvMessage.State)status;
                        msisdn = message.Msisdn;
                    }
                }
                SubmitWithConflictResolve(context);
            }
            return msisdn;
        }

        public static void deleteAllMessages()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                SubmitWithConflictResolve(context);
            }
        }

        public static void deleteMessage(long msgId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(DbCompiledQueries.GetMessagesForMsgId(context, msgId));
                SubmitWithConflictResolve(context);
            }
        }

        public static void deleteAllMessagesForMsisdn(string msisdn)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(DbCompiledQueries.GetMessagesForMsisdn(context, msisdn));
                SubmitWithConflictResolve(context);
            }
        }

        public static void addMessages(List<ConvMessage> messages)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.messages.InsertAllOnSubmit(messages);
                context.SubmitChanges();
            }
        }

        public static void SubmitWithConflictResolve(HikeChatsDb context)
        {
            try
            {
                context.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (ChangeConflictException e)
            {
                Console.WriteLine(e.Message);
                // Automerge database values for members that client
                // has not modified.
                foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                {
                    occ.Resolve(RefreshMode.KeepChanges); // second client changes will be submitted.
                }
            }
            // Submit succeeds on second try.
            context.SubmitChanges(ConflictMode.FailOnFirstConflict);
        }
    }
}
