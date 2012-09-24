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
using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Phone.Controls;
using System.Net.NetworkInformation;

namespace windows_client.utils
{
    public class ContactUtils
    {
        private static Stopwatch st;
        public static Dictionary<string, List<ContactInfo>> contactsMap = null;
        public static Dictionary<string, List<ContactInfo>> hike_contactsMap = null;

        public delegate void contacts_Callback(object sender, ContactsSearchEventArgs e);

        public static void getContacts(contacts_Callback callback)
        {
            st = Stopwatch.StartNew();
            Debug.WriteLine("Get Contacts thread : {0}", Thread.CurrentThread.ToString());
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += (s, e) =>
            {
                Debug.WriteLine("Contacts async thread : {0}", Thread.CurrentThread.ToString());
                App.isABScanning = true;
                Contacts cons = new Contacts();
                cons.SearchCompleted += new EventHandler<ContactsSearchEventArgs>(callback);
                cons.SearchAsync(string.Empty, FilterKind.None, "State string 1");
            };
            bw.RunWorkerAsync();
        }

        /* This is called when addressbook scanning on phone gets completed.*/
        public static void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to scan contacts from phone : {0}", msec);
                Debug.WriteLine("Contacts callback thread : {0}", Thread.CurrentThread.ToString());
                IEnumerable<Contact> contacts = e.Results;
                Dictionary<string, List<ContactInfo>> contactListMap = getContactsListMap(contacts);
                contactsMap = contactListMap;
                if (!NetworkInterface.GetIsNetworkAvailable())
                {
                    App.WriteToIsoStorageSettings(App.CONTACT_SCANNING_FAILED, true);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;

                        if (currentPage != null)
                        {
                            EnterName enterNamePage = (EnterName)currentPage;
                            if (enterNamePage.isClicked)
                            {
                                enterNamePage.msgTxtBlk.Opacity = 0;
                                enterNamePage.nameErrorTxt.Text = "Network Error. Try Again!!";
                                enterNamePage.nameErrorTxt.Visibility = Visibility.Visible;
                                enterNamePage.progressBar.IsEnabled = false;
                                enterNamePage.progressBar.Opacity = 0;
                                enterNamePage.nextIconButton.IsEnabled = true;
                                enterNamePage.txtBxEnterName.IsReadOnly = false;
                            }
                        }
                    });

                    return;
                }
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
            int count = 0;
            int duplicates = 0;
            Dictionary<string, List<ContactInfo>> contactListMap = null;
            if (contacts == null)
                return null;
            contactListMap = new Dictionary<string, List<ContactInfo>>();
            foreach (Contact cn in contacts)
            {
                CompleteName cName = cn.CompleteName;

                foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
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
            }
            Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
            Debug.WriteLine("Total contacts with no phone number : {0}", count);
            return contactListMap;
        }

        /* This is the callback function which is called when server returns the addressbook*/
        public static void postAddressBook_Callback(JObject jsonForAddressBookAndBlockList)
        {
            Debug.WriteLine("Post Addressbook callback thread : {0}", Thread.CurrentThread.ToString());
            // test this is called
            JObject obj = jsonForAddressBookAndBlockList;
            if (obj == null)
            {
                App.WriteToIsoStorageSettings(App.CONTACT_SCANNING_FAILED, true);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;

                    if (currentPage != null)
                    {
                        EnterName enterNamePage = (EnterName)currentPage;
                        if (enterNamePage.isClicked)
                        {
                            enterNamePage.msgTxtBlk.Opacity = 0;
                            enterNamePage.nameErrorTxt.Text = "Contact Scanning failed!! Try Later!!";
                            enterNamePage.nameErrorTxt.Visibility = Visibility.Visible;
                            enterNamePage.progressBar.IsEnabled = false;
                            enterNamePage.progressBar.Opacity = 0;
                            enterNamePage.nextIconButton.IsEnabled = true;
                            enterNamePage.txtBxEnterName.IsReadOnly = false;
                        }
                    }
                });
                return;
            }
            List<ContactInfo> addressbook = AccountUtils.getContactList(jsonForAddressBookAndBlockList, contactsMap);
            List<string> blockList = AccountUtils.getBlockList(jsonForAddressBookAndBlockList);
            int count = 1;
            // waiting for DB to be created
            while (!App.appSettings.Contains(App.IS_DB_CREATED) && count <= 30)
            {
                Thread.Sleep(1 * 1000);
                count++;
            }
            if (!App.appSettings.Contains(App.IS_DB_CREATED)) // if DB is not created for so long
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;

                    if (currentPage != null)
                    {
                        EnterName enterNamePage = (EnterName)currentPage;
                        enterNamePage.msgTxtBlk.Text = "Failed. Close App and try again !!";
                    }
                });
            }

            if (addressbook != null)
            {
                UsersTableUtils.deleteAllContacts();
                UsersTableUtils.deleteBlocklist();
                Stopwatch st = Stopwatch.StartNew();
                UsersTableUtils.addContacts(addressbook); // add the contacts to hike users db.
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to add addressbook {0}", msec);
                UsersTableUtils.addBlockList(blockList);
                App.WriteToIsoStorageSettings(App.IS_ADDRESS_BOOK_SCANNED, true);
                App.RemoveKeyFromAppSettings(App.CONTACT_SCANNING_FAILED);
            }
            App.Ab_scanned = true;
            App.isABScanning = false;

            while (!App.appSettings.Contains(App.ACCOUNT_NAME) && !App.appSettings.Contains(App.SET_NAME_FAILED))
            {
                Thread.Sleep(1 * 1000);
            }

            if (App.appSettings.Contains(App.SET_NAME_FAILED)) // case when set name api is failed
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var currentPage = ((PhoneApplicationFrame)Application.Current.RootVisual).Content;

                if (currentPage != null)
                {
                    EnterName enterNamePage = (EnterName)currentPage;
                    enterNamePage.processEnterName();
                }
            });
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
