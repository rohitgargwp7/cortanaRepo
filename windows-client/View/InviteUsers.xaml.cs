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

namespace windows_client.View
{
    public partial class InviteUsers : PhoneApplicationPage
    {
        private bool _isAddToFavPage;
        private bool isClicked = false;
        public List<Group<ContactInfo>> jumpList = null; // list that will contain the complete jump list
        private List<ContactInfo> allContactsList = null; // contacts list
        private Dictionary<string, bool> contactsList = new Dictionary<string, bool>(); // this will work as a hashset and will be used in invite
        private List<ContactInfo> hikeFavList = null;
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
            object hikeFriends;
            if (PhoneApplicationService.Current.State.TryGetValue("HIKE_FRIENDS", out hikeFriends))
            {
                topHeader.Text = AppResources.Add_To_Fav_Txt;
                title.Text = AppResources.Hike_Friends_Text;
                _isAddToFavPage = true;
            }
            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                if (_isAddToFavPage)
                    allContactsList = UsersTableUtils.GetAllHikeContactsOrdered();
                else
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

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("HIKE_FRIENDS");
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
                if (_isAddToFavPage && App.ViewModel.Isfavourite(c.Msisdn))
                    c.IsFav = true;
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

        private void Invite_Or_Fav_Click(object sender, EventArgs e)
        {
            if (_isAddToFavPage)
            {
                bool isPendingRemoved = false;
                for (int i = 0; i < (hikeFavList == null ? 0 : hikeFavList.Count);i++ )
                {
                    if (!App.ViewModel.Isfavourite(hikeFavList[i].Msisdn)) // if not already favourite then only add to fav
                    {
                        ConversationListObject favObj = null;
                        if (App.ViewModel.ConvMap.ContainsKey(hikeFavList[i].Msisdn))
                            favObj = App.ViewModel.ConvMap[hikeFavList[i].Msisdn];
                        else
                            favObj = new ConversationListObject(hikeFavList[i].Msisdn, hikeFavList[i].Name, hikeFavList[i].OnHike, hikeFavList[i].Avatar);
                            
                        App.ViewModel.FavList.Insert(0, favObj);
                        if (App.ViewModel.IsPending(favObj.Msisdn)) // if this is in pending already , remove from pending and add to fav
                        {
                            App.ViewModel.PendingRequests.Remove(favObj);
                            isPendingRemoved = true;
                        }
                        int count = 0;
                        App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                        App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

                        JObject data = new JObject();
                        data["id"] = hikeFavList[i].Msisdn;
                        JObject obj = new JObject();
                        obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                        obj[HikeConstants.DATA] = data;
                        App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
                        MiscDBUtil.SaveFavourites(favObj);
                    }
                }
                MiscDBUtil.SaveFavourites();
                if (isPendingRemoved)
                    MiscDBUtil.SavePendingRequests();
                App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV_OR_PENDING, null);
            }
            else
            {
                string inviteToken = "";
                App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
                int count = 0;
                foreach (string key in contactsList.Keys)
                {
                    JObject obj = new JObject();
                    JObject data = new JObject();
                    data[HikeConstants.SMS_MESSAGE] = string.Format(AppResources.sms_invite_message, inviteToken);
                    data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                    data[HikeConstants.MESSAGE_ID] = -1;
                    obj[HikeConstants.TO] = key;
                    obj[HikeConstants.DATA] = data;
                    obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    count++;
                }
                if (count > 0)
                    MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }
            NavigationService.GoBack();
        }

        private void contactsListBox_ScrollingStarted(object sender, EventArgs e)
        {
            contactsListBox.Focus();
        }

        private void CheckBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            CheckBox c = sender as CheckBox;
            ContactInfo cn = c.DataContext as ContactInfo;
            string msisdn = cn.Msisdn;
            if ((bool)c.IsChecked) // this will be true when checkbox is not checked initially and u clicked it
            {
                if(_isAddToFavPage)
                {
                    if (hikeFavList == null)
                        hikeFavList = new List<ContactInfo>();
                    hikeFavList.Add(cn);
                }
                else
                    contactsList[msisdn] = true;
            }
            else // this will be true when checkbox is checked initially and u clicked it to make it uncheck
            {
                if (_isAddToFavPage)
                    hikeFavList.Remove(cn);
                else
                    contactsList.Remove(msisdn);
            }

            if (_isAddToFavPage)
            {
                if(hikeFavList.Count > 0)
                    doneIconButton.IsEnabled = true;
                else
                    doneIconButton.IsEnabled = false;
            }
            else
            {
                if (contactsList.Count > 0)
                    doneIconButton.IsEnabled = true;
                else
                    doneIconButton.IsEnabled = false;
            }
        }

    }
}