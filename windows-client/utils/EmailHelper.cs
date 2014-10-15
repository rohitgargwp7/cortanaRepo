﻿using Microsoft.Phone.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using windows_client.Model;
using windows_client.DbUtils;
using System.Diagnostics;
using windows_client.Misc;
using windows_client.Languages;
using Microsoft.Phone.Shell;

namespace windows_client.utils
{
    static class EmailHelper
    {
        /// <summary>
        /// function to send email
        /// </summary>
        /// <param name="subject">subject of email</param>
        /// <param name="body">body of email</param>
        /// <param name="recipient">recipient</param>
        /// <param name="cc">carbon copy</param>
        /// <param name="bcc">blind carbon copy</param>
        public static void SendEmail(string subject = "", string body = "", string recipient = "", string cc = "", string bcc = "")
        {
            try
            {
                EmailComposeTask emailComposeTask = new EmailComposeTask();
                emailComposeTask.Subject = subject;
                emailComposeTask.Body = body;
                emailComposeTask.To = recipient;
                emailComposeTask.Cc = cc;
                emailComposeTask.Bcc = bcc;
                emailComposeTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EmailHelper::SendMail:" + ex.StackTrace);
            }
        }

        /// <summary>
        /// reads stack of message and builds a string for email body
        /// </summary>
        /// <param name="messagesStack">stack of message</param>
        /// <returns>concatenated string of messages</returns>
        private static string GetEmailString(Stack<String> messagesStack)
        {
            StringBuilder sb = new StringBuilder();

            while (messagesStack.Count > 0)
                sb.AppendLine(messagesStack.Pop()); // appending using new line

            return sb.ToString();
        }

        /// <summary>
        /// fetch mail from database and sends mail
        /// </summary>
        /// <param name="msisdn">msisdn of user or group</param>
        /// <param name="displayName">username or group name</param>
        /// <param name="isGroupChat">group chat or not</param>
        /// <returns>void</returns>
        public static async Task FetchAndEmail(string msisdn, string displayName, bool isGroupChat)
        {
            // Fetch messages asynchronously
            await Task.Run(() =>
                {
                    int bytesConsumed = 0;
                    int sizeOfChar = sizeof(char);
                    long lastMessageId = long.MaxValue;
                    bool isFirstTime = true;
                    int msgcount = 0;
                    List<ConvMessage> convList;
                    string currentMessage;
                    string messageTime;
                    string messageSender;
                    string messageText;
                    string contentType;
                    string subject = string.Format(AppResources.EmailConv_Subject_Txt, displayName);
                    Dictionary<string, string> groupChatParticipantInfo = null;
                    Attachment attachment;
                    StringBuilder emailText = new StringBuilder();
                    bool isShowTruncatedText = false;
                    Stack<string> messagesStack = new Stack<string>();
                    bool hasMoreMsg;

                    do
                    {
                        hasMoreMsg = false;

                        convList = MessagesTableUtils.getMessagesForMsisdn(msisdn, lastMessageId, HikeConstants.EmailConversation.CHAT_FETCH_LIMIT);

                        if (convList != null)
                        {
                            if (isGroupChat && isFirstTime)
                            {
                                ContactInfo contactObj;
                                groupChatParticipantInfo = new Dictionary<string, string>();
                                List<GroupParticipant> grpParticipantList = GroupManager.Instance.GetParticipantList(msisdn);

                                string participantName;

                                foreach (GroupParticipant grpParticipant in grpParticipantList)
                                {
                                    participantName = grpParticipant.Name;

                                    if (string.IsNullOrEmpty(participantName))
                                    {
                                        contactObj = UsersTableUtils.getContactInfoFromMSISDN(grpParticipant.Msisdn);

                                        if (contactObj == null)
                                            participantName = grpParticipant.Msisdn;
                                        else
                                            participantName = contactObj.NameToshow;
                                    }

                                    groupChatParticipantInfo.Add(grpParticipant.Msisdn, participantName);
                                }
                            }

                            foreach (ConvMessage convMsg in convList)
                            {
                                contentType = string.Empty; // Reset content type
                                messageTime = TimeUtils.getTimeStringForEmailConversation(convMsg.Timestamp);

                                if (convMsg.HasAttachment)
                                {
                                    attachment = MiscDBUtil.getFileAttachment(msisdn, convMsg.MessageId.ToString());

                                    if (attachment != null)
                                        contentType = attachment.ContentType;

                                    if (contentType.Contains(HikeConstants.IMAGE))
                                        messageText = AppResources.EmailConv_SharedImage_Txt;
                                    else if (contentType.Contains(HikeConstants.AUDIO))
                                        messageText = AppResources.EmailConv_SharedAudio_Txt;
                                    else if (contentType.Contains(HikeConstants.VIDEO))
                                        messageText = AppResources.EmailConv_SharedVideo_Txt;
                                    else if (contentType.Contains(HikeConstants.CT_CONTACT))
                                        messageText = AppResources.EmailConv_SharedContact_Txt;
                                    else if (contentType.Contains(HikeConstants.LOCATION))
                                        messageText = AppResources.EmailConv_SharedLocation_Txt;
                                    else
                                        messageText = AppResources.EmailConv_SharedFile_Txt;
                                }
                                else
                                {
                                    if (convMsg.MetaDataString != null && convMsg.MetaDataString.Contains(HikeConstants.LONG_MESSAGE))
                                    {
                                        string message = MessagesTableUtils.ReadLongMessageFile(convMsg.Timestamp, convMsg.Msisdn);
                                        messageText = message;
                                    }
                                    else
                                        messageText = convMsg.Message;

                                    if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                                        messageText = "\"" + messageText + "\"";
                                    else if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                                        messageText = AppResources.EmailConv_PostedPin_Txt + "-\"" + messageText + "\"";
                                }

                                if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE || convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                                {
                                    if (convMsg.IsSent)
                                        messageSender = AppResources.You_Txt;
                                    else if (isGroupChat)
                                    {
                                        if (convMsg.GroupParticipant == App.MSISDN)
                                            messageSender = AppResources.You_Txt;
                                        else if (!groupChatParticipantInfo.TryGetValue(convMsg.GroupParticipant, out messageSender))
                                            messageSender = convMsg.GroupParticipant;
                                    }
                                    else
                                        messageSender = displayName;

                                    currentMessage = string.Format(HikeConstants.EmailConversation.CONV_MSG_DISP_FMT, messageTime, messageSender, messageText);
                                }
                                else
                                    currentMessage = string.Format(HikeConstants.EmailConversation.SYS_MSG_DISP_FMT, messageTime, messageText);

                                int messageSize = currentMessage.Length * sizeOfChar; //sizeOfChar is 2

                                if (messageSize + bytesConsumed <= HikeConstants.EmailConversation.EMAIL_LIMIT)
                                {
                                    msgcount++;
                                    messagesStack.Push(currentMessage);
                                    bytesConsumed += messageSize;
                                }
                                else
                                {
                                    isShowTruncatedText = true;
                                    break;
                                }
                            }

                            if (!isShowTruncatedText)
                            {
                                isFirstTime = false;
                                lastMessageId = convList[convList.Count - 1].MessageId-1;
                                hasMoreMsg = true;
                            }
                        }
                    }while(hasMoreMsg);

                    string header;

                    if (msgcount == 0)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(AppResources.EmailConvError_Txt, AppResources.Alert_Txt, MessageBoxButton.OK);
                        });
                    }
                    else
                    {
                        if (isShowTruncatedText)
                            header = string.Format(AppResources.EmailConv_Header_Truncation_Txt, displayName, msgcount);
                        else if (msgcount == 1)
                            header = string.Format(AppResources.EmailConv_Header_One_Message_Txt, displayName);
                        else
                            header = string.Format(AppResources.EmailConv_Header_Txt, displayName, msgcount);

                        header += "\r\n";
                        messagesStack.Push(header);
                        SendEmail(subject, GetEmailString(messagesStack));
                    }
                });
        }
    }
}