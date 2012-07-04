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
        #region user table

        public static void block(string msisdn)
        {
            Blocked userBlocked = new Blocked(msisdn);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.blockedUsersTable.InsertOnSubmit(userBlocked);
                context.SubmitChanges();
            }
        }

        public static void unblock(string msisdn)
        {
            Blocked userUnblocked = new Blocked(msisdn);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.blockedUsersTable.DeleteOnSubmit(userUnblocked);
                context.SubmitChanges();
            }
        }

        public static void addContact(ContactInfo user)
        {
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                context.users.InsertOnSubmit(user);
                context.SubmitChanges();
            }
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
                where o.Name.Contains(m)
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
        public static void addBlockList(List<string> msisdns)
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


        public static List<Blocked> getBlockList()
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

        public static void deleteBlocklist()
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

        public static bool isUserBlocked(string msisdn)
        {
            Func<HikeDataContext, string, IQueryable<Blocked>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<Blocked>>
             ((HikeDataContext hdc, string ms) =>
                 from o in hdc.blockedUsersTable
                 where o.Msisdn == ms
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                if (q(context, msisdn).Count<Blocked>() > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void deleteMultipleRows(List<string> ids)
        {
            if(ids == null || ids.Count == 0)
                return;
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
             ((HikeDataContext hdc, string i) =>
                 from o in hdc.users
                 where o.Id == i
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    context.users.DeleteAllOnSubmit<ContactInfo>(q(context, ids[i]));
                }
                context.SubmitChanges();
            }
        }

        public static void deleteMultipleRows(List<ContactInfo> ids)
        {
            if (ids == null || ids.Count == 0)
                return;
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
             ((HikeDataContext hdc, string i) =>
                 from o in hdc.users
                 where o.Id == i
                 select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    context.users.DeleteAllOnSubmit<ContactInfo>(q(context, ids[i].Id));
                }
                context.SubmitChanges();
            }
        }

        public static void updateContacts(List<ContactInfo> updatedContacts)
        {
            if (updatedContacts == null)
                return;
            deleteMultipleRows(updatedContacts);
            addContacts(updatedContacts);
        }

        public static List<ContactInfo> getAllContactsToInvite()
        {
            Func<HikeDataContext, IQueryable<ContactInfo>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
            ((HikeDataContext hdc) =>
                from o in hdc.users 
                where o.OnHike == false 
                select o);
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<ContactInfo> myList = q(context).ToList<ContactInfo>();
                return myList.Count == 0 ? null : myList;
            }
        }
    }
}
