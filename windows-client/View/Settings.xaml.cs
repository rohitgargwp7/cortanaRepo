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
using windows_client.Languages;

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();

            int creditsRemaining = 0;
            App.appSettings.TryGetValue(App.SMS_SETTING, out creditsRemaining);
            smsCounterText.Text = String.Format(AppResources.Settings_SubtitleSMSSettings_Txt, creditsRemaining);
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

        //private void BlockList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    App.AnalyticsInstance.addEvent(Analytics.BLOCKLIST);
        //    NavigationService.Navigate(new Uri("/View/BlockListPage.xaml", UriKind.Relative));
        //}

        private void SMS_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/FreeSMS.xaml", UriKind.Relative));
        }

        private void Privacy_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //NavigationService.Navigate(new Uri("/View/Privacy.xaml", UriKind.Relative));
        }

        private void Sync_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void Help_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Help.xaml", UriKind.Relative));
        }
    }
}