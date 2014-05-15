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
using windows_client.Languages;
using windows_client.utils;
using windows_client.DbUtils;
using windows_client.Misc;
using windows_client.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading;
using Microsoft.Phone.UserData;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using windows_client.ViewModel;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Text;

namespace windows_client.View
{
    public partial class ForwardTo : PhoneApplicationPage, HikePubSub.Listener
    {
        private readonly int MAX_USERS_ALLOWED_IN_GROUP = 50;

        private bool _canGoBack = true;
        private bool _showSmsContacts;
        private bool _isFreeSmsOn = true;
        private bool _showExistingGroups;
        private StringBuilder stringBuilderForContactNames = new StringBuilder();
        private bool _stopContactScanning = false;
        private bool _isContactShared = false;
        private bool _flag;

        private bool _isExistingGroup = false;
        private bool _isGroupChat = false;
        int _smsUserCount = 0;
        private int _smsCredits;
        private string _charsEntered;

        ObservableCollection<ContactGroup<ContactInfo>> _glistFiltered = null;
        ObservableCollection<ContactGroup<ContactInfo>> _completeGroupedContactList = null; // list that will contain the complete jump list

        ObservableCollection<ContactInfo> SelectedContacts = new ObservableCollection<ContactInfo>(); // this is used to store all those contacts which are selected for forwarding message

        List<ContactInfo> _allContactsList = null; // contacts list
        ContactGroup<ContactInfo> _smsContactsGroup; // sms contacts list
        ContactGroup<ContactInfo> _emptySMSGroup = new ContactGroup<ContactInfo>(AppResources.NewComposeGroup_SMSContacts, AppResources.NewComposeGroup_1SMSContact); // empty sms contacts list

        private ProgressIndicatorControl progressIndicator;

        private ApplicationBarIconButton _doneIconButton = null;
        private ApplicationBarIconButton _refreshIconButton = null;
        private ApplicationBarMenuItem _onHikeFilterMenuItem = null;

        Dictionary<string, ObservableCollection<ContactGroup<ContactInfo>>> groupListDictionary = new Dictionary<string, ObservableCollection<ContactGroup<ContactInfo>>>();

        /// <summary>
        /// maintain state dictionary for showSMScontacts in parallel to groupListDictionary
        /// so that while searching the value of showsmscontacts is considered too
        /// </summary>
        Dictionary<string, bool> groupListStateDictionary = new Dictionary<string, bool>();

        Dictionary<string, string> groupInfoDictionary = new Dictionary<string, string>();

        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 

        string _pageTitle = AppResources.Chat_With_Txt;

        public ForwardTo()
        {
            InitializeComponent();

            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out _isFreeSmsOn);
            _showSmsContacts = _isFreeSmsOn ? true : false;

            App.appSettings.TryGetValue(App.SMS_SETTING, out _smsCredits);

            object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.FORWARD_MSG, out obj))
            {
                _showExistingGroups = true;
                _pageTitle = AppResources.SelectUser_Forward_To_Txt;

                if (obj is object[])
                {
                    object[] attachmentForwardMessage = (object[])obj;
                    if (attachmentForwardMessage.Length == 6
                        && ((string)attachmentForwardMessage[0]).Contains(HikeConstants.CONTACT))
                    {
                        _showSmsContacts = false;
                        _isContactShared = true;
                    }
                }
            }
            /* Case when this page is called from GroupInfo page*/
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                _isGroupChat = true;
                _pageTitle = AppResources.SelectUser_Title_AddParticipant_Txt;
            }
            /* Case when this page is called from create group button.*/
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.START_NEW_GROUP))
            {
                _isGroupChat = (bool)PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP];
                _pageTitle = AppResources.GrpChat_Txt;
            }

            if(_isGroupChat || _isForward)
                enterNameTxt.AddHandler(TextBox.KeyDownEvent, new KeyEventHandler(enterNameTxt_KeyDown), true);

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        shellProgress.IsIndeterminate = true;
                    });

                _allContactsList = UsersTableUtils.getAllContactsByGroup();
                _completeGroupedContactList = GetGroupedList(_allContactsList);
            };

            bw.RunWorkerAsync();

            bw.RunWorkerCompleted += (s, e) =>
            {
                if (!_showSmsContacts)
                    _completeGroupedContactList[4] = _emptySMSGroup;

                contactsListBox.ItemsSource = _completeGroupedContactList;
                shellProgress.IsIndeterminate = false;
            };

            initPage();

            PageTitle.Text = _pageTitle;
        }

        #region AppBar

        private void initPage()
        {
            ApplicationBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            _refreshIconButton = new ApplicationBarIconButton();
            _refreshIconButton.IconUri = new Uri("/View/images/AppBar/icon_refresh.png", UriKind.Relative);
            _refreshIconButton.Text = AppResources.SelectUser_RefreshContacts_Txt;
            _refreshIconButton.Click += new EventHandler(refreshContacts_Click);
            _refreshIconButton.IsEnabled = true;
            ApplicationBar.Buttons.Add(_refreshIconButton);

            if (!_isContactShared && _isFreeSmsOn)
            {
                _onHikeFilterMenuItem = new ApplicationBarMenuItem();
                _onHikeFilterMenuItem.Text = _showSmsContacts ? AppResources.SelectUser_HideSmsContacts_Txt : AppResources.SelectUser_ShowSmsContacts_Txt;
                _onHikeFilterMenuItem.Click += OnHikeFilter_Click;
                ApplicationBar.MenuItems.Add(_onHikeFilterMenuItem);
            }

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
            {
                _isForward = true;

                if (_doneIconButton != null)
                    return;
                _doneIconButton = new ApplicationBarIconButton();
                _doneIconButton.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.Relative);
                _doneIconButton.Text = AppResources.AppBar_Done_Btn;
                _doneIconButton.Click += forwardTo_Click;
                _doneIconButton.IsEnabled = false;
                ApplicationBar.Buttons.Add(_doneIconButton);
            }

            if (_isGroupChat)
            {
                /* Add icons */
                if (_doneIconButton != null)
                    return;
                _doneIconButton = new ApplicationBarIconButton();
                _doneIconButton.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.Relative);
                _doneIconButton.Text = AppResources.AppBar_Done_Btn;
                _doneIconButton.Click += startGroup_Click;
                _doneIconButton.IsEnabled = false;
                ApplicationBar.Buttons.Add(_doneIconButton);
            }
        }

        private void startGroup_Click(object sender, EventArgs e)
        {
            if (_isClicked)
                return;

            _isClicked = true;

            PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT] = SelectedContacts.ToList();

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

        private void OnHikeFilter_Click(object sender, EventArgs e)
        {
            if (_isGroupChat || _isForward)
                enterNameTxt.Text = stringBuilderForContactNames.ToString();
            else
                enterNameTxt.Text = string.Empty;

            if (_completeGroupedContactList == null)
                return;

            if (_showSmsContacts)
            {
                _completeGroupedContactList[4] = _emptySMSGroup;
                _showSmsContacts = !_showSmsContacts;
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_ShowSmsContacts_Txt;
            }
            else
            {
                _completeGroupedContactList[4] = _smsContactsGroup;
                _showSmsContacts = !_showSmsContacts;
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_HideSmsContacts_Txt;
            }

            contactsListBox.ItemsSource = _completeGroupedContactList;

            contactsListBox.InvalidateArrange();
        }

        void forwardTo_Click(object sender, EventArgs e)
        {
            if (_isClicked)
                return;

            _isClicked = true;

            App.ViewModel.ForwardMessage(SelectedContacts.ToList());

            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        #endregion

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_flag) // this is done to avoid twice calling of "enterNameTxt_TextChanged" function
            {
                _flag = !_flag;
                return;
            }
            _flag = !_flag;

            if (_isTextSelected)
                return;

            _isTextSelected = false;

            if (_backPressed)
            {
                enterNameTxt.Text =_textBeforeBackPress;
                enterNameTxt.SelectionStart = Math.Max(0, enterNameTxt.Text.Substring(0, enterNameTxt.Text.Length - 1).LastIndexOf(", "));
                if (enterNameTxt.Text.Substring(enterNameTxt.SelectionStart, 1) == ",") enterNameTxt.SelectionStart += 2;
                enterNameTxt.SelectionLength = enterNameTxt.Text.Length - enterNameTxt.SelectionStart;
                _backPressed = false;
                _isTextSelected = true;
                return;
            }

            _contactToBeRemoved = null;

            if (_isGroupChat || _isForward)
                _charsEntered = enterNameTxt.Text.Substring(stringBuilderForContactNames.Length);
            else
                _charsEntered = enterNameTxt.Text.ToLower();

            Debug.WriteLine("Chars Entered : {0}", _charsEntered);

            _charsEntered = _charsEntered.Trim();

            if (String.IsNullOrWhiteSpace(_charsEntered))
            {
                if (!_showSmsContacts)
                    _completeGroupedContactList[4] = _emptySMSGroup;

                contactsListBox.ItemsSource = _completeGroupedContactList;

                return;
            }

            if (groupListDictionary.ContainsKey(_charsEntered)
                && groupListStateDictionary.ContainsKey(_charsEntered)
                && groupListStateDictionary[_charsEntered] == _showSmsContacts)
            {
                ObservableCollection<ContactGroup<ContactInfo>> gl = groupListDictionary[_charsEntered];

                if (gl == null)
                {
                    groupListDictionary.Remove(_charsEntered);
                    groupListStateDictionary.Remove(_charsEntered);
                    contactsListBox.ItemsSource = null;
                    return;
                }

                if (gl[5].Count > 0 && gl[5][0].Msisdn != null)
                {
                    if (defaultContact.IsSelected)
                    {
                        gl[5].Remove(defaultContact);
                        defaultContact = new ContactInfo();
                        gl[5].Insert(0, defaultContact);
                    }
                    defaultContact.Name = _charsEntered;
                    string num = Utils.NormalizeNumber(_charsEntered);
                    defaultContact.Msisdn = num;
                    defaultContact.ContactListLabel = _charsEntered.Length >= 1 && _charsEntered.Length <= 15 ? num : AppResources.SelectUser_EnterValidNo_Txt;
                    defaultContact.IsSelected = SelectedContacts.Where(c => c.Msisdn == num).Count() > 0;
                    defaultContact.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;
                }

                contactsListBox.ItemsSource = gl;
                Thread.Sleep(5);
                return;
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, ev) =>
            {
                _glistFiltered = GetFilteredContactsFromNameOrPhoneAsync(_charsEntered, 0, 5);
            };
            bw.RunWorkerAsync();

            bw.RunWorkerCompleted += (s, ev) =>
            {
                if (_glistFiltered != null)
                {
                    groupListDictionary[_charsEntered] = _glistFiltered;
                    groupListStateDictionary[_charsEntered] = _showSmsContacts;
                }

                contactsListBox.ItemsSource = _glistFiltered;
                Thread.Sleep(2);
            };
        }

        ContactInfo _contactToBeRemoved;
        bool _backPressed = false;
        bool _isTextSelected = false;
        string _textBeforeBackPress;
        private void enterNameTxt_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isGroupChat && !_isForward) // logic is valid only for Group Chat
                return;

            _isTextSelected = false;

            if (e.Key == Key.Back)
            {
                if (enterNameTxt.Text.Length == 0) return;

                if (_contactToBeRemoved != null)
                {
                    CheckUnCheckContact(_contactToBeRemoved);
                    _contactToBeRemoved = null;
                    e.Handled = true;
                }
                else
                {
                    if (SelectedContacts.Count > 0)
                    {
                        int cursorPosition = enterNameTxt.SelectionStart;

                        cursorPosition = cursorPosition == 0 ? cursorPosition : cursorPosition - 1;

                        if (cursorPosition <= 0 || cursorPosition >= stringBuilderForContactNames.Length)
                            return;

                        _contactToBeRemoved = SelectedContacts[SelectedContacts.Count - 1];
                        _backPressed = true;
                        _textBeforeBackPress = enterNameTxt.Text;
                        return;
                    }
                }
            }
        }

        private void enterNameTxt_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!_isGroupChat && !_isForward) // logic is valid only for Group Chat
                return;

            int nameLength = 0, startIndex = 0;
            int cursorPosition = enterNameTxt.SelectionStart;

            _backPressed = _isTextSelected = false;
            _contactToBeRemoved = null;

            if (stringBuilderForContactNames.Length <= cursorPosition) // if textbox is tapped @ last position simply return
                return;

            for (int k = 0; k < SelectedContacts.Count; k++)
            {
                nameLength += SelectedContacts[k].Name.Length + 2; // length of name + "; " i.e 2
                if (cursorPosition < nameLength)
                {
                    enterNameTxt.Select(enterNameTxt.Text.Length,0);
                    var cInfo = SelectedContacts[k];
                    contactsListBox.ScrollTo(cInfo);
                    return;
                }
                else
                {
                    startIndex = nameLength;
                }
            }
        }

        private ObservableCollection<ContactGroup<ContactInfo>> GetFilteredContactsFromNameOrPhoneAsync(string charsEntered, int start, int end)
        {
            _glistFiltered = null;
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

            ObservableCollection<ContactGroup<ContactInfo>> listToIterate = null;
            int charsLength = charsEntered.Length - 1;

            if (charsLength > 0)
            {
                if (groupListDictionary.ContainsKey(charsEntered.Substring(0, charsLength))
                    && groupListStateDictionary.ContainsKey(charsEntered.Substring(0, charsLength))
                    && groupListStateDictionary[charsEntered.Substring(0, charsEntered.Length - 1)] == _showSmsContacts)
                {
                    listToIterate = groupListDictionary[charsEntered.Substring(0, charsEntered.Length - 1)];

                    if (listToIterate == null)
                        listToIterate = _completeGroupedContactList;
                }
                else
                    listToIterate = _completeGroupedContactList;
            }
            else
                listToIterate = _completeGroupedContactList;

            bool createNewFilteredList = true;

            for (int i = start; i <= end; i++)
            {
                int maxJ = listToIterate == null ? 0 : (listToIterate[i] == null ? 0 : listToIterate[i].Count);
                for (int j = 0; j < maxJ; j++)
                {
                    ContactInfo cn = listToIterate[i][j];
                    if (cn == null || (!_showSmsContacts && !cn.OnHike)) // hide sms contacts from search
                        continue;

                    cn.IsSelected = SelectedContacts.Where(c => c.Msisdn == cn.Msisdn).Count() > 0 ? true : false;
                    cn.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;

                    bool containsCharacter = false;

                    containsCharacter = Utils.isGroupConversation(cn.Msisdn) ? cn.Name.ToLower().Contains(charsEntered)
                        : cn.Name.ToLower().Contains(charsEntered) || cn.Msisdn.Contains(charsEntered);

                    if (containsCharacter)
                    {
                        if (createNewFilteredList)
                        {
                            createNewFilteredList = false;
                            _glistFiltered = CreateGroups();
                        }

                        _glistFiltered[i].Add(cn);
                    }
                }
            }

            ObservableCollection<ContactGroup<ContactInfo>> list = null;
            if (areCharsNumber)
            {
                if (_glistFiltered == null || createNewFilteredList)
                    list = CreateGroups();
                else
                    list = _glistFiltered;

                if (list[5].Contains(defaultContact))
                    list[5].Remove(defaultContact);

                if (list[5].Count == 0 || !list[5].Contains(defaultContact))
                {
                    list[5].Insert(0, defaultContact);

                    defaultContact.Msisdn = Utils.NormalizeNumber(_charsEntered);

                    charsEntered = (isPlus ? "+" : "") + charsEntered;
                    defaultContact.Name = charsEntered;
                    defaultContact.ContactListLabel = Utils.IsNumberValid(charsEntered) ? defaultContact.Msisdn : AppResources.SelectUser_EnterValidNo_Txt;
                    defaultContact.IsSelected = SelectedContacts.Where(c => c.Msisdn == defaultContact.Msisdn).Count() > 0;
                    defaultContact.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            if (!areCharsNumber && createNewFilteredList)
                return null;

            if (areCharsNumber)
                return list;

            return _glistFiltered;
        }

        #region REFRESH CONTACTS

        private void refreshContacts_Click(object sender, EventArgs e)
        {
            this.Focus();

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }

            App.AnalyticsInstance.addEvent(Analytics.REFRESH_CONTACTS);
            contactsListBox.IsHitTestVisible = false;
            DisableApplicationBar();

            if (progressIndicator == null)
                progressIndicator = new ProgressIndicatorControl();

            progressIndicator.Show(LayoutRoot, AppResources.SelectUser_RefreshWaitMsg_Txt);

            _canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        /* This callback is on background thread started by getContacts function */
        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }

            Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            /* If no contacts in Phone as well as App , simply return */
            if ((new_contacts_by_id == null || new_contacts_by_id.Count == 0) && hike_contacts_by_id == null)
            {
                scanningComplete();
                _canGoBack = true;
                return;
            }

            Dictionary<string, List<ContactInfo>> contacts_to_update_or_add = new Dictionary<string, List<ContactInfo>>();
            List<ContactInfo> contactsToBeUpdated = null;

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

                    var listToUpdate = ContactUtils.getContactsToUpdateList(phList, hkList);
                    if (listToUpdate != null)
                    {
                        if (contactsToBeUpdated == null)
                            contactsToBeUpdated = new List<ContactInfo>();

                        foreach (var item in listToUpdate)
                            contactsToBeUpdated.Add(item);
                    }

                    hike_contacts_by_id.Remove(id);
                }

                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
            }

            if (contactsToBeUpdated != null && contactsToBeUpdated.Count > 0)
            {
                UsersTableUtils.updateContacts(contactsToBeUpdated);
                ConversationTableUtils.updateConversation(contactsToBeUpdated);
            }

            /* If nothing is changed simply return without sending update request*/
            if (contacts_to_update_or_add.Count == 0 && (hike_contacts_by_id == null || hike_contacts_by_id.Count == 0))
            {
                Thread.Sleep(1000);
                scanningComplete();
                _canGoBack = true;
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
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }
            AccountUtils.updateAddressBook(contacts_to_update_or_add, ids_to_delete, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                scanningComplete();
                _canGoBack = true;
                return;
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap, true);
            List<ContactInfo.DelContacts> hikeIds = null;
            List<ContactInfo> deletedContacts = null;
            // Code to delete the removed contacts
            if (ContactUtils.hike_contactsMap != null && ContactUtils.hike_contactsMap.Count != 0)
            {
                hikeIds = new List<ContactInfo.DelContacts>(ContactUtils.hike_contactsMap.Count);
                deletedContacts = new List<ContactInfo>(ContactUtils.hike_contactsMap.Count);
                // This loop deletes all those contacts which are removed.
                Dictionary<string, GroupInfo> allGroupsInfo = null;
                GroupManager.Instance.LoadGroupCache();
                List<GroupInfo> gl = GroupTableUtils.GetAllGroups();
                for (int i = 0; i < gl.Count; i++)
                {
                    if (allGroupsInfo == null)
                        allGroupsInfo = new Dictionary<string, GroupInfo>();
                    allGroupsInfo[gl[i].GroupId] = gl[i];
                }

                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo cinfo = ContactUtils.hike_contactsMap[id][0];
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, cinfo.Msisdn);
                    hikeIds.Add(dCn);
                    deletedContacts.Add(cinfo);
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
                        obj = App.ViewModel.GetFav(cinfo.Msisdn);
                        if (obj == null) // this msisdn is not in favs , check in pending
                            obj = App.ViewModel.GetPending(cinfo.Msisdn);
                        if (obj != null)
                            obj.ContactName = null;
                    }

                    if (App.ViewModel.ContactsCache.ContainsKey(dCn.Msisdn))
                        App.ViewModel.ContactsCache[dCn.Msisdn].Name = null;
                    cinfo.Name = cinfo.Msisdn;
                    GroupManager.Instance.RefreshGroupCache(cinfo, allGroupsInfo);
                }
            }
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
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
                Object[] obj = new object[2];
                obj[0] = true;//denotes updated/added contact
                obj[1] = updatedContacts;
                App.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            if (deletedContacts != null && deletedContacts.Count > 0)
            {
                Object[] obj = new object[2];
                obj[0] = false;//denotes deleted contact
                obj[1] = deletedContacts;
                App.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            _allContactsList = UsersTableUtils.getAllContactsByGroup();
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            _completeGroupedContactList = GetGroupedList(_allContactsList);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                // this logic handles the case where hide sms contacts is there and user refreshed the list 
                if (!_showSmsContacts)
                    _completeGroupedContactList[4] = _emptySMSGroup;

                contactsListBox.ItemsSource = _completeGroupedContactList;

                scanningComplete();
            });
            _canGoBack = true;
        }

        private void scanningComplete()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (_isGroupChat || _isForward)
                    enterNameTxt.Text = stringBuilderForContactNames.ToString();
                else
                    enterNameTxt.Text = string.Empty;

                contactsListBox.IsHitTestVisible = true;
                progressIndicator.Hide(LayoutRoot);
                EnableApplicationBar();
            });
        }

        #endregion

        #region  MAKE JUMP LIST

        /// <summary>
        /// Get the group contact list. Should be called only once
        /// 1. When page loads
        /// 2. If user presses contact sync
        /// </summary>
        /// <param name="allContactsList">list of all contacts</param>
        /// <returns>group list</returns>
        private ObservableCollection<ContactGroup<ContactInfo>> GetGroupedList(List<ContactInfo> allContactsList)
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.EXISTING_GROUP_MEMBERS))
            {
                _isExistingGroup = true;
                activeExistingGroupMembers = PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] as List<GroupParticipant>;
                _existingGroupUsers = activeExistingGroupMembers.Count;
            }

            ObservableCollection<ContactGroup<ContactInfo>> glist = CreateGroups();
            ExistingContacts = new Dictionary<string, ContactInfo>();
            _smsContactsGroup = new ContactGroup<ContactInfo>(AppResources.NewComposeGroup_SMSContacts, AppResources.NewComposeGroup_1SMSContact);

            PopulateGroupChats(glist);
            PopulateRecentChats(glist);
            PopulateFriends(glist);

            #region Populate Hike and SMS contacts

            foreach (var cInfo in allContactsList)
            {
                if ((_isExistingGroup && msisdnAlreadyExists(cInfo.Msisdn, activeExistingGroupMembers))
                || cInfo.Msisdn == App.MSISDN || ExistingContacts.ContainsKey(cInfo.Msisdn) || Utils.IsHikeBotMsg(cInfo.Msisdn))
                    continue;

                //Added IsSelected because if user resyncs contacts, the new contacts should be pre selected if they ere already selected
                cInfo.IsSelected = SelectedContacts.Where(c => c.Msisdn == cInfo.Msisdn).Count() > 0;
                cInfo.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;

                if (cInfo.OnHike)
                    glist[3].Add(cInfo);
                else
                {
                    _smsContactsGroup.Add(cInfo);

                    if (_showSmsContacts)
                        glist[4].Add(cInfo);
                }
            }

            #endregion

            return glist;
        }

        Dictionary<string, ContactInfo> ExistingContacts;

        private void PopulateGroupChats(ObservableCollection<ContactGroup<ContactInfo>> glist)
        {
            if (_showExistingGroups)
            {
                bool forwardedFromGroupChat = false;
                string groupId = string.Empty;
                if (App.newChatThreadPage != null)
                {
                    groupId = App.newChatThreadPage.mContactNumber;
                    if (Utils.isGroupConversation(groupId))
                    {
                        forwardedFromGroupChat = true;
                    }
                }

                var gi = GroupTableUtils.getAllGroupInfo();

                foreach (var grp in gi)
                {
                    if (!forwardedFromGroupChat || grp.GroupId != groupId)//handled ended group
                    {
                        ContactInfo cInfo = new ContactInfo();
                        cInfo.Name = grp.GroupName ?? App.ViewModel.ConvMap[grp.GroupId].NameToShow;
                        cInfo.ContactListLabel = AppResources.GrpChat_Txt;//to show in tap msg
                        cInfo.OnHike = true;
                        cInfo.HasCustomPhoto = true;//show it is group chat
                        cInfo.Msisdn = grp.GroupId;//groupid
                        cInfo.IsSelected = SelectedContacts.Where(c => c.Msisdn == cInfo.Msisdn).Count() > 0;
                        cInfo.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;
                        glist[1].Add(cInfo);

                        groupInfoDictionary[grp.GroupId] = grp.GroupOwner;
                    }
                }
            }
        }

        private void PopulateRecentChats(ObservableCollection<ContactGroup<ContactInfo>> glist)
        {
            if (_isGroupChat || _isForward)
            {
                foreach (var entry in App.ViewModel.ConvMap)
                {
                    var conv = entry.Value;

                    if (conv.IsGroupChat || Utils.IsHikeBotMsg(conv.Msisdn))
                        continue;

                    if (_isExistingGroup && msisdnAlreadyExists(conv.Msisdn, activeExistingGroupMembers))
                        continue;

                    if (ExistingContacts.ContainsKey(conv.Msisdn))
                        continue;

                    if (!conv.IsGroupChat)
                    {
                        ContactInfo cInfo = new ContactInfo();
                        cInfo.Name = conv.NameToShow;
                        cInfo.ContactListLabel = conv.Msisdn;
                        cInfo.OnHike = true;
                        cInfo.Msisdn = conv.Msisdn;
                        cInfo.Avatar = conv.Avatar;
                        cInfo.IsSelected = SelectedContacts.Where(c => c.Msisdn == cInfo.Msisdn).Count() > 0;
                        cInfo.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;
                        glist[0].Add(cInfo);

                        ExistingContacts.Add(cInfo.Msisdn, cInfo);
                    }
                }
            }
        }

        private void PopulateFriends(ObservableCollection<ContactGroup<ContactInfo>> glist)
        {
            foreach (var friend in App.ViewModel.FavList)
            {
                if ((_isExistingGroup && msisdnAlreadyExists(friend.Msisdn, activeExistingGroupMembers)) || Utils.IsHikeBotMsg(friend.Msisdn))
                    continue;

                if (ExistingContacts.ContainsKey(friend.Msisdn))
                    continue; 
                
                if (friend.Avatar == null)
                {
                    if (App.ViewModel.ConvMap.ContainsKey(friend.Msisdn))
                        friend.Avatar = App.ViewModel.ConvMap[friend.Msisdn].Avatar;
                    else
                    {
                        friend.Avatar = MiscDBUtil.getThumbNailForMsisdn(friend.Msisdn);
                    }
                }

                ContactInfo cInfo = new ContactInfo();
                cInfo.Name = friend.NameToShow;
                cInfo.ContactListLabel = friend.Msisdn;//to show in tap msg
                cInfo.OnHike = true;
                cInfo.Msisdn = friend.Msisdn;
                cInfo.Avatar = friend.Avatar;
                cInfo.IsSelected = SelectedContacts.Where(c => c.Msisdn == cInfo.Msisdn).Count() > 0;
                cInfo.CheckBoxVisibility = (_isForward || _isGroupChat) ? Visibility.Visible : Visibility.Collapsed;
                glist[2].Add(cInfo);

                ExistingContacts.Add(cInfo.Msisdn, cInfo);
            }
        }

        private bool msisdnAlreadyExists(string msisdn, List<GroupParticipant> activeExistingGroupMembers)
        {
            return activeExistingGroupMembers.Where(m => m.Msisdn == msisdn).Count() > 0;
        }

        private ObservableCollection<ContactGroup<ContactInfo>> CreateGroups()
        {
            string[] Groups = new string[]
            {
                AppResources.NewComposeGroup_RecentContacts,
                AppResources.NewComposeGroup_1RecentContact,
                AppResources.NewComposeGroup_GroupChats,
                 AppResources.NewComposeGroup_1GroupChat,
                AppResources.NewComposeGroup_Friends,
                AppResources.NewComposeGroup_1Friend,
                AppResources.NewComposeGroup_HikeContacts,
                AppResources.NewComposeGroup_1HikeContact,
                AppResources.NewComposeGroup_SMSContacts,
                AppResources.NewComposeGroup_1SMSContact,
                AppResources.NewComposeGroup_OtherContacts,
                AppResources.NewComposeGroup_1OtherContact
            };

            ObservableCollection<ContactGroup<ContactInfo>> glist = new ObservableCollection<ContactGroup<ContactInfo>>();

            for (int i = 0; i < Groups.Length;i++,i++ )
            {
                ContactGroup<ContactInfo> g = new ContactGroup<ContactInfo>(Groups[i], Groups[i + 1]);
                glist.Add(g);
            }

            return glist;
        }

        #endregion

        private void DisableApplicationBar()
        {
            ApplicationBar.IsMenuEnabled = false;
            _refreshIconButton.IsEnabled = false;

            if (_doneIconButton != null)
                _doneIconButton.IsEnabled = false;
        }

        private void EnableApplicationBar()
        {
            _refreshIconButton.IsEnabled = true;
            ApplicationBar.IsMenuEnabled = true;

            if (_doneIconButton != null)
                _doneIconButton.IsEnabled = true;
        }

        #region Contact Select Based Functions

        private void ContactItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var cInfo = (sender as FrameworkElement).DataContext as ContactInfo;

            CheckUnCheckContact(cInfo);
        }

        private void CheckUnCheckContact(ContactInfo cInfo)
        {
            enterNameTxt.Text = String.Empty;

            if (cInfo != null)
            {
                if (_isForward || _isGroupChat)
                {
                    int oldSmsCount = _smsUserCount;

                    cInfo.IsSelected = !cInfo.IsSelected;

                    //count sms users
                    if (cInfo.IsSelected)
                    {
                        if (!SelectedContacts.Contains(cInfo))
                        {
                            if (IsUserBlocked(cInfo))
                            {
                                cInfo.IsSelected = false;
                                return;
                            }

                            if (_isExistingGroup && msisdnAlreadyExists(cInfo.Msisdn, activeExistingGroupMembers))
                            {
                                MessageBoxResult result = MessageBox.Show(string.Format(AppResources.SelectUser_UserAlreadyAdded_Txt, cInfo.Msisdn), AppResources.SelectUser_AlreadyAdded_Txt, MessageBoxButton.OK);
                                cInfo.IsSelected = false; 
                                return;
                            }

                            if (_isGroupChat && (SelectedContacts.Count + _existingGroupUsers == MAX_USERS_ALLOWED_IN_GROUP))
                            {
                                MessageBoxResult result = MessageBox.Show(string.Format(AppResources.SelectUser_MaxUsersSelected_Txt, MAX_USERS_ALLOWED_IN_GROUP), AppResources.SelectUser_CantAddUser_Txt, MessageBoxButton.OK);
                                cInfo.IsSelected = false;
                                return;
                            }

                            if (!_isContactShared && _isFreeSmsOn && _isForward)
                            {
                                if (!Utils.isGroupConversation(cInfo.Msisdn))
                                {
                                    if (!cInfo.OnHike)
                                        _smsUserCount++;
                                }
                                else
                                    _smsUserCount += GroupManager.Instance.GetSMSParticiantCount(cInfo.Msisdn);

                                if (_smsUserCount > _smsCredits)
                                {
                                    MessageBox.Show(AppResources.H2HOfline_0SMS_Message, AppResources.H2HOfline_Confirmation_Message_Heading, MessageBoxButton.OK);

                                    cInfo.IsSelected = false;
                                    _smsUserCount = oldSmsCount;

                                    return;
                                }
                            }

                            if (defaultContact == cInfo)
                                defaultContact = new ContactInfo();

                            SelectedContacts.Add(cInfo);
                        }
                    }
                    else
                    {
                        if (!_isContactShared && _isFreeSmsOn)
                        {
                            var list = SelectedContacts.Where(x => x.Msisdn == cInfo.Msisdn).ToList();
                            foreach (var item in list)
                            {
                                item.IsSelected = false;

                                if (!Utils.isGroupConversation(item.Msisdn))
                                {
                                    if (!item.OnHike)
                                        _smsUserCount--;
                                }
                                else
                                    _smsUserCount -= GroupManager.Instance.GetSMSParticiantCount(item.Msisdn);
                            }
                        }

                        var itemList = SelectedContacts.Where(x => x.Msisdn == cInfo.Msisdn).ToList();

                        if (itemList.Count() > 0)
                        {
                            foreach (var item in itemList)
                                SelectedContacts.Remove(item);
                        }
                    }

                    if (_isGroupChat && !_isExistingGroup) // case if group is new
                        _doneIconButton.IsEnabled = SelectedContacts.Count > 1;
                    else
                        _doneIconButton.IsEnabled = SelectedContacts.Count > 0;

                    if (SelectedContacts.Count > 0)
                    {
                        stringBuilderForContactNames = new StringBuilder(string.Join(", ", SelectedContacts)).Append(", ");
                        PageTitle.Text = String.Format(AppResources.Selected_Txt, SelectedContacts.Count);
                    }
                    else
                    {
                        stringBuilderForContactNames = new StringBuilder();
                        PageTitle.Text = _pageTitle;
                    }

                    enterNameTxt.Text = stringBuilderForContactNames.ToString();
                    enterNameTxt.Select(enterNameTxt.Text.Length, 0);
                }
                else
                {
                    if (cInfo == null || cInfo.Msisdn == AppResources.SelectUser_EnterValidNo_Txt || cInfo.Msisdn == App.MSISDN)
                        return;

                    if (IsUserBlocked(cInfo))
                        return;

                    PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = cInfo;
                    string uri = "/View/NewChatThread.xaml";
                    NavigationService.Navigate(new Uri(uri, UriKind.Relative));
                }
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cInfo = (sender as FrameworkElement).DataContext as ContactInfo;

            if (cInfo.IsSelected) return;

            CheckUnCheckContact(cInfo);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            var cInfo = (sender as FrameworkElement).DataContext as ContactInfo;

            if (!cInfo.IsSelected) return;

            CheckUnCheckContact(cInfo);
        }

        bool IsUserBlocked(ContactInfo cInfo)
        {
            if (Utils.isGroupConversation(cInfo.Msisdn))
            {
                if (App.ViewModel.BlockedHashset.Contains(groupInfoDictionary[cInfo.Msisdn]))
                {
                    var result = MessageBox.Show(AppResources.GroupBlocked_PomptTxt, AppResources.Confirmation_HeaderTxt, MessageBoxButton.OKCancel);

                    if (result == MessageBoxResult.OK)
                    {
                        App.ViewModel.BlockedHashset.Remove(groupInfoDictionary[cInfo.Msisdn]);
                        App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_GROUPOWNER, groupInfoDictionary[cInfo.Msisdn]);
                        return false;
                    }
                }
                else
                    return false;
            }
            else if (App.ViewModel.BlockedHashset.Contains(cInfo.Msisdn))
            {
                var result = MessageBox.Show(AppResources.UserBlocked_PomptTxt, AppResources.Confirmation_HeaderTxt, MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    App.ViewModel.BlockedHashset.Remove(cInfo.Msisdn);
                    App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, cInfo.Msisdn);
                    return false;
                }
            }
            else
                return false;

            cInfo.IsSelected = false;
            return true;
        }

        #endregion

        #region Page State Functions

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
                // Get a dictionary of query string keys and values.
                IDictionary<string, string> queryStrings = this.NavigationContext.QueryString;

                // Ensure that there is at least one key in the query string, and check 
                // whether the "FileId" key is present.
                if (queryStrings.ContainsKey("FileId"))
                {
                    _showExistingGroups = true;
                    PhoneApplicationService.Current.State["SharePicker"] = queryStrings["FileId"];
                    queryStrings.Clear();
                    _pageTitle = AppResources.Share_With_Txt;
                }

                if (App.APP_LAUNCH_STATE != App.LaunchState.NORMAL_LAUNCH)
                {
                    while (NavigationService.CanGoBack)
                        NavigationService.RemoveBackEntry();
                }

                enterNameTxt.Hint = AppResources.SelectUser_TxtBoxHint_Txt;
            }

            //remove if push came directly from upgrade page
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.LAUNCH_FROM_UPGRADEPAGE))
            {
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove(HikeConstants.LAUNCH_FROM_UPGRADEPAGE);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (!_canGoBack)
            {
                MessageBoxResult mbox = MessageBox.Show(AppResources.Stop_Contact_Scanning, AppResources.Stop_Caption_txt, MessageBoxButton.OKCancel);
                if (mbox == MessageBoxResult.OK)
                {
                    _stopContactScanning = true;
                    contactsListBox.IsHitTestVisible = true;
                    progressIndicator.Hide(LayoutRoot);
                    EnableApplicationBar();
                    _canGoBack = true;
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
                Debug.WriteLine("ForwardTo.xaml :: OnRemovedFromJournal, Exception : " + ex.StackTrace);
            }

            PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
            PhoneApplicationService.Current.State.Remove(HikeConstants.START_NEW_GROUP);
            PhoneApplicationService.Current.State.Remove(HikeConstants.EXISTING_GROUP_MEMBERS);
            PhoneApplicationService.Current.State.Remove("Group_GroupId");
            base.OnRemovedFromJournal(e);
        }

        #endregion

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

        bool _isForward;
        bool _isClicked;

        int _existingGroupUsers; // 1 because owner of the group is already included

        List<GroupParticipant> activeExistingGroupMembers;
    }
}