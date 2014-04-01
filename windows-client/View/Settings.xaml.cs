﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.utils;
using System.Windows.Media.Imaging;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            accountImage.Source = new BitmapImage(new Uri("images/account_black.png", UriKind.Relative));
            blockListImage.Source = new BitmapImage(new Uri("images/block_list_icon_black.png", UriKind.Relative));
            preferencesImage.Source = new BitmapImage(new Uri("images/settings_icon_dark.png", UriKind.Relative));
            notificationsImage.Source = new BitmapImage(new Uri("images/notifications_black.png", UriKind.Relative));
        }

        private void Preferences_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.PREFERENCES);
            NavigationService.Navigate(new Uri("/View/Preferences.xaml", UriKind.Relative));
        }

        private void Notifications_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.NOTIFICATIONS);
            NavigationService.Navigate(new Uri("/View/Notifications.xaml", UriKind.Relative));
        }

        private void Account_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ACCOUNT);
            NavigationService.Navigate(new Uri("/View/Account.xaml", UriKind.Relative));
        }

        private void BlockList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.BLOCKLIST);
            NavigationService.Navigate(new Uri("/View/BlockListPage.xaml", UriKind.Relative));
        }
    }
}