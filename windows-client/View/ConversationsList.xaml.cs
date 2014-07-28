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

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region Instances
        bool isDeleteAllChats = false;
        bool _isFavListBound = false;
        private bool firstLoad = true;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
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
            App.RemoveKeyFromAppSettings(HikeConstants.PHONE_ADDRESS_BOOK);

            if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                this.Loaded += ConversationsList_Loaded;

            ProTipHelper.Instance.ShowProTip -= Instance_ShowProTip;
            ProTipHelper.Instance.ShowProTip += Instance_ShowProTip;

            App.ViewModel.StatusNotificationsStatusChanged -= ViewModel_statusNotificationsStatusChanged;
            App.ViewModel.StatusNotificationsStatusChanged += ViewModel_statusNotificationsStatusChanged;

            if (ProTipHelper.CurrentProTip != null)
                showProTip();

            int tipCount;
            App.appSettings.TryGetValue(App.PRO_TIP_COUNT, out tipCount);
            ProTipCount = tipCount;

            App.ViewModel.ShowTypingNotification += ShowTypingNotification;
            App.ViewModel.AutohideTypingNotification += AutoHidetypingNotification;

            appSettings.TryGetValue(App.ACCOUNT_NAME, out _userName);

            _firstName = Utils.GetFirstName(_userName);

            headerIcon.Source = App.appSettings.Contains(HikeConstants.HIDDEN_MODE) ? UI_Utils.Instance.HiddenModeHeaderIcon : UI_Utils.Instance.HeaderIcon;
            App.appSettings.TryGetValue(HikeConstants.HIDDEN_MODE_PASSWORD, out _password);

            App.appSettings.TryGetValue(HikeConstants.HIDDEN_TOOLTIP_STATUS, out _tipMode);
        }

        string _firstName;
        string _userName;

        void ViewModel_statusNotificationsStatusChanged(object sender, EventArgs e)
        {
            byte statusSettingsValue;
            App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue);

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

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GO_TO_CONV_VIEW))
            {
                launchPagePivot.SelectedIndex = 0;
                PhoneApplicationService.Current.State.Remove(HikeConstants.GO_TO_CONV_VIEW);
            }

            if (launchPagePivot.SelectedIndex == 2)
                TotalUnreadStatuses = 0;

            this.llsConversations.SelectedItem = null;
            this.statusLLS.SelectedItem = null;

            App.IS_TOMBSTONED = false;
            App.APP_LAUNCH_STATE = App.LaunchState.NORMAL_LAUNCH;
            App.newChatThreadPage = null;

            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (firstLoad)
            {
                shellProgress.IsIndeterminate = true;
                mPubSub = App.HikePubSubInstance;
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

                        if (!appSettings.Contains(HikeConstants.SHOW_CHAT_FTUE))
                        {
                            if (App.appSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE))
                                ShowAppUpdateAvailableMessage();
                            else
                                ShowInvitePopups();
                        }
                    };

                bw.RunWorkerAsync();

                #endregion

                App.WriteToIsoStorageSettings(HikeConstants.SHOW_GROUP_CHAT_OVERLAY, true);
                firstLoad = false;
            }
            // this should be called only if its not first load as it will get called in first load section
            else if (App.ViewModel.MessageListPageCollection.Count == 0 || (!App.ViewModel.IsHiddenModeActive && App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
            {
                ShowFTUECards();
                UpdateLayout();
            }
            else
            {
                ShowChats();

                if (App.ViewModel.IsConversationUpdated && App.ViewModel.MessageListPageCollection[0] != null)
                {
                    try
                    {
                        llsConversations.ScrollTo(App.ViewModel.MessageListPageCollection[0]);
                    }
                    catch
                    {
                        Debug.WriteLine("llsConversations Scroll to null Exception :: OnNavigatedTo");
                    }
                    App.ViewModel.IsConversationUpdated = false;
                }
            }

            byte statusSettingsValue;
            _isStatusUpdatesNotMute = App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue) && statusSettingsValue == 0;

            if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                launchPagePivot.SelectedIndex = 2;

            if (!conversationPageToolTip.IsShow) // dont show reset if its already being shown
            {
                long resetTime;
                if (App.appSettings.TryGetValue<long>(HikeConstants.HIDDEN_MODE_RESET_TIME, out resetTime))
                {
                    _resetTimeSeconds = 10 - (TimeUtils.getCurrentTimeStamp() - resetTime);
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
                        _isModeChanged = true;
                        UpdateToolTip();
                    }
                }
            }

            FrameworkDispatcher.Update();
        }

        void _resetTimer_Tick(object sender, EventArgs e)
        {
            if (_resetTimeSeconds <= 0)
            {
                if (_resetTimer != null)
                    _resetTimer.Stop();

                _tipMode = ToolTipMode.RESET_HIDDEN_MODE_COMPLETED;
                _isModeChanged = true;
                UpdateToolTip();
            }
            else
            {
                _tipMode = ToolTipMode.RESET_HIDDEN_MODE;
                _isModeChanged = true;
                UpdateToolTip();
            }

            _resetTimeSeconds--;
        }

        ToolTipMode _tipMode;

        void conversationPageToolTip_RightIconClicked(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.RESET_HIDDEN_MODE:
                    conversationPageToolTip.IsShow = false;
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_MODE_RESET_TIME);
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS);
                    _tipMode = ToolTipMode.DEFAULT;
                    
                    if (_resetTimer != null)
                    {
                        _resetTimer.Stop();
                        _resetTimer = null;
                    }
                    
                    break;
                case ToolTipMode.HIDDEN_MODE_GETSTARTED:
                    conversationPageToolTip.IsShow = false;
                    break;
                case ToolTipMode.HIDDEN_MODE_STEP2:
                    conversationPageToolTip.IsShow = false;
                    break;
                case ToolTipMode.HIDDEN_MODE_COMPLETE:
                    conversationPageToolTip.IsShow = false;
                    break;
                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:
                    conversationPageToolTip.IsShow = false;
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_MODE_RESET_TIME);
                    ResetHiddenMode();
                    
                    if (_resetTimer != null)
                    {
                        _resetTimer.Stop();
                        _resetTimer = null;
                    }

                    _isModeChanged = true;
                    UpdateToolTip();
                    break;
            }
        }

        void ResetHiddenMode()
        {
            App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_MODE_PASSWORD);
            _confirmPassword = false;
            _password = _tempPassword = null; 
            App.ViewModel.ResetHiddenMode();
            headerIcon.Source = UI_Utils.Instance.HeaderIcon;

            _tipMode = ToolTipMode.HIDDEN_MODE_GETSTARTED;
            App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS, ToolTipMode.HIDDEN_MODE_GETSTARTED);

            //to:do delete hidden chats and send server reset packet
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
            favCollectionView.Source = App.ViewModel.FavList;

            contactGrid.RowDefinitions[0].Height = GridLength.Auto;
            cohProgressBar.Visibility = Visibility.Collapsed;

            txtCircleOfFriends.Visibility = Visibility.Visible;
            txtContactsOnHike.Visibility = Visibility.Visible;

            favourites.Visibility = App.ViewModel.FavList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            emptyListPlaceholderFiends.Visibility = App.ViewModel.FavList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
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
        //    App.RemoveKeyFromAppSettings(App.SHOW_STATUS_UPDATES_TUTORIAL);
        //}

        #endregion

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
        private void loadingCompleted()
        {
            shellProgress.IsIndeterminate = false;
            llsConversations.ItemsSource = App.ViewModel.MessageListPageCollection;

            if (App.ViewModel.MessageListPageCollection.Count == 0 || (!App.ViewModel.IsHiddenModeActive && App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
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

            App.MqttManagerInstance.connect();

            if (App.appSettings.Contains(HikeConstants.IS_NEW_INSTALLATION) || App.appSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE))
            {
                if (App.appSettings.Contains(HikeConstants.IS_NEW_INSTALLATION))
                    Utils.RequestHikeBot();
                else
                    Utils.requestAccountInfo();
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, Utils.deviceInforForAnalytics());
                App.RemoveKeyFromAppSettings(HikeConstants.IS_NEW_INSTALLATION);
                App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE);
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

            if (profileFTUECard.Visibility == Visibility.Visible && MiscDBUtil.hasCustomProfileImage(App.MSISDN))
                profileFTUECard.Visibility = Visibility.Collapsed;

            if (String.IsNullOrEmpty(groupCountCard.Text))
                groupCountCard.Text = String.Format(AppResources.Conversations_FTUE_Group_SubTxt, HikeConstants.MAX_GROUP_MEMBER_SIZE);

            if (h2oFTUECard.Visibility == Visibility.Collapsed)
            {
                bool showFreeSMS = true;
                App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out showFreeSMS);
                if (showFreeSMS && App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))
                    h2oFTUECard.Visibility = Visibility.Visible;
            }

            if (peopleOnHikeListBox.ItemsSource == null)
            {
                List<ContactInfo> cl = null;
                List<string> contacts = null;
                App.appSettings.TryGetValue(HikeConstants.AppSettings.CONTACTS_TO_SHOW, out contacts);

                if (contacts == null)
                    return;

                cl = new List<ContactInfo>();
                ContactInfo cn;
                foreach (var msisdn in contacts)
                {
                    if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                        cn = App.ViewModel.ContactsCache[msisdn];
                    else
                    {
                        cn = UsersTableUtils.getHikeContactInfoFromMSISDN(msisdn);
                        if (cn != null)
                            App.ViewModel.ContactsCache[msisdn] = cn;
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
            if (!App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue)) // settings dont exist on new sign up, hence on by default
                statusSettingsValue = (byte)1;
            muteStatusMenu.Text = statusSettingsValue > 0 ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;
            muteStatusMenu.Click += muteStatusMenu_Click;

            inviteMenu = new ApplicationBarMenuItem();
            inviteMenu.Text = AppResources.Conversations_TellFriend_Txt;
            inviteMenu.Click += inviteMenu_Click;
            inviteMenu.IsEnabled = false;//it will be enabled after loading of all conversations
            appBar.MenuItems.Add(inviteMenu);

            bool showRewards;
            if (App.appSettings.TryGetValue<bool>(HikeConstants.SHOW_REWARDS, out showRewards) && showRewards == true)
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
            if (!App.appSettings.Contains(HikeConstants.REWARDS_TOKEN))
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
                App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)1);
                settingsValue = 0;
            }
            else
            {
                App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)0);
                settingsValue = -1;
            }

            muteStatusMenu.Text = _isStatusUpdatesNotMute ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;

            _isStatusUpdatesNotMute = !_isStatusUpdatesNotMute;

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_SU, settingsValue);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        void profileMenu_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_PROFILE] = null;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        void settingsMenu_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.Relative));
        }

        //void deleteChatIconButton_Click(object sender, EventArgs e)
        //{
        //    var list = App.ViewModel.MessageListPageCollection.Where(c => c.IsSelected == true).ToList();
        //    string message = list.Count > 1 ? AppResources.Conversations_Delete_MoreThan1Chat_Confirmation : AppResources.Conversations_Delete_Chat_Confirmation;

        //    MessageBoxResult result = MessageBox.Show(message, AppResources.Conversations_DelChat_Txt, MessageBoxButton.OKCancel);
        //    if (result != MessageBoxResult.OK)
        //    {
        //        foreach (var item in list)
        //            item.IsSelected = false;

        //        //ChangeAppBarOnConvSelected();

        //        return;
        //    }

        //    for (int i = 0; i < App.ViewModel.MessageListPageCollection.Count;)
        //    {
        //        if (App.ViewModel.MessageListPageCollection[i].IsSelected)
        //        {
        //            var conv = App.ViewModel.MessageListPageCollection[i];
        //            App.ViewModel.MessageListPageCollection.RemoveAt(i);
        //            deleteConversation(conv);
        //            continue;
        //        }

        //        i++;
        //    }

        //    ChangeAppBarOnConvSelected();
        //}

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
        //    if (App.appSettings.TryGetValue<bool>(HikeConstants.SHOW_REWARDS, out showRewards) && showRewards == true)
        //        rewardsPanel.Visibility = Visibility.Visible;

        //    int rew_val = 0;

        //    string name;
        //    appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
        //    if (name != null)
        //        accountName.Text = name;
        //    int smsCount = 0;
        //    App.appSettings.TryGetValue<int>(App.SMS_SETTING, out smsCount);
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

            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
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

        private void deleteConversation(ConversationListObject convObj)
        {
            App.ViewModel.ConvMap.Remove(convObj.Msisdn); // removed entry from map for UI
            App.ViewModel.MessageListPageCollection.Remove(convObj); // removed from observable collection

            if (App.ViewModel.MessageListPageCollection.Count == 0 || (!App.ViewModel.IsHiddenModeActive && App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
                ShowFTUECards();

            if (Utils.isGroupConversation(convObj.Msisdn)) // if group conv , leave the group too.
            {
                JObject jObj = new JObject();
                jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
                jObj[HikeConstants.TO] = convObj.Msisdn;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);
            }
            mPubSub.publish(HikePubSub.DELETE_CONVERSATION, convObj.Msisdn);

            if (convObj.IsHidden)
            {
                JObject hideObj = new JObject();
                hideObj.Add(HikeConstants.TYPE, HikeConstants.STEALTH);

                JObject data = new JObject();
                JArray msisdn = new JArray();
                msisdn.Add(convObj.Msisdn);
                data.Add(HikeConstants.CHAT_DISABLED, msisdn);

                hideObj.Add(HikeConstants.DATA, data);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, hideObj);
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
                // there will be two background workers that will independently load three sections
                #region FAVOURITES

                if (!_isFavListBound)
                {
                    _isFavListBound = true;
                    BackgroundWorker favBw = new BackgroundWorker();
                    cohProgressBar.Visibility = Visibility.Visible;
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
                                if (!msisdns.Contains(cinfoTemp.Msisdn) && !App.ViewModel.Isfavourite(cinfoTemp.Msisdn) && !App.ViewModel.BlockedHashset.Contains(cinfoTemp.Msisdn) && cinfoTemp.Msisdn != App.MSISDN)
                                {
                                    msisdns.Add(cinfoTemp.Msisdn);
                                    hikeContactList.Add(cinfoTemp);
                                    if (!App.ViewModel.ContactsCache.ContainsKey(cinfoTemp.Msisdn))
                                        App.ViewModel.ContactsCache[cinfoTemp.Msisdn] = cinfoTemp;
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
                        favCollectionView.Source = App.ViewModel.FavList; // this is done to sort in view
                        favourites.SelectedIndex = -1;
                        hikeContactListBox.SelectedIndex = -1;

                        UpdateFriendsCounter();

                        txtCircleOfFriends.Visibility = Visibility.Visible;
                        UpdateContactsOnHikeCounter();
                        txtContactsOnHike.Visibility = Visibility.Visible;
                        if (App.ViewModel.FavList.Count > 0)
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
                        App.ViewModel.LoadPendingRequests();
                        //corresponding counters should be handled for eg unread count
                        statusMessagesFromDBUnblocked = GetUnblockedStatusUpdates(HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                    };
                    statusBw.RunWorkerAsync();
                    shellProgress.IsIndeterminate = true;

                    statusBw.RunWorkerCompleted += (ss, ee) =>
                    {
                        shellProgress.IsIndeterminate = false;

                        foreach (ConversationListObject co in App.ViewModel.PendingRequests.Values)
                        {
                            var friendRequest = new FriendRequestStatusUpdate(co);
                            App.ViewModel.StatusList.Add(friendRequest);
                        }

                        AddStatusToViewModel(statusMessagesFromDBUnblocked, HikeConstants.STATUS_INITIAL_FETCH_COUNT);

                        this.statusLLS.ItemsSource = App.ViewModel.StatusList;

                        if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                            App.ViewModel.StatusList.Add(DefaultStatus);

                        RefreshBarCount = 0;
                        UnreadFriendRequests = 0;

                        if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                        {
                            NetworkManager.turnOffNetworkManager = false;
                            PhoneApplicationService.Current.State.Remove("IsStatusPush");
                        }

                        isStatusMessagesLoaded = true;
                    };
                    //if (appSettings.Contains(App.SHOW_STATUS_UPDATES_TUTORIAL))
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
                        statusLLS.ItemsSource = App.ViewModel.StatusList;
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
                    if (!App.ViewModel.BlockedHashset.Contains(listStatusUpdate[i].Msisdn))
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
                        App.ViewModel.StatusList.Add(status);
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

                if (!isDeleteAllChats) // this is to avoid exception caused due to deleting all chats while receiving msgs
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            ShowChats();

                            if (App.ViewModel.MessageListPageCollection.Count > 0 && App.ViewModel.MessageListPageCollection[0] != null)
                                llsConversations.ScrollTo(App.ViewModel.MessageListPageCollection[0]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ConversationList ::  onEventReceived,MESSAGE_RECEIVED  , Exception : " + ex.StackTrace);
                        }
                    });
                }

                if (App.newChatThreadPage == null && showPush && (!Utils.isGroupConversation(mObj.Msisdn) || !mObj.IsMute) && Utils.ShowNotificationAlert())
                {
                    bool isHikeJingleEnabled = true;
                    App.appSettings.TryGetValue<bool>(App.HIKEJINGLE_PREF, out isHikeJingleEnabled);
                    if (isHikeJingleEnabled)
                    {
                        PlayAudio();
                    }
                    bool isVibrateEnabled = true;
                    App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
                    if (isVibrateEnabled)
                    {
                        VibrateController vibrate = VibrateController.Default;
                        vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                    }
                    appSettings[HikeConstants.LAST_NOTIFICATION_TIME] = DateTime.Now.Ticks;
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

                    if (App.ViewModel.FavList.Count == 0 && emptyListPlaceholderFiends.Visibility == Visibility.Collapsed) // remove fav
                    {
                        emptyListPlaceholderFiends.Visibility = Visibility.Visible;
                        favourites.Visibility = Visibility.Collapsed;
                        //addFavsPanel.Opacity = 0;
                    }
                    else if (App.ViewModel.FavList.Count > 0 && emptyListPlaceholderFiends.Visibility == Visibility.Visible)
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
                if (!App.ViewModel.IsPendingListLoaded || !isStatusMessagesLoaded)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ConversationListObject co = (ConversationListObject)obj;
                    if (co != null)
                    {
                        // if isStatusMessagesLoaded & pending list are not loaded simply ignore this packet , as then this packet will
                        // be shown twice , one here and one from DB.
                        if (isStatusMessagesLoaded && App.ViewModel.IsPendingListLoaded)
                        {
                            int index = 0;
                            if (ProTipHelper.CurrentProTip != null)
                                index = 1;

                            if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatus)
                                App.ViewModel.StatusList.RemoveAt(index);

                            FriendRequestStatusUpdate frs = new FriendRequestStatusUpdate(co);
                            App.ViewModel.StatusList.Insert(index, frs);

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
                    //if its image update and status are laoded, update each status userImage async
                    if (sm.Status_Type == StatusMessage.StatusType.PROFILE_PIC_UPDATE && isStatusMessagesLoaded)
                    {
                        UpdateUserImageInStatus(sm.Msisdn);
                    }

                    if (sm.Msisdn == App.MSISDN || sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
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

                                if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatus)
                                    App.ViewModel.StatusList.RemoveAt(index);
                                int count = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count : 0;
                                App.ViewModel.StatusList.Insert(count + index, status);
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

                                    if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatus)
                                        App.ViewModel.StatusList.RemoveAt(index);
                                    int count = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count : 0;
                                    App.ViewModel.StatusList.Insert(count + index, status);
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
                        App.ViewModel.StatusList.Remove(sb);
                        if (sb.IsUnread)
                        {
                            _totalUnreadStatuses--;
                            RefreshBarCount--;
                        }

                        if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                            App.ViewModel.StatusList.Add(DefaultStatus);
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

                        if (c != null && c.Msisdn != App.MSISDN)
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
                    if (App.ViewModel.ConvMap.ContainsKey(ms))
                    {
                        ConversationListObject co = App.ViewModel.ConvMap[ms];
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
                        if (App.ViewModel.StatusList != null)
                        {
                            for (int i = 0; i < App.ViewModel.StatusList.Count; i++)
                            {
                                if (App.ViewModel.StatusList[i] is FriendRequestStatusUpdate)
                                {
                                    FriendRequestStatusUpdate f = App.ViewModel.StatusList[i] as FriendRequestStatusUpdate;
                                    if (f.Msisdn == c.Msisdn)
                                    {
                                        Dispatcher.BeginInvoke(() =>
                                        {
                                            if (i < UnreadFriendRequests)
                                                UnreadFriendRequests--;

                                            try
                                            {
                                                App.ViewModel.StatusList.Remove(f);

                                                if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                                                    App.ViewModel.StatusList.Add(DefaultStatus);
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
                       if (favCollectionView.Source != null && App.ViewModel.FavList.Count == 0)
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
                            UpdateUserImageInStatus(c.Msisdn);
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
                    if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                        c = App.ViewModel.ContactsCache[msisdn];
                    else
                        c = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                }

                // ignore if not onhike or not in addressbook
                if (c == null || !c.OnHike || string.IsNullOrEmpty(c.Name)) // c==null for unknown contacts
                    return;

                Dispatcher.BeginInvoke(() =>
                {
                    if (c.Msisdn != App.MSISDN)
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
                    App.ViewModel.MessageListPageCollection.Remove(co);

                    if (App.ViewModel.MessageListPageCollection.Count == 0 || (!App.ViewModel.IsHiddenModeActive && App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0))
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
                    if (cinfo.OnHike && !App.ViewModel.Isfavourite(cinfo.Msisdn))
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
                                if (cinfo.OnHike && !App.ViewModel.Isfavourite(cinfo.Msisdn) && hikeContactList.Where(c => c.Msisdn == cinfo.Msisdn).Count() == 0)
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
                               App.ViewModel.ContactsCache.Remove(cinfo.Msisdn);
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
                appSettings.TryGetValue(HikeConstants.SHOW_REWARDS, out showRewards);
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

            if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS))
            {
                _isModeChanged = true;
                UpdateToolTip();
            }
        }

        bool _isModeChanged;

        void UpdateToolTip()
        {
            conversationPageToolTip.ResetClickEvents();

            switch (_tipMode)
            {
                case ToolTipMode.HIDDEN_MODE_GETSTARTED:

                    if (_isModeChanged)
                    {
                        conversationPageToolTip.TipText = AppResources.HiddenMode_GetStarted_Txt;
                        conversationPageToolTip.LeftIconSource = UI_Utils.Instance.ToolTipArrow;
                        conversationPageToolTip.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
                        conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
                        conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;
                    }

                    if (!conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = true;

                    break;

                case ToolTipMode.HIDDEN_MODE_STEP2:

                    if (!App.ViewModel.IsHiddenModeActive)
                        return;

                    if (_isModeChanged)
                    {
                        conversationPageToolTip.TipText = AppResources.HiddenMode_Step2_Txt;
                        conversationPageToolTip.LeftIconSource = UI_Utils.Instance.SheildIcon;
                        conversationPageToolTip.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
                        conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
                        conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;
                    }

                    if (!conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = true;

                    break;

                case ToolTipMode.HIDDEN_MODE_COMPLETE:

                    if (_isModeChanged)
                    {
                        conversationPageToolTip.TipText = AppResources.HiddenMode_Completed_Txt;
                        conversationPageToolTip.LeftIconSource = UI_Utils.Instance.ToolTipArrow;
                        conversationPageToolTip.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
                        conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
                        conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;
                    }

                    if (!conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = true;

                    break;

                case ToolTipMode.RESET_HIDDEN_MODE:

                    conversationPageToolTip.TipText = String.Format(AppResources.ResetTip_Txt, Utils.GetFormattedTimeFromSeconds(_resetTimeSeconds));

                    if (_isModeChanged)
                    {
                        conversationPageToolTip.LeftIconSource = UI_Utils.Instance.SheildIcon;
                        conversationPageToolTip.RightIconSource = UI_Utils.Instance.ToolTipCrossIcon;
                        conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
                        conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;
                    }

                    if (!conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = true;

                    break;

                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:

                    if (_isModeChanged)
                    {
                        conversationPageToolTip.TipText = AppResources.HiddenModeReset_Completed_Txt;
                        conversationPageToolTip.LeftIconSource = UI_Utils.Instance.ToolTipCrossIcon;
                        conversationPageToolTip.RightIconSource = UI_Utils.Instance.ToolTipTickIcon;
                        conversationPageToolTip.LeftIconClicked -= conversationPageToolTip_LeftIconClicked;
                        conversationPageToolTip.LeftIconClicked += conversationPageToolTip_LeftIconClicked;
                        conversationPageToolTip.RightIconClicked -= conversationPageToolTip_RightIconClicked;
                        conversationPageToolTip.RightIconClicked += conversationPageToolTip_RightIconClicked;
                    }

                    if (!conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = true;

                    break;
            }

            App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS, _tipMode);
        }

        void conversationPageToolTip_LeftIconClicked(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.RESET_HIDDEN_MODE_COMPLETED:
                    conversationPageToolTip.IsShow = false;
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS);
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_MODE_RESET_TIME);
                    _tipMode = ToolTipMode.DEFAULT;

                    if (_resetTimer != null)
                    {
                        _resetTimer.Stop();
                        _resetTimer = null;
                    }
                    break;
            }
        }

        private void UpdateFriendsCounter()
        {
            if (App.ViewModel.FavList.Count == 0)
                txtCircleOfFriends.Text = AppResources.Conversations_Circle_Of_friends_txt;
            else if (App.ViewModel.FavList.Count == 1)
                txtCircleOfFriends.Text = AppResources.Conversations_1Circle_Of_friends_txt;
            else
                txtCircleOfFriends.Text = string.Format(AppResources.Conversations_NCircle_Of_friends_txt, App.ViewModel.FavList.Count);
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

        private async void UpdateUserImageInStatus(string msisdn)
        {
            await Task.Delay(1);

            foreach (var status in App.ViewModel.StatusList)
            {
                if (status.Msisdn == msisdn)
                    status.UpdateImage();
            }
        }

        #endregion

        #region App Update Available


        void ShowAppUpdateAvailableMessage()
        {
            String updateObj;
            if (App.appSettings.TryGetValue(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE, out updateObj))
            {
                JObject obj = JObject.Parse(updateObj);

                var currentVersion = App.appSettings[HikeConstants.FILE_SYSTEM_VERSION].ToString();
                var version = (string)obj[HikeConstants.VERSION];
                if (Utils.compareVersion(version, currentVersion) <= 0)
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE);
                    return;
                }

                var message = (string)obj[HikeConstants.TEXT_UPDATE_MSG];
                bool isCriticalUpdate = (bool)obj[HikeConstants.CRITICAL];

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
                                 App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE);
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
                deleteConversation(convObj);
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
                App.ViewModel.FavList.Remove(convObj);
                UpdateFriendsCounter();
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
                    emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                    favourites.Visibility = System.Windows.Visibility.Collapsed;
                    //addFavsPanel.Opacity = 0;
                }

                FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);

                // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                if (convObj.IsOnhike && !string.IsNullOrEmpty(convObj.ContactName))
                {
                    ContactInfo c = null;
                    if (App.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                        c = App.ViewModel.ContactsCache[convObj.Msisdn];
                    else
                    {
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                        c.Avatar = convObj.Avatar;
                    }

                    if (c.Msisdn != App.MSISDN && isContactListLoaded)
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
                if (App.ViewModel.ContactsCache.ContainsKey(convObj.Msisdn))
                    c = App.ViewModel.ContactsCache[convObj.Msisdn];
                else
                    c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);

                hikeContactList.Remove(c);
                UpdateContactsOnHikeCounter();
                FriendsTableUtils.FriendStatusEnum fs = FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                App.ViewModel.FavList.Insert(0, convObj);
                UpdateFriendsCounter();
                if (App.ViewModel.IsPending(convObj.Msisdn))
                {
                    App.ViewModel.PendingRequests.Remove(convObj.Msisdn);
                    MiscDBUtil.SavePendingRequests();
                    App.ViewModel.RemoveFrndReqFromTimeline(convObj.Msisdn, fs);
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

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            BaseStatusUpdate selectedItem = (sender as MenuItem).DataContext as BaseStatusUpdate;

            if (selectedItem == null)
                return;

            Clipboard.SetText(selectedItem.Text);
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
                passwordOverlay.IsShow = false;
                e.Cancel = true;
                return;
            }

            if (App.IS_VIEWMODEL_LOADED)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && App.ViewModel.ConvMap.Count == 0)
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
                if (obj == null || (!App.ViewModel.IsHiddenModeActive && App.ViewModel.ConvMap.ContainsKey(obj.Msisdn) && App.ViewModel.ConvMap[obj.Msisdn].IsHidden))
                    return;
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = obj;
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
                App.ViewModel.FavList.Remove(convObj);
                UpdateFriendsCounter();
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
                    {
                        c = new ContactInfo(convObj.Msisdn, convObj.NameToShow, convObj.IsOnhike);
                        c.Avatar = convObj.Avatar;
                    }

                    if (c.Msisdn != App.MSISDN)
                    {
                        hikeContactList.Add(c);
                        UpdateContactsOnHikeCounter();
                    }
                }
            }
            if (App.ViewModel.FavList.Count == 0)
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

            if (!App.ViewModel.IsHiddenModeActive && App.ViewModel.ConvMap.ContainsKey(c.Msisdn) && App.ViewModel.ConvMap[c.Msisdn].IsHidden)
                return;

            StartNewChatWithSelectContact(c);
        }

        private void StartNewChatWithSelectContact(ContactInfo c)
        {
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

                if (App.ViewModel.Isfavourite(contactInfo.Msisdn))
                {
                    hikeContactList.Remove(contactInfo);
                    UpdateContactsOnHikeCounter();
                    return;
                }

                JObject data = new JObject();
                data["id"] = contactInfo.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.DATA] = data;
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
                ConversationListObject cObj = null;
                if (App.ViewModel.ConvMap.ContainsKey(contactInfo.Msisdn))
                {
                    cObj = App.ViewModel.ConvMap[contactInfo.Msisdn];
                    cObj.IsFav = true;
                }
                else
                {
                    var bytes = contactInfo.Avatar == null ? UI_Utils.Instance.ConvertToBytes(contactInfo.AvatarImage) : contactInfo.Avatar;
                    cObj = new ConversationListObject(contactInfo.Msisdn, contactInfo.Name, contactInfo.OnHike, bytes);
                }

                hikeContactList.Remove(contactInfo);
                UpdateContactsOnHikeCounter();
                App.ViewModel.FavList.Add(cObj);
                UpdateFriendsCounter();
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(cObj);
                int count = 0;
                App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

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

                if (App.ViewModel.IsPending(contactInfo.Msisdn))
                {
                    App.ViewModel.PendingRequests.Remove(contactInfo.Msisdn);
                    MiscDBUtil.SavePendingRequests();
                    App.ViewModel.RemoveFrndReqFromTimeline(contactInfo.Msisdn, fs);
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
                if (App.IS_TOMBSTONED)
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
                    App.WriteToIsoStorageSettings(App.PRO_TIP_COUNT, value);
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
                    if (value == 0 && (App.ViewModel.StatusList.Count >= App.ViewModel.PendingRequests.Count + _totalUnreadStatuses))
                    {
                        for (int i = App.ViewModel.PendingRequests.Count;
                            i < App.ViewModel.PendingRequests.Count + _totalUnreadStatuses; i++)
                        {
                            App.ViewModel.StatusList[i].IsUnread = false;
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
            if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatus && FreshStatusUpdates != null && FreshStatusUpdates.Count > 0)
                App.ViewModel.StatusList.RemoveAt(index);

            // this fix will solve the possible crash , suggested by nitesh
            int pendingCount = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count + index : index;
            for (int i = 0; i < (FreshStatusUpdates != null ? FreshStatusUpdates.Count : 0); i++)
            {
                var status = StatusUpdateHelper.Instance.CreateStatusUpdate(FreshStatusUpdates[i], true);
                if (status != null)
                {
                    App.ViewModel.StatusList.Insert(pendingCount, status);
                }
            }

            //scroll to the recent item(the most recent status update on tapping this bar)
            if (App.ViewModel.StatusList.Count > pendingCount)
                statusLLS.ScrollTo(App.ViewModel.StatusList[pendingCount]);

            RefreshBarCount = 0;
        }

        private void postStatusBtn_Click(object sender, EventArgs e)
        {
            //if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            //{
            //    RemoveStatusUpdateTutorial();
            //}

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
                App.ViewModel.StatusList.Remove(fObj);
                FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
                App.ViewModel.PendingRequests.Remove(fObj.Msisdn);
                MiscDBUtil.SavePendingRequests();
                if (App.ViewModel.Isfavourite(fObj.Msisdn)) // if already favourite just ignore
                    return;

                ConversationListObject cObj = null;
                ContactInfo cn = null;
                if (App.ViewModel.ConvMap.ContainsKey(fObj.Msisdn))
                {
                    cObj = App.ViewModel.ConvMap[fObj.Msisdn];
                    cObj.IsFav = true;
                }
                else
                {
                    if (App.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                        cn = App.ViewModel.ContactsCache[fObj.Msisdn];
                    else
                    {
                        cn = UsersTableUtils.getContactInfoFromMSISDN(fObj.Msisdn);
                        if (cn != null)
                            App.ViewModel.ContactsCache[fObj.Msisdn] = cn;
                    }
                    bool onHike = cn != null ? cn.OnHike : true; // by default only hiek user can send you friend request
                    cObj = new ConversationListObject(fObj.Msisdn, fObj.UserName, onHike, MiscDBUtil.getThumbNailForMsisdn(fObj.Msisdn));
                }

                if (cn == null && App.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                    cn = App.ViewModel.ContactsCache[fObj.Msisdn];

                if (cn != null)
                {
                    hikeContactList.Remove(cn);
                    UpdateContactsOnHikeCounter();
                }
                App.ViewModel.FavList.Insert(0, cObj);
                UpdateFriendsCounter(); JObject data = new JObject();
                data["id"] = fObj.Msisdn;
                JObject obj = new JObject();
                obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                obj[HikeConstants.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(cObj);
                int count = 0;
                App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
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
                obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
                obj[HikeConstants.DATA] = data;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                App.ViewModel.StatusList.Remove(fObj);
                App.ViewModel.PendingRequests.Remove(fObj.Msisdn);

                if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                    App.ViewModel.StatusList.Add(DefaultStatus);

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

                    int pendingCount = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count : 0;
                    //if no new status scroll to latest unseen friends request
                    if (UnreadFriendRequests > 0 && (pendingCount > UnreadFriendRequests))
                    {
                        int x = pendingCount - UnreadFriendRequests;
                        if (x >= 0 && App.ViewModel.StatusList.Count > (x + index))
                            statusLLS.ScrollTo(App.ViewModel.StatusList[x + index]); //handling index out of bounds exception
                    }
                    //scroll to latest unread status
                    else if ((App.ViewModel.StatusList.Count > (pendingCount + index)) && RefreshBarCount > 0) //handling index out of bounds exception
                    {
                        statusLLS.ScrollTo(App.ViewModel.StatusList[pendingCount + index]);
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
                PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY] = statusImageInfo;
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
            PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_TIMELINE] = obj;
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

            if (!App.ViewModel.IsHiddenModeActive && App.ViewModel.ConvMap.ContainsKey(status.Msisdn) && App.ViewModel.ConvMap[status.Msisdn].IsHidden)
                return;

            if (status.Msisdn == App.MSISDN)
            {
                Object[] obj = new Object[2];
                obj[0] = status.Msisdn;
                obj[1] = status.UserName;
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_TIMELINE] = obj;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                return;
            }

            if (App.ViewModel.ConvMap.ContainsKey(status.Msisdn))
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = App.ViewModel.ConvMap[status.Msisdn];
            else
            {
                ConversationListObject cFav = App.ViewModel.GetFav(status.Msisdn);
                if (cFav != null)
                {
                    if (!_isFavListBound)
                        cFav.Avatar = MiscDBUtil.getThumbNailForMsisdn(cFav.Msisdn);

                    PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = cFav;
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
                    PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = contactInfo;
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

            for (i = 0; i < App.ViewModel.StatusList.Count; i++)
            {
                if (App.ViewModel.StatusList[i] is ProTipStatusUpdate)
                {
                    isPresent = true;
                    break;
                }
            }

            if (!isPresent)
                return;

            App.ViewModel.StatusList.RemoveAt(i);

            App.WriteToIsoStorageSettings(App.PRO_TIP_LAST_DISMISS_TIME, DateTime.Now);

            ProTipCount = 0;

            Analytics.SendAnalyticsEvent(HikeConstants.ST_UI_EVENT, HikeConstants.PRO_TIPS_DISMISSED, ProTipHelper.CurrentProTip._id);

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (ss, ee) =>
                {
                    if (App.appSettings.Contains(App.PRO_TIP))
                    {
                        ProTipHelper.Instance.RemoveCurrentProTip();
                        App.appSettings.Remove(App.PRO_TIP);
                        App.appSettings.Save();
                    }
                };
            worker.RunWorkerAsync();
        }

        void showProTip()
        {
            if (ProTipHelper.CurrentProTip != null)
            {
                if (App.ViewModel.StatusList != null && App.ViewModel.StatusList.Count > 0 && App.ViewModel.StatusList[0] is ProTipStatusUpdate)
                    App.ViewModel.StatusList.RemoveAt(0);

                var proTipStatus = new ProTipStatusUpdate();
                App.ViewModel.StatusList.Insert(0, proTipStatus);

                ProTipCount = 1;
            }
        }

        private void ProTipImage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.IMAGE_TO_DISPLAY] = true;
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

            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = convListObj;

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

            if (App.appSettings.TryGetValue(HikeConstants.SHOW_POPUP, out obj))
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
                App.RemoveKeyFromAppSettings(HikeConstants.SHOW_POPUP);
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
                    PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] = obj.Msisdn;
                    PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = obj.NameToShow;
                    NavigationService.Navigate(new Uri("/View/GroupInfoPage.xaml", UriKind.Relative));
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_CONVERSATION_PAGE] = obj;
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
            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
            NavigationService.Navigate(new Uri("/View/NewGroup.xaml", UriKind.Relative));
        }

        private void GoToProfile_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.FTUE_CARD_PROFILE_PIC_CLICKED);
            PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_PROFILE] = null;
            PhoneApplicationService.Current.State[HikeConstants.SET_PROFILE_PIC] = true;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        #endregion

        #region Typing Notification

        void ShowTypingNotification(object sender, object[] vals)
        {
            string typerMsisdn = (string)vals[0];
            string searchBy = vals[1] != null ? (string)vals[1] : typerMsisdn;

            var list = App.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

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

            var list = App.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

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

            var list = App.ViewModel.MessageListPageCollection.Where(f => f.Msisdn == searchBy);

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

            App.ViewModel.Hyperlink_Clicked(sender as object[]);
        }

        void ViewMoreMessage_Clicked(object sender, EventArgs e)
        {
            _hyperlinkedInsideStatusUpdateClicked = true;

            App.ViewModel.ViewMoreMessage_Clicked(sender);
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
        //    if (App.ViewModel.MessageListPageCollection.Where(c => c.IsSelected == true).Count() > 0)
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

        #region Hidden Mode

        string _password;

        /// <summary>
        /// Function called when hike logo tapped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hikeLogo_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.ViewModel.IsHiddenModeActive)
            {
                passwordOverlay.Text = App.appSettings.Contains(HikeConstants.HIDDEN_MODE_PASSWORD) ?
                    AppResources.EnterPassword_Txt : AppResources.EnterNewPassword_Txt;

                passwordOverlay.IsShow = true;
            }
            else
            {
                InitHidddenMode();

                if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_COMPLETE)
                {
                    App.RemoveKeyFromAppSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS);

                    if (conversationPageToolTip.IsShow)
                        conversationPageToolTip.IsShow = false;
                }

                if (App.ViewModel.MessageListPageCollection.Count == 0 || App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == false).Count() == 0)
                    ShowFTUECards();
            }
        }

        /// <summary>
        /// Initialize hidden mode.
        /// </summary>
        private void InitHidddenMode()
        {
            if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                conversationPageToolTip.IsShow = false;

            if (App.ViewModel.MessageListPageCollection.Where(m => m.IsHidden == true).Count() > 0)
                ShowChats();

            App.ViewModel.SetHiddenMode();

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateLayout();
                });

            //send qos 0 for toggling for stealth mode on server
            JObject hideObj = new JObject();
            hideObj.Add(HikeConstants.TYPE, HikeConstants.HIDDEN_MODE_TYPE);

            JObject data = new JObject();

            if (App.appSettings.Contains(HikeConstants.HIDDEN_MODE))
            {
                headerIcon.Source = UI_Utils.Instance.HiddenModeHeaderIcon;
                data.Add(HikeConstants.HIDDEN_MODE_ENABLED, true);
            }
            else
            {
                headerIcon.Source = UI_Utils.Instance.HeaderIcon;
                data.Add(HikeConstants.HIDDEN_MODE_ENABLED, false);
            }

            hideObj.Add(HikeConstants.DATA, data);

            Object[] objArr = new object[2];
            objArr[0] = hideObj;
            objArr[1] = 0;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, objArr);
        }

        /// <summary>
        /// Mark individual chat as hidden/unhidden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Click_HideChat(object sender, RoutedEventArgs e)
        {
            var obj = (sender as MenuItem).DataContext as ConversationListObject;
            if (obj != null)
            {
                obj.IsHidden = !obj.IsHidden;

                if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                {
                    _tipMode = ToolTipMode.HIDDEN_MODE_COMPLETE;
                    _isModeChanged = true;
                    UpdateToolTip();
                }

                JObject hideObj = new JObject();
                hideObj.Add(HikeConstants.TYPE, HikeConstants.STEALTH);

                JObject data = new JObject();
                JArray msisdn = new JArray();
                msisdn.Add(obj.Msisdn);

                if (obj.IsHidden)
                    data.Add(HikeConstants.CHAT_ENABLED, msisdn);
                else
                    data.Add(HikeConstants.CHAT_DISABLED, msisdn);

                hideObj.Add(HikeConstants.DATA, data);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, hideObj);
            }
        }

        bool _confirmPassword;
        string _tempPassword;

        /// <summary>
        /// password has been entered by the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void passwordOverlay_PasswordEntered(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                if (String.IsNullOrWhiteSpace(_password))
                {
                    if (_confirmPassword)
                    {
                        if (_tempPassword.Equals(popup.Password))
                        {
                            _password = popup.Password;
                            App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_MODE_PASSWORD, _password);

                            InitHidddenMode();

                            if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS))
                            {
                                _tipMode = ToolTipMode.HIDDEN_MODE_STEP2;
                                _isModeChanged = true;
                                UpdateToolTip();
                            } 
                        }

                        _confirmPassword = false;
                        popup.IsShow = false;
                    }
                    else
                    {
                        _confirmPassword = true;
                        _tempPassword = popup.Password;
                        popup.Text = AppResources.ConfirmPassword_Txt;
                        popup.Password = String.Empty;
                    }
                }
                else if (_password == popup.Password)
                {
                    InitHidddenMode();
                    popup.IsShow = false;

                    if (App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS) && _tipMode == ToolTipMode.HIDDEN_MODE_STEP2)
                        UpdateToolTip();
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
        void popup_PasswordOverlayVisibilityChanged(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                ApplicationBar.IsVisible = popup.IsShow ? false : true;
            }
        }

        #endregion
    }

}