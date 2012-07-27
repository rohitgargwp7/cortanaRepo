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

namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region CONSTANTS

        private readonly string DELETE_ALL_CONVERSATIONS = "Delete All Chats";
        private readonly string INVITE_USERS = "Invite Users";

        #endregion

        #region Instances

        public MyProgressIndicator progress = null;
        private HikePubSub mPubSub;
        private IsolatedStorageSettings appSettings = App.appSettings;
        private static Dictionary<string, ConversationListObject> convMap = null; // this holds msisdn -> conversation mapping
        public static Dictionary<string, bool> convMap2 = null;
        private PhotoChooserTask photoChooserTask;
        private string msisdn;
        private ApplicationBar appBar;
        ApplicationBarMenuItem delConvsMenu;
        ApplicationBarMenuItem delAccountMenu;

        public static Dictionary<string, ConversationListObject> ConvMap
        {
            get
            {
                return convMap;
            }
        }

        #endregion

        #region Page Based Functions

        public ConversationsList()
        {
            InitializeComponent();
            //myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
            convMap = new Dictionary<string, ConversationListObject>();
            convMap2 = new Dictionary<string, bool>();
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            #region Load App level instances

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_LoadAppInstances);
            bw.RunWorkerAsync();

            #endregion
            initAppBar();
            initProfilePage();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (App.ViewModel.MessageListPageCollection.Count == 0)
            {
                emptyScreenImage.Visibility = Visibility.Visible;
            }
            else
            {
                emptyScreenImage.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region ConvList Page

        private void bw_LoadAppInstances(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                mPubSub = App.HikePubSubInstance;
                Stopwatch stopwatch = Stopwatch.StartNew();
                LoadMessages();
                stopwatch.Stop();
                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine("Total Time to load messages: {0} ms", elapsedMilliseconds);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                    stopwatch = Stopwatch.StartNew();
                    myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
                    stopwatch.Stop();
                    elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    Debug.WriteLine("Total Time to load messages to itemsource: {0} ms", elapsedMilliseconds);
                
                    if (App.ViewModel.MessageListPageCollection.Count == 0)
                    {
                        emptyScreenImage.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        emptyScreenImage.Visibility = Visibility.Collapsed;
                    }
                    appBar.Mode = ApplicationBarMode.Default;
                    appBar.IsMenuEnabled = true;
                    appBar.Opacity = 1;
                });
                App.DbListener.registerListeners();
                registerListeners();
                NetworkManager.turnOffSystem = false;
                App.MqttManagerInstance.connect();
            }
        }

        private static void LoadMessages()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();            
            List<Conversation> conversationList = ConversationTableUtils.getAllConversations();
            stopwatch.Stop();
            long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("Time to get {0} Conversations: {1} ms",conversationList==null?0:conversationList.Count, elapsedMilliseconds);
            List<ConversationListObject> sortedConversationObjects = new List<ConversationListObject>();
            if (conversationList == null)
            {
                return;
            }
            for (int i = 0; i < conversationList.Count; i++)
            {
                Conversation conv = conversationList[i];
                stopwatch = Stopwatch.StartNew();
                ConvMessage lastMessage = MessagesTableUtils.getLastMessageForMsisdn(conv.Msisdn); // why we are not getting only lastmsg as string 
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine("Time to get Last Message for Conversation # {0} with MSISDN {1}: {2} ms",i+1,conv.Msisdn ,elapsedMilliseconds);

                stopwatch = Stopwatch.StartNew();
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(conv.Msisdn);
                stopwatch.Stop();
                elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                Debug.WriteLine("Time to get ContactInfo for Conversation # {0} : {1} ms", i + 1, elapsedMilliseconds);
                
                ConversationListObject mObj = new ConversationListObject((contact == null) ? conv.Msisdn : contact.Msisdn, (contact == null) ? null : contact.Name, lastMessage.Message, (contact == null) ? conv.OnHike : contact.OnHike,
                    TimeUtils.getTimeString(lastMessage.Timestamp), lastMessage.Timestamp);
                convMap.Add(conv.Msisdn, mObj);
                convMap2.Add(conv.Msisdn, false);
                sortedConversationObjects.Add(mObj);
            }
            stopwatch = Stopwatch.StartNew();
            sortedConversationObjects.Sort();           
            stopwatch.Stop();
            elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            Debug.WriteLine("Time to get Sort {0} Conversations: {1} ms", conversationList == null ? 0 : conversationList.Count, elapsedMilliseconds);

            App.ViewModel.MessageListPageCollection.AddRange(sortedConversationObjects);
        }

        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;
            appBar.Opacity = 0;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            /* Add icons */
            ApplicationBarIconButton composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/appbar.add.rest.png", UriKind.Relative);
            composeIconButton.Text = "compose";
            composeIconButton.Click += new EventHandler(selectUserBtn_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            /* Add Menu Items*/
            ApplicationBarMenuItem inviteUsersMenu = new ApplicationBarMenuItem();
            inviteUsersMenu.Text = INVITE_USERS;
            inviteUsersMenu.Click += new EventHandler(inviteUsers_Click);
            appBar.MenuItems.Add(inviteUsersMenu);
            convListPagePivot.ApplicationBar = appBar;

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
                convMap2.Clear();
                LoadMessages();
            });

            App.MqttManagerInstance.connect();
        }

        private void btnGetSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ConversationListObject obj = myListBox.SelectedItem as ConversationListObject;
            if (obj == null)
                return;
            PhoneApplicationService.Current.State["objFromConversationPage"] = obj;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        #endregion

        #region Listeners

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.SEND_NEW_MSG, this);
            mPubSub.addListener(HikePubSub.MSG_READ, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.ACCOUNT_DELETED, this);
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.removeListener(HikePubSub.SEND_NEW_MSG, this);
            mPubSub.removeListener(HikePubSub.MSG_READ, this);
            mPubSub.removeListener(HikePubSub.USER_JOINED, this);
            mPubSub.removeListener(HikePubSub.USER_LEFT, this);
            mPubSub.removeListener(HikePubSub.UPDATE_UI, this);
            mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.removeListener(HikePubSub.ACCOUNT_DELETED, this);
        }

        #endregion

        #region Profile Screen

        private void initProfilePage()
        {
            msisdn = (string)App.appSettings[App.MSISDN_SETTING];
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

            Thumbnails profileThumbnail = MiscDBUtil.getThumbNailForMSisdn(msisdn + "::large");
            if (profileThumbnail != null)
            {
                MemoryStream memStream = new MemoryStream(profileThumbnail.Avatar);
                memStream.Seek(0, SeekOrigin.Begin);

                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                avatarImage.Source = empImage;
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            byte[] buffer = null;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            MemoryStream msLargeImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
            MemoryStream msSmallImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msSmallImage, 35, 35, 0, 95);
            buffer = msSmallImage.ToArray();
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(buffer, new AccountUtils.postResponseFunction(updateProfile_Callback));

            object[] vals = new object[3];
            vals[0] = msisdn;
            vals[1] = msSmallImage;
            vals[2] = msLargeImage;
            mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
        }

        public void updateProfile_Callback(JObject obj)
        {
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
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

        private void onProfilePicButtonClick(object sender, RoutedEventArgs e)
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

            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            ConversationTableUtils.deleteAllConversations();
            MessagesTableUtils.deleteAllMessages();
            convMap.Clear();
            convMap2.Clear();

            App.ViewModel.MessageListPageCollection.Clear();
            emptyScreenImage.Visibility = Visibility.Visible;
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            progressBar.IsEnabled = false;
            App.MqttManagerInstance.connect();
        }

        #region Delete Account

        private void deleteAccount_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting account.", "Delete Account ?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
            {
                progress = new MyProgressIndicator();
            }

            progress.Show();
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || "fail" == (string)obj["stat"])
            {
                Debug.WriteLine("Delete Account", "Could not delete account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    progress.Hide();
                });
                return;
            }
            NetworkManager.turnOffSystem = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            appSettings.Clear();
            mPubSub.publish(HikePubSub.DELETE_ACCOUNT, null);
        }

        #endregion
        /* Start or continue the conversation*/
        private void selectUserBtn_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
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
            convMap.Remove(convObj.Msisdn); // removed entry from map for UI
            convMap2.Remove(convObj.Msisdn); // removed entry from map for DB
            App.ViewModel.MessageListPageCollection.Remove(convObj); // removed from observable collection
            if (App.ViewModel.MessageListPageCollection.Count == 0)
            {
                emptyScreenImage.Visibility = Visibility.Visible;
            }
            ConversationTableUtils.deleteConversation(convObj.Msisdn); // removed entry from conversation table
            MessagesTableUtils.deleteAllMessagesForMsisdn(convObj.Msisdn); //removed all chat messages for this msisdn
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
                    appBar.MenuItems.Add(delConvsMenu);
                if (appBar.MenuItems.Contains(delAccountMenu))
                    appBar.MenuItems.Remove(delAccountMenu);
            }
            else if (selectedIndex == 1)
            {
                if (appBar.MenuItems.Contains(delConvsMenu))
                    appBar.MenuItems.Remove(delConvsMenu);
                if (!appBar.MenuItems.Contains(delAccountMenu))
                    appBar.MenuItems.Add(delAccountMenu);
            }
        }

        #endregion

        #region PUBSUB

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type || HikePubSub.SEND_NEW_MSG == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                ConversationListObject mObj;
                bool isNewConversation = false;

                if (convMap.ContainsKey(convMessage.Msisdn))
                {
                    mObj = convMap[convMessage.Msisdn];
                    mObj.LastMessage = convMessage.Message;
                    mObj.TimeStamp = TimeUtils.getTimeString(convMessage.Timestamp);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        App.ViewModel.MessageListPageCollection.Remove(mObj);
                    });
                }
                else
                {
                    ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
                    mObj = new ConversationListObject(convMessage.Msisdn, contact == null ? null : contact.Name, convMessage.Message,
                    contact == null ? !convMessage.IsSms : contact.OnHike, TimeUtils.getTimeString(convMessage.Timestamp), convMessage.Timestamp);
                    convMap[convMessage.Msisdn] = mObj;
                    isNewConversation = true;
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if(emptyScreenImage.Visibility == Visibility.Visible)
                        emptyScreenImage.Visibility = Visibility.Collapsed;
                    App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                });
                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = isNewConversation;
                if (HikePubSub.SEND_NEW_MSG == type)
                    mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
            }
            else if (HikePubSub.MSG_READ == type)
            {
                string msisdn = (string)obj;
                try
                {
                    ConversationListObject convObj = convMap[msisdn];
                    convObj.MessageStatus = ConvMessage.State.RECEIVED_READ;
                    //TODO : update the UI here also.
                }
                catch (KeyNotFoundException)
                {
                }
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
                string msisdn = (string)obj;
                try
                {
                    ConversationListObject convObj = convMap[msisdn];
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        convObj.NotifyPropertyChanged("AvatarImage");
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
                    emptyScreenImage.Visibility = Visibility.Visible;
                    App.ViewModel.MessageListPageCollection.Clear();
                    progress.Hide();
                    NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
                });
            }
        }

        #endregion

    }
}