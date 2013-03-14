using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using System.ComponentModel;
using windows_client.DbUtils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using windows_client.utils;

namespace windows_client.View
{
    public partial class UnblockedUsersPage : PhoneApplicationPage
    {
        List<ContactInfo> unblockedContacts = null;
        public ObservableCollection<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        bool xyz = true; // this is used to avoid double calling of Text changed function in Textbox
        private string charsEntered;
        Dictionary<string, ObservableCollection<Group<ContactInfo>>> groupListDictionary = new Dictionary<string, ObservableCollection<Group<ContactInfo>>>();
        ObservableCollection<Group<ContactInfo>> glistFiltered = null;
        List<ContactInfo> listContactInfo = null;

        public UnblockedUsersPage()
        {
            InitializeComponent();
            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                Object obj;
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.BLOCKLIST_PAGE, out  obj))
                {
                    BlockListPage blkListPage = obj as BlockListPage;
                    if (blkListPage != null)
                    {
                        unblockedContacts = blkListPage.unblockedContacts;
                    }
                }
                if (unblockedContacts == null)
                {
                    //in case it is null then fetch from db
                    unblockedContacts = UsersTableUtils.getAllContactsByGroup();
                    List<Blocked> listBlocked = UsersTableUtils.getBlockList();
                    FilterBlockedUsers(listBlocked);
                }
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {
                jumpList = getGroupedList(unblockedContacts);
                contactsListBox.ItemsSource = jumpList;
                shellProgress.IsVisible = false;
            };
        }

        private void FilterBlockedUsers(List<Blocked> blockedList)
        {
            if (blockedList == null || blockedList.Count == 0 || unblockedContacts == null || unblockedContacts.Count == 0)
                return;
            Dictionary<string, bool> dictBlocked = new Dictionary<string, bool>();
            foreach (Blocked bl in blockedList)
            {
                dictBlocked.Add(bl.Msisdn, true);
            }
            for (int i = unblockedContacts.Count - 1; i >= 0; i--)
            {
                if (dictBlocked.ContainsKey(unblockedContacts[i].Msisdn))
                    unblockedContacts.RemoveAt(i);
            }
        }

        #region GROUP CREATION FUNCTIONS

        private ObservableCollection<Group<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            ObservableCollection<Group<ContactInfo>> glist = createGroups();

            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
            {
                ContactInfo c = allContactsList[i];

                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Add(c);
            }
            return glist;
        }

        private ObservableCollection<Group<ContactInfo>> createGroups()
        {
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            ObservableCollection<Group<ContactInfo>> glist = new ObservableCollection<Group<ContactInfo>>();
            foreach (char c in Groups)
            {
                Group<ContactInfo> g = new Group<ContactInfo>(c.ToString(), new ObservableCollection<ContactInfo>());
                glist.Add(g);
            }
            return glist;
        }

        private static string GetCaptionGroup(ContactInfo c)
        {
            char key = char.ToLower(c.Name[0]);
            if (key < 'a' || key > 'z')
            {
                key = '#';
            }
            return key.ToString();
        }
        public class Group<T> : ObservableCollection<T>
        {
            public Group(string name, ObservableCollection<T> items)
            {
                this.Title = name;
            }

            public string Title
            {
                get;
                set;
            }
        }

        #endregion

        private void contactsListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //todo:handle tap of blocks
            ContactInfo c = contactsListBox.SelectedItem as ContactInfo;
            if (c == null)
                return;
            shellProgress.IsVisible = true;
            App.HikePubSubInstance.publish(HikePubSub.BLOCK_USER, c);

            string ch = GetCaptionGroup(c);
            int index = (ch == "#") ? 26 : ch[0] - 'a';
            jumpList[index].Remove(c);

            if (glistFiltered != null)
            {
                glistFiltered[index].Remove(c);
            }

            //removed contacts to be removed from search dictionary
            if (listContactInfo == null)
                listContactInfo = new List<ContactInfo>() { c };
            else
                listContactInfo.Add(c);

            contactsListBox.SelectedItem = null;
            shellProgress.IsVisible = false;
        }

        #region SEARCH HELPER FUNCTIONS

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (xyz) // this is done to avoid twice calling of "enterNameTxt_TextChanged" function
            {
                xyz = !xyz;
                return;
            }
            xyz = !xyz;


            charsEntered = enterNameTxt.Text.ToLower();
            Debug.WriteLine("Chars Entered : {0}", charsEntered);

            charsEntered = charsEntered.Trim();
            if (String.IsNullOrWhiteSpace(charsEntered))
            {
                contactsListBox.ItemsSource = jumpList;
                return;
            }

            if (groupListDictionary.ContainsKey(charsEntered))
            {
                ObservableCollection<Group<ContactInfo>> gl = groupListDictionary[charsEntered];
                if (gl == null)
                {
                    groupListDictionary.Remove(charsEntered);
                    contactsListBox.ItemsSource = null;
                    return;
                }
                else
                {
                    if (listContactInfo != null)
                    {
                        foreach (ContactInfo cinfo in listContactInfo)
                        {
                            string ch = GetCaptionGroup(cinfo);
                            // calculate the index into the list
                            int index = (ch == "#") ? 26 : ch[0] - 'a';
                            // and remove the entry
                            gl[index].Remove(cinfo);
                        }
                    }
                }
                glistFiltered = gl;

                contactsListBox.ItemsSource = glistFiltered;
                Thread.Sleep(5);
                return;
            }
            //glistFiltered = createGroups();
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, ev) =>
            {
                glistFiltered = getFilteredContactsFromNameOrPhoneAsync(charsEntered, 0, 26);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, ev) =>
            {
                if (glistFiltered != null)
                    groupListDictionary[charsEntered] = glistFiltered;
                contactsListBox.ItemsSource = glistFiltered;
                Thread.Sleep(2);
            };
        }

        private ObservableCollection<Group<ContactInfo>> getFilteredContactsFromNameOrPhoneAsync(string charsEntered, int start, int end)
        {
            ObservableCollection<Group<ContactInfo>> listToIterate = null;
            int charsLength = charsEntered.Length - 1;
            if (charsLength > 0)
            {
                if (groupListDictionary.ContainsKey(charsEntered.Substring(0, charsLength)))
                {
                    listToIterate = groupListDictionary[charsEntered.Substring(0, charsEntered.Length - 1)];
                    if (listToIterate == null)
                        listToIterate = jumpList;
                    else
                    {
                        if (listContactInfo != null)
                        {
                            foreach (ContactInfo cinfo in listContactInfo)
                            {
                                string ch = GetCaptionGroup(cinfo);
                                // calculate the index into the list
                                int index = (ch == "#") ? 26 : ch[0] - 'a';
                                // and remove the entry
                                listToIterate[index].Remove(cinfo);
                            }
                        }
                    }
                }
                else
                    listToIterate = jumpList;
            }
            else
                listToIterate = jumpList;
            bool createNewFilteredList = true;
            for (int i = start; i < end; i++)
            {
                int maxJ = listToIterate == null ? 0 : (listToIterate[i] == null ? 0 : listToIterate[i].Count);
                for (int j = 0; j < maxJ; j++)
                {
                    ContactInfo cn = listToIterate[i][j];
                    if (cn.Name.ToLower().Contains(charsEntered) || cn.Msisdn.Contains(charsEntered) || cn.PhoneNo.Contains(charsEntered))
                    {
                        if (createNewFilteredList)
                        {
                            createNewFilteredList = false;
                            glistFiltered = createGroups();
                        }
                        glistFiltered[i].Add(cn);
                    }
                }
            }

            if (createNewFilteredList)
                return null;

            return glistFiltered;
        }

        #endregion
    }
}