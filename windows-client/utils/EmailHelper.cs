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
    class EmailHelper
    {
        private static object _sendEmailLock = new object();
        private static int _emailLimit = 60 * 1024; //60KB
        private static Dictionary<string, string> _groupChatParticipantInfo = null;

        public static void SendEmail(string subject, string body, string recipient, string cc, string bcc)
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
                MessageBox.Show(ex.ToString());
            }
        }

        private static string ReverseStringLineByLine(string str)
        {
            StringBuilder sb = new StringBuilder();
            int current = str.Length - 1;

            while (current > -1)
            {
                int cnt = 0;

                while (current > -1 && str[current] != '\n')
                {
                    cnt++;
                    current--;
                }

                for (int i = 1; i <= cnt; i++)
                    sb.Append(str[current + i]);

                if (current != -1)
                    sb.Append(str[current]);

                current--;
            }

            return sb.ToString();
        }

        private static string GetEmailString(Stack<String> messagesStack)
        {
            StringBuilder sb = new StringBuilder();

            while (messagesStack.Count > 0)
                sb.AppendLine(messagesStack.Pop());

            return sb.ToString();
        }

        public static void FetchAndEmail(string msisdn)
        {
            int bytes_consumed = 0;
            List<ConvMessage> convList = MessagesTableUtils.getMessagesForMsisdn(msisdn, long.MaxValue, 400);
            StringBuilder emailText = new StringBuilder();
            String contactName = "";
            bool isGroupConversation = Utils.isGroupConversation(msisdn);
            ContactInfo cinfo;
            Stack<string> messagesStack = new Stack<string>();

            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                ConversationListObject convObj = App.ViewModel.ConvMap[msisdn];
                contactName = (convObj != null && convObj.ContactName != null) ? convObj.ContactName : msisdn;
            }
            else
            {
                cinfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                contactName = cinfo == null ? msisdn : cinfo.NameToshow;
            }

            string subject = "Backup of hike chat with " + contactName;
            string header = "--------------------------------------------------------------------------------------------------------\r\nChat with " + contactName + " - last ";
            string header1 = "Chat with " + contactName + " - last ";
            contactName = " " + contactName + ": ";

            if (isGroupConversation)
            {
                _groupChatParticipantInfo = new Dictionary<string, string>();
                List<GroupParticipant> grpParticipantList = GroupManager.Instance.GetParticipantList(msisdn);

                foreach (GroupParticipant grpParticipant in grpParticipantList)
                {
                    string cname = "";

                    cinfo = UsersTableUtils.getContactInfoFromMSISDN(grpParticipant.Msisdn);

                    if (cinfo == null)
                        cname = grpParticipant.Name != grpParticipant.Msisdn ? grpParticipant.Name + "(" + grpParticipant.Msisdn + ")" : grpParticipant.Msisdn;
                    else
                        cname = cinfo.NameToshow;

                    cname = " " + cname + ": ";
                    _groupChatParticipantInfo.Add(grpParticipant.Msisdn, cname);
                }
            }

            if (convList != null)
            {
                int msgcount = convList.Count;
                header += (msgcount + " messages");
                header1 += (msgcount + " messages\r\n");
                header1 += "--------------------------------------------------------------------------------------------------------";
                bytes_consumed += header.Length;

                foreach (ConvMessage convMsg in convList)
                {
                    StringBuilder currentMessage = new StringBuilder();

                    currentMessage.Append(TimeUtils.getTimeStringForEmailConversation(convMsg.Timestamp));

                    if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                    {
                        if (convMsg.IsSent || convMsg.Msisdn == App.MSISDN)
                            currentMessage.Append(" You: ");
                        else
                        {
                            if (isGroupConversation)
                            {
                                if (convMsg.GroupParticipant == App.MSISDN)
                                    contactName = " You: ";
                                else if (!_groupChatParticipantInfo.TryGetValue(convMsg.GroupParticipant, out contactName))
                                    contactName = " " + convMsg.GroupParticipant + ": ";
                            }

                            currentMessage.Append(contactName);
                        }
                    }
                    else
                        currentMessage.Append(": ");

                    if (convMsg.HasAttachment)
                    {
                        if (convMsg.Message.Equals(AppResources.Image_Txt))
                            currentMessage.Append("shared image");
                        else if (convMsg.Message.Equals(AppResources.Audio_Txt))
                            currentMessage.Append("shared audio");
                        else if (convMsg.Message.Equals(AppResources.Video_Txt))
                            currentMessage.Append("shared video");
                        else if (convMsg.Message.Equals(AppResources.ContactTransfer_Text))
                            currentMessage.Append("shared contact");
                        else if (convMsg.Message.Equals(AppResources.Location_Txt))
                            currentMessage.Append("shared location");
                        else
                            currentMessage.Append("shared file");
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(convMsg.MetaDataString) && convMsg.MetaDataString.Contains(HikeConstants.STICKER_ID))
                        {
                            currentMessage.Append("Sent a " + convMsg.Message);
                        }
                        else if (convMsg.MetaDataString != null && convMsg.MetaDataString.Contains("lm"))
                        {
                            string message = MessagesTableUtils.ReadLongMessageFile(convMsg.Timestamp, convMsg.Msisdn);
                            currentMessage.Append(message);
                        }
                        else
                            currentMessage.Append(convMsg.Message);
                    }

                    if (currentMessage.Length + bytes_consumed <= _emailLimit)
                    {
                        //emailText.Insert(0, currentMessage);
                        emailText.AppendLine(currentMessage.ToString());
                        messagesStack.Push(currentMessage.ToString());
                        bytes_consumed += currentMessage.Length;
                    }
                    else
                        break;
                }

                //emailText.Insert(0, header);
                emailText.Append(header);
                messagesStack.Push(header);
                _groupChatParticipantInfo = null;
                //SendEmail(subject, ReverseStringLineByLine(emailText.ToString()), "", "", "");
                SendEmail(subject, GetEmailString(messagesStack), "", "", "");
            }
        }
    }
}
