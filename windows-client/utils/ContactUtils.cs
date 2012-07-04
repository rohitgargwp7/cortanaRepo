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
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        private static Dictionary<string, List<ContactInfo>> contactsMap = null;
        private static Dictionary<string, List<ContactInfo>> hike_contactsMap = null;

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
                string token = (string)appSettings["token"];
                AccountUtils.postAddressBook(token, contactListMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
            }
            catch (System.Exception)
            {
                //That's okay, no results//
            }
        }


        public static void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> new_contacts_by_id = getContactsListMap(e.Results);
                Dictionary<string, List<ContactInfo>> hike_contacts_by_id = convertListToMap(UsersTableUtils.getAllContacts());
                Dictionary<string, List<ContactInfo>> contacts_to_update = new Dictionary<string, List<ContactInfo>>();
                foreach (string id in new_contacts_by_id.Keys)
                {
                    List<ContactInfo> phList = new_contacts_by_id[id];
                    if (!hike_contacts_by_id.ContainsKey(id))
                    {
                        contacts_to_update.Add(id, phList);
                        continue;
                    }

                    List<ContactInfo> hkList = hike_contacts_by_id[id];
                    if (!areListsEqual(phList, hkList))
                    {
                        contacts_to_update.Add(id, phList);
                    }
                    hike_contacts_by_id.Remove(id);
                }
                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
                /* If nothing is changed simply return without sending update request*/
                if (contacts_to_update.Count == 0 && hike_contacts_by_id.Count == 0)
                {
                    return;
                }

                JArray ids_json = new JArray();
                foreach (string id in hike_contacts_by_id.Keys)
                {
                    ids_json.Add(id);
                }
                contactsMap = contacts_to_update;
                hike_contactsMap = hike_contacts_by_id;
                AccountUtils.updateAddressBook(contacts_to_update, ids_json, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
            }
            catch (Exception ex)
            {
            }
        }

        private static bool areListsEqual(List<ContactInfo> list1, List<ContactInfo> list2)
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

        private static Dictionary<string, List<ContactInfo>> convertListToMap(List<ContactInfo> hclist)
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

        private static Dictionary<string, List<ContactInfo>> getContactsListMap(IEnumerable<Contact> contacts)
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

            if (addressbook != null)
            {
                UsersTableUtils.deleteAllContacts();
                UsersTableUtils.deleteBlocklist();
                UsersTableUtils.addContacts(addressbook); // add the contacts to hike users db.
                UsersTableUtils.addBlockList(blockList);
                App.Ab_scanned = true;
                appSettings[App.ADDRESS_BOOK_SCANNED] = "y";
                appSettings.Save();
            }
        }

        public static void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (patchJsonObj == null)
                return;
            List<ContactInfo> updatedContacts = AccountUtils.getContactList(patchJsonObj, contactsMap);
            List<string> hikeIds = new List<string>();
            foreach (string id in hike_contactsMap.Keys)
            {
                hikeIds.Add(id);
            }

            if (hikeIds != null && hikeIds.Count > 0)
            {
                /* Delete ids from hike user DB */
                UsersTableUtils.deleteMultipleRows(hikeIds); // this will delete all rows in HikeUser DB that are not in Addressbook.
            }
            if (updatedContacts != null && updatedContacts.Count > 0)
            {
                UsersTableUtils.updateContacts(updatedContacts);
            }
        }
    }
}
