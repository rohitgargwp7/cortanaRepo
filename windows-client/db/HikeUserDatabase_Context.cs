using System.Data.Linq;
using System.Collections.Generic;
using System.Linq;
using windows_client.Model;

namespace windows_client.db
{
//    [Database(Name="HikeUserDB")]
    public class HikeUserDatabase_Context : DataContext
    {
        // Specify the connection string as a static, used in main page and app.xaml.
        public static string DBConnectionstring = "Data Source=isostore:/HikeUserDb.sdf";

        // Pass the connection string to the base class.
        public HikeUserDatabase_Context(string connectionstring)
            : base(connectionstring)
        { }

        #region User Table
        public Table<ContactInfo> Users
        {
            get
            {
                return this.GetTable<ContactInfo>();
            }
        }

        /* This function inserts all the elements in the list in the db.
         */
        public static void addContacts(List<ContactInfo> user)
        {
            using (HikeUserDatabase_Context db = new HikeUserDatabase_Context(DBConnectionstring))
            {
                db.Users.InsertAllOnSubmit(user);
                db.SubmitChanges();
            }
        }

        public static void addContact(ContactInfo user)
        {
            using (HikeUserDatabase_Context db = new HikeUserDatabase_Context(DBConnectionstring))
            {
                db.Users.InsertOnSubmit(user);
                db.SubmitChanges();
            }
        }

        /* This function returns list of users*/
        public static IList<ContactInfo> getUsers()
        {
            IList<ContactInfo> usersList = null;
            using (HikeUserDatabase_Context db = new HikeUserDatabase_Context(DBConnectionstring))
            {
                IQueryable<ContactInfo> userQuery = from user in db.Users select user;
                usersList = userQuery.ToList();
            }
            return usersList;
        }

        #endregion

    }
}
