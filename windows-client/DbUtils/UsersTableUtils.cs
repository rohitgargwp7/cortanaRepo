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

        #region user table

        public static void block(string msisdn)
        {

        }

        public static void unblock(string msisdn)
        {

        }

        public static void addContact(ContactInfo user)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.users.InsertOnSubmit(user);
                context.SubmitChanges();
            }
            //TODO update observable list
        }

        public static void addContacts(List<ContactInfo> contacts)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.users.InsertAllOnSubmit(contacts);
                context.SubmitChanges();
            }
        }

        public static List<ContactInfo> getAllContacts()
        {
            Func<HikeDataContext, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
            ((HikeDataContext hdc) =>
                from o in hdc.users
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context).Count<ContactInfo>() == 0 ? null :
                    q(context).ToList<ContactInfo>();
            }
        }

        public static List<ContactInfo> getContactInfoFromName(string name)
        {
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.users
                where o.Name.Contains(name)
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, name).Count<ContactInfo>() == 0 ? null :
                    q(context, name).ToList<ContactInfo>();
            }
        }

        public static ContactInfo getContactInfoFromMSISDN(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
            ((HikeDataContext hdc, string m) =>
                from o in hdc.users
                where o.Msisdn == m
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, msisdn).Count<ContactInfo>() == 0 ? null :
                   q(context, msisdn).First();
            }
        }

        public static void deleteAllContacts()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                context.SubmitChanges();
            }
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
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context, conv.Timestamp, conv.Message).Count() != 0;
            }
        }

      

        public static void addMessages(List<ConvMessage> messages)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.InsertAllOnSubmit(messages);
                context.SubmitChanges();
            }
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

            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(messages(context, msisdn));
                context.conversations.DeleteAllOnSubmit<Conversation>(conversation(context, msisdn));
                context.SubmitChanges();
            }
        }

      
        
        #endregion

        #region blocked table
        public void addBlockList(List<string> msisdns)
        {
            if (msisdns == null)
            {
                return;
            }
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                foreach (string m in msisdns)
                {
                    context.blockedUsersTable.InsertOnSubmit(new Blocked(m));
                }
                try
                {
                    context.SubmitChanges();
                }
                catch (DuplicateKeyException dke)
                {
                    dke.ToString();
                }
            }
        }


        public List<Blocked> getBlockList()
        {
            Func<HikeDataContext, IQueryable<Blocked>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<Blocked>>
            ((HikeDataContext hdc) =>
                from o in hdc.blockedUsersTable
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                return q(context).ToList<Blocked>();
            }
        }

        public void deleteBlocklist()
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.SubmitChanges();
            }
        }

        #endregion

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            ContactInfo cInfo = null;
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
             ((HikeDataContext hdc, string ms) =>
                 from o in hdc.users
                 where o.Msisdn == ms
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                if (q(context, msisdn).Count<ContactInfo>() == 1)
                {
                    cInfo = q(context, msisdn).ToList<ContactInfo>().First<ContactInfo>();
                    cInfo.OnHike = (bool)joined;
                    context.SubmitChanges();
                }
            }
        }
    }
}
