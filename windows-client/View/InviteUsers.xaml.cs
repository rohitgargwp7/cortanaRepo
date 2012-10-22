using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Text;
using windows_client.Model;
using System.ComponentModel;
using windows_client.DbUtils;
using Phone.Controls;
using windows_client.utils;
using System.Diagnostics;
using System.Threading;
using System.Net.NetworkInformation;
using Microsoft.Phone.UserData;
using Newtonsoft.Json.Linq;
using windows_client.Mqtt;

namespace windows_client.View
{
    public partial class InviteUsers : PhoneApplicationPage
    {
        private bool isClicked = false;
        public List<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        List<ContactInfo> allContactsList = null; // contacts list
        Dictionary<string, bool> inviteeList = new Dictionary<string, bool>(); // this will work as a hashset
        private ApplicationBar appBar;
        private ApplicationBarIconButton doneIconButton = null;

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
            };
            initPage();
        }

        private void initPage()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            /* Add icons */
            if (doneIconButton != null)
                return;
            doneIconButton = new ApplicationBarIconButton();
            doneIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            doneIconButton.Text = "Done";
            doneIconButton.Click += new EventHandler(sendInvite_Click);
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
                glist[index].Items.Add(c);
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

        private void sendInvite_Click(object sender, EventArgs e)
        {
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            int count = 0;
            foreach (string key in inviteeList.Keys)
            {
                JObject obj = new JObject();
                JObject data = new JObject();               
                data[HikeConstants.SMS_MESSAGE] = string.Format(App.sms_invite_message, inviteToken);
                data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                data[HikeConstants.MESSAGE_ID] = -1;
                obj[HikeConstants.TO] = key;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                App.MqttManagerInstance.mqttPublishToServer(obj);
                count++;
            }
            if(count > 0)
                MessageBox.Show("Total invites sent : "+count,"Friends Invited",MessageBoxButton.OK);
            NavigationService.GoBack();
        }

        private void contactsListBox_ScrollingStarted(object sender, EventArgs e)
        {
            contactsListBox.Focus();
        }

        private void ItemStackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel sp = (StackPanel)sender;
            CheckBox c = sp.Children[0] as CheckBox;
            StackPanel spc1 = sp.Children[1] as StackPanel;
            TextBlock tb = ((StackPanel)spc1.Children[1]).Children[1] as TextBlock;
            string msisdn = tb.Text;
            if (c.IsFocused) // in this case do the reverse thing as checkbox in this case thinks 
            {
                if (!(bool)c.IsChecked)
                {
                    c.IsChecked = false;
                    inviteeList.Remove(msisdn);
                }
                else
                {
                    c.IsChecked = true;
                    inviteeList[msisdn] = true;
                }
            }
            else
            {
                if ((bool)c.IsChecked)
                {
                    c.IsChecked = false;
                    inviteeList.Remove(msisdn);
                }
                else
                {
                    c.IsChecked = true;
                    inviteeList[msisdn] = true;
                }
            }
            if (inviteeList.Count > 0)
                doneIconButton.IsEnabled = true;
            else
                doneIconButton.IsEnabled = false;

            contactsListBox.Focus();
        }
    }
}