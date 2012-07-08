using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using windows_client.ViewModel;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.IO;
using System.Threading;
namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        #region CONSTANTS

        private readonly string DELETE_ACCOUNT = "Delete Account";
        private readonly string INVITE_USERS = "Invite Users";

        #endregion
        private HikePubSub mPubSub;
        private readonly IsolatedStorageSettings appSettings;
        private NLog.Logger logger;
        private static Dictionary<string, ConversationListObject> convMap; // this holds msisdn -> conversation mapping
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

        public ConversationsList()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;
            logger = NLog.LogManager.GetCurrentClassLogger();
            appSettings = App.appSettings;
            App.ViewModel.MessageListPageCollection = new ObservableCollection<ConversationListObject>();
            this.myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
            convMap = new Dictionary<string, ConversationListObject>();
            LoadMessages();
            registerListeners();
            initAppBar();
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
            menuItem1.Click += new EventHandler(inviteUsers_Click);
            appBar.MenuItems.Add(menuItem2);
            convListPage.ApplicationBar = appBar;
        }

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
            else
            {
                Uri uri = new Uri("/View/images/tux.png", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                avatarImage.Source = image;
                avatarImage.Height = 90;
                avatarImage.Width = 90;
            }
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

        private void LoadMessages()
        {
            List<Conversation> conversationList = ConversationTableUtils.getAllConversations();
            if (conversationList == null)
            {
                return;
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
                App.ViewModel.MessageListPageCollection.Add(mObj);
            }
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            App.MqttManagerInstance.connect();
        }

        private void deleteAccount_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting account.", "Delete Account ?", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || "fail" == (string)obj["stat"])
            {
                logger.Info("Delete Account", "Could not delete account !!");
                return;
            }

            removeListeners();
            appSettings.Clear();
            MiscDBUtil.clearDatabase();
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
            });
        }

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
            convMap.Remove(convObj.Msisdn); // removed entry from map
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

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PanoramaItem pItem = e.AddedItems[0] as PanoramaItem;
            var panorama = pItem.Parent as Panorama;
            var selectedIndex = panorama.SelectedIndex;
            if (selectedIndex == 0)
            {
                //appBar.IsVisible = true;
            }
            else if (selectedIndex == 1)
            {
                //appBar.IsVisible = false;
            }
        }

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

                    convMap.Add(convMessage.Msisdn, mObj);
                    isNewConversation = true;
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (App.ViewModel.MessageListPageCollection == null)
                        App.ViewModel.MessageListPageCollection = new ObservableCollection<ConversationListObject>();
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