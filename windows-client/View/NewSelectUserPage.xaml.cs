using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using windows_client.Misc;
using windows_client.Languages;
using windows_client.Controls;


namespace windows_client.View
{
    public partial class NewSelectUserPage : PhoneApplicationPage, HikePubSub.Listener
    {
        private bool frmBlockedList;
        private bool hideSmsContacts;
        private bool isFreeSmsOn = true;
        private bool canGoBack = true;
        private bool stopContactScanning = false;
        private bool isClicked = false;
        private bool isContactShared = false;
        private string TAP_MSG = AppResources.SelectUser_TapMsg_Txt;
        bool xyz = true; // this is used to avoid double calling of Text changed function in Textbox
        private bool isExistingGroup = false;
        private bool isGroupChat = false;
        public List<ContactInfo> contactsForgroup = null; // this is used to store all those contacts which are selected for group
        List<Group<ContactInfo>> glistFiltered = null;
        public List<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        public List<Group<ContactInfo>> filteredJumpList = null;
        private List<Group<ContactInfo>> defaultJumpList = null;
        private string charsEntered;
        ContactInfo contactInfoObj;
        private readonly int MAX_USERS_ALLOWED_IN_GROUP = 20;
        private int defaultGroupmembers = 0;
        private ProgressIndicatorControl progressIndicator;
        private StringBuilder stringBuilderForContactNames = new StringBuilder();

        List<ContactInfo> allContactsList = null; // contacts list

        private ApplicationBar appBar;
        private ApplicationBarIconButton doneIconButton = null;
        private ApplicationBarIconButton refreshIconButton = null;
        private ApplicationBarMenuItem onHikeFilter = null;

        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 

        Dictionary<string, List<Group<ContactInfo>>> groupListDictionary = new Dictionary<string, List<Group<ContactInfo>>>();
        HashSet<string> blockedSet = null;

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

        public class Group<T> :List<T>
        {
            public Group(string name, List<T> items)
            {
                this.Title = name;
            }

            public string Title
            {
                get;
                set;
            }
        }

        public NewSelectUserPage()
        {
            InitializeComponent();
            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out isFreeSmsOn);
            if (isFreeSmsOn)
                hideSmsContacts = true;
            else
                hideSmsContacts = false;
            object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.FORWARD_MSG, out obj) && obj is object[])
            {
                object[] attachmentForwardMessage = (object[])obj;
                if (attachmentForwardMessage.Length == 2 && attachmentForwardMessage[0] is MyChatBubble
                    && ((MyChatBubble)attachmentForwardMessage[0]).FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                {
                    hideSmsContacts = false;
                    isContactShared = true;
                }
            }
            //case when share contact is called
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARE_CONTACT))
            {
                isContactShared = true;
                hideSmsContacts = true;
                txtChat.Visibility = Visibility.Collapsed;
                txtTitle.Text = AppResources.ShareContact_Txt;
            }
            /* Case when this page is called from GroupInfo page*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                isGroupChat = true;
                TAP_MSG = AppResources.SelectUser_TapMsg_Grp_Txt;
            }

            /* Case when this page is called from create group button.*/
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.START_NEW_GROUP))
            {
                isGroupChat = (bool)PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP];
                TAP_MSG = AppResources.SelectUser_TapMsg_Grp_Txt;
            }

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_BLOCKED_LIST))
            {
                frmBlockedList = true;
                hideSmsContacts = true;
                blockedSet = new HashSet<string>();
                TAP_MSG = AppResources.Block_Tap_Txt;
            }

            if (isGroupChat)
                txtChat.Text = AppResources.GrpChat_Txt.ToUpper();

            if(frmBlockedList) //  this is to show block button
                contactsListBox.ItemTemplate = this.blockTemplate;
            else
                contactsListBox.ItemTemplate = this.defaultTemplate;

            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                if (frmBlockedList)
                {
                    List<Blocked> blkMsisdns = UsersTableUtils.getBlockList();
                    if (blkMsisdns != null)
                    {
                        foreach (Blocked c in blkMsisdns)
                            blockedSet.Add(c.Msisdn);
                    }
                }
                allContactsList = UsersTableUtils.getAllContactsByGroup();
                if(hideSmsContacts)
                    jumpList = getGroupedList(allContactsList);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {                
                if (!hideSmsContacts)
                {
                    if (filteredJumpList == null)
                        MakeFilteredJumpList();
                    contactsListBox.ItemsSource = filteredJumpList;
                }
                else
                    contactsListBox.ItemsSource = jumpList;
                shellProgress.IsVisible = false;
            };
            initPage();
            App.HikePubSubInstance.addListener(HikePubSub.GROUP_END, this);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Get a dictionary of query string keys and values.
            IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

            // Ensure that there is at least one key in the query string, and check 
            // whether the "FileId" key is present.
            if (queryStrings.ContainsKey("FileId"))
            {
                PhoneApplicationService.Current.State["SharePicker"] = queryStrings["FileId"];
                queryStrings.Clear();
                txtChat.Text = AppResources.Share_With_Txt;
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (!canGoBack)
            {
                MessageBoxResult mbox = MessageBox.Show(AppResources.Stop_Contact_Scanning, AppResources.Stop_Caption_txt, MessageBoxButton.OKCancel);
                if (mbox == MessageBoxResult.OK)
                {
                    stopContactScanning = true;
                    progressIndicator.Hide(LayoutRoot);
                    enableAppBar();
                    canGoBack = true;
                }
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.GROUP_END, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewSelectUserPage.xaml :: OnRemovedFromJournal, Exception : " + ex.StackTrace);
            }
            PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_BLOCKED_LIST);
            PhoneApplicationService.Current.State.Remove(HikeConstants.START_NEW_GROUP);
            PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS);
            PhoneApplicationService.Current.State.Remove(HikeConstants.SHARE_CONTACT);
            PhoneApplicationService.Current.State.Remove("Group_GroupId");
            base.OnRemovedFromJournal(e);
        }

        private void initPage()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            refreshIconButton = new ApplicationBarIconButton();
            refreshIconButton.IconUri = new Uri("/View/images/icon_refresh.png", UriKind.Relative);
            refreshIconButton.Text = AppResources.SelectUser_RefreshContacts_Txt;
            refreshIconButton.Click += new EventHandler(refreshContacts_Click);
            refreshIconButton.IsEnabled = true;
            appBar.Buttons.Add(refreshIconButton);

            if (!isContactShared)
            {
                onHikeFilter = new ApplicationBarMenuItem();
                if (isFreeSmsOn)
                    onHikeFilter.Text = AppResources.SelectUser_HideSmsContacts_Txt;
                else
                    onHikeFilter.Text = AppResources.SelectUser_ShowSmsContacts_Txt;
                onHikeFilter.Click += new EventHandler(OnHikeFilter_Click);
                appBar.MenuItems.Add(onHikeFilter);
            }
            selectUserPage.ApplicationBar = appBar;

            if (isGroupChat)
            {
                /* Add icons */
                if (doneIconButton != null)
                    return;
                doneIconButton = new ApplicationBarIconButton();
                doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
                doneIconButton.Text = AppResources.AppBar_Done_Btn;
                doneIconButton.Click += new EventHandler(startGroup_Click);
                doneIconButton.IsEnabled = false;
                appBar.Buttons.Add(doneIconButton);
                contactsListBox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(contactSelectedForGroup_Click);
                enterNameTxt.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(enterNameTxt_KeyDown), true);
                enterNameTxt.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(enterNameTxt_Tap);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARE_CONTACT))
            {
                contactsListBox.Tap += contactShared_Click;
            }
            else if (frmBlockedList)
            {
                contactsListBox.Tap += contactBlockUnblock_Click;
            }
            else
            {
                contactsListBox.Tap += contactSelected_Click;
            }
        }

        private void OnHikeFilter_Click(object sender, EventArgs e)
        {
            enterNameTxt.Text = stringBuilderForContactNames.ToString();
            if (hideSmsContacts)
            {
                if (filteredJumpList == null)
                {
                    MakeFilteredJumpList();
                }
                contactsListBox.ItemsSource = filteredJumpList;
                hideSmsContacts = !hideSmsContacts;
                onHikeFilter.Text = AppResources.SelectUser_ShowSmsContacts_Txt;
            }
            else
            {
                contactsListBox.ItemsSource = jumpList;
                hideSmsContacts = !hideSmsContacts;
                onHikeFilter.Text = AppResources.SelectUser_HideSmsContacts_Txt;
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
                    if (!GroupManager.Instance.getGroupParticipant(activeExistingGroupMembers[i].Name, activeExistingGroupMembers[i].Msisdn, activeExistingGroupMembers[i].GroupId).IsOnHike)
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

                // skip already blocked people
                else if (frmBlockedList && blockedSet.Contains(c.Msisdn))
                    continue;

                #region FREE SMS SETTINGS SUPPORT
                if (!isContactShared)
                {
                    if (isFreeSmsOn) // free sms is on 
                    {
                        if (!c.OnHike && !Utils.IsIndianNumber(c.Msisdn)) // if non hike non indian user
                        {
                            if (isGroupChat)
                                continue;
                            else
                                c.IsInvited = true;
                        }
                    }
                    else // free sms is off
                    {
                        if (!c.OnHike)
                        {
                            if (isGroupChat)
                                continue;
                            else
                                c.IsInvited = true;
                        }
                    }

                }
                #endregion

                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Add(c);
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

        private void MakeFilteredJumpList()
        {
            filteredJumpList = createGroups();
            for (int i = 0; i < jumpList.Count; i++)
            {
                Group<ContactInfo> g = jumpList[i];
                if (g == null || g.Count <= 0)
                    continue;
                for (int j = 0; j < g.Count; j++)
                {
                    ContactInfo c = g[j];
                    if (c.OnHike) // if on hike 
                        filteredJumpList[i].Add(c);
                }
            }
        }

        #endregion

        private void contactShared_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            contactInfoObj = contactsListBox.SelectedItem as ContactInfo;
            if (contactInfoObj == null)
                return;
            MessageBoxResult mr = MessageBox.Show(string.Format(AppResources.ShareContact_ConfirmationText, contactInfoObj.Name), AppResources.ShareContact_Txt, MessageBoxButton.OKCancel);
            if (mr == MessageBoxResult.OK)
            {
                string searchNumber = contactInfoObj.Msisdn;
                string country_code = null;
                if (App.appSettings.TryGetValue(App.COUNTRY_CODE_SETTING, out country_code))
                {
                    searchNumber = searchNumber.Replace(country_code, "");
                }
                ContactUtils.getContact(searchNumber, contactSearchCompleted_Callback);
            }
        }

        public void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            if (contactInfoObj == null)
                return;
            IEnumerable<Contact> contacts = e.Results;
            Contact contact = null;
            foreach (Contact c in contacts)
            {
                if (c.DisplayName == contactInfoObj.Name)
                {
                    contact = c;
                    break;
                }
            }

            if (contact == null)
            {
                MessageBox.Show(AppResources.SharedContactNotFoundText, AppResources.SharedContactNotFoundCaptionText, MessageBoxButton.OK);
            }
            else
            {
                PhoneApplicationService.Current.State[HikeConstants.CONTACT_SELECTED] = contact;
                NavigationService.GoBack();
            }
        }

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null || contact.Msisdn == AppResources.SelectUser_EnterValidNo_Txt || contact.Msisdn == App.MSISDN)
                return;

            if (contact.Msisdn.Equals(TAP_MSG)) // represents this is for unadded number
            {
                contact.Msisdn = Utils.NormalizeNumber(contact.Name);
                contact.Name = null;
                contact = GetContactIfExists(contact);
                if (App.ViewModel.ConvMap.ContainsKey(contact.Msisdn))
                    contact.OnHike = App.ViewModel.ConvMap[contact.Msisdn].IsOnhike;
            }

            if (contact.IsInvited) // if this is invite simply ignore
                return;

            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = contact;
            string uri = "/View/NewChatThread.xaml";
            try
            {
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewSelectUserPage.xaml :: contactSelected_Click, Exception : " + ex.StackTrace);
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
                if (!hideSmsContacts)
                {
                    if (filteredJumpList == null)
                        MakeFilteredJumpList();
                    contactsListBox.ItemsSource = filteredJumpList;
                }
                else
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
                if (gl[26].Count > 0 && gl[26][0].Msisdn != null)
                {
                    gl[26][0].Name = charsEntered;
                    if (charsEntered.Length >= 1 && charsEntered.Length <= 15)
                    {
                        if(frmBlockedList)
                        {
                            if(blockedSet.Contains(Utils.NormalizeNumber(gl[26][0].Name)))
                                gl[26][0].Msisdn = string.Format(TAP_MSG,AppResources.UnBlock_Txt.ToLower());
                            else
                                gl[26][0].Msisdn = string.Format(TAP_MSG, AppResources.Block_Txt.ToLower());
                        }
                        else
                            gl[26][0].Msisdn = TAP_MSG;
                    }
                    else
                    {
                        gl[26][0].Msisdn = AppResources.SelectUser_EnterValidNo_Txt;
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
            bool isPlus = false;
            if (Utils.IsNumber(charsEntered))
            {
                areCharsNumber = true;
                if (charsEntered.StartsWith("+"))
                {
                    isPlus = true;
                    charsEntered = charsEntered.Substring(1);
                }
            }
            List<Group<ContactInfo>> listToIterate = null;
            int charsLength = charsEntered.Length - 1;
            if (charsLength > 0)
            {
                if (groupListDictionary.ContainsKey(charsEntered.Substring(0, charsLength)))
                {
                    listToIterate = groupListDictionary[charsEntered.Substring(0, charsEntered.Length - 1)];
                    if (listToIterate == null)
                        listToIterate = jumpList;
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
            if (isContactShared)
                areCharsNumber = false;
            List<Group<ContactInfo>> list = null;
            if (areCharsNumber)
            {

                if (glistFiltered == null || createNewFilteredList)
                {
                    if (defaultJumpList == null)
                        defaultJumpList = createGroups();
                    list = defaultJumpList;
                    if (defaultJumpList[26].Count == 0)
                        defaultJumpList[26].Insert(0, defaultContact);
                }
                else
                {
                    list = glistFiltered;
                    list[26].Insert(0, defaultContact);
                }
                charsEntered = (isPlus ? "+" : "") + charsEntered;
                list[26][0].Name = charsEntered;
                if (Utils.IsNumberValid(charsEntered))
                {
                    if (frmBlockedList)
                    {
                        if (blockedSet.Contains(Utils.NormalizeNumber(charsEntered)))
                            list[26][0].Msisdn = string.Format(TAP_MSG, AppResources.UnBlock_Txt.ToLower());
                        else
                            list[26][0].Msisdn = string.Format(TAP_MSG, AppResources.Block_Txt.ToLower());
                        list[26][0].IsInvited = true; // this is done to hide block/unblock button
                    }
                    else
                        list[26][0].Msisdn = TAP_MSG;
                }
                else
                {
                    list[26][0].Msisdn = AppResources.SelectUser_EnterValidNo_Txt;
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

                Debug.WriteLine(Environment.OSVersion);
                int cursorPosition = enterNameTxt.SelectionStart;

                // this has to be done for WP8 device. therse is a problem in win phone os
                // for wp7 , cursorPosition = x ; for wp8 , cursorPosition = x + 1
                if (Utils.IsWP8)
                    cursorPosition = cursorPosition == 0 ? cursorPosition : cursorPosition - 1;

                if (cursorPosition <= 0 || cursorPosition >= stringBuilderForContactNames.Length)
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

            if (contact == null || contact.Msisdn == AppResources.SelectUser_EnterValidNo_Txt || contact.Msisdn == App.MSISDN)
                return;

            if (contact.Msisdn.Equals(TAP_MSG)) // represents this is for unadded number
            {
                contact.Msisdn = Utils.NormalizeNumber(contact.Name);
                contact = GetContactIfExists(contact);
            }

            //if (!contact.OnHike && smsUserCount == MAX_SMS_USRES_ALLOWED)
            //{
            //    MessageBoxResult result = MessageBox.Show("5 SMS users already selected", AppResources.SelectUser_CantAddUser_Txt, MessageBoxButton.OK);
            //    return;
            //}
            if (existingGroupUsers == MAX_USERS_ALLOWED_IN_GROUP)
            {
                MessageBoxResult result = MessageBox.Show(string.Format(AppResources.SelectUser_MaxUsersSelected_Txt, MAX_USERS_ALLOWED_IN_GROUP), AppResources.SelectUser_CantAddUser_Txt, MessageBoxButton.OK);
                return;
            }

            if (isNumberAlreadySelected(contact.Msisdn, contactsForgroup))
            {
                MessageBoxResult result = MessageBox.Show(string.Format(AppResources.SelectUser_UserAlreadyAdded_Txt, contact.Msisdn), AppResources.SelectUser_AlreadyAdded_Txt, MessageBoxButton.OK);
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
            App.AnalyticsInstance.addEvent(Analytics.REFRESH_CONTACTS);
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }
            disableAppBar();

            if (progressIndicator == null)
                progressIndicator = new ProgressIndicatorControl();

            progressIndicator.Show(LayoutRoot, AppResources.SelectUser_RefreshWaitMsg_Txt);

            canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        /* This callback is on background thread started by getContacts function */
        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            if (stopContactScanning)
            {
                stopContactScanning = false;
                return;
            }
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
            if (stopContactScanning)
            {
                stopContactScanning = false;
                return;
            }
            AccountUtils.updateAddressBook(contacts_to_update_or_add, ids_to_delete, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (stopContactScanning)
            {
                stopContactScanning = false;
                return;
            }
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                scanningComplete();
                canGoBack = true;
                return;
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap, true);
            List<ContactInfo.DelContacts> hikeIds = null;

            // Code to delete the removed contacts
            if (ContactUtils.hike_contactsMap != null && ContactUtils.hike_contactsMap.Count != 0)
            {
                hikeIds = new List<ContactInfo.DelContacts>(ContactUtils.hike_contactsMap.Count);
                // This loop deletes all those contacts which are removed.
                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, ContactUtils.hike_contactsMap[id][0].Msisdn);
                    hikeIds.Add(dCn);
                    if (App.ViewModel.ConvMap.ContainsKey(dCn.Msisdn)) // check convlist map to remove the 
                    {
                        try
                        {
                            // here we are removing name so that Msisdn will be shown instead of Name
                            App.ViewModel.ConvMap[dCn.Msisdn].ContactName = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("REFRESH CONTACTS :: Delete contact exception " + e.StackTrace);
                        }
                    }
                    else // if this contact is in favourite or pending and not in convMap update this also
                    {
                        ConversationListObject obj;
                        obj = App.ViewModel.GetFav(id);
                        if (obj == null) // this msisdn is not in favs , check in pending
                            obj = App.ViewModel.GetPending(id);
                        if (obj != null)
                            obj.ContactName = null;
                    }
                }
            }
            if (stopContactScanning)
            {
                stopContactScanning = false;
                return;
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

            allContactsList = UsersTableUtils.getAllContactsByGroup();
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                filteredJumpList = null;
                jumpList = getGroupedList(allContactsList);

                // this logic handles the case where hide sms contacts is there and user refreshed the list 
                if (!hideSmsContacts)
                {
                    MakeFilteredJumpList();
                    contactsListBox.ItemsSource = filteredJumpList;
                }
                else
                    contactsListBox.ItemsSource = jumpList;
                progressIndicator.Hide(LayoutRoot);
                enableAppBar();
            });
            canGoBack = true;
        }

        private void scanningComplete()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                progressIndicator.Hide(LayoutRoot);
                enableAppBar();
            });
        }

        #endregion

        private ContactInfo GetContactIfExists(ContactInfo contact)
        {
            if (glistFiltered == null)
                return contact;
            for (int i = 0; i < 26; i++)
            {
                if (glistFiltered[i] == null || glistFiltered[i] == null)
                    return contact;
                for (int k = 0; k < glistFiltered[i].Count; k++)
                {
                    if (glistFiltered[i][k].Msisdn == contact.Msisdn)
                        return glistFiltered[i][k];
                }
            }
            // if not found
            //contact.Name = contact.Msisdn;
            return contact;
        }

        private void disableAppBar()
        {
            appBar.IsMenuEnabled = false;
            refreshIconButton.IsEnabled = false;
            if (isGroupChat)
                doneIconButton.IsEnabled = false;
        }

        private void enableAppBar()
        {
            refreshIconButton.IsEnabled = true;
            appBar.IsMenuEnabled = true;
            // should be Group Chat
            // if new group then number of users should be greater than equal to 3 
            // if existing group then added user should atleast be 1
            if (isGroupChat && ((!isExistingGroup && existingGroupUsers >= 3) || (isExistingGroup && (existingGroupUsers - defaultGroupmembers > 0))))
            {
                doneIconButton.IsEnabled = true;
            }
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            enterNameTxt.BorderBrush = UI_Utils.Instance.Black;
        }

        /// <summary>
        /// This function is for GC. This is called when each contact is tapped, so that it can shown in text box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                    MessageBoxResult result = MessageBox.Show(string.Format(AppResources.SelectUser_ContactRemoved_Txt, contactsForgroup[k].Name, contactsForgroup[k].Msisdn), AppResources.SelectUser_RemoveContact_Txt, MessageBoxButton.OKCancel);
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

        private void contactsListBox_ScrollingStarted(object sender, EventArgs e)
        {
            contactsListBox.Focus();
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.GROUP_END == type)
            {
                string gId = (string)obj;
                object gIdSaved = null;
                PhoneApplicationService.Current.State.TryGetValue("Group_GroupId", out gIdSaved);
                if (gIdSaved == null)
                    return;
                if (gId == gIdSaved.ToString())
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS);
                        PhoneApplicationService.Current.State.Remove("Group_GroupId");
                        NavigationService.RemoveBackEntry();
                        NavigationService.GoBack();
                    });
                }
            }
        }

        private void Block_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            ContactInfo ci = btn.DataContext as ContactInfo;
            if (ci == null)
                return;
            
            if (btn.Content.Equals(AppResources.Block_Txt)) // block request
            {
                btn.Content = AppResources.UnBlock_Txt;
                App.HikePubSubInstance.publish(HikePubSub.BLOCK_USER, ci);
            }
            else // unblock request
            {
                btn.Content = AppResources.Block_Txt;
                App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, ci);
            }           
        }

        private void contactBlockUnblock_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null || contact.Msisdn == AppResources.SelectUser_EnterValidNo_Txt || contact.Msisdn == App.MSISDN)
                return;
            if (!contact.IsInvited) // this is handled by block unblock button
                return;
            string tapStr = contact.Msisdn;
            ContactInfo c = new ContactInfo(contact);
            c.Msisdn = Utils.NormalizeNumber(contact.Name);
            c.Name = c.Msisdn;

            if (tapStr.Contains(AppResources.UnBlock_Txt.ToLower())) // unblock request
            {
                contact.Msisdn = string.Format(TAP_MSG, AppResources.Block_Txt.ToLower());
                App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, c);
            }
            else // block request
            {
                contact.Msisdn = string.Format(TAP_MSG,AppResources.UnBlock_Txt.ToLower());
                App.HikePubSubInstance.publish(HikePubSub.BLOCK_USER, c);
            }
        }

        private void Invite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.IsEnabled)
                return;
            btn.Content = AppResources.Invited;
            ContactInfo ci = btn.DataContext as ContactInfo;
            if (ci == null)
                return;
            long time = TimeUtils.getCurrentTimeStamp();
            string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ConvMessage convMessage = new ConvMessage(string.Format(AppResources.sms_invite_message, inviteToken), ci.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = true;
            convMessage.IsInvite = true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(convMessage.IsSms ? false : true));
            btn.IsEnabled = false;
        }


    }
}