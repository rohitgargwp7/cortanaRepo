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
using Phone.Controls;
using windows_client.Misc;
using System.Windows.Media;
using windows_client.Languages;
using windows_client.ViewModel;
using Microsoft.Phone.Net.NetworkInformation;
using System.Collections.ObjectModel;
using windows_client.Controls;

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {


        #region Instances

        bool _isFavListBound = false;
        bool _isPendingListBound = false;
        bool isProfilePicTapped = false;
        byte[] fullViewImageBytes = null;
        byte[] largeImageBytes = null;
        private bool firstLoad = true;
        private bool showFreeSMS = false;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
        private PhotoChooserTask photoChooserTask;
        private ApplicationBar appBar;
        ApplicationBarMenuItem delConvsMenu;
        ApplicationBarIconButton composeIconButton;
        BitmapImage profileImage = null;
        public MyProgressIndicator progress = null; // there should be just one instance of this.
        private bool isShowFavTute = true;
        #endregion

        #region Page Based Functions

        public ConversationsList()
        {
            InitializeComponent();
            initAppBar();
            initProfilePage();
            DeviceNetworkInformation.NetworkAvailabilityChanged += new EventHandler<NetworkNotificationEventArgs>(OnNetworkChange);
            if (isShowFavTute)
                showTutorial();
            App.ViewModel.ConversationListPage = this;
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

        private static void OnNetworkChange(object sender, EventArgs e)
        {
            //Microsoft.Phone.Net.NetworkInformation.NetworkInterface inherits from System.Net.NetworkInformation.NetworkInterface 
            //and adds the GetNetworkInterface static method and the NetworkInterfaceType static property
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                App.MqttManagerInstance.connect();
            }
            else
            {
                App.MqttManagerInstance.setConnectionStatus(Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET);
            }
        }


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.myListBox.SelectedIndex = -1;
            this.favourites.SelectedIndex = -1;
            this.pendingRequests.SelectedIndex = -1;
            myListBox.ScrollIntoView(App.ViewModel.MessageListPageCollection[0]);
//            convScroller.ScrollToVerticalOffset(0);
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

                firstLoad = false;
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
            DeviceNetworkInformation.NetworkAvailabilityChanged -= OnNetworkChange;
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
                    ContextMenuService.SetContextMenu(convObj.ConvBoxObj, createAttachmentContextMenu(convObj));
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
            #region PUSH NOTIFICATIONS STUFF

            bool isPushEnabled = true;
            appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
            if (isPushEnabled)
            {
                App.PushHelperInstance.registerPushnotifications();
            }
            #endregion
            #region CHECK UPDATES
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
            for (int i = 0; i < cl.Count; i++)
            {
                ConvMessage c = null;
                JObject j = new JObject();
                if (cl[i].OnHike)
                {
                    j[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.HIKE_USER;
                    c = new ConvMessage(ConvMessage.ParticipantInfoState.HIKE_USER, j);
                    c.Message = string.Format(AppResources.Conversations_OnHike_Txt, cl[i].Name);
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
            composeIconButton.Click += new EventHandler(selectUserBtn_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            /* Add Menu Items*/
            //convListPagePivot.ApplicationBar = appBar;

            ApplicationBarMenuItem groupChatIconButton = new ApplicationBarMenuItem();
            groupChatIconButton.Text = AppResources.GrpChat_Txt;
            groupChatIconButton.Click += new EventHandler(createGroup_Click);
            groupChatIconButton.IsEnabled = true;
            appBar.MenuItems.Add(groupChatIconButton);

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

        #region Listeners

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
            mPubSub.addListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
            mPubSub.addListener(HikePubSub.ADD_REMOVE_FAV_OR_PENDING, this);
            mPubSub.addListener(HikePubSub.REWARDS_TOGGLE, this);
            mPubSub.addListener(HikePubSub.REWARDS_CHANGED, this);
            mPubSub.addListener(HikePubSub.BAD_USER_PASS, this);
        }

        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
                mPubSub.removeListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
                mPubSub.removeListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
                mPubSub.removeListener(HikePubSub.ADD_REMOVE_FAV_OR_PENDING, this);
                mPubSub.removeListener(HikePubSub.REWARDS_TOGGLE, this);
                mPubSub.removeListener(HikePubSub.REWARDS_CHANGED, this);
                mPubSub.removeListener(HikePubSub.BAD_USER_PASS, this);
            }
            catch { }
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
                //favsBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x36, 0x36, 0x36));
            }
            else
            {
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_white.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_white.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite.png", UriKind.Relative));
                rewards.Source = new BitmapImage(new Uri("images/rewards_link.png", UriKind.Relative));
                //favsBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xe9, 0xe9, 0xe9));
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
                rewardsTxtBlk.Visibility = System.Windows.Visibility.Collapsed;
            }

            string name;
            appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name != null)
                accountName.Text = name;
            creditsTxtBlck.Text = string.Format(AppResources.SMS_Left_Txt, (int)App.appSettings[App.SMS_SETTING]);
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            Stopwatch st = Stopwatch.StartNew();
            byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(HikeConstants.MY_PROFILE_PIC);
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to fetch profile image : {0}", msec);

            if (_avatar != null)
            {
                MemoryStream memStream = new MemoryStream(_avatar);
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                avatarImage.Source = empImage;
            }
            else
            {
                avatarImage.Source = UI_Utils.Instance.getDefaultAvatar((string)App.appSettings[App.MSISDN_SETTING]);
            }
        }

        private void onProfilePicButtonTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!isProfilePicTapped)
                {
                    photoChooserTask.Show();
                    isProfilePicTapped = true;
                }
            }
            catch
            {

            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            //progressBarTop.IsEnabled = true;
            shellProgress.IsVisible = true;
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                profileImage = new BitmapImage(uri);
                profileImage.CreateOptions = BitmapCreateOptions.None;
                profileImage.UriSource = uri;
                profileImage.ImageOpened += imageOpenedHandler;
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                isProfilePicTapped = false;
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            using (var msLargeImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                largeImageBytes = msLargeImage.ToArray();
            }
            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                fullViewImageBytes = msSmallImage.ToArray();
            }
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    avatarImage.Source = profileImage;
                    avatarImage.MaxHeight = 83;
                    avatarImage.MaxWidth = 83;
                    object[] vals = new object[3];
                    vals[0] = App.MSISDN;
                    vals[1] = fullViewImageBytes;
                    vals[2] = largeImageBytes;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                }
                else
                {
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                isProfilePicTapped = false;
            });
        }

        #endregion

        #region AppBar Button Events

        private void deleteAllConvs_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Conversations_Delete_Chats_Confirmation, AppResources.Conversations_DelAllChats_Txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
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

        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            //if (isAppEnabled)
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
                if (!appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Insert(1, delConvsMenu);
            }
            else if (selectedIndex == 1)
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
            }
            else if (selectedIndex == 2) // favourite
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);

                // there will be two background workers that will independently load three sections

                #region Favourites

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
                    };
                    favBw.RunWorkerAsync();
                    favBw.RunWorkerCompleted += (sf, ef) =>
                    {
                        favourites.ItemsSource = App.ViewModel.FavList;
                    };
                }

                #endregion

                #region Pending Requests

                if (!_isPendingListBound)
                {
                    _isPendingListBound = true;
                    BackgroundWorker pendingBw = new BackgroundWorker();
                    pendingBw.DoWork += (sf, ef) =>
                    {
                        if (!App.ViewModel.IsPendingListLoaded) // if list is not loaded
                            MiscDBUtil.LoadPendingRequests();
                        App.ViewModel.IsPendingListLoaded = true;
                    };
                    pendingBw.RunWorkerAsync();
                    pendingBw.RunWorkerCompleted += (sf, ef) =>
                    {
                        pendingRequests.ItemsSource = App.ViewModel.PendingRequests;
                        if (App.ViewModel.FavList.Count > 0 || App.ViewModel.PendingRequests.Count > 0)
                        {
                            emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                            favourites.Visibility = System.Windows.Visibility.Visible;
                            addFavsPanel.Opacity = 1;
                        }
                    };
                }
                #endregion

            }
        }

        #endregion

        #region PUBSUB

        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConversationListObject mObj = (ConversationListObject)vals[1];
                if (mObj == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (emptyScreenImage.Visibility == Visibility.Visible)
                    {
                        emptyScreenTip.Opacity = 0;
                        emptyScreenImage.Opacity = 0;
                    }
//                    convScroller.ScrollToVerticalOffset(0);
                    myListBox.ScrollIntoView(App.ViewModel.MessageListPageCollection[0]);

                });
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
            #region ADD TO FAV OR PENDING
            else if (HikePubSub.ADD_REMOVE_FAV_OR_PENDING == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                        favourites.Visibility = System.Windows.Visibility.Visible;
                        addFavsPanel.Opacity = 1;
                    }
                    else if (App.ViewModel.FavList.Count == 0 && App.ViewModel.PendingRequests.Count == 0) // remove fav
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                        favourites.Visibility = System.Windows.Visibility.Collapsed;
                        addFavsPanel.Opacity = 0;
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
                catch (Exception e)
                {
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
                    if (App.ViewModel.FavList.Count == 0 && App.ViewModel.PendingRequests.Count == 0)
                    {
                        emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                        favourites.Visibility = System.Windows.Visibility.Collapsed;
                        addFavsPanel.Opacity = 0;
                    }
                    menuFavourite.Header = AppResources.Add_To_Fav_Txt;
                    App.AnalyticsInstance.addEvent(Analytics.REMOVE_FAVS_CONTEXT_MENU_CONVLIST);
                }
                else // add to fav
                {
                    convObj.IsFav = true;
                    App.ViewModel.FavList.Insert(0, convObj);
                    if (App.ViewModel.IsPending(convObj.Msisdn))
                    {
                        App.ViewModel.PendingRequests.Remove(convObj);
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
                        addFavsPanel.Opacity = 1;
                    }
                    menuFavourite.Header = AppResources.RemFromFav_Txt;
                    App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_CONVLIST);
                }
            }
        }

        public ContextMenu createAttachmentContextMenu(ConversationListObject convObj)
        {
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = true;

            MenuItem menuItemDelete = new MenuItem();
            menuItemDelete.Header = AppResources.Delete_Txt;
            var glCopy = GestureService.GetGestureListener(menuItemDelete);
            glCopy.Tap += MenuItem_Tap_Delete;
            menu.Items.Add(menuItemDelete);

            if (!convObj.IsGroupChat)
            {
                MenuItem menuItemFavourite = new MenuItem();
                if (convObj.IsFav) // if already favourite
                    menuItemFavourite.Header = AppResources.RemFromFav_Txt;
                else
                    menuItemFavourite.Header = AppResources.Add_To_Fav_Txt;

                var glCancel = GestureService.GetGestureListener(menuItemFavourite);
                glCancel.Tap += MenuItem_Tap_AddRemoveFav;
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
            App.AnalyticsInstance.addEvent(Analytics.EDIT_PROFILE);
            NavigationService.Navigate(new Uri("/View/EditProfile.xaml", UriKind.Relative));
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
        }

        public void checkUpdate_Callback(JObject obj)
        {
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
                        showCriticalUpdateMessage();
                        //critical update
                    }
                    else if ((Utils.compareVersion(latest, current) == 1) && (String.IsNullOrEmpty(lastDismissedUpdate) ||
                        (Utils.compareVersion(latest, lastDismissedUpdate) == 1)))
                    {
                        showNormalUpdateMessage();
                        //normal update
                    }
                    App.WriteToIsoStorageSettings(App.LAST_UPDATE_CHECK_TIME, TimeUtils.getCurrentTimeStamp());
                }
            }
            catch (Exception)
            {
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
                catch { }
            }
        }

        #endregion

        #region FAVOURITE ZONE

        private void yes_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            ConversationListObject fObj = (sender as Button).DataContext as ConversationListObject;

            if (App.ViewModel.Isfavourite(fObj.Msisdn)) // if already favourite just ignore
                return;

            App.ViewModel.PendingRequests.Remove(fObj);
            App.ViewModel.FavList.Insert(0, fObj);

            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);

            MiscDBUtil.SaveFavourites();
            MiscDBUtil.SaveFavourites(fObj);
            MiscDBUtil.SavePendingRequests();
            int count = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
            if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
            {
                emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
                favourites.Visibility = System.Windows.Visibility.Visible;
                addFavsPanel.Opacity = 1;
            }
        }

        private void no_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ConversationListObject fObj = (sender as Button).DataContext as ConversationListObject;
            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            App.ViewModel.PendingRequests.Remove(fObj);
            MiscDBUtil.SavePendingRequests();
            if (App.ViewModel.FavList.Count == 0 && App.ViewModel.PendingRequests.Count == 0)
            {
                emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                favourites.Visibility = System.Windows.Visibility.Collapsed;
                addFavsPanel.Opacity = 0;
            }
        }

        private void favourites_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ConversationListObject obj = favourites.SelectedItem as ConversationListObject;
            if (obj == null)
                return;
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = obj;
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
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
            }
            if (App.ViewModel.FavList.Count == 0 && App.ViewModel.PendingRequests.Count == 0)
            {
                emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
                favourites.Visibility = System.Windows.Visibility.Collapsed;
                addFavsPanel.Opacity = 0;
            }
        }

        #endregion

        private void Button_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (addFavsPanel.Opacity == 0)
                return;
            PhoneApplicationService.Current.State["HIKE_FRIENDS"] = true;
            string uri = "/View/InviteUsers.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void Button_Tap_2(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["HIKE_FRIENDS"] = true;
            string uri = "/View/InviteUsers.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}