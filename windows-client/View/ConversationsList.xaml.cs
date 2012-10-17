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
using Microsoft.Phone.Notification;
using System.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Devices;
using Microsoft.Xna.Framework.GamerServices;

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region CONSTANTS

        private readonly string DELETE_ALL_CONVERSATIONS = "Delete All Chats";
        private readonly string INVITE_USERS = "Invite Users";

        #endregion

        #region Instances

        bool isProfilePicTapped = false;
        byte[] thumbnailBytes = null;
        byte[] largeImageBytes = null;
        private bool firstLoad = true;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
        private PhotoChooserTask photoChooserTask;
        private ApplicationBar appBar;
        ApplicationBarMenuItem delConvsMenu;
        ApplicationBarIconButton composeIconButton;
        BitmapImage profileImage = null;

        #endregion

        #region Page Based Functions

        public ConversationsList()
        {
            InitializeComponent();
            initAppBar();
            initProfilePage();
            App.APP_LAUNCH_STATE = App.LaunchState.NORMAL_LAUNCH;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            this.myListBox.SelectedIndex = -1;
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (Utils.isCriticalUpdatePending())
            {
                showCriticalUpdateMessage();
            }
            if (firstLoad)
            {
                progressBar.Opacity = 1;
                progressBar.IsEnabled = true;
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
        }

        //Push notifications
        #region push notifications
        public void postPushNotification_Callback(JObject obj)
        {
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            AccountUtils.postPushNotification(e.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        //void PushChannel_ShellToastNotificationReceived(object sender, NotificationEventArgs e)
        //{
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
        private void loadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;

            myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;

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
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_NEW_INSTALLATION))
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.IS_NEW_INSTALLATION);
                Utils.requestAccountInfo();
            }

            // move to seperate thread later
            #region PUSH NOTIFICATIONS STUFF

            bool isPushEnabled = true;
            appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
            if (isPushEnabled)
            {
                HttpNotificationChannel pushChannel;
                // Try to find the push channel.
                pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);

                try
                {
                    // If the channel was not found, then create a new connection to the push service.
                    if (pushChannel == null)
                    {
                        pushChannel = new HttpNotificationChannel(HikeConstants.pushNotificationChannelName);

                        // Register for all the events before attempting to open the channel.
                        pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                        pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                        // Register for this notification only if you need to receive the notifications while your application is running.
                        //pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);
                        pushChannel.Open();
                        // Bind this new channel for toast events.
                        pushChannel.BindToShellToast();
                        pushChannel.BindToShellTile();

                    }
                    else
                    {
                        // The channel was already open, so just register for all the events.
                        pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                        pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                        // Register for this notification only if you need to receive the notifications while your application is running.
                        //pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                        if (pushChannel.ChannelUri != null)
                        {
                            System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                            AccountUtils.postPushNotification(pushChannel.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
                        }
                    }
                }
                catch (InvalidOperationException ioe)
                {

                }
                catch (Exception)
                {
                }
            }
            #endregion
            #region CHECK UPDATES
            checkForUpdates();
            #endregion
            postAnalytics();
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
            composeIconButton.Text = "new chat";
            composeIconButton.Click += new EventHandler(selectUserBtn_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            /* Add Menu Items*/
            convListPagePivot.ApplicationBar = appBar;

            ApplicationBarMenuItem groupChatIconButton = new ApplicationBarMenuItem();
            groupChatIconButton.Text = "Group Chat";
            groupChatIconButton.Click += new EventHandler(createGroup_Click);
            groupChatIconButton.IsEnabled = true;
            appBar.MenuItems.Add(groupChatIconButton);

            delConvsMenu = new ApplicationBarMenuItem();
            delConvsMenu.Text = DELETE_ALL_CONVERSATIONS;
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
            ConversationListObject obj = myListBox.SelectedItem as ConversationListObject;
            if (obj == null)
                return;
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = obj;
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
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.removeListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
            mPubSub.removeListener(HikePubSub.UPDATE_ACCOUNT_NAME, this);
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
            }
            else
            {
                emptyScreenImage.Source = new BitmapImage(new Uri("images/empty_screen_logo_white.png", UriKind.Relative));
                emptyScreenTip.Source = new BitmapImage(new Uri("images/empty_screen_tip_white.png", UriKind.Relative));
                invite.Source = new BitmapImage(new Uri("images/invite.png", UriKind.Relative));
            }
            editProfileTextBlck.Foreground = creditsTxtBlck.Foreground = UI_Utils.Instance.EditProfileForeground;
            string name;
            appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name != null)
                accountName.Text = name;
            creditsTxtBlck.Text = Convert.ToString(App.appSettings[App.SMS_SETTING]) + " Left";

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 83;
            photoChooserTask.PixelWidth = 83;
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
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show("Please try again", "No network connectivity", MessageBoxButton.OK);
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
                writeableBitmap.SaveJpeg(msSmallImage, 83, 83, 0, 95);
                thumbnailBytes = msSmallImage.ToArray();
            }

            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(thumbnailBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && "ok" == (string)obj["stat"])
                {
                    avatarImage.Source = profileImage;
                    avatarImage.MaxHeight = 83;
                    avatarImage.MaxWidth = 83;
                    object[] vals = new object[3];
                    vals[0] = App.MSISDN;
                    vals[1] = thumbnailBytes;
                    vals[2] = largeImageBytes;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                }
                else
                {
                    MessageBox.Show("Cannot change Profile Image. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
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
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting all chats?", "Delete All Chats", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            disableAppBar();
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            NetworkManager.turnOffNetworkManager = true;
            mPubSub.publish(HikePubSub.DELETE_ALL_CONVERSATIONS, null);
            App.AnalyticsInstance.addEvent(Analytics.DELETE_ALL_CHATS);
        }

        private void createGroup_Click(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.GROUP_CHAT);
            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
            //NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            //if (isAppEnabled)
            App.AnalyticsInstance.addEvent(Analytics.COMPOSE);
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void MenuItem_Tap_Delete(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting this chat?", "Delete Chat", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
            {
                return;
            }
            ConversationListObject convObj = selectedListBoxItem.DataContext as ConversationListObject;
            if (convObj != null)
                deleteConversation(convObj);
        }

        private void deleteConversation(ConversationListObject convObj)
        {
            App.ViewModel.ConvMap.Remove(convObj.Msisdn); // removed entry from map for UI
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
                    creditsTxtBlck.Text = Convert.ToString((int)obj) + " Left";
                });
            }
            #endregion
            #region DELETED_ALL_CONVERSATIONS
            else if (HikePubSub.DELETED_ALL_CONVERSATIONS == type)
            {
                App.ViewModel.ConvMap.Clear();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.MessageListPageCollection.Clear();
                    emptyScreenImage.Opacity = 1;
                    emptyScreenTip.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    enableAppBar();
                });
                NetworkManager.turnOffNetworkManager = false;
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
            NavigationService.Navigate(new Uri("/View/Privacy.xaml", UriKind.Relative));
        }

        private void Help_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.HELP);
            NavigationService.Navigate(new Uri("/View/Help.xaml", UriKind.Relative));
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
                AccountUtils.checkForUpdates(new AccountUtils.postResponseFunction(checkUpdate_Callback));
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
                    string current = Utils.GetVersion();
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
                Guide.BeginShowMessageBox(HikeConstants.CRITICAL_UPDATE_HEADING, HikeConstants.CRITICAL_UPDATE_TEXT,
                     new List<string> { "Update" }, 0, MessageBoxIcon.Alert,
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
                Guide.BeginShowMessageBox(HikeConstants.NORMAL_UPDATE_HEADING, HikeConstants.NORMAL_UPDATE_TEXT,
                     new List<string> { "Update", "Ignore" }, 0, MessageBoxIcon.Alert,
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
                    //                    isAppEnabled = false;
                    composeIconButton.IsEnabled = false;
                });
            }
        }


        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            //if (!String.IsNullOrEmpty(latestVersionString))
            //{
            //    App.WriteToIsoStorageSettings(App.LAST_DISMISSED_UPDATE_VERSION, latestVersionString);
            //}
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
                marketplaceDetailTask.Show();
            }
        }

        #endregion

    }
}