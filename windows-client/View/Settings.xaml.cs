using System;
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
            if (Utils.isDarkTheme())
            {
                privacyImage.Source = new BitmapImage(new Uri("images/privacy_white.png", UriKind.Relative));
                blockListImage.Source = new BitmapImage(new Uri("images/block_list_icon_white.png", UriKind.Relative));
                settingsImage.Source = new BitmapImage(new Uri("images/notification_white.png", UriKind.Relative));
                notificationsImage.Source = new BitmapImage(new Uri("images/notification_white.png", UriKind.Relative));
            }
            else
            {
                privacyImage.Source = new BitmapImage(new Uri("images/privacy_black.png", UriKind.Relative));
                blockListImage.Source = new BitmapImage(new Uri("images/block_list_icon.png", UriKind.Relative));
                settingsImage.Source = new BitmapImage(new Uri("images/notification_black.png", UriKind.Relative));
                notificationsImage.Source = new BitmapImage(new Uri("images/notification_black.png", UriKind.Relative));
            }
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