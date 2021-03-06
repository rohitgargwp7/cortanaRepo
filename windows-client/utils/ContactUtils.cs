﻿using System;
using System.Windows;
using Microsoft.Phone.UserData;
using System.Collections.Generic;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Phone.Controls;
using System.Net.NetworkInformation;
using windows_client.Languages;
using System.IO;

namespace windows_client.utils
{
    public class ContactUtils
    {
        public static readonly string IS_ADDRESS_BOOK_SCANNED = "isabscanned";
        public static bool isABScanning;
        private static Stopwatch st;
        private static Stopwatch st2;
        public static Dictionary<string, List<ContactInfo>> contactsMap = null;
        public static Dictionary<string, List<ContactInfo>> hike_contactsMap = null;

        public delegate void contacts_Callback(object sender, ContactsSearchEventArgs e);
        public delegate void contactSearch_Callback(object sender, SaveContactResult e);

        private static volatile ContactScanState cState = ContactScanState.ADDBOOK_NOT_SCANNING;
        public static ContactScanState ContactState
        {
            get
            {
                return cState;
            }
            set
            {
                if (value != cState)
                {
                    cState = value;
                    Debug.WriteLine("Contact state : " + cState.ToString());
                }
            }
        }

        public enum ContactScanState
        {
            ADDBOOK_NOT_SCANNING, // when api is not scanning the phone add book    
            ADDBOOK_SCANNING, // when api is running and scanning
            ADDBOOK_SCANNED, // contacts are scanned
            ADDBOOK_POSTED, // http request is made for addbook 
            ADDBOOK_STORE_FAILED,
            ADDBOOK_STORED_IN_HIKE_DB
        }

        public static void getContacts(contacts_Callback callback)
        {
            st = Stopwatch.StartNew();
            Debug.WriteLine("Contact Scanning started .....");
            cState = ContactScanState.ADDBOOK_SCANNING;
            Contacts cons = new Contacts();
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(callback);
            cons.SearchAsync(string.Empty, FilterKind.None, "State string 1");
        }

        public static void getContact(string number, contacts_Callback callback)
        {
            Contacts cons = new Contacts();
            cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(callback);
            cons.SearchAsync(number, FilterKind.PhoneNumber, "State string 1");
        }

        /* This is called when addressbook scanning on phone gets completed.*/
        public static void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Debug.WriteLine("Contact Scanning Completed ...... ");
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to scan contacts from phone : {0}", msec);

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    if (e != null && e.Results != null)
                        contactsMap = getContactsListMap(e.Results);
                    cState = ContactScanState.ADDBOOK_SCANNED;
                };
                bw.RunWorkerAsync();
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
            int count = 0;
            int duplicates = 0;
            Dictionary<string, List<ContactInfo>> contactListMap = null;
            if (contacts == null)
                return null;
            contactListMap = new Dictionary<string, List<ContactInfo>>();

            foreach (Contact cn in contacts)
            {
                try
                {
                    foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(ph.PhoneNumber)) // if no phone number simply ignore the contact
                            {
                                count++;
                                continue;
                            }
                            ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber);
                            int idd = cInfo.GetHashCode();
                            cInfo.Id = Convert.ToString(Math.Abs(idd));

                            if (contactListMap.ContainsKey(cInfo.Id))
                            {
                                if (!contactListMap[cInfo.Id].Contains(cInfo))
                                    contactListMap[cInfo.Id].Add(cInfo);
                                else
                                {
                                    duplicates++;
                                    Debug.WriteLine("Duplicate Contact !! for Phone Number {0}", cInfo.PhoneNo);
                                }
                            }
                            else
                            {
                                List<ContactInfo> contactList = new List<ContactInfo>();
                                contactList.Add(cInfo);
                                contactListMap.Add(cInfo.Id, contactList);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ContactUtils : getContactsListMap(Inner loop) : Exception : " + ex.StackTrace);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("ContactUtils : getContactsListMap(Outer loop) : Exception : " + e.StackTrace);
                }
            }
            Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
            Debug.WriteLine("Total contacts with no phone number : {0}", count);
            return contactListMap;
        }

        public static void saveContact(string phone, contactSearch_Callback callback)
        {
            SaveContactTask saveContactTask = new SaveContactTask();
            saveContactTask.Completed += new EventHandler<SaveContactResult>(callback);
            saveContactTask.MobilePhone = phone;
            try
            {
                saveContactTask.Show();
            }
            catch { }
        }

    }
}
