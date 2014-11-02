using System.Collections.Generic;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using windows_client.Languages;
using windows_client.Model;
using FileTransfer;
using CommonLibrary.Constants;

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

            if (!HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.USE_LOCATION_SETTING, out isLocationEnabled))
                isLocationEnabled = true;

            this.locationToggle.IsChecked = isLocationEnabled;
            this.locationToggle.Content = isLocationEnabled ? AppResources.On : AppResources.Off;

            List<string> listSettingsValue = new List<string>();
            //by default immediate is to be shown
            listSettingsValue.Add(AppResources.Settings_StatusUpdate_Immediate_Txt);

            byte firstSetting;

            if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.STATUS_UPDATE_FIRST_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.STATUS_UPDATE_SECOND_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            bool value;

            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.ENTER_TO_SEND, out value))
                value = true;
            enterToSendToggle.IsChecked = value;
            this.enterToSendToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.SEND_NUDGE, out value))
                value = true;
            nudgeSettingToggle.IsChecked = value;
            this.nudgeSettingToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.BLACK_THEME, out value))
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
            HikeInstantiation.AppSettings.Remove(AppSettingsKeys.USE_LOCATION_SETTING);
            HikeInstantiation.AppSettings.Save();

            HikeInstantiation.ViewModel.LoadCurrentLocation(); // load current location
        }

        private void locationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.locationToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.USE_LOCATION_SETTING, false);
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.LOCATION_DEVICE_COORDINATE);
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
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.ENTER_TO_SEND);
            HikeInstantiation.SendEnterToSendStatusToServer();
        }

        private void enterToSendToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.enterToSendToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.ENTER_TO_SEND, false);
            HikeInstantiation.SendEnterToSendStatusToServer();
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
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.SEND_NUDGE);
        }

        private void nudgeSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.nudgeSettingToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.SEND_NUDGE, false);
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
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.BLACK_THEME, true);
            Analytics.SendAnalyticsEvent(ServerJsonKeys.ST_CONFIG_EVENT, AnalyticsKeys.DARK_MODE_CLICKED, 1);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

        private void blackSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.blackSettingToggle.Content = AppResources.Off;
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.BLACK_THEME);
            Analytics.SendAnalyticsEvent(ServerJsonKeys.ST_CONFIG_EVENT, AnalyticsKeys.DARK_MODE_CLICKED, 0);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

    }
}