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
using Microsoft.Phone.Tasks;

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            if (Utils.isDarkTheme())
            {
                accountImage.Source = new BitmapImage(new Uri("images/account_white.png", UriKind.Relative));
                blockListImage.Source = new BitmapImage(new Uri("images/block_list_icon_white.png", UriKind.Relative));
                preferencesImage.Source = new BitmapImage(new Uri("images/settings_icon_white.png", UriKind.Relative));
                notificationsImage.Source = new BitmapImage(new Uri("images/notifications_white.png", UriKind.Relative));
            }
            else
            {
                accountImage.Source = new BitmapImage(new Uri("images/account_black.png", UriKind.Relative));
                blockListImage.Source = new BitmapImage(new Uri("images/block_list_icon_black.png", UriKind.Relative));
                preferencesImage.Source = new BitmapImage(new Uri("images/settings_icon_dark.png", UriKind.Relative));
                notificationsImage.Source = new BitmapImage(new Uri("images/notifications_black.png", UriKind.Relative));
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

        private void ClearLogs_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Logging.LogWriter.Instance.ClearLogs();
            MQttLogging.LogWriter.Instance.ReadFile();
        }

        private void ViewLogs_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIEW_MORE_MESSAGE_OBJ] = Logging.LogWriter.Instance.ReadFile();
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void ViewMqtt_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.VIEW_MORE_MESSAGE_OBJ] = MQttLogging.LogWriter.Instance.ReadFile();
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void Email_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string text= MQttLogging.LogWriter.Instance.ReadFile();
            if (text.Length > 65535)
            {
                MessageBox.Show("Log file greater than max email size");
                return;
            }

            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "Mqtt logs";
            emailComposeTask.Body = text;

            emailComposeTask.Show();
        }

        private void EmailApp_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string text = Logging.LogWriter.Instance.ReadFile();
            if (text.Length > 65535)
            {
                MessageBox.Show("Log file greater than max email size");
                return;
            }

            EmailComposeTask emailComposeTask = new EmailComposeTask();

            emailComposeTask.Subject = "App logs";
            emailComposeTask.Body = text;

            emailComposeTask.Show();
        }
    }
}