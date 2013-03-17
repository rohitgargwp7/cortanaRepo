using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Documents;
using Microsoft.Devices;
using Microsoft.Xna.Framework.GamerServices;
using windows_client.Misc;
using windows_client.Languages;
using windows_client.ViewModel;
using Microsoft.Phone.Net.NetworkInformation;
using System.Collections.ObjectModel;
using windows_client.Controls;
using windows_client.Controls.StatusUpdate;

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region Instances
        bool isDeleteAllChats = false;
        bool _isFavListBound = false;
        private bool firstLoad = true;
        private bool showFreeSMS = false;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
        private ApplicationBar appBar;
        private BitmapImage _avatarImageBitmap = new BitmapImage();
        ApplicationBarMenuItem delConvsMenu;
        ApplicationBarIconButton composeIconButton;

        ApplicationBarIconButton postStatusIconButton;

        ApplicationBarIconButton groupChatIconButton;
        ApplicationBarIconButton addFriendIconButton;
        
        private bool isShowFavTute = true;
        private bool isStatusMessagesLoaded = false;
        private ObservableCollection<ContactInfo> hikeContactList = new ObservableCollection<ContactInfo>(); //all hike contacts - hike friends
        #endregion

        #region Page Based Functions

        public ConversationsList()
        {
            InitializeComponent();
            initAppBar();
            initProfilePage();
            if (isShowFavTute)
                showTutorial();
            App.ViewModel.ConversationListPage = this;
            string lastStatus = "";
            App.appSettings.TryGetValue<string>(HikeConstants.LAST_STATUS, out lastStatus);
            App.appSettings.TryGetValue(HikeConstants.UNREAD_UPDATES, out _totalUnreadStatuses);
            App.appSettings.TryGetValue(HikeConstants.REFRESH_BAR, out _refreshBarCount);
            App.appSettings.TryGetValue(HikeConstants.UNREAD_FRIEND_REQUESTS, out _unreadFriendRequests);
            setNotificationCounter(RefreshBarCount + UnreadFriendRequests);
            App.RemoveKeyFromAppSettings(HikeConstants.PHONE_ADDRESS_BOOK);
        }

        private void favTutePvt_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (favTutePvt.SelectedIndex == 0)
            {
                favBox1.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                favBox0.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
            }
            else
            {
                favBox0.Fill = UI_Utils.Instance.WalkThroughSelectedColumn;
                favBox1.Fill = UI_Utils.Instance.WalkThroughUnselectedColumn;
            }
        }

        private void showTutorial()
        {
            if (App.appSettings.Contains(App.SHOW_FAVORITES_TUTORIAL))
            {
                overlay.Visibility = Visibility.Visible;
                TutorialsGrid.Visibility = Visibility.Visible;
                launchPagePivot.IsHitTestVisible = false;
            }
            else
            {
                convListPagePivot.ApplicationBar = appBar;
            }
        }

        private void dismissTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            overlay.Visibility = Visibility.Collapsed;
            TutorialsGrid.Visibility = Visibility.Collapsed;
            convListPagePivot.ApplicationBar = appBar;
            launchPagePivot.IsHitTestVisible = true;
            App.RemoveKeyFromAppSettings(App.SHOW_FAVORITES_TUTORIAL);
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (UnreadFriendRequests == 0 && RefreshBarCount == 0)
                TotalUnreadStatuses = 0;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (launchPagePivot.SelectedIndex == 3)
            {
                RefreshBarCount = 0;
                UnreadFriendRequests = 0;
            }
            this.myListBox.SelectedIndex = -1;
            this.favourites.SelectedIndex = -1;
            if (App.ViewModel.MessageListPageCollection.Count > 0)
                myListBox.ScrollIntoView(App.ViewModel.MessageListPageCollection[0]);
            App.IS_TOMBSTONED = false;
            App.APP_LAUNCH_STATE = App.LaunchState.NORMAL_LAUNCH;
            App.newChatThreadPage = null;
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (Utils.isCriticalUpdatePending())
            {
                showCriticalUpdateMessage();
            }
            if (firstLoad)
            {
                shellProgress.IsVisible = true;
                mPubSub = App.HikePubSubInstance;
                registerListeners();

                #region LOAD MESSAGES

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    LoadMessages();
                };
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(loadingCompleted);
                bw.RunWorkerAsync();

                #endregion
                App.WriteToIsoStorageSettings(HikeConstants.SHOW_GROUP_CHAT_OVERLAY, true);
                UsersTableUtils.DeleteContactsFile();
                firstLoad = false;
            }
            // this should be called only if its not first load as it will get called in first load section
            else if (App.ViewModel.MessageListPageCollection.Count == 0)
            {
                emptyScreenImage.Opacity = 1;
                emptyScreenTip.Opacity = 1;
            }
            else
            {
                emptyScreenImage.Opacity = 0;
                emptyScreenTip.Opacity = 0;
            }
            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out showFreeSMS);
            if (showFreeSMS)
            {
                freeSMSPanel.Visibility = Visibility.Visible;
            }
            else
            {
                freeSMSPanel.Visibility = Visibility.Collapsed;
            }

        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        #endregion

        #region ConvList Page

        public static void LoadMessages()
        {
            if (App.ViewModel.MessageListPageCollection == null || App.ViewModel.MessageListPageCollection.Count == 0)
            {
                return;
            }
            foreach (string key in App.ViewModel.ConvMap.Keys)
            {
                string id = key.Replace(":", "_");
                byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(id);
                App.ViewModel.ConvMap[key].Avatar = _avatar;
            }
        }

        /* This function will run on UI Thread */
        private void loadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (App.appSettings.Contains(HikeConstants.IS_NEW_INSTALLATION))
            {
                ShowLaunchMessages();
            }
            shellProgress.IsVisible = false;
            myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
            foreach (ConversationListObject convObj in App.ViewModel.ConvMap.Values)
            {
                if (convObj.ConvBoxObj != null)
                    ContextMenuService.SetContextMenu(convObj.ConvBoxObj, createConversationContextMenu(convObj));
            }

            if (App.ViewModel.MessageListPageCollection.Count == 0)
            {
                emptyScreenImage.Opacity = 1;
                emptyScreenTip.Opacity = 1;
            }
            else
            {
                emptyScreenImage.Opacity = 0;
                emptyScreenTip.Opacity = 0;
            }

            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsMenuEnabled = true;
            appBar.Opacity = 1;
            NetworkManager.turnOffNetworkManager = false;
            App.MqttManagerInstance.connect();
            if (App.appSettings.Contains(HikeConstants.IS_NEW_INSTALLATION) || App.appSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE))
            {
                Utils.requestAccountInfo();
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, Utils.deviceInforForAnalytics());
                App.RemoveKeyFromAppSettings(HikeConstants.IS_NEW_INSTALLATION);
                App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE);
            }

            // move to seperate thread later
            #region CHECK UPDATES
            //rate the app is handled within this
            checkForUpdates();
            #endregion

            postAnalytics();
        }

        private void ShowLaunchMessages()
        {
            List<ContactInfo> cl = null;
            App.appSettings.TryGetValue(HikeConstants.AppSettings.CONTACTS_TO_SHOW, out cl);
            if (cl == null)
            {
                App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.CONTACTS_TO_SHOW);
                return;
            }
            Random rnd = new Random();
            for (int i = 0; i < cl.Count; i++)
            {
                ConvMessage c = null;
                JObject j = new JObject();
                if (cl[i].OnHike)
                {
                    j[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.HIKE_USER;
                    c = new ConvMessage(ConvMessage.ParticipantInfoState.HIKE_USER, j);
                    c.Message = string.Format(rnd.Next(1, 3) == 1 ? AppResources.Conversations_MessageOnHike_Txt : AppResources.Conversations_SayHI_Txt, cl[i].Name);
                }
                else
                {
                    j[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.SMS_USER;
                    c = new ConvMessage(ConvMessage.ParticipantInfoState.SMS_USER, j);
                    c.Message = string.Format(AppResources.Conversations_OnSMS_Txt, cl[i].Name);
                }
                c.Msisdn = cl[i].Msisdn;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(c, false);
                if (convObj != null)
                {
                    //cannot use convMap here because object has pushed to map but not to ui
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                         if (convObj.ConvBoxObj == null)
                         {
                             convObj.ConvBoxObj = new ConversationBox(convObj);//context menu will bind on page load
                         }
                         else if (App.ViewModel.MessageListPageCollection.Contains(convObj.ConvBoxObj))
                         {
                             App.ViewModel.MessageListPageCollection.Remove(convObj.ConvBoxObj);
                         }
                         App.ViewModel.MessageListPageCollection.Insert(0, convObj.ConvBoxObj);
                         emptyScreenImage.Opacity = 0;
                         emptyScreenTip.Opacity = 0;
                     });
                }
            }
            App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.CONTACTS_TO_SHOW);
        }

        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;
            appBar.Opacity = 0;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            /* Add icons */
            composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/appbar.add.rest.png", UriKind.Relative);
            composeIconButton.Text = AppResources.Conversations_NewChat_AppBar_Btn;
            composeIconButton.Click += selectUserBtn_Click;
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            postStatusIconButton = new ApplicationBarIconButton();
            postStatusIconButton.IconUri = new Uri("/View/images/icon_status.png", UriKind.Relative);
            postStatusIconButton.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusIconButton.Click += new EventHandler(postStatusBtn_Click);
            postStatusIconButton.IsEnabled = true;
            //appBar.Buttons.Add(composeIconButton);

            groupChatIconButton = new ApplicationBarIconButton();
            groupChatIconButton.IconUri = new Uri("/View/images/icon_group.png", UriKind.Relative);
            groupChatIconButton.Text = AppResources.GrpChat_Txt;
            groupChatIconButton.Click += createGroup_Click;
            groupChatIconButton.IsEnabled = true;
            appBar.Buttons.Add(groupChatIconButton);

            addFriendIconButton = new ApplicationBarIconButton();
            addFriendIconButton.IconUri = new Uri("/View/images/appbar_addfriend.png", UriKind.Relative);
            addFriendIconButton.Text = AppResources.Favorites_AddMore;
            addFriendIconButton.Click += addFriend_Click;
            addFriendIconButton.IsEnabled = true;

            delConvsMenu = new ApplicationBarMenuItem();
            delConvsMenu.Text = AppResources.Conversations_DelAllChats_Txt;
            delConvsMenu.Click += new EventHandler(deleteAllConvs_Click);
            appBar.MenuItems.Add(delConvsMenu);
        }

        public static void ReloadConversations() // running on some background thread
        {
            App.MqttManagerInstance.disconnectFromBroker(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.MessageListPageCollection.Clear();
                App.ViewModel.ConvMap.Clear();
                LoadMessages();
            });

            App.MqttManagerInstance.connect();
        }

        private void btnGetSelected_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ConversationBox obj = myListBox.SelectedItem as ConversationBox;
            if (obj == null)
                return;

            ConversationListObject convListObj;
            if (!App.ViewModel.ConvMap.TryGetValue(obj.Msisdn, out convListObj))
                return;
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = convListObj;
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        #endregion

        #region LISTENERS

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
            mPubSub.addListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
            mPubSub.addListener(HikePubSub.ADD_REMOVE_FAV, this);
            mPubSub.addListener(HikePubSub.ADD_TO_PENDING, this);
            mPubSub.addListener(HikePubSub.REWARDS_TOGGLE, this);
            mPubSub.addListener(HikePubSub.REWARDS_CHANGED, this);
            mPubSub.addListener(HikePubSub.BAD_USER_PASS, this);
            mPubSub.addListener(HikePubSub.STATUS_RECEIVED, this);
            mPubSub.addListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.addListener(HikePubSub.STATUS_DELETED, this);
            mPubSub.addListener(HikePubSub.REMOVE_FRIENDS, this);
            mPubSub.addListener(HikePubSub.ADD_FRIENDS, this);
        }

        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
                mPubSub.removeListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
                mPubSub.removeListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
                mPubSub.removeListener(HikePubSub.ADD_REMOVE_FAV, this);
                mPubSub.removeListener(HikePubSub.ADD_TO_PENDING, this);
                mPubSub.removeListener(HikePubSub.REWARDS_TOGGLE, this);
                mPubSub.removeListener(HikePubSub.REWARDS_CHANGED, this);
                mPubSub.removeListener(HikePubSub.BAD_USER_PASS, this);
                mPubSub.removeListener(HikePubSub.STATUS_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
                mPubSub.removeListener(HikePubSub.STATUS_DELETED, this);
                mPubSub.removeListener(HikePubSub.REMOVE_FRIENDS, this);
                mPubSub.removeListener(HikePubSub.ADD_FRIENDS, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationList ::  removeListeners , Exception : " + ex.StackTrace);
            }
        }

        #endregion

        #region Profile Screen

        private void initProfilePage()
        {
            if (Utils.isDarkTheme())
            {
                freeSmsImage.Source = new BitmapImage(new Uri("images/free_sms_dark.png", UriKind.Relative));
                settingsImage.Source = new BitmapImage(new Uri("images/settings_dark.png", UriKind.Relative));
                privacyImage.Source = new BitmapImage(new Uri("images/privacy_dark.png", UriKind.Relative));
                helpImage.Source = new BitmapImage(new Uri("images/help_dark.png", UriKind.Relative));
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_black.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_black.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite_dark.png", UriKind.Relative));
                rewards.Source = new BitmapImage(new Uri("images/rewards_link_dark.png", UriKind.Relative));
            }
            else
            {
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_white.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_white.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite.png", UriKind.Relative));
                rewards.Source = new BitmapImage(new Uri("images/rewards_link.png", UriKind.Relative));
            }
            bool showRewards;
            if (App.appSettings.TryGetValue<bool>(HikeConstants.SHOW_REWARDS, out showRewards) && showRewards == true)
                rewardsPanel.Visibility = Visibility.Visible;

            editProfileTextBlck.Foreground = creditsTxtBlck.Foreground = rewardsTxtBlk.Foreground = UI_Utils.Instance.EditProfileForeground;

            int rew_val = 0;
            App.appSettings.TryGetValue<int>(HikeConstants.REWARDS_VALUE, out rew_val);
            if (rew_val <= 0)
                rewardsTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
            else
            {
                rewardsTxtBlk.Text = string.Format(AppResources.Rewards_Txt + " ({0})", Convert.ToString(rew_val));
                rewardsTxtBlk.Visibility = System.Windows.Visibility.Visible;
            }

            string name;
            appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name != null)
                accountName.Text = name;
            creditsTxtBlck.Text = string.Format(AppResources.SMS_Left_Txt, (int)App.appSettings[App.SMS_SETTING]);

            Stopwatch st = Stopwatch.StartNew();
            avatarImage.Source = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to fetch profile image : {0}", msec);
        }

        #endregion

        #region AppBar Button Events

        private void deleteAllConvs_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_Delete_Chats_Confirmation, AppResources.Conversations_DelAllChats_Txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            isDeleteAllChats = true;
            shellProgress.IsVisible = true;
            disableAppBar();
            NetworkManager.turnOffNetworkManager = true;
            ClearAllDB();
            App.ViewModel.ConvMap.Clear();
            App.ViewModel.MessageListPageCollection.Clear();
            emptyScreenImage.Opacity = 1;
            emptyScreenTip.Opacity = 1;
            enableAppBar();
            NetworkManager.turnOffNetworkManager = false;
            App.AnalyticsInstance.addEvent(Analytics.DELETE_ALL_CHATS);
            shellProgress.IsVisible = false;
            isDeleteAllChats = false;
        }

        private void ClearAllDB()
        {
            MessagesTableUtils.deleteAllMessages();
            ConversationTableUtils.deleteAllConversations();
            MiscDBUtil.DeleteAllAttachmentData();
            foreach (string convMsisdn in App.ViewModel.ConvMap.Keys)
            {
                if (Utils.isGroupConversation(convMsisdn))
                {
                    JObject jObj = new JObject();
                    jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
                    jObj[HikeConstants.TO] = convMsisdn;
                    App.MqttManagerInstance.mqttPublishToServer(jObj);
                }
            }
            GroupManager.Instance.GroupCache.Clear();
            GroupManager.Instance.DeleteAllGroups();
            GroupTableUtils.deleteAllGroups();
        }

        private void createGroup_Click(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.GROUP_CHAT);
            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void addFriend_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State["HIKE_FRIENDS"] = true;
            string uri = "/View/InviteUsers.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }


        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.COMPOSE);
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void deleteConversation(ConversationBox convObj)
        {
            App.ViewModel.ConvMap.Remove(convObj.Msisdn); // removed entry from map for UI
            int convs = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);

            App.ViewModel.MessageListPageCollection.Remove(convObj); // removed from observable collection

            if (App.ViewModel.MessageListPageCollection.Count == 0)
            {
                emptyScreenImage.Opacity = 1;
                emptyScreenTip.Opacity = 1;
            }

            if (Utils.isGroupConversation(convObj.Msisdn)) // if group conv , leave the group too.
            {
                JObject jObj = new JObject();
                jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
                jObj[HikeConstants.TO] = convObj.Msisdn;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);
            }
            mPubSub.publish(HikePubSub.DELETE_CONVERSATION, convObj.Msisdn);
        }

        private void inviteUsers_Click(object sender, EventArgs e)
        {
            Uri nextPage = new Uri("/View/InviteUsers.xaml", UriKind.Relative);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(nextPage);
            });
        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PivotItem pItem = e.AddedItems[0] as PivotItem;
            var panorama = pItem.Parent as Pivot;
            var selectedIndex = panorama.SelectedIndex;
            if (selectedIndex == 0)
            {
                if (!appBar.Buttons.Contains(composeIconButton))
                    appBar.Buttons.Add(composeIconButton);
                if (!appBar.Buttons.Contains(groupChatIconButton))
                    appBar.Buttons.Add(groupChatIconButton);
                if (!appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Insert(0, delConvsMenu);
                if (appBar.Buttons.Contains(addFriendIconButton))
                    appBar.Buttons.Remove(addFriendIconButton);
                if (appBar.Buttons.Contains(postStatusIconButton))
                    appBar.Buttons.Remove(postStatusIconButton);
            }
            else if (selectedIndex == 1)
            {
                if (!appBar.Buttons.Contains(composeIconButton))
                    appBar.Buttons.Add(composeIconButton);
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                if (appBar.Buttons.Contains(addFriendIconButton))
                    appBar.Buttons.Remove(addFriendIconButton);
                if (!appBar.Buttons.Contains(groupChatIconButton))
                    appBar.Buttons.Add(groupChatIconButton);
                if (appBar.Buttons.Contains(postStatusIconButton))
                    appBar.Buttons.Remove(postStatusIconButton);
            }
            else if (selectedIndex == 2) // favourite
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                //if (!appBar.Buttons.Contains(addFriendIconButton))
                //    appBar.Buttons.Add(addFriendIconButton);
                if (!appBar.Buttons.Contains(postStatusIconButton))
                    appBar.Buttons.Add(postStatusIconButton);
                if (appBar.Buttons.Contains(composeIconButton))
                    appBar.Buttons.Remove(composeIconButton);
                if (appBar.Buttons.Contains(groupChatIconButton))
                    appBar.Buttons.Remove(groupChatIconButton);
                // there will be two background workers that will independently load three sections
                #region FAVOURITES

                if (!_isFavListBound)
                {
                    _isFavListBound = true;
                    BackgroundWorker favBw = new BackgroundWorker();
                    favBw.DoWork += (sf, ef) =>
                    {
                        for (int i = 0; i < App.ViewModel.FavList.Count; i++)
                        {
                            if (App.ViewModel.ConvMap.ContainsKey(App.ViewModel.FavList[i].Msisdn))
                                App.ViewModel.FavList[i].Avatar = App.ViewModel.ConvMap[App.ViewModel.FavList[i].Msisdn].Avatar;
                            else
                            {
                                App.ViewModel.FavList[i].Avatar = MiscDBUtil.getThumbNailForMsisdn(App.ViewModel.FavList[i].Msisdn);
                            }
                        }
                        List<ContactInfo> tempHikeContactList = UsersTableUtils.GetAllHikeContacts();
                        if (hikeContactList != null)
                        {
                            int count = tempHikeContactList.Count;
                            for (int i = count - 1; i >= 0; i--)
                            {
                                if (!App.ViewModel.Isfavourite(tempHikeContactList[i].Msisdn))
                                    hikeContactList.Add(tempHikeContactList[i]);
                            }
                        }
                    };
                    favBw.RunWorkerAsync();
                    favBw.RunWorkerCompleted += (sf, ef) =>
                    {

                        hikeContactListBox.ItemsSource = hikeContactList;
                        favourites.ItemsSource = App.ViewModel.FavList;
                        if (App.ViewModel.FavList.Count > 0)
                        {
                            emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                            favourites.Visibility = System.Windows.Visibility.Visible;
                            //addFavsPanel.Opacity = 1;
                        }
                    };
                }
                #endregion
            }
            else if (selectedIndex == 3)
            {
                if (appBar.Buttons.Contains(composeIconButton))
                    appBar.Buttons.Remove(composeIconButton);
                if (appBar.Buttons.Contains(groupChatIconButton))
                    appBar.Buttons.Remove(groupChatIconButton);
                if (!appBar.Buttons.Contains(postStatusIconButton))
                    appBar.Buttons.Add(postStatusIconButton);
                if (appBar.Buttons.Contains(addFriendIconButton))
                    appBar.Buttons.Remove(addFriendIconButton);
                if (!isStatusMessagesLoaded)
                    loadStatuses();
                RefreshBarCount = 0;
                UnreadFriendRequests = 0;
            }
            if (selectedIndex != 3)
            {
                if (UnreadFriendRequests == 0 && RefreshBarCount == 0)
                    TotalUnreadStatuses = 0;
            }
        }

        #endregion

        #region PUBSUB

        public void onEventReceived(string type, object obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("ConversationsList :: OnEventReceived : Object received is null");
                return;
            }

            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConversationListObject mObj = (ConversationListObject)vals[1];
                if (mObj == null)
                    return;
                if (!isDeleteAllChats) // this is to avoid exception caused due to deleting all chats while receiving msgs
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            if (emptyScreenImage.Visibility == Visibility.Visible)
                            {
                                emptyScreenTip.Opacity = 0;
                                emptyScreenImage.Opacity = 0;
                            }
                            if (App.ViewModel.MessageListPageCollection.Count > 0)
                                myListBox.ScrollIntoView(App.ViewModel.MessageListPageCollection[0]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ConversationList ::  onEventReceived,MESSAGE_RECEIVED  , Exception : " + ex.StackTrace);
                        }
                    });
                }
                bool isVibrateEnabled = true;
                App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
                if (isVibrateEnabled)
                {
                    if (App.newChatThreadPage == null || App.newChatThreadPage.mContactNumber != mObj.Msisdn)
                    {
                        VibrateController vibrate = VibrateController.Default;
                        vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                    }
                }
            }
            #endregion
            #region SMS_CREDIT_CHANGED
            else if (HikePubSub.SMS_CREDIT_CHANGED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    creditsTxtBlck.Text = string.Format(AppResources.SMS_Left_Txt, Convert.ToString((int)obj));
                });
            }
            #endregion
            #region UPDATE_ACCOUNT_NAME
            else if (HikePubSub.UPDATE_ACCOUNT_NAME == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    accountName.Text = (string)obj;
                });
            }
            #endregion
            #region ADD OR REMOVE FAV
            else if (HikePubSub.ADD_REMOVE_FAV == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                        favourites.Visibility = System.Windows.Visibility.Visible;
                        //addFavsPanel.Opacity = 1;
                    }
                    else if (App.ViewModel.FavList.Count == 0) // remove fav
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                        favourites.Visibility = System.Windows.Visibility.Collapsed;
                        //addFavsPanel.Opacity = 0;
                    }
                });
            }
            #endregion
            #region ADD TO PENDING
            else if (HikePubSub.ADD_TO_PENDING == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ConversationListObject co = (ConversationListObject)obj;
                    if (co != null)
                    {
                        FriendRequestStatus frs = new FriendRequestStatus(co, yes_Click, no_Click);
                        if (launchPagePivot.SelectedIndex != 3)
                        {
                            UnreadFriendRequests++;
                        }
                        App.ViewModel.StatusList.Insert(0, frs);
                    }
                });
            }
            #endregion
            #region REWARDS TOGGLE
            else if (HikePubSub.REWARDS_TOGGLE == type)
            {
                bool showRewards;
                appSettings.TryGetValue(HikeConstants.SHOW_REWARDS, out showRewards);
                if (showRewards) // show rewards option
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsPanel.Visibility == System.Windows.Visibility.Collapsed)
                            rewardsPanel.Visibility = System.Windows.Visibility.Visible;
                    });
                }
                else // hide rewards option 
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsPanel.Visibility == System.Windows.Visibility.Visible)
                            rewardsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    });
                }
            }
            #endregion
            #region REWARDS CHANGED
            else if (HikePubSub.REWARDS_CHANGED == type)
            {
                int rew_val = (int)obj;
                if (rew_val <= 0) // hide value
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsTxtBlk.Visibility == System.Windows.Visibility.Visible)
                            rewardsTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                    });
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsTxtBlk.Visibility == System.Windows.Visibility.Collapsed)
                            rewardsTxtBlk.Visibility = System.Windows.Visibility.Visible;
                        rewardsTxtBlk.Text = string.Format(AppResources.Rewards_Txt + " ({0})", Convert.ToString(rew_val));
                    });
                }
            }
            #endregion
            #region BAD_USER_PASS
            else if (HikePubSub.BAD_USER_PASS == type)
            {
                try
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        App.ViewModel.ClearViewModel();
                        Uri nextPage = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                        NavigationService.Navigate(nextPage);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConversationList ::  onEventReceived,BAD_USER_PASS  , Exception : " + ex.StackTrace);
                }
            }
            #endregion
            #region STATUS UPDATE RECEIVED
            else if (HikePubSub.STATUS_RECEIVED == type)
            {
                StatusMessage sm = obj as StatusMessage;
                if (sm == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    int count = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count : 0;
                    if (sm.Msisdn == App.MSISDN)
                    {
                        App.appSettings[HikeConstants.LAST_STATUS] = sm.Message;
                        App.ViewModel.StatusList.Insert(count, StatusUpdateHelper.Instance.createStatusUIObject(sm,
                            statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap));
                    }
                    else
                    {
                        if (!sm.ShowOnTimeline)
                            return;
                        // here we have to check 2 way firendship
                        if (launchPagePivot.SelectedIndex == 3)
                        {
                            FreshStatusUpdates.Add(sm);
                        }
                        else
                        {
                            App.ViewModel.StatusList.Insert(count, StatusUpdateHelper.Instance.createStatusUIObject(sm,
                                statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap));
                        }
                        RefreshBarCount++;//persist in this.State. it will be cleared 
                    }
                    TotalUnreadStatuses++;
                });
            }
            #endregion
            #region ADD_OR_UPDATE_PROFILE
            else if (HikePubSub.ADD_OR_UPDATE_PROFILE == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   avatarImage.Source = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
               });
            }
            #endregion
            #region STATUS_DELETED
            else if (HikePubSub.STATUS_DELETED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    StatusUpdateBox sb = obj as StatusUpdateBox;
                    if (sb != null)
                    {
                        App.ViewModel.StatusList.Remove(sb);
                        if (sb.IsUnread)
                            _totalUnreadStatuses -= 1;

                        //todo:handle ui for handling zero status
                    }
                });
            }
            #endregion
            #region REMOVE_FRIENDS
            else if (HikePubSub.REMOVE_FRIENDS == type)
            {
                string msisdn;
                if (obj != null)
                {
                    msisdn = (string)obj;
                    ContactInfo c = null;
                    if (!App.ViewModel.ContactsCache.TryGetValue(msisdn, out c))
                    {
                        ConversationListObject convObj = null;
                        if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                        {
                            convObj = App.ViewModel.ConvMap[msisdn];
                            if (convObj != null && convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                            {
                                c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                                c.Avatar = convObj.Avatar;
                            }
                        }
                        else
                        {
                            c = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                            if (c != null)
                            {
                                //TODO : Use image caching
                                c.Avatar = MiscDBUtil.getThumbNailForMsisdn(msisdn);
                            }
                        }
                    }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (c != null)
                            hikeContactList.Add(c);
                    });
                }
            }
            #endregion
            #region ADD_FRIENDS
            else if (HikePubSub.ADD_FRIENDS == type)
            {
                if (obj is ContactInfo)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (obj != null)
                            hikeContactList.Remove(obj as ContactInfo);
                    });
                }
                else if (obj is string)
                {
                    string ms = obj as string;
                    if (App.ViewModel.ConvMap.ContainsKey(ms))
                    {
                        ConversationListObject co = App.ViewModel.ConvMap[ms];
                        if (co != null && co.IsOnhike && !string.IsNullOrEmpty(co.ContactName))
                        {
                            ContactInfo c = new ContactInfo(ms, co.NameToShow, co.IsOnhike);
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                hikeContactList.Remove(c);
                            });
                        }
                    }
                    else
                    {
                        ContactInfo c = UsersTableUtils.getContactInfoFromMSISDN(ms);
                        if (c != null)
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                hikeContactList.Remove(c);
                            });
                        }
                    }
                }


            }
            #endregion
        }

        #endregion

        #region CONTEXT MENUS


        private void MenuItem_Tap_Delete(object sender, GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_Delete_Chat_Confirmation, AppResources.Conversations_DelChat_Txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
            {
                return;
            }
            ConversationBox convObj = selectedListBoxItem.DataContext as ConversationBox;
            if (convObj != null)
                deleteConversation(convObj);
        }

        private void MenuItem_Tap_AddRemoveFav(object sender, GestureEventArgs e)
        {
            MenuItem menuFavourite = sender as MenuItem;
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem(menuFavourite.DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
            {
                return;
            }
            ConversationBox convBox = selectedListBoxItem.DataContext as ConversationBox;
            ConversationListObject convObj;
            if (convBox != null && App.ViewModel.ConvMap.TryGetValue(convBox.Msisdn, out convObj))
            {

                if (convObj.IsFav) // already fav , remove request
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Conversations_RemFromFav_Confirm_Txt, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                        return;
                    convObj.IsFav = false;
                    App.ViewModel.FavList.Remove(convObj);
                    JObject data = new JObject();
                    data["id"] = convObj.Msisdn;
                    JObject obj = new JObject();
                    obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
                    obj[HikeConstants.DATA] = data;
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.DeleteFavourite(convObj.Msisdn);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                    if (App.ViewModel.FavList.Count == 0)
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                        favourites.Visibility = System.Windows.Visibility.Collapsed;
                        //addFavsPanel.Opacity = 0;
                    }
                    menuFavourite.Header = AppResources.Add_To_Fav_Txt;
                    App.AnalyticsInstance.addEvent(Analytics.REMOVE_FAVS_CONTEXT_MENU_CONVLIST);

                    FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);

                    // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                    if (convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                    {

                        ContactInfo c = null;
                        if (App.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                            c = App.ViewModel.ContactsCache[convObj.Msisdn];
                        else
                            c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                        c.Avatar = convObj.Avatar;
                        hikeContactList.Add(c);
                    }
                }
                else // add to fav
                {
                    convObj.IsFav = true;

                    ContactInfo c = null;
                    if (App.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                        c = App.ViewModel.ContactsCache[convObj.Msisdn];
                    else
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                    hikeContactList.Remove(c);
                    App.ViewModel.FavList.Insert(0, convObj);
                    if (App.ViewModel.IsPending(convObj.Msisdn))
                    {
                        App.ViewModel.PendingRequests.Remove(convObj.Msisdn);
                        MiscDBUtil.SavePendingRequests();
                    }
                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.SaveFavourites(convObj);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
                    JObject data = new JObject();
                    data["id"] = convObj.Msisdn;
                    JObject obj = new JObject();
                    obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                    obj[HikeConstants.DATA] = data;
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                    if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                        favourites.Visibility = System.Windows.Visibility.Visible;
                        //addFavsPanel.Opacity = 1;
                    }
                    FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                    menuFavourite.Header = AppResources.RemFromFav_Txt;
                    App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_CONVLIST);
                }
            }
        }

        public ContextMenu createConversationContextMenu(ConversationListObject convObj)
        {
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = true;

            MenuItem menuItemDelete = new MenuItem();
            menuItemDelete.Header = AppResources.Delete_Txt;
            var glCopy = GestureService.GetGestureListener(menuItemDelete);
            glCopy.Tap += MenuItem_Tap_Delete;
            menu.Items.Add(menuItemDelete);

            if (!convObj.IsGroupChat && !Utils.IsHikeBotMsg(convObj.Msisdn)) // if its not GC and not hike bot msg then only show add to fav 
            {
                MenuItem menuItemFavourite = new MenuItem();
                if (convObj.IsFav) // if already favourite
                    menuItemFavourite.Header = AppResources.RemFromFav_Txt;
                else
                    menuItemFavourite.Header = AppResources.Add_To_Fav_Txt;

                var glFavourites = GestureService.GetGestureListener(menuItemFavourite);
                glFavourites.Tap += MenuItem_Tap_AddRemoveFav;
                menu.Items.Add(menuItemFavourite);

                if (convObj.ConvBoxObj != null)
                    convObj.ConvBoxObj.FavouriteMenuItem = menuItemFavourite;
            }
            return menu;
        }


        #endregion

        #region Emoticons
        private static Thickness imgMargin = new Thickness(0, 5, 0, 0);

        private void RichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            //TODO read message upto the length it woud be shown on screen
            var richTextBox = sender as RichTextBox;
            if (richTextBox.Tag == null)
                return;
            string messageString = richTextBox.Tag.ToString();
            Paragraph linkified = SmileyParser.Instance.LinkifyEmoticons(messageString);
            richTextBox.Blocks.Clear();
            richTextBox.Blocks.Add(linkified);
        }
        #endregion

        private void disableAppBar()
        {
            composeIconButton.IsEnabled = false;
            appBar.IsMenuEnabled = false;
        }

        private void enableAppBar()
        {
            composeIconButton.IsEnabled = true;
            appBar.IsMenuEnabled = true;
        }

        private void Notifications_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.SETTINGS);
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.Relative));
        }

        private void EditProfile_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_PROFILE] = null;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        private void FreeSMS_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.FREE_SMS);
            NavigationService.Navigate(new Uri("/View/FreeSMS.xaml", UriKind.Relative));
        }

        private void Privacy_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.PRIVACY);
            NavigationService.Navigate(new Uri("/View/Privacy.xaml", UriKind.Relative));
        }

        private void Help_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.HELP);
            NavigationService.Navigate(new Uri("/View/Help.xaml", UriKind.Relative));
        }

        private void Rewards_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                App.AnalyticsInstance.addEvent(Analytics.REWARDS);
                NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CONVERSATIONSLIST SCREEN :: Exception while navigating to SocialPages screen : " + ex.StackTrace);
            }
        }

        private void Invite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE);
            Uri nextPage = new Uri("/View/Invite.xaml", UriKind.Relative);
            try
            {
                NavigationService.Navigate(nextPage);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CONVERSATIONSLIST SCREEN :: Exception while navigating to Invite screen : " + ex.StackTrace);
            }
        }

        #region ANALYTICS
        public void postAnalytics()
        {
            long lastAnalyticsTimeStamp = -1;
            App.appSettings.TryGetValue<long>(App.LAST_ANALYTICS_POST_TIME, out lastAnalyticsTimeStamp);
            if (lastAnalyticsTimeStamp > 0 && TimeUtils.isAnalyticsTimeElapsed(lastAnalyticsTimeStamp))
            {
                JObject analyticsJson = App.AnalyticsInstance.serialize();
                if (analyticsJson != null)
                {
                    object[] publishData = new object[2];
                    publishData[0] = analyticsJson;
                    publishData[1] = 1; //qos
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, publishData);
                    App.AnalyticsInstance.clearObject();
                }
                App.WriteToIsoStorageSettings(App.LAST_ANALYTICS_POST_TIME, TimeUtils.getCurrentTimeStamp());
            }
        }

        #endregion

        #region IN APP UPDATE

        //private bool isAppEnabled = true;
        private string latestVersionString = "";

        public void checkForUpdates()
        {
            long lastTimeStamp = -1;
            App.appSettings.TryGetValue<long>(App.LAST_UPDATE_CHECK_TIME, out lastTimeStamp);

            if (lastTimeStamp == -1 || TimeUtils.isUpdateTimeElapsed(lastTimeStamp))
            {
                AccountUtils.createGetRequest(HikeConstants.UPDATE_URL, new AccountUtils.postResponseFunction(checkUpdate_Callback), false);
            }
            else
                checkForRateApp();
        }

        public void checkUpdate_Callback(JObject obj)
        {
            bool isUpdateShown = false;
            try
            {
                if (obj != null)
                {
                    string critical = obj[HikeConstants.CRITICAL].ToString();
                    string latest = obj[HikeConstants.LATEST].ToString();
                    string current = Utils.getAppVersion();
                    latestVersionString = latest;
                    string lastDismissedUpdate = "";
                    App.appSettings.TryGetValue<string>(App.LAST_DISMISSED_UPDATE_VERSION, out lastDismissedUpdate);
                    string appID = obj[HikeConstants.APP_ID].ToString();
                    if (!String.IsNullOrEmpty(appID))
                    {
                        App.WriteToIsoStorageSettings(App.APP_ID_FOR_LAST_UPDATE, appID);
                    }
                    if (Utils.compareVersion(critical, current) == 1)
                    {
                        App.WriteToIsoStorageSettings(App.LAST_CRITICAL_VERSION, critical);
                        showCriticalUpdateMessage();//critical update
                        isUpdateShown = true;
                    }
                    else if ((Utils.compareVersion(latest, current) == 1) && (String.IsNullOrEmpty(lastDismissedUpdate) ||
                        (Utils.compareVersion(latest, lastDismissedUpdate) == 1)))
                    {
                        showNormalUpdateMessage();//normal update
                        isUpdateShown = true;
                    }
                    App.WriteToIsoStorageSettings(App.LAST_UPDATE_CHECK_TIME, TimeUtils.getCurrentTimeStamp());
                }
                if (!isUpdateShown)
                {
                    checkForRateApp();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationList ::  checkUpdate_Callback , checkUpdate_Callback, Exception : " + ex.StackTrace);
            }
        }

        private void showCriticalUpdateMessage()
        {
            if (!Guide.IsVisible)
            {
                Guide.BeginShowMessageBox(AppResources.CRITICAL_UPDATE_HEADING, AppResources.CRITICAL_UPDATE_TEXT,
                     new List<string> { AppResources.Update_Txt }, 0, MessageBoxIcon.Alert,
                     asyncResult =>
                     {
                         int? returned = Guide.EndShowMessageBox(asyncResult);
                         if (returned != null && returned == 0)
                         {
                             openMarketPlace();
                         }
                         else
                         {
                             criticalUpdateMessageBoxReturned(returned);
                         }

                     }, null);
            }
        }

        private void showNormalUpdateMessage()
        {
            if (!Guide.IsVisible)
            {
                Guide.BeginShowMessageBox(AppResources.NORMAL_UPDATE_HEADING, AppResources.NORMAL_UPDATE_TEXT,
                     new List<string> { AppResources.Update_Txt, AppResources.Ignore_Txt }, 0, MessageBoxIcon.Alert,
                     asyncResult =>
                     {
                         int? returned = Guide.EndShowMessageBox(asyncResult);
                         if (returned != null && returned == 0)
                         {
                             openMarketPlace();
                         }
                         else if (returned == null || returned == 1)
                         {
                             App.WriteToIsoStorageSettings(App.LAST_DISMISSED_UPDATE_VERSION, latestVersionString);
                         }
                     }, null);
            }
        }

        private void criticalUpdateMessageBoxReturned(int? ret)
        {
            if (ret == null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    LayoutRoot.IsHitTestVisible = false;
                    appBar.IsMenuEnabled = false;
                    composeIconButton.IsEnabled = false;
                });
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            NetworkManager.turnOffNetworkManager = true;
            if (App.IS_VIEWMODEL_LOADED)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && App.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
            }
            base.OnBackKeyPress(e);
        }

        private void openMarketPlace()
        {

            //keep the code below for final. it is commented for testing
            string appID;
            App.appSettings.TryGetValue<string>(App.APP_ID_FOR_LAST_UPDATE, out appID);
            if (!String.IsNullOrEmpty(appID))
            {
                MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
                //                marketplaceDetailTask.ContentIdentifier = "c14e93aa-27d7-df11-a844-00237de2db9e";
                marketplaceDetailTask.ContentIdentifier = appID;
                marketplaceDetailTask.ContentType = MarketplaceContentType.Applications;
                try
                {
                    marketplaceDetailTask.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConversationList ::  openMarketPlace, openMarketPlace  , Exception : " + ex.StackTrace);
                }
            }
        }

        #endregion

        #region RATE THE APP
        private void checkForRateApp()
        {
            int appLaunchCount = 0;
            if (App.appSettings.TryGetValue(HikeConstants.AppSettings.APP_LAUNCH_COUNT, out appLaunchCount) && appLaunchCount > 0)
            {
                double result = Math.Log(appLaunchCount / 5f, 2);//using gp
                if (result == Math.Ceiling(result) && NetworkInterface.GetIsNetworkAvailable()) //TODO - we can use mqtt connection status. 
                //if mqtt is connected it would safe to assume that user is online.
                {
                    showRateAppMessage();
                }
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, appLaunchCount + 1);
            }
        }

        private void showRateAppMessage()
        {
            if (!Guide.IsVisible)
            {
                Guide.BeginShowMessageBox(AppResources.Love_Using_Hike_Txt, AppResources.Rate_Us_Txt,
                     new List<string> { AppResources.Rate_Now_Txt, AppResources.Ask_Me_Later_Txt }, 0, MessageBoxIcon.None,
                     asyncResult =>
                     {
                         int? returned = Guide.EndShowMessageBox(asyncResult);
                         if (returned != null)
                         {
                             if (returned == 0)
                             {
                                 MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
                                 try
                                 {
                                     marketplaceReviewTask.Show();
                                     App.appSettings.Remove(HikeConstants.AppSettings.APP_LAUNCH_COUNT);
                                 }
                                 catch (Exception ex)
                                 {
                                     Debug.WriteLine("ConversationList ::  showRateAppMessage, showRateAppMessage  , Exception : " + ex.StackTrace);
                                 }
                             }
                         }
                     }, null);
            }
        }

        #endregion

        #region FAVOURITE ZONE
        private void favourites_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (favourites.SelectedItem != null)
            {
                ConversationListObject obj = favourites.SelectedItem as ConversationListObject;
                if (obj == null)
                    return;
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = obj;
                string uri = "/View/NewChatThread.xaml";
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
        }

        private void RemoveFavourite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_RemFromFav_Confirm_Txt, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;
            if (convObj != null)
            {
                convObj.IsFav = false;
                App.ViewModel.FavList.Remove(convObj);
                JObject data = new JObject();
                data["id"] = convObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
                obj[HikeConstants.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.DeleteFavourite(convObj.Msisdn);// remove single file too
                int count = 0;
                App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);

                // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                if (convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                {

                    ContactInfo c = null;
                    if (App.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                        c = App.ViewModel.ContactsCache[convObj.Msisdn];
                    else
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                    c.Avatar = convObj.Avatar;
                    hikeContactList.Add(c);
                }
            }
            if (App.ViewModel.FavList.Count == 0)
            {
                emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                favourites.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void hikeContacts_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo c = hikeContactListBox.SelectedItem as ContactInfo;
            if (c == null)
                return;

            object objToSend;
            if (App.ViewModel.ConvMap.ContainsKey(c.Msisdn))
                objToSend = App.ViewModel.ConvMap[c.Msisdn];
            else
                objToSend = c;
            // TODO: Handle this properly
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = objToSend;
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void addToFriends_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (hikeContactListBox.SelectedItem != null)
            {
                ContactInfo contactInfo = hikeContactListBox.SelectedItem as ContactInfo;
                if (contactInfo == null)
                    return;
                JObject data = new JObject();
                data["id"] = contactInfo.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.DATA] = data;
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
                ConversationListObject cObj = new ConversationListObject(contactInfo.Msisdn, contactInfo.Name, contactInfo.OnHike, contactInfo.Avatar);
                hikeContactList.Remove(contactInfo);
                App.ViewModel.FavList.Add(cObj);
                MiscDBUtil.SaveFavourites(cObj);
                if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
                {
                    emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                    favourites.Visibility = System.Windows.Visibility.Visible;
                }
                FriendsTableUtils.SetFriendStatus(cObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
            }
        }
        #endregion

        #region TIMELINE

        private List<StatusMessage> _freshStatusUpdates;
        private List<StatusMessage> FreshStatusUpdates
        {
            set
            {
                if (_freshStatusUpdates != value)
                {
                    _freshStatusUpdates = value;
                }
            }
            get
            {
                if (App.IS_TOMBSTONED)
                {
                    _freshStatusUpdates = StatusMsgsTable.GetUnReadStatusMsgs(TotalUnreadStatuses);
                }
                else if (_freshStatusUpdates == null)
                {
                    _freshStatusUpdates = new List<StatusMessage>();
                }
                return _freshStatusUpdates;
            }
        }

        //number of unread status updates by friends
        private int _refreshBarCount = 0;
        private int RefreshBarCount
        {
            get
            {
                return _refreshBarCount;
            }
            set
            {
                if (value != _refreshBarCount)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (launchPagePivot.SelectedIndex == 3)
                        {
                            if (_refreshBarCount == 0 && value > 0)
                            {
                                refreshStatusBackground.Visibility = System.Windows.Visibility.Visible;
                                refreshStatusText.Visibility = System.Windows.Visibility.Visible;
                            }
                            else if (_refreshBarCount > 0 && value == 0)
                            {
                                refreshStatusBackground.Visibility = System.Windows.Visibility.Collapsed;
                                refreshStatusText.Visibility = System.Windows.Visibility.Collapsed;
                                FreshStatusUpdates.Clear();
                            }
                            if (refreshStatusText.Visibility == System.Windows.Visibility.Visible && value > 0)
                            {
                                if (value == 1)
                                    refreshStatusText.Text = string.Format(AppResources.Conversations_Timeline_Refresh_SingleStatus, value);
                                else
                                    refreshStatusText.Text = string.Format(AppResources.Conversations_Timeline_Refresh_Status, value);
                            }
                            setNotificationCounter(0);
                        }
                        else
                        {
                            setNotificationCounter(value + _unreadFriendRequests);
                        }
                        _refreshBarCount = value;
                        App.WriteToIsoStorageSettings(HikeConstants.REFRESH_BAR, value);
                    });
                }
            }
        }

        //number of unread statuses. It includes self updates as well i.e. refreshBarCount + self updates
        private int _totalUnreadStatuses;
        private int TotalUnreadStatuses
        {
            get
            {
                return _totalUnreadStatuses;
            }
            set
            {
                if (value != _totalUnreadStatuses)
                {
                    if (value == 0 && launchPagePivot.SelectedIndex == 3)
                    {
                        for (int i = App.ViewModel.PendingRequests.Count;
                            i < App.ViewModel.PendingRequests.Count + _totalUnreadStatuses; i++)
                        {
                            App.ViewModel.StatusList[i].IsUnread = false;
                        }
                    }
                    App.WriteToIsoStorageSettings(HikeConstants.UNREAD_UPDATES, value);
                    _totalUnreadStatuses = value;
                }
            }
        }

        //number of unread friend requests
        private int _unreadFriendRequests = 0;
        private int UnreadFriendRequests
        {
            get
            {
                return _unreadFriendRequests;
            }
            set
            {
                if (value != _unreadFriendRequests)
                {
                    _unreadFriendRequests = value;
                    setNotificationCounter(value + _refreshBarCount);
                    App.WriteToIsoStorageSettings(HikeConstants.UNREAD_FRIEND_REQUESTS, value);
                }
            }
        }

        private void setNotificationCounter(int newCounterValue)
        {
            int currentCounter = 0;
            Int32.TryParse(notificationCountTxtBlk.Text, out currentCounter);
            if (currentCounter == 0 && newCounterValue > 0)
            {
                notificationIndicator.Source = UI_Utils.Instance.NewNotificationImage;
            }
            else if (currentCounter > 0 && newCounterValue == 0)
            {
                notificationIndicator.Source = UI_Utils.Instance.NoNewNotificationImage;
                notificationCountTxtBlk.Text = "";
            }
            if (newCounterValue > 0)
                notificationCountTxtBlk.Text = newCounterValue.ToString();
        }


        private void refreshStatuses_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            for (int i = 0; i < FreshStatusUpdates.Count; i++)
            {
                App.ViewModel.StatusList.Insert(App.ViewModel.PendingRequests.Count,
                    StatusUpdateHelper.Instance.createStatusUIObject(FreshStatusUpdates[i],
                    statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap));
            }
            statusLLS.ScrollIntoView(App.ViewModel.StatusList[App.ViewModel.PendingRequests.Count]);
            RefreshBarCount = 0;
        }
        private void postStatusBtn_Click(object sender, EventArgs e)
        {
            Uri nextPage = new Uri("/View/PostStatus.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        private void yes_Click(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            FriendRequestStatus fObj = (sender as Button).DataContext as FriendRequestStatus;
            App.ViewModel.StatusList.Remove(fObj);
            FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
            if (App.ViewModel.Isfavourite(fObj.Msisdn)) // if already favourite just ignore
                return;

            ConversationListObject cObj = null;
            ContactInfo cn = null;
            if (App.ViewModel.ConvMap.ContainsKey(fObj.Msisdn))
            {
                cObj = App.ViewModel.ConvMap[fObj.Msisdn];
            }
            else
            {
                if (App.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                    cn = App.ViewModel.ContactsCache[fObj.Msisdn];
                else
                {
                    cn = UsersTableUtils.getContactInfoFromMSISDN(fObj.Msisdn);
                    App.ViewModel.ContactsCache[fObj.Msisdn] = cn;
                }
                bool onHike = cn != null ? cn.OnHike : true; // by default only hiek user can send you friend request
                cObj = new ConversationListObject(fObj.Msisdn, fObj.UserName, onHike, MiscDBUtil.getThumbNailForMsisdn(fObj.Msisdn));
            }
            hikeContactList.Remove(cn);
            App.ViewModel.FavList.Insert(0, cObj);
            App.ViewModel.PendingRequests.Remove(cObj.Msisdn);
            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            MiscDBUtil.SaveFavourites();
            MiscDBUtil.SaveFavourites(cObj);
            MiscDBUtil.SavePendingRequests();
            int count = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
            if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
            {
                emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                favourites.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void no_Click(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            FriendRequestStatus fObj = (sender as Button).DataContext as FriendRequestStatus;
            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            App.ViewModel.StatusList.Remove(fObj);
            App.ViewModel.PendingRequests.Remove(fObj.Msisdn);
            MiscDBUtil.SavePendingRequests();
            FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.IGNORED);
        }

        private void notification_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (launchPagePivot.SelectedIndex != 3)
            {
                launchPagePivot.SelectedIndex = 3;
            }
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (statusLLS.SelectedItem != null && statusLLS.SelectedItem is ImageStatusUpdate)
            {
                PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY] = (statusLLS.SelectedItem as
                    ImageStatusUpdate);
                Uri nextPage = new Uri("/View/DisplayImage.xaml", UriKind.Relative);
                NavigationService.Navigate(nextPage);
            }
        }


        //tap event of photo in status bubble
        private void statusBubblePhoto_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (statusLLS.SelectedItem != null && statusLLS.SelectedItem is StatusUpdateBox)
            {
                StatusUpdateBox sb = statusLLS.SelectedItem as StatusUpdateBox;
                if (sb == null)
                    return;
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_TIMELINE] = sb;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
            }
        }

        private void statusBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (statusLLS.SelectedItem != null && statusLLS.SelectedItem is StatusUpdateBox)
            {
                StatusUpdateBox stsBox = statusLLS.SelectedItem as StatusUpdateBox;
                if (stsBox == null)
                    return;

                if (stsBox.Msisdn == App.MSISDN)
                    return;
                if (App.ViewModel.ConvMap.ContainsKey(stsBox.Msisdn))
                    PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = App.ViewModel.ConvMap[stsBox.Msisdn];
                else
                {
                    ConversationListObject cFav = App.ViewModel.GetFav(stsBox.Msisdn);
                    if (cFav != null)
                        PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = cFav;
                    else
                    {
                        ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(stsBox.Msisdn);
                        if (contactInfo == null)
                        {
                            contactInfo = new ContactInfo();
                            contactInfo.Msisdn = stsBox.Msisdn;
                            contactInfo.OnHike = true;
                        }
                        PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = contactInfo;
                    }
                }
                NavigationService.Navigate(new Uri("/View/NewChatThread.xaml", UriKind.Relative));
            }
        }

        private void loadStatuses()
        {
            MiscDBUtil.LoadPendingRequests();
            App.ViewModel.IsPendingListLoaded = true;
            foreach (ConversationListObject co in App.ViewModel.PendingRequests.Values)
            {
                FriendRequestStatus frs = new FriendRequestStatus(co, yes_Click, no_Click);
                App.ViewModel.StatusList.Add(frs);
            }
            //TODO - MG - handle case when you receive unread status from 1 way friend. Since, we are showing only 2-way su on timeline
            //corresponding counters should be handled for eg unread count
            List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetAllStatusMsgsForTimeline();
            if (statusMessagesFromDB != null)
            {
                for (int i = 0; i < statusMessagesFromDB.Count; i++)
                {
                    if (i < TotalUnreadStatuses)
                        statusMessagesFromDB[i].IsUnread = true;
                    App.ViewModel.StatusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i],
                        statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap));
                }
            }
            this.statusLLS.ItemsSource = App.ViewModel.StatusList;
            isStatusMessagesLoaded = true;
        }

        #endregion

        //private void Button_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    PhoneApplicationService.Current.State["HIKE_FRIENDS"] = true;
        //    string uri = "/View/InviteUsers.xaml";
        //    NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        //}
    }
}