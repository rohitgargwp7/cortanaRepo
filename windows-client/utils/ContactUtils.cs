using System;
using System.Windows;
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;
using windows_client.View;
using System.Threading;
using Phone.Controls;

namespace windows_client.utils
{
    public class ContactUtils
    {
        public static Dictionary<string, List<ContactInfo>> contactsMap = null;
        public static Dictionary<string, List<ContactInfo>> hike_contactsMap = null;

        public delegate void contacts_Callback(object sender, ContactsSearchEventArgs e);

        public static void getContacts(contacts_Callback callback)
        {
            App.isABScanning = true;
            Contacts cons = new Contacts();
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(callback);
            cons.SearchAsync(string.Empty, FilterKind.None, "State string 1");
        }

        /* This is called when addressbook scanning on windows gets completed.*/
        public static void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                IEnumerable<Contact> contacts = e.Results;
                Dictionary<string, List<ContactInfo>> contactListMap = getContactsListMap(contacts);
                contactsMap = contactListMap;
                string token = (string)App.appSettings["token"];
                AccountUtils.postAddressBook(contactListMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
            }
            catch (System.Exception)
            {
                //That's okay, no results//
            }
        }

        public static bool areListsEqual(List<ContactInfo> list1, List<ContactInfo> list2)
        {
            if (list1 != null && list2 != null)
            {
                if (list1.Count != list2.Count)
                    return false;
                else if (list1.Count == 0 && list2.Count == 0)
                {
                    return false;
                }
                else
                // represents same number of elements
                {
                    /* compare each element */
                    /* As Windows phone does not have a hashset we are using Dictionary*/
                    Dictionary<ContactInfo, bool> set1 = new Dictionary<ContactInfo, bool>();
                    for (int i = 0; i < list1.Count; i++)
                    {
                        set1.Add(list1[i], true);
                    }
                    bool flag = true;
                    for (int i = 0; i < list2.Count; i++)
                    {
                        ContactInfo c = list2[i];
                        if (!set1.ContainsKey(c))
                        {
                            flag = false;
                            break;
                        }
                    }
                    return flag;
                }
            }
            return false;
        }

        public static Dictionary<string, List<ContactInfo>> convertListToMap(List<ContactInfo> hclist)
        {
            if (hclist == null)
                return null;
            Dictionary<string, List<ContactInfo>> hikeContactListMap = new Dictionary<string, List<ContactInfo>>();
            List<ContactInfo> contactList = null;
            for (int i = 0; i < hclist.Count; i++)
            {
                if (hikeContactListMap.ContainsKey(hclist[i].Id))
                {
                    hikeContactListMap[hclist[i].Id].Add(hclist[i]);
                }
                else
                {
                    contactList = new List<ContactInfo>();
                    contactList.Add(hclist[i]);
                    hikeContactListMap.Add(hclist[i].Id, contactList);
                }
            }
            return hikeContactListMap;
        }

        public static Dictionary<string, List<ContactInfo>> getContactsListMap(IEnumerable<Contact> contacts)
        {

            Dictionary<string, List<ContactInfo>> contactListMap = null;
            if (contacts == null)
                return null;
            contactListMap = new Dictionary<string, List<ContactInfo>>();
            foreach (Contact cn in contacts)
            {
                CompleteName cName = cn.CompleteName;

                foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                {
                    ContactInfo cInfo = new ContactInfo(null, cn.DisplayName, ph.PhoneNumber);
                    int idd = cInfo.GetHashCode();
                    cInfo.Id = Convert.ToString(Math.Abs(idd));
                    if (contactListMap.ContainsKey(cInfo.Id))
                    {
                        contactListMap[cInfo.Id].Add(cInfo);
                    }
                    else
                    {
                        List<ContactInfo> contactList = new List<ContactInfo>();
                        contactList.Add(cInfo);
                        contactListMap.Add(cInfo.Id, contactList);
                    }
                }
            }
            return contactListMap;
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

            while (!App.isDbCreated)
                Thread.Sleep(50);
            if (addressbook != null)
            {
                UsersTableUtils.deleteAllContacts();
                UsersTableUtils.deleteBlocklist();
                UsersTableUtils.addContacts(addressbook); // add the contacts to hike users db.
                UsersTableUtils.addBlockList(blockList);
                App.Ab_scanned = true;
                App.WriteToIsoStorageSettings(App.IS_ADDRESS_BOOK_SCANNED,true);
            }
        }

        public static void saveContact(string phone)
        {
            SaveContactTask saveContactTask = new SaveContactTask();
            saveContactTask.Completed += new EventHandler<SaveContactResult>(saveContactTask_Completed);
            saveContactTask.MobilePhone = phone;
            saveContactTask.Show();
        }

        private static void saveContactTask_Completed(object sender, SaveContactResult e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    MessageBox.Show("Contact is successfully saved.");
                    break;
                case TaskResult.Cancel:
                    MessageBox.Show("The user canceled the task.");
                    break;
                case TaskResult.None:
                    MessageBox.Show("NO information regarding the task result is available.");
                    break;
            }
        }
    }
}
