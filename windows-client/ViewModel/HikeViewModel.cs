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

using windows_client.Model;
using System.Collections.ObjectModel;
using windows_client.utils;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged
    {
        private HikeDataContext hikeDataContext;

        private String hikeConnectionString;

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

        public HikeViewModel(String hikeConnectionString)
        {
            this.hikeConnectionString = hikeConnectionString;
            hikeDataContext = new HikeDataContext(hikeConnectionString);
        }

        public void SaveChangesToDB()
        {
            hikeDataContext.SubmitChanges();
        }

        #region user table
        public void addContact(ContactInfo user)
        {
            hikeDataContext.users.InsertOnSubmit(user);
            hikeDataContext.SubmitChanges();
            //TODO update observable list
        }

        public void addContacts(List<ContactInfo> contacts)
        {
            hikeDataContext.users.InsertAllOnSubmit(contacts);
            hikeDataContext.SubmitChanges();

        }

        public List<ContactInfo> getAllContacts()
        {
            Func<HikeDataContext, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
            ((HikeDataContext hdc) =>
                from o in hdc.users
                select o);
            return q(hikeDataContext).ToList<ContactInfo>();
        }

        public ContactInfo getContactInfoFromMSISDN(String msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.users
                where o.Msisdn == m
                select o);

            //foreach (ContactInfo c in q(hikeDataContext, msisdn))
            //{ 

            //}
            return q(hikeDataContext, msisdn).First();
            //IEnumerable<ContactInfo> contact = from ContactInfo c in hikeDataContext.users
            //              where c.Msisdn == msisdn
            //              select c;
            //return contact.First();
        }

        public void deleteAllContacts()
        {
            hikeDataContext.users.DeleteAllOnSubmit<ContactInfo>(hikeDataContext.GetTable<ContactInfo>());
            hikeDataContext.SubmitChanges();

        }
        #endregion


        #region convmessage functions
        public bool wasMessageReceived(ConvMessage conv)
        {
            // conv.t
            Func<HikeDataContext, long, long, string, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, long, string, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long conversationId, long timestamp, string m) =>
                from cm in hdc.messages
                where cm.Msisdn == m && cm.ConversationId == conversationId && cm.Timestamp == timestamp
                select cm);
            //conv.MappedMessageId
            return q(hikeDataContext, conv.ConversationId, conv.Timestamp, conv.Message).Count() != 0;
        }

        public void deleteMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.messages
                where o.MessageId == id
                select o);
            hikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(q(hikeDataContext, msgId));
        }


        public Conversation addConversation(String msisdn, bool onhike)
        {
            ContactInfo contactInfo = getContactInfoFromMSISDN(msisdn);

            if (contactInfo != null)
                onhike |= contactInfo.OnHike;

            Conversation conv = new Conversation(msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : null, onhike);
            //                HikeMessengerApp.getPubSub().publish(HikePubSub.NEW_CONVERSATION, conv);
            return conv;
        }

        public List<Conversation> getConversations()
        {
            Func<HikeDataContext, IQueryable<Conversation>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
            ((HikeDataContext hdc) =>
                from o in hdc.conversations
                select o);
            List<Conversation> conversations = q(hikeDataContext).ToList<Conversation>();
            conversations.Sort();
            conversations.Reverse();
            return conversations;
        }
        #endregion

        #region blocked table
        public void addBlockList(List<String> msisdns)
        {
            if (msisdns == null)
            {
                return;
            }

            foreach (String m in msisdns)
            {
                hikeDataContext.blockedUsersTable.InsertOnSubmit(new Blocked(m));
            }
            try
            {
                hikeDataContext.SubmitChanges();
            }
            catch (DuplicateKeyException dke)
            {

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
                String contactName = c.ContactName;
                String lastMessage = message.Message;
                String relativeTime = TimeUtils.getRelativeTime(message.Timestamp);
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
