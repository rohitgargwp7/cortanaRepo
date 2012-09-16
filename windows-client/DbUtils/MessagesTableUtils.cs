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
using Microsoft.Phone.Shell;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {
        private static object lockObj = new object();

        //keep a set of currently uploading or downloading messages.
        private static Dictionary<long, int> uploadingOrDownloadingMessages = new Dictionary<long, int>();

        public static void addUploadingOrDownloadingMessage(long messageId)
        {
            lock (lockObj)
            {
                if (!uploadingOrDownloadingMessages.ContainsKey(messageId))
                    uploadingOrDownloadingMessages.Add(messageId, -1);
            }
        }

        public static void removeUploadingOrDownloadingMessage(long messageId)
        {
            lock (lockObj)
            {
                if (uploadingOrDownloadingMessages.ContainsKey(messageId))
                    uploadingOrDownloadingMessages.Remove(messageId);
            }
        }

        public static bool isUploadingOrDownloadingMessage(long messageId)
        {
            lock (lockObj)
            {
                return uploadingOrDownloadingMessages.ContainsKey(messageId);
            }
        }

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
                long currentMessageId = convMessage.MessageId;

                context.messages.InsertOnSubmit(convMessage);
                context.SubmitChanges();
                if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    long msgId = convMessage.MessageId;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {

                        NewChatThread currentPage = App.newChatThreadPage;

                        if (currentPage != null)
                        {
                            if (convMessage.IsSent)
                            {
                                SentChatBubble sentChatBubble;
                                currentPage.OutgoingMsgsMap.TryGetValue(currentMessageId, out sentChatBubble);
                                currentPage.OutgoingMsgsMap.Remove(currentMessageId);
                                currentPage.OutgoingMsgsMap.Add(convMessage.MessageId, sentChatBubble);
                                sentChatBubble.MessageId = convMessage.MessageId;
                            }
                        }
                    });
                }
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOIN)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (App.newChatThreadPage != null)
                            App.newChatThreadPage.IsFirstMsg = false;
                    });
                }
            }
        }

        // this is called in case of gcj from Network manager
        public static ConversationListObject addGroupChatMessage(ConvMessage convMsg, JObject jsonObj)
        {
            ConversationListObject obj = null;
            //List<GroupMembers> gmList = Utils.getGroupMemberList(jsonObj);
            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn)) // represents group is new
            {
                string groupName = Utils.defaultGroupName(convMsg.Msisdn); // here name shud be what stored in contacts
                obj = ConversationTableUtils.addGroupConversation(convMsg, groupName);
                ConversationsList.ConvMap.Add(convMsg.Msisdn, obj);
                GroupInfo gi = new GroupInfo(convMsg.Msisdn, null, convMsg.GroupParticipant, true);
                GroupTableUtils.addGroupInfo(gi);
            }
            else // add a member to a group
            {
                List<GroupParticipant> existingMembers = null;
                Utils.GroupCache.TryGetValue(convMsg.Msisdn, out existingMembers);
                if (existingMembers == null)
                    return null;

                obj = ConversationsList.ConvMap[convMsg.Msisdn];
                GroupInfo gi = GroupTableUtils.getGroupInfoForId(convMsg.Msisdn);

                if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set                
                    obj.ContactName = Utils.defaultGroupName(obj.Msisdn);

                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
                {
                    GroupParticipant gp = Utils.GroupCache[obj.Msisdn][Utils.GroupCache[obj.Msisdn].Count - 1]; // get last element of group in sorted order.

                    string text = HikeConstants.USER_JOINED;
                    if (!gp.IsOnHike)
                        text = HikeConstants.USER_INVITED;
                    obj.LastMessage = gp.Name + text;
                    if (PhoneApplicationService.Current.State.ContainsKey("GC_" + convMsg.Msisdn))
                    {
                        obj.IsFirstMsg = true;
                        PhoneApplicationService.Current.State.Remove("GC_" + convMsg.Msisdn);
                    }
                }
                else
                    obj.LastMessage = convMsg.Message;

                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                ConversationTableUtils.updateConversation(obj);
            }
            addMessage(convMsg);
            return obj;
        }

        public static ConversationListObject addChatMessage(ConvMessage convMsg, bool isNewGroup)
        {
            ConversationListObject obj = null;

            if (!ConversationsList.ConvMap.ContainsKey(convMsg.Msisdn))
            {
                if (Utils.isGroupConversation(convMsg.Msisdn) && !isNewGroup) // if its a group chat msg and group does not exist , simply ignore msg.
                    return null;

                obj = ConversationTableUtils.addConversation(convMsg, isNewGroup);
                ConversationsList.ConvMap.Add(convMsg.Msisdn, obj);
            }
            else
            {
                obj = ConversationsList.ConvMap[convMsg.Msisdn];
                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
                {
                    string[] msisdns = NewChatThread.splitUserJoinedMessage(convMsg);
                    GroupParticipant gp = Utils.getGroupParticipant("", msisdns[msisdns.Length - 1], obj.Msisdn);
                    string text = HikeConstants.USER_JOINED;
                    if (!gp.IsOnHike)
                        text = HikeConstants.USER_INVITED;
                    obj.LastMessage = gp.Name + text;

                    if (PhoneApplicationService.Current.State.ContainsKey("GC_" + convMsg.Msisdn)) // this is to store firstMsg logic
                    {
                        obj.IsFirstMsg = true;
                        PhoneApplicationService.Current.State.Remove("GC_" + convMsg.Msisdn);
                        Debug.WriteLine("Phone Application Service : GC_{0} removed.", convMsg.Msisdn);
                    }
                    else
                        obj.IsFirstMsg = false;
                }
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOIN) // shows invite msg
                {
                    if (!obj.IsFirstMsg)
                        return obj;
                    obj.IsFirstMsg = false;

                    string[] msisdns = NewChatThread.splitUserJoinedMessage(convMsg);
                    GroupParticipant gp = Utils.getGroupParticipant("", msisdns[msisdns.Length - 1], obj.Msisdn);
                    string text = HikeConstants.USER_JOINED;
                    if (!gp.IsOnHike && gp.IsDND && !gp.HasOptIn)
                        text = HikeConstants.WAITING_TO_JOIN;
                    obj.LastMessage = gp.Name + text;
                }
                else
                    obj.LastMessage = convMsg.Message;

                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                Stopwatch st = Stopwatch.StartNew();
                ConversationTableUtils.updateConversation(obj);
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to update conversation  : {0}", msec);
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
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
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
