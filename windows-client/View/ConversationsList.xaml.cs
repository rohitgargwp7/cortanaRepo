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
using windows_client.Controls.StatusUpdate;
using Coding4Fun.Phone.Controls;
using System.Windows.Media;

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
        //ApplicationBarIconButton addFriendIconButton;
        private bool isStatusUpdatesMute;
        private bool isStatusMessagesLoaded = false;

        public bool ConversationListUpdated
        {
            get;
            set;
        }
        
        private ObservableCollection<ContactInfo> hikeContactList = new ObservableCollection<ContactInfo>(); //all hike contacts - hike friends
        #endregion
        #region Page Based Functions

        public ConversationsList()
        {
            InitializeComponent();
            initAppBar();
            initProfilePage();
            App.ViewModel.ConversationListPage = this;
            convListPagePivot.ApplicationBar = appBar;
            _totalUnreadStatuses = StatusMsgsTable.GetUnreadCount(HikeConstants.UNREAD_UPDATES);
            _refreshBarCount = StatusMsgsTable.GetUnreadCount(HikeConstants.REFRESH_BAR);
            _unreadFriendRequests = StatusMsgsTable.GetUnreadCount(HikeConstants.UNREAD_FRIEND_REQUESTS);
            setNotificationCounter(RefreshBarCount + UnreadFriendRequests + ProTipCount);
            App.RemoveKeyFromAppSettings(HikeConstants.PHONE_ADDRESS_BOOK);

            if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
            {
                this.Loaded += ConversationsList_Loaded;
            }

            ProTipHelper.Instance.ShowProTip -= Instance_ShowProTip;
            ProTipHelper.Instance.ShowProTip += Instance_ShowProTip;

            if (ProTipHelper.CurrentProTip != null)
                showProTip();

            int tipCount;
            App.appSettings.TryGetValue(App.PRO_TIP_COUNT, out tipCount);
            ProTipCount = tipCount;
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
            launchPagePivot.SelectedIndex = 3;
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
                TotalUnreadStatuses = 0;
            }
            this.llsConversations.SelectedItem = null;
            this.favourites.SelectedIndex = -1;
            this.hikeContactListBox.SelectedIndex = -1;
            this.statusLLS.SelectedIndex = -1;

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
                firstLoad = false;

                if (appSettings.Contains(App.SHOW_BASIC_TUTORIAL))
                {
                    overlay.Visibility = Visibility.Visible;
                    overlay.Tap += DismissTutorial_Tap;
                    gridBasicTutorial.Visibility = Visibility.Visible;
                    launchPagePivot.IsHitTestVisible = false;
                }
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
                if (ConversationListUpdated)
                {
                    llsConversations.ScrollTo(App.ViewModel.MessageListPageCollection[0]);
                    ConversationListUpdated = false;
                }
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
            byte statusSettingsValue;
            isStatusUpdatesMute = App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue) && statusSettingsValue == 0;
            imgToggleStatus.Source = isStatusUpdatesMute ? UI_Utils.Instance.MuteIcon : UI_Utils.Instance.UnmuteIcon;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
            if (launchPagePivot.SelectedIndex == 3) //if user quits app from timeline when a few statuses were shown as unread
                TotalUnreadStatuses = RefreshBarCount;  //and new statuses arrived in refresh bar
        }

        #region STATUS UPDATE TUTORIAL
        private void DismissStatusUpdateTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RemoveStatusUpdateTutorial();
        }

        private void RemoveStatusUpdateTutorial()
        {
            overlay.Tap -= DismissStatusUpdateTutorial_Tap;
            overlay.Visibility = Visibility.Collapsed;
            TutorialStatusUpdate.Visibility = Visibility.Collapsed;
            launchPagePivot.IsHitTestVisible = true;
            App.RemoveKeyFromAppSettings(App.SHOW_STATUS_UPDATES_TUTORIAL);
        }
        #endregion

        #region BASIC TUTORIAL
        private void DismissTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            RemoveTutorial();
        }

        private void RemoveTutorial()
        {
            overlay.Tap -= DismissTutorial_Tap;
            overlay.Visibility = Visibility.Collapsed;
            gridBasicTutorial.Visibility = Visibility.Collapsed;
            launchPagePivot.IsHitTestVisible = true;
            App.RemoveKeyFromAppSettings(App.SHOW_BASIC_TUTORIAL);
        }
        #endregion

        private void CircleOfFriends_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            launchPagePivot.SelectedIndex = 1;
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
            llsConversations.ItemsSource = App.ViewModel.MessageListPageCollection;

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
            if (!PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
            {
                NetworkManager.turnOffNetworkManager = false;
                Utils.RequestServerEpochTime();
            }
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
                         if (App.ViewModel.MessageListPageCollection.Contains(convObj))
                         {
                             App.ViewModel.MessageListPageCollection.Remove(convObj);
                         }
                         App.ViewModel.MessageListPageCollection.Insert(0, convObj);
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
            //appBar.Mode = ApplicationBarMode.Minimized;
            //appBar.Opacity = 0;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            /* Add icons */
            groupChatIconButton = new ApplicationBarIconButton();
            groupChatIconButton.IconUri = new Uri("/View/images/icon_group.png", UriKind.Relative);
            groupChatIconButton.Text = AppResources.GrpChat_Txt;
            groupChatIconButton.Click += createGroup_Click;
            groupChatIconButton.IsEnabled = true;
            appBar.Buttons.Add(groupChatIconButton);

            composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/icon_message.png", UriKind.Relative);
            composeIconButton.Text = AppResources.Conversations_NewChat_AppBar_Btn;
            composeIconButton.Click += selectUserBtn_Click;
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            postStatusIconButton = new ApplicationBarIconButton();
            postStatusIconButton.IconUri = new Uri("/View/images/icon_status.png", UriKind.Relative);
            postStatusIconButton.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusIconButton.Click += new EventHandler(postStatusBtn_Click);
            postStatusIconButton.IsEnabled = true;
            appBar.Buttons.Add(postStatusIconButton);

            //addFriendIconButton = new ApplicationBarIconButton();
            //addFriendIconButton.IconUri = new Uri("/View/images/appbar_addfriend.png", UriKind.Relative);
            //addFriendIconButton.Text = AppResources.Favorites_AddMore;
            //addFriendIconButton.Click += addFriend_Click;
            //addFriendIconButton.IsEnabled = true;

            delConvsMenu = new ApplicationBarMenuItem();
            delConvsMenu.Text = AppResources.Conversations_DelAllChats_Txt;
            delConvsMenu.Click += new EventHandler(deleteAllConvs_Click);
            appBar.MenuItems.Add(delConvsMenu);

            //toggleStatusUpdatesMenu = new ApplicationBarMenuItem();
            //byte statusSettingsValue;
            //App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue);
            //toggleStatusUpdatesMenu.Text = statusSettingsValue > 0 ? AppResources.Conversations_MuteStatusNotification_txt : AppResources.Conversations_UnmuteStatusNotification_txt;
            //appBar.MenuItems.Add(toggleStatusUpdatesMenu);
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
            ConversationListObject convListObj = llsConversations.SelectedItem as ConversationListObject;
            if (convListObj == null)
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
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.DELETE_STATUS_AND_CONV, this);
            mPubSub.addListener(HikePubSub.PRO_TIPS_REC, this);
        }

        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
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
                mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
                mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
                mPubSub.removeListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
                mPubSub.removeListener(HikePubSub.DELETE_STATUS_AND_CONV, this);
                mPubSub.removeListener(HikePubSub.PRO_TIPS_REC, this);
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
                settingsImage.Source = new BitmapImage(new Uri("images/settings_icon_white.png", UriKind.Relative));
                helpImage.Source = new BitmapImage(new Uri("images/help_icon_white.png", UriKind.Relative));
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_black.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_black.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite_dark.png", UriKind.Relative));
                rewards.Source = new BitmapImage(new Uri("images/new_icon_white.png", UriKind.Relative));
                //favsBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x36, 0x36, 0x36));
            }
            else
            {
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_white.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_white.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite.png", UriKind.Relative));
                rewards.Source = new BitmapImage(new Uri("images/new_icon.png", UriKind.Relative));
                helpImage.Source = new BitmapImage(new Uri("images/help_icon_dark.png", UriKind.Relative));
                settingsImage.Source = new BitmapImage(new Uri("images/settings_icon_dark.png", UriKind.Relative));
                //favsBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xe9, 0xe9, 0xe9));
            }
            bool showRewards;
            if (App.appSettings.TryGetValue<bool>(HikeConstants.SHOW_REWARDS, out showRewards) && showRewards == true)
                rewardsPanel.Visibility = Visibility.Visible;

            txtStatus.Foreground = creditsTxtBlck.Foreground = UI_Utils.Instance.EditProfileForeground;
            int moodId;
            string lastStatus = StatusMsgsTable.GetLastStatusMessage(out moodId);
            if (!string.IsNullOrEmpty(lastStatus))
            {
                txtStatus.Text = lastStatus;
                if (moodId > 0)
                {
                    statusImage.Height = 30;
                    statusImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(moodId);
                }
                else
                {
                    statusImage.Height = 25;
                    statusImage.Source = UI_Utils.Instance.TextStatusImage;
                }
            }
            else
            {
                statusImage.Source = UI_Utils.Instance.TextStatusImage;
                txtStatus.Text = AppResources.Conversations_DefaultStatus_Txt;
                //todo:change default status
            }
            int rew_val = 0;

            string name;
            appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name != null)
                accountName.Text = name;
            int smsCount = 0;
            App.appSettings.TryGetValue<int>(App.SMS_SETTING, out smsCount);
            creditsTxtBlck.Text = string.Format(AppResources.SMS_Left_Txt, smsCount);

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
            if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            {
                RemoveStatusUpdateTutorial();
                return;
            }
            else if (gridBasicTutorial.Visibility == Visibility.Visible)
            {
                RemoveTutorial();
            }
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

        private void ToggleStatusUpdateNotification(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBox.Show(isStatusUpdatesMute ? AppResources.Unmute_Success_Txt : AppResources.Mute_Success_Txt, AppResources.StatusNotToggle_Caption_Txt, MessageBoxButton.OK);
            int settingsValue = 0;
            if (isStatusUpdatesMute)
            {
                imgToggleStatus.Source = UI_Utils.Instance.UnmuteIcon;
                App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)1);
                settingsValue = 0;
            }
            else
            {
                imgToggleStatus.Source = UI_Utils.Instance.MuteIcon;
                App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)0);
                settingsValue = -1;
            }
            isStatusUpdatesMute = !isStatusUpdatesMute;

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_SU, settingsValue);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            {
                RemoveStatusUpdateTutorial();
                return;
            }
            else if (gridBasicTutorial.Visibility == Visibility.Visible)
            {
                RemoveTutorial();
            }
            App.AnalyticsInstance.addEvent(Analytics.COMPOSE);
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void deleteConversation(ConversationListObject convObj)
        {
            App.ViewModel.ConvMap.Remove(convObj.Msisdn); // removed entry from map for UI
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

            if (selectedIndex != 3 && RefreshBarCount > 0)
                UpdatePendingStatusFromRefreshBar();

            if (selectedIndex == 0)
            {
                if (!appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Insert(0, delConvsMenu);

                gridToggleStatus.Visibility = Visibility.Collapsed;
            }
            else if (selectedIndex == 1) // favourite
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                // there will be two background workers that will independently load three sections
                #region FAVOURITES

                if (!_isFavListBound)
                {
                    _isFavListBound = true;
                    BackgroundWorker favBw = new BackgroundWorker();
                    shellProgress.IsVisible = true;
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
                                cinfoTemp.IsUsedAtMiscPlaces = true;

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
                        shellProgress.IsVisible = false;
                        contactsCollectionView.Source = hikeContactList;
                        favCollectionView.Source = App.ViewModel.FavList; // this is done to sort in view
                        favourites.SelectedIndex = -1;
                        hikeContactListBox.SelectedIndex = -1;
                        circleOfFriendsTitleTxtBlck.Visibility = System.Windows.Visibility.Visible;
                        contactOnHikeTitleTxtBlck.Visibility = System.Windows.Visibility.Visible;
                        if (App.ViewModel.FavList.Count > 0)
                        {
                            emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                            favourites.Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                            if (hikeContactList.Count > 0)
                                addContactsTxtBlk.Text = AppResources.Conversations_NoFriend_Tap_Txt;
                            else
                                addContactsTxtBlk.Text = AppResources.Conversations_TapYesToAdd_Txt;
                        }

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
                #endregion
            }
            else if (selectedIndex == 2)
            {
                gridToggleStatus.Visibility = Visibility.Collapsed;
            }
            else if (selectedIndex == 3)
            {
                ProTipCount = 0;

                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                gridToggleStatus.Visibility = Visibility.Visible;
                if (!isStatusMessagesLoaded)
                {
                    List<StatusMessage> statusMessagesFromDB = null;
                    BackgroundWorker statusBw = new BackgroundWorker();
                    statusBw.DoWork += (sf, ef) =>
                    {
                        App.ViewModel.LoadPendingRequests();
                        //corresponding counters should be handled for eg unread count
                        statusMessagesFromDB = StatusMsgsTable.GetAllStatusMsgsForTimeline();
                    };
                    statusBw.RunWorkerAsync();
                    shellProgress.IsVisible = true;

                    statusBw.RunWorkerCompleted += (ss, ee) =>
                    {
                        shellProgress.IsVisible = false;
                        
                        foreach (ConversationListObject co in App.ViewModel.PendingRequests.Values)
                        {
                            FriendRequestStatus frs = new FriendRequestStatus(co, yes_Click, no_Click, statusBubblePhoto_Tap);
                            App.ViewModel.StatusList.Add(frs);
                        }

                        if (statusMessagesFromDB != null)
                        {
                            for (int i = 0; i < statusMessagesFromDB.Count; i++)
                            {
                                // if this user is blocked dont show his/her statuses
                                if (App.ViewModel.BlockedHashset.Contains(statusMessagesFromDB[i].Msisdn))
                                    continue;
                                
                                if (i < TotalUnreadStatuses)
                                    statusMessagesFromDB[i].IsUnread = true;
                                
                                StatusUpdateBox statusUpdate = StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], true,
                                    statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap);
                                
                                if (statusUpdate != null)
                                    App.ViewModel.StatusList.Add(statusUpdate);
                            }
                        }
                        
                        this.statusLLS.ItemsSource = App.ViewModel.StatusList;
                        
                        if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                        {
                            string firstName = Utils.GetFirstName(accountName.Text);
                            App.ViewModel.StatusList.Add(new DefaultStatusUpdateUC(string.Format(AppResources.Conversations_EmptyStatus_Hey_Txt, firstName), CircleOfFriends_Tap, UpdateStatus_Tap));
                        }

                        RefreshBarCount = 0;
                        UnreadFriendRequests = 0;
                        
                        if (PhoneApplicationService.Current.State.ContainsKey("IsStatusPush"))
                        {
                            NetworkManager.turnOffNetworkManager = false;
                            Utils.RequestServerEpochTime();
                            PhoneApplicationService.Current.State.Remove("IsStatusPush");
                        }
                        
                        isStatusMessagesLoaded = true;
                    };
                    if (appSettings.Contains(App.SHOW_STATUS_UPDATES_TUTORIAL))
                    {
                        overlay.Visibility = Visibility.Visible;
                        overlay.Tap += DismissStatusUpdateTutorial_Tap;
                        TutorialStatusUpdate.Visibility = Visibility.Visible;
                        launchPagePivot.IsHitTestVisible = false;
                    }
                }
                else
                {
                    RefreshBarCount = 0;
                    UnreadFriendRequests = 0;
                }
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
                                llsConversations.ScrollTo(App.ViewModel.MessageListPageCollection[0]);
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
                    if (App.newChatThreadPage == null && (!Utils.isGroupConversation(mObj.Msisdn) || !mObj.IsMute))
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
                    if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
                    {
                        emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                        favourites.Visibility = System.Windows.Visibility.Visible;
                        //addFavsPanel.Opacity = 1;
                    }
                    else if (App.ViewModel.FavList.Count == 0) // remove fav
                    {
                        emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                        favourites.Visibility = System.Windows.Visibility.Collapsed;
                        //addFavsPanel.Opacity = 0;
                    }
                });
            }
            #endregion
            #region ADD TO PENDING
            else if (HikePubSub.ADD_TO_PENDING == type)
            {

                if (!App.ViewModel.IsPendingListLoaded)
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

                            if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatusUpdateUC)
                                App.ViewModel.StatusList.RemoveAt(index);

                            FriendRequestStatus frs = new FriendRequestStatus(co, yes_Click, no_Click, statusBubblePhoto_Tap);
                            App.ViewModel.StatusList.Insert(index, frs);

                        }
                        if (launchPagePivot.SelectedIndex != 3)
                        {
                            UnreadFriendRequests++;
                        }

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
                //int rew_val = (int)obj;
                //if (rew_val <= 0) // hide value
                //{
                //    Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    {
                //        if (rewardsTxtBlk.Visibility == System.Windows.Visibility.Visible)
                //            rewardsTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
                //    });
                //}
                //else
                //{
                //    Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    {
                //        if (rewardsTxtBlk.Visibility == System.Windows.Visibility.Collapsed)
                //            rewardsTxtBlk.Visibility = System.Windows.Visibility.Visible;
                //        rewardsTxtBlk.Text = string.Format(AppResources.Rewards_Txt + " ({0})", Convert.ToString(rew_val));
                //    });
                //}
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
                    if (sm.Msisdn == App.MSISDN || sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
                    {
                        if (sm.Status_Type == StatusMessage.StatusType.TEXT_UPDATE)
                        {
                            StatusMsgsTable.SaveLastStatusMessage(sm.Message, sm.MoodId);
                            //update profile status
                            if (sm.MoodId > 0)
                                statusImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(sm.MoodId);
                            else
                                statusImage.Source = UI_Utils.Instance.TextStatusImage;

                            txtStatus.Text = sm.Message;
                        }
                        // if status list is not loaded simply ignore this packet , as then this packet will
                        // be shown twice , one here and one from DB.
                        if (isStatusMessagesLoaded)
                        {
                            StatusUpdateBox statusUpdate = StatusUpdateHelper.Instance.createStatusUIObject(sm, true,
                                statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap);
                            if (statusUpdate != null)
                            {
                                int index = 0;
                                if (ProTipHelper.CurrentProTip != null)
                                    index = 1;

                                if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatusUpdateUC)
                                    App.ViewModel.StatusList.RemoveAt(index);

                                App.ViewModel.StatusList.Insert(count + index, statusUpdate);
                            }
                        }
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
                            // if status list is not loaded simply ignore this packet , as then this packet will
                            // be shown twice , one here and one from DB.
                            if (isStatusMessagesLoaded)
                            {
                                StatusUpdateBox statusUpdate = StatusUpdateHelper.Instance.createStatusUIObject(sm, true,
                                    statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap);
                                if (statusUpdate != null)
                                {
                                    int index = 0;
                                    if (ProTipHelper.CurrentProTip != null)
                                        index = 1;

                                    if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatusUpdateUC)
                                        App.ViewModel.StatusList.RemoveAt(index);
                                    
                                    App.ViewModel.StatusList.Insert(count + index, statusUpdate);
                                }
                            }
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
                        {
                            _totalUnreadStatuses--;
                            RefreshBarCount--;
                        }

                        if (App.ViewModel.StatusList.Count == 0 || (App.ViewModel.StatusList.Count == 1 && ProTipHelper.CurrentProTip != null))
                        {
                            string firstName = Utils.GetFirstName(accountName.Text);
                            App.ViewModel.StatusList.Add(new DefaultStatusUpdateUC(string.Format(AppResources.Conversations_EmptyStatus_Hey_Txt, firstName), CircleOfFriends_Tap, UpdateStatus_Tap));
                        }
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
                        if (c != null && c.Msisdn != App.MSISDN)
                        {
                            c.IsUsedAtMiscPlaces = true;
                            hikeContactList.Add(c);
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
                if (obj is ContactInfo)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (obj != null)
                        {
                            ContactInfo c = obj as ContactInfo;
                            c.IsUsedAtMiscPlaces = true;
                            hikeContactList.Remove(c);
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
                            c.IsUsedAtMiscPlaces = true;
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
                            c.IsUsedAtMiscPlaces = true;
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                hikeContactList.Remove(c);
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
                //TODO : Madhur Garg , you can handle bug#3999 https://hike.fogbugz.com/default.asp?3999 here 
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
                                if (App.ViewModel.StatusList[i] is FriendRequestStatus)
                                {
                                    FriendRequestStatus f = App.ViewModel.StatusList[i] as FriendRequestStatus;
                                    if (f.Msisdn == c.Msisdn)
                                    {
                                        Dispatcher.BeginInvoke(() =>
                                        {
                                            if (i < UnreadFriendRequests)
                                            {
                                                UnreadFriendRequests--;
                                            }
                                            try
                                            {
                                                App.ViewModel.StatusList.Remove(f);
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
                        c.IsUsedAtMiscPlaces = true;
                        Dispatcher.BeginInvoke(() =>
                        {
                            hikeContactList.Remove(c);
                            if (hikeContactList.Count == 0)
                            {
                                emptyListPlaceholderHikeContacts.Visibility = Visibility.Visible;
                                hikeContactListBox.Visibility = Visibility.Collapsed;
                            }
                        });
                    }
                    //if conatct is removed from circle of friends then show no friends placehoder
                    if (App.ViewModel.FavList.Count == 0)
                    {
                        Dispatcher.BeginInvoke(() =>
                       {
                           emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                           favourites.Visibility = System.Windows.Visibility.Collapsed;
                       });
                    }
                    #endregion
                }
            }
            #endregion
            #region UNBLOCK_USER
            else if (HikePubSub.UNBLOCK_USER == type || HikePubSub.UNBLOCK_GROUPOWNER == type)
            {
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
                    c.IsUsedAtMiscPlaces = true;
                    if (c.Msisdn != App.MSISDN)
                        hikeContactList.Add(c);
                    if (emptyListPlaceholderHikeContacts.Visibility == Visibility.Visible)
                    {
                        emptyListPlaceholderHikeContacts.Visibility = Visibility.Collapsed;
                        hikeContactListBox.Visibility = Visibility.Visible;
                    }
                });

            }
            #endregion
            #region PRO_TIPS
            else if (HikePubSub.PRO_TIPS_REC == type)
            {
                var vals = obj as object[];

                var id = (string)vals[0];
                var header = (string)vals[1];
                var text = (string)vals[2];
                var imageUrl = (string)vals[3];
                Int64 time = 0;
                try
                {
                    time = (Int64)vals[4];
                }
                catch
                {
                }

                if (time > 0)
                {
                    if (App.appSettings.Contains(App.PRO_TIP_DISMISS_TIME))
                        App.appSettings[App.PRO_TIP_DISMISS_TIME] = time;
                    else
                        App.WriteToIsoStorageSettings(App.PRO_TIP_DISMISS_TIME, time);

                    ProTipHelper.Instance.ChangeTimerTime(time);
                }
                else
                {
                    if (!App.appSettings.Contains(App.PRO_TIP_DISMISS_TIME))
                        App.WriteToIsoStorageSettings(App.PRO_TIP_DISMISS_TIME, HikeConstants.DEFAULT_PRO_TIP_TIME);
                }

                ProTipHelper.Instance.AddProTip(id, header, text, imageUrl);
            }
            #endregion
            #region DELETE CONVERSATION
            else if (HikePubSub.DELETE_STATUS_AND_CONV == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ConversationListObject co = obj as ConversationListObject;
                    App.ViewModel.MessageListPageCollection.Remove(co);
                    if (App.ViewModel.MessageListPageCollection.Count == 0)
                    {
                        emptyScreenImage.Opacity = 1;
                        emptyScreenTip.Opacity = 1;
                    }
                });
            }
            #endregion
        }

        #endregion

        #region CONTEXT MENUS

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_Delete_Chat_Confirmation, AppResources.Conversations_DelChat_Txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
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
                    emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Visible;
                    favourites.Visibility = System.Windows.Visibility.Collapsed;
                    //addFavsPanel.Opacity = 0;
                }
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
                    c.IsUsedAtMiscPlaces = true;
                    if (c.Msisdn != App.MSISDN)
                        hikeContactList.Add(c);
                }
                if (hikeContactList.Count > 0)
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
                c.IsUsedAtMiscPlaces = true;
                hikeContactList.Remove(c);
                FriendsTableUtils.FriendStatusEnum fs = FriendsTableUtils.SetFriendStatus(convObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                App.ViewModel.FavList.Insert(0, convObj);
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
                App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_CONVLIST);
            }
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

        private void InviteBtn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
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

        private void Settings_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.SETTINGS);
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.Relative));
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
            favourites.SelectedIndex = -1;
        }

        private void RemoveFavourite_Click(object sender, RoutedEventArgs e)
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
                    c.IsUsedAtMiscPlaces = true;
                    if (c.Msisdn != App.MSISDN)
                        hikeContactList.Add(c);
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
                    contactInfo.IsUsedAtMiscPlaces = true;
                    hikeContactList.Remove(contactInfo);
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
                    cObj = new ConversationListObject(contactInfo.Msisdn, contactInfo.Name, contactInfo.OnHike, contactInfo.Avatar);
                }
                contactInfo.IsUsedAtMiscPlaces = true;
                hikeContactList.Remove(contactInfo);
                App.ViewModel.FavList.Add(cObj);
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
                        if (launchPagePivot.SelectedIndex == 3)
                        {
                            if (_refreshBarCount == 0 && value > 0)
                            {
                                refreshStatusBackground.Visibility = System.Windows.Visibility.Visible;
                                refreshBarPanel.Visibility = System.Windows.Visibility.Visible;
                            }
                            else if (_refreshBarCount > 0 && value == 0)
                            {
                                refreshStatusBackground.Visibility = System.Windows.Visibility.Collapsed;
                                refreshBarPanel.Visibility = System.Windows.Visibility.Collapsed;
                                FreshStatusUpdates.Clear();
                            }
                            if (refreshBarPanel.Visibility == System.Windows.Visibility.Visible && value > 0)
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
                        if (launchPagePivot.SelectedIndex == 3)
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
            UpdatePendingStatusFromRefreshBar();
        }

        private void UpdatePendingStatusFromRefreshBar()
        {
            int index = 0;
            if (ProTipHelper.CurrentProTip != null)
                index = 1;

            // this fix will solve the possible crash , suggested by nitesh
            int pendingCount = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count + index : index;
            for (int i = 0; i < (FreshStatusUpdates != null ? FreshStatusUpdates.Count : 0); i++)
            {
                StatusUpdateBox statusUpdate = StatusUpdateHelper.Instance.createStatusUIObject(FreshStatusUpdates[i], true,
                    statusBox_Tap, statusBubblePhoto_Tap, enlargePic_Tap);
                if (statusUpdate != null)
                {
                    App.ViewModel.StatusList.Insert(pendingCount, statusUpdate);
                }
            }

            if (pendingCount > index)
            {
                if (App.ViewModel.StatusList.Count > index && App.ViewModel.StatusList[index] is DefaultStatusUpdateUC)
                    App.ViewModel.StatusList.RemoveAt(index);

                statusLLS.ScrollIntoView(App.ViewModel.StatusList[pendingCount]);
            }
            
            RefreshBarCount = 0;
        }
        private void postStatusBtn_Click(object sender, EventArgs e)
        {
            if (TutorialStatusUpdate.Visibility == Visibility.Visible)
            {
                RemoveStatusUpdateTutorial();
            }
            else if (gridBasicTutorial.Visibility == Visibility.Visible)
            {
                RemoveTutorial();
            }
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
            if (cn == null)
            {
                if (App.ViewModel.ContactsCache.ContainsKey(fObj.Msisdn))
                {
                    cn = App.ViewModel.ContactsCache[fObj.Msisdn];
                    cn.IsUsedAtMiscPlaces = true;
                    hikeContactList.Remove(cn);
                }
            }

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
            if (emptyListPlaceholderFiends.Visibility == System.Windows.Visibility.Visible)
            {
                emptyListPlaceholderFiends.Visibility = System.Windows.Visibility.Collapsed;
                favourites.Visibility = System.Windows.Visibility.Visible;
            }
            StatusMessage sm = new StatusMessage(fObj.Msisdn, AppResources.Now_Friends_Txt, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1);
            mPubSub.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
            mPubSub.publish(HikePubSub.STATUS_RECEIVED, sm);
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
            FriendsTableUtils.SetFriendStatus(fObj.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
        }

        private void notification_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (launchPagePivot.SelectedIndex != 3)
            {
                launchPagePivot.SelectedIndex = 3;
                
                int index = 0;
                if (ProTipHelper.CurrentProTip != null)
                    index = 1;

                int pendingCount = App.ViewModel.PendingRequests != null ? App.ViewModel.PendingRequests.Count : 0;
                //if no new status scroll to latest unseen friends request
                if (UnreadFriendRequests > 0 && (pendingCount > UnreadFriendRequests))
                {
                    int x = pendingCount - UnreadFriendRequests;
                    if (x >= 0 && App.ViewModel.StatusList.Count > x)
                        statusLLS.ScrollIntoView(App.ViewModel.StatusList[x + index]); //handling index out of bounds exception
                }
                //scroll to latest unread status
                else if ((App.ViewModel.StatusList.Count > pendingCount) && RefreshBarCount > 0
                    && App.ViewModel.StatusList.Count > pendingCount) //handling index out of bounds exception
                {
                    statusLLS.ScrollIntoView(App.ViewModel.StatusList[pendingCount + index]);
                }
            }
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (statusLLS.SelectedItem != null && statusLLS.SelectedItem is ImageStatusUpdate)
            {
                string[] statusImageInfo = new string[2];
                ImageStatusUpdate statusUpdate = (statusLLS.SelectedItem as ImageStatusUpdate);
                statusImageInfo[0] = statusUpdate.Msisdn;
                statusImageInfo[1] = statusUpdate.serverId;
                PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY] = statusImageInfo;
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
                Object[] obj = new Object[2];
                obj[0] = sb.Msisdn;
                obj[1] = sb.UserName;
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_TIMELINE] = obj;
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
                {
                    Object[] obj = new Object[2];
                    obj[0] = stsBox.Msisdn;
                    obj[1] = stsBox.UserName;
                    PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_TIMELINE] = obj;
                    NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                    return;
                }
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

        private void UpdateStatus_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Uri nextPage = new Uri("/View/PostStatus.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }


        #endregion

        #region Pro Tips

        private void dismissProTip_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.StatusList.RemoveAt(0);

            ProTipCount = 0;

            JObject proTipAnalyticsJson = new JObject();
            proTipAnalyticsJson.Add(Analytics.PRO_TIPS_DISMISSED, ProTipHelper.CurrentProTip._id);

            JObject data = new JObject();
            data.Add(HikeConstants.METADATA, proTipAnalyticsJson);
            data.Add(HikeConstants.SUB_TYPE, HikeConstants.UI_EVENT);
            data[HikeConstants.TAG] = utils.Utils.IsWP8 ? "wp8" : "wp7";

            JObject jsonObj = new JObject();
            jsonObj.Add(HikeConstants.TYPE, HikeConstants.LOG_EVENT);
            jsonObj.Add(HikeConstants.DATA, data);

            object[] publishData = new object[2];
            publishData[0] = jsonObj;
            publishData[1] = 1; //qos
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, publishData);

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

            ProTipHelper.Instance.StartTimer();
        }

        void showProTip()
        {
            ProTip proTip;
            App.appSettings.TryGetValue(App.PRO_TIP, out proTip);

            if (proTip != null)
            {
                App.ViewModel.StatusList.Insert(0, new ProTipUC(proTip, ProTipImage_Tapped, dismissProTip_Click));
                ProTipCount = 1;
            }
        }

        private void ProTipImage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.IMAGE_TO_DISPLAY] = true;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
        }

        #endregion
    }
}