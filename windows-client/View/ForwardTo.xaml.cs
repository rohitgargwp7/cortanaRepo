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

namespace windows_client.View
{
    public partial class ForwardTo : PhoneApplicationPage
    {
        private bool _canGoBack = true;
        private bool _showSmsContacts;
        private bool _isFreeSmsOn = true;
        private bool _showExistingGroups;
        private bool _stopContactScanning = false;
        private bool _isContactShared = false;
        private bool _flag;

        int _smsUserCount = 0;
        private int _smsCredits;
        private int _maxCharGroups = 26;
        private string _charsEntered;

        private string TAP_MSG = AppResources.SelectUser_TapMsg_Txt;

        List<Group<ContactInfo>> _glistFiltered = null;
        public List<ContactInfo> _contactsForForward = new List<ContactInfo>(); // this is used to store all those contacts which are selected for forwarding message

        public List<Group<ContactInfo>> _completeGroupedContactList = null; // list that will contain the complete jump list
        public List<Group<ContactInfo>> _filteredGroupedContactList = null;
        private List<Group<ContactInfo>> _defaultGroupedContactList = null;

        List<ContactInfo> _allContactsList = null; // contacts list

        private ProgressIndicatorControl progressIndicator;

        private ApplicationBarIconButton _doneIconButton = null;
        private ApplicationBarIconButton _refreshIconButton = null;
        private ApplicationBarMenuItem _onHikeFilterMenuItem = null;

        Dictionary<string, List<Group<ContactInfo>>> groupListDictionary = new Dictionary<string, List<Group<ContactInfo>>>();

        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 

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
                txtTitle.Visibility = Visibility.Collapsed;
                txtChat.Text = AppResources.SelectUser_Forward_To_Txt;
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

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                _allContactsList = UsersTableUtils.getAllContactsByGroup();
                _completeGroupedContactList = GetGroupedList(_allContactsList);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {
                if (!_showSmsContacts)
                {
                    if (_filteredGroupedContactList == null)
                        MakeFilteredJumpList();

                    contactsListBox.ItemsSource = _filteredGroupedContactList;
                }
                else
                    contactsListBox.ItemsSource = _completeGroupedContactList;

                shellProgress.IsVisible = false;
            };
            initPage();
            //App.HikePubSubInstance.addListener(HikePubSub.GROUP_END, this);
        }

        private void initPage()
        {
            ApplicationBar = new ApplicationBar();
            ApplicationBar.Mode = ApplicationBarMode.Default;
            ApplicationBar.Opacity = 1;
            ApplicationBar.IsVisible = true;
            ApplicationBar.IsMenuEnabled = true;

            _refreshIconButton = new ApplicationBarIconButton();
            _refreshIconButton.IconUri = new Uri("/View/images/icon_refresh.png", UriKind.Relative);
            _refreshIconButton.Text = AppResources.SelectUser_RefreshContacts_Txt;
            _refreshIconButton.Click += new EventHandler(refreshContacts_Click);
            _refreshIconButton.IsEnabled = true;
            ApplicationBar.Buttons.Add(_refreshIconButton);

            if (!_isContactShared && _isFreeSmsOn)
            {
                _onHikeFilterMenuItem = new ApplicationBarMenuItem();
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_HideSmsContacts_Txt;
                _onHikeFilterMenuItem.Click += new EventHandler(OnHikeFilter_Click);
                ApplicationBar.MenuItems.Add(_onHikeFilterMenuItem);
            }

            ApplicationBar = ApplicationBar;

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
            {
                if (_doneIconButton != null)
                    return;
                _doneIconButton = new ApplicationBarIconButton();
                _doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
                _doneIconButton.Text = AppResources.AppBar_Done_Btn;
                _doneIconButton.Click += forwardTo_Click;
                _doneIconButton.IsEnabled = false;
                ApplicationBar.Buttons.Add(_doneIconButton);
            }
        }

        private void OnHikeFilter_Click(object sender, EventArgs e)
        {
            if (_showSmsContacts)
            {
                if (_filteredGroupedContactList == null)
                {
                    MakeFilteredJumpList();
                }
                contactsListBox.ItemsSource = _filteredGroupedContactList;
                _showSmsContacts = !_showSmsContacts;
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_ShowSmsContacts_Txt;
            }
            else
            {
                contactsListBox.ItemsSource = _completeGroupedContactList;
                _showSmsContacts = !_showSmsContacts;
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_HideSmsContacts_Txt;
            }
        }

        void forwardTo_Click(object sender, EventArgs e)
        {
            App.ViewModel.ForwardMessage(_contactsForForward);

            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_flag) // this is done to avoid twice calling of "enterNameTxt_TextChanged" function
            {
                _flag = !_flag;
                return;
            }
            _flag = !_flag;

            _charsEntered = enterNameTxt.Text.ToLower();
            Debug.WriteLine("Chars Entered : {0}", _charsEntered);

            _charsEntered = _charsEntered.Trim();

            if (String.IsNullOrWhiteSpace(_charsEntered))
            {
                contactsListBox.ItemsSource = _completeGroupedContactList;
                return;
            }

            if (groupListDictionary.ContainsKey(_charsEntered))
            {
                List<Group<ContactInfo>> gl = groupListDictionary[_charsEntered];
                
                if (gl == null)
                {
                    groupListDictionary.Remove(_charsEntered);
                    contactsListBox.ItemsSource = null;
                    return;
                }

                if (gl[_maxCharGroups].Count > 0 && gl[_maxCharGroups][0].Msisdn != null)
                {
                    if (gl[_maxCharGroups][0].IsSelected)
                        gl[_maxCharGroups][0] = defaultContact;
                    
                    gl[_maxCharGroups][0].Name = _charsEntered;
                    string num = Utils.NormalizeNumber(_charsEntered);
                    gl[_maxCharGroups][0].Msisdn = num;
                    gl[_maxCharGroups][0].ContactListLabel = _charsEntered.Length >= 1 && _charsEntered.Length <= 15 ? num : AppResources.SelectUser_EnterValidNo_Txt;
                    gl[_maxCharGroups][0].IsSelected = _contactsForForward.Where(c => c.Msisdn == num).Count() > 0 ? true : false;
                }

                contactsListBox.ItemsSource = gl;
                Thread.Sleep(5);
                return;
            }
            //glistFiltered = createGroups();
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, ev) =>
            {
                _glistFiltered = GetFilteredContactsFromNameOrPhoneAsync(_charsEntered, 0, _maxCharGroups);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, ev) =>
            {
                if (_glistFiltered != null)
                    groupListDictionary[_charsEntered] = _glistFiltered;
                contactsListBox.ItemsSource = _glistFiltered;
                Thread.Sleep(2);
            };
        }

        private List<Group<ContactInfo>> GetFilteredContactsFromNameOrPhoneAsync(string charsEntered, int start, int end)
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

                    cn.IsSelected = _contactsForForward.Where(c => c.Msisdn == cn.Msisdn).Count() > 0 ? true : false;

                    bool containsCharacter = false;

                    if (Utils.isGroupConversation(cn.Msisdn))
                    {
                        var gplist = GroupManager.Instance.GetParticipantList(cn.Msisdn);

                        foreach (var gp in gplist)
                        {
                            if (gp.HasLeft)
                                continue;

                            if( gp.Name.ToLower().Contains(charsEntered) || gp.Msisdn.Contains(charsEntered))
                            {
                                containsCharacter =true;
                                break;
                            }
                        }
                    }
                    else
                        containsCharacter = cn.Name.ToLower().Contains(charsEntered) || cn.Msisdn.Contains(charsEntered);

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

            List<Group<ContactInfo>> list = null;
            if (areCharsNumber)
            {
                if (_glistFiltered == null || createNewFilteredList)
                {
                    if (_defaultGroupedContactList == null)
                        _defaultGroupedContactList = CreateGroups();
                    
                    list = _defaultGroupedContactList;

                    if (_defaultGroupedContactList[_maxCharGroups].Count == 0)
                        _defaultGroupedContactList[_maxCharGroups].Insert(0, defaultContact);
                }
                else
                {
                    list = _glistFiltered;
                    list[_maxCharGroups].Insert(0, defaultContact);
                }

                list[_maxCharGroups][0].Msisdn = Utils.NormalizeNumber(_charsEntered);

                charsEntered = (isPlus ? "+" : "") + charsEntered;
                list[_maxCharGroups][0].Name = charsEntered;
                list[_maxCharGroups][0].ContactListLabel = Utils.IsNumberValid(charsEntered) ? list[_maxCharGroups][0].Msisdn : AppResources.SelectUser_EnterValidNo_Txt;
                list[_maxCharGroups][0].IsSelected = _contactsForForward.Where(c => c.Msisdn == defaultContact.Msisdn).Count() > 0;
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
            contactsListBox.IsHitTestVisible = false;

            App.AnalyticsInstance.addEvent(Analytics.REFRESH_CONTACTS);
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }
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

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                _filteredGroupedContactList = null;
                _completeGroupedContactList = GetGroupedList(_allContactsList);

                // this logic handles the case where hide sms contacts is there and user refreshed the list 
                if (!_showSmsContacts)
                {
                    MakeFilteredJumpList();
                    contactsListBox.ItemsSource = _filteredGroupedContactList;
                }
                else
                    contactsListBox.ItemsSource = _completeGroupedContactList;
                progressIndicator.Hide(LayoutRoot);
                EnableApplicationBar();
                contactsListBox.IsHitTestVisible = true;
            });
            _canGoBack = true;
        }

        private void scanningComplete()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                contactsListBox.IsHitTestVisible = true;
                progressIndicator.Hide(LayoutRoot);
                EnableApplicationBar();
            });
        }

        #endregion

        #region  MAKE JUMP LIST

        private List<Group<ContactInfo>> GetGroupedList(List<ContactInfo> allContactsList)
        {
            List<Group<ContactInfo>> glist = CreateGroups();
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
                List<ConversationListObject> listGroupChats = new List<ConversationListObject>();
                foreach (ConversationListObject convList in App.ViewModel.ConvMap.Values)
                {
                    if (convList.IsGroupChat && convList.IsGroupAlive && (!forwardedFromGroupChat || convList.Msisdn != groupId))//handled ended group
                    {
                        listGroupChats.Add(convList);
                    }
                }
                listGroupChats.Sort((g1, g2) => g2.TimeStamp.CompareTo(g1.TimeStamp));

                foreach (ConversationListObject convList in listGroupChats)
                {
                    ContactInfo cinfo = new ContactInfo();
                    cinfo.Name = convList.NameToShow;
                    cinfo.ContactListLabel = AppResources.GrpChat_Txt;//to show in tap msg
                    cinfo.OnHike = true;
                    cinfo.HasCustomPhoto = true;//show it is group chat
                    cinfo.Msisdn = convList.Msisdn;//groupid
                    glist[0].Add(cinfo);
                }
            }

            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
            {
                ContactInfo c = allContactsList[i];
                if (c.Msisdn == App.MSISDN) // don't show own number in any chat.
                    continue;

                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = ((ch == "#") ? 26 : ch[0] - 'a') + (_showExistingGroups ? 1 : 0);
                // and add the entry
                glist[index].Add(c);
            }

            _maxCharGroups = glist.Count - 1;

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

        private List<Group<ContactInfo>> CreateGroups()
        {
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            List<Group<ContactInfo>> glist;
            if (_showExistingGroups)
            {
                glist = new List<Group<ContactInfo>>(28);
                Group<ContactInfo> g = new Group<ContactInfo>(string.Empty, true, new List<ContactInfo>(1));
                glist.Add(g);
            }
            else
                glist = new List<Group<ContactInfo>>(27);

            foreach (char c in Groups)
            {
                Group<ContactInfo> g = new Group<ContactInfo>(c.ToString(), false, new List<ContactInfo>(1));
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
            _filteredGroupedContactList = CreateGroups();
            for (int i = 0; i < _completeGroupedContactList.Count; i++)
            {
                Group<ContactInfo> g = _completeGroupedContactList[i];
                if (g == null || g.Count <= 0)
                    continue;
                for (int j = 0; j < g.Count; j++)
                {
                    ContactInfo c = g[j];
                    if (c.OnHike) // if on hike 
                        _filteredGroupedContactList[i].Add(c);
                }
            }
        }

        #endregion

        private void DisableApplicationBar()
        {
            ApplicationBar.IsMenuEnabled = false;
            _refreshIconButton.IsEnabled = false;
            _doneIconButton.IsEnabled = false;
        }

        private void EnableApplicationBar()
        {
            _refreshIconButton.IsEnabled = true;
            ApplicationBar.IsMenuEnabled = true;
            _doneIconButton.IsEnabled = true;
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

        private void ContactItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var cInfo = (sender as FrameworkElement).DataContext as ContactInfo;
            CheckUnCheckContact(cInfo);
        }

        private void CheckUnCheckContact(ContactInfo cInfo)
        {
            int oldSmsCount = _smsUserCount;

            if (cInfo != null)
            {
                cInfo.IsSelected = !cInfo.IsSelected;

                    //count sms users
                if (cInfo.IsSelected)
                {
                    if (!_contactsForForward.Contains(cInfo))
                    {
                        if (defaultContact == cInfo)
                            defaultContact = new ContactInfo();

                        if (!_isContactShared && _isFreeSmsOn)
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
                                MessageBox.Show(AppResources.H2HOfline_0SMS_Message);

                                cInfo.IsSelected = false;
                                _smsUserCount = oldSmsCount;

                                return;
                            }
                        }

                        _contactsForForward.Add(cInfo);
                    }
                }
                else
                {
                    if (!_isContactShared && _isFreeSmsOn)
                    {
                        var list = _contactsForForward.Where(x => x.Msisdn == cInfo.Msisdn).ToList();
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

                    _contactsForForward.RemoveAll(x => x.Msisdn == cInfo.Msisdn);
                }

                _doneIconButton.IsEnabled = _contactsForForward.Count > 0;
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
    }

    public class Group<T> : List<T>
    {
        bool _isGroup;

        public Visibility TextVisibility
        {
            get
            {
                return !_isGroup ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Visibility GrpImageVisibility
        {
            get
            {
                return _isGroup ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public Group(string name, bool isGroup, List<T> items)
        {
            this.Title = name;
            _isGroup = isGroup;
        }

        public string Title
        {
            get;
            set;
        }
    }
}