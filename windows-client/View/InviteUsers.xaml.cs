using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using System.ComponentModel;
using windows_client.DbUtils;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using windows_client.ViewModel;
using System.Threading;
using System.Diagnostics;
using Microsoft.Phone.Tasks;

namespace windows_client.View
{
    public partial class InviteUsers : PhoneApplicationPage
    {

        private bool _flag;
        private string _charsEntered;

        List<ItemGroup<ContactInfo>> _glistFiltered = null;
        Dictionary<string, List<ItemGroup<ContactInfo>>> _groupListDictionary = new Dictionary<string, List<ItemGroup<ContactInfo>>>();
        public List<ItemGroup<ContactInfo>> _jumpList = null; // list that will contain the complete jump list
        private List<ContactInfo> _allContactsList = null; // contacts list

        List<ContactInfo> SelectedContacts = new List<ContactInfo>();
        ContactInfo _defaultContact = new ContactInfo(); // this is used to store default phone number 

        private ApplicationBar appBar;
        private ApplicationBarIconButton _selectAllButton = null;
        private ApplicationBarIconButton _doneIconButton = null;

        string _defaultMsg = AppResources.Tap_To_Invite_Txt;
        string _pageTitle = AppResources.Invite_Friends_Txt;

        public InviteUsers()
        {
            InitializeComponent();

            initPage();

            shellProgress.IsIndeterminate = true;
            
            BackgroundWorker bw = new BackgroundWorker();
            
            bw.DoWork += (s, e) =>
            {
                _allContactsList = UsersTableUtils.getAllContactsToInvite();
            };
            
            bw.RunWorkerAsync();

            bw.RunWorkerCompleted += (s, e) =>
            {
                _jumpList = getGroupedList(_allContactsList);
                contactsListBox.ItemsSource = _jumpList;
                shellProgress.IsIndeterminate = false;

                if (_allContactsList != null && _allContactsList.Count > 0)
                {
                    _selectAllButton.IsEnabled = true;
                    emptyGrid.Visibility = Visibility.Collapsed;
                }
                else
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                }
            };
        }

