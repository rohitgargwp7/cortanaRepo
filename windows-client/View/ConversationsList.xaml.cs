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
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Devices;
using Microsoft.Xna.Framework.GamerServices;
using windows_client.Misc;
using windows_client.Languages;
using windows_client.ViewModel;
using Microsoft.Phone.Net.NetworkInformation;
using System.Collections.ObjectModel;
using windows_client.Controls;
using Coding4Fun.Phone.Controls;
using System.Windows.Media;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Phone.BackgroundAudio;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework;
using System.Windows.Threading;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using windows_client.utils.ServerTips;

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region Instances
        bool isDeleteAllChats = false;
        bool _isFavListBound = false;
        private bool firstLoad = true;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = HikeInstantiation.AppSettings;
        private ApplicationBar appBar;
        //private ApplicationBar deleteAppBar;
        private BitmapImage _avatarImageBitmap = new BitmapImage();
        ApplicationBarMenuItem muteStatusMenu;
        ApplicationBarMenuItem settingsMenu;
        ApplicationBarMenuItem profileMenu;
        ApplicationBarMenuItem rewardsMenu;
        ApplicationBarMenuItem inviteMenu;
        ApplicationBarIconButton composeIconButton;
        ApplicationBarIconButton postStatusIconButton;
        ApplicationBarIconButton groupChatIconButton;
        ApplicationBarIconButton deleteChatIconButton;
        private bool _isStatusUpdatesNotMute;
        private bool isStatusMessagesLoaded = false;
        private bool showFreeMessageOverlay;

        private ObservableCollection<ContactInfo> hikeContactList = new ObservableCollection<ContactInfo>(); //all hike contacts - hike friends

        DefaultStatus _defaultStatus;
        DefaultStatus DefaultStatus
        {
            get
            {
                if (_defaultStatus == null)
                    _defaultStatus = new DefaultStatus();
                return _defaultStatus;
            }
        }

        #endregion

        #region Page Based Functions

        DispatcherTimer _resetTimer;
        long _resetTimeSeconds;

        public ConversationsList()
        {
            InitializeComponent();
            initAppBar();

            ApplicationBar = appBar;
            //ChangeAppBarOnConvSelected();

            _totalUnreadStatuses = StatusMsgsTable.GetUnreadCount(HikeConstants.UNREAD_UPDATES);
            _refreshBarCount = StatusMsgsTable.GetUnreadCount(HikeConstants.REFRESH_BAR);
            _unreadFriendRequests = StatusMsgsTable.GetUnreadCount(HikeConstants.UNREAD_FRIEND_REQUESTS);
            setNotificationCounter(RefreshBarCount + UnreadFriendRequests + ProTipCount);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.PHONE_ADDRESS_BOOK);

            if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                this.Loaded += ConversationsList_Loaded;

            ProTipHelper.Instance.ShowProTip -= Instance_ShowProTip;
            ProTipHelper.Instance.ShowProTip += Instance_ShowProTip;

            TipManager.Instance.ConversationPageTipChanged -= Instance_ShowServerTip;
            TipManager.Instance.ConversationPageTipChanged += Instance_ShowServerTip;

            HikeInstantiation.ViewModel.StatusNotificationsStatusChanged -= ViewModel_statusNotificationsStatusChanged;
            HikeInstantiation.ViewModel.StatusNotificationsStatusChanged += ViewModel_statusNotificationsStatusChanged;

            if (ProTipHelper.CurrentProTip != null)
                showProTip();

            int tipCount;
            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.PRO_TIP_COUNT, out tipCount);
            ProTipCount = tipCount;

            HikeInstantiation.ViewModel.ShowTypingNotification += ShowTypingNotification;
            HikeInstantiation.ViewModel.AutohideTypingNotification += AutoHidetypingNotification;

            appSettings.TryGetValue(HikeConstants.AppSettings.ACCOUNT_NAME, out _userName);

            _firstName = Utils.GetFirstName(_userName);
            string password = HikeInstantiation.ViewModel.Password;
            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.HIDDEN_MODE_PASSWORD, out password);
            HikeInstantiation.ViewModel.Password = password;
            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS, out _tipMode);

            HikeInstantiation.ViewModel.StartResetHiddenModeTimer += ViewModel_ResetHiddenModeClicked;
        }

        string _firstName;
        string _userName;

        void ViewModel_statusNotificationsStatusChanged(object sender, EventArgs e)
        {
            byte statusSettingsValue;
            HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.STATUS_UPDATE_SETTING, out statusSettingsValue);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                muteStatusMenu.Text = statusSettingsValue > 0 ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;
            });
        }

        void Instance_ShowProTip(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    showProTip();
                });
        }

        void Instance_ShowServerTip(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ShowServerTips();
            });
        }

        private void ConversationsList_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.Loaded -= ConversationsList_Loaded;
            launchPagePivot.SelectedIndex = 2;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            if (UnreadFriendRequests == 0 && RefreshBarCount == 0)
                TotalUnreadStatuses = 0;

            if (launchPagePivot.SelectedIndex != 1)
            {
                contactsCollectionView.Source = null;
                favCollectionView.Source = null;
            }

            if (launchPagePivot.SelectedIndex != 2)
                statusLLS.ItemsSource = null;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.NavigationKeys.GO_TO_CONV_VIEW))
            {
                launchPagePivot.SelectedIndex = 0;
                PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.GO_TO_CONV_VIEW);
            }

            if (launchPagePivot.SelectedIndex == 2)
                TotalUnreadStatuses = 0;

            this.llsConversations.SelectedItem = null;
            this.statusLLS.SelectedItem = null;

            HikeInstantiation.IsTombstoneLaunch = false;
            HikeInstantiation.NewChatThreadPageObj = null;

            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (firstLoad)
            {
                shellProgress.IsIndeterminate = true;
                mPubSub = HikeInstantiation.HikePubSubInstance;
                registerListeners();

                #region LOAD MESSAGES

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    LoadMessages();
                };

                bw.RunWorkerCompleted += (ss, ee) =>
                    {
                        loadingCompleted();
                        if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE))
                            ShowAppUpdateAvailableMessage();
                        else
                            ShowInvitePopups();
                    };

                bw.RunWorkerAsync();

                #endregion

                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.SHOW_GROUP_CHAT_OVERLAY, true);
                firstLoad = false;
            }
            // this should be called only if its not first load as it will get called in first load section
            else if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
            {
                ShowFTUECards();
                UpdateLayout();
            }
            else
            {
                ShowChats();

                if (HikeInstantiation.ViewModel.IsConversationUpdated && HikeInstantiation.ViewModel.MessageListPageCollection[0] != null)
                {
                    try
                    {
                        llsConversations.ScrollTo(HikeInstantiation.ViewModel.MessageListPageCollection[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("llsConversations Scroll to null Exception :: OnNavigatedTo");
                    }
                    HikeInstantiation.ViewModel.IsConversationUpdated = false;
                }
            }

            byte statusSettingsValue;
            _isStatusUpdatesNotMute = HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.STATUS_UPDATE_SETTING, out statusSettingsValue) && statusSettingsValue == 0;

            if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                launchPagePivot.SelectedIndex = 2;

            if (!conversationPageToolTip.IsShow) // dont show reset if its already being shown
            {
                ShowHiddenModeResetToolTip();
            }
            else if (_tipMode == ToolTipMode.RESET_HIDDEN_MODE)
                UpdateResetHiddenModeTimer();

            #region server Tips

            if (_tipMode == ToolTipMode.DEFAULT && TipManager.ConversationPageTip != null)
                ShowServerTips();

            #endregion

            FrameworkDispatcher.Update();
        }

        private async void BindFriendsAsync()
        {
            contactGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            cohProgressBar.Visibility = Visibility.Visible;

            txtCircleOfFriends.Visibility = Visibility.Collapsed;
            txtContactsOnHike.Visibility = Visibility.Collapsed;
            emptyListPlaceholderFiends.Visibility = Visibility.Collapsed;
            favourites.Visibility = Visibility.Collapsed;
            emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
            hikeContactListBox.Visibility = Visibility.Collapsed;

            //Await aync are used so that the UI thread is not blocked by the below binding computation.
            await Task.Delay(500);

            contactsCollectionView.Source = hikeContactList;
            favCollectionView.Source = HikeInstantiation.ViewModel.FavList;

            contactGrid.RowDefinitions[0].Height = GridLength.Auto;
            cohProgressBar.Visibility = Visibility.Collapsed;

            txtCircleOfFriends.Visibility = Visibility.Visible;
            txtContactsOnHike.Visibility = Visibility.Visible;

            favourites.Visibility = HikeInstantiation.ViewModel.FavList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            emptyListPlaceholderFiends.Visibility = HikeInstantiation.ViewModel.FavList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            emptyListPlaceholderHikeContacts.Visibility = hikeContactList.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            hikeContactListBox.Visibility = hikeContactList.Count == 0 ? Visibility.Collapsed : Visibility.Visible;

            this.favourites.SelectedIndex = -1;
            this.hikeContactListBox.SelectedIndex = -1;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
            if (launchPagePivot.SelectedIndex == 2) //if user quits app from timeline when a few statuses were shown as unread
                TotalUnreadStatuses = RefreshBarCount;  //and new statuses arrived in refresh bar
        }

        #region STATUS UPDATE TUTORIAL

        //private void DismissStatusUpdateTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    RemoveStatusUpdateTutorial();
        //}

        //private void RemoveStatusUpdateTutorial()
        //{
        //    overlay.Tap -= DismissStatusUpdateTutorial_Tap;
        //    overlay.Visibility = Visibility.Collapsed;
        //    TutorialStatusUpdate.Visibility = Visibility.Collapsed;
        //    launchPagePivot.IsHitTestVisible = true;
        //    HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.SHOW_STATUS_UPDATES_TUTORIAL);
        //}

        #endregion

        #endregion

        #region ConvList Page

        public static void LoadMessages()
        {
            if (HikeInstantiation.ViewModel.MessageListPageCollection == null || HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0)
            {
                return;
            }
            foreach (string key in HikeInstantiation.ViewModel.ConvMap.Keys)
            {
                string id = key.Replace(":", "_");
                byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(id);
                HikeInstantiation.ViewModel.ConvMap[key].Avatar = _avatar;
            }
        }

        /* This function will run on UI Thread */
        private void loadingCompleted()
        {
            shellProgress.IsIndeterminate = false;
            llsConversations.ItemsSource = HikeInstantiation.ViewModel.MessageListPageCollection;

            if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                ShowFTUECards();
            else
                ShowChats();

            appBar.IsMenuEnabled = true;

            if (settingsMenu != null)
                settingsMenu.IsEnabled = true;

            if (profileMenu != null)
                profileMenu.IsEnabled = true;

            if (inviteMenu != null)
                inviteMenu.IsEnabled = true;

            if (rewardsMenu != null)
                rewardsMenu.IsEnabled = true;

            if (!PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                NetworkManager.turnOffNetworkManager = false;

            HikeInstantiation.MqttManagerInstance.connect();

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.IS_NEW_INSTALLATION) || HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE))
            {
                if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.IS_NEW_INSTALLATION))
                    Utils.RequestHikeBot();
                else
                    Utils.requestAccountInfo();

                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, Utils.deviceInforForAnalytics());
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.IS_NEW_INSTALLATION);
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE);
            }
        }

        int _usersOnHike;

        private void ShowFTUECards()
        {
            if (emptyScreenGrid.Visibility == Visibility.Collapsed)
                emptyScreenGrid.Visibility = Visibility.Visible;

            if (llsConversations.Visibility == Visibility.Visible)
                llsConversations.Visibility = Visibility.Collapsed;

            if (_tipMode != null)
                conversationPageToolTip.Visibility = Visibility.Collapsed;

            if (profileFTUECard.Visibility == Visibility.Visible && MiscDBUtil.HasCustomProfileImage(HikeInstantiation.MSISDN))
                profileFTUECard.Visibility = Visibility.Collapsed;

            if (String.IsNullOrEmpty(groupCountCard.Text))
                groupCountCard.Text = String.Format(AppResources.Conversations_FTUE_Group_SubTxt, HikeConstants.MAX_GROUP_MEMBER_SIZE);

            if (h2oFTUECard.Visibility == Visibility.Collapsed)
            {
                bool showFreeSMS = true;
                HikeInstantiation.AppSettings.TryGetValue<bool>(HikeConstants.AppSettings.SHOW_FREE_SMS_SETTING, out showFreeSMS);
                if (showFreeSMS && HikeInstantiation.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))
                    h2oFTUECard.Visibility = Visibility.Visible;
            }

            if (peopleOnHikeListBox.ItemsSource == null)
            {
                List<ContactInfo> cl = null;
                List<string> contacts = null;
                HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.CONTACTS_TO_SHOW, out contacts);

                if (contacts == null)
                    return;

                cl = new List<ContactInfo>();
                ContactInfo cn;
                foreach (var msisdn in contacts)
                {
                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(msisdn))
                        cn = HikeInstantiation.ViewModel.ContactsCache[msisdn];
                    else
                    {
                        cn = UsersTableUtils.getHikeContactInfoFromMSISDN(msisdn);
                        if (cn != null)
                            HikeInstantiation.ViewModel.ContactsCache[msisdn] = cn;
                    }

                    if (cn != null)
                        cl.Add(cn);

                    if (cl.Count >= 4)
                        break;
                }

                peopleOnHikeListBox.ItemsSource = cl;
            }

            var list = peopleOnHikeListBox.ItemsSource as IEnumerable<ContactInfo>;
            if (list != null)
            {
                _usersOnHike = UsersTableUtils.getHikeContactCount();
                _usersOnHike = _usersOnHike < list.Count() ? list.Count() : _usersOnHike;

                if (_usersOnHike != 0)
                {
                    peopleOnHikeText.Text = String.Format(AppResources.Conversations_Empty_PeopleOnHike_Txt, _firstName, _usersOnHike);
                    peopleOnHikeBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    peopleOnHikeBorder.Visibility = Visibility.Collapsed;
                }
            }
            else
                peopleOnHikeBorder.Visibility = Visibility.Collapsed;
        }

        private void initAppBar()
        {
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["AppBarBackground"]).Color,
                Opacity = 0.95
            };

            appBar.StateChanged += appBar_StateChanged;

            /* Add icons */
            groupChatIconButton = new ApplicationBarIconButton();
            groupChatIconButton.IconUri = new Uri("/View/images/AppBar/icon_group_chat.png", UriKind.Relative);
            groupChatIconButton.Text = AppResources.NewGrpChat_Txt;
            groupChatIconButton.Click += createGroup_Click;
            groupChatIconButton.IsEnabled = true;
            appBar.Buttons.Add(groupChatIconButton);

            composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/AppBar/icon_message.png", UriKind.Relative);
            composeIconButton.Text = AppResources.Conversations_NewChat_AppBar_Btn;
            composeIconButton.Click += selectUserBtn_Click;
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            postStatusIconButton = new ApplicationBarIconButton();
            postStatusIconButton.IconUri = new Uri("/View/images/AppBar/icon_status.png", UriKind.Relative);
            postStatusIconButton.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusIconButton.Click += new EventHandler(postStatusBtn_Click);
            postStatusIconButton.IsEnabled = true;
            appBar.Buttons.Add(postStatusIconButton);

            muteStatusMenu = new ApplicationBarMenuItem();
            byte statusSettingsValue;
            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.STATUS_UPDATE_SETTING, out statusSettingsValue)) // settings dont exist on new sign up, hence on by default
                statusSettingsValue = (byte)1;
            muteStatusMenu.Text = statusSettingsValue > 0 ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;
            muteStatusMenu.Click += muteStatusMenu_Click;

            inviteMenu = new ApplicationBarMenuItem();
            inviteMenu.Text = AppResources.Conversations_TellFriend_Txt;
            inviteMenu.Click += inviteMenu_Click;
            inviteMenu.IsEnabled = false;//it will be enabled after loading of all conversations
            appBar.MenuItems.Add(inviteMenu);

            bool showRewards;
            if (HikeInstantiation.AppSettings.TryGetValue<bool>(HikeConstants.ServerJsonKeys.SHOW_REWARDS, out showRewards) && showRewards == true)
            {
                rewardsMenu = new ApplicationBarMenuItem();
                rewardsMenu.Text = AppResources.ConversationsList_Rewards_Txt;
                rewardsMenu.Click += rewardsMenu_Click;
                rewardsMenu.IsEnabled = false;//it will be enabled after loading of all conversations
                appBar.MenuItems.Add(rewardsMenu);
            }

            profileMenu = new ApplicationBarMenuItem();
            profileMenu.Text = AppResources.Profile_Txt;
            profileMenu.Click += profileMenu_Click;
            profileMenu.IsEnabled = false;//it will be enabled after loading of all conversations
            appBar.MenuItems.Add(profileMenu);

            settingsMenu = new ApplicationBarMenuItem();
            settingsMenu.Text = AppResources.Settings;
            settingsMenu.Click += settingsMenu_Click;
            settingsMenu.IsEnabled = false;//it will be enabled after loading of all conversations
            appBar.MenuItems.Add(settingsMenu);

            //deleteAppBar = new ApplicationBar();
            //deleteChatIconButton = new ApplicationBarIconButton();
            //deleteChatIconButton.IconUri = new Uri("/View/images/AppBar/appbar.delete.png", UriKind.Relative);
            //deleteChatIconButton.Text = AppResources.Delete_Txt;
            //deleteChatIconButton.Click += deleteChatIconButton_Click;
            //deleteChatIconButton.IsEnabled = true;
            //deleteAppBar.Buttons.Add(deleteChatIconButton);

            appBar.IsMenuEnabled = false;
        }

        void rewardsMenu_Click(object sender, EventArgs e)
        {
            // do not open rewards if rewards token not recieved yet.
            if (!HikeInstantiation.AppSettings.Contains(HikeConstants.ServerJsonKeys.REWARDS_TOKEN))
                return;

            try
            {
                NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CONVERSATIONSLIST SCREEN :: Exception while navigating to SocialPages screen : " + ex.StackTrace);
            }
        }

        void inviteMenu_Click(object sender, EventArgs e)
        {
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

        void appBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible)
                ApplicationBar.Opacity = 1;
            else
                ApplicationBar.Opacity = 0.95;
        }

        void muteStatusMenu_Click(object sender, EventArgs e)
        {
            MessageBox.Show(_isStatusUpdatesNotMute ? AppResources.Unmute_Success_Txt : AppResources.Mute_Success_Txt, AppResources.StatusNotToggle_Caption_Txt, MessageBoxButton.OK);
            int settingsValue = 0;

            if (_isStatusUpdatesNotMute)
            {

                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.STATUS_UPDATE_SETTING, (byte)1);
                settingsValue = 0;
            }
            else
            {
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.STATUS_UPDATE_SETTING, (byte)0);
                settingsValue = -1;
            }

            muteStatusMenu.Text = _isStatusUpdatesNotMute ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;

            _isStatusUpdatesNotMute = !_isStatusUpdatesNotMute;

            JObject obj = new JObject();
            obj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.ServerJsonKeys.PUSH_SU, settingsValue);
            obj.Add(HikeConstants.ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        void profileMenu_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_PROFILE] = null;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        void settingsMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.Relative));
        }

        //void deleteChatIconButton_Click(object sender, EventArgs e)
        //{
        //    var list = HikeInstantiation.ViewModel.MessageListPageCollection.Where(c => c.IsSelected == true).ToList();
        //    string message = list.Count > 1 ? AppResources.Conversations_Delete_MoreThan1Chat_Confirmation : AppResources.Conversations_Delete_Chat_Confirmation;

        //    MessageBoxResult result = MessageBox.Show(message, AppResources.Conversations_DelChat_Txt, MessageBoxButton.OKCancel);
        //    if (result != MessageBoxResult.OK)
        //    {
        //        foreach (var item in list)
        //            item.IsSelected = false;

        //        //ChangeAppBarOnConvSelected();

        //        return;
        //    }

        //    for (int i = 0; i < HikeInstantiation.ViewModel.MessageListPageCollection.Count;)
        //    {
        //        if (HikeInstantiation.ViewModel.MessageListPageCollection[i].IsSelected)
        //        {
        //            var conv = HikeInstantiation.ViewModel.MessageListPageCollection[i];
        //            HikeInstantiation.ViewModel.MessageListPageCollection.RemoveAt(i);
        //            deleteConversation(conv);
        //            continue;
        //        }

        //        i++;
        //    }

        //    ChangeAppBarOnConvSelected();
        //}

        public static void ReloadConversations() // running on some background thread
        {
            HikeInstantiation.MqttManagerInstance.disconnectFromBroker(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                HikeInstantiation.ViewModel.MessageListPageCollection.Clear();
                HikeInstantiation.ViewModel.ConvMap.Clear();
                LoadMessages();
            });

            HikeInstantiation.MqttManagerInstance.connect();
        }

        #endregion

        #region LISTENERS

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
            mPubSub.addListener(HikePubSub.ADD_REMOVE_FAV, this);
            mPubSub.addListener(HikePubSub.ADD_TO_PENDING, this);
            mPubSub.addListener(HikePubSub.BAD_USER_PASS, this);
            mPubSub.addListener(HikePubSub.STATUS_RECEIVED, this);
            mPubSub.addListener(HikePubSub.STATUS_DELETED, this);
            mPubSub.addListener(HikePubSub.REMOVE_FRIENDS, this);
            mPubSub.addListener(HikePubSub.ADD_FRIENDS, this);
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.DELETE_STATUS_AND_CONV, this);
            mPubSub.addListener(HikePubSub.CONTACT_ADDED, this);
            mPubSub.addListener(HikePubSub.ADDRESSBOOK_UPDATED, this);
            mPubSub.addListener(HikePubSub.APP_UPDATE_AVAILABLE, this);
            mPubSub.addListener(HikePubSub.REWARDS_TOGGLE, this);

        }

        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
                mPubSub.removeListener(HikePubSub.ADD_REMOVE_FAV, this);
                mPubSub.removeListener(HikePubSub.ADD_TO_PENDING, this);
                mPubSub.removeListener(HikePubSub.BAD_USER_PASS, this);
                mPubSub.removeListener(HikePubSub.STATUS_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.STATUS_DELETED, this);
                mPubSub.removeListener(HikePubSub.REMOVE_FRIENDS, this);
                mPubSub.removeListener(HikePubSub.ADD_FRIENDS, this);
                mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
                mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
                mPubSub.removeListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
                mPubSub.removeListener(HikePubSub.DELETE_STATUS_AND_CONV, this);
                mPubSub.removeListener(HikePubSub.CONTACT_ADDED, this);
                mPubSub.removeListener(HikePubSub.ADDRESSBOOK_UPDATED, this);
                mPubSub.removeListener(HikePubSub.APP_UPDATE_AVAILABLE, this);
                mPubSub.removeListener(HikePubSub.REWARDS_TOGGLE, this);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationList ::  removeListeners , Exception : " + ex.StackTrace);
            }
        }

        #endregion

        #region Profile Screen

        //private void initProfilePage()
        //{
        //    bool showRewards;
        //    if (HikeInstantiation.appSettings.TryGetValue<bool>(HikeConstants.SHOW_REWARDS, out showRewards) && showRewards == true)
        //        rewardsPanel.Visibility = Visibility.Visible;

        //    int rew_val = 0;

        //    string name;
        //    appSettings.TryGetValue(HikeInstantiation.ACCOUNT_NAME, out name);
        //    if (name != null)
        //        accountName.Text = name;
        //    int smsCount = 0;
        //    HikeInstantiation.appSettings.TryGetValue<int>(HikeInstantiation.SMS_SETTING, out smsCount);
        //    creditsTxtBlck.Text = string.Format(AppResources.SMS_Left_Txt, smsCount);

        //    Stopwatch st = Stopwatch.StartNew();
        //    avatarImage.Source = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
        //    st.Stop();
        //    long msec = st.ElapsedMilliseconds;
        //    Debug.WriteLine("Time to fetch profile image : {0}", msec);
        //}

        #endregion

        #region AppBar Button Events

        private void createGroup_Click(object sender, EventArgs e)
        {
            //if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            //{
            //    RemoveStatusUpdateTutorial();
            //    return;
            //}

            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.START_NEW_GROUP] = true;
            NavigationService.Navigate(new Uri("/View/NewGroup.xaml", UriKind.Relative));
        }

        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            //if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            //{
            //    RemoveStatusUpdateTutorial();
            //    return;
            //}

            Analytics.SendClickEvent(HikeConstants.NEW_CHAT_FROM_TOP_BAR);

            NavigationService.Navigate(new Uri("/View/ForwardTo.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Delete conversation
        /// </summary>
        /// <param name="convObj">Conversation object to be deleted</param>
        /// <param name="sendHiddenToggledPacket">send hidden mode toggled packet to server</param>
        void DeleteConversation(ConversationListObject convObj, bool sendHiddenChatToogledPacket)
        {
            // Remove entry from map for UI.
            HikeInstantiation.ViewModel.ConvMap.Remove(convObj.Msisdn);

            // Removed from observable collection.
            HikeInstantiation.ViewModel.MessageListPageCollection.Remove(convObj);

            if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                ShowFTUECards();

            // If group conversation, send group leave packet too.
            if (Utils.isGroupConversation(convObj.Msisdn))
            {
                JObject jObj = new JObject();
                jObj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_LEAVE;
                jObj[HikeConstants.ServerJsonKeys.TO] = convObj.Msisdn;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);
            }

            mPubSub.publish(HikePubSub.DELETE_CONVERSATION, convObj.Msisdn);

            if (sendHiddenChatToogledPacket && convObj.IsHidden)
            {
                HikeInstantiation.ViewModel.SendRemoveStealthPacket(convObj);
            }
        }

        bool isContactListLoaded = false;
        int _oldIndex = 0, _newIndex = 0;
        long lastStatusId = -1;
        bool hasMoreStatus;
        int currentlyLoadedStatusCount = 0;
        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _oldIndex = _newIndex;
            _newIndex = (sender as Pivot).SelectedIndex;

            if (_newIndex != 2 && _oldIndex == 2 && RefreshBarCount > 0)
                UpdatePendingStatusFromRefreshBar();

            if (_newIndex == 0)
            {
                if (appBar.MenuItems.Contains(muteStatusMenu))
                    appBar.MenuItems.Remove(muteStatusMenu);
            }
            else if (_newIndex == 1) // favourite
            {
                if (appBar.MenuItems.Contains(muteStatusMenu))
                    appBar.MenuItems.Remove(muteStatusMenu);

                if (_tipMode == ToolTipMode.FAVOURITES)
                    HideTips();

                // there will be two background workers that will independently load three sections
                #region FAVOURITES

                if (!_isFavListBound)
                {
                    _isFavListBound = true;
                    BackgroundWorker favBw = new BackgroundWorker();
                    cohProgressBar.Visibility = Visibility.Visible;
                    favBw.DoWork += (sf, ef) =>
                    {
                        for (int i = 0; i < HikeInstantiation.ViewModel.FavList.Count; i++)
                        {
                            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(HikeInstantiation.ViewModel.FavList[i].Msisdn))
                                HikeInstantiation.ViewModel.FavList[i].Avatar = HikeInstantiation.ViewModel.ConvMap[HikeInstantiation.ViewModel.FavList[i].Msisdn].Avatar;
                            else
                            {
                                HikeInstantiation.ViewModel.FavList[i].Avatar = MiscDBUtil.getThumbNailForMsisdn(HikeInstantiation.ViewModel.FavList[i].Msisdn);
                            }
                        }
                        List<ContactInfo> tempHikeContactList = UsersTableUtils.GetAllHikeContactsOrdered();
                        if (tempHikeContactList != null)
                        {
                            HashSet<string> msisdns = new HashSet<string>(); // used to remove duplicate contacts
                            int count = tempHikeContactList.Count;
                            // this loop will filter out already added fav and blocked contacts from hike user list
                            for (int i = count - 1; i >= 0; i--)
                            {
                                ContactInfo cinfoTemp = tempHikeContactList[i];

                                // if user is not fav and is not blocked then add to hike contacts
                                if (!msisdns.Contains(cinfoTemp.Msisdn) && !HikeInstantiation.ViewModel.Isfavourite(cinfoTemp.Msisdn) && !HikeInstantiation.ViewModel.BlockedHashset.Contains(cinfoTemp.Msisdn) && cinfoTemp.Msisdn != HikeInstantiation.MSISDN)
                                {
                                    msisdns.Add(cinfoTemp.Msisdn);
                                    hikeContactList.Add(cinfoTemp);
                                    if (!HikeInstantiation.ViewModel.ContactsCache.ContainsKey(cinfoTemp.Msisdn))
                                        HikeInstantiation.ViewModel.ContactsCache[cinfoTemp.Msisdn] = cinfoTemp;
                                }
                            }
                        }
                    };
                    favBw.RunWorkerAsync();
                    favBw.RunWorkerCompleted += (sf, ef) =>
                    {
                        isContactListLoaded = true;
                        contactGrid.RowDefinitions[0].Height = GridLength.Auto;
                        cohProgressBar.Visibility = Visibility.Collapsed;
                        contactsCollectionView.Source = hikeContactList;
                        favCollectionView.Source = HikeInstantiation.ViewModel.FavList; // this is done to sort in view
                        favourites.SelectedIndex = -1;
                        hikeContactListBox.SelectedIndex = -1;

                        UpdateFriendsCounter();

                        txtCircleOfFriends.Visibility = Visibility.Visible;
                        UpdateContactsOnHikeCounter();
                        txtContactsOnHike.Visibility = Visibility.Visible;
                        if (HikeInstantiation.ViewModel.FavList.Count > 0)
                        {
                            emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                            favourites.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                            emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;

                        if (hikeContactList.Count == 0)
                        {
                            emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                            hikeContactListBox.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                            hikeContactListBox.Visibility = Visibility.Visible;
                        }
                    };
                }
                else if (favCollectionView.Source == null)
                {
                    BindFriendsAsync();
                }
                #endregion
            }
            else if (_newIndex == 2)
            {
                ProTipCount = 0;

                if (!appBar.MenuItems.Contains(muteStatusMenu))
                    appBar.MenuItems.Insert(0, muteStatusMenu);

                if (!isStatusMessagesLoaded)
                {
                    List<StatusMessage> statusMessagesFromDBUnblocked = new List<StatusMessage>();
                    BackgroundWorker statusBw = new BackgroundWorker();
                    statusBw.DoWork += (sf, ef) =>
                    {
                        HikeInstantiation.ViewModel.LoadPendingRequests();
                        //corresponding counters should be handled for eg unread count
                        statusMessagesFromDBUnblocked = GetUnblockedStatusUpdates(HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                    };
                    statusBw.RunWorkerAsync();
                    shellProgress.IsIndeterminate = true;

                    statusBw.RunWorkerCompleted += (ss, ee) =>
                    {
                        shellProgress.IsIndeterminate = false;

                        foreach (ConversationListObject co in HikeInstantiation.ViewModel.PendingRequests.Values)
                        {
                            var friendRequest = new FriendRequestStatusUpdate(co);
                            HikeInstantiation.ViewModel.StatusList.Add(friendRequest);
                        }

                        AddStatusToViewModel(statusMessagesFromDBUnblocked, HikeConstants.STATUS_INITIAL_FETCH_COUNT);

                        this.statusLLS.ItemsSource = HikeInstantiation.ViewModel.StatusList;

                        if (HikeInstantiation.ViewModel.StatusList.Count == 0 || (HikeInstantiation.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                            HikeInstantiation.ViewModel.StatusList.Add(DefaultStatus);

                        RefreshBarCount = 0;
                        UnreadFriendRequests = 0;

                        if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                        {
                            NetworkManager.turnOffNetworkManager = false;
                            PhoneApplicationService.Current.State.Remove("IsStatusPush");
                        }

                        isStatusMessagesLoaded = true;
                    };
                    //if (appSettings.Contains(HikeConstants.SHOW_STATUS_UPDATES_TUTORIAL))
                    //{
                    //    overlay.Visibility = Visibility.Visible;
                    //    overlay.Tap += DismissStatusUpdateTutorial_Tap;
                    //    TutorialStatusUpdate.Visibility = Visibility.Visible;
                    //    launchPagePivot.IsHitTestVisible = false;
                    //}
                }
                else
                {
                    RefreshBarCount = 0;
                    UnreadFriendRequests = 0;

                    if (statusLLS.ItemsSource == null)
                        statusLLS.ItemsSource = HikeInstantiation.ViewModel.StatusList;
                }
            }
            if (_newIndex != 2)
            {
                if (UnreadFriendRequests == 0 && RefreshBarCount == 0)
                    TotalUnreadStatuses = 0;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateTabImages(_newIndex);
            });
        }

        private void UpdateTabImages(int index)
        {
            switch (index)
            {
                case 0:
                    statusTabImage.Source = UI_Utils.Instance.StatusTabImageNotSelected;
                    chatsTabImage.Source = UI_Utils.Instance.ChatsTabImageSelected;
                    friendsTabImage.Source = UI_Utils.Instance.FriendsTabImageNotSelected;
                    break;
                case 1:
                    statusTabImage.Source = UI_Utils.Instance.StatusTabImageNotSelected;
                    chatsTabImage.Source = UI_Utils.Instance.ChatsTabImageNotSelected;
                    friendsTabImage.Source = UI_Utils.Instance.FriendsTabImageSelected;
                    break;
                case 2:
                    statusTabImage.Source = UI_Utils.Instance.StatusTabImageSelected;
                    chatsTabImage.Source = UI_Utils.Instance.ChatsTabImageNotSelected;
                    friendsTabImage.Source = UI_Utils.Instance.FriendsTabImageNotSelected;
                    break;
            }
        }

        private List<StatusMessage> GetUnblockedStatusUpdates(int fetchCount)
        {
            List<StatusMessage> statusMessagesFromDBUnblocked = new List<StatusMessage>();
            do
            {
                List<StatusMessage> listStatusUpdate = StatusMsgsTable.GetPaginatedStatusMsgsForTimeline(lastStatusId < 0 ? long.MaxValue : lastStatusId, fetchCount);

                if (listStatusUpdate == null || listStatusUpdate.Count == 0)
                    break;
                //count-number of status updates required from db
                int count = fetchCount - statusMessagesFromDBUnblocked.Count;
                hasMoreStatus = false;

                //no of status update fetched from db is more than required than more status updates exists
                if (listStatusUpdate.Count > (fetchCount - statusMessagesFromDBUnblocked.Count - 1))
                {
                    lastStatusId = listStatusUpdate[--count].StatusId;
                    hasMoreStatus = true;
                }
                else
                    //no of status update fetched is less than required so update count to number of status updates fetched
                    count = listStatusUpdate.Count;

                for (int i = 0; i < count; i++)
                {
                    // if this user is blocked dont show his/her statuses
                    if (!HikeInstantiation.ViewModel.BlockedHashset.Contains(listStatusUpdate[i].Msisdn))
                        statusMessagesFromDBUnblocked.Add(listStatusUpdate[i]);
                }

            } while (statusMessagesFromDBUnblocked.Count < (fetchCount - 1) && hasMoreStatus);

            return statusMessagesFromDBUnblocked;
        }

        private void AddStatusToViewModel(List<StatusMessage> statusMessagesFromDB, int messageFetchCount)
        {

            if (statusMessagesFromDB != null)
            {
                for (int i = 0; i < statusMessagesFromDB.Count; i++)
                {
                    StatusMessage statusMessage = statusMessagesFromDB[i];

                    //handle if total unread status are more than total loaded at first time
                    if (currentlyLoadedStatusCount++ < TotalUnreadStatuses)
                        statusMessage.IsUnread = true;

                    var status = StatusUpdateHelper.Instance.CreateStatusUpdate(statusMessage, true);
                    if (status != null)
                        HikeInstantiation.ViewModel.StatusList.Add(status);
                }
            }
        }

        #endregion

        #region PUBSUB

        public void onEventReceived(string type, object obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("ConversationsList :: OnEventReceived : Object received is null");
                if (type != HikePubSub.ADD_REMOVE_FAV)
                    return;
            }

            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConversationListObject mObj = (ConversationListObject)vals[1];
                if (mObj == null)
                    return;

                bool showPush = true;
                if (vals.Length == 3 && vals[2] is bool)
                    showPush = (Boolean)vals[2];

                mObj.TypingNotificationText = null;

                if (!isDeleteAllChats && (!mObj.IsHidden || (mObj.IsHidden && HikeInstantiation.ViewModel.IsHiddenModeActive))) // this is to avoid exception caused due to deleting all chats while receiving msgs
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            ShowChats();

                            if (HikeInstantiation.ViewModel.MessageListPageCollection.Count > 0 && HikeInstantiation.ViewModel.MessageListPageCollection[0] != null)
                                llsConversations.ScrollTo(HikeInstantiation.ViewModel.MessageListPageCollection[0]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ConversationList ::  onEventReceived,MESSAGE_RECEIVED  , Exception : " + ex.StackTrace);
                        }
                    });
                }

                if (HikeInstantiation.NewChatThreadPageObj == null && showPush && (!Utils.isGroupConversation(mObj.Msisdn) || !mObj.IsMute) && Utils.ShowNotificationAlert())
                {
                    bool isHikeJingleEnabled = true;
                    HikeInstantiation.AppSettings.TryGetValue<bool>(HikeConstants.AppSettings.HIKEJINGLE_PREF, out isHikeJingleEnabled);
                    if (isHikeJingleEnabled && (!mObj.IsHidden || (mObj.IsHidden && HikeInstantiation.ViewModel.IsHiddenModeActive)))
                    {
                        PlayAudio();
                    }
                    bool isVibrateEnabled = true;
                    HikeInstantiation.AppSettings.TryGetValue<bool>(HikeConstants.AppSettings.VIBRATE_PREF, out isVibrateEnabled);
                    if (isVibrateEnabled && (!mObj.IsHidden || (mObj.IsHidden && HikeInstantiation.ViewModel.IsHiddenModeActive)))
                    {
                        VibrateController vibrate = VibrateController.Default;
                        vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                    }
                    appSettings[HikeConstants.AppSettings.LAST_NOTIFICATION_TIME] = DateTime.Now.Ticks;
                }
            }
            #endregion
            #region UPDATE_ACCOUNT_NAME
            else if (HikePubSub.UPDATE_ACCOUNT_NAME == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _userName = (string)obj;
                    _firstName = Utils.GetFirstName(_userName);
                    peopleOnHikeText.Text = String.Format(AppResources.Conversations_Empty_PeopleOnHike_Txt, _firstName, _usersOnHike);
                });
            }
            #endregion
            #region ADD OR REMOVE FAV
            else if (HikePubSub.ADD_REMOVE_FAV == type)
            {
                if (!isContactListLoaded)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateFriendsCounter();

                    if (HikeInstantiation.ViewModel.FavList.Count == 0 && emptyListPlaceholderFiends.Visibility == Visibility.Collapsed) // remove fav
                    {
                        emptyListPlaceholderFiends.Visibility = Visibility.Visible;
                        favourites.Visibility = Visibility.Collapsed;
                        //addFavsPanel.Opacity = 0;
                    }
                    else if (HikeInstantiation.ViewModel.FavList.Count > 0 && emptyListPlaceholderFiends.Visibility == Visibility.Visible)
                    {
                        emptyListPlaceholderFiends.Visibility = Visibility.Collapsed;
                        favourites.Visibility = Visibility.Visible;
                        //addFavsPanel.Opacity = 1;
                    }

                });
            }
            #endregion
            #region ADD TO PENDING
            else if (HikePubSub.ADD_TO_PENDING == type)
            {
                if (!HikeInstantiation.ViewModel.IsPendingListLoaded || !isStatusMessagesLoaded)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ConversationListObject co = (ConversationListObject)obj;
                    if (co != null)
                    {
                        // if isStatusMessagesLoaded & pending list are not loaded simply ignore this packet , as then this packet will
                        // be shown twice , one here and one from DB.
                        if (isStatusMessagesLoaded && HikeInstantiation.ViewModel.IsPendingListLoaded)
                        {
                            int index = 0;
                            if (ProTipHelper.CurrentProTip != null)
                                index = 1;

                            if (HikeInstantiation.ViewModel.StatusList.Count > index && HikeInstantiation.ViewModel.StatusList[index] is DefaultStatus)
                                HikeInstantiation.ViewModel.StatusList.RemoveAt(index);

                            FriendRequestStatusUpdate frs = new FriendRequestStatusUpdate(co);
                            HikeInstantiation.ViewModel.StatusList.Insert(index, frs);

                        }
                        if (launchPagePivot.SelectedIndex != 2)
                        {
                            UnreadFriendRequests++;
                        }
                    }
                });
            }
            #endregion
            #region BAD_USER_PASS
            else if (HikePubSub.BAD_USER_PASS == type)
            {
                try
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        HikeInstantiation.ViewModel.ClearViewModel();
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
                    //if its image update and status are laoded, update each status userImage async
                    if (sm.Status_Type == StatusMessage.StatusType.PROFILE_PIC_UPDATE && isStatusMessagesLoaded)
                    {
                        HikeInstantiation.ViewModel.UpdateUserImageInStatus(sm.Msisdn);
                    }

                    if (sm.Msisdn == HikeInstantiation.MSISDN || sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
                    {
                        if (sm.Status_Type == StatusMessage.StatusType.TEXT_UPDATE)
                            StatusMsgsTable.SaveLastStatusMessage(sm.Message, sm.MoodId);

                        // if status list is not loaded simply ignore this packet , as then this packet will
                        // be shown twice , one here and one from DB.
                        if (isStatusMessagesLoaded)
                        {
                            var status = StatusUpdateHelper.Instance.CreateStatusUpdate(sm, true);
                            if (status != null)
                            {
                                int index = 0;
                                if (ProTipHelper.CurrentProTip != null)
                                    index = 1;

                                if (HikeInstantiation.ViewModel.StatusList.Count > index && HikeInstantiation.ViewModel.StatusList[index] is DefaultStatus)
                                    HikeInstantiation.ViewModel.StatusList.RemoveAt(index);
                                int count = HikeInstantiation.ViewModel.PendingRequests != null ? HikeInstantiation.ViewModel.PendingRequests.Count : 0;
                                HikeInstantiation.ViewModel.StatusList.Insert(count + index, status);
                            }
                        }
                    }
                    else
                    {
                        if (!sm.ShowOnTimeline)
                            return;

                        // here we have to check 2 way firendship
                        if (launchPagePivot.SelectedIndex == 2)
                        {
                            FreshStatusUpdates.Add(sm);
                        }
                        else
                        {
                            // if status list is not loaded simply ignore this packet , as then this packet will
                            // be shown twice , one here and one from DB.
                            if (isStatusMessagesLoaded)
                            {
                                var status = StatusUpdateHelper.Instance.CreateStatusUpdate(sm, true);
                                if (status != null)
                                {
                                    int index = 0;
                                    if (ProTipHelper.CurrentProTip != null)
                                        index = 1;

                                    if (HikeInstantiation.ViewModel.StatusList.Count > index && HikeInstantiation.ViewModel.StatusList[index] is DefaultStatus)
                                        HikeInstantiation.ViewModel.StatusList.RemoveAt(index);
                                    int count = HikeInstantiation.ViewModel.PendingRequests != null ? HikeInstantiation.ViewModel.PendingRequests.Count : 0;
                                    HikeInstantiation.ViewModel.StatusList.Insert(count + index, status);
                                }
                            }
                        }
                        RefreshBarCount++;//persist in this.State. it will be cleared 
                    }
                    TotalUnreadStatuses++;

                });
            }
            #endregion
            #region STATUS_DELETED
            else if (HikePubSub.STATUS_DELETED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    BaseStatusUpdate sb = obj as BaseStatusUpdate;
                    if (sb != null)
                    {
                        HikeInstantiation.ViewModel.StatusList.Remove(sb);
                        if (sb.IsUnread)
                        {
                            _totalUnreadStatuses--;
                            RefreshBarCount--;
                        }

                        if (HikeInstantiation.ViewModel.StatusList.Count == 0 || (HikeInstantiation.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                            HikeInstantiation.ViewModel.StatusList.Add(DefaultStatus);
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
                    //will be populated automatically while loading from db
                    if (!isContactListLoaded)
                        return;
                    if (!HikeInstantiation.ViewModel.ContactsCache.TryGetValue(msisdn, out c))
                    {
                        ConversationListObject convObj = null;

                        if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                        {
                            convObj = HikeInstantiation.ViewModel.ConvMap[msisdn];

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

                        if (c != null && c.Msisdn != HikeInstantiation.MSISDN)
                        {
                            hikeContactList.Add(c);
                            UpdateContactsOnHikeCounter();
                        }

                        if (hikeContactList.Count > 0)
                        {
                            emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                            hikeContactListBox.Visibility = Visibility.Visible;
                        }
                    });
                }
            }
            #endregion
            #region ADD_FRIENDS
            else if (HikePubSub.ADD_FRIENDS == type)
            {
                if (!isContactListLoaded)
                    return;
                if (obj is ContactInfo)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (obj != null)
                        {
                            ContactInfo c = obj as ContactInfo;
                            hikeContactList.Remove(c);
                            UpdateContactsOnHikeCounter();
                        }
                        if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                        {
                            emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                            favourites.Visibility = System.Windows.Visibility.Visible;
                        }
                        if (hikeContactList.Count == 0)
                        {
                            emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                            hikeContactListBox.Visibility = Visibility.Collapsed;
                        }
                    });
                }
                else if (obj is string)
                {
                    string ms = obj as string;
                    if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(ms))
                    {
                        ConversationListObject co = HikeInstantiation.ViewModel.ConvMap[ms];
                        if (co != null && co.IsOnhike && !string.IsNullOrEmpty(co.ContactName))
                        {
                            ContactInfo c = new ContactInfo(ms, co.NameToShow, co.IsOnhike);

                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                hikeContactList.Remove(c);
                                UpdateContactsOnHikeCounter();
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
                                UpdateContactsOnHikeCounter();
                            });
                        }
                    }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                          {
                              if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                              {
                                  emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                                  favourites.Visibility = System.Windows.Visibility.Visible;
                              }
                              if (hikeContactList.Count == 0)
                              {
                                  emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                                  hikeContactListBox.Visibility = Visibility.Collapsed;
                              }
                          });
                }
            }
            #endregion
            #region BLOCK_USER
            else if (HikePubSub.BLOCK_USER == type)
            {
                if (obj is ContactInfo)
                {
                    ContactInfo c = obj as ContactInfo;

                    if (isStatusMessagesLoaded)
                    {
                        #region removing friend request
                        // UI and Data is decoupled by pubsub , so have to remove from UI here
                        if (HikeInstantiation.ViewModel.StatusList != null)
                        {
                            for (int i = 0; i < HikeInstantiation.ViewModel.StatusList.Count; i++)
                            {
                                if (HikeInstantiation.ViewModel.StatusList[i] is FriendRequestStatusUpdate)
                                {
                                    FriendRequestStatusUpdate f = HikeInstantiation.ViewModel.StatusList[i] as FriendRequestStatusUpdate;
                                    if (f.Msisdn == c.Msisdn)
                                    {
                                        Dispatcher.BeginInvoke(() =>
                                        {
                                            if (i < UnreadFriendRequests)
                                                UnreadFriendRequests--;

                                            try
                                            {
                                                HikeInstantiation.ViewModel.StatusList.Remove(f);

                                                if (HikeInstantiation.ViewModel.StatusList.Count == 0 || (HikeInstantiation.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                                                    HikeInstantiation.ViewModel.StatusList.Add(DefaultStatus);
                                            }
                                            catch (Exception e)
                                            {
                                                Debug.WriteLine("ConversationsList :: BLOCK USER : Exception while removing a friendRequest, Exception : " + e.StackTrace);
                                            }
                                        });
                                        break;
                                    }

                                }
                                else
                                    break;
                            }
                        }
                        #endregion
                    }
                    #region removing hike contact if blocked
                    if (c.OnHike && !string.IsNullOrEmpty(c.Name)) // if friend request is not there , try to remove from contacts
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            hikeContactList.Remove(c);
                            if (isContactListLoaded && hikeContactList.Count == 0)
                            {
                                emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                                hikeContactListBox.Visibility = Visibility.Collapsed;
                            }
                            UpdateContactsOnHikeCounter();
                        });
                    }
                    //if conatct is removed from circle of friends then show no friends placehoder
                    Dispatcher.BeginInvoke(() =>
                   {
                       if (favCollectionView.Source != null && HikeInstantiation.ViewModel.FavList.Count == 0)
                       {
                           emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                           favourites.Visibility = System.Windows.Visibility.Collapsed;
                       }
                   });
                    #endregion

                    #region remove pic
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (isStatusMessagesLoaded)
                            HikeInstantiation.ViewModel.UpdateUserImageInStatus(c.Msisdn);
                    });
                    #endregion
                }
                Dispatcher.BeginInvoke(() =>
                {
                    UpdateFriendsCounter();
                });
            }
            #endregion
            #region UNBLOCK_USER
            else if (HikePubSub.UNBLOCK_USER == type || HikePubSub.UNBLOCK_GROUPOWNER == type)
            {
                //will be populated automatically while loading from db
                if (!isContactListLoaded)
                    return;
                ContactInfo c = null;
                if (obj is ContactInfo)
                    c = obj as ContactInfo;
                else
                {
                    string msisdn = obj as string;
                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(msisdn))
                        c = HikeInstantiation.ViewModel.ContactsCache[msisdn];
                    else
                        c = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                }

                // ignore if not onhike or not in addressbook
                if (c == null || !c.OnHike || string.IsNullOrEmpty(c.Name)) // c==null for unknown contacts
                    return;

                Dispatcher.BeginInvoke(() =>
                {
                    if (c.Msisdn != HikeInstantiation.MSISDN)
                    {
                        hikeContactList.Add(c);
                        UpdateContactsOnHikeCounter();
                    }
                    if (emptyListPlaceholderHikeContacts.Visibility == Visibility.Visible)
                    {
                        emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                        hikeContactListBox.Visibility = Visibility.Visible;
                    }
                });

            }
            #endregion
            #region DELETE CONVERSATION
            else if (HikePubSub.DELETE_STATUS_AND_CONV == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ConversationListObject co = obj as ConversationListObject;
                    HikeInstantiation.ViewModel.MessageListPageCollection.Remove(co);

                    if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                        ShowFTUECards();
                });
            }
            #endregion
            #region CONTACT ADDED
            else if (HikePubSub.CONTACT_ADDED == type)
            {
                if (obj is ContactInfo)
                {
                    //will be populated automatically while loading from db
                    if (!isContactListLoaded)
                        return;
                    ContactInfo cinfo = obj as ContactInfo;
                    if (cinfo.OnHike && !HikeInstantiation.ViewModel.Isfavourite(cinfo.Msisdn))
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            hikeContactList.Add(cinfo);
                            UpdateContactsOnHikeCounter();
                            if (hikeContactListBox.Visibility == Visibility.Collapsed)
                            {
                                emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                                hikeContactListBox.Visibility = Visibility.Visible;
                            }
                        });
                    }

                }
            }
            #endregion
            #region ADDRESSBOOK UPDATE
            else if (type == HikePubSub.ADDRESSBOOK_UPDATED)
            {
                //will be populated automatically while loading from db
                if (!isContactListLoaded)
                    return;
                if (obj is object[] && ((object[])obj).Length == 2)
                {
                    Object[] objContacts = (Object[])obj;
                    bool isContactAdded = (bool)objContacts[0];
                    if (isContactAdded)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action<List<ContactInfo>>(delegate(List<ContactInfo> listAddedContacts)
                        {

                            bool isNewUserAdded = false;
                            foreach (ContactInfo cinfo in listAddedContacts)
                            {
                                if (cinfo.OnHike && !HikeInstantiation.ViewModel.Isfavourite(cinfo.Msisdn) && hikeContactList.Where(c => c.Msisdn == cinfo.Msisdn).Count() == 0)
                                {
                                    hikeContactList.Add(cinfo);
                                    isNewUserAdded = true;
                                }
                            }
                            if (isNewUserAdded)
                            {
                                UpdateContactsOnHikeCounter();
                                if (hikeContactListBox.Visibility == Visibility.Collapsed)
                                {
                                    emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                                    hikeContactListBox.Visibility = Visibility.Visible;
                                }
                            }
                        }), objContacts[1]);
                    }
                    else
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action<List<ContactInfo>>(delegate(List<ContactInfo> listDeletedContacts)
                       {
                           foreach (ContactInfo cinfo in listDeletedContacts)
                           {
                               hikeContactList.Remove(cinfo);
                               HikeInstantiation.ViewModel.ContactsCache.Remove(cinfo.Msisdn);
                           }
                           UpdateContactsOnHikeCounter();
                           if (hikeContactList.Count == 0)
                           {
                               emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                               hikeContactListBox.Visibility = Visibility.Collapsed;
                           }
                       }), objContacts[1]);
                    }
                }
            }
            #endregion
            #region UPDATE AVAILABLE
            else if (type == HikePubSub.APP_UPDATE_AVAILABLE)
            {
                ShowAppUpdateAvailableMessage();
            }
            #endregion
            #region REWARDS TOGGLE
            else if (HikePubSub.REWARDS_TOGGLE == type)
            {
                bool showRewards;
                appSettings.TryGetValue(HikeConstants.ServerJsonKeys.SHOW_REWARDS, out showRewards);
                if (showRewards) // show rewards option
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsMenu == null)
                        {
                            rewardsMenu = new ApplicationBarMenuItem();
                            rewardsMenu.Text = AppResources.ConversationsList_Rewards_Txt;
                            rewardsMenu.Click += rewardsMenu_Click;
                        }

                        if (!appBar.MenuItems.Contains(rewardsMenu))
                        {
                            if (launchPagePivot.SelectedIndex == 1)
                                appBar.MenuItems.Insert(1, rewardsMenu);
                            else
                                appBar.MenuItems.Insert(2, rewardsMenu);
                        }
                        rewardsMenu.IsEnabled = true;
                    });
                }
                else // hide rewards option 
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (rewardsMenu != null)
                            appBar.MenuItems.Remove(rewardsMenu);

                        rewardsMenu = null;
                    });
                }
            }
            #endregion
        }

        private void ShowChats()
        {
            if (emptyScreenGrid.Visibility == Visibility.Visible)
                emptyScreenGrid.Visibility = Visibility.Collapsed;

            if (llsConversations.Visibility == Visibility.Collapsed)
                llsConversations.Visibility = Visibility.Visible;

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS))
                UpdateToolTip(true);
        }

        private void UpdateFriendsCounter()
        {
            if (HikeInstantiation.ViewModel.FavList.Count == 0)
                txtCircleOfFriends.Text = AppResources.Conversations_Circle_Of_friends_txt;
            else if (HikeInstantiation.ViewModel.FavList.Count == 1)
                txtCircleOfFriends.Text = AppResources.Conversations_1Circle_Of_friends_txt;
            else
                txtCircleOfFriends.Text = string.Format(AppResources.Conversations_NCircle_Of_friends_txt, HikeInstantiation.ViewModel.FavList.Count);
        }

        private void UpdateContactsOnHikeCounter()
        {
            if (hikeContactList.Count == 0)
                txtContactsOnHike.Text = AppResources.Conversations_Contacts_on_hike;
            else if (hikeContactList.Count == 1)
                txtContactsOnHike.Text = AppResources.Conversations_1Contact_on_hike;
            else
                txtContactsOnHike.Text = string.Format(AppResources.Conversations_NContacts_on_hike, hikeContactList.Count);
        }

        #endregion

        #region App Update Available


        void ShowAppUpdateAvailableMessage()
        {
            String updateObj;
            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE, out updateObj))
            {
                JObject obj = JObject.Parse(updateObj);

                var currentVersion = HikeInstantiation.AppSettings[HikeConstants.AppSettings.FILE_SYSTEM_VERSION].ToString();
                var version = (string)obj[HikeConstants.ServerJsonKeys.VERSION];
                if (Utils.compareVersion(version, currentVersion) <= 0)
                {
                    HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE);
                    return;
                }

                var message = (string)obj[HikeConstants.ServerJsonKeys.TEXT_UPDATE_MSG];
                bool isCriticalUpdate = (bool)obj[HikeConstants.ServerJsonKeys.CRITICAL];

                if (isCriticalUpdate)
                    showCriticalUpdateMessage(message);
                else
                    showNormalUpdateMessage(message);
            }
        }

        private void showCriticalUpdateMessage(string message)
        {
            if (!Guide.IsVisible)
            {
                Guide.BeginShowMessageBox(AppResources.CRITICAL_UPDATE_HEADING, message,
                     new List<string> { AppResources.Update_Now_Txt.ToLower() }, 0, MessageBoxIcon.Alert,
                     asyncResult =>
                     {
                         int? returned = Guide.EndShowMessageBox(asyncResult);
                         if (returned != null && returned == 0)
                         {
                             openMarketPlace();
                         }
                         criticalUpdateMessageBoxReturned();

                     }, null);
            }
        }

        private void showNormalUpdateMessage(string message)
        {
            if (!Guide.IsVisible)
            {
                Guide.BeginShowMessageBox(AppResources.NORMAL_UPDATE_HEADING, message,
                     new List<string> { AppResources.Conversations_Dismiss_Tip.ToLower(), AppResources.Update_Now_Txt.ToLower() }, 0, MessageBoxIcon.Alert,
                     asyncResult =>
                     {
                         int? returned = Guide.EndShowMessageBox(asyncResult);
                         if (returned != null)
                         {
                             if (returned == 1)
                                 openMarketPlace();
                             else
                                 HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE);
                         }

                     }, null);
            }
        }

        private void criticalUpdateMessageBoxReturned()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                LayoutRoot.IsHitTestVisible = false;
                disableAppBar();
            });
        }

        private void openMarketPlace()
        {
            MarketplaceDetailTask marketplaceDetailTask = new MarketplaceDetailTask();
            marketplaceDetailTask.ContentIdentifier = "b4703e38-092f-4144-826a-3e3d41f50714";//app id to be used from our dev account
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

        #endregion

        #region CONTEXT MENUS

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;

            contextMenu.ClearValue(FrameworkElement.DataContextProperty);
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_Delete_Chat_Confirmation, AppResources.Conversations_DelChat_Txt, MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;
            if (convObj != null)
                DeleteConversation(convObj, true);
        }

        private void MenuItem_Click_AddRemoveFav(object sender, RoutedEventArgs e)
        {
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;
            if (convObj == null)
                return;

            if (convObj.IsFav) // already fav , remove request
            {
                var text = String.Format(AppResources.Conversations_RemFromFav_Confirm_Txt, convObj.NameToShow);
                MessageBoxResult result = MessageBox.Show(text, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                    return;
                convObj.IsFav = false;
                HikeInstantiation.ViewModel.FavList.Remove(convObj);
                UpdateFriendsCounter();
                JObject data = new JObject();
                data["id"] = convObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.REMOVE_FAVOURITE;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.DeleteFavourite(convObj.Msisdn);
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                if (HikeInstantiation.ViewModel.FavList.Count == 0)
                {
                    emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                    favourites.Visibility = System.Windows.Visibility.Collapsed;
                    //addFavsPanel.Opacity = 0;
                }

                FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);

                // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                if (convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                {
                    ContactInfo c = null;
                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                        c = HikeInstantiation.ViewModel.ContactsCache[convObj.Msisdn];
                    else
                    {
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                        c.Avatar = convObj.Avatar;
                    }

                    if (c.Msisdn != HikeInstantiation.MSISDN && isContactListLoaded)
                    {
                        hikeContactList.Add(c);
                        UpdateContactsOnHikeCounter();
                    }
                }
                if (hikeContactList.Count > 0 && isContactListLoaded)
                {
                    emptyListPlaceholderHikeContacts.Visibility = System.Windows.Visibility.Collapsed;
                    hikeContactListBox.Visibility = Visibility.Visible;
                }
            }
            else // add to fav
            {
                convObj.IsFav = true;

                ContactInfo c = null;
                if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                    c = HikeInstantiation.ViewModel.ContactsCache[convObj.Msisdn];
                else
                    c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);

                hikeContactList.Remove(c);
                UpdateContactsOnHikeCounter();
                FriendsTableUtils.FriendStatusEnum fs = FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                HikeInstantiation.ViewModel.FavList.Insert(0, convObj);
                UpdateFriendsCounter();
                if (HikeInstantiation.ViewModel.IsPending(convObj.Msisdn))
                {
                    HikeInstantiation.ViewModel.PendingRequests.Remove(convObj.Msisdn);
                    MiscDBUtil.SavePendingRequests();
                    HikeInstantiation.ViewModel.RemoveFrndReqFromTimeline(convObj.Msisdn, fs);
                }
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(convObj);
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
                JObject data = new JObject();
                data["id"] = convObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                if (isContactListLoaded)
                {
                    if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                    {
                        emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                        favourites.Visibility = System.Windows.Visibility.Visible;
                        //addFavsPanel.Opacity = 1;
                    }
                    if (hikeContactList.Count == 0)
                    {
                        emptyListPlaceholderHikeContacts.Visibility = System.Windows.Visibility.Visible;
                        hikeContactListBox.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void MenuItem_Click_Mute(object sender, RoutedEventArgs e)
        {
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;
            if (convObj == null)
                return;

            JObject obj = new JObject();
            JObject o = new JObject();
            o["id"] = convObj.Msisdn;
            obj[HikeConstants.ServerJsonKeys.DATA] = o;

            if (convObj.IsMute) // GC is muted , request to unmute
            {
                obj[HikeConstants.ServerJsonKeys.TYPE] = "unmute";
                HikeInstantiation.ViewModel.ConvMap[convObj.Msisdn].MuteVal = -1;
                ConversationTableUtils.saveConvObject(HikeInstantiation.ViewModel.ConvMap[convObj.Msisdn], convObj.Msisdn.Replace(":", "_"));
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
            else // GC is unmute , request to mute
            {
                obj[HikeConstants.ServerJsonKeys.TYPE] = "mute";
                HikeInstantiation.ViewModel.ConvMap[convObj.Msisdn].MuteVal = 0;
                ConversationTableUtils.saveConvObject(HikeInstantiation.ViewModel.ConvMap[convObj.Msisdn], convObj.Msisdn.Replace(":", "_"));
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            BaseStatusUpdate selectedItem = (sender as MenuItem).DataContext as BaseStatusUpdate;

            if (selectedItem == null)
                return;

            Clipboard.SetText(selectedItem.Text);
        }

        private void MenuItem_EmailConversation_Clicked(object sender, RoutedEventArgs e)
        {
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;

            if (convObj == null)
                return;

            EmailHelper.FetchAndEmail(convObj.Msisdn, convObj.ContactName, convObj.IsGroupChat);
        }

        #endregion

        private void disableAppBar()
        {
            composeIconButton.IsEnabled = false;
            postStatusIconButton.IsEnabled = false;
            groupChatIconButton.IsEnabled = false;
            ApplicationBar.IsMenuEnabled = false;
        }

        private void enableAppBar()
        {
            composeIconButton.IsEnabled = true;
            postStatusIconButton.IsEnabled = true;
            groupChatIconButton.IsEnabled = true;
            ApplicationBar.IsMenuEnabled = true;
        }

        private void InviteBtn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (passwordOverlay.IsShow)
            {
                _isConfirmPassword = false;
                _tempPassword = null;
                passwordOverlay.IsShow = false;
                e.Cancel = true;
                return;
            }

            if (HikeInstantiation.IsViewModelLoaded)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && HikeInstantiation.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
            }

            if (FileTransfers.FileTransferManager.Instance.IsBusy())
            {
                var result = MessageBox.Show(AppResources.FileTransfer_InProgress_Msg, AppResources.FileTransfer_InProgress, MessageBoxButton.OKCancel);

                if (result != MessageBoxResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
            }

            NetworkManager.turnOffNetworkManager = true;

            base.OnBackKeyPress(e);
        }

        #region RATE THE APP
        private void checkForRateApp()
        {
            int appLaunchCount = 0;
            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.APP_LAUNCH_COUNT, out appLaunchCount) && appLaunchCount > 0)
            {
                double result = Math.Log(appLaunchCount / 5f, 2);//using gp
                if (result == Math.Ceiling(result) && NetworkInterface.GetIsNetworkAvailable()) //TODO - we can use mqtt connection status. 
                //if mqtt is connected it would safe to assume that user is online.
                {
                    showRateAppMessage();
                }
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, appLaunchCount + 1);
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
                                     HikeInstantiation.AppSettings.Remove(HikeConstants.AppSettings.APP_LAUNCH_COUNT);
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
                if (obj == null || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.ConvMap.ContainsKey(obj.Msisdn) && HikeInstantiation.ViewModel.ConvMap[obj.Msisdn].IsHidden))
                    return;
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_CONVERSATIONS_PAGE] = obj;
                string uri = "/View/NewChatThread.xaml";
                NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            }
            favourites.SelectedIndex = -1;
        }

        private void RemoveFavourite_Click(object sender, RoutedEventArgs e)
        {
            ConversationListObject convObj = (sender as MenuItem).DataContext as ConversationListObject;
            if (convObj != null)
            {
                var text = String.Format(AppResources.Conversations_RemFromFav_Confirm_Txt, convObj.NameToShow);
                MessageBoxResult result = MessageBox.Show(text, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                    return;

                convObj.IsFav = false;
                HikeInstantiation.ViewModel.FavList.Remove(convObj);
                UpdateFriendsCounter();
                JObject data = new JObject();
                data["id"] = convObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.REMOVE_FAVOURITE;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.DeleteFavourite(convObj.Msisdn);// remove single file too
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);

                // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                if (convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                {

                    ContactInfo c = null;
                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                        c = HikeInstantiation.ViewModel.ContactsCache[convObj.Msisdn];
                    else
                    {
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                        c.Avatar = convObj.Avatar;
                    }

                    if (c.Msisdn != HikeInstantiation.MSISDN)
                    {
                        hikeContactList.Add(c);
                        UpdateContactsOnHikeCounter();
                    }
                }
            }
            if (HikeInstantiation.ViewModel.FavList.Count == 0)
            {
                emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                favourites.Visibility = System.Windows.Visibility.Collapsed;
            }
            if (hikeContactList.Count > 0)
            {
                emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                hikeContactListBox.Visibility = Visibility.Visible;
            }
        }

        private void hikeContacts_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo c = hikeContactListBox.SelectedItem as ContactInfo;
            if (c == null)
                return;

            StartNewChatWithSelectContact(c);
        }

        private void StartNewChatWithSelectContact(ContactInfo c)
        {
            if (!HikeInstantiation.ViewModel.IsHiddenModeActive
                && HikeInstantiation.ViewModel.ConvMap.ContainsKey(c.Msisdn) && HikeInstantiation.ViewModel.ConvMap[c.Msisdn].IsHidden)
                return;

            object objToSend;
            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(c.Msisdn))
                objToSend = HikeInstantiation.ViewModel.ConvMap[c.Msisdn];
            else
                objToSend = c;
            // TODO: Handle this properly
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_CONVERSATIONS_PAGE] = objToSend;
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

                if (HikeInstantiation.ViewModel.Isfavourite(contactInfo.Msisdn))
                {
                    hikeContactList.Remove(contactInfo);
                    UpdateContactsOnHikeCounter();
                    return;
                }

                JObject data = new JObject();
                data["id"] = contactInfo.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
                ConversationListObject cObj = null;
                if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(contactInfo.Msisdn))
                {
                    cObj = HikeInstantiation.ViewModel.ConvMap[contactInfo.Msisdn];
                    cObj.IsFav = true;
                }
                else
                {
                    var bytes = contactInfo.Avatar == null ? UI_Utils.Instance.ConvertToBytes(contactInfo.AvatarImage) : contactInfo.Avatar;
                    cObj = new ConversationListObject(contactInfo.Msisdn, contactInfo.Name, contactInfo.OnHike, bytes);
                }

                hikeContactList.Remove(contactInfo);
                UpdateContactsOnHikeCounter();
                HikeInstantiation.ViewModel.FavList.Add(cObj);
                UpdateFriendsCounter();
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(cObj);
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

                if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                {
                    emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                    favourites.Visibility = System.Windows.Visibility.Visible;
                }
                if (hikeContactList.Count == 0)
                {
                    emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                    hikeContactListBox.Visibility = Visibility.Collapsed;
                }
                FriendsTableUtils.FriendStatusEnum fs = FriendsTableUtils.SetFriendStatus(cObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);

                if (HikeInstantiation.ViewModel.IsPending(contactInfo.Msisdn))
                {
                    HikeInstantiation.ViewModel.PendingRequests.Remove(contactInfo.Msisdn);
                    MiscDBUtil.SavePendingRequests();
                    HikeInstantiation.ViewModel.RemoveFrndReqFromTimeline(contactInfo.Msisdn, fs);
                }
            }
            hikeContactListBox.SelectedIndex = -1;
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
                if (HikeInstantiation.IsTombstoneLaunch)
                {
                    _freshStatusUpdates = StatusMsgsTable.GetUnReadStatusMsgs(TotalUnreadStatuses);
                }
                if (_freshStatusUpdates == null) //not in "else" because db too can return null
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
                        if (_refreshBarCount == 0 && value > 0)
                        {
                            if (isStatusMessagesLoaded && launchPagePivot.SelectedIndex == 2)
                                refreshBarButton.Visibility = System.Windows.Visibility.Visible;
                        }
                        else if (_refreshBarCount > 0 && value == 0)
                        {
                            refreshBarButton.Visibility = System.Windows.Visibility.Collapsed;
                            FreshStatusUpdates.Clear();
                        }
                        if (refreshBarButton.Visibility == System.Windows.Visibility.Visible && value > 0)
                        {
                            if (value == 1)
                                refreshStatusText.Text = string.Format(AppResources.Conversations_Timeline_Refresh_SingleStatus, value);
                            else
                                refreshStatusText.Text = string.Format(AppResources.Conversations_Timeline_Refresh_Status, value);
                        }

                        if (launchPagePivot.SelectedIndex == 2)
                        {
                            setNotificationCounter(0);
                        }
                        else
                        {
                            setNotificationCounter(value + _unreadFriendRequests + _proTipCount);
                        }
                        _refreshBarCount = value;
                        StatusMsgsTable.SaveUnreadCounts(HikeConstants.REFRESH_BAR, value);
                    });
                }
            }
        }

        private int _proTipCount = 0;
        private int ProTipCount
        {
            get
            {
                return _proTipCount;
            }
            set
            {
                if (value != _proTipCount)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (launchPagePivot.SelectedIndex == 2)
                        {
                            setNotificationCounter(0);
                        }
                        else
                        {
                            setNotificationCounter(value + _unreadFriendRequests + _refreshBarCount);
                        }
                    });

                    _proTipCount = value;
                    HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.PRO_TIP_COUNT, value);
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
                    if (value == 0 && (HikeInstantiation.ViewModel.StatusList.Count >= HikeInstantiation.ViewModel.PendingRequests.Count + _totalUnreadStatuses))
                    {
                        for (int i = HikeInstantiation.ViewModel.PendingRequests.Count;
                            i < HikeInstantiation.ViewModel.PendingRequests.Count + _totalUnreadStatuses; i++)
                        {
                            HikeInstantiation.ViewModel.StatusList[i].IsUnread = false;
                        }
                    }
                    StatusMsgsTable.SaveUnreadCounts(HikeConstants.UNREAD_UPDATES, value);
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
                    setNotificationCounter(value + _refreshBarCount + _proTipCount);
                    StatusMsgsTable.SaveUnreadCounts(HikeConstants.UNREAD_FRIEND_REQUESTS, value);
                }
            }
        }

        private void setNotificationCounter(int newCounterValue)
        {
            if (newCounterValue > 0)
            {
                notificationCountTxtBlk.Text = newCounterValue <= 9 ? newCounterValue.ToString() : "9+";
                notificationCountGrid.Visibility = Visibility.Visible;
            }
            else
            {
                notificationCountTxtBlk.Text = "";
                notificationCountGrid.Visibility = Visibility.Collapsed;
            }
        }

        private void refreshStatuses_Tap(object sender, RoutedEventArgs e)
        {
            UpdatePendingStatusFromRefreshBar();
        }

        private void UpdatePendingStatusFromRefreshBar()
        {
            int index = 0;
            if (ProTipHelper.CurrentProTip != null)
                index = 1;
            if (HikeInstantiation.ViewModel.StatusList.Count > index && HikeInstantiation.ViewModel.StatusList[index] is DefaultStatus && FreshStatusUpdates != null && FreshStatusUpdates.Count > 0)
                HikeInstantiation.ViewModel.StatusList.RemoveAt(index);

            // this fix will solve the possible crash , suggested by nitesh
            int pendingCount = HikeInstantiation.ViewModel.PendingRequests != null ? HikeInstantiation.ViewModel.PendingRequests.Count + index : index;
            for (int i = 0; i < (FreshStatusUpdates != null ? FreshStatusUpdates.Count : 0); i++)
            {
                var status = StatusUpdateHelper.Instance.CreateStatusUpdate(FreshStatusUpdates[i], true);
                if (status != null)
                {
                    HikeInstantiation.ViewModel.StatusList.Insert(pendingCount, status);
                }
            }

            //scroll to the recent item(the most recent status update on tapping this bar)
            if (HikeInstantiation.ViewModel.StatusList.Count > pendingCount)
                statusLLS.ScrollTo(HikeInstantiation.ViewModel.StatusList[pendingCount]);

            RefreshBarCount = 0;
        }

        private void postStatusBtn_Click(object sender, EventArgs e)
        {
            //if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            //{
            //    RemoveStatusUpdateTutorial();
            //}
            if (_tipMode == ToolTipMode.STATUS_UPDATE)
                HideTips();

            Uri nextPage = new Uri("/View/PostStatus.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        bool _buttonInsideStatusUpdateTapped = false;

        private void yes_Click(object sender, RoutedEventArgs e)
        {
            FriendRequestStatusUpdate fObj = (sender as Button).DataContext as FriendRequestStatusUpdate;
            if (fObj != null)
            {
                _buttonInsideStatusUpdateTapped = true;
                HikeInstantiation.ViewModel.StatusList.Remove(fObj);
                FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
                HikeInstantiation.ViewModel.PendingRequests.Remove(fObj.Msisdn);
                MiscDBUtil.SavePendingRequests();

                if (HikeInstantiation.ViewModel.Isfavourite(fObj.Msisdn)) // if already favourite just ignore
                {
                    if (HikeInstantiation.ViewModel.StatusList.Count == 0 || (HikeInstantiation.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                        HikeInstantiation.ViewModel.StatusList.Add(DefaultStatus);

                    return;
                }

                ConversationListObject cObj = null;
                ContactInfo cn = null;
                if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(fObj.Msisdn))
                {
                    cObj = HikeInstantiation.ViewModel.ConvMap[fObj.Msisdn];
                    cObj.IsFav = true;
                }
                else
                {
                    if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                        cn = HikeInstantiation.ViewModel.ContactsCache[fObj.Msisdn];
                    else
                    {
                        cn = UsersTableUtils.getContactInfoFromMSISDN(fObj.Msisdn);
                        if (cn != null)
                            HikeInstantiation.ViewModel.ContactsCache[fObj.Msisdn] = cn;
                    }
                    bool onHike = cn != null ? cn.OnHike : true; // by default only hiek user can send you friend request
                    cObj = new ConversationListObject(fObj.Msisdn, fObj.UserName, onHike, MiscDBUtil.getThumbNailForMsisdn(fObj.Msisdn));
                }

                if (cn == null && HikeInstantiation.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                    cn = HikeInstantiation.ViewModel.ContactsCache[fObj.Msisdn];

                if (cn != null)
                {
                    hikeContactList.Remove(cn);
                    UpdateContactsOnHikeCounter();
                }
                HikeInstantiation.ViewModel.FavList.Insert(0, cObj);
                UpdateFriendsCounter(); JObject data = new JObject();
                data["id"] = fObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(cObj);
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
                if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                {
                    emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                    favourites.Visibility = System.Windows.Visibility.Visible;
                }
                StatusMessage sm = new StatusMessage(fObj.Msisdn, String.Empty, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1);
                mPubSub.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
                mPubSub.publish(HikePubSub.STATUS_RECEIVED, sm);
            }
        }

        private void no_Click(object sender, RoutedEventArgs e)
        {
            FriendRequestStatusUpdate fObj = (sender as Button).DataContext as FriendRequestStatusUpdate;
            if (fObj != null)
            {
                _buttonInsideStatusUpdateTapped = true;
                JObject data = new JObject();
                data["id"] = fObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.ServerJsonKeys.TYPE] = HikeConstants.ServerJsonKeys.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
                obj[HikeConstants.ServerJsonKeys.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                HikeInstantiation.ViewModel.StatusList.Remove(fObj);
                HikeInstantiation.ViewModel.PendingRequests.Remove(fObj.Msisdn);

                if (HikeInstantiation.ViewModel.StatusList.Count == 0 || (HikeInstantiation.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                    HikeInstantiation.ViewModel.StatusList.Add(DefaultStatus);

                MiscDBUtil.SavePendingRequests();
                FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
            }
        }

        private void notification_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (launchPagePivot.SelectedIndex != 2)
            {
                launchPagePivot.SelectedIndex = 2;

                if (isStatusMessagesLoaded)
                {
                    int index = 0;
                    if (ProTipHelper.CurrentProTip != null)
                        index = 1;

                    int pendingCount = HikeInstantiation.ViewModel.PendingRequests != null ? HikeInstantiation.ViewModel.PendingRequests.Count : 0;
                    //if no new status scroll to latest unseen friends request
                    if (UnreadFriendRequests > 0 && (pendingCount > UnreadFriendRequests))
                    {
                        int x = pendingCount - UnreadFriendRequests;
                        if (x >= 0 && HikeInstantiation.ViewModel.StatusList.Count > (x + index))
                            statusLLS.ScrollTo(HikeInstantiation.ViewModel.StatusList[x + index]); //handling index out of bounds exception
                    }
                    //scroll to latest unread status
                    else if ((HikeInstantiation.ViewModel.StatusList.Count > (pendingCount + index)) && RefreshBarCount > 0) //handling index out of bounds exception
                    {
                        statusLLS.ScrollTo(HikeInstantiation.ViewModel.StatusList[pendingCount + index]);
                    }
                }
            }
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string[] statusImageInfo = new string[2];
            ImageStatus statusUpdate = (sender as Grid).DataContext as ImageStatus;
            if (statusUpdate != null)
            {
                statusImageInfo[0] = statusUpdate.Msisdn;
                statusImageInfo[1] = statusUpdate.ServerId;
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.STATUS_IMAGE_TO_DISPLAY] = statusImageInfo;
                Uri nextPage = new Uri("/View/DisplayImage.xaml", UriKind.Relative);
                NavigationService.Navigate(nextPage);
            }
        }

        //tap event of photo in status bubble
        private void statusBubblePhoto_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            BaseStatusUpdate sb = (sender as Image).DataContext as BaseStatusUpdate;
            if (sb == null)
                return;

            Object[] obj = new Object[2];
            obj[0] = sb.Msisdn;
            obj[1] = sb.UserName;
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_TIMELINE] = obj;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        private void statusItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_buttonInsideStatusUpdateTapped)
            {
                _buttonInsideStatusUpdateTapped = false;
                return;
            }

            BaseStatusUpdate status = (sender as Border).DataContext as BaseStatusUpdate;
            if (status == null || status is ProTipStatusUpdate)
                return;

            if (_hyperlinkedInsideStatusUpdateClicked)
            {
                _hyperlinkedInsideStatusUpdateClicked = false;
                return;
            }

            if (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.ConvMap.ContainsKey(status.Msisdn) && HikeInstantiation.ViewModel.ConvMap[status.Msisdn].IsHidden)
                return;

            if (status.Msisdn == HikeInstantiation.MSISDN)
            {
                Object[] obj = new Object[2];
                obj[0] = status.Msisdn;
                obj[1] = status.UserName;
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_TIMELINE] = obj;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                return;
            }

            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(status.Msisdn))
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_STATUSPAGE] = HikeInstantiation.ViewModel.ConvMap[status.Msisdn];
            else
            {
                ConversationListObject cFav = HikeInstantiation.ViewModel.GetFav(status.Msisdn);
                if (cFav != null)
                {
                    if (!_isFavListBound)
                        cFav.Avatar = MiscDBUtil.getThumbNailForMsisdn(cFav.Msisdn);

                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_STATUSPAGE] = cFav;
                }
                else
                {
                    ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(status.Msisdn);
                    if (contactInfo == null)
                    {
                        contactInfo = new ContactInfo();
                        contactInfo.Msisdn = status.Msisdn;
                        contactInfo.OnHike = true;
                    }
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_STATUSPAGE] = contactInfo;
                }
            }

            NavigationService.Navigate(new Uri("/View/NewChatThread.xaml", UriKind.Relative));
        }

        private void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            Uri nextPage = new Uri("/View/PostStatus.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        private void CircleOfFriends_Click(object sender, RoutedEventArgs e)
        {
            launchPagePivot.SelectedIndex = 1;
        }

        private void statusLLS_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (isStatusMessagesLoaded && statusLLS.ItemsSource != null && statusLLS.ItemsSource.Count > 0 && hasMoreStatus)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as BaseStatusUpdate).Equals(statusLLS.ItemsSource[statusLLS.ItemsSource.Count - 1]))
                    {
                        List<StatusMessage> statusMessagesFromDB = null;
                        shellProgress.IsIndeterminate = true;
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += (s1, ev1) =>
                        {
                            statusMessagesFromDB = GetUnblockedStatusUpdates(HikeConstants.STATUS_SUBSEQUENT_FETCH_COUNT);
                        };
                        bw.RunWorkerAsync();
                        bw.RunWorkerCompleted += (s1, ev1) =>
                        {
                            AddStatusToViewModel(statusMessagesFromDB, HikeConstants.STATUS_SUBSEQUENT_FETCH_COUNT);
                            shellProgress.IsIndeterminate = false;
                        };
                    }
                }
            }
        }
        #endregion

        #region Pro Tips

        private void dismissProTip_Click(object sender, RoutedEventArgs e)
        {
            bool isPresent = false;
            int i;

            for (i = 0; i < HikeInstantiation.ViewModel.StatusList.Count; i++)
            {
                if (HikeInstantiation.ViewModel.StatusList[i] is ProTipStatusUpdate)
                {
                    isPresent = true;
                    break;
                }
            }

            if (!isPresent)
                return;

            HikeInstantiation.ViewModel.StatusList.RemoveAt(i);
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.PRO_TIP_LAST_DISMISS_TIME, DateTime.Now);

            ProTipCount = 0;

            Analytics.SendAnalyticsEvent(HikeConstants.ServerJsonKeys.ST_UI_EVENT, HikeConstants.PRO_TIPS_DISMISSED, ProTipHelper.CurrentProTip._id);

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (ss, ee) =>
                {
                    if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.PRO_TIP))
                    {
                        ProTipHelper.Instance.RemoveCurrentProTip();
                        HikeInstantiation.AppSettings.Remove(HikeConstants.AppSettings.PRO_TIP);
                        HikeInstantiation.AppSettings.Save();
                    }
                };
            worker.RunWorkerAsync();
        }

        void showProTip()
        {
            if (ProTipHelper.CurrentProTip != null)
            {
                if (HikeInstantiation.ViewModel.StatusList != null && HikeInstantiation.ViewModel.StatusList.Count > 0 && HikeInstantiation.ViewModel.StatusList[0] is ProTipStatusUpdate)
                    HikeInstantiation.ViewModel.StatusList.RemoveAt(0);

                var proTipStatus = new ProTipStatusUpdate();
                HikeInstantiation.ViewModel.StatusList.Insert(0, proTipStatus);

                ProTipCount = 1;
            }
        }

        private void ProTipImage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.IMAGE_TO_DISPLAY] = true;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
        }

        #endregion

        private void llsConversations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConversationListObject convListObj = llsConversations.SelectedItem as ConversationListObject;
            if (convListObj == null)
                return;

            llsConversations.SelectedItem = null;

            //if (_profileImageTapped)
            //{
            //    _profileImageTapped = false;
            //    return;
            //}

            //if (ApplicationBar == deleteAppBar)
            //    return;

            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_CONVERSATIONS_PAGE] = convListObj;

            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        bool resumeMediaPlayerAfterDone = false;

        private void PlayAudio()
        {
            Dispatcher.BeginInvoke(() =>
                {
                    if (!MediaPlayer.GameHasControl)
                    {
                        FrameworkDispatcher.Update();
                        MediaPlayer.Pause();
                        resumeMediaPlayerAfterDone = true;
                    }
                    if (App.GlobalMediaElement.Source == null)
                    {
                        App.GlobalMediaElement.Source = new Uri("Audio/v1.mp3", UriKind.Relative);

                        App.GlobalMediaElement.MediaOpened += MediaElement_MediaOpened;//it shows file has been loaded
                        App.GlobalMediaElement.MediaEnded += mediaElement_MediaEnded;
                    }
                    else
                    {
                        App.GlobalMediaElement.Play();
                    }
                });
        }

        private void MediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            App.GlobalMediaElement.Play();
        }

        void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (resumeMediaPlayerAfterDone)
            {
                FrameworkDispatcher.Update();
                MediaPlayer.Resume();
                resumeMediaPlayerAfterDone = false;
            }
        }

        #region Overlay
        void ShowInvitePopups()
        {
            Object[] obj = null;

            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AppSettings.SHOW_POPUP, out obj))
            {
                showFreeMessageOverlay = obj == null;
                if (!showFreeMessageOverlay)
                {
                    Object[] popupDataobj = obj as Object[];
                    customOverlay.Title = popupDataobj[0] == null ? AppResources.InvitePopUp_Rewards_Title : (string)popupDataobj[0];
                    customOverlay.Message = popupDataobj[1] == null ? AppResources.InvitePopUp_Rewards_Message : (string)popupDataobj[1];
                    customOverlay.DisplayImage = UI_Utils.Instance.OverlayRupeeImage;
                }
                else
                {
                    customOverlay.Title = AppResources.InvitePopUp_FreeSMS_Title;
                    customOverlay.Message = AppResources.InvitePopUp_FreeSMS_Message;
                    customOverlay.DisplayImage = UI_Utils.Instance.OverlaySmsImage;
                }
                customOverlay.LeftButtonContent = AppResources.FreeSMS_InviteNow_Btn;
                customOverlay.RightButtonContent = AppResources.InvitePopUp_LearnMore_Btn_Text;

                customOverlay.LeftClicked += customOverlay_LeftClicked;
                customOverlay.RightClicked += customOverlay_RightClicked;
                customOverlay.VisibilityChanged += customOverlay_VisibilityChanged;
                customOverlay.SetVisibility(true);
            }
        }

        void customOverlay_VisibilityChanged(object sender, EventArgs e)
        {
            Overlay overlay = sender as Overlay;
            if (overlay.Visibility == Visibility.Collapsed)
            {
                foreach (ApplicationBarIconButton button in appBar.Buttons)
                    button.IsEnabled = true;

                ApplicationBar.IsMenuEnabled = true;
                launchPagePivot.IsHitTestVisible = true;
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.SHOW_POPUP);
            }
            else
            {
                foreach (ApplicationBarIconButton button in appBar.Buttons)
                    button.IsEnabled = false;

                ApplicationBar.IsMenuEnabled = false;
                launchPagePivot.IsHitTestVisible = false;
            }
        }

        void customOverlay_RightClicked(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        void customOverlay_LeftClicked(object sender, EventArgs e)
        {
            if (showFreeMessageOverlay)
                Analytics.SendClickEvent(HikeConstants.INVITE_FRIENDS_FROM_POPUP_FREE_SMS);
            else
                Analytics.SendClickEvent(HikeConstants.INVITE_FRIENDS_FROM_POPUP_REWARDS);

            NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
        }
        #endregion

        #region FTUE

        private void DefaultStatus_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_POST_STATUS_CLICKED);
            Uri nextPage = new Uri("/View/PostStatus.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        private void SeeAllButton_Click(object sender, RoutedEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_SEE_ALL_CLICKED);
            NavigationService.Navigate(new Uri("/View/ForwardTo.xaml", UriKind.Relative));
        }

        private void DefaultChat_Selected(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var listBox = sender as ListBox;
            ContactInfo c = listBox.SelectedItem as ContactInfo;

            if (c == null)
                return;

            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_START_CHAT_CLICKED);

            StartNewChatWithSelectContact(c);
        }

        private void MenuItem_Click_GoToUserInfo(object sender, RoutedEventArgs e)
        {
            var obj = (sender as MenuItem).DataContext as ConversationListObject;
            if (obj != null)
            {
                if (obj.IsGroupChat)
                {
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.GROUP_ID_FROM_CHATTHREAD] = obj.Msisdn;
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.GROUP_NAME_FROM_CHATTHREAD] = obj.NameToShow;
                    NavigationService.Navigate(new Uri("/View/GroupInfoPage.xaml", UriKind.Relative));
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_CONVERSATION_PAGE] = obj;
                    NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                }
            }
        }

        private void GoToInvite_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_INVITE_CLICKED);
            NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
        }

        private void GoToGroup_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_GROUP_CHAT_CLICKED);
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.START_NEW_GROUP] = true;
            NavigationService.Navigate(new Uri("/View/NewGroup.xaml", UriKind.Relative));
        }

        private void GoToProfile_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_PROFILE_PIC_CLICKED);
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_PROFILE] = null;
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.SET_PROFILE_PIC] = true;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        #endregion

        #region Typing Notification

        void ShowTypingNotification(object sender, object[] vals)
        {
            string typerMsisdn = (string)vals[0];
            string searchBy = vals[1] != null ? (string)vals[1] : typerMsisdn;

            var list = HikeInstantiation.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

            if (list.Count() == 0)
                return;

            ConversationListObject convListObj = (ConversationListObject)list.FirstOrDefault();

            if (vals[1] != null && !convListObj.IsGroupChat)
                return;
            if (convListObj.IsGroupChat)
            {
                GroupManager.Instance.LoadGroupParticipants(searchBy);
                if (GroupManager.Instance.GroupCache != null && GroupManager.Instance.GroupCache.ContainsKey(searchBy))
                {
                    var a = (GroupManager.Instance.GroupCache[searchBy]).Where(gp => gp.Msisdn == typerMsisdn);
                    if (a.Count() > 0)
                    {
                        GroupParticipant gp = (GroupParticipant)a.FirstOrDefault();
                        convListObj.TypingNotificationText = string.Format(AppResources.ConversationList_grp_istyping_txt, gp.Name);
                    }
                }
            }
            else
            {
                convListObj.TypingNotificationText = AppResources.ConversationList_istyping_txt;
            }
        }

        void AutoHidetypingNotification(object sender, object[] vals)
        {
            string typerMsisdn = (string)vals[0];
            string searchBy = vals[1] != null ? (string)vals[1] : typerMsisdn;

            var list = HikeInstantiation.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

            if (list.Count() == 0)
                return;

            ConversationListObject convListObj = (ConversationListObject)list.FirstOrDefault();

            if (vals[1] != null && !convListObj.IsGroupChat)
                return;
            convListObj.AutoHidetypingNotification();
        }

        void HideTypingNotification(object sender, object[] vals)
        {
            string typerMsisdn = (string)vals[0];
            string searchBy = vals[1] != null ? (string)vals[1] : typerMsisdn;

            var list = HikeInstantiation.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

            if (list.Count() == 0)
                return;

            ConversationListObject convListObj = (ConversationListObject)list.FirstOrDefault();

            if (vals[1] != null && !convListObj.IsGroupChat)
                return;
            convListObj.TypingNotificationText = null;
        }

        #endregion

        //hyperlink was clicked in bubble. dont perform actions like page navigation.
        private bool _hyperlinkedInsideStatusUpdateClicked;

        void Hyperlink_Clicked(object sender, EventArgs e)
        {
            _hyperlinkedInsideStatusUpdateClicked = true;

            HikeInstantiation.ViewModel.Hyperlink_Clicked(sender as object[]);
        }

        void ViewMoreMessage_Clicked(object sender, EventArgs e)
        {
            _hyperlinkedInsideStatusUpdateClicked = true;

            HikeInstantiation.ViewModel.ViewMoreMessage_Clicked(sender);
        }

        //bool _profileImageTapped = false;
        //private void profileImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    var conv = (sender as Grid).DataContext as ConversationListObject;

        //    if (ApplicationBar == deleteAppBar)
        //    {
        //        _profileImageTapped = true;

        //        if (conv != null)
        //            conv.IsSelected = !conv.IsSelected;

        //        ChangeAppBarOnConvSelected();
        //    }
        //}

        //private void ChangeAppBarOnConvSelected()
        //{
        //    if (HikeInstantiation.ViewModel.MessageListPageCollection.Where(c => c.IsSelected == true).Count() > 0)
        //    {
        //        if (ApplicationBar != deleteAppBar)
        //        {
        //            launchPagePivot.IsLocked = true;
        //            ApplicationBar = deleteAppBar;
        //            notificationCountGrid.Visibility = Visibility.Collapsed;
        //        }
        //    }
        //    else if (ApplicationBar != appBar)
        //    {
        //        launchPagePivot.IsLocked = false;
        //        ApplicationBar = appBar;
        //        notificationCountGrid.Visibility = Visibility.Visible;
        //    }
        //}

        private void Grid_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //e.Handled = ApplicationBar == deleteAppBar;
        }

        private void chatsTabImage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            launchPagePivot.SelectedIndex = 0;
        }

        private void friendsTabImage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            launchPagePivot.SelectedIndex = 1;
        }

        /// <summary>
        /// this function is for intializing tooltipmode
        /// </summary>
        /// <param name="leftIconSource">left icon source of image </param>
        /// <param name="rightIconSource">right icon source of image</param>
        /// <param name="headerText">header of tool tip</param>
        /// <param name="bodyText">body of tool tip</param>
        /// <param name="isRightIconClickedEnabled">right icon click event enabled or not</param>
        /// <param name="isFullTipTappedEnabled">full tip tap event click event enabled or not</param>
        void InitializeToolTipControl(ImageSource leftIconSource, ImageSource rightIconSource, string headerText, string bodyText,
            bool isRightIconClickedEnabled, bool isFullTipTappedEnabled)
        {
            conversationPageToolTip.ResetToolTip();

            if (_tipMode == ToolTipMode.INVITE_FRIENDS || _tipMode == ToolTipMode.INFORMATIONAL || _tipMode == ToolTipMode.PROFILE_PIC || _tipMode == ToolTipMode.FAVOURITES
                || _tipMode == ToolTipMode.STATUS_UPDATE)
                conversationPageToolTip.ControlBackgroundColor = (SolidColorBrush)App.Current.Resources["TipGreen"];
            else
                conversationPageToolTip.ControlBackgroundColor = (SolidColorBrush)App.Current.Resources["StealthRed"];

            conversationPageToolTip.LeftIconSource = leftIconSource;
            conversationPageToolTip.RightIconSource = rightIconSource;
            conversationPageToolTip.TipText = bodyText;
            conversationPageToolTip.TipHeaderText = headerText;

            conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
            conversationPageToolTip.FullTipTapped -= conversationPageToolTip_FullTipTapped;

            if (isRightIconClickedEnabled)
                conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;

            if (isFullTipTappedEnabled)
                conversationPageToolTip.FullTipTapped += conversationPageToolTip_FullTipTapped;
        }

        #region Hidden Mode

        // Confirm hidden mode password
        bool _isConfirmPassword;

        // Temporary password for confirmation
        string _tempPassword;

        // Tool tip mode
        ToolTipMode _tipMode;

        /// <summary>
        /// Function called when hike logo is tapped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hikeLogo_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0)
            {
                MessageBox.Show(AppResources.HiddenMode_ZeroChatConf_Body_Txt, AppResources.HiddenMode_ZeroChatConf_Header_Txt, MessageBoxButton.OK);
                return;
            }
            else
            {
                if (!HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_MODE_PASSWORD))
                {
                    if (_tipMode == ToolTipMode.HIDDEN_MODE_GETSTARTED)
                        Analytics.SendClickEvent(HikeConstants.ANALYTICS_TAP_HI_WHILE_TIP);
                    else
                        Analytics.SendClickEvent(HikeConstants.ANALYTICS_TAP_HI_WHILE_NO_TIP);
                }

                if (!HikeInstantiation.ViewModel.IsHiddenModeActive)
                {
                    if (launchPagePivot.SelectedIndex != 0)
                        launchPagePivot.SelectedIndex = 0;

                    passwordOverlay.Text = HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_MODE_PASSWORD) ?
                        AppResources.EnterPassword_Txt : AppResources.EnterNewPassword_Txt;

                    passwordOverlay.IsShow = true;
                }
                else
                {
                    ToggleHidddenMode();

                    if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_COMPLETE)
                    {
                        HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS);

                        _tipMode = ToolTipMode.DEFAULT;

                        if (conversationPageToolTip.IsShow)
                            conversationPageToolTip.IsShow = false;
                    }
                }
            }
        }

        /// <summary>
        /// Toggle hidden mode.
        /// </summary>
        void ToggleHidddenMode()
        {
            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                conversationPageToolTip.IsShow = false;

            HikeInstantiation.ViewModel.ToggleHiddenMode();

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (llsConversations.ItemsSource.Count > 0)
                            llsConversations.ScrollTo(llsConversations.ItemsSource[0]);
                    }
                    catch (Exception)
                    {
                        Debug.WriteLine("llsConversations Scroll to null Exception :: HiddenToggleMode");
                    }

                    if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                        ShowFTUECards();
                    else
                        ShowChats();
                });

            SendHiddenModeToggledPacket();
        }

        /// <summary>
        /// Send hidden mode packet toggled to server.
        /// </summary>
        void SendHiddenModeToggledPacket()
        {
            //send qos 0 for toggling for stealth mode on server
            JObject hideObj = new JObject();
            hideObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.HIDDEN_MODE_TYPE);

            JObject data = new JObject();

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_MODE_ACTIVATED))
                data.Add(HikeConstants.ServerJsonKeys.HIDDEN_MODE_ENABLED, true);

            else
                data.Add(HikeConstants.ServerJsonKeys.HIDDEN_MODE_ENABLED, false);

            hideObj.Add(HikeConstants.ServerJsonKeys.DATA, data);

            Object[] objArr = new object[2];
            objArr[0] = hideObj;
            objArr[1] = 0;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, objArr);
        }

        /// <summary>
        /// Mark individual chat as hidden/unhidden.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_HideChat(object sender, RoutedEventArgs e)
        {
            var obj = (sender as MenuItem).DataContext as ConversationListObject;
            if (obj != null)
            {
                obj.IsHidden = !obj.IsHidden;

                if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                {
                    _tipMode = ToolTipMode.HIDDEN_MODE_COMPLETE;
                    UpdateToolTip(true);
                }

                JObject hideObj = new JObject();
                hideObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.STEALTH);

                JObject data = new JObject();
                JArray msisdn = new JArray();
                msisdn.Add(obj.Msisdn);

                if (obj.IsHidden)
                    data.Add(HikeConstants.ServerJsonKeys.CHAT_ENABLED, msisdn);
                else
                    data.Add(HikeConstants.ServerJsonKeys.CHAT_DISABLED, msisdn);

                hideObj.Add(HikeConstants.ServerJsonKeys.DATA, data);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, hideObj);
            }
        }


        /// <summary>
        /// Password has been entered by the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void passwordOverlay_PasswordEntered(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                if (String.IsNullOrWhiteSpace(HikeInstantiation.ViewModel.Password))
                {
                    if (_isConfirmPassword)
                    {
                        if (_tempPassword.Equals(popup.Password))
                        {
                            Analytics.SendClickEvent(HikeConstants.ANALYTICS_HIDDEN_MODE_PASSWORD_CONFIRMATION);

                            HikeInstantiation.ViewModel.Password = popup.Password;
                            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.HIDDEN_MODE_PASSWORD, HikeInstantiation.ViewModel.Password);
                            ToggleHidddenMode();

                            _tipMode = ToolTipMode.HIDDEN_MODE_STEP2;
                            UpdateToolTip(true);
                        }
                        else
                            MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.Password_Mismatch_Txt, MessageBoxButton.OK);

                        _isConfirmPassword = false;
                        popup.IsShow = false;
                    }
                    else
                    {
                        _isConfirmPassword = true;
                        _tempPassword = popup.Password;
                        popup.Text = AppResources.ConfirmPassword_Txt;
                        popup.Password = String.Empty;
                    }
                }
                else if (HikeInstantiation.ViewModel.Password == popup.Password)
                {
                    ToggleHidddenMode();
                    popup.IsShow = false;

                    if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                        UpdateToolTip(false);
                }
                else
                {
                    popup.IsShow = false;
                }
            }
        }

        /// <summary>
        /// Handle password visibility changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void passwordOverlay_PasswordOverlayVisibilityChanged(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                SystemTray.IsVisible = popup.IsShow ? false : true;
                ApplicationBar.IsVisible = popup.IsShow ? false : true;
                headerGrid.IsHitTestVisible = popup.IsShow ? false : true;
                tipControl.IsHitTestVisible = popup.IsShow ? false : true;
                launchPagePivot.IsHitTestVisible = popup.IsShow ? false : true;
            }
        }

        /// <summary>
        /// Show reset hidden mode tool tip.
        /// </summary>
        private void ShowHiddenModeResetToolTip()
        {
            long resetTime;
            if (HikeInstantiation.AppSettings.TryGetValue<long>(HikeConstants.AppSettings.HIDDEN_MODE_RESET_TIME, out resetTime))
            {
                _resetTimeSeconds = HikeConstants.HIDDEN_MODE_RESET_TIMER - (TimeUtils.getCurrentTimeStamp() - resetTime);
                if (_resetTimeSeconds > 0)
                {
                    var resetTimeTimeSpan = TimeSpan.FromSeconds(1);
                    if (_resetTimer == null)
                    {
                        _resetTimer = new DispatcherTimer();
                        _resetTimer.Interval = resetTimeTimeSpan;
                        _resetTimer.Tick -= _resetTimer_Tick;
                        _resetTimer.Tick += _resetTimer_Tick;
                    }

                    if (!_resetTimer.IsEnabled)
                        _resetTimer.Start();
                }
                else
                {
                    _tipMode = ToolTipMode.RESET_HIDDEN_MODE_COMPLETED;
                    UpdateToolTip(true);
                }
            }
        }

        /// <summary>
        /// Update reset timer after resuming app on home page.
        /// </summary>
        private void UpdateResetHiddenModeTimer()
        {
            long resetTime;
            if (HikeInstantiation.AppSettings.TryGetValue<long>(HikeConstants.AppSettings.HIDDEN_MODE_RESET_TIME, out resetTime))
            {
                _resetTimeSeconds = HikeConstants.HIDDEN_MODE_RESET_TIMER - (TimeUtils.getCurrentTimeStamp() - resetTime);
                if (_resetTimeSeconds <= 0)
                {
                    if (_resetTimer != null)
                        _resetTimer.Stop();

                    _tipMode = ToolTipMode.RESET_HIDDEN_MODE_COMPLETED;
                    UpdateToolTip(true);
                }
                else
                {
                    _tipMode = ToolTipMode.RESET_HIDDEN_MODE;
                    UpdateToolTip(true);
                }
            }
        }

        /// <summary>
        /// Reset timer tick event. Update timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _resetTimer_Tick(object sender, EventArgs e)
        {
            if (_resetTimeSeconds <= 0)
            {
                if (_resetTimer != null)
                    _resetTimer.Stop();

                _tipMode = ToolTipMode.RESET_HIDDEN_MODE_COMPLETED;
                UpdateToolTip(true);
            }
            else
            {
                _tipMode = ToolTipMode.RESET_HIDDEN_MODE;
                UpdateToolTip(true);
            }

            _resetTimeSeconds--;
        }

        /// <summary>
        /// Update tool tip based on tip mode status.
        /// </summary>
        /// <param name="isModeChanged">is the mode changed. If yes then reset tool tip values.</param>
        void UpdateToolTip(bool isModeChanged)
        {

            switch (_tipMode)
            {
                case ToolTipMode.DEFAULT:

                    break;

                case ToolTipMode.HIDDEN_MODE_GETSTARTED:

                    if (isModeChanged)
                        InitializeToolTipControl(UI_Utils.Instance.ToolTipArrow, UI_Utils.Instance.ToolTipCrossIcon, null, AppResources.HiddenMode_GetStarted_Txt, true, false);

                    break;

                case ToolTipMode.HIDDEN_MODE_STEP2:

                    if (!HikeInstantiation.ViewModel.IsHiddenModeActive)
                        return;

                    if (isModeChanged)
                        InitializeToolTipControl(UI_Utils.Instance.SheildIcon, UI_Utils.Instance.ToolTipCrossIcon, null, AppResources.HiddenMode_Step2_Txt, true, false);

                    break;

                case ToolTipMode.HIDDEN_MODE_COMPLETE:

                    if (isModeChanged)
                        InitializeToolTipControl(UI_Utils.Instance.ToolTipArrow, UI_Utils.Instance.ToolTipCrossIcon, null, AppResources.HiddenMode_Completed_Txt, true, false);

                    break;

                case ToolTipMode.RESET_HIDDEN_MODE:

                    if (isModeChanged)
                        InitializeToolTipControl(UI_Utils.Instance.SheildIcon, UI_Utils.Instance.ToolTipCrossIcon, null, String.Format(AppResources.ResetTip_Txt, Utils.GetFormattedTimeFromSeconds(_resetTimeSeconds)), true, false);

                    break;

                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:

                    if (isModeChanged)
                        InitializeToolTipControl(UI_Utils.Instance.SheildIcon, UI_Utils.Instance.ToolTipCrossIcon, null, AppResources.HiddenModeReset_Completed_Txt, true, true);

                    break;

                case ToolTipMode.PROFILE_PIC:

                    if (!MiscDBUtil.HasCustomProfileImage(HikeConstants.MY_PROFILE_PIC))
                        InitializeToolTipControl(UI_Utils.Instance.ToolTipProfilePic, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ConversationPageTip.HeaderText, TipManager.ConversationPageTip.BodyText, true, true);
                    else
                        HideTips();

                    break;

                case ToolTipMode.STATUS_UPDATE:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipStatusUpdate, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ConversationPageTip.HeaderText, TipManager.ConversationPageTip.BodyText, true, true);
                    break;

                case ToolTipMode.INFORMATIONAL:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipInformational, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ConversationPageTip.HeaderText, TipManager.ConversationPageTip.BodyText, true, false);
                    break;

                case ToolTipMode.INVITE_FRIENDS:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipInvite, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ConversationPageTip.HeaderText, TipManager.ConversationPageTip.BodyText, true, true);
                    break;

                case ToolTipMode.FAVOURITES:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipFavourites, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ConversationPageTip.HeaderText, TipManager.ConversationPageTip.BodyText, true, true);
                    break;
            }

            if (_tipMode != ToolTipMode.DEFAULT)
            {
                if (!conversationPageToolTip.IsShow)
                    conversationPageToolTip.IsShow = true;

                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS, _tipMode);
            }
            else if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS))
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS);
        }

        /// <summary>
        /// Full tool tip tapped.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void conversationPageToolTip_FullTipTapped(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:

                    MessageBoxResult mBox = MessageBox.Show(AppResources.HiddenModeReset_FinalConf_Body_Txt, AppResources.HiddenModeReset_FinalConf_Header_Txt, MessageBoxButton.OKCancel);

                    if (mBox == MessageBoxResult.OK)
                    {
                        ResetHiddenMode();

                        if (_resetTimer != null)
                        {
                            _resetTimer.Stop();
                            _resetTimer = null;
                        }
                    }
                    else
                        HideTips();

                    HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_MODE_RESET_TIME);

                    break;

                case ToolTipMode.PROFILE_PIC:

                    HideTips();

                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_PROFILE] = null;
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.SET_PROFILE_PIC] = true;
                    Analytics.SendClickEvent(HikeConstants.ServerTips.PROFILE_PIC_TIP_TAP_EVENT);

                    NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                    break;

                case ToolTipMode.STATUS_UPDATE:

                    HideTips();

                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.USERINFO_FROM_PROFILE] = null;

                    Analytics.SendClickEvent(HikeConstants.ServerTips.STATUS_TIP_TAP_EVENT);
                    NavigationService.Navigate(new Uri("/View/PostStatus.xaml", UriKind.Relative));
                    break;

                case ToolTipMode.INVITE_FRIENDS:

                    HideTips();

                    Analytics.SendClickEvent(HikeConstants.ServerTips.INVITE_TIP_TAP_EVENT);
                    NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
                    break;

                case ToolTipMode.FAVOURITES:

                    HideTips();

                    Analytics.SendClickEvent(HikeConstants.ServerTips.FAVOURITE_TIP_TAP_EVENT);
                    launchPagePivot.SelectedIndex = 1;

                    break;
            }
        }

        /// <summary>
        /// Right icon clicked in tool tip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void conversationPageToolTip_RightIconClicked(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.RESET_HIDDEN_MODE:

                    if (_resetTimer != null)
                        _resetTimer.Stop();

                    MessageBoxResult mBox = MessageBox.Show(AppResources.HiddenModeReset_CancelConf_Body_Txt, AppResources.HiddenModeReset_CancelConf_Header_Txt, MessageBoxButton.OKCancel);

                    if (mBox == MessageBoxResult.OK)
                    {
                        HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_MODE_RESET_TIME);

                        if (_resetTimer != null)
                        {
                            _resetTimer.Stop();
                            _resetTimer = null;
                        }

                        HideTips();
                    }
                    else
                    {
                        if (_resetTimer != null)
                            _resetTimer.Start();
                    }

                    break;
                case ToolTipMode.HIDDEN_MODE_GETSTARTED:

                    HideTips();
                    break;

                case ToolTipMode.HIDDEN_MODE_STEP2:

                    HideTips();
                    break;

                case ToolTipMode.HIDDEN_MODE_COMPLETE:

                    HideTips();
                    break;

                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:


                    MessageBoxResult mBox1 = MessageBox.Show(AppResources.HiddenModeReset_CancelConf_Body_Txt, AppResources.HiddenModeReset_CancelConf_Header_Txt, MessageBoxButton.OKCancel);

                    if (mBox1 == MessageBoxResult.OK)
                    {
                        HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_MODE_RESET_TIME);

                        if (_resetTimer != null)
                        {
                            _resetTimer.Stop();
                            _resetTimer = null;
                        }
                        HideTips();

                    }

                    break;

                case ToolTipMode.PROFILE_PIC:

                    HideTips();
                    break;

                case ToolTipMode.STATUS_UPDATE:

                    HideTips();
                    break;

                case ToolTipMode.INFORMATIONAL:

                    HideTips();
                    break;

                case ToolTipMode.INVITE_FRIENDS:

                    HideTips();
                    break;

                case ToolTipMode.FAVOURITES:

                    HideTips();
                    break;
            }
        }

        /// <summary>
        /// Reset hidden mode, remove saved pasword, reset tooltip and delete chats.
        /// </summary>
        void ResetHiddenMode()
        {
            _isConfirmPassword = false;
            _tempPassword = null;

            RemoveHiddenModePassword();

            HikeInstantiation.ViewModel.ResetHiddenMode();

            ResetHiddenModeToolTip();

            DeleteHiddenChats();

            SendResetPacketToServer();
        }

        /// <summary>
        /// Reset hidden mode password.
        /// </summary>
        private void RemoveHiddenModePassword()
        {
            HikeInstantiation.ViewModel.Password = null;
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.HIDDEN_MODE_PASSWORD);
        }

        /// <summary>
        /// Reset hidden mode tooltip.
        /// </summary>
        private void ResetHiddenModeToolTip()
        {
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.HIDDEN_TOOLTIP_STATUS, ToolTipMode.HIDDEN_MODE_GETSTARTED);
            _tipMode = ToolTipMode.HIDDEN_MODE_GETSTARTED;
            UpdateToolTip(true);
        }

        /// <summary>
        /// Delete hidden chats.
        /// </summary>
        void DeleteHiddenChats()
        {
            var list = HikeInstantiation.ViewModel.MessageListPageCollection.Where(c => c.IsHidden);

            if (list != null && list.Count() > 0)
            {
                for (int i = 0; i < HikeInstantiation.ViewModel.MessageListPageCollection.Count; )
                {
                    if (HikeInstantiation.ViewModel.MessageListPageCollection[i].IsHidden)
                    {
                        var convObj = HikeInstantiation.ViewModel.MessageListPageCollection[i];
                        DeleteConversation(convObj, false);
                    }
                    else
                        i++;
                }

                if (HikeInstantiation.ViewModel.MessageListPageCollection.Count == 0 || (!HikeInstantiation.ViewModel.IsHiddenModeActive && HikeInstantiation.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                    ShowFTUECards();
            }
        }

        /// <summary>
        /// Send reset hidden mode packet to server
        /// </summary>
        void SendResetPacketToServer()
        {
            JObject hideObj = new JObject();
            hideObj.Add(HikeConstants.ServerJsonKeys.TYPE, HikeConstants.ServerJsonKeys.STEALTH);

            JObject data = new JObject();
            data.Add(HikeConstants.ServerJsonKeys.RESET, true);

            hideObj.Add(HikeConstants.ServerJsonKeys.DATA, data);
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, hideObj);
        }

        void ViewModel_ResetHiddenModeClicked(object sender, EventArgs e)
        {
            ShowHiddenModeResetToolTip();
        }

        /// <summary>
        /// hide tool tip control
        /// </summary>
        void HideTips()
        {
            if ((_tipMode == ToolTipMode.INVITE_FRIENDS || _tipMode == ToolTipMode.INFORMATIONAL || _tipMode == ToolTipMode.PROFILE_PIC || _tipMode == ToolTipMode.FAVOURITES
                || _tipMode == ToolTipMode.STATUS_UPDATE) && TipManager.ConversationPageTip != null)
                TipManager.Instance.RemoveTip(TipManager.ConversationPageTip.TipId);

            conversationPageToolTip.IsShow = false;
            _tipMode = ToolTipMode.DEFAULT;
            UpdateToolTip(true);
        }

        #endregion

        #region SERVER TIPS

        /// <summary>
        /// showing server tips
        /// </summary>
        void ShowServerTips()
        {

            if (_tipMode != ToolTipMode.HIDDEN_MODE_GETSTARTED && _tipMode != ToolTipMode.HIDDEN_MODE_STEP2 && _tipMode != ToolTipMode.HIDDEN_MODE_COMPLETE
                && _tipMode != ToolTipMode.RESET_HIDDEN_MODE && _tipMode != ToolTipMode.RESET_HIDDEN_MODE_COMPLETED && TipManager.ConversationPageTip != null)
            {
                _tipMode = TipManager.ConversationPageTip.TipType;
                UpdateToolTip(true);
            }

        }
        #endregion

    }

}