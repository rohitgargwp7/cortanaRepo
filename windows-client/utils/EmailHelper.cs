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
        private static string _displayNameFormat = "- {0}: ";

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

            Dictionary<string, string> GroupChatParticipantInfo = null;
            int bytes_consumed = 0;
            List<ConvMessage> convList = MessagesTableUtils.getMessagesForMsisdn(msisdn, long.MaxValue, 500);
            StringBuilder emailText = new StringBuilder();
            String contactName = "";
            bool isGroupConversation = Utils.isGroupConversation(msisdn);
            bool isShowTruncatedText = false;
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

            string subject = String.Format(AppResources.EmailConv_Subject_Txt, contactName);
            string nameToShow = String.Format(_displayNameFormat, contactName);

            if (isGroupConversation)
            {
                GroupChatParticipantInfo = new Dictionary<string, string>();
                List<GroupParticipant> grpParticipantList = GroupManager.Instance.GetParticipantList(msisdn);

                foreach (GroupParticipant grpParticipant in grpParticipantList)
                {
                    string cname = "";

                    cinfo = UsersTableUtils.getContactInfoFromMSISDN(grpParticipant.Msisdn);

                    if (cinfo == null)
                        cname = grpParticipant.Msisdn;
                    else
                        cname = cinfo.NameToshow;

                    nameToShow = String.Format(_displayNameFormat, cname);
                    GroupChatParticipantInfo.Add(grpParticipant.Msisdn, cname);
                }
            }

            if (convList != null)
            {
                int msgcount = 0;

                foreach (ConvMessage convMsg in convList)
                {
                    StringBuilder currentMessage = new StringBuilder();

                    currentMessage.Append(TimeUtils.getTimeStringForEmailConversation(convMsg.Timestamp));

                    if (convMsg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                    {
                        if (convMsg.IsSent || convMsg.Msisdn == App.MSISDN)
                            currentMessage.Append(String.Format(_displayNameFormat, AppResources.You_Txt));
                        else
                        {
                            if (isGroupConversation)
                            {
                                if (convMsg.GroupParticipant == App.MSISDN)
                                    nameToShow = AppResources.You_Txt;
                                else if (!GroupChatParticipantInfo.TryGetValue(convMsg.GroupParticipant, out nameToShow))
                                    nameToShow = convMsg.GroupParticipant;

                                nameToShow = String.Format(_displayNameFormat, nameToShow);
                            }

                            currentMessage.Append(nameToShow);
                        }
                    }
                    else
                        currentMessage.Append("- ");

                    if (convMsg.HasAttachment)
                    {
                        if (convMsg.Message.Equals(AppResources.Image_Txt))
                            currentMessage.Append(AppResources.EmailConv_SharedImage_Txt);
                        else if (convMsg.Message.Equals(AppResources.Audio_Txt))
                            currentMessage.Append(AppResources.EmailConv_SharedAudio_Txt);
                        else if (convMsg.Message.Equals(AppResources.Video_Txt))
                            currentMessage.Append(AppResources.EmailConv_SharedVideo_Txt);
                        else if (convMsg.Message.Equals(AppResources.ContactTransfer_Text))
                            currentMessage.Append(AppResources.EmailConv_SharedContact_Txt);
                        else if (convMsg.Message.Equals(AppResources.Location_Txt))
                            currentMessage.Append(AppResources.EmailConv_SharedLocation_Txt);
                        else
                            currentMessage.Append(AppResources.EmailConv_SharedFile_Txt);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(convMsg.MetaDataString) && convMsg.MetaDataString.Contains(HikeConstants.STICKER_ID))
                            currentMessage.Append(convMsg.Message);
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
                        msgcount++;
                        messagesStack.Push(currentMessage.ToString());
                        bytes_consumed += currentMessage.Length;
                    }
                    else
                    {
                        isShowTruncatedText = true;
                        break;
                    }
                }

                string header = String.Format(AppResources.EmailConv_Header_Txt, contactName, msgcount);

                if (isShowTruncatedText)
                    header += AppResources.EmailConv_Header_Truncation_Txt;

                header += "\r\n";
                messagesStack.Push(header);
                SendEmail(subject, GetEmailString(messagesStack), "", "", "");
            }
        }
    }
}