        private void initPage()
        {
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            _selectAllButton = new ApplicationBarIconButton();
            _selectAllButton.IconUri = new Uri("/View/images/AppBar/appbar.select.png", UriKind.Relative);
            _selectAllButton.Text = AppResources.AppBar_Done_Btn;
            _selectAllButton.Click += selectAllButton_Click;
            _selectAllButton.IsEnabled = false;
            appBar.Buttons.Add(_selectAllButton);
           
            _doneIconButton = new ApplicationBarIconButton();
            _doneIconButton.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.Relative);
            _doneIconButton.Text = AppResources.AppBar_Done_Btn;
            _doneIconButton.Click += new EventHandler(Invite_Or_Fav_Click);
            _doneIconButton.IsEnabled = false;
            appBar.Buttons.Add(_doneIconButton);
            ApplicationBar = appBar;
        }

        void selectAllButton_Click(object sender, EventArgs e)
        {
            if (_allContactsList == null)
                return;

            _isSelectAllChecked = !_isSelectAllChecked;

            _selectAllButton.Text = _isSelectAllChecked ? AppResources.DeSelectAll_Txt : AppResources.SelectAll_Txt;

            if (_isSelectAllChecked)
            {
                foreach (ContactInfo cInfo in _allContactsList)
                {
                    if (!cInfo.IsSelected) // this will be true when checkbox is not checked initially and u clicked it
                    {
                        cInfo.IsSelected = true;

                        if (_defaultContact == cInfo)
                            _defaultContact = new ContactInfo();

                        SelectedContacts.Add(cInfo);
                    }
                }

                _doneIconButton.IsEnabled = true;
                PageTitle.Text = String.Format(AppResources.Selected_Txt, SelectedContacts.Count);
            }
            else
            {
                for (int i = 0; i < SelectedContacts.Count;)
                {
                    SelectedContacts[i].IsSelected = false;
                    SelectedContacts.RemoveAt(i);
                }

                _doneIconButton.IsEnabled = false;
                PageTitle.Text = _pageTitle;
            }

            enterNameTxt.Text = String.Empty;
        }

        #region  MAKE JUMP LIST

        private List<ItemGroup<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            List<ItemGroup<ContactInfo>> glist = CreateGroups();
            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
            {
                ContactInfo c = allContactsList[i];
                c.CheckBoxVisibility = Visibility.Visible;

                if (c.Msisdn == App.MSISDN)
                    continue;

                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Add(c);
            }
            return glist;
        }

        private List<ItemGroup<ContactInfo>> CreateGroups()
        {
            string Groups = "abcdefghijklmnopqrstuvwxyz#";
            List<ItemGroup<ContactInfo>> glist = new List<ItemGroup<ContactInfo>>(27);
            foreach (char c in Groups)
            {
                ItemGroup<ContactInfo> g = new ItemGroup<ContactInfo>(c.ToString());
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

        private void Invite_Or_Fav_Click(object sender, EventArgs e)
        {
            if (_isSelectAllChecked)
                Analytics.SendClickEvent(HikeConstants.SELECT_ALL_INVITE);

            string msisdns = string.Empty, toNum = String.Empty;
            JObject obj = new JObject();
            JArray numlist = new JArray();
            JObject data = new JObject();

            var smsString = AppResources.sms_invite_message;
            var ts = TimeUtils.getCurrentTimeStamp();

            foreach (var item in SelectedContacts)
            {
                if (item.Msisdn != App.MSISDN)
                {
                    msisdns += item.Msisdn + ";";
                    toNum = item.Msisdn;
                    numlist.Add(item.Msisdn);
                }
            }

            if (SelectedContacts.Count == 1)
            {
                obj[HikeConstants.TO] = toNum;
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.HIKE_MESSAGE] = smsString;
                data[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.INVITE;
            }
            else
            {
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.INVITE_LIST] = numlist;
                obj[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.MULTIPLE_INVITE;
            }

            if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
            {
                App.MqttManagerInstance.mqttPublishToServer(obj);

                MessageBox.Show(AppResources.InviteUsers_TotalInvitesSent_Txt, AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }
            else
            {
                obj[HikeConstants.SUB_TYPE] = HikeConstants.NO_SMS;
                App.MqttManagerInstance.mqttPublishToServer(obj);

                SmsComposeTask smsComposeTask = new SmsComposeTask();
                smsComposeTask.To = msisdns;
                smsComposeTask.Body = smsString;
                smsComposeTask.Show();
            }

            NavigationService.GoBack();
        }

        private void contactsListBox_ScrollingStarted(object sender, EventArgs e)
        {
            contactsListBox.Focus();
        }

        private ContactInfo GetContactIfExists(ContactInfo contact)
        {
            if (_glistFiltered == null)
                return contact;
            for (int i = 0; i < 26; i++)
            {
                if (_glistFiltered[i] == null || _glistFiltered[i] == null)
                    return contact;
                for (int k = 0; k < _glistFiltered[i].Count; k++)
                {
                    if (_glistFiltered[i][k].Msisdn == contact.Msisdn)
                        return _glistFiltered[i][k];
                }
            }
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
                contactsListBox.ItemsSource = _jumpList;

                if (_jumpList == null || _jumpList.Where(c => c.Count > 0).Count() == 0)
                {
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoContactsToDisplay_Txt;
                }
                else
                    emptyGrid.Visibility = Visibility.Collapsed;
                
                return;
            }

            if (_groupListDictionary.ContainsKey(_charsEntered))
            {
                List<ItemGroup<ContactInfo>> gl = _groupListDictionary[_charsEntered];

                if (gl == null)
                {
                    _groupListDictionary.Remove(_charsEntered);
                    contactsListBox.ItemsSource = null;
                    emptyGrid.Visibility = Visibility.Visible;
                    noResultTextBlock.Text = AppResources.NoSearchToDisplay_Txt; 
                    return;
                }

                if (gl[26].Count > 0 && gl[26][0].Msisdn != null)
                {
                    if (_defaultContact.IsSelected)
                    {
                        gl[26].Remove(_defaultContact);
                        _defaultContact = new ContactInfo();
                        gl[26].Insert(0, _defaultContact);
                    }
                    _defaultContact.Name = _charsEntered;
                    string num = Utils.NormalizeNumber(_charsEntered);
                    _defaultContact.Msisdn = num;
                    _defaultContact.ContactListLabel = _charsEntered.Length >= 1 && _charsEntered.Length <= 15 ? num : AppResources.SelectUser_EnterValidNo_Txt;
                    _defaultContact.IsSelected = SelectedContacts.Where(c => c.Msisdn == num).Count() > 0;
                    _defaultContact.CheckBoxVisibility = Visibility.Visible;
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

            BackgroundWorker bw = new BackgroundWorker();
            
            bw.DoWork += (s, ev) =>
            {
                _glistFiltered = GetFilteredContactsFromNameOrPhoneAsync(_charsEntered, 0, 26);
            };
            
            bw.RunWorkerAsync();

            bw.RunWorkerCompleted += (s, ev) =>
            {
                if (_glistFiltered != null)
                    _groupListDictionary[_charsEntered] = _glistFiltered;

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
                if (_groupListDictionary.ContainsKey(charsEntered.Substring(0, charsLength)))
                {
                    listToIterate = _groupListDictionary[charsEntered.Substring(0, charsEntered.Length - 1)];

                    if (listToIterate == null)
                        listToIterate = _jumpList;
                }
                else
                    listToIterate = _jumpList;
            }
            else
                listToIterate = _jumpList;

            bool createNewFilteredList = true;

            for (int i = start; i <= end; i++)
            {
                int maxJ = listToIterate == null ? 0 : (listToIterate[i] == null ? 0 : listToIterate[i].Count);
                for (int j = 0; j < maxJ; j++)
                {
                    ContactInfo cn = listToIterate[i][j];

                    cn.IsSelected = SelectedContacts.Where(c => c.Msisdn == cn.Msisdn).Count() > 0 ? true : false;
                    cn.CheckBoxVisibility = Visibility.Visible;

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
                    list = CreateGroups();
                else
                    list = _glistFiltered;

                if (list[26].Contains(_defaultContact))
                    list[26].Remove(_defaultContact);

                if (list[26].Count == 0 || !list[26].Contains(_defaultContact))
                {
                    list[26].Insert(0, _defaultContact);

                    _defaultContact.Msisdn = Utils.NormalizeNumber(_charsEntered);

                    charsEntered = (isPlus ? "+" : "") + charsEntered;
                    _defaultContact.Name = charsEntered;
                    _defaultContact.ContactListLabel = Utils.IsNumberValid(charsEntered) ? _defaultContact.Msisdn : AppResources.SelectUser_EnterValidNo_Txt;
                    _defaultContact.IsSelected = SelectedContacts.Where(c => c.Msisdn == _defaultContact.Msisdn).Count() > 0;
                    _defaultContact.CheckBoxVisibility = Visibility.Visible;
                }
            }

            if (!areCharsNumber && createNewFilteredList)
                return null;

            if (areCharsNumber)
                return list;

            return _glistFiltered;
        }

        #region Contact Select Based Functions

        bool _isSelectAllChecked = false;

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
                cInfo.IsSelected = !cInfo.IsSelected;

                if (cInfo.IsSelected) // this will be true when checkbox is not checked initially and u clicked it
                {
                    if (_defaultContact == cInfo)
                        _defaultContact = new ContactInfo();

                    SelectedContacts.Add(cInfo);
                }
                else // this will be true when checkbox is checked initially and u clicked it to make it uncheck
                {
                    var list = SelectedContacts.Where(x => x.Msisdn == cInfo.Msisdn).ToList();
                    foreach (var item in list)
                    {
                        item.IsSelected = false;
                        SelectedContacts.Remove(item);
                    }
                }

                if (SelectedContacts.Count > 0)
                {
                    _doneIconButton.IsEnabled = true;
                    PageTitle.Text = String.Format(AppResources.Selected_Txt, SelectedContacts.Count);
                }
                else
                {
                    _doneIconButton.IsEnabled = false;
                    PageTitle.Text = _pageTitle;
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

        #endregion
    }
}