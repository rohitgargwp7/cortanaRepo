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
using System.ComponentModel;
using windows_client.Model;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using windows_client.utils;

namespace windows_client.DbUtils
{
    public class HikeDbUtils : INotifyPropertyChanged
    {
        private HikeDataContext hikeDataContext;

        public HikeDataContext HikeDataContext
        {
            get
            {
                return hikeDataContext;
            }
        }

        private string hikeConnectionString;

        #region properties

        private ObservableCollection<MessageListPage> _messageListPageCollection;

        public ObservableCollection<MessageListPage> MessageListPageCollection
        {
            get { return _messageListPageCollection; }
            set
            {
                _messageListPageCollection = value;
                NotifyPropertyChanged("MessageListPageCollection");
            }
        }

        
        private ObservableCollection<ContactInfo> _contacts;

        public ObservableCollection<ContactInfo> Contacts
        {
            get { return _contacts; }
            set
            {
                _contacts = value;
                NotifyPropertyChanged("Contacts");
            }
        }

        private ObservableCollection<Blocked> _blockedUsers;

        public ObservableCollection<Blocked> BlockedUsers
        {
            get { return _blockedUsers; }
            set
            {
                _blockedUsers = value;
                NotifyPropertyChanged("BlockedUsers");
            }
        }

        private ObservableCollection<Conversation> _conversation;

        public ObservableCollection<Conversation> Conversation
        {
            get { return _conversation; }
            set
            {
                _conversation = value;
                NotifyPropertyChanged("Conversation");
            }
        }

        private ObservableCollection<Thumbnails> _thumbnails;
        public ObservableCollection<Thumbnails> Thumbnails
        {
            get { return _thumbnails; }
            set
            {
                _thumbnails = value;
                NotifyPropertyChanged("Thumbnails");
            }
        }



        private ObservableCollection<ConvMessage> _messages;
        public ObservableCollection<ConvMessage> Messages
        {
            get { return _messages; }
            set
            {
                _messages = value;
                NotifyPropertyChanged("Messages");
            }
        }
        #endregion

        public HikeDbUtils(string hikeConnectionString)
        {
            this.hikeConnectionString = hikeConnectionString;
            hikeDataContext = new HikeDataContext(hikeConnectionString);
        }

        public void SaveChangesToDB()
        {
            hikeDataContext.SubmitChanges();
        }

        #region user table

        public static void addContact(ContactInfo user)
        {
            App.ViewModel.HikeDataContext.users.InsertOnSubmit(user);
            App.ViewModel.HikeDataContext.SubmitChanges();
            //TODO update observable list
        }

        public static void addContacts(List<ContactInfo> contacts)
        {
            App.ViewModel.HikeDataContext.users.InsertAllOnSubmit(contacts);
            App.ViewModel.HikeDataContext.SubmitChanges();

        }

        public static List<ContactInfo> getAllContacts()
        {
            Func<HikeDataContext, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
            ((HikeDataContext hdc) =>
                from o in hdc.users
                select o);
            return q(App.ViewModel.HikeDataContext).Count<ContactInfo>() == 0 ? null:
                q(App.ViewModel.HikeDataContext).ToList<ContactInfo>();
        }

        public static List<ConvMessage> getAllMessages()
        {
            Func<HikeDataContext, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ConvMessage>>
            ((HikeDataContext hdc) =>
                from o in hdc.messages
                select o);
            return q(App.ViewModel.HikeDataContext).Count<ConvMessage>() == 0? null:
                q(App.ViewModel.HikeDataContext).ToList<ConvMessage>();
        }


        public static ContactInfo getContactInfoFromMSISDN(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.users
                where o.Msisdn == m
                select o);

            return q(App.ViewModel.HikeDataContext, msisdn).First();
            //IEnumerable<ContactInfo> contact = from ContactInfo c in hikeDataContext.users
            //              where c.Msisdn == msisdn
            //              select c;
            //return contact.First();
        }

        public static void deleteAllContacts()
        {
            App.ViewModel.HikeDataContext.users.DeleteAllOnSubmit<ContactInfo>(App.ViewModel.HikeDataContext.GetTable<ContactInfo>());
            App.ViewModel.HikeDataContext.SubmitChanges();

        }
        #endregion


