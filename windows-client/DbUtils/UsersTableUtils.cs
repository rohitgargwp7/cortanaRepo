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
            Func<HikeDataContext,string,IQueryable<Blocked>> q =
            CompiledQuery.Compile<HikeDataContext,string,IQueryable<Blocked>>
            ((HikeDataContext hdc,string m) =>
                from o in hdc.blockedUsersTable
                where o.Msisdn == m
                select o);
           
            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<Blocked> res = DbCompiledQueries.GetBlockedUserForMsisdn(context, msisdn).ToList<Blocked>();
                if (res == null || res.Count == 0)
                    return;
                context.blockedUsersTable.DeleteAllOnSubmit(res);
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
                List<ContactInfo> res;
                try
                {
                    res = DbCompiledQueries.GetAllContacts(context).ToList<ContactInfo>();
                }
                catch (ArgumentNullException)
                {
                    res = null;
                }
                return (res == null || res.Count == 0) ? null : res;
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
                List<ContactInfo> res;
                try
                {
                    res = DbCompiledQueries.GetContactFromName(context, name).ToList<ContactInfo>();
                }
                catch (Exception)
                {
                    res = null;
                }
                return (res == null || res.Count == 0) ? null : res;
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
                List<ContactInfo> res;
                try
                {
                    res = DbCompiledQueries.GetContactFromMsisdn(context, msisdn).ToList<ContactInfo>();
                }
                catch (Exception)
                {
                    res = null;
                }
                return (res == null || res.Count == 0) ? null : res.First();
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
                List<Blocked> res = DbCompiledQueries.GetBlockList(context).ToList<Blocked>();
                return (res == null || res.Count == 0) ? null : res;
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
            Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
             CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
             ((HikeDataContext hdc, string ms) =>
                 from o in hdc.users
                 where o.Msisdn == ms
                 select o);

            using (HikeDataContext context = new HikeDataContext(App.DBConnectionstring))
            {
                List<ContactInfo> res = DbCompiledQueries.GetContactFromMsisdn(context, msisdn).ToList<ContactInfo>();
                if(res == null || res.Count == 0)
                    return;
                foreach(ContactInfo cInfo in res)
                {
                    cInfo.OnHike = (bool)joined;
                }
                context.SubmitChanges();
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
                List<Blocked> res = DbCompiledQueries.GetBlockedUserForMsisdn(context, msisdn).ToList<Blocked>();
                if (res != null && res.Count > 0)
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
                    context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.DeleteUsersWithGivenId(context, ids[i]));
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
                    context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.DeleteUsersWithGivenId(context, ids[i].Id));
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
                List<ContactInfo> res = DbCompiledQueries.GetContactsForOnhikeStatus(context).ToList<ContactInfo>();
                return (res==null || res.Count == 0) ? null : res;
            }
        }
    }
}
