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
using Phone.Controls;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using Microsoft.Phone.Notification;
using System.Net.NetworkInformation;

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region CONSTANTS

        private readonly string DELETE_ALL_CONVERSATIONS = "Delete All Chats";
        private readonly string INVITE_USERS = "Invite Users";

        #endregion

        #region Instances

        private bool firstLoad = true;
        public MyProgressIndicator progress = null; // there should be just one instance of this.
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
        private static Dictionary<string, ConversationListObject> convMap = null; // this holds msisdn -> conversation mapping
        private PhotoChooserTask photoChooserTask;
        private ApplicationBar appBar;
        ApplicationBarMenuItem delConvsMenu;
        ApplicationBarMenuItem delAccountMenu;
        ApplicationBarIconButton composeIconButton;

        public static Dictionary<string, ConversationListObject> ConvMap
        {
            get
            {
                return convMap;
            }
            set
            {
                if (value != convMap)
                    convMap = value;
            }
        }

        #endregion

        #region Page Based Functions

        public ConversationsList()
        {
            Stopwatch stPage = Stopwatch.StartNew();
            InitializeComponent();
            initAppBar();
            initProfilePage();
            stPage.Stop();
            long tinmsec = stPage.ElapsedMilliseconds;
            Debug.WriteLine("Conversations List Page : Total Loading time : {0}", tinmsec);
            App.isConvCreated = true;
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


        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (firstLoad)
            {
                if (convMap == null)
                    convMap = new Dictionary<string, ConversationListObject>();
                progressBar.Opacity = 1; ;
                progressBar.IsEnabled = true;
                mPubSub = App.HikePubSubInstance;
                registerListeners();
                #region LOAD MESSAGES

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    if (App.IsAppLaunched)  // represents normal launch
                        LoadMessages();
                    else // tombstone launch
                    {
                        Debug.WriteLine("CONVERSATIONS LIST :: Recovered from tombstone.");
                    }

                };
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(loadingCompleted);
                bw.RunWorkerAsync();

                #endregion

                if (App.IsAppLaunched)
                {
                    #region InitializeEmoticons

                    Stopwatch st = Stopwatch.StartNew();
                    SmileyParser.loadEmoticons();
                    st.Stop();
                    long msec = st.ElapsedMilliseconds;
                    Debug.WriteLine("APP: Time to Instantiate emoticons : {0}", msec);

                    #endregion
                }
                firstLoad = false;
            }
            if (App.ViewModel.MessageListPageCollection.Count == 0)
                emptyScreenImage.Opacity = 1;
            else
                emptyScreenImage.Opacity = 0;
        }

        #endregion

        #region ConvList Page

        /* This function will run on UI Thread */
        private void loadingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;

            myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;

            if (App.ViewModel.MessageListPageCollection.Count == 0)
                emptyScreenImage.Opacity = 1;
            else
                emptyScreenImage.Opacity = 0;

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

                        if (pushChannel.ChannelUri == null)
                            return;
                        System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                        AccountUtils.postPushNotification(pushChannel.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
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

        }

        public static void LoadMessages()
        {

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<ConversationListObject> conversationList = ConversationTableUtils.getAllConversations();
            stopwatch.Stop();
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("Time to get {0} Conversations from DB : {1} ms", conversationList == null ? 0 : conversationList.Count, elapsedMilliseconds);
            if (conversationList == null || conversationList.Count == 0)
            {
                return;
            }
            for (int i = 0; i < conversationList.Count; i++)
            {
                stopwatch.Reset(); 
                stopwatch.Start();
                byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(conversationList[i].Msisdn);
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                ConversationListObject conv = conversationList[i];
                conv.Avatar = _avatar;
                if (convMap == null)
                    convMap = new Dictionary<string, ConversationListObject>();
                convMap.Add(conv.Msisdn, conv);
                App.ViewModel.MessageListPageCollection.Add(conv);
            }
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
            composeIconButton.Text = "Compose";
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

            delAccountMenu = new ApplicationBarMenuItem();
            delAccountMenu.Text = "delete account";
            delAccountMenu.Click += new EventHandler(deleteAccount_Click);
        }

        public static void ReloadConversations() // running on some background thread
        {
            App.MqttManagerInstance.disconnectFromBroker(false);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.MessageListPageCollection.Clear();
                convMap.Clear();
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
            mPubSub.addListener(HikePubSub.SEND_NEW_MSG, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.ACCOUNT_DELETED, this);
            mPubSub.addListener(HikePubSub.GROUP_NAME_CHANGED, this);
            mPubSub.addListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.removeListener(HikePubSub.SEND_NEW_MSG, this);
            mPubSub.removeListener(HikePubSub.USER_JOINED, this);
            mPubSub.removeListener(HikePubSub.USER_LEFT, this);
            mPubSub.removeListener(HikePubSub.UPDATE_UI, this);
            mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.removeListener(HikePubSub.ACCOUNT_DELETED, this);
            mPubSub.removeListener(HikePubSub.GROUP_NAME_CHANGED, this);
            mPubSub.removeListener(HikePubSub.DELETED_ALL_CONVERSATIONS, this);
        }

        #endregion

        #region Profile Screen

        private void initProfilePage()
        {
            string name;
            appSettings.TryGetValue(App.ACCOUNT_NAME, out name);
            if (name != null)
                accountName.Text = name;
            creditsTxtBlck.Text = Convert.ToString(App.appSettings[App.SMS_SETTING]);

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 95;
            photoChooserTask.PixelWidth = 95;
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

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            byte[] thumbnailBytes = null;
            byte[] largeImageBytes = null;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            using (var msLargeImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                largeImageBytes = msLargeImage.ToArray();
            }

            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, 35, 35, 0, 95);
                thumbnailBytes = msSmallImage.ToArray();
            }
            
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(thumbnailBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
            object[] vals = new object[3];
            vals[0] = App.MSISDN;
            vals[1] = thumbnailBytes;
            vals[2] = largeImageBytes;
            mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
        }

        public void updateProfile_Callback(JObject obj)
        {
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show("Connection Problem. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                return;
            }
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                avatarImage.Source = image;
                avatarImage.Height = 90;
                avatarImage.Width = 90;
            }
            //else
            //{
            //    Uri uri = new Uri("/View/images/tux.png", UriKind.Relative);
            //    BitmapImage image = new BitmapImage(uri);
            //    image.CreateOptions = BitmapCreateOptions.None;
            //    image.UriSource = uri;
            //    image.ImageOpened += imageOpenedHandler;
            //    avatarImage.Source = image;
            //    avatarImage.Height = 90;
            //    avatarImage.Width = 90;
            //}
        }

        private void onProfilePicButtonTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                photoChooserTask.Show();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }

        #endregion

        #region AppBar Button Events

        private void deleteAllConvs_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting all chats.", "Delete All Chats ?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            disableAppBar();
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            NetworkManager.turnOffNetworkManager = true;
            mPubSub.publish(HikePubSub.DELETE_ALL_CONVERSATIONS, null);
        }

        #region Delete Account

        private void deleteAccount_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting account.", "Delete Account ?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
            {
                progress = new MyProgressIndicator("Deleting Account...");
            }

            disableAppBar();
            progress.Show();
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        // will be called on UI Thread
        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || "fail" == (string)obj["stat"])
            {
                Debug.WriteLine("Delete Account", "Could not delete account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show("Could not deleting account now. Try again later.", "Delete Account Failed?", MessageBoxButton.OKCancel);
                    enableAppBar();
                    progress.Hide();
                });
                return;
            }
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            appSettings.Clear();
            App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            mPubSub.publish(HikePubSub.DELETE_ACCOUNT, null);
        }

        #endregion

        private void createGroup_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.START_NEW_GROUP] = true;
            //NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            //NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void MenuItem_Tap_Delete(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting conversation.", "Delete Conversation ?", MessageBoxButton.OKCancel);
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
            convMap.Remove(convObj.Msisdn); // removed entry from map for UI
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
                if (appBar.MenuItems.Contains(delAccountMenu))
                    appBar.MenuItems.Remove(delAccountMenu);
            }
            else if (selectedIndex == 1)
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                if (!appBar.MenuItems.Contains(delAccountMenu))
                    appBar.MenuItems.Insert(0, delAccountMenu);
            }
        }

        #endregion

        #region PUBSUB

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConversationListObject mObj = (ConversationListObject)vals[1];
                if (convMap.ContainsKey(mObj.Msisdn))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        App.ViewModel.MessageListPageCollection.Remove(mObj);
                    });
                }
                convMap[mObj.Msisdn] = mObj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (emptyScreenImage.Visibility == Visibility.Visible)
                        emptyScreenImage.Opacity = 0;
                    App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                });

            }
            else if ((HikePubSub.USER_LEFT == type) || (HikePubSub.USER_JOINED == type))
            {
                string msisdn = (string)obj;
                try
                {
                    ConversationListObject convObj = convMap[msisdn];
                    convObj.IsOnhike = HikePubSub.USER_JOINED == type;
                }
                catch (KeyNotFoundException)
                {
                }
            }
            else if (HikePubSub.UPDATE_UI == type)
            {
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                if (!convMap.ContainsKey(msisdn))
                    return;

                ConversationListObject convObj = convMap[msisdn];
                byte[] _avatar = (byte[])vals[1];
                try
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        convObj.Avatar = _avatar;
                        //convObj.NotifyPropertyChanged("AvatarImage");
                    });
                }
                catch (KeyNotFoundException)
                {
                }
            }
            else if (HikePubSub.SMS_CREDIT_CHANGED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    creditsTxtBlck.Text = Convert.ToString((int)obj);
                });
            }
            else if (HikePubSub.ACCOUNT_DELETED == type)
            {
                removeListeners();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    emptyScreenImage.Opacity = 1;
                    myListBox.ItemsSource = null;
                    App.ViewModel.MessageListPageCollection.Clear();
                    convMap.Clear();
                    progress.Hide();
                    NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
                });
            }
            else if (HikePubSub.DELETED_ALL_CONVERSATIONS == type)
            {
                convMap.Clear();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.MessageListPageCollection.Clear();
                    emptyScreenImage.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    enableAppBar();
                });
                NetworkManager.turnOffNetworkManager = false;
            }

            #region GROUP NAME CHANGED

            else if (HikePubSub.GROUP_NAME_CHANGED == type)
            {
                object[] vals = (object[])obj;
                string groupId = (string)vals[0];
                string groupName = (string)vals[1];
                ConversationListObject cObj = convMap[groupId];
                cObj.ContactName = groupName;
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

            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(messageString);
            Paragraph p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;

            int maxCount = matchCollection.Count < HikeConstants.MAX_EMOTICON_SUPPORTED ? matchCollection.Count : HikeConstants.MAX_EMOTICON_SUPPORTED;
            for (int i = 0; i < maxCount; i++)
            {
                String emoticon = matchCollection[i].ToString();

                //Regex never returns an empty string. Still have added an extra check
                if (String.IsNullOrEmpty(emoticon))
                    continue;

                int index = matchCollection[i].Index;
                endIndex = index - 1;

                if (index > 0)
                {
                    Run r = new Run();
                    r.Text = messageString.Substring(startIndex, endIndex - startIndex + 1);
                    p.Inlines.Add(r);
                }

                startIndex = index + emoticon.Length;

                //TODO check if imgPath is null or not
                Image img = new Image();
                img.Source = SmileyParser.lookUpFromCache(emoticon);
                img.Height = 25;
                img.Width = 25;
                img.Margin = imgMargin;

                InlineUIContainer ui = new InlineUIContainer();
                ui.Child = img;
                p.Inlines.Add(ui);
            }
            if (startIndex < messageString.Length)
            {
                Run r2 = new Run();
                r2.Text = messageString.Substring(startIndex, messageString.Length - startIndex);
                p.Inlines.Add(r2);
            }
            richTextBox.Blocks.Clear();
            richTextBox.Blocks.Add(p);
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
            NavigationService.Navigate(new Uri("/View/Settings.xaml", UriKind.Relative));

        }
    }
}