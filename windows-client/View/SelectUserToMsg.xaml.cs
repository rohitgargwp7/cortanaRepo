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
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using System.Threading;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        private List<ConvMessage> messages;
        private string msisdn;
        private bool onHike;

        public SelectUserToMsg()
        {
            InitializeComponent();
            this.DataContext = App.ViewModel;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            /*IDictionary<string, string> parameters = this.NavigationContext.QueryString;
            if (parameters.ContainsKey("Index"))
            {
                int selectedIndex = Convert.ToInt32(NavigationContext.QueryString["Index"]);
                this.DataContext = App.ViewModel.MessageListPageCollection[selectedIndex];
            }*/
        }

        //loads pre-existing messages for msisdn, returns null if empty
        //sets on-hike status
        private void setConversationPage(string msisdn)
        {
            List<ConvMessage> messages = MessagesTableUtils.getMessagesForMsisdn(msisdn);
            onHike = UsersTableUtils.getContactInfoFromMSISDN(msisdn).OnHike;
        }

        private void deleteConversation(string msisdn)
        {
            UsersTableUtils.deleteConversation(msisdn);
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            string charsEnetered = enterNameTxt.Text;
            if (String.IsNullOrEmpty(charsEnetered))
            {
                contactsListBox.ItemsSource = null;
                return;
            }
            List<ContactInfo> contactsList = UsersTableUtils.getContactInfoFromName(charsEnetered);
            contactsListBox.ItemsSource = contactsList;
        }

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo obj = contactsListBox.SelectedItem as ContactInfo;
            if (obj == null)
                return;
            MessageListPage mObj = new MessageListPage();
            mObj.MSISDN = obj.Msisdn;
            if (App.ViewModel.MessageListPageCollection.Contains(mObj))
            {
                int idx = App.ViewModel.MessageListPageCollection.IndexOf(mObj);
                mObj = App.ViewModel.MessageListPageCollection[idx];
            }
            PhoneApplicationService.Current.State["messageListPageObject"] = mObj;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}