using System;
using System.Collections.Generic;
using windows_client.Model;
using windows_client.DbUtils;

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
            List<ConversationListObject> convs = ConversationTableUtils.getAllConversations();

            //addMessage("Hey 1", "+919876543211");
            //addMessage("Hey 2", "+919876543211");
            //addMessage("Hey 3", "+919876543211");
            //addMessage("Hey 4", "+919876543211");
            //addMessage("Hey 5", "+919876543211");

            //clearMessages();
            //msgs = MessagesTableUtils.getAllMessages(); ;
            //convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Vijay 1", "+919910000474");
            addMessage("Hey Vijay 2", "+919910000474");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Robby 1", "+919999711370");
            addMessage("Hey Robby 2", "+919999711370");
            addMessage("Hey Robby 3", "+919999711370");
            addMessage("Hey Robby 4", "+919999711370");
            addMessage("Hey Robby 5", "+919999711370");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey Rishabh 1", "+919582021646");
            addMessage("Hey Rishabh 2", "+919582021646");
            addMessage("Hey Rishabh 3", "+919582021646");
            addMessage("Hey Rishabh 4", "+919582021646");
            addMessage("Hey Rishabh 5", "+919582021646");

            clearMessages();
            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

            addMessage("Hey GK2 1", "+918826670738");
            addMessage("Hey GK2 2", "+918826670738");
            addMessage("Hey GK2 3", "+918826670738");
            addMessage("Hey GK2 4", "+918826670738");
            addMessage("Hey GK2 5", "+918826670738");

            clearMessages();
            addMessage("Hey Madhur", "+919873480092");

            msgs = MessagesTableUtils.getAllMessages(); ;
            convs = ConversationTableUtils.getAllConversations();

        }

        private static List<ConvMessage> messages = new List<ConvMessage>();
        public static void AddContactEntries()
        {
            List<ContactInfo> list = new List<ContactInfo>();
            ContactInfo obj = new ContactInfo("-1", "+919876543210", "Gautam", false, "9876543210", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            obj = new ContactInfo("-1", "+919910000474", "Vijay", false, "9910000474", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            obj = new ContactInfo("-1", "+919582021646", "Rishabh", false, "9582021646", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            obj = new ContactInfo("-1", "+919999711370", "Robby", true, "9999711370", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            obj = new ContactInfo("-1", "+919873480092", "Madhur", true, "9873480092", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            obj = new ContactInfo("-1", "+918826670738", "GK", true, "8826670738", false);
            obj.Id = Convert.ToString(Math.Abs(obj.GetHashCode()));
            list.Add(obj);
            list.Add(new ContactInfo("-1", "+918826670738", "GK", true, "8826670738", false));
            UsersTableUtils.addContacts(list);
        }

        public static void addMessage(String message, String msisdn)
        {
            //ConvMessage convMessage = new ConvMessage(message, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            //ConversationListObject m = new ConversationListObject(msisdn, "", null, TimeUtils.getRelativeTime(TimeUtils.getCurrentTimeStamp()));
            //convMessage.Conversation = m;
            //if (messages.Count == 0)
            //{
            //    ConversationTableUtils.addConversation(convMessage);
            //    //TO discusse
            //    // add message to list of existing messages and write to db when user quits this page
            //}           
            //MessagesTableUtils.addMessage(convMessage);
            //messages.Add(convMessage);
        }

        public static void clearMessages()
        {
            messages.Clear();
        }
    }
}
