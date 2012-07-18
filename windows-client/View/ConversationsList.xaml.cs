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
using WP7Contrib.Collections;
using System.Threading;
using System.ComponentModel;
using Clarity.Phone.Controls;
using Clarity.Phone.Controls.Animations;
using windows_client.ViewModel;
using windows_client.Mqtt;

namespace windows_client.View
{
    public partial class ConversationsList : AnimatedBasePage, HikePubSub.Listener
    {
        #region CONSTANTS

        private readonly string DELETE_ACCOUNT = "Delete Account";
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
            myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
            convMap = new Dictionary<string, ConversationListObject>();
            convMap2 = new Dictionary<string, bool>();
            LoadMessages();
            #region Load App level instances

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_LoadAppInstances);
            bw.RunWorkerAsync();

            #endregion
            AnimationContext = LayoutRoot;
            initAppBar();
            initProfilePage();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            App.MqttManagerInstance.connect();
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
                App.HikePubSubInstance = new HikePubSub(); // instantiate pubsub
                App.DbListener = new DbConversationListener();
                App.NetworkManagerInstance = NetworkManager.Instance;
                App.MqttManagerInstance = new HikeMqttManager();

                mPubSub = App.HikePubSubInstance;
                //Load Hike Contacts
                App.ViewModel.allContactsList = UsersTableUtils.getAllContacts();
                registerListeners();
            }
        }

        private static void LoadMessages()
        {
            List<Conversation> conversationList = ConversationTableUtils.getAllConversations();
            if (conversationList == null)
            {
                return;
            }
            if (convMap == null)
            {
                convMap = new Dictionary<string, ConversationListObject>();
                convMap2 = new Dictionary<string, bool>();
            }
            for (int i = 0; i < conversationList.Count; i++)
            {
                Conversation conv = conversationList[i];
                ConvMessage lastMessage = MessagesTableUtils.getLastMessageForMsisdn(conv.Msisdn); // why we are not getting only lastmsg as string 
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(conv.Msisdn);

                Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(conv.Msisdn);
                ConversationListObject mObj = new ConversationListObject((contact == null) ? conv.Msisdn : contact.Msisdn, (contact == null) ? conv.Msisdn : contact.Name, lastMessage.Message, (contact == null) ? conv.OnHike : contact.OnHike,
                    TimeUtils.getTimeString(lastMessage.Timestamp), thumbnail == null ? null : thumbnail.Avatar);
                convMap.Add(conv.Msisdn, mObj);
                convMap2.Add(conv.Msisdn, false);
                //Deployment.Current.Dispatcher.BeginInvoke(() =>
                //{
                    App.ViewModel.MessageListPageCollection.Add(mObj);
                //});
            }
        }

        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            /* Add icons */
            ApplicationBarIconButton composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/appbar.add.rest.png", UriKind.Relative);
            composeIconButton.Text = "compose";
            composeIconButton.Click += new EventHandler(selectUserBtn_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);

            /* Add Menu Items*/
            ApplicationBarMenuItem menuItem1 = new ApplicationBarMenuItem();
            menuItem1.Text = DELETE_ACCOUNT;
            menuItem1.Click += new EventHandler(deleteAccount_Click);
            appBar.MenuItems.Add(menuItem1);

            ApplicationBarMenuItem menuItem2 = new ApplicationBarMenuItem();
            menuItem2.Text = INVITE_USERS;
            menuItem2.Click += new EventHandler(inviteUsers_Click);
            appBar.MenuItems.Add(menuItem2);
            convListPagePivot.ApplicationBar = appBar;
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
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            MemoryStream msLargeImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
            MemoryStream msSmallImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msSmallImage, 35, 35, 0, 95);

            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(msSmallImage, new AccountUtils.postResponseFunction(updateProfile_Callback));

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
            App.MqttManagerInstance.disconnectFromBroker(false);
            removeListeners();
            appSettings.Clear();
            MiscDBUtil.clearDatabase();            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                progress.Hide();
                NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
            });
        }

        #endregion

        #region AppBar Button Events

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

        #endregion

        #region Transition Animations

        protected override Clarity.Phone.Controls.Animations.AnimatorHelperBase GetAnimation(AnimationType animationType, Uri toOrFrom)
        {

            //you could factor this into an intermediate base page to have soem other defaults
            //such as always continuuming to a pivot page or rultes based on the page, direction and where you are goign to/coming from

            if (toOrFrom != null)
            {
                if (toOrFrom.OriginalString.Contains("SelectUserToMsg.xaml"))
                {
                    return null;
                }
                if (animationType == AnimationType.NavigateForwardOut)
                    return new TurnstileFeatherForwardOutAnimator() { ListBox = myListBox, RootElement = LayoutRoot };
                else
                    return new TurnstileFeatherBackwardInAnimator() { ListBox = myListBox, RootElement = LayoutRoot };
            }

            return base.GetAnimation(animationType, toOrFrom);
        }

        protected override void AnimationsComplete(AnimationType animationType)
        {
            switch (animationType)
            {
                case AnimationType.NavigateForwardIn:
                    //Add code to set data context and bind data
                    //you really only need to do that on forward in. on backward in everything
                    //will be there that existed on forward out
                    break;

                case AnimationType.NavigateBackwardIn:
                    //reset list so you can select the same element again
                    myListBox.SelectedIndex = -1;
                    break;
            }


            base.AnimationsComplete(animationType);
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

                /*This is used to avoid cross thread invokation exception*/
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
                    Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(convMessage.Msisdn);
                    mObj = new ConversationListObject(convMessage.Msisdn, contact == null ? convMessage.Msisdn : contact.Name, convMessage.Message,
                    contact == null ? !convMessage.IsSms : contact.OnHike, TimeUtils.getTimeString(convMessage.Timestamp),
                    thumbnail == null ? null : thumbnail.Avatar);
                    convMap[convMessage.Msisdn] = mObj;
                    isNewConversation = true;
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
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
                        convObj.NotifyPropertyChanged("Msisdn");
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
        }

        #endregion

    }
}