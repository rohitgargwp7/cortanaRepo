using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;

namespace windows_client.Test
{
    public class Test
    {
        private static List<ConvMessage> messages = new List<ConvMessage>();
        public static void AddContactEntries()
        {
            List<ContactInfo> list = new List<ContactInfo>();
            list.Add(new ContactInfo("-1", "+919876543210", "Gautam", false, "9876543210", false));
            list.Add(new ContactInfo("-1", "+919876543211", "Madhur", false, "9876543211", false));
            list.Add(new ContactInfo("-1", "+919876543212", "Vijay", false, "9876543212", false));
            HikeDbUtils.addContacts(list);
            List<ContactInfo> c2 = HikeDbUtils.getAllContacts();
        }

        public static void addMessage(String message, String msisdn)
        {
            if (messages.Count == 0)
            {
                HikeDbUtils.addConversation(msisdn, false);
                //TO discusse
                // add message to list of existing messages and write to db when user quits this page
            }

            ConvMessage convMessage = new ConvMessage(message, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);

            HikeDbUtils.addMessage(convMessage);
            messages.Add(convMessage);
        }

        public static void clearMessages()
        {
            messages.Clear();
        }

    }
}
