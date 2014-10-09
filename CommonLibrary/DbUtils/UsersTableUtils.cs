using System;
using CommonLibrary.Model;
using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using System.IO.IsolatedStorage;
using System.IO;
using System.Diagnostics;
using CommonLibrary.utils;
using CommonLibrary.Constants;
using CommonLibrary.Misc;

namespace CommonLibrary.DbUtils
{
    public class UsersTableUtils
    {
        public static ContactInfo getContactInfoFromMSISDN(string msisdn)
        {
            using (HikeUsersDb context = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring))
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

        public static List<Blocked> getBlockList()
        {
            using (HikeUsersDb context = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring))
            {
                List<Blocked> res = DbCompiledQueries.GetBlockList(context).ToList<Blocked>();
                return (res == null || res.Count == 0) ? null : res;
            }
        }
        
        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            using (HikeUsersDb context = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring))
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

        public static void deleteMultipleRows(List<ContactInfo> ids)
        {
            if (ids == null || ids.Count == 0)
                return;

            try
            {
                using (HikeUsersDb context = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring))
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
