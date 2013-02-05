using System;
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

namespace windows_client.utils
{
    public class ContactUtils
    {
        private static Stopwatch st;
        public static Dictionary<string, List<ContactInfo>> contactsMap = null;
        public static Dictionary<string, List<ContactInfo>> hike_contactsMap = null;
        public static List<ContactInfo> closeFriends = null;

        public delegate void contacts_Callback(object sender, ContactsSearchEventArgs e);
        public delegate void contactSearch_Callback(object sender, SaveContactResult e);

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
                                enterNamePage.nameErrorTxt.Text = AppResources.No_Network_Txt + " " + AppResources.Please_Try_Again_Txt;
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
            closeFriends = new List<ContactInfo>();
            string lastName = GetLastName();
            bool isLastNameCheckApplicable = lastName != null;
            foreach (Contact cn in contacts)
            {
                CompleteName cName = cn.CompleteName;
                bool hasFacebookAccount = false;
                byte accNumber = 0;
                foreach (Account acc in cn.Accounts)
                {
                    if (acc.Kind == StorageKind.Facebook)
                        hasFacebookAccount = true;
                    accNumber++;

                }
                ContactInfo cInfo = null;
                foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                {
                    if (string.IsNullOrWhiteSpace(ph.PhoneNumber)) // if no phone number simply ignore the contact
                    {
                        count++;
                        continue;
                    }
                    cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber);
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
                if (cInfo != null)
                {
                    if ((hasFacebookAccount || accNumber > 1) && !closeFriends.Contains(cInfo))
                        closeFriends.Add(cInfo);

                    if (isLastNameCheckApplicable)
                    {
                        if (cName.LastName.Trim().ToLower() == lastName && !closeFriends.Contains(cInfo))
                        {
                            closeFriends.Add(cInfo);
                        }
                    }

                    if (MatchFromFamilyVocab(cn.CompleteName) && !closeFriends.Contains(cInfo))
                    {
                        closeFriends.Add(cInfo);
                    }
                }

            }
            Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
            Debug.WriteLine("Total contacts with no phone number : {0}", count);
            return contactListMap;
        }

        public static string GetLastName()
        {
            string name;
            App.appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name == null)
                return null;

            string[] nameArray = name.Trim().Split(' ');
            if (nameArray.Length == 1)
                return null;

            return nameArray[nameArray.Length].ToLower();
        }


        private static string[] familyVocab = new string[] { "aunty", "uncle", "mom", "dad", "daddy" };
     
        public static bool MatchFromFamilyVocab(CompleteName cn)
        {
            if (cn == null)
                return false;
            if (!string.IsNullOrEmpty(cn.FirstName))
            {
                foreach (string vocab in familyVocab)
                {
                    if (cn.FirstName == vocab)
                        return true;
                }
            }

            if (!string.IsNullOrEmpty(cn.MiddleName))
            {
                foreach (string vocab in familyVocab)
                {
                    if (cn.MiddleName == vocab)
                        return true;
                }
            }

            if (!string.IsNullOrEmpty(cn.LastName))
            {
                foreach (string vocab in familyVocab)
                {
                    if (cn.LastName == vocab)
                        return true;
                }
            }
            return false;
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
                            enterNamePage.nameErrorTxt.Text = AppResources.Contact_Scanning_Failed_Txt;
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
            List<ContactInfo> addressbook = AccountUtils.getContactList(jsonForAddressBookAndBlockList, contactsMap, false);
            List<string> blockList = AccountUtils.getBlockList(jsonForAddressBookAndBlockList);

            UpdateCloseFriendListHikeStatus(addressbook);
            App.WriteToIsoStorageSettings(HikeConstants.CLOSE_FRIENDS_NUX, closeFriends);
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
                        enterNamePage.msgTxtBlk.Text = AppResources.EnterName_Failed_Txt;
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

        public static void UpdateCloseFriendListHikeStatus(List<ContactInfo> listAddressBook)
        {
            if (listAddressBook == null)
                return;
            if (closeFriends == null)
                return;

            for (int i = closeFriends.Count - 1; i >= 0; i--)
            {
                ContactInfo cinfo = closeFriends[i];
                int index = listAddressBook.IndexOf(cinfo);
                if (index >= 0)
                {
                    ContactInfo cInfoFromAddressBook = listAddressBook[index];
                    if (cInfoFromAddressBook.OnHike)
                    {
                        closeFriends.RemoveAt(i);
                    }
                }
            }
        }
    }
}
