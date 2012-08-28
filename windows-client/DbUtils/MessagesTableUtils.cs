using System.Windows;
using System.Linq;
using windows_client.Model;
using System.Collections.Generic;
using windows_client.View;
using System;
using windows_client.utils;
using System.Data.Linq;
using Newtonsoft.Json.Linq;
using windows_client.Controls;
using System.Diagnostics;
using System.Threading;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {
        //private static HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring); // use this chatsDbContext to improve performance

        /* This is shown on chat thread screen*/
        public static List<ConvMessage> getMessagesForMsisdn(string msisdn)
        {
            List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsisdn(DbCompiledQueries.chatsDbContext, msisdn).ToList<ConvMessage>();
            return (res == null || res.Count == 0) ? null : res;
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
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
            {
                context.messages.InsertOnSubmit(convMessage);
                context.SubmitChanges();

                long msgId = convMessage.MessageId;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                    if (currentPage != null)
                    {
                        if (convMessage.IsSent)
                        {
                            SentChatBubble sentChatBubble;
                            currentPage.ConvMessageSentBubbleMap.TryGetValue(convMessage, out sentChatBubble);
                            currentPage.ConvMessageSentBubbleMap.Remove(convMessage);
                            currentPage.OutgoingMsgsMap.Add(convMessage.MessageId, sentChatBubble);
                        }
                        //if (convMessage.IsSent)
                        //    currentPage.OutgoingMsgsMap.Add(msgId, convMessage);
                        //else
                        //    currentPage.IncomingMessages.Add(convMessage);
                    }
                });
            }
        }

        // this is called in case of gcj
        public static ConversationListObject addGroupChatMessage(ConvMessage convMsg, JObject jsonObj)
        {
            ConversationListObject obj = null;
            List<GroupMembers> gmList = Utils.getGroupMemberList(jsonObj);
            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn)) // represents group is new
            {
                string groupName = Utils.defaultGroupName(gmList); // here name shud be what stored in contacts
                obj = ConversationTableUtils.addGroupConversation(convMsg, groupName);
                ConversationsList.ConvMap.Add(convMsg.Msisdn, obj);

                GroupTableUtils.addGroupMembers(gmList);
                GroupInfo gi = new GroupInfo(gmList[0].GroupId, null, convMsg.GroupParticipant, true);
                GroupTableUtils.addGroupInfo(gi);
            }
            else // add a member to a group
            {
                List<GroupMembers> existingMembers = GroupTableUtils.getActiveGroupMembers(convMsg.Msisdn);
                List<GroupMembers> actualMembersToAdd = getNewMembers(gmList, existingMembers);
                if (actualMembersToAdd == null)
                    return null;
                GroupTableUtils.addGroupMembers(actualMembersToAdd);
                obj = ConversationsList.ConvMap[convMsg.Msisdn];
                GroupInfo gi = GroupTableUtils.getGroupInfoForId(convMsg.Msisdn);
                if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set
                {
                    existingMembers.AddRange(actualMembersToAdd);
                    obj.ContactName = Utils.defaultGroupName(existingMembers);
                }

                obj.LastMessage = convMsg.Message;
                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                ConversationTableUtils.updateConversation(obj);
            }
            addMessage(convMsg);
            return obj;
        }

        private static List<GroupMembers> getNewMembers(List<GroupMembers> gmList, List<GroupMembers> existingMembers)
        {
            List<GroupMembers> newGrpUserList = null;
            for (int j = 0; j < gmList.Count; j++)
            {
                if (!existingMembers.Contains(gmList[j]))
                {
                    if (newGrpUserList == null)
                        newGrpUserList = new List<GroupMembers>();
                    newGrpUserList.Add(gmList[j]);
                }
            }
            return newGrpUserList;
        }

        public static ConversationListObject addChatMessage(ConvMessage convMsg, bool isNewGroup)
        {
            ConversationListObject obj = null;
            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn))
            {
                if (Utils.isGroupConversation(convMsg.Msisdn)&& !isNewGroup) // if its a group chat msg and group does not exist , simply ignore msg.
                    return null;
                
                obj = ConversationTableUtils.addConversation(convMsg, isNewGroup);
                ConversationsList.ConvMap.Add(convMsg.Msisdn, obj);
            }
            else
            {
                obj = ConversationsList.ConvMap[convMsg.Msisdn];
                obj.LastMessage = convMsg.Message;
                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;

                App.WriteToIsoStorageSettings("CONV::" + convMsg.Msisdn,obj);
                //App.ViewModel.ConvMsisdnsToUpdate.Add(convMsg.Msisdn);
                //ConversationTableUtils.updateConversation(obj);
            }
            Stopwatch st1 = Stopwatch.StartNew();
            addMessage(convMsg);
            st1.Stop();
            long msec1 = st1.ElapsedMilliseconds;
            Debug.WriteLine("Time to add chat msg : {0}", msec1);
            return obj;
        }

        private static void updateConvThreadPool(object p)
        {
            ConversationListObject obj = (ConversationListObject)p;
            ConversationTableUtils.updateConversation(obj);
            App.ViewModel.ConvMsisdnsToUpdate.Remove(obj.Msisdn);
        }

        public static void updateMsgStatus(long msgID, int val)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring+";Max Buffer Size = 1024"))
            {
                ConvMessage message = DbCompiledQueries.GetMessagesForMsgId(context, msgID).FirstOrDefault<ConvMessage>();
                if (message != null)
                {
                    if ((int)message.MessageStatus < val)
                    {
                        message.MessageStatus = (ConvMessage.State)val;
                        SubmitWithConflictResolve(context);
                    }
                }
                else
                {
                    // show some logs and errors
                }
            }

        }

        /// <summary>
        /// Thread safe function to update msg status
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string updateAllMsgStatus(long[] ids, int status)
        {
            bool shouldSubmit = false;
            string msisdn = null;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
            {
                for (int i = 0; i < ids.Length; i++)
                {
                    ConvMessage message = DbCompiledQueries.GetMessagesForMsgId(context, ids[i]).FirstOrDefault<ConvMessage>();
                    if (message != null)
                    {
                        if ((int)message.MessageStatus < status)
                        {
                            message.MessageStatus = (ConvMessage.State)status;
                            msisdn = message.Msisdn;
                            shouldSubmit = true;
                        }
                    }
                }
                if (shouldSubmit)
                    SubmitWithConflictResolve(context);
                shouldSubmit = false;
                return msisdn;
            }

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
