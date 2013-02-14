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
using Microsoft.Phone.Notification;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using System.Net.NetworkInformation;

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            initializeBaseOnState();
        }

        private void initializeBaseOnState()
        {
            bool isPushEnabled = true;
            App.appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
            this.pushNotifications.IsChecked = isPushEnabled;
            if (isPushEnabled)
                this.pushNotifications.Content = AppResources.On;
            else
                this.pushNotifications.Content = AppResources.Off;
            
            bool isVibrateEnabled = true;
            App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
            this.vibrate.IsChecked = isVibrateEnabled;
            if (isVibrateEnabled)
                this.vibrate.Content = AppResources.On;
            else
                this.vibrate.Content = AppResources.Off;

            bool showFreeSMS = true;
            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out showFreeSMS);
            this.showFreeSMSToggle.IsChecked = showFreeSMS;
            if (showFreeSMS)
                this.showFreeSMSToggle.Content = AppResources.On;
            else
                this.showFreeSMSToggle.Content = AppResources.Off;
        }

        private void pushNotifications_Checked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED,true);
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                App.PUSH_REGISTERATION_PENDING = true;
                return;
            }
            App.PushHelperInstance.registerPushnotifications();
        }

        private void pushNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED,false);
            App.PushHelperInstance.closePushnotifications();

        }

        private void vibrate_Checked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, true);
        }

        private void vibrate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, false);
        }

        private void showFreeSMSToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.showFreeSMSToggle.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, true);
        }

        private void showFreeSMSToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.showFreeSMSToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, false);
        }
    }
}