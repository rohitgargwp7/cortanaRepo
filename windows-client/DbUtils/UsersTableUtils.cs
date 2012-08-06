﻿using System;
using windows_client.Model;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using windows_client.View;

namespace windows_client.DbUtils
{
    public class UsersTableUtils
    {
        #region user table

        public static void block(string msisdn)
        {
            Blocked userBlocked = new Blocked(msisdn);
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.InsertOnSubmit(userBlocked);
                SubmitWithConflictResolve(context);
            }
        }

        public static void unblock(string msisdn)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Blocked> res = DbCompiledQueries.GetBlockedUserForMsisdn(context, msisdn).ToList<Blocked>();
                if (res == null || res.Count == 0)
                    return;
                context.blockedUsersTable.DeleteAllOnSubmit(res);
                SubmitWithConflictResolve(context);
            }
        }

        public static void addContact(ContactInfo user)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.users.InsertOnSubmit(user);
                context.SubmitChanges();
            }
        }

        public static void addContacts(List<ContactInfo> contacts)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.users.InsertAllOnSubmit(contacts);
                context.SubmitChanges();
            }
        }

        public static List<ContactInfo> getAllContacts()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
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

        public static List<ContactInfo> getAllContactsByGroup()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                var users = from user in context.users orderby user.Name select user;
                return users.ToList<ContactInfo>();
            }
        }

        public static List<ContactInfo> getContactInfoFromName(string name)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
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
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
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
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                SubmitWithConflictResolve(context);
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
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
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
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Blocked> res = DbCompiledQueries.GetBlockList(context).ToList<Blocked>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }

        public static void deleteBlocklist()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.SubmitChanges();
            }
        }

        #endregion

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<ContactInfo> res = DbCompiledQueries.GetContactFromMsisdn(context, msisdn).ToList<ContactInfo>();
                if(res == null || res.Count == 0)
                    return;
                foreach(ContactInfo cInfo in res)
                {
                    cInfo.OnHike = (bool)joined;
                }
                SubmitWithConflictResolve(context);
            }
        }

        public static bool isUserBlocked(string msisdn)
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<Blocked> res = DbCompiledQueries.GetBlockedUserForMsisdn(context, msisdn).ToList<Blocked>();
                if (res != null && res.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static void deleteMultipleRows(List<SelectUserToMsg.DelContacts> ids)
        {
            if(ids == null || ids.Count == 0)
                return;
            bool shouldSubmit = false;
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                using (HikeChatsDb chats = new HikeChatsDb(App.MsgsDBConnectionstring))
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.GetUsersWithGivenId(context, ids[i].Id));
                        if (ConversationsList.ConvMap.ContainsKey(ids[i].Msisdn))
                        {
                            ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(chats, ids[i].Msisdn).FirstOrDefault();
                            if (cObj._contactName != null)
                            {
                                cObj.ContactName = null;
                                shouldSubmit = true;
                            }
                            ConversationListObject obj = ConversationsList.ConvMap[ids[i].Msisdn];
                            obj.ContactName = null;
                        }
                    }
                    if (shouldSubmit)
                    {
                        MessagesTableUtils.SubmitWithConflictResolve(chats);
                    }
                }
                SubmitWithConflictResolve(context);
            }
        }

        public static void deleteMultipleRows(List<ContactInfo> ids)
        {
            if (ids == null || ids.Count == 0)
                return;
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.GetUsersWithGivenId(context, ids[i].Id));
                }
                SubmitWithConflictResolve(context);
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
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<ContactInfo> res = DbCompiledQueries.GetContactsForOnhikeStatus(context).ToList<ContactInfo>();
                return (res==null || res.Count == 0) ? null : res;
            }
        }

        private static void SubmitWithConflictResolve(HikeUsersDb context)
        {
            try
            {
                context.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (ChangeConflictException e)
            {
                Console.WriteLine(e.Message);
                // Automerge database values for members that client
                // has not modified.
                foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                {
                    occ.Resolve(RefreshMode.KeepChanges); // second client changes will be submitted.
                }
            }
            // Submit succeeds on second try.
            context.SubmitChanges(ConflictMode.FailOnFirstConflict);
        }
    }
}
