using System;
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
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Net.NetworkInformation;


namespace windows_client.View
{
    public partial class NewSelectUserPage : PhoneApplicationPage
    {
        bool canGoBack = true;
        private bool isClicked = false;
        private string TAP_MSG = "Tap here to message this person";
        bool xyz = true; // this is used to avoid double calling of Text changed function in Textbox
        private bool isExistingGroup = false;
        private bool isGroupChat = false;
        public List<ContactInfo> contactsForgroup = null; // this is used to store all those contacts which are selected for group
        public MyProgressIndicator progress = null;
        List<Group<ContactInfo>> glistFiltered = null;
        public List<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        private List<Group<ContactInfo>> defaultJumpList = null;
        private string charsEntered;

        private readonly int MAX_SMS_USRES_ALLOWED = 5;
        private readonly int MAX_USERS_ALLOWED_IN_GROUP = 10;
        private int defaultGroupmembers = 0;

        private StringBuilder stringBuilderForContactNames = new StringBuilder();

        List<ContactInfo> allContactsList = null; // contacts list

        private ApplicationBar appBar;
        private ApplicationBarIconButton doneIconButton = null;
        ApplicationBarIconButton refreshIconButton = null;

        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 

        Dictionary<string, List<Group<ContactInfo>>> groupListDictionary = new Dictionary<string, List<Group<ContactInfo>>>();

        private int smsUserCount = 0;
        private int existingGroupUsers = 1; // 1 because owner of the group is already included

        public int ExistingGroupUsers
        {
            get
            {
                return existingGroupUsers;
            }
            set
            {
                if (value != existingGroupUsers)
                {
                    existingGroupUsers = value;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!isExistingGroup) // case if group is new
                        {
                            if (existingGroupUsers >= 3)
                            {
                                if (!doneIconButton.IsEnabled)
                                    doneIconButton.IsEnabled = true;
                            }
                            else
                            {
                                if (doneIconButton.IsEnabled)
                                    doneIconButton.IsEnabled = false;
                            }
                        }
                        else
                        {
                            if (existingGroupUsers - defaultGroupmembers > 0)
                            {
                                if (!doneIconButton.IsEnabled)
                                    doneIconButton.IsEnabled = true;
                            }
                            else
                            {
                                if (doneIconButton.IsEnabled)
                                    doneIconButton.IsEnabled = false;
                            }
                        }
                    });
                }
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

        public NewSelectUserPage()
        {
            InitializeComponent();
            /* Case whe this page is called from GroupInfo page*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
                isGroupChat = true;

            /* Case when this page is called from create group button.*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.START_NEW_GROUP))
                isGroupChat = (bool)PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP];

            //if (isGroupChat)
            //    title.Text = "new group chat";
            progressBar.Opacity = 1;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                allContactsList = UsersTableUtils.getAllContactsByGroup();
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {
                jumpList = getGroupedList(allContactsList);
                contactsListBox.ItemsSource = jumpList;
                progressBar.Opacity = 0;
            };
            initPage();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (!canGoBack)
                e.Cancel = true;
            base.OnBackKeyPress(e);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.START_NEW_GROUP);
            PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS);
        }

        private void initPage()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            refreshIconButton = new ApplicationBarIconButton();
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
                enterNameTxt.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(enterNameTxt_Tap);
            }
            else
            {
                contactsListBox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(contactSelected_Click);
            }
        }

        #region  MAKE JUMP LIST

        private List<Group<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            List<GroupParticipant> activeExistingGroupMembers = null;

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                isExistingGroup = true;
                activeExistingGroupMembers = PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] as List<GroupParticipant>;

                //TODO start this loop from end, after sorting is done on onHike status
                smsUserCount = 0;
                for (int i = 0; i < activeExistingGroupMembers.Count; i++)
                {
                    if (!Utils.getGroupParticipant(activeExistingGroupMembers[i].Name, activeExistingGroupMembers[i].Msisdn, activeExistingGroupMembers[i].GroupId).IsOnHike)
                    {
                        smsUserCount++;
                    }
                    existingGroupUsers++;
                }
                defaultGroupmembers = ExistingGroupUsers;
            }

            List<Group<ContactInfo>> glist = createGroups();
            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
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
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Items.Add(c);
            }
            return glist;
        }

        private bool msisdnAlreadyExists(string msisdn, List<GroupParticipant> activeExistingGroupMembers)
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
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            List<Group<ContactInfo>> glist = new List<Group<ContactInfo>>(27);
            foreach (char c in Groups)
            {
                Group<ContactInfo> g = new Group<ContactInfo>(c.ToString(), new List<ContactInfo>(1));
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

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null)
                return;
            if (contact.Msisdn == "Enter Valid Number")
                return;
            if (contact.Msisdn.Equals(TAP_MSG)) // represents this is for unadded number
            {
                contact.Msisdn = normalizeNumber(contact.Name);
                contact.Name = null;
                contact = GetContactIfExists(contact);
                if (ConversationsList.ConvMap.ContainsKey(contact.Msisdn))
                    contact.OnHike = ConversationsList.ConvMap[contact.Msisdn].IsOnhike;
            }
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = contact;
            string uri = "/View/NewChatThread.xaml";
            try
            {
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
            catch (Exception ex)
            {
            }
        }

        /*
         * Simplistic normalization function.
         * TODO: Improve it more later
         */

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (xyz) // this is done to avoid twice calling of "enterNameTxt_TextChanged" function
            {
                xyz = !xyz;
                return;
            }
            xyz = !xyz;

            if (isGroupChat)
                charsEntered = enterNameTxt.Text.Substring(stringBuilderForContactNames.Length);
            else
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
                List<Group<ContactInfo>> gl = groupListDictionary[charsEntered];
                if (gl == null)
                {
                    groupListDictionary.Remove(charsEntered);
                    contactsListBox.ItemsSource = null;
                    return;
                }
                if (gl[26].Items.Count > 0 && gl[26].Items[0].Msisdn != null)
                {
                    gl[26].Items[0].Name = charsEntered;
                    if (charsEntered.Length >= 10 && charsEntered.Length <= 13)
                    {
                        gl[26].Items[0].Msisdn = TAP_MSG;
                    }
                    else
                    {
                        gl[26].Items[0].Msisdn = "Enter Valid Number";
                    }
                }
                contactsListBox.ItemsSource = gl;
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

        private List<Group<ContactInfo>> getFilteredContactsFromNameOrPhoneAsync(string charsEntered, int start, int end)
        {
            bool areCharsNumber = false;
            if (isNumber(charsEntered))
            {
                areCharsNumber = true;
                if (charsEntered.StartsWith("+"))
                    charsEntered = charsEntered.Substring(1);
            }
            List<Group<ContactInfo>> listToIterate = null;
            int charsLength = charsEntered.Length - 1;
            if (charsLength > 0)
            {
                if (groupListDictionary.ContainsKey(charsEntered.Substring(0, charsLength)))
                    listToIterate = groupListDictionary[charsEntered.Substring(0, charsEntered.Length - 1)];
                else
                    listToIterate = jumpList;
            }
            else
                listToIterate = jumpList;

            bool createNewFilteredList = true;
            for (int i = start; i < end; i++)
            {
                for (int j = 0; j < (listToIterate[i].Items == null ? 0 : listToIterate[i].Items.Count); j++)
                {
                    ContactInfo cn = listToIterate[i].Items[j];
                    if (cn.Name.ToLower().Contains(charsEntered) || cn.Msisdn.Contains(charsEntered) || cn.PhoneNo.Contains(charsEntered))
                    {
                        if (createNewFilteredList)
                        {
                            createNewFilteredList = false;
                            glistFiltered = createGroups();
                        }
                        glistFiltered[i].Items.Add(cn);
                    }
                }
            }
            List<Group<ContactInfo>> list = null;
            if (areCharsNumber)
            {
                
                if (glistFiltered == null || createNewFilteredList)
                {
                    if (defaultJumpList == null)
                        defaultJumpList = createGroups();
                    list = defaultJumpList;
                    if (defaultJumpList[26].Items.Count == 0)
                        defaultJumpList[26].Items.Insert(0, defaultContact);
                }
                else
                {
                    list = glistFiltered;
                    list[26].Items.Insert(0, defaultContact);
                }

                list[26].Items[0].Name = charsEntered;
                if (charsEntered.Length >= 10 && charsEntered.Length <= 13)
                {
                    list[26].Items[0].Msisdn = TAP_MSG;
                }
                else
                {
                    list[26].Items[0].Msisdn = "Enter Valid Number";
                }

            }
            if (!areCharsNumber && createNewFilteredList)
                return null;
            if (areCharsNumber)
                return list;
            return glistFiltered;
        }

        #region GROUP CHAT RELATED

        private void startGroup_Click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            isClicked = true;
            PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT] = contactsForgroup;

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                PhoneApplicationService.Current.State[HikeConstants.IS_EXISTING_GROUP] = true;
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry(); // will remove groupinfo page
                NavigationService.GoBack();
            }
            else
            {
                string uri = "/View/NewChatThread.xaml";
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
        }

        /*
         * This function will be called only in case of Group Chat
         */
        private void enterNameTxt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                int cursorPosition = enterNameTxt.SelectionStart;
                if (cursorPosition == 0 || cursorPosition >= stringBuilderForContactNames.Length)
                    return;

                ContactInfo cn = contactsForgroup[contactsForgroup.Count - 1];
                contactsForgroup.RemoveAt(contactsForgroup.Count - 1);
                Debug.WriteLine("Contacts selected : {0}, char count = {1}", stringBuilderForContactNames.ToString(), stringBuilderForContactNames.Length);
                stringBuilderForContactNames.Remove(stringBuilderForContactNames.Length - (cn.Name.Length + 2), (cn.Name.Length + 2));
                Debug.WriteLine("Contacts selected : {0}, char count = {1}", stringBuilderForContactNames.ToString(), stringBuilderForContactNames.Length);
                enterNameTxt.Text = stringBuilderForContactNames.ToString();
                enterNameTxt.Select(enterNameTxt.Text.Length, 0);
                // update user count
                if (!cn.OnHike)
                    smsUserCount--;
                ExistingGroupUsers--;
            }
        }

        private void contactSelectedForGroup_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;

            if (contact == null || contact.Msisdn == "Enter Valid Number")
                return;

            if (contact.Msisdn.Equals(TAP_MSG)) // represents this is for unadded number
            {
                contact.Msisdn = normalizeNumber(contact.Name);
                contact = GetContactIfExists(contact);
            }

            if (!contact.OnHike && smsUserCount == MAX_SMS_USRES_ALLOWED)
            {
                MessageBoxResult result = MessageBox.Show("5 SMS users already selected", "Cannot add user !!", MessageBoxButton.OK);
                return;
            }
            if (existingGroupUsers == MAX_USERS_ALLOWED_IN_GROUP)
            {
                MessageBoxResult result = MessageBox.Show("10 users already selected", "Cannot add user !!", MessageBoxButton.OK);
                return;
            }

            if (isNumberAlreadySelected(contact.Msisdn, contactsForgroup))
            {
                MessageBoxResult result = MessageBox.Show(contact.Msisdn + " is already added to group.", "User already added !!", MessageBoxButton.OK);
                return;
            }

            if (contactsForgroup == null)
                contactsForgroup = new List<ContactInfo>();

            // new object is created for every numbered contacts i.e contact not in your list and you have added him by entering number
            ContactInfo contactToAdd = new ContactInfo(contact);
            contactsForgroup.Add(contactToAdd);
            stringBuilderForContactNames.Append(contactToAdd.Name).Append("; ");
            enterNameTxt.Text = stringBuilderForContactNames.ToString();
            enterNameTxt.Select(enterNameTxt.Text.Length, 0);

            if (!contact.OnHike)
                smsUserCount++;
            ExistingGroupUsers++;

            charsEntered = "";
        }

        private bool isNumberAlreadySelected(string msisdn, List<ContactInfo> l)
        {
            for (int i = 0; i < (l != null ? l.Count : 0); i++)
            {
                if (l[i].Msisdn == msisdn)
                    return true;
            }
            // if add to existing group then check number already added or not
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                List<GroupParticipant> list = PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] as List<GroupParticipant>;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Msisdn == msisdn)
                        return true;
                }
            }
            return false;
        }

        #endregion

        #region REFRESH CONTACTS

        private void refreshContacts_Click(object sender, EventArgs e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show("Connection Problem. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                return;
            }
            if (progress == null)
                progress = new MyProgressIndicator("This may take a minute or two...");

            disableAppBar();
            progress.Show();
            canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        /* This callback is on background thread started by getContacts function */
        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            /* If no contacts in Phone as well as App , simply return */
            if ((new_contacts_by_id == null || new_contacts_by_id.Count == 0) && hike_contacts_by_id == null)
            {
                scanningComplete();
                canGoBack = true;
                return;
            }

            Dictionary<string, List<ContactInfo>> contacts_to_update_or_add = new Dictionary<string, List<ContactInfo>>();

            if (new_contacts_by_id != null) // if there are contacts in phone perform this step
            {
                foreach (string id in new_contacts_by_id.Keys)
                {
                    List<ContactInfo> phList = new_contacts_by_id[id];
                    if (hike_contacts_by_id == null || !hike_contacts_by_id.ContainsKey(id))
                    {
                        contacts_to_update_or_add.Add(id, phList);
                        continue;
                    }

                    List<ContactInfo> hkList = hike_contacts_by_id[id];
                    if (!ContactUtils.areListsEqual(phList, hkList))
                    {
                        contacts_to_update_or_add.Add(id, phList);
                    }
                    hike_contacts_by_id.Remove(id);
                }
                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
            }

            /* If nothing is changed simply return without sending update request*/
            if (contacts_to_update_or_add.Count == 0 && (hike_contacts_by_id == null || hike_contacts_by_id.Count == 0))
            {
                Thread.Sleep(1000);
                scanningComplete();
                canGoBack = true;
                return;
            }

            JArray ids_to_delete = new JArray();
            if (hike_contacts_by_id != null)
            {
                foreach (string id in hike_contacts_by_id.Keys)
                {
                    ids_to_delete.Add(id);
                }
            }

            ContactUtils.contactsMap = contacts_to_update_or_add;
            ContactUtils.hike_contactsMap = hike_contacts_by_id;

            App.MqttManagerInstance.disconnectFromBroker(false);
            NetworkManager.turnOffNetworkManager = true;

            /*
             * contacts_to_update : These are the contacts to add
             * ids_json : These are the contacts to delete
             */

            AccountUtils.updateAddressBook(contacts_to_update_or_add, ids_to_delete, new AccountUtils.postResponseFunction(updateAddressBook_Callback));

        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                scanningComplete();
                canGoBack = true;
                return;
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap);
            List<ContactInfo.DelContacts> hikeIds = null;
            if (ContactUtils.hike_contactsMap != null && ContactUtils.hike_contactsMap.Count != 0)
            {
                hikeIds = new List<ContactInfo.DelContacts>(ContactUtils.hike_contactsMap.Count);
                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, ContactUtils.hike_contactsMap[id][0].Msisdn);
                    hikeIds.Add(dCn);
                }
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

            List<ContactInfo> allContactsList = UsersTableUtils.getAllContactsByGroup();
            App.isABScanning = false;
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                jumpList = getGroupedList(allContactsList);
                contactsListBox.ItemsSource = jumpList;
                progress.Hide();
                enableAppBar();
            });
            canGoBack = true;
        }

        private void scanningComplete()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                progress.Hide();
                enableAppBar();
                App.isABScanning = false;
            });
        }

        #endregion

        private string normalizeNumber(string msisdn)
        {
            switch (msisdn.Length)
            {
                case 10: return ("+91" + msisdn);
                case 11: return ("+91" + msisdn.Substring(1));
                case 12: return ("+" + msisdn);
                case 13: return msisdn;
                default: return msisdn;
            }
        }

        private bool isNumber(string charsEntered)
        {
            if (charsEntered.StartsWith("+")) // as in +91981 etc etc
            {
                charsEntered = charsEntered.Substring(1);
            }
            long i = 0;
            return long.TryParse(charsEntered, out i);
        }

        private ContactInfo GetContactIfExists(ContactInfo contact)
        {
            if (glistFiltered == null)
                return contact;
            for (int i = 0; i < 26; i++)
            {
                if (glistFiltered[i] == null || glistFiltered[i].Items == null)
                    return contact;
                for (int k = 0; k < glistFiltered[i].Items.Count; k++)
                {
                    if (glistFiltered[i].Items[k].Msisdn == contact.Msisdn)
                        return glistFiltered[i].Items[k];
                }
            }
            return contact;
        }

        private void disableAppBar()
        {
            refreshIconButton.IsEnabled = false;
            if (isGroupChat)
                doneIconButton.IsEnabled = false;
        }

        private void enableAppBar()
        {
            refreshIconButton.IsEnabled = true;
            if (isGroupChat && existingGroupUsers >= 3)
                doneIconButton.IsEnabled = true;
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            enterNameTxt.BorderBrush = UI_Utils.Instance.Black;
        }

        private void enterNameTxt_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isGroupChat) // logic is valid only for Group Chat
                return;

            int nameLength = 0;
            int cursorPosition = enterNameTxt.SelectionStart;

            if (stringBuilderForContactNames.Length <= cursorPosition) // if textbox is tapped @ last position simply return
                return;

            for (int k = 0; k < contactsForgroup.Count; k++)
            {
                nameLength += contactsForgroup[k].Name.Length + 2; // length of name + "; " i.e 2
                if (cursorPosition < nameLength)
                {
                    MessageBoxResult result = MessageBox.Show(contactsForgroup[k].Name + "[" + contactsForgroup[k].Msisdn + "]" + " will be removed from group.", "Remove Contact ?", MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                    {
                        enterNameTxt.Select(enterNameTxt.Text.Length, 0);
                        return;
                    }
                    ExistingGroupUsers--;
                    if (!contactsForgroup[k].OnHike)
                        smsUserCount--;
                    contactsForgroup.RemoveAt(k);
                    stringBuilderForContactNames.Clear();
                    for (int i = 0; i < contactsForgroup.Count; i++)
                        stringBuilderForContactNames.Append(contactsForgroup[i].Name).Append("; ");
                    enterNameTxt.Text = stringBuilderForContactNames.ToString() + charsEntered;
                    enterNameTxt.Select(enterNameTxt.Text.Length, 0);
                    return;
                }

            }
        }

    }
}