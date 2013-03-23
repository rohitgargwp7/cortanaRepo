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
using System.Text;
using windows_client.Misc;
using windows_client.Languages;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {
        private static object lockObj = new object();

        //keep a set of currently uploading or downloading messages.
        private static Dictionary<long, MyChatBubble> uploadingOrDownloadingMessages = new Dictionary<long, MyChatBubble>();

        public static void addUploadingOrDownloadingMessage(long messageId, MyChatBubble chatBubble)
        {
            if (messageId == -1)
                return;
            lock (lockObj)
            {
                if (!uploadingOrDownloadingMessages.ContainsKey(messageId))
                    uploadingOrDownloadingMessages.Add(messageId, chatBubble);
            }
        }

        public static void removeUploadingOrDownloadingMessage(long messageId)
        {
            if (messageId == -1)
                return;
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

        public static MyChatBubble getUploadingOrDownloadingMessage(long messageId)
        {
            if (messageId == -1)
                return null;
            lock (lockObj)
            {
                if (uploadingOrDownloadingMessages.ContainsKey(messageId))
                {
                    return uploadingOrDownloadingMessages[messageId];
                }
                return null;
            }
        }


        //private static HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring); // use this chatsDbContext to improve performance

        /* This is shown on chat thread screen*/
        public static List<ConvMessage> getMessagesForMsisdn(string msisdn, long lastMessageId, int count)
        {
            List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsisdnForPaging(DbCompiledQueries.chatsDbContext, msisdn, lastMessageId, count).ToList<ConvMessage>();
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
        public static bool addMessage(ConvMessage convMessage)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
            {
                if (convMessage.MappedMessageId > 0)
                {
                    IQueryable<ConvMessage> qq = DbCompiledQueries.GetMessageForMappedMsgIdMsisdn(context, convMessage.Msisdn, convMessage.MappedMessageId, convMessage.Message);
                    ConvMessage cm = qq.FirstOrDefault();
                    if (cm != null)
                        return false;
                }
                long currentMessageId = convMessage.MessageId;
                context.messages.InsertOnSubmit(convMessage);
                try
                {
                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MessagesTableUtils :: addMessage : submit changes, Exception : " + ex.StackTrace);
                    return false;
                }
                
                //if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                //{
                //    long msgId = convMessage.MessageId;
                //    Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    {

                //        NewChatThread currentPage = App.newChatThreadPage;

                //        if (currentPage != null)
                //        {
                //            if (convMessage.IsSent)
                //            {
                //                SentChatBubble sentChatBubble;
                //                currentPage.OutgoingMsgsMap.TryGetValue(currentMessageId, out sentChatBubble);
                //                if (sentChatBubble != null)
                //                {
                //                    currentPage.OutgoingMsgsMap.Remove(currentMessageId);
                //                    currentPage.OutgoingMsgsMap.Add(convMessage.MessageId, sentChatBubble);
                //                    sentChatBubble.MessageId = convMessage.MessageId;
                //                }
                //            }
                //        }
                //    });
                //}
            }
            return true;
        }

        // this is called in case of gcj from Network manager
        public static ConversationListObject addGroupChatMessage(ConvMessage convMsg, JObject jsonObj)
        {
            ConversationListObject obj = null;
            if (!App.ViewModel.ConvMap.ContainsKey(convMsg.Msisdn)) // represents group is new
            {
                bool success = addMessage(convMsg);
                if (!success)
                    return null;
                string groupName = GroupManager.Instance.defaultGroupName(convMsg.Msisdn);
                obj = ConversationTableUtils.addGroupConversation(convMsg, groupName);
                App.ViewModel.ConvMap[convMsg.Msisdn] = obj;
                GroupInfo gi = new GroupInfo(convMsg.Msisdn, null, convMsg.GroupParticipant, true);
                GroupTableUtils.addGroupInfo(gi);
            }
            else // add a member to a group
            {
                List<GroupParticipant> existingMembers = null;
                GroupManager.Instance.GroupCache.TryGetValue(convMsg.Msisdn, out existingMembers);
                if (existingMembers == null)
                    return null;

                obj = App.ViewModel.ConvMap[convMsg.Msisdn];
                GroupInfo gi = GroupTableUtils.getGroupInfoForId(convMsg.Msisdn);

                if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set                
                    obj.ContactName = GroupManager.Instance.defaultGroupName(obj.Msisdn);

                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.MEMBERS_JOINED)
                {
                    string[] vals = convMsg.Message.Split(';');
                    if (vals.Length == 2)
                        obj.LastMessage = vals[1];
                    else
                        obj.LastMessage = convMsg.Message;
                }
                else
                    obj.LastMessage = convMsg.Message;

                bool success = addMessage(convMsg);
                if (!success)
                    return null;

                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                obj.LastMsgId = convMsg.MessageId;
                ConversationTableUtils.updateConversation(obj);
            }

            return obj;
        }

        public static ConversationListObject addChatMessage(ConvMessage convMsg, bool isNewGroup)
        {
            if (convMsg == null)
                return null;
            ConversationListObject obj = null;
            if (!App.ViewModel.ConvMap.ContainsKey(convMsg.Msisdn))
            {
                if (Utils.isGroupConversation(convMsg.Msisdn) && !isNewGroup) // if its a group chat msg and group does not exist , simply ignore msg.
                    return null;
                // if status update dont create a new conversation if not already there
                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                    return null;

                obj = ConversationTableUtils.addConversation(convMsg, isNewGroup);
                App.ViewModel.ConvMap.Add(convMsg.Msisdn, obj);
            }
            else
            {
                obj = App.ViewModel.ConvMap[convMsg.Msisdn];
                obj.IsLastMsgStatusUpdate = false;
                #region PARTICIPANT_JOINED
                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
                {
                    obj.LastMessage = convMsg.Message;

                    GroupInfo gi = GroupTableUtils.getGroupInfoForId(convMsg.Msisdn);
                    if (gi == null)
                        return null;
                    if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set
                        obj.ContactName = GroupManager.Instance.defaultGroupName(convMsg.Msisdn);
                }
                #endregion
                #region PARTICIPANT_LEFT
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_GROUP_USER)
                {
                    obj.LastMessage = convMsg.Message;
                    GroupInfo gi = GroupTableUtils.getGroupInfoForId(convMsg.Msisdn);
                    if (gi == null)
                        return null;
                    if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set
                        obj.ContactName = GroupManager.Instance.defaultGroupName(convMsg.Msisdn);
                }
                #endregion
                #region GROUP_JOINED_OR_WAITING
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_JOINED_OR_WAITING) // shows invite msg
                {
                    string[] vals = Utils.splitUserJoinedMessage(convMsg.Message);
                    List<string> waitingParticipants = null;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        string[] vars = vals[i].Split(HikeConstants.DELIMITERS, StringSplitOptions.RemoveEmptyEntries); // msisdn:0 or msisdn:1

                        // every participant is either on DND or not on DND
                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, vars[0], convMsg.Msisdn);
                        if (vars[1] == "0") // DND USER and not OPTED IN
                        {
                            if (waitingParticipants == null)
                                waitingParticipants = new List<string>();
                            waitingParticipants.Add(gp.FirstName);
                        }
                    }
                    if (waitingParticipants != null && waitingParticipants.Count > 0) // show waiting msg
                    {
                        StringBuilder msgText = new StringBuilder();
                        if (waitingParticipants.Count == 1)
                            msgText.Append(waitingParticipants[0]);
                        else if (waitingParticipants.Count == 2)
                            msgText.Append(waitingParticipants[0] + AppResources.And_txt + waitingParticipants[1]);
                        else
                        {
                            for (int i = 0; i < waitingParticipants.Count; i++)
                            {
                                msgText.Append(waitingParticipants[0]);
                                if (i == waitingParticipants.Count - 2)
                                    msgText.Append(AppResources.And_txt);
                                else if (i < waitingParticipants.Count - 2)
                                    msgText.Append(",");
                            }
                        }
                        obj.LastMessage = string.Format(AppResources.WAITING_TO_JOIN, msgText.ToString());
                    }
                    else
                    {
                        string[] vars = vals[vals.Length - 1].Split(':');
                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, vars[0], convMsg.Msisdn);
                        string text = AppResources.USER_JOINED_GROUP_CHAT;
                        obj.LastMessage = gp.FirstName + text;
                    }
                }
                #endregion
                #region USER_OPT_IN
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
                {
                    if (Utils.isGroupConversation(obj.Msisdn))
                    {
                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, convMsg.Message, obj.Msisdn);
                        obj.LastMessage = gp.FirstName + AppResources.USER_JOINED_GROUP_CHAT;
                    }
                    else
                    {
                        obj.LastMessage = obj.NameToShow + AppResources.USER_OPTED_IN_MSG;
                    }
                    convMsg.Message = obj.LastMessage;
                }
                #endregion
                #region CREDITS_GAINED
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
                {
                    obj.LastMessage = convMsg.Message;
                }
                #endregion
                #region DND_USER
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
                {
                    obj.LastMessage = string.Format(AppResources.DND_USER, obj.NameToShow);
                    convMsg.Message = obj.LastMessage;
                }
                #endregion
                #region USER_JOINED
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED)
                {
                    if (Utils.isGroupConversation(obj.Msisdn))
                    {
                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, convMsg.Message, obj.Msisdn);
                        obj.LastMessage = string.Format(AppResources.USER_JOINED_HIKE, gp.FirstName);
                    }
                    else // 1-1 chat
                    {
                        obj.LastMessage = string.Format(AppResources.USER_JOINED_HIKE, obj.NameToShow);
                    }
                    convMsg.Message = obj.LastMessage;
                }
                #endregion\
                #region GROUP NAME/PIC CHANGED
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED)
                {
                    obj.LastMessage = convMsg.Message;
                }
                #endregion
                #region STATUS UPDATES
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    obj.IsLastMsgStatusUpdate = true;
                    obj.LastMessage = "\""+convMsg.Message+"\"";
                }
                #endregion
                #region NO_INFO or OTHER MSGS
                else
                    obj.LastMessage = convMsg.Message;
                #endregion

                Stopwatch st1 = Stopwatch.StartNew();
                bool success = addMessage(convMsg);
                if (!success)
                    return null;
                st1.Stop();
                long msec1 = st1.ElapsedMilliseconds;
                Debug.WriteLine("Time to add chat msg : {0}", msec1);

                if (convMsg.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    obj.MessageStatus = convMsg.MessageStatus;
                    obj.TimeStamp = convMsg.Timestamp;
                }
                else if (obj.MessageStatus != ConvMessage.State.RECEIVED_UNREAD)// its for status msg
                {
                        obj.MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                
                obj.LastMsgId = convMsg.MessageId;
                Stopwatch st = Stopwatch.StartNew();
                ConversationTableUtils.updateConversation(obj);
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to update conversation  : {0}", msec);
            }
            return obj;
        }

        public static string updateMsgStatus(string fromUser, long msgID, int val)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
            {
                ConvMessage message = DbCompiledQueries.GetMessagesForMsgId(context, msgID).FirstOrDefault<ConvMessage>();
                if (message != null)
                {
                    if ((int)message.MessageStatus < val)
                    {
                        if (fromUser == null || fromUser == message.Msisdn)
                        {
                            message.MessageStatus = (ConvMessage.State)val;
                            SubmitWithConflictResolve(context);
                            return message.Msisdn;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// Thread safe function to update msg status
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static string updateAllMsgStatus(string fromUser, long[] ids, int status)
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
                            if (fromUser == null || fromUser == message.Msisdn)
                            {
                                message.MessageStatus = (ConvMessage.State)status;
                                msisdn = message.Msisdn;
                                shouldSubmit = true;
                            }
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
            catch (ChangeConflictException ex)
            {
                Debug.WriteLine("MessageTableUtils :: SubmitWithConflictResolve : submitChanges, Exception : " + ex.StackTrace);
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