        #region convmessage functions
        public bool wasMessageReceived(ConvMessage conv)
        {
            Func<HikeDataContext, long, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long timestamp, string m) =>
                from cm in hdc.messages
//                where cm.Msisdn == m && cm.ConversationId == conversationId && cm.Timestamp == timestamp
                where cm.Msisdn == m && cm.Timestamp == timestamp
                select cm);
            return q(hikeDataContext, conv.Timestamp, conv.Message).Count() != 0;
        }

        public void deleteMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.messages
                where o.MessageId == id
                select o);
            App.ViewModel.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(q(hikeDataContext, msgId));
            App.ViewModel.HikeDataContext.SubmitChanges();
        }

        public static void addMessages(List<ConvMessage> messages)
        {
            App.ViewModel.HikeDataContext.messages.InsertAllOnSubmit(messages);
            App.ViewModel.HikeDataContext.SubmitChanges();
        }

        public static void deleteAllConversations()
        {
            App.ViewModel.HikeDataContext.conversations.DeleteAllOnSubmit<Conversation>(App.ViewModel.HikeDataContext.GetTable<Conversation>());
            App.ViewModel.HikeDataContext.SubmitChanges();

        }


        public static void deleteAllMessages()
        {
            App.ViewModel.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(App.ViewModel.HikeDataContext.GetTable<ConvMessage>());
            App.ViewModel.HikeDataContext.SubmitChanges();

        }


        //public static long getConvIdForMsisdn(string msisdn)
        //{
        //    Func<HikeDataContext, string, IQueryable<long>> q =
        //    CompiledQuery.Compile<HikeDataContext, string, IQueryable<long>>
        //    ((HikeDataContext hdc, string myId) =>
        //        from o in hdc.conversations
        //        where o.Msisdn == myId
        //        select o.ConvId);
        //    return q(App.ViewModel.HikeDataContext, msisdn).Count<long>() == 0? -1 :
        //            q(App.ViewModel.HikeDataContext, msisdn).First<long>();
        //}



        public static List<ConvMessage> getMessagesForMsisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string myMsisdn) =>
                from o in hdc.messages
                where o.Msisdn == myMsisdn
                select o);
            return q(App.ViewModel.HikeDataContext, msisdn).Count<ConvMessage>() == 0 ? null :
                q(App.ViewModel.HikeDataContext, msisdn).ToList<ConvMessage>();
        }

        public static ConvMessage getLastMessageForMsisdn(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string myMsisdn) =>
                from o in hdc.messages
                where o.Msisdn == myMsisdn
                select o);
            return q(App.ViewModel.HikeDataContext, msisdn).Count<ConvMessage>() == 0 ? null :
                q(App.ViewModel.HikeDataContext, msisdn).Last<ConvMessage>();
        }

        public static void deleteConversation(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ConvMessage>> messages =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, string phoneNum) =>
                from o in hdc.messages
                where o.Msisdn == phoneNum
                select o);

            Func<HikeDataContext, string, IQueryable<Conversation>> conversation =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<Conversation>>
            ((HikeDataContext hdc, string phoneNum) =>
                from o in hdc.conversations
                where o.Msisdn == phoneNum
                select o);

            App.ViewModel.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(messages(App.ViewModel.HikeDataContext, msisdn));
            App.ViewModel.HikeDataContext.conversations.DeleteAllOnSubmit<Conversation>(conversation(App.ViewModel.HikeDataContext, msisdn));
            App.ViewModel.HikeDataContext.SubmitChanges();
        }

//        public static Conversation addConversation(string msisdn, bool onhike)
        public static void addConversation(string msisdn, bool onhike)
        {
            ContactInfo contactInfo = getContactInfoFromMSISDN(msisdn);

            if (contactInfo != null)
                onhike |= contactInfo.OnHike;
            Conversation conv = new Conversation(msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : null, onhike);
            App.ViewModel.HikeDataContext.conversations.InsertOnSubmit(conv);
            App.ViewModel.HikeDataContext.SubmitChanges();
        }

        public static void addMessage(ConvMessage convMessage)
        {
            App.ViewModel.HikeDataContext.messages.InsertOnSubmit(convMessage);
            App.ViewModel.HikeDataContext.SubmitChanges();
        }

        public static List<Conversation> getConversations()
        {
            Func<HikeDataContext, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
            ((HikeDataContext hdc) =>
                from o in hdc.conversations
                select o);
            if (q(App.ViewModel.HikeDataContext).Count<Conversation>() == 0)
                return null;
            List<Conversation> conversations = q(App.ViewModel.HikeDataContext).ToList<Conversation>();
            conversations.Sort();
            conversations.Reverse();
            return conversations;
        }
        #endregion

        #region blocked table
        public void addBlockList(List<string> msisdns)
        {
            if (msisdns == null)
            {
                return;
            }

            foreach (string m in msisdns)
            {
                hikeDataContext.blockedUsersTable.InsertOnSubmit(new Blocked(m));
            }
            try
            {
                hikeDataContext.SubmitChanges();
            }
            catch (DuplicateKeyException dke)
            {
                dke.ToString();
            }
        }


        public List<Blocked> getBlockList()
        {
            Func<HikeDataContext, IQueryable<Blocked>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Blocked>>
            ((HikeDataContext hdc) =>
                from o in hdc.blockedUsersTable
                select o);
            return q(hikeDataContext).ToList<Blocked>();
        }

        public void deleteBlocklist()
        {
            hikeDataContext.blockedUsersTable.DeleteAllOnSubmit<Blocked>(hikeDataContext.GetTable<Blocked>());
            hikeDataContext.SubmitChanges();
        }

        #endregion

        public void LoadMessagePageList()
        {
            List<Conversation> conversations = getConversations();

            MessageListPageCollection = new ObservableCollection<MessageListPage>();
            foreach (Conversation c in conversations)
            {
                ConvMessage message = c.Messages[c.Messages.Count - 1]; 
                string contactName = c.ContactName;
                string lastMessage = message.Message;
                string relativeTime = TimeUtils.getRelativeTime(message.Timestamp);
                MessageListPageCollection.Add(new MessageListPage(contactName, lastMessage, relativeTime));
            }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
