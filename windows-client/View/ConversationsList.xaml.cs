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
namespace windows_client.View
{
    public partial class ConversationsList : PhoneApplicationPage, HikePubSub.Listener
    {
        private HikePubSub mPubSub;
        private readonly IsolatedStorageSettings appSettings;
        private NLog.Logger logger;
        private static Dictionary<string, ConversationListObject> convMap; // this holds msisdn -> conversation mapping

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

            App.MqttManagerInstance.connect();
        }

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.MSG_READ, this);
            mPubSub.addListener(HikePubSub.SEND_NEW_MSG, this);
            mPubSub.addListener(HikePubSub.ICON_CHANGED, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);
        }

        private void LoadMessages()
        {
            List<Conversation> conversationList = ConversationTableUtils.getAllConversations();
            if (conversationList == null)
            {
                //mainBackImage.ImageSource = new BitmapImage(new Uri("images\\empty_messages_hike_logo.png", UriKind.Relative));
                return;
            }
            for (int i = 0; i < conversationList.Count; i++)
            {
                Conversation conv = conversationList[i];
                ConvMessage lastMessage = MessagesTableUtils.getLastMessageForMsisdn(conv.Msisdn); // why we are not getting only lastmsg as string 
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(conv.Msisdn);

                Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(conv.Msisdn);
                ConversationListObject mObj = new ConversationListObject(contact.Msisdn, contact.Name, lastMessage.Message, contact.OnHike,
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
            appSettings.Clear();
            UsersTableUtils.deleteAllContacts();
            ConversationTableUtils.deleteAllConversations();
            MessagesTableUtils.deleteAllMessages();
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

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type || HikePubSub.SEND_NEW_MSG == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                ConversationListObject mObj;
                bool isNewConversation = false;

                /*This is used to avoid cross thread invokation exception*/
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (convMap.ContainsKey(convMessage.Msisdn))
                    {
                        mObj = convMap[convMessage.Msisdn];
                        mObj.LastMessage = convMessage.Message;
                        mObj.TimeStamp = TimeUtils.getTimeString(convMessage.Timestamp);
                        App.ViewModel.MessageListPageCollection.Remove(mObj);
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
                    if (App.ViewModel.MessageListPageCollection == null)
                        App.ViewModel.MessageListPageCollection = new ObservableCollection<ConversationListObject>();
                    App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                    object[] vals = new object[2];
                    vals[0] = convMessage;
                    vals[1] = isNewConversation;
                    if (HikePubSub.SEND_NEW_MSG == type)
                        mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);

                });
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
                        convObj.NotifyPropertyChanged("MSISDN");
                    });
                }
                catch (KeyNotFoundException)
                {
                }
            }
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
            convMap.Remove(convObj.MSISDN); // removed entry from map
            App.ViewModel.MessageListPageCollection.Remove(convObj); // removed from observable collection
            ConversationTableUtils.deleteConversation(convObj.MSISDN); // removed entry from conversation table
            MessagesTableUtils.deleteAllMessagesForMsisdn(convObj.MSISDN); //removed all chat messages for this msisdn
        }
    }
}