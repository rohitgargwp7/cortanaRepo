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
    public class UsersTableUtils
    {
        #region properties

   /*
        
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
    * */
        #endregion

        public void SaveChangesToDB()
        {
            App.HikeDataContext.SubmitChanges();
        }

        #region user table

        public static void block(string msisdn)
        {

        }

        public static void unblock(string msisdn)
        {

        }

        public static void addContact(ContactInfo user)
        {
            App.HikeDataContext.users.InsertOnSubmit(user);
            App.HikeDataContext.SubmitChanges();
            //TODO update observable list
        }

        public static void addContacts(List<ContactInfo> contacts)
        {
            App.HikeDataContext.users.InsertAllOnSubmit(contacts);
            App.HikeDataContext.SubmitChanges();

        }

        public static List<ContactInfo> getAllContacts()
        {
            Func<HikeDataContext, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
            ((HikeDataContext hdc) =>
                from o in hdc.users
                select o);
            return q(App.HikeDataContext).Count<ContactInfo>() == 0 ? null:
                q(App.HikeDataContext).ToList<ContactInfo>();
        }


        public static ContactInfo getContactInfoFromMSISDN(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.users
                where o.Msisdn == m
                select o);

            return q(App.HikeDataContext, msisdn).First();
            //IEnumerable<ContactInfo> contact = from ContactInfo c in App.HikeDataContext.users
            //              where c.Msisdn == msisdn
            //              select c;
            //return contact.First();
        }

        public static void deleteAllContacts()
        {
            App.HikeDataContext.users.DeleteAllOnSubmit<ContactInfo>(App.HikeDataContext.GetTable<ContactInfo>());
            App.HikeDataContext.SubmitChanges();

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
            return q(App.HikeDataContext, conv.Timestamp, conv.Message).Count() != 0;
        }

      

        public static void addMessages(List<ConvMessage> messages)
        {
            App.HikeDataContext.messages.InsertAllOnSubmit(messages);
            App.HikeDataContext.SubmitChanges();
        }

        public static void deleteAllConversations()
        {
            App.HikeDataContext.conversations.DeleteAllOnSubmit<Conversation>(App.HikeDataContext.GetTable<Conversation>());
            App.HikeDataContext.SubmitChanges();

        }


        public static void deleteAllMessages()
        {
            App.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(App.HikeDataContext.GetTable<ConvMessage>());
            App.HikeDataContext.SubmitChanges();

        }


        //public static long getConvIdForMsisdn(string msisdn)
        //{
        //    Func<HikeDataContext, string, IQueryable<long>> q =
        //    CompiledQuery.Compile<HikeDataContext, string, IQueryable<long>>
        //    ((HikeDataContext hdc, string myId) =>
        //        from o in hdc.conversations
        //        where o.Msisdn == myId
        //        select o.ConvId);
        //    return q(App.HikeDataContext, msisdn).Count<long>() == 0? -1 :
        //            q(App.HikeDataContext, msisdn).First<long>();
        //}


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

            App.HikeDataContext.messages.DeleteAllOnSubmit<ConvMessage>(messages(App.HikeDataContext, msisdn));
            App.HikeDataContext.conversations.DeleteAllOnSubmit<Conversation>(conversation(App.HikeDataContext, msisdn));
            App.HikeDataContext.SubmitChanges();
        }

//        public static Conversation addConversation(string msisdn, bool onhike)
        public static void addConversation(string msisdn, bool onhike)
        {
            ContactInfo contactInfo = getContactInfoFromMSISDN(msisdn);

            if (contactInfo != null)
                onhike |= contactInfo.OnHike;
            Conversation conv = new Conversation(msisdn, (contactInfo != null) ? contactInfo.Id : null, (contactInfo != null) ? contactInfo.Name : null, onhike);
            App.HikeDataContext.conversations.InsertOnSubmit(conv);
            App.HikeDataContext.SubmitChanges();
        }

        public static void addMessage(ConvMessage convMessage)
        {
            App.HikeDataContext.messages.InsertOnSubmit(convMessage);
            App.HikeDataContext.SubmitChanges();
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
                App.HikeDataContext.blockedUsersTable.InsertOnSubmit(new Blocked(m));
            }
            try
            {
                App.HikeDataContext.SubmitChanges();
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
            return q(App.HikeDataContext).ToList<Blocked>();
        }

        public void deleteBlocklist()
        {
            App.HikeDataContext.blockedUsersTable.DeleteAllOnSubmit<Blocked>(App.HikeDataContext.GetTable<Blocked>());
            App.HikeDataContext.SubmitChanges();
        }

        #endregion

       /* public void LoadMessagePageList()
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
        */
    
    }
}
