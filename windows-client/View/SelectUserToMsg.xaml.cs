﻿using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;

namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        public static MyProgressIndicator progress = null;
        public static bool canGoBack = true;

        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 239, 239, 239));

        public SelectUserToMsg()
        {
            InitializeComponent();         
        }
        
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            contactsListBox.ItemsSource = App.ViewModel.allContactsList;
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            string charsEnetered = enterNameTxt.Text.ToLower();
            if (String.IsNullOrEmpty(charsEnetered))
            {
                contactsListBox.ItemsSource = App.ViewModel.allContactsList;
                return;
            }
            List<ContactInfo> contactsList = getContactInfoFromNameOrPhone(charsEnetered);
            if (contactsList == null || contactsList.Count == 0)
            {
                contactsListBox.ItemsSource = null;
                return;
            }
            contactsListBox.ItemsSource = contactsList;
        }

        private List<ContactInfo> getContactInfoFromNameOrPhone(string charsEnetered)
        {
            List<ContactInfo> contactsList = new List<ContactInfo>();
            for (int i = 0; i < App.ViewModel.allContactsList.Count; i++)
            {
                if (App.ViewModel.allContactsList[i].Name.ToLower().Contains(charsEnetered) || App.ViewModel.allContactsList[i].Msisdn.Contains(charsEnetered) || App.ViewModel.allContactsList[i].PhoneNo.Contains(charsEnetered))
                {
                    contactsList.Add(App.ViewModel.allContactsList[i]);
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

        private void refreshContacts_Click(object sender, EventArgs e)
        {
            if (progress == null)
            {
                progress = new MyProgressIndicator();
            }

            progress.Show();
            canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.makePatchRequest_Callback));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (canGoBack)
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            enterNameTxt.Background = textBoxBackground;

        }
    }
}