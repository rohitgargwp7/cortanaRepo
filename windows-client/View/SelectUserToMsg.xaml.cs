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


namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        public List<string> groupMsisdns = null;
        public List<string> groupNames = null;
        public MyProgressIndicator progress = null;
        public bool canGoBack = true;
        public List<Group<ContactInfo>> groupedList = null;
        private readonly SolidColorBrush textBoxBorder = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
        private string charsEntered;
        private bool typedTextDeleted = true;
        private readonly int maxSMSUsersAllowed = 5;
        private readonly int maxUsersAllowed = 10;
        private int smsUserCount = 0;
        private ApplicationBar appBar;

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

            public IList<T> Items
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
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_LoadAllContacts);
            bw.RunWorkerAsync();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            ApplicationBarIconButton composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/icon_refresh.png", UriKind.Relative);
            composeIconButton.Text = "Refresh Contacts";
            composeIconButton.Click += new EventHandler(refreshContacts_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);
            selectUserPage.ApplicationBar = appBar;

            enterNameTxt.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(enterNameTxt_KeyDown), true);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString.ContainsKey("param"))
            {
                /* Add icons */
                ApplicationBarIconButton doneIconButton = new ApplicationBarIconButton();
                doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
                doneIconButton.Text = "Done";
                doneIconButton.Click += new EventHandler(startGroup_Click);
                doneIconButton.IsEnabled = true;
                appBar.Buttons.Add(doneIconButton);
                contactsListBox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(contactSelectedForGroup_Click);
            }
        }

        private void startGroup_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State["groupChat"] = true;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true;
            PhoneApplicationService.Current.State["groupMsidns"] = groupMsisdns;
            PhoneApplicationService.Current.State["groupNames"] = groupNames;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

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

            List<Group<ContactInfo>> glist = createGroups();
            for (int i = 0; i < allContactsList.Count; i++)
            {
                ContactInfo c = allContactsList[i];
                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 0 : ch[0] - 'a' + 1;
                // and add the entry
                glist[index].Items.Add(c);
            }
            return glist;
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

        private void enterNameTxt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            int c = e.PlatformKeyCode;
            if (Char.IsLetterOrDigit((char)c))
            {
                charsEntered += Char.ToLower((char)c);
            }
            else if (e.Key == Key.Back)
            {
                typedTextDeleted = true;
                int indexOfLastColon = enterNameTxt.Text.LastIndexOf(';');
                if (indexOfLastColon == enterNameTxt.Text.Length - 1)
                {
                    typedTextDeleted = false;
                    int indexOfLastColonSpace = enterNameTxt.Text.LastIndexOf("; ");
                    if (indexOfLastColonSpace == -1)
                    {
                        enterNameTxt.Text = "";
                        return;
                    }
                    groupMsisdns.RemoveAt(groupMsisdns.Count - 1);
                    groupNames.RemoveAt(groupNames.Count - 1);
                    enterNameTxt.Text = enterNameTxt.Text.Substring(0, indexOfLastColonSpace + 2);
                    enterNameTxt.Select(indexOfLastColonSpace + 2, 0);
                }
                if (typedTextDeleted)
                    charsEntered = charsEntered.Substring(0, charsEntered.Length - 1);
            }
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
//            enterNameTxt.Focus();
            if (String.IsNullOrEmpty(enterNameTxt.Text))
            {
                return;
            }
            enterNameTxt.Select(enterNameTxt.Text.Length, 0);
            if (String.IsNullOrEmpty(charsEntered))
            {
                contactsListBox.ItemsSource = groupedList;
                return;
            }
            List<Group<ContactInfo>> glistFiltered = getFilteredContactsFromNameOrPhone(charsEntered);
            contactsListBox.ItemsSource = glistFiltered;
        }

        private void contactSelectedForGroup_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            charsEntered = "";
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;

            if (contact == null)
                return;
            if (groupMsisdns == null)
                groupMsisdns = new List<string>();
            if (smsUserCount == maxSMSUsersAllowed || groupMsisdns.Count == maxUsersAllowed)
                return;

            groupMsisdns.Add(contact.Msisdn);

            if (groupNames == null)
                groupNames = new List<string>();
            groupNames.Add(contact.Name);

            if (!contact.OnHike)
                smsUserCount++;
            string contactNameTemp = "";
            if (!String.IsNullOrEmpty(enterNameTxt.Text))
                contactNameTemp = enterNameTxt.Text.Substring(0, enterNameTxt.Text.LastIndexOf("; ") + 2);
            contactNameTemp += contact.Name + "; ";
            enterNameTxt.Text = contactNameTemp;
            enterNameTxt.Select(enterNameTxt.Text.Length, 0);
            charsEntered = "";
        }

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
    }
}