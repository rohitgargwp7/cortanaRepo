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

namespace windows_client
{
    public partial class MessageList : PhoneApplicationPage, HikePubSub.Listener
    {
        private readonly IsolatedStorageSettings appSettings;
        private NLog.Logger logger;
        private static Dictionary<string, MessageListPage> convMap; // this holds msisdn -> conversation mapping

        public static Dictionary<string, MessageListPage> ConvMap
        {
            get
            {
                return convMap;
            }
        }
        public MessageList()
        {
            InitializeComponent();
            logger = NLog.LogManager.GetCurrentClassLogger();
            appSettings = App.appSettings;
            App.MqttManagerInstance.connect();

            convMap = new Dictionary<string, MessageListPage>();
            LoadMessages();
            this.myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;

            App.HikePubSubInstance.addListener(HikePubSub.TYPING_CONVERSATION, this);
            App.HikePubSubInstance.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
            App.HikePubSubInstance.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_FAILED, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.ICON_CHANGED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
        }

        private void LoadMessages()
        {
            List<Conversation> conversationList = ConversationTableUtils.getAllConversations();
            if (conversationList == null)
            {
                //mainBackImage.ImageSource = new BitmapImage(new Uri("images\\empty_messages_hike_logo.png", UriKind.Relative));
                return;
            }
            App.ViewModel.MessageListPageCollection = new ObservableCollection<MessageListPage>();

            for (int i = 0; i < conversationList.Count; i++)
            {
                Conversation conv = conversationList[i];
                ConvMessage lastMessage = MessagesTableUtils.getLastMessageForMsisdn(conv.Msisdn); // why we are not getting only lastmsg as string 
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(conv.Msisdn);
                MessageListPage mObj = new MessageListPage(conv.Msisdn, contact.Name, lastMessage.Message, contact.OnHike, TimeUtils.getRelativeTime(lastMessage.Timestamp));
                convMap.Add(conv.Msisdn, mObj);
                App.ViewModel.MessageListPageCollection.Add(mObj);
            }
        }


        private void btnGetSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageListPage obj = myListBox.SelectedItem as MessageListPage;
            if (obj == null)
                return;
            //this.myListBox.SelectedIndex;
            PhoneApplicationService.Current.State["messageListPageObject"] = obj;
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
            UsersTableUtils.deleteAllConversations();
            UsersTableUtils.deleteAllMessages();
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
            });
        }

        /* Start or continue the conversation*/
        private void startConversation_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type || HikePubSub.MESSAGE_SENT == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                MessageListPage mObj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (convMap.ContainsKey(convMessage.Msisdn))
                    {
                        mObj = convMap[convMessage.Msisdn];
                        mObj.LastMessage = convMessage.Message;
                        mObj.TimeStamp = TimeUtils.getRelativeTime(convMessage.Timestamp);

                        /*This is used to avoid cross thread invokation exception*/

                        App.ViewModel.MessageListPageCollection.Remove(mObj);
                        //App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                    }
                    else
                    {
                        ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
                        mObj = new MessageListPage(convMessage.Msisdn, contact == null ? convMessage.Msisdn : contact.Name, convMessage.Message, contact == null ? !convMessage.IsSms : contact.OnHike, TimeUtils.getRelativeTime(convMessage.Timestamp));
                    }
                    App.ViewModel.MessageListPageCollection.Insert(0, mObj);     
                });
            }
        }
    }
}