using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using System.Collections.ObjectModel;
using windows_client.utils;

using windows_client.DbUtils;
using windows_client.Model;
using Newtonsoft.Json.Linq;

namespace windows_client
{
    public partial class MessageList : PhoneApplicationPage
    {
        public MessageList()
        {
            InitializeComponent();
            LoadMessages();
            this.myListBox.ItemsSource = App.ViewModel.MessageListPageCollection;
        }

        private void LoadMessages()
        {
            List<Conversation> conversationList = HikeDbUtils.getConversations();

            App.ViewModel.MessageListPageCollection = new ObservableCollection<MessageListPage>();

            foreach (Conversation conversation in conversationList)
            {
                ConvMessage lastMessage = HikeDbUtils.getLastMessageForMsisdn(conversation.Msisdn);
                ContactInfo contact = HikeDbUtils.getContactInfoFromMSISDN(lastMessage.Msisdn);
                App.ViewModel.MessageListPageCollection.Add(
                    new MessageListPage(contact.Name, lastMessage.Message, TimeUtils.getRelativeTime(lastMessage.Timestamp)));
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
    }
}