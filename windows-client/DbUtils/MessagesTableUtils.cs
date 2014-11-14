﻿﻿using System.Linq;
using windows_client.Model;
using System.Collections.Generic;
using System;
using windows_client.utils;
using System.Data.Linq;
using Newtonsoft.Json.Linq;
using windows_client.Controls;
using System.Diagnostics;
using System.Text;
using windows_client.Misc;
using windows_client.Languages;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Phone.Shell;

namespace windows_client.DbUtils
{
    public class MessagesTableUtils
    {
        private static object lockObj = new object();

        //private static HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring); // use this chatsDbContext to improve performance

        /* This is shown on chat thread screen*/
        public static List<ConvMessage> getMessagesForMsisdn(string msisdn, long lastMessageId, int count)
        {
            using (HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                List<ConvMessage> res = DbCompiledQueries.GetMessagesForMsisdnForPaging(chatsDbContext, msisdn, lastMessageId, count).ToList<ConvMessage>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        /// <summary>
        /// DB Call to retrieve message by MessageID
        /// </summary>
        /// <param name="lastMessageId">message ID</param>
        /// <returns>ConvMessage</returns>
        public static ConvMessage getMessagesForMsgId(long lastMessageId)
        {
            using (HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConvMessage res = DbCompiledQueries.GetMessagesForMsgId(chatsDbContext, lastMessageId).FirstOrDefault<ConvMessage>();
                return res;
            }
        }

        /// <summary>
        /// Db Call to retrieve pin history for a group
        /// </summary>
        /// <param name="msisdn">msisdn of group</param>
        /// <returns>list of pins</returns>
        public static List<ConvMessage> getPinMessagesForMsisdn(string msisdn, long lastmessageId, int count)
        {
            using (HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                List<ConvMessage> res = DbCompiledQueries.GetPinMessagesForMsisdnForPaging(chatsDbContext, msisdn, lastmessageId, count).ToList<ConvMessage>();
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

        public static ConvMessage getSecondLastMessageForMsisdn(string msisdn)
        {
            ConvMessage res;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                res = DbCompiledQueries.GetLastSecondMessageForMsisdn(context, msisdn).FirstOrDefault<ConvMessage>();
                return res;
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
            }
            return true;
        }


        /// <summary>
        /// Insert messages at once
        /// </summary>
        /// <param name="listMessages">list of non duplicate messages</param>
        /// <returns></returns>
        public static bool BulkInsertMessage(IList<ConvMessage> listMessages)
        {
            if (listMessages == null || listMessages.Count == 0)
                return false;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
            {
                context.messages.InsertAllOnSubmit(listMessages);
                try
                {
                    context.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MessagesTableUtils :: addMessage : submit changes, Exception : " + ex.StackTrace);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Removes duplicate messages from the list of messages passed
        /// </summary>
        /// <param name="listConvMessage"></param>
        public static void FilterDuplicateMessage(List<ConvMessage> listConvMessage)
        {
            if (listConvMessage == null || listConvMessage.Count == 0)
                return;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024;"))
            {
                for (int i = 0; i < listConvMessage.Count; )
                {
                    ConvMessage convMessage = listConvMessage[i];
                    if (convMessage.MappedMessageId > 0)
                    {
                        IQueryable<ConvMessage> qq = DbCompiledQueries.GetMessageForMappedMsgIdMsisdn(context, convMessage.Msisdn, convMessage.MappedMessageId, convMessage.Message);
                        ConvMessage cm = qq.FirstOrDefault();
                        if (cm != null)
                            listConvMessage.RemoveAt(i);
                        else
                            i++;
                    }
                    else
                        i++;
                }
            }
        }
        private static void SaveLongMessage(ConvMessage convMessage)
        {
            convMessage.TempLongMessage = convMessage.Message;
            SaveLongMessageFile(convMessage.Message, convMessage.Msisdn, convMessage.Timestamp);
            convMessage.Message = String.Empty;

            if (String.IsNullOrEmpty(convMessage.MetaDataString))
                convMessage.MetaDataString = "{lm:1}";
            else
            {
                JObject metaData = JObject.Parse(convMessage.MetaDataString);
                metaData[HikeConstants.LONG_MESSAGE] = "1";
                convMessage.MetaDataString = metaData.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        // this is called in case of gcj from Network manager
        public static ConversationListObject addGroupChatMessage(ConvMessage convMsg, string gName)
        {
            ConversationListObject obj = null;
            if (!App.ViewModel.ConvMap.ContainsKey(convMsg.Msisdn)) // represents group is new
            {
                string groupName = string.IsNullOrEmpty(gName) ? GroupManager.Instance.defaultGroupName(convMsg.Msisdn) : gName;
                PhoneApplicationService.Current.State[convMsg.Msisdn] = groupName;
                obj = ConversationTableUtils.addConversation(convMsg, true, null);
                App.ViewModel.ConvMap[convMsg.Msisdn] = obj;
                GroupInfo gi = new GroupInfo(convMsg.Msisdn, groupName, convMsg.GroupParticipant, true);
                GroupTableUtils.addGroupInfo(gi);
            }
            else // add a member to a group
            {
                List<GroupParticipant> existingMembers = null;
                GroupManager.Instance.GroupParticpantsCache.TryGetValue(convMsg.Msisdn, out existingMembers);
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

                obj.MessageStatus = convMsg.MessageStatus;
                obj.TimeStamp = convMsg.Timestamp;
                obj.LastMsgId = convMsg.MessageId;
                ConversationTableUtils.updateConversation(obj);
            }

            return obj;
        }

        public static ConversationListObject addChatMessage(ConvMessage convMsg, bool isNewGroup, byte[] imageBytes = null, string from = "")
        {
            UpdateConvMessageText(convMsg, from);
            if (addMessage(convMsg))
            {
                //should be done before updating conversation
                if (!string.IsNullOrEmpty(convMsg.TempLongMessage))
                {
                    convMsg.Message = convMsg.TempLongMessage;
                    convMsg.TempLongMessage = null;
                }
                ConversationListObject cobj = UpdateConversationList(convMsg, isNewGroup, imageBytes, from);
                if (cobj != null && convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                {
                    ProcessConversationMetadata(convMsg, cobj);
                    ConversationTableUtils.updateConversation(cobj);
                }

                return cobj;
            }
            return null;
        }

        /// <summary>
        /// updates conversationlist according to paricipant info and update conversation file
        /// </summary>
        /// <param name="convMsg"></param>
        /// <param name="isNewGroup"></param>
        /// <param name="imageBytes"></param>
        /// <param name="from"></param>
        /// <returns></returns>
        public static ConversationListObject UpdateConversationList(ConvMessage convMsg, bool isNewGroup, byte[] imageBytes = null, string from = "")
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

                obj = ConversationTableUtils.addConversation(convMsg, isNewGroup, imageBytes, from);
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
                }
                #endregion
                #region PARTICIPANT_LEFT
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_GROUP_USER)
                {
                    obj.LastMessage = convMsg.Message;
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
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, vars[0], convMsg.Msisdn);
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
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, vars[0], convMsg.Msisdn);
                        obj.LastMessage = String.Format(AppResources.USER_JOINED_GROUP_CHAT, gp.FirstName);
                    }
                }
                #endregion
                #region USER_OPT_IN
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
                {
                    if (Utils.isGroupConversation(obj.Msisdn))
                    {
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMsg.Message, obj.Msisdn);
                        obj.LastMessage = String.Format(AppResources.USER_JOINED_GROUP_CHAT, gp.FirstName);
                    }
                    else
                    {
                        obj.LastMessage = String.Format(AppResources.USER_OPTED_IN_MSG, obj.NameToShow);
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
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_REJOINED)
                {
                    string msgtext = convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED ? AppResources.USER_JOINED_HIKE : AppResources.USER_REJOINED_HIKE_TXT;
                    if (Utils.isGroupConversation(obj.Msisdn))
                    {
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMsg.Message, obj.Msisdn);
                        obj.LastMessage = string.Format(msgtext, gp.FirstName);
                    }
                    else // 1-1 chat
                    {
                        obj.LastMessage = string.Format(msgtext, obj.NameToShow);
                    }
                    convMsg.Message = obj.LastMessage;
                }
                #endregion
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
                    obj.LastMessage = "\"" + convMsg.Message + "\"";
                }
                #endregion
                #region NO_INFO
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO
                    || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                {
                    string toastText = String.Empty;

                    //convMsg.GroupParticipant is null means message sent by urself
                    if (convMsg.GroupParticipant != null && Utils.isGroupConversation(convMsg.Msisdn))
                    {
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMsg.GroupParticipant, convMsg.Msisdn);

                        if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                        {
                            toastText = gp != null ? (gp.FirstName + " " + HikeConstants.TOAST_FOR_PIN + " - " + convMsg.Message) : convMsg.Message;
                            obj.LastMessage = gp != null ? (gp.FirstName + " - " + convMsg.Message) : convMsg.Message;
                        }
                        else
                        {
                            toastText = gp != null ? (gp.FirstName + " - " + convMsg.Message) : convMsg.Message;
                            obj.LastMessage = toastText;
                        }

                        if (obj.IsHidden)
                            toastText = HikeConstants.TOAST_FOR_HIDDEN_MODE;
                        else if (App.appSettings.Contains(App.HIDE_MESSAGE_PREVIEW_SETTING))
                        {
                            toastText = GetToastNotification(convMsg);
                            toastText = gp != null ? (gp.FirstName + " - " + toastText) : toastText;
                        }

                        obj.ToastText = toastText;
                    }
                    else
                    {
                        obj.LastMessage = toastText = convMsg.Message;

                        if (obj.IsHidden)
                            toastText = HikeConstants.TOAST_FOR_HIDDEN_MODE;
                        else if (App.appSettings.Contains(App.HIDE_MESSAGE_PREVIEW_SETTING))
                            toastText = GetToastNotification(convMsg);

                        obj.ToastText = toastText;
                    }
                }
                #endregion
                #region Chat Background Changed
                else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED)
                {
                    if (!Utils.isGroupConversation(from))
                    {
                        if (from == App.MSISDN)
                            convMsg.Message = obj.LastMessage = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
                        else
                            convMsg.Message = obj.LastMessage = string.Format(AppResources.ChatBg_Changed_Text, obj.NameToShow);
                    }
                    else
                        obj.LastMessage = convMsg.Message;

                    if (obj.IsHidden)
                        obj.ToastText = HikeConstants.TOAST_FOR_HIDDEN_MODE;
                }
                #endregion
                #region OTHER MSGS
                else
                    obj.LastMessage = convMsg.Message;
                #endregion

                if (convMsg.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    obj.MessageStatus = convMsg.MessageStatus;
                    obj.TimeStamp = convMsg.Timestamp;
                }
                else if (obj.MessageStatus != ConvMessage.State.RECEIVED_UNREAD)// its for status msg
                {
                    obj.MessageStatus = ConvMessage.State.RECEIVED_READ;
                }

                if (obj.LastMessage.Length > 100)
                    obj.LastMessage = obj.LastMessage.Substring(0, 100);

                obj.LastMsgId = convMsg.MessageId;
                ConversationTableUtils.updateConversation(obj);
            }
            return obj;
        }


        /// <summary>
        /// Updates Conversation Message display text on basis of conditions 
        /// </summary>
        /// <param name="convMsg">ConvMessage to be updated</param>
        /// <param name="from"></param>
        public static void UpdateConvMessageText(ConvMessage convMsg, string from)
        {
            string nameToShow;
            if (App.ViewModel.ConvMap.ContainsKey(convMsg.Msisdn))
                nameToShow = App.ViewModel.ConvMap[convMsg.Msisdn].NameToShow;
            else if (Utils.IsHikeBotMsg(convMsg.Msisdn))
                nameToShow = Utils.GetHikeBotName(convMsg.Msisdn);
            else
            {
                ContactInfo contactInfo = ContactUtils.GetContactInfo(convMsg.Msisdn);
                nameToShow = contactInfo == null ? string.Empty : contactInfo.Name;
            }

            #region USER_OPT_IN
            if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
            {
                if (Utils.isGroupConversation(convMsg.Msisdn))
                {
                    GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMsg.Message, convMsg.Msisdn);
                    convMsg.Message = String.Format(AppResources.USER_JOINED_GROUP_CHAT, gp.FirstName);
                }
                else
                {
                    convMsg.Message = String.Format(AppResources.USER_OPTED_IN_MSG, nameToShow);
                }
            }
            #endregion
            #region Chat Background Changed
            else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED)
            {
                if (!Utils.isGroupConversation(from))
                {
                    if (from == App.MSISDN)
                        convMsg.Message = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
                    else
                        convMsg.Message = string.Format(AppResources.ChatBg_Changed_Text, nameToShow);
                }
            }
            #endregion
            #region DND_USER
            else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
            {
                convMsg.Message = string.Format(AppResources.DND_USER, nameToShow);
            }
            #endregion
            #region USER_JOINED
            else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_REJOINED)
            {
                string msgtext = convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED ? AppResources.USER_JOINED_HIKE : AppResources.USER_REJOINED_HIKE_TXT;
                if (Utils.isGroupConversation(convMsg.Msisdn))
                {
                    GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMsg.Message, convMsg.Msisdn);
                    convMsg.Message = string.Format(msgtext, gp.FirstName);
                }
                else // 1-1 chat
                {
                    convMsg.Message = string.Format(msgtext, nameToShow);
                }
            }
            #endregion
            #region handle long message
            if (convMsg.Message.Length > 4000)
            {
                SaveLongMessage(convMsg);
            }
            #endregion
           
        }

        /// <summary>
        /// Updates metadata related to pin for conversation object 
        /// </summary>
        /// <param name="convMsg">PinMessge</param>
        /// <param name="obj">Conversation List object</param>
        public static void ProcessConversationMetadata(ConvMessage convMsg, ConversationListObject obj)
        {
            JObject metaData = new JObject();

            if (obj.MetaData == null || obj.MetaData.Value<long>(HikeConstants.TIMESTAMP) < convMsg.Timestamp) //latest pin wins
            {
                metaData[HikeConstants.PINID] = convMsg.MessageId;
                metaData[HikeConstants.TIMESTAMP] = convMsg.Timestamp;
            }
            else
            {
                metaData[HikeConstants.PINID] = obj.MetaData[HikeConstants.PINID];
                metaData[HikeConstants.TIMESTAMP] = obj.MetaData[HikeConstants.TIMESTAMP];
            }

            metaData[HikeConstants.READPIN] = (convMsg.IsSent) ? true : false;

            if (obj.MetaData == null) //check for "should unread counter be increased??"
                metaData[HikeConstants.UNREADPINS] = convMsg.IsSent ? 0 : 1; //if I pinned 0 unread
            else
                metaData[HikeConstants.UNREADPINS] = (convMsg.IsSent) ? obj.MetaData.Value<int>(HikeConstants.UNREADPINS) : obj.MetaData.Value<int>(HikeConstants.UNREADPINS) + 1;

            obj.MetaData = metaData;
        }

        /// <summary>
        /// Creates in-app toast string while Message Preview is off
        /// </summary>
        /// <param name="convMsg"></param>
        /// <returns></returns>
        private static string GetToastNotification(ConvMessage convMsg)
        {
            string toastText = HikeConstants.TOAST_FOR_MESSAGE;

            if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                toastText = HikeConstants.TOAST_FOR_PIN;
            else if (!String.IsNullOrEmpty(convMsg.MetaDataString) && convMsg.MetaDataString.Contains(HikeConstants.STICKER_ID))
                toastText = HikeConstants.TOAST_FOR_STICKER;
            else if (convMsg.FileAttachment != null)
            {
                if (convMsg.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                    toastText = HikeConstants.TOAST_FOR_PHOTO;
                else if (convMsg.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                    toastText = HikeConstants.TOAST_FOR_AUDIO;
                else if (convMsg.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                    toastText = HikeConstants.TOAST_FOR_VIDEO;
                else if (convMsg.FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                    toastText = HikeConstants.TOAST_FOR_CONTACT;
                else if (convMsg.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                    toastText = HikeConstants.TOAST_FOR_LOCATION;
                else
                    toastText = HikeConstants.TOAST_FOR_FILE;
            }

            return toastText;
        }

        public static string updateMsgStatus(string fromUser, long msgID, int val)
        {
            try
            {
                using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
                {
                    ConvMessage message = DbCompiledQueries.GetMessagesForMsgId(context, msgID).FirstOrDefault<ConvMessage>();
                    if (message != null)
                    {
                        var msgState = (ConvMessage.State)val;

                        if (message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED
                            || message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED
                            || message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                        {
                            if (msgState == ConvMessage.State.SENT_DELIVERED)
                                val = (int)ConvMessage.State.FORCE_SMS_SENT_DELIVERED;
                            else if (msgState == ConvMessage.State.SENT_DELIVERED_READ)
                                val = (int)ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;
                        }

                        //hack to update db for sent socket write
                        if ((int)message.MessageStatus < val ||
                                (message.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE &&
                                    (val == (int)ConvMessage.State.SENT_CONFIRMED || val == (int)ConvMessage.State.SENT_DELIVERED || val == (int)ConvMessage.State.SENT_DELIVERED_READ)))
                        {
                            if (fromUser == null || fromUser == message.Msisdn)
                            {
                                message.MessageStatus = (ConvMessage.State)val;
                                SubmitWithConflictResolve(context);
                                Debug.WriteLine("MESSAGE STATUS UPDATE:ID:{0},STATUS:{1}", message.MessageId, message.MessageStatus);
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MessagetableUtils::updateMsgStatus,messagestatus:{0},exception:{1},stacktrace:{2}", val, ex.Message, ex.StackTrace);
            }

            return null;
        }

        public static IList<long> updateBulkMsgDeliveredStatus(string msisdn, long msgID)
        {
            IList<long> listUpdatedMsgIds = new List<long>();
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
            {
                IList<ConvMessage> listMessages = DbCompiledQueries.GetUndeliveredMessagesForMsisdn(context, msgID, msisdn).ToList<ConvMessage>();
                foreach (var message in listMessages)
                {
                    if (message != null)
                    {
                        ConvMessage.State newMessageState = ConvMessage.State.SENT_DELIVERED;

                        if (message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED)
                            newMessageState = ConvMessage.State.FORCE_SMS_SENT_DELIVERED;

                        message.MessageStatus = newMessageState;
                        listUpdatedMsgIds.Add(message.MessageId);
                    }
                }
                if (listUpdatedMsgIds.Count > 0)
                    SubmitWithConflictResolve(context);
            }
            return listUpdatedMsgIds;
        }

        public static IList<long> updateBulkMsgReadStatus(string msisdn, long msgID)
        {
            IList<long> listUpdatedMsgIds = new List<long>();
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
            {
                IList<ConvMessage> listMessages = DbCompiledQueries.GetDeliveredUnreadMessagesForMsisdn(context, msgID, msisdn).ToList<ConvMessage>();
                foreach (var message in listMessages)
                {
                    if (message != null)
                    {
                        ConvMessage.State newMessageState = ConvMessage.State.SENT_DELIVERED_READ;
                        if (message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED)
                            newMessageState = ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;

                        message.MessageStatus = newMessageState;
                        listUpdatedMsgIds.Add(message.MessageId);
                    }
                }
                if (listUpdatedMsgIds.Count > 0)
                    SubmitWithConflictResolve(context);
            }
            return listUpdatedMsgIds;
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

            try
            {
                List<ConvMessage> messageList = new List<ConvMessage>();

                using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
                {
                    for (int i = 0; i < ids.Length; i++)
                        messageList.Add(DbCompiledQueries.GetMessagesForMsgId(context, ids[i]).FirstOrDefault<ConvMessage>());

                    foreach (var message in messageList)
                    {
                        var val = status;

                        if (message != null)
                        {
                            if (message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED
                               || message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED
                               || message.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                            {
                                var msgState = (ConvMessage.State)val;

                                if (msgState == ConvMessage.State.SENT_DELIVERED)
                                    val = (int)ConvMessage.State.FORCE_SMS_SENT_DELIVERED;
                                else if (msgState == ConvMessage.State.SENT_DELIVERED_READ)
                                    val = (int)ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;
                            }

                            //hack to update db for sent socket write
                            if ((int)message.MessageStatus < val ||
                                    (message.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE &&
                                        (val == (int)ConvMessage.State.SENT_CONFIRMED || val == (int)ConvMessage.State.SENT_DELIVERED || val == (int)ConvMessage.State.SENT_DELIVERED_READ)))
                            {
                                if (fromUser == null || fromUser == message.Msisdn)
                                {
                                    message.MessageStatus = (ConvMessage.State)val;

                                    msisdn = message.Msisdn;
                                    shouldSubmit = true;
                                }
                            }
                        }
                    }

                    if (shouldSubmit)
                        SubmitWithConflictResolve(context);
                }

                messageList.Clear();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MessageTableUtils::updateAllMsgStatus,Exception:{0},StackTrace:{1}", ex.Message, ex.StackTrace);
            }
            return msisdn;
        }

        public static long GetLastSentMessageId(string msisdn)
        {
            long maxReturnId = 0;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                var convMessage = DbCompiledQueries.GetLastSentMsgId(context, msisdn).FirstOrDefault<ConvMessage>();
                if (convMessage != null)
                {
                    maxReturnId = convMessage.MessageId;
                }
            }

            return maxReturnId;
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

        /// <summary>
        /// Delete all messages from db
        /// </summary>
        /// <param name="msisdn">user id</param>
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

        private const string LONG_MSG_DIRECTORY = "LONGMSG";

        public static void SaveLongMessageFile(string message, string msisdn, long timestamp)
        {
            lock (lockObj)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        msisdn = msisdn.Replace(':', '_');
                        if (!store.DirectoryExists(LONG_MSG_DIRECTORY))
                        {
                            store.CreateDirectory(LONG_MSG_DIRECTORY);
                        }
                        string msidnDirectory = LONG_MSG_DIRECTORY + "\\" + msisdn;
                        if (!store.DirectoryExists(msidnDirectory))
                        {
                            store.CreateDirectory(msidnDirectory);
                        }
                        string fileName = msidnDirectory + "\\" + timestamp;

                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);

                        using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.WriteStringBytes(message);
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MessageTableUtils::SaveLongMessage, Exception:", ex.Message);
                }
            }
        }

        public static string ReadLongMessageFile(long timestamp, string msisdn)
        {
            string message = string.Empty;
            lock (lockObj)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        msisdn = msisdn.Replace(':', '_');
                        string fileName = LONG_MSG_DIRECTORY + "\\" + msisdn + "\\" + timestamp;
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                using (BinaryReader reader = new BinaryReader(file))
                                {
                                    int count = reader.ReadInt32();
                                    message = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    reader.Close();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MessageTableUtils :: ReadLongMessage, Exception:", ex.Message);
                }
            }
            return message;
        }

        /// <summary>
        /// delete long messages for given user id
        /// </summary>
        /// <param name="msisdn">user id</param>
        public static void DeleteLongMessages(string msisdn)
        {
            lock (lockObj)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    msisdn = msisdn.Replace(':', '_');
                    try
                    {
                        string msisdnDirectory = LONG_MSG_DIRECTORY + "\\" + msisdn;
                        if (store.DirectoryExists(msisdnDirectory))
                        {
                            string[] files = store.GetFileNames(msisdnDirectory + "\\*");
                            if (files != null)
                                foreach (string fileName in files)
                                    store.DeleteFile(msisdnDirectory + "\\" + fileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: deleteConversation : deleteConversation , Exception : " + ex.StackTrace);
                    }
                }
            }

        }

        public static void DeleteAllLongMessages()
        {
            lock (lockObj)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    try
                    {
                        if (store.DirectoryExists(LONG_MSG_DIRECTORY))
                        {
                            string[] directories = store.GetDirectoryNames(LONG_MSG_DIRECTORY + "\\*");
                            if (directories != null)
                                foreach (string msisdn in directories)
                                {
                                    string msisdnDirectory = LONG_MSG_DIRECTORY + "\\" + msisdn;

                                    string[] files = store.GetFileNames(msisdnDirectory + "\\*");
                                    if (files != null)
                                        foreach (string fileName in files)
                                            store.DeleteFile(msisdnDirectory + "\\" + fileName);
                                }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationTableUtils :: deleteConversation : deleteConversation , Exception : " + ex.StackTrace);
                    }
                }
            }

        }
    }
}
