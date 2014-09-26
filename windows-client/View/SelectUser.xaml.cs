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

namespace windows_client.View
{
    /// <summary>
    /// Called for Blocked user and share contact use case
    /// </summary>
    public partial class SelectUser : PhoneApplicationPage
    {
        private bool _canGoBack = true;
        private bool _showSmsContacts = true;
        private bool _stopContactScanning = false;
        private bool _isContactShared = false;
        private bool _flag;

        int _smsUserCount = 0;
        private int _smsCredits;
        private int _maxCharGroups = 27;
        private string _charsEntered;

        List<ItemGroup<ContactInfo>> _glistFiltered = null;

        List<ItemGroup<ContactInfo>> _completeGroupedContactList = null; // list that will contain the complete jump list
        List<ItemGroup<ContactInfo>> _filteredGroupedContactList = null;
        List<ItemGroup<ContactInfo>> _defaultGroupedContactList = null;

        List<ContactInfo> _allContactsList = null; // contacts list

        private ProgressIndicatorControl progressIndicator;

        private ApplicationBarIconButton _doneIconButton = null;
        private ApplicationBarIconButton _refreshIconButton = null;
        private ApplicationBarMenuItem _onHikeFilterMenuItem = null;

        Dictionary<string, List<ItemGroup<ContactInfo>>> groupListDictionary = new Dictionary<string, List<ItemGroup<ContactInfo>>>();

        /// <summary>
        /// maintain state dictionary for showSMScontacts in parallel to groupListDictionary
        /// so that while searching the value of showsmscontacts is considered too
        /// </summary>
        Dictionary<string, bool> groupListStateDictionary = new Dictionary<string, bool>();

        Dictionary<string, string> groupInfoDictionary = new Dictionary<string, string>();

        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 

