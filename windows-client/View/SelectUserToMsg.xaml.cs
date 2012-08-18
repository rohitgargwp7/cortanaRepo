﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;
using Microsoft.Phone.UserData;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Windows.Input;


namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        int xyz = 1; // this is used to avoid double calling of Text changed function in Textbox
        private bool isExistingGroup = false;
        private bool isGroupChat = false;
        public List<ContactInfo> contactsForgroup = null;
        public MyProgressIndicator progress = null;
        public bool canGoBack = true;
        public List<Group<ContactInfo>> groupedList = null;
        private readonly SolidColorBrush textBoxBorder = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        private string charsEntered;
        private bool typedTextDeleted = true;
        private readonly int maxSMSUsersAllowed = 5;
        private readonly int maxUsersAllowed = 10;
        private int smsUserCount = 0;
        private int existingGroupUsers = 1; // 1 because owner of the group is already included
        private int defaultGroupmembers = 0;
        private ApplicationBar appBar;
        private ApplicationBarIconButton doneIconButton = null;
        private Dictionary<string, List<MsisdnCordinates>> msisdnPositions = null;
        private Stack<int> indexOfAddedContacts = new Stack<int>();
        private bool textChangedFromDelete = false;
        private StringBuilder stringBuilderForContactNames = new StringBuilder();

        public class MsisdnCordinates
        {
            private int _groupIdx;
            private int _listIdx;
            ContactInfo _cInfo;

            public MsisdnCordinates(int grId, int liId, ContactInfo c)
            {
                _groupIdx = grId;
                _listIdx = liId;
                _cInfo = c;
            }
            public int GroupIdx
            {
                get { return _groupIdx; }
                set { _groupIdx = value; }
            }
            public int ListIdx
            {
                get { return _listIdx; }
                set { _listIdx = value; }
            }
            public ContactInfo Contact
            {
                get { return _cInfo; }
                set { _cInfo = value; }
            }
        }

        public class Group<T> : IEnumerable<T>
        {
            public Group(string name, List<T> items)
            {
                this.Title = name;
                this.Items = items;
            }

            public override bool Equals(object obj)
            {
                Group<T> that = obj as Group<T>;

                return (that != null) && (this.Title.Equals(that.Title));
            }
            public override int GetHashCode()
            {
                return this.Title.GetHashCode();
            }
            public string Title
            {
                get;
                set;
            }

            public List<T> Items
            {
                get;
                set;
            }
            public bool HasItems
            {
                get
                {
                    return (Items == null || Items.Count == 0) ? false : true;
                }
            }

            /// <summary>
            /// This is used to colour the tiles - greying out those that have no entries
            /// </summary>
            public Brush GroupBackgroundBrush
            {
                get
                {
                    return (SolidColorBrush)Application.Current.Resources[(HasItems) ? "PhoneAccentBrush" : "PhoneChromeBrush"];
                }
            }
            #region IEnumerable<T> Members

            public IEnumerator<T> GetEnumerator()
            {
                return this.Items.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.Items.GetEnumerator();
            }

            #endregion
        }

        public SelectUserToMsg()
        {
            InitializeComponent();
            /* Case whe this page is called from GroupInfo page*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
                isGroupChat = true;

            /* Case when this page is called from create group button.*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.START_NEW_GROUP))
            {
                isGroupChat = (bool)PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP];
                PhoneApplicationService.Current.State.Remove(HikeConstants.START_NEW_GROUP);
            }
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_LoadAllContacts);
            bw.RunWorkerAsync();
            initPage();
        }

        private void initPage()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            ApplicationBarIconButton refreshIconButton = new ApplicationBarIconButton();
            refreshIconButton.IconUri = new Uri("/View/images/icon_refresh.png", UriKind.Relative);
            refreshIconButton.Text = "Refresh Contacts";
            refreshIconButton.Click += new EventHandler(refreshContacts_Click);
            refreshIconButton.IsEnabled = true;
            appBar.Buttons.Add(refreshIconButton);
            selectUserPage.ApplicationBar = appBar;

            if (isGroupChat)
            {
                /* Add icons */
                if (doneIconButton != null)
                    return;
                doneIconButton = new ApplicationBarIconButton();
                doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
                doneIconButton.Text = "Done";
                doneIconButton.Click += new EventHandler(startGroup_Click);
                doneIconButton.IsEnabled = false;
                appBar.Buttons.Add(doneIconButton);
                contactsListBox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(contactSelectedForGroup_Click);
                enterNameTxt.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(enterNameTxt_KeyDown), true);
            }
            else
            {
                contactsListBox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(contactSelected_Click);
            }
        }

        #region  MAKE JUMP LIST

        private void bw_LoadAllContacts(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                List<ContactInfo> allContactsList = UsersTableUtils.getAllContactsByGroup();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupedList = getGroupedList(allContactsList);
                    contactsListBox.ItemsSource = groupedList;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;

                });
            }
        }

        private List<Group<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            if (allContactsList == null || allContactsList.Count == 0)
                return null;
            List<GroupMembers> activeExistingGroupMembers = null;
            if (isGroupChat)
                msisdnPositions = new Dictionary<string, List<MsisdnCordinates>>();
            List<Group<ContactInfo>> glist = createGroups();

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                isExistingGroup = true;
                activeExistingGroupMembers = PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] as List<GroupMembers>;

                //TODO start this loop from end, after sorting is done on onHike status
                smsUserCount = 0;
                existingGroupUsers = 0;
                for (int i = 0; i < activeExistingGroupMembers.Count; i++)
                {
                    if (!Utils.getGroupParticipant(activeExistingGroupMembers[i].Name, activeExistingGroupMembers[i].Msisdn).IsOnHike)
                    {
                        smsUserCount++;
                    }
                    existingGroupUsers++;
                }
                defaultGroupmembers = existingGroupUsers;
            }
            for (int i = 0; i < allContactsList.Count; i++)
            {
                ContactInfo c = allContactsList[i];
                if (isExistingGroup)
                {
                    if (msisdnAlreadyExists(c.Msisdn, activeExistingGroupMembers))
                        continue;
                }
                if (c.Msisdn == App.MSISDN) // don't show own number in any chat.
                    continue;
                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 0 : ch[0] - 'a' + 1;
                // and add the entry
                glist[index].Items.Add(c);
                if (isGroupChat)
                {
                    if (msisdnPositions.ContainsKey(c.Msisdn))
                    {
                        msisdnPositions[c.Msisdn].Add(new MsisdnCordinates(index, glist[index].Items.IndexOf(c), c));
                    }
                    else
                    {
                        List<MsisdnCordinates> list = new List<MsisdnCordinates>();
                        list.Add(new MsisdnCordinates(index, glist[index].Items.IndexOf(c), c));
                        msisdnPositions.Add(c.Msisdn, list);
                    }
                }
            }
            return glist;
        }

        private bool msisdnAlreadyExists(string msisdn, List<GroupMembers> activeExistingGroupMembers)
        {
            for (int i = 0; i < activeExistingGroupMembers.Count; i++)
            {
                if (msisdn == activeExistingGroupMembers[i].Msisdn)
                    return true;
            }
            return false;
        }

        private List<Group<ContactInfo>> createGroups()
        {
            string Groups = "#abcdefghijklmnopqrstuvwxyz";
            List<Group<ContactInfo>> glist = new List<Group<ContactInfo>>();
            foreach (char c in Groups)
            {
                Group<ContactInfo> g = new Group<ContactInfo>(c.ToString(), new List<ContactInfo>());
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

        #endregion

        private List<Group<ContactInfo>> getFilteredContactsFromNameOrPhone(string charsEnetered)
        {
            if (groupedList == null || groupedList.Count == 0)
                return null;
            List<Group<ContactInfo>> glistFiltered = createGroups();
            for (int i = 0; i < groupedList.Count; i++)
            {
                for (int j = 0; j < (groupedList[i].Items == null ? 0 : groupedList[i].Items.Count); j++)
                {
                    ContactInfo cn = groupedList[i].Items[j];
                    if (cn.Name.ToLower().Contains(charsEnetered) || cn.Msisdn.Contains(charsEnetered) || cn.PhoneNo.Contains(charsEnetered))
                    {
                        glistFiltered[i].Items.Add(cn);
                    }
                }
            }
            return glistFiltered;
        }

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null)
                return;
            PhoneApplicationService.Current.State["objFromSelectUserPage"] = contact;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (xyz % 2 == 0) // this is done to avoid twice calling of "enterNameTxt_TextChanged" function
            {
                xyz++;
                return;
            }
            xyz++;

            if (!textChangedFromDelete)
            {
                if (indexOfAddedContacts.Count > 0)
                {
                    if (enterNameTxt.Text.Length > (indexOfAddedContacts.Peek() + 1))
                    {
                        charsEntered += enterNameTxt.Text.Substring(enterNameTxt.Text.Length - 1);
                    }
                }
                else
                {
                    charsEntered = enterNameTxt.Text;
                }
            }
            textChangedFromDelete = false;
            if (isGroupChat)
            {
                if (String.IsNullOrEmpty(enterNameTxt.Text))
                {
                    contactsListBox.ItemsSource = groupedList;
                    return;
                }
                enterNameTxt.Select(enterNameTxt.Text.Length, 0);
            }
            else
                charsEntered = enterNameTxt.Text.ToLower();

            if (String.IsNullOrEmpty(charsEntered))
            {
                contactsListBox.ItemsSource = groupedList;
                return;
            }
            List<Group<ContactInfo>> glistFiltered = getFilteredContactsFromNameOrPhone(charsEntered);
            contactsListBox.ItemsSource = glistFiltered;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (canGoBack)
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            enterNameTxt.BorderBrush = textBoxBorder;
        }

        #region REFRESH CONTACTS

        private void refreshContacts_Click(object sender, EventArgs e)
        {
            if (progress == null)
            {
                progress = new MyProgressIndicator();
            }

            progress.Show();
            canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
                Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());
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
                    if (!ContactUtils.areListsEqual(phList, hkList))
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
                    Thread.Sleep(1000);
                    progress.Hide();
                    canGoBack = true;
                    App.isABScanning = false;
                    return;
                }

                JArray ids_json = new JArray();
                foreach (string id in hike_contacts_by_id.Keys)
                {
                    ids_json.Add(id);
                }
                ContactUtils.contactsMap = contacts_to_update;
                ContactUtils.hike_contactsMap = hike_contacts_by_id;

                App.MqttManagerInstance.disconnectFromBroker(false);
                NetworkManager.turnOffNetworkManager = true;
                AccountUtils.updateAddressBook(contacts_to_update, ids_json, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
            }
            catch (Exception)
            {
            }
        }

        public class DelContacts
        {
            private string _id;
            private string _msisdn;

            public string Id
            {
                get
                {
                    return _id;
                }
            }
            public string Msisdn
            {
                get
                {
                    return _msisdn;
                }
            }
            public DelContacts(string id, string msisdn)
            {
                _id = id;
                _msisdn = msisdn;
            }
        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    progress.Hide();
                    canGoBack = true;
                });
                return;
            }

            List<ContactInfo> updatedContacts = AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap);
            List<DelContacts> hikeIds = new List<DelContacts>();
            foreach (string id in ContactUtils.hike_contactsMap.Keys)
            {
                DelContacts dCn = new DelContacts(id, ContactUtils.hike_contactsMap[id][0].Msisdn);
                hikeIds.Add(dCn);
            }

            if (hikeIds != null && hikeIds.Count > 0)
            {
                /* Delete ids from hike user DB */
                UsersTableUtils.deleteMultipleRows(hikeIds); // this will delete all rows in HikeUser DB that are not in Addressbook.
            }
            if (updatedContacts != null && updatedContacts.Count > 0)
            {
                UsersTableUtils.updateContacts(updatedContacts);
                ConversationTableUtils.updateConversation(updatedContacts);
            }

            List<ContactInfo> allContactsList = UsersTableUtils.getAllContacts();
            App.isABScanning = false;
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                groupedList = getGroupedList(allContactsList);
                contactsListBox.ItemsSource = groupedList;
                progress.Hide();
                canGoBack = true;
            });
        }

        #endregion

        #region GROUP CHAT RELATED

        private void startGroup_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT] = contactsForgroup;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true; // this is added to remove the back entry from the stack on chat thread page.

            if (PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS);
                PhoneApplicationService.Current.State[HikeConstants.IS_EXISTING_GROUP] = true;
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                NavigationService.GoBack();
            }
            else
            {
                string uri = "/View/ChatThread.xaml";
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
        }

        private void enterNameTxt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                typedTextDeleted = true;

                if (indexOfAddedContacts.Count > 0 && indexOfAddedContacts.Peek() == enterNameTxt.Text.Length)
                {
                    typedTextDeleted = false;
                    indexOfAddedContacts.Pop();
                    ContactInfo cn = contactsForgroup[contactsForgroup.Count - 1];
                    contactsForgroup.RemoveAt(contactsForgroup.Count - 1);
                    addBackDeletedContacts(cn);

                    stringBuilderForContactNames.Clear();
                    for (int i = 0; i < contactsForgroup.Count; i++)
                    {
                        stringBuilderForContactNames.Append(contactsForgroup[i].Name).Append("; ");
                    }
                    enterNameTxt.Text = stringBuilderForContactNames.ToString();
                    enterNameTxt.Select(enterNameTxt.Text.Length, 0);

                }
                if (typedTextDeleted)
                {
                    if (!String.IsNullOrEmpty(charsEntered))
                    {
                        charsEntered = charsEntered.Substring(0, charsEntered.Length - 1);
                        textChangedFromDelete = true;
                    }
                }
            }
        }
        private void addBackDeletedContacts(ContactInfo contact)
        {
            List<MsisdnCordinates> ml = msisdnPositions[contact.Msisdn];
            for (int j = 0; j < ml.Count; j++)
            {
                groupedList[ml[j].GroupIdx].Items.Add(ml[j].Contact);
                groupedList[ml[j].GroupIdx].Items.Sort(Utils.CompareByName<ContactInfo>);
            }
            contactsListBox.ItemsSource = null;
            contactsListBox.ItemsSource = groupedList;
        }

        private void contactSelectedForGroup_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;

            if (contact == null)
                return;
            if (contactsForgroup == null)
                contactsForgroup = new List<ContactInfo>();
            if (smsUserCount == maxSMSUsersAllowed || existingGroupUsers == maxUsersAllowed)
                return;

            contactsForgroup.Add(contact);

            if (!contact.OnHike)
                smsUserCount++;
            existingGroupUsers++;
            stringBuilderForContactNames.Clear();
            for (int i = 0; i < contactsForgroup.Count; i++)
            {
                stringBuilderForContactNames.Append(contactsForgroup[i].Name).Append("; ");
            }
            enterNameTxt.Text = stringBuilderForContactNames.ToString();
            enterNameTxt.Select(enterNameTxt.Text.Length, 0);

            enterNameTxt.Select(enterNameTxt.Text.Length, 0);
            indexOfAddedContacts.Push(enterNameTxt.Text.Length - 1);
            charsEntered = "";
            deleteContactFromGroupList(contact);
            if (existingGroupUsers >= 3)
                doneIconButton.IsEnabled = true;
        }

        private void deleteContactFromGroupList(ContactInfo contact)
        {
            List<MsisdnCordinates> ml = msisdnPositions[contact.Msisdn];
            for (int j = 0; j < ml.Count; j++)
            {
                groupedList[ml[j].GroupIdx].Items.Remove(ml[j].Contact);
            }
            contactsListBox.ItemsSource = null;
            contactsListBox.ItemsSource = groupedList;
        }

        #endregion
    }
}