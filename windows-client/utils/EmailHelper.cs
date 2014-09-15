using Microsoft.Phone.Tasks;
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
        /// object for locking while sending email
        /// </summary>
        private static object _sendEmailLock = new object();

        /// <summary>
        /// function to send email
        /// </summary>
        /// <param name="subject">subject of email</param>
        /// <param name="body">body of email</param>
        /// <param name="recipient">recipient</param>
        /// <param name="cc">carbon copy</param>
        /// <param name="bcc">blind carbon copy</param>
        public static void SendEmail(string subject="", string body="", string recipient="", string cc="", string bcc="")
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
                Debug.WriteLine("EmailHelper::SendMail:"+ex.StackTrace);
            }
        }

        /// <summary>
        /// reads stack of message and builds a string for email body
        /// </summary>
        /// <param name="messagesStack">stack of message</param>
        /// <returns>returns string</returns>
        private static string GetEmailString(Stack<String> messagesStack)
        {
            StringBuilder sb = new StringBuilder();

            while (messagesStack.Count > 0)
                sb.AppendLine(messagesStack.Pop());

            return sb.ToString();
        }

        /// <summary>
        /// fetch mail from database and sends mail
        /// </summary>
        /// <param name="msisdn">msisdn of user or group</param>
        /// <param name="contactName">username or group name</param>
        /// <param name="isGroupChat">group chat or not</param>
        /// <returns>void</returns>
        public static async Task FetchAndEmail(string msisdn, string contactName, bool isGroupChat)
        {
            await Task.Run(() =>
                {
                    int bytes_consumed = 0;
                    List<ConvMessage> convList = MessagesTableUtils.getMessagesForMsisdn(msisdn, long.MaxValue, HikeConstants.EmailConversation.CHAT_FETCH_LIMIT);

                    if (convList != null)
                    {
                        Dictionary<string, string> groupChatParticipantInfo = null;
                        StringBuilder emailText = new StringBuilder();
                        bool isShowTruncatedText = false;
                        Stack<string> messagesStack = new Stack<string>();

                        string subject = string.Format(AppResources.EmailConv_Subject_Txt, contactName);

                        if (isGroupChat)
                        {
                            ContactInfo cinfo;
                            groupChatParticipantInfo = new Dictionary<string, string>();
                            List<GroupParticipant> grpParticipantList = GroupManager.Instance.GetParticipantList(msisdn);

                            foreach (GroupParticipant grpParticipant in grpParticipantList)
                            {
                                string cname = string.Empty;

                                cinfo = UsersTableUtils.getContactInfoFromMSISDN(grpParticipant.Msisdn);

                                if (cinfo == null)
                                    cname = grpParticipant.Msisdn;
                                else
                                    cname = cinfo.NameToshow;

                                groupChatParticipantInfo.Add(grpParticipant.Msisdn, cname);
                            }
                        }

                        int msgcount = 0;

                        foreach (ConvMessage convMsg in convList)
                        {
                            string currentMessage = string.Empty;

                            string messageTime = string.Empty;
                            string messageSender = string.Empty;
                            string messageText = string.Empty;

                            messageTime = TimeUtils.getTimeStringForEmailConversation(convMsg.Timestamp);

                            if (convMsg.HasAttachment)
                            {
                                if (convMsg.Message.Equals(AppResources.Image_Txt))
                                    messageText = AppResources.EmailConv_SharedImage_Txt;
                                else if (convMsg.Message.Equals(AppResources.Audio_Txt))
                                    messageText = AppResources.EmailConv_SharedAudio_Txt;
                                else if (convMsg.Message.Equals(AppResources.Video_Txt))
                                    messageText = AppResources.EmailConv_SharedVideo_Txt;
                                else if (convMsg.Message.Equals(AppResources.ContactTransfer_Text))
                                    messageText = AppResources.EmailConv_SharedContact_Txt;
                                else if (convMsg.Message.Equals(AppResources.Location_Txt))
                                    messageText = AppResources.EmailConv_SharedLocation_Txt;
                                else
                                    messageText = AppResources.EmailConv_SharedFile_Txt;
                            }
                            else
                            {
                                if (convMsg.MetaDataString != null && convMsg.MetaDataString.Contains("lm"))
                                {
                                    string message = MessagesTableUtils.ReadLongMessageFile(convMsg.Timestamp, convMsg.Msisdn);
                                    messageText = message;
                                }
                                else
                                    messageText = convMsg.Message;
                            }

                            if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                            {
                                if (convMsg.IsSent || convMsg.Msisdn == App.MSISDN)
                                {
                                    messageSender = AppResources.You_Txt;
                                }
                                else if (isGroupChat)
                                {
                                    if (convMsg.GroupParticipant == App.MSISDN)
                                        messageSender = AppResources.You_Txt;
                                    else if (!groupChatParticipantInfo.TryGetValue(convMsg.GroupParticipant, out messageSender))
                                        messageSender = convMsg.GroupParticipant;
                                }
                                else
                                    messageSender = contactName;

                                currentMessage = string.Format(HikeConstants.EmailConversation.CONV_MSG_DISP_FMT, messageTime, messageSender, messageText);
                            }
                            else
                                currentMessage = string.Format(HikeConstants.EmailConversation.SYS_MSG_DISP_FMT, messageTime, messageText);

                            if (currentMessage.Length + bytes_consumed <= HikeConstants.EmailConversation.EMAIL_LIMIT)
                            {
                                msgcount++;
                                messagesStack.Push(currentMessage);
                                bytes_consumed += currentMessage.Length;
                            }
                            else
                            {
                                isShowTruncatedText = true;
                                break;
                            }
                        }

                        string header;

                        if (isShowTruncatedText)
                            header = string.Format(AppResources.EmailConv_Header_Truncation_Txt, contactName, msgcount);
                        else
                            header = string.Format(AppResources.EmailConv_Header_Txt, contactName, msgcount);

                        header += "\r\n";
                        messagesStack.Push(header);
                        SendEmail(subject, GetEmailString(messagesStack));
                    }
                });
        }
    }
}