        public SelectUser()
        {
            InitializeComponent();

            enterNameTxt.Hint = AppResources.SelectUser_TxtBoxHint_Txt;


            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.SMS_SETTING, out _smsCredits);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.NavigationKeys.SHARE_CONTACT))
            {
                _isContactShared = true;
                PageTitle.Text = (AppResources.ShareContact_Txt).ToLower();
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_BLOCKED_LIST))
            {
                _frmBlockedList = true;
                blockedSet = new HashSet<string>();
                PageTitle.Text = AppResources.Blocklist_user_txt;
            }

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
                {
                    if (_filteredGroupedContactList == null)
                        MakeFilteredJumpList();

                    contactsListBox.ItemsSource = _filteredGroupedContactList;

                    if (_filteredGroupedContactList == null || _filteredGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    contactsListBox.ItemsSource = _completeGroupedContactList;

                    if (_completeGroupedContactList == null || _completeGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }

                shellProgress.IsIndeterminate = false;
            };

            initPage();
        }

        #region AppBar

        private void initPage()
        {
            ApplicationBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["AppBarBackground"]).Color,
                Opacity = 0.95
            };

            ApplicationBar.StateChanged += ApplicationBar_StateChanged;

            _refreshIconButton = new ApplicationBarIconButton();
            _refreshIconButton.IconUri = new Uri("/View/images/AppBar/icon_refresh.png", UriKind.Relative);
            _refreshIconButton.Text = AppResources.SelectUser_RefreshContacts_Txt;
            _refreshIconButton.Click += new EventHandler(refreshContacts_Click);
            _refreshIconButton.IsEnabled = true;
            ApplicationBar.Buttons.Add(_refreshIconButton);

            _onHikeFilterMenuItem = new ApplicationBarMenuItem();
            _onHikeFilterMenuItem.Text = _showSmsContacts ? AppResources.SelectUser_HideSmsContacts_Txt : AppResources.SelectUser_ShowSmsContacts_Txt;
            _onHikeFilterMenuItem.Click += OnHikeFilter_Click;
            ApplicationBar.MenuItems.Add(_onHikeFilterMenuItem);
        }

        void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible)
                ApplicationBar.Opacity = 1;
            else
                ApplicationBar.Opacity = 0.95;
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

                if (_filteredGroupedContactList == null || _filteredGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                }
                else
                    emptyGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                contactsListBox.ItemsSource = _completeGroupedContactList;
                _showSmsContacts = !_showSmsContacts;
                _onHikeFilterMenuItem.Text = AppResources.SelectUser_HideSmsContacts_Txt;

                if (_completeGroupedContactList == null || _completeGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                }
                else
                    emptyGrid.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        private ContactInfo GetContactIfExists(ContactInfo contact)
        {
            if (_glistFiltered == null)
                return contact;
            for (int i = 0; i < _maxCharGroups; i++)
            {
                if (_glistFiltered[i] == null || _glistFiltered[i] == null)
                    return contact;
                for (int k = 0; k < _glistFiltered[i].Count; k++)
                {
                    if (_glistFiltered[i][k].Msisdn == contact.Msisdn)
                        return _glistFiltered[i][k];
                }
            }
            // if not found
            //contact.Name = contact.Msisdn;
            return contact;
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
                if (!_showSmsContacts)
                {
                    if (_filteredGroupedContactList == null)
                    {
                        MakeFilteredJumpList();
                    }
                    contactsListBox.ItemsSource = _filteredGroupedContactList;

                    if (_filteredGroupedContactList == null || _filteredGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    contactsListBox.ItemsSource = _completeGroupedContactList;

                    if (_completeGroupedContactList == null || _completeGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }
                return;
            }

            if (groupListDictionary.ContainsKey(_charsEntered)
                && groupListStateDictionary.ContainsKey(_charsEntered)
                && groupListStateDictionary[_charsEntered] == _showSmsContacts)
            {
                List<ItemGroup<ContactInfo>> gl = groupListDictionary[_charsEntered];

                if (gl == null)
                {
                    groupListDictionary.Remove(_charsEntered);
                    groupListStateDictionary.Remove(_charsEntered);
                    contactsListBox.ItemsSource = null;

                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoSearchToDisplay_Txt;

                    return;
                }

                if (gl[_maxCharGroups].Count > 0 && gl[_maxCharGroups][0].Msisdn != null)
                {
                    if (defaultContact.IsSelected)
                    {
                        gl[_maxCharGroups].Remove(defaultContact);
                        defaultContact = new ContactInfo();
                        gl[_maxCharGroups].Insert(0, defaultContact);
                    }
                    defaultContact.Name = _charsEntered;
                    string num = Utils.NormalizeNumber(_charsEntered);
                    defaultContact.Msisdn = num;
                    defaultContact.ContactListLabel = _charsEntered.Length >= 1 && _charsEntered.Length <= 15 ? num : AppResources.SelectUser_EnterValidNo_Txt;

                    if (!HikeInstantiation.ViewModel.IsHiddenModeActive
                        && HikeInstantiation.ViewModel.ConvMap.ContainsKey(defaultContact.Msisdn)
                        && HikeInstantiation.ViewModel.ConvMap[defaultContact.Msisdn].IsHidden)
                        defaultContact.IsSelected = false;
                    else
                        defaultContact.IsSelected = _frmBlockedList && IsUserBlocked(defaultContact);

                    defaultContact.CheckBoxVisibility = _frmBlockedList ? Visibility.Visible : Visibility.Collapsed;
                }

                contactsListBox.ItemsSource = gl;

                if (gl == null || gl.Where(c => c.Count > 0).Count() == 0)
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoSearchToDisplay_Txt;
                }
                else
                    emptyGrid.Visibility = Visibility.Collapsed;

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
                {
                    groupListDictionary[_charsEntered] = _glistFiltered;
                    groupListStateDictionary[_charsEntered] = _showSmsContacts;
                }

                contactsListBox.ItemsSource = _glistFiltered;

                if (_glistFiltered == null || _glistFiltered.Where(c => c.Count > 0).Count() == 0)
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoSearchToDisplay_Txt;
                }
                else
                    emptyGrid.Visibility = Visibility.Collapsed;

                Thread.Sleep(2);
            };
        }

        private List<ItemGroup<ContactInfo>> GetFilteredContactsFromNameOrPhoneAsync(string charsEntered, int start, int end)
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

            List<ItemGroup<ContactInfo>> listToIterate = null;
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

                    cn.IsSelected = _frmBlockedList && IsUserBlocked(cn);
                    cn.CheckBoxVisibility = _frmBlockedList ? Visibility.Visible : Visibility.Collapsed;

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

            List<ItemGroup<ContactInfo>> list = null;
            if (areCharsNumber)
            {
                if (_glistFiltered == null || createNewFilteredList)
                {
                    if (_defaultGroupedContactList == null)
                        _defaultGroupedContactList = CreateGroups();

                    list = _defaultGroupedContactList;
                }
                else
                {
                    list = _glistFiltered;
                }

                if (list[_maxCharGroups].Contains(defaultContact))
                    list[_maxCharGroups].Remove(defaultContact);

                if (list[_maxCharGroups].Count == 0 || !list[_maxCharGroups].Contains(defaultContact))
                {
                    list[_maxCharGroups].Insert(0, defaultContact);

                    defaultContact.Msisdn = Utils.NormalizeNumber(_charsEntered);

                    charsEntered = (isPlus ? "+" : String.Empty) + charsEntered;
                    defaultContact.Name = charsEntered;
                    defaultContact.ContactListLabel = Utils.IsNumberValid(charsEntered) ? defaultContact.Msisdn : AppResources.SelectUser_EnterValidNo_Txt;

                    if (!HikeInstantiation.ViewModel.IsHiddenModeActive
                        && HikeInstantiation.ViewModel.ConvMap.ContainsKey(defaultContact.Msisdn)
                        && HikeInstantiation.ViewModel.ConvMap[defaultContact.Msisdn].IsHidden)
                        defaultContact.IsSelected = false;
                    else
                        defaultContact.IsSelected = _frmBlockedList && IsUserBlocked(defaultContact);

                    defaultContact.CheckBoxVisibility = _frmBlockedList ? Visibility.Visible : Visibility.Collapsed;
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

            HikeInstantiation.MqttManagerInstance.disconnectFromBroker(false);
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
                HikeInstantiation.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                scanningComplete();
                _canGoBack = true;
                return;
            }

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

                bool isFavUpdated = false, isPendingUpdated = false;
                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo cinfo = ContactUtils.hike_contactsMap[id][0];
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, cinfo.Msisdn);
                    hikeIds.Add(dCn);
                    deletedContacts.Add(cinfo);
                    if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(dCn.Msisdn)) // check convlist map to remove the 
                    {
                        try
                        {
                            // here we are removing name so that Msisdn will be shown instead of Name
                            HikeInstantiation.ViewModel.ConvMap[dCn.Msisdn].ContactName = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("REFRESH CONTACTS :: Delete contact exception " + e.StackTrace);
                        }
                    }
                    else // if this contact is in favourite or pending and not in convMap update this also
                    {
                        ConversationListObject obj;
                        obj = HikeInstantiation.ViewModel.GetFav(cinfo.Msisdn);

                        if (obj == null) // this msisdn is not in favs , check in pending
                        {
                            obj = HikeInstantiation.ViewModel.GetPending(cinfo.Msisdn);

                            if (obj != null)
                            {
                                obj.ContactName = null;
                                isPendingUpdated = true;
                            }
                        }
                        else
                        {
                            obj.ContactName = null;
                            MiscDBUtil.SaveFavourites(obj);
                            isFavUpdated = true;
                        }
                    }

                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(dCn.Msisdn))
                        HikeInstantiation.ViewModel.ContactsCache[dCn.Msisdn].Name = null;
                    cinfo.Name = cinfo.Msisdn;
                    GroupManager.Instance.RefreshGroupCache(cinfo, allGroupsInfo, false);
                }

                if (isFavUpdated)
                    MiscDBUtil.SaveFavourites();

                if (isPendingUpdated)
                    MiscDBUtil.SavePendingRequests();
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap);

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

            if (deletedContacts != null && deletedContacts.Count > 0)
            {
                Object[] obj = new object[2];
                obj[0] = false;//denotes deleted contact
                obj[1] = deletedContacts;
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            if (updatedContacts != null && updatedContacts.Count > 0)
            {
                UsersTableUtils.updateContacts(updatedContacts);
                ConversationTableUtils.updateConversation(updatedContacts);
                Object[] obj = new object[2];
                obj[0] = true;//denotes updated/added contact
                obj[1] = updatedContacts;
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            HikeInstantiation.ViewModel.DeleteImageForDeletedContacts(deletedContacts, updatedContacts);

            _allContactsList = UsersTableUtils.getAllContactsByGroup();
            HikeInstantiation.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            _completeGroupedContactList = GetGroupedList(_allContactsList);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                _filteredGroupedContactList = null;
                
                // this logic handles the case where hide sms contacts is there and user refreshed the list 
                if (!_showSmsContacts)
                {
                    MakeFilteredJumpList();
                    contactsListBox.ItemsSource = _filteredGroupedContactList;

                    if (_filteredGroupedContactList == null || _filteredGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    contactsListBox.ItemsSource = _completeGroupedContactList;

                    if (_completeGroupedContactList == null || _completeGroupedContactList.Where(c => c.Count > 0).Count() == 0)
                    {
                        emptyGrid.Visibility = Visibility.Visible;
                        noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                    }
                    else
                        emptyGrid.Visibility = Visibility.Collapsed;
                }

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

        private List<ItemGroup<ContactInfo>> GetGroupedList(List<ContactInfo> allContactsList)
        {
            List<ItemGroup<ContactInfo>> glist = CreateGroups();

            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
            {
                ContactInfo cInfo = allContactsList[i];

                if (cInfo.Msisdn == HikeInstantiation.MSISDN) // don't show own number in any chat.
                    continue;

                if (!HikeInstantiation.ViewModel.IsHiddenModeActive &&
                    HikeInstantiation.ViewModel.ConvMap.ContainsKey(cInfo.Msisdn) && HikeInstantiation.ViewModel.ConvMap[cInfo.Msisdn].IsHidden)
                    continue;

                cInfo.CheckBoxVisibility = _frmBlockedList ? Visibility.Visible : Visibility.Collapsed;
                cInfo.IsSelected = _frmBlockedList && IsUserBlocked(cInfo);

                string ch = GetCaptionGroup(cInfo);
                // calculate the index into the list
                int index = ((ch == "#") ? 26 : ch[0] - 'a');
                // and add the entry
                glist[index].Add(cInfo);
            }

            _maxCharGroups = glist.Count - 1;

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

        private List<ItemGroup<ContactInfo>> CreateGroups()
        {
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            List<ItemGroup<ContactInfo>> glist;
            glist = new List<ItemGroup<ContactInfo>>(_maxCharGroups);

            foreach (char c in Groups)
            {
                ItemGroup<ContactInfo> g = new ItemGroup<ContactInfo>(c.ToString());
                glist.Add(g);
            }

            return glist;
        }

        private void MakeFilteredJumpList()
        {
            _filteredGroupedContactList = CreateGroups();
            for (int i = 0; i < _completeGroupedContactList.Count; i++)
            {
                ItemGroup<ContactInfo> g = _completeGroupedContactList[i];
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
            if (cInfo != null)
            {
                int oldSmsCount = _smsUserCount;

                if (cInfo != null && !HikeInstantiation.ViewModel.IsHiddenModeActive
                    && HikeInstantiation.ViewModel.ConvMap.ContainsKey(cInfo.Msisdn) && HikeInstantiation.ViewModel.ConvMap[cInfo.Msisdn].IsHidden)
                    return;

                if (_isContactShared)
                {
                    MessageBoxResult mr = MessageBox.Show(string.Format(AppResources.ShareContact_ConfirmationText, cInfo.Name), AppResources.ShareContact_Txt, MessageBoxButton.OKCancel);
                    if (mr == MessageBoxResult.OK)
                    {
                        string searchNumber = cInfo.Msisdn;
                        string country_code = null;


                        if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.COUNTRY_CODE_SETTING, out country_code))
                            searchNumber = searchNumber.Replace(country_code, String.Empty);

                        contactInfoObj = cInfo;

                        ContactUtils.getContact(searchNumber, contactSearchCompleted_Callback);
                    }
                }
                else
                {
                    BlockUnblockUser(cInfo);
                }
            }

            enterNameTxt.Text = String.Empty;
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

        ContactInfo contactInfoObj;
        private void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            if (contactInfoObj == null)
                return;
            IEnumerable<Contact> contacts = e.Results;
            Contact contact = null;
            foreach (Contact c in contacts)
            {
                if (c.DisplayName.Trim() == contactInfoObj.Name)
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
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.CONTACT_SELECTED] = contact;
                NavigationService.GoBack();
            }
        }

        bool IsUserBlocked(ContactInfo cInfo)
        {
            if (HikeInstantiation.ViewModel.BlockedHashset.Contains(cInfo.Msisdn))
                return true;
            else
                return false;
        }

        private void Block_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            ContactInfo ci = btn.DataContext as ContactInfo;
            BlockUnblockUser(ci);
        }

        private void BlockUnblockUser(ContactInfo ci)
        {
            enterNameTxt.Text = String.Empty;

            if (ci == null)
                return;
            if (!ci.IsSelected) // block request
            {
                ci.IsSelected = true;
                if (ci.Name == ci.Msisdn)
                {
                    ci.Msisdn = Utils.NormalizeNumber(ci.Msisdn);
                    ci.Name = ci.Msisdn;
                }

                HikeInstantiation.ViewModel.BlockedHashset.Add(ci.Msisdn);

                if (HikeInstantiation.ViewModel.FavList != null)
                {
                    ConversationListObject co = new ConversationListObject();
                    co.Msisdn = ci.Msisdn;
                    if (HikeInstantiation.ViewModel.FavList.Remove(co))
                    {
                        MiscDBUtil.SaveFavourites();
                        MiscDBUtil.DeleteFavourite(ci.Msisdn);
                        int count = 0;
                        HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                        HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                    }
                }

                HikeInstantiation.ViewModel.DeleteImageForMsisdn(ci.Msisdn);

                FriendsTableUtils.SetFriendStatus(ci.Msisdn, FriendsTableUtils.FriendStatusEnum.NOT_SET);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.BLOCK_USER, ci);
            }
            else // unblock request
            {
                ci.IsSelected = false;

                if (ci.Msisdn == string.Empty)
                    ci.Msisdn = ci.Name;

                HikeInstantiation.ViewModel.BlockedHashset.Remove(ci.Msisdn);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, ci);
            }
        }

        #endregion

        #region Page State Functions

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
            PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_BLOCKED_LIST);
            PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.SHARE_CONTACT);
            base.OnRemovedFromJournal(e);
        }

        #endregion

        HashSet<string> blockedSet = null;

        bool _frmBlockedList;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cInfo = (button.DataContext) as ContactInfo;
            if (cInfo != null)
            {
                CheckUnCheckContact(cInfo);
            }
        }
    }
}