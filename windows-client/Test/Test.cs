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

            addMessage("Hey Gautam 1", "+919876543210");
            addMessage("Hey Gautam 2", "+919876543210");
            addMessage("Hey Gautam 3", "+919876543210");
            addMessage("Hey Gautam 4", "+919876543210");
            addMessage("Hey Gautam 5", "+919876543210");

            clearMessages();
            List<ConvMessage> msgs = MessagesTableUtils.getAllMessages();
            List<Conversation> convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Madhur 1", "+919876543211");
            addMessage("Hey Madhur 2", "+919876543211");
            addMessage("Hey Madhur 3", "+919876543211");
            addMessage("Hey Madhur 4", "+919876543211");
            addMessage("Hey Madhur 5", "+919876543211");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Vijay 1", "+919876543212");
            addMessage("Hey Vijay 2", "+919876543212");
            addMessage("Hey Vijay 3", "+919876543212");
            addMessage("Hey Vijay 4", "+919876543212");
            addMessage("Hey Vijay 5", "+919876543212");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Robby 1", "+919999711370");
            addMessage("Hey Robby 2", "+919999711370");
            addMessage("Hey Robby 3", "+919999711370");
            addMessage("Hey Robby 4", "+919999711370");
            addMessage("Hey Robby 5", "+919999711370");
            addMessage("Hey Robby 6", "+919999711370");
            addMessage("Hey Robby 7", "+919999711370");
            addMessage("Hey Robby 8", "+919999711370");
            addMessage("Hey Robby 9", "+919999711370");
            addMessage("Hey Robby 10", "+919999711370");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Rishabh 1", "+919876543213");
            addMessage("Hey Rishabh 2", "+919876543213");
            addMessage("Hey Rishabh 3", "+919876543213");
            addMessage("Hey Rishabh 4", "+919876543213");
            addMessage("Hey Rishabh 5", "+919876543213");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey GK2 1", "+919818082868");
            addMessage("Hey GK2 2", "+919818082868");
            addMessage("Hey GK2 3", "+919818082868");
            addMessage("Hey GK2 4", "+919818082868");
            addMessage("Hey GK2 5", "+919818082868");

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
            list.Add(new ContactInfo("-1", "+919876543213", "Rishabh", false, "9876543213", false));
            list.Add(new ContactInfo("-1", "+919999711370", "Robby", false, "9999711370", false));
            list.Add(new ContactInfo("-1", "+919818082868", "GK2", false, "9818082868", false));
            UsersTableUtils.addContacts(list);
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
