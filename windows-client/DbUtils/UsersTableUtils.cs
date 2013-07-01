using System;
using windows_client.Model;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using windows_client.View;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;

namespace windows_client.DbUtils
{
    public class UsersTableUtils
    {
        public static string CONTACTS_FILENAME = "_Contacts";
        public static object readWriteLock = new object();
        #region user table

        public static void block(string msisdn)
        {
            Blocked userBlocked = new Blocked(msisdn);
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.InsertOnSubmit(userBlocked);
                try
                {
                    SubmitWithConflictResolve(context);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("UsersTable :: Block user {0} , Exception : {1}", msisdn, e.StackTrace);
                }
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
            if (contacts == null)
                return;
            try
            {
                using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring + "; Max Buffer Size = 2048"))
                {
                    context.users.InsertAllOnSubmit(contacts);
                    context.SubmitChanges();
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("UserTableUtils :: addContacts : submit changes, Exception : " + e.StackTrace);
            }
        }

        public static List<ContactInfo> GetAllHikeContacts()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<ContactInfo> res;
                try
                {
                    res = DbCompiledQueries.GetAllHikeContacts(context).ToList<ContactInfo>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UserTableUtils :: GetAllHikeContacts : GetAllHikeContacts, Exception : " + ex.StackTrace);
                    res = null;
                }
                return res;
            }
        }
        public static List<ContactInfo> GetAllHikeContactsOrdered()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                List<ContactInfo> res;
                try
                {
                    res = DbCompiledQueries.GetAllHikeContactsOrdered(context).ToList<ContactInfo>();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UserTableUtils :: GetAllHikeContactsOrdered : GetAllHikeContactsOrdered, Exception : " + ex.StackTrace);
                    res = null;
                }
                return res;
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
                catch (Exception ex)
                {
                    Debug.WriteLine("UserTableUtils :: getAllContacts : getAllContacts, Exception : " + ex.StackTrace);
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

        public static List<ContactInfo> getAllContactsToInvite()
        {
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                var users = from user in context.users where user.OnHike == false orderby user.Name select user;
                return users.ToList<ContactInfo>();
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
                catch (Exception ex)
                {
                    Debug.WriteLine("UserTableUtils :: getContactInfoFromMSISDN : getContactInfoFromMSISDN, Exception : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("UserTableUtils :: addBlockList : addBlockList, Exception : " + ex.StackTrace);
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
                if (res == null || res.Count == 0)
                    return;
                foreach (ContactInfo cInfo in res)
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

        public static void deleteMultipleRows(List<ContactInfo.DelContacts> ids)
        {
            if (ids == null || ids.Count == 0)
                return;
            bool shouldSubmit = false;
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                //using (HikeChatsDb chats = new HikeChatsDb(App.MsgsDBConnectionstring))
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.GetUsersWithGivenId(context, ids[i].Id));
                        if (App.ViewModel.ConvMap.ContainsKey(ids[i].Msisdn))
                        {
                            ConversationListObject obj = App.ViewModel.ConvMap[ids[i].Msisdn];
                            obj.ContactName = null;
                            ConversationTableUtils.saveConvObject(obj, obj.Msisdn);
                            //ConversationTableUtils.saveConvObjectList();
                        }
                    }
                }
                SubmitWithConflictResolve(context);
            }
        }

        public static void deleteMultipleRows(List<ContactInfo> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            try
            {
                using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
                {
                    for (int i = 0; i < ids.Count; i++)
                    {
                        context.users.DeleteAllOnSubmit<ContactInfo>(DbCompiledQueries.GetUsersWithGivenId(context, ids[i].Id));
                    }
                    SubmitWithConflictResolve(context);
                }
            }
            catch
            {
            }
        }

        public static void updateContacts(List<ContactInfo> updatedContacts)
        {
            if (updatedContacts == null)
                return;
            deleteMultipleRows(updatedContacts);
            addContacts(updatedContacts);
        }

        private static void SubmitWithConflictResolve(HikeUsersDb context)
        {
            try
            {
                context.SubmitChanges(ConflictMode.ContinueOnConflict);
            }
            catch (ChangeConflictException e)
            {
                Debug.WriteLine("UserTableUtils :: SubmitWithConflictResolve : SubmitWithConflictResolve, Exception : " + e.StackTrace);

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
