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
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using windows_client.DbUtils;

namespace windows_client.utils
{
    public class ContactUtils
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        private static Dictionary<string, List<ContactInfo>> contactsMap = null;

        public delegate void contacts_Callback(object sender, ContactsSearchEventArgs e);

        public static void getContacts(contacts_Callback callback)
        {
            Contacts cons = new Contacts();
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(callback);
            cons.SearchAsync(string.Empty, FilterKind.None, "State string 1");
        }

        /* This is called when addressbook scanning on windows gets completed.*/
        public static void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> contactListMap = new Dictionary<string, List<ContactInfo>>();
                int id = 0;
                IEnumerable<Contact> contacts = e.Results;
                foreach (Contact cn in contacts)
                {
                    id++;
                    CompleteName cName = cn.CompleteName;
                    List<ContactInfo> contactList = new List<ContactInfo>();
                    foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                    {
                        ContactInfo cInfo = new ContactInfo(Convert.ToString(id), null, cn.DisplayName, ph.PhoneNumber);
                        contactList.Add(cInfo);
                    }
                    contactListMap.Add(Convert.ToString(id), contactList);
                }
                contactsMap = contactListMap;
                string token = (string)appSettings["token"];
                AccountUtils.postAddressBook(token, contactListMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
            }
            catch (System.Exception)
            {
                //That's okay, no results//
            }
        }

        /* This is the callback function which is called when server returns the addressbook*/
        public static void postAddressBook_Callback(JObject jsonForAddressBookAndBlockList)
        {
            // test this is called
            JObject obj = jsonForAddressBookAndBlockList;
            if (obj == null)
            {
                return;
            }
            List<ContactInfo> addressbook = AccountUtils.getContactList(jsonForAddressBookAndBlockList, contactsMap);
            List<string> blockList = AccountUtils.getBlockList(jsonForAddressBookAndBlockList);
       
            if (addressbook != null)
            {
                HikeDbUtils.addContacts(addressbook); // add the contacts to hike users db.
                App.Ab_scanned = true;
                appSettings[App.ADDRESS_BOOK_SCANNED] = "y";
                appSettings.Save();
            }
        }
    }
}
