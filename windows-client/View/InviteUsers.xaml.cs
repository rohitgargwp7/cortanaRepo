using System;
using System.Collections.Generic;
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

        private bool xyz;
        private string charsEntered;
        private List<Group<ContactInfo>> defaultJumpList = null;
        List<Group<ContactInfo>> glistFiltered = null;
        Dictionary<string, List<Group<ContactInfo>>> groupListDictionary = new Dictionary<string, List<Group<ContactInfo>>>();
        public List<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        private List<ContactInfo> allContactsList = null; // contacts list
        private Dictionary<string, byte> contactsList = new Dictionary<string, byte>(); // this will work as a hashset and will be used in invite
        //used value as byte so that if two contacts have same msisdn then they must be treated as two different contacts
        ContactInfo defaultContact = new ContactInfo(); // this is used to store default phone number 
        private ApplicationBar appBar;
        private ApplicationBarIconButton doneIconButton = null;
        string defaultMsg = AppResources.Tap_To_Invite_Txt;
        public class Group<T> : List<T>
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

        public InviteUsers()
        {
            InitializeComponent();

            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                allContactsList = UsersTableUtils.getAllContactsToInvite();
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {
                jumpList = getGroupedList(allContactsList);
                contactsListBox.ItemsSource = jumpList;
                shellProgress.IsVisible = false;
                if (allContactsList != null && allContactsList.Count > 0)
                    gridSelectAll.Visibility = Visibility.Visible;
            };
            initPage();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
        }

        private void initPage()
        {
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            /* Add icons */
            if (doneIconButton != null)
                return;
            doneIconButton = new ApplicationBarIconButton();
            doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            doneIconButton.Text = AppResources.AppBar_Done_Btn;
            doneIconButton.Click += new EventHandler(Invite_Or_Fav_Click);
            doneIconButton.IsEnabled = false;
            appBar.Buttons.Add(doneIconButton);
            inviteUsersPage.ApplicationBar = appBar;
        }

        #region  MAKE JUMP LIST

        private List<Group<ContactInfo>> getGroupedList(List<ContactInfo> allContactsList)
        {
            List<Group<ContactInfo>> glist = createGroups();
            for (int i = 0; i < (allContactsList != null ? allContactsList.Count : 0); i++)
            {
                ContactInfo c = allContactsList[i];
                if (c.Msisdn == App.MSISDN) // don't show own number in any chat.
                    continue;

                string ch = GetCaptionGroup(c);
                // calculate the index into the list
                int index = (ch == "#") ? 26 : ch[0] - 'a';
                // and add the entry
                glist[index].Add(c);
            }
            return glist;
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

        private void Invite_Or_Fav_Click(object sender, EventArgs e)
        {
            if ((bool)SelectAll_Chkbx.IsChecked)
                Analytics.SendClickEvent(HikeConstants.SELECT_ALL_INVITE);

            string msisdns = string.Empty, toNum = String.Empty;
            int count = 0;
            JObject obj = new JObject();
            JArray numlist = new JArray();
            JObject data = new JObject();

            foreach (string key in contactsList.Keys)
            {
                if (key != App.MSISDN)
                {
                    msisdns += key + ";";
                    toNum = key;
                    numlist.Add(key);
                }

                count++;
            }

            var smsString = AppResources.sms_invite_message;
            var ts = TimeUtils.getCurrentTimeStamp();

            if (count == 1)
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
                if (count > 0)
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

        private int checkedContactCount = 0;

        public void CheckBox_Tap(ContactInfo cn)
        {
            string msisdn;
            bool isDefaultContact = false;
            if (cn.Msisdn.Equals(defaultMsg)) // represents this is for unadded number
            {
                msisdn = Utils.NormalizeNumber(cn.Name);
                cn = GetContactIfExists(cn);
                isDefaultContact = true;
            }
            else
                msisdn = cn.Msisdn;
            if (cn.IsFav) // this will be true when checkbox is not checked initially and u clicked it
            {
                byte count;
                if (contactsList.TryGetValue(msisdn, out count) && isDefaultContact)//to ignore unsaved contact saving state
                    return;
                checkedContactCount++;
                contactsList[msisdn] = ++count;
                txtSelectedCounter.Text = string.Format("({0})", checkedContactCount);
                txtSelectedCounter.Visibility = Visibility.Visible;
            }
            else // this will be true when checkbox is checked initially and u clicked it to make it uncheck
            {
                byte count;
                if (contactsList.TryGetValue(msisdn, out count))
                {
                    checkedContactCount--;
                    if (count > 1)
                        contactsList[msisdn] = --count;
                    else
                        contactsList.Remove(msisdn);
                    if (checkedContactCount > 0)
                        txtSelectedCounter.Text = string.Format("({0})", checkedContactCount);
                    else
                        txtSelectedCounter.Visibility = Visibility.Collapsed;
                }
            }

            if (contactsList.Count > 0)
                doneIconButton.IsEnabled = true;
            else
                doneIconButton.IsEnabled = false;
        }

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
            return contact;
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            enterNameTxt.BorderBrush = UI_Utils.Instance.Black;
        }

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
                    string num = Utils.NormalizeNumber(charsEntered);
                   
                    if (charsEntered.Length >= 1 && charsEntered.Length <= 15)
                    {
                        gl[26][0].Msisdn = defaultMsg;
                    }
                    else
                    {
                        gl[26][0].Msisdn = AppResources.SelectUser_EnterValidNo_Txt;
                    }

                    if (contactsList.ContainsKey(num))
                    {
                        gl[26][0].IsFav = true;
                    }
                    else
                    {
                        gl[26][0].IsFav = false;
                    }
                }
                contactsListBox.ItemsSource = null;
                contactsListBox.ItemsSource = gl;
                Thread.Sleep(5);
                return;
            }

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
                    cn.IsFav = contactsList.ContainsKey(cn.Msisdn) ? true : false;

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
                    list[26][0].Msisdn = defaultMsg;
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

        private void Sender_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel sp = sender as StackPanel;
            if (sp.DataContext == null)
                return;

            ContactInfo cinfo = sp.DataContext as ContactInfo;
            cinfo.IsFav = !cinfo.IsFav;

        }

        bool isSelectAllChecked = false;
        private void gridSelectAll_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (allContactsList == null)
                return;

            isSelectAllChecked = !isSelectAllChecked;
            SelectAll_Chkbx.IsChecked = isSelectAllChecked;

            foreach (ContactInfo cinfo in allContactsList)
            {
                cinfo.IsFav = isSelectAllChecked;
            }
        }
    }
}