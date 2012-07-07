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
        List<ContactInfo> allContactsList;
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
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(enterNameTxt.Text))
            {
                return;
            }
            string charsEnetered = enterNameTxt.Text.Trim().ToLower();

            if (charsEnetered.Length == 1)
            {
                allContactsList = UsersTableUtils.getContactInfoFromName(charsEnetered);
                contactsListBox.ItemsSource = allContactsList;
                return;
            }
            List<ContactInfo> contactsList = getContactInfoFromNameOrPhone(charsEnetered);
            if(contactsList == null || contactsList.Count == 0)
            {
                contactsListBox.ItemsSource = null;
                return;
            }
            contactsListBox.ItemsSource = contactsList;
        }

        private List<ContactInfo> getContactInfoFromNameOrPhone(string charsEnetered)
        {
            if (allContactsList == null || allContactsList.Count == 0)
                return null;
            List<ContactInfo> contactsList = new List<ContactInfo>();
            for (int i = 0; i < allContactsList.Count; i++)
            {
                if (allContactsList[i].Name.ToLower().Contains(charsEnetered) || allContactsList[i].Msisdn.Contains(charsEnetered) || allContactsList[i].PhoneNo.Contains(charsEnetered))
                {
                    contactsList.Add(allContactsList[i]);
                }
            }
            return contactsList;
        }

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null)
                return;
            PhoneApplicationService.Current.State["objFromSelectUserPage"] = contact;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}