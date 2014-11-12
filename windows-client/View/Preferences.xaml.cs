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
using windows_client.Model;

namespace windows_client.View
{
    public partial class Preferences : PhoneApplicationPage
    {
        public Preferences()
        {
            InitializeComponent();
            initializeBaseOnState();
        }

        private void initializeBaseOnState()
        {
            bool isLocationEnabled = true;
            if (!App.appSettings.TryGetValue<bool>(App.USE_LOCATION_SETTING, out isLocationEnabled))
                isLocationEnabled = true;

            this.locationToggle.IsChecked = isLocationEnabled;
            this.locationToggle.Content = isLocationEnabled ? AppResources.On : AppResources.Off;

            List<string> listSettingsValue = new List<string>();
            //by default immediate is to be shown
            listSettingsValue.Add(AppResources.Settings_StatusUpdate_Immediate_Txt);

            byte firstSetting;
            if (App.appSettings.TryGetValue(App.STATUS_UPDATE_FIRST_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            if (App.appSettings.TryGetValue(App.STATUS_UPDATE_SECOND_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            bool value;

            if (!App.appSettings.TryGetValue(App.ENTER_TO_SEND, out value))
                value = true;
            enterToSendToggle.IsChecked = value;
            this.enterToSendToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!App.appSettings.TryGetValue(App.SEND_NUDGE, out value))
                value = true;
            nudgeSettingToggle.IsChecked = value;
            this.nudgeSettingToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!App.appSettings.TryGetValue(HikeConstants.BLACK_THEME, out value))
                value = false;
            blackSettingToggle.IsChecked = value;
            this.blackSettingToggle.Content = value ? AppResources.On : AppResources.Off;

        }

        private void locationToggle_Loaded(object sender, RoutedEventArgs e)
        {
            locationToggle.Loaded -= locationToggle_Loaded;
            locationToggle.Checked += locationToggle_Checked;
            locationToggle.Unchecked += locationToggle_Unchecked;
        }

        private void locationToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.locationToggle.Content = AppResources.On;
            App.appSettings.Remove(App.USE_LOCATION_SETTING);
            App.appSettings.Save();

            App.ViewModel.LoadCurrentLocation(); // load current location
        }

        private void locationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.locationToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.USE_LOCATION_SETTING, false);
            App.RemoveKeyFromAppSettings(HikeConstants.LOCATION_DEVICE_COORDINATE);
        }

        private void enterToSendToggle_Loaded(object sender, RoutedEventArgs e)
        {
            enterToSendToggle.Loaded -= enterToSendToggle_Loaded;
            enterToSendToggle.Checked += enterToSendToggle_Checked;
            enterToSendToggle.Unchecked += enterToSendToggle_Unchecked;
        }

        private void enterToSendToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.enterToSendToggle.Content = AppResources.On;
            App.RemoveKeyFromAppSettings(App.ENTER_TO_SEND);

            App.SendEnterToSendStatusToServer();
        }

        private void enterToSendToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.enterToSendToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.ENTER_TO_SEND, false);
            App.SendEnterToSendStatusToServer();
        }

        private void nudgeSettingsToggle_Loaded(object sender, RoutedEventArgs e)
        {
            nudgeSettingToggle.Loaded -= nudgeSettingsToggle_Loaded;
            nudgeSettingToggle.Checked += nudgeSettingToggle_Checked;
            nudgeSettingToggle.Unchecked += nudgeSettingToggle_UnChecked;
        }

        private void nudgeSettingToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.nudgeSettingToggle.Content = AppResources.On;
            App.RemoveKeyFromAppSettings(App.SEND_NUDGE);
        }

        private void nudgeSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.nudgeSettingToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.SEND_NUDGE, false);
        }

        private void blackSettingToggle_Loaded(object sender, RoutedEventArgs e)
        {
            blackSettingToggle.Loaded -= blackSettingToggle_Loaded;
            blackSettingToggle.Checked += blackSettingToggle_Checked;
            blackSettingToggle.Unchecked += blackSettingToggle_UnChecked;
        }

        bool _isPopUpDisplayed;

        private void blackSettingToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.blackSettingToggle.Content = AppResources.On;
            App.WriteToIsoStorageSettings(HikeConstants.BLACK_THEME, true);
            Analytics.SendAnalyticsEvent(HikeConstants.ST_CONFIG_EVENT, HikeConstants.DARK_MODE_CLICKED, 1);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

        private void blackSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.blackSettingToggle.Content = AppResources.Off;
            App.RemoveKeyFromAppSettings(HikeConstants.BLACK_THEME);
            Analytics.SendAnalyticsEvent(HikeConstants.ST_CONFIG_EVENT, HikeConstants.DARK_MODE_CLICKED, 0);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

    }
}