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

namespace windows_client
{
    public partial class MessageList : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public MessageList()
        {
            InitializeComponent();
            LoadMessages();
            this.myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
        }

        private void LoadMessages()
        {
            List<Conversation> conversationList = ConversationDbUtils.getConversations();
            if (conversationList == null)
            {
                mainBackImage.ImageSource = new BitmapImage(new Uri("images\\empty_messages_hike_logo.png", UriKind.Relative));
                return;
            }
            App.ViewModel.MessageListPageCollection = new ObservableCollection<MessageListPage>();

            for (int i=0;i<conversationList.Count;i++) 
            {
                Conversation conversation = conversationList[i];
                ConvMessage lastMessage = UsersTableUtils.getLastMessageForMsisdn(conversation.Msisdn);
                ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(lastMessage.Msisdn);
                App.ViewModel.MessageListPageCollection.Add(new MessageListPage(contact.Name, lastMessage.Message, TimeUtils.getRelativeTime(lastMessage.Timestamp)));
            }
        }


        private void btnGetSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBoxItem selectedItem = this.myListBox.ItemContainerGenerator.ContainerFromItem(this.myListBox.SelectedItem) as ListBoxItem;
            //this.myListBox.SelectedIndex;
            string uri = "/View/ConversationPage.xaml?Index=";
            uri += this.myListBox.SelectedIndex;
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }

        private void deleteAccount_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure about deleting account.","Delete Account ?",MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        private void deleteAccountResponse_Callback(JObject obj )
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
            NavigationService.Navigate(new Uri("/View/ChatThread.xaml", UriKind.Relative));
        }

     
    }
}