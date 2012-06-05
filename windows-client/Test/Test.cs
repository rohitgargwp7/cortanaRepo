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
      
        public static void insertMessages()
        {

            addMessage("Hello1", "+919876543210");
            addMessage("Hello2", "+919876543210");
            addMessage("Hello3", "+919876543210");
            addMessage("Hello4", "+919876543210");
            addMessage("Hello5", "+919876543210");

            clearMessages();
            List<ConvMessage> msgs = MessagesTableUtils.getAllMessages();
            List<Conversation> convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey1", "+919876543211");
            addMessage("Hey2", "+919876543211");
            addMessage("Hey3", "+919876543211");
            addMessage("Hey4", "+919876543211");
            addMessage("Hey5", "+919876543211");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey11", "+919876543212");
            addMessage("Hey12", "+919876543212");
            addMessage("Hey13", "+919876543212");
            addMessage("Hey14", "+919876543212");
            addMessage("Hey15", "+919876543212");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

        }

        private static List<ConvMessage> messages = new List<ConvMessage>();
        public static void AddContactEntries()
        {
            List<ContactInfo> list = new List<ContactInfo>();
            list.Add(new ContactInfo("-1", "+919876543210", "Gautam", false, "9876543210", false));
            list.Add(new ContactInfo("-1", "+919876543211", "Madhur", false, "9876543211", false));
            list.Add(new ContactInfo("-1", "+919876543212", "Vijay", false, "9876543212", false));
            list.Add(new ContactInfo("-1", "+919876543213", "Rishabh", false, "919876543213", false));
            UsersTableUtils.addContacts(list);
            List<ContactInfo> c2 = UsersTableUtils.getAllContacts();
        }

        public static void addMessage(String message, String msisdn)
        {
            if (messages.Count == 0)
            {
                UsersTableUtils.addConversation(msisdn, false);
                //TO discusse
                // add message to list of existing messages and write to db when user quits this page
            }

            ConvMessage convMessage = new ConvMessage(message, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);

            UsersTableUtils.addMessage(convMessage);
            messages.Add(convMessage);
        }

        public static void clearMessages()
        {
            messages.Clear();
        }

    }
}
