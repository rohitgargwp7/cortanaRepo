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

            if (!HikeInstantiation.AppSettings.TryGetValue<bool>(HikeConstants.USE_LOCATION_SETTING, out isLocationEnabled))
                isLocationEnabled = true;

            this.locationToggle.IsChecked = isLocationEnabled;
            this.locationToggle.Content = isLocationEnabled ? AppResources.On : AppResources.Off;

            List<string> listSettingsValue = new List<string>();
            //by default immediate is to be shown
            listSettingsValue.Add(AppResources.Settings_StatusUpdate_Immediate_Txt);

            byte firstSetting;

            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.STATUS_UPDATE_FIRST_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            if (HikeInstantiation.AppSettings.TryGetValue(HikeConstants.STATUS_UPDATE_SECOND_SETTING, out firstSetting) && firstSetting > 0)
            {
                if (firstSetting == 1)
                    listSettingsValue.Add(AppResources.Settings_StatusUpdate_Every1Hour_txt);
                else
                    listSettingsValue.Add(string.Format(AppResources.Settings_StatusUpdate_EveryXHour_txt, firstSetting));
            }

            bool value;
            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AUTO_DOWNLOAD_SETTING, out value))
                value = true;
            autoDownloadToggle.IsChecked = value;
            this.autoDownloadToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.AUTO_RESUME_SETTING, out value))
                value = true;
            autoResumeToggle.IsChecked = value;
            this.autoResumeToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.ENTER_TO_SEND, out value))
                value = true;
            enterToSendToggle.IsChecked = value;
            this.enterToSendToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.SEND_NUDGE, out value))
                value = true;
            nudgeSettingToggle.IsChecked = value;
            this.nudgeSettingToggle.Content = value ? AppResources.On : AppResources.Off;

            if (!HikeInstantiation.AppSettings.TryGetValue(HikeConstants.BLACK_THEME, out value))
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
            HikeInstantiation.AppSettings.Remove(HikeConstants.USE_LOCATION_SETTING);
            HikeInstantiation.AppSettings.Save();

            HikeInstantiation.ViewModel.LoadCurrentLocation(); // load current location
        }

        private void locationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.locationToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.USE_LOCATION_SETTING, false);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.LOCATION_DEVICE_COORDINATE);
        }
        private void autoDownloadToggle_Loaded(object sender, RoutedEventArgs e)
        {
            autoDownloadToggle.Loaded -= autoDownloadToggle_Loaded;
            autoDownloadToggle.Checked += autoDownloadToggle_Checked;
            autoDownloadToggle.Unchecked += autoDownloadToggle_Unchecked;
        }
        private void autoDownloadToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.autoDownloadToggle.Content = AppResources.On;
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AUTO_DOWNLOAD_SETTING);
        }

        private void autoDownloadToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.autoDownloadToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AUTO_DOWNLOAD_SETTING, false);
        }

        private void autoUploadToggle_Loaded(object sender, RoutedEventArgs e)
        {
            autoResumeToggle.Loaded -= autoUploadToggle_Loaded;
            autoResumeToggle.Checked += autoResumeToggle_Checked;
            autoResumeToggle.Unchecked += autoResumeToggle_Unchecked;
        }

        private void autoResumeToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.autoResumeToggle.Content = AppResources.On;
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AUTO_RESUME_SETTING);
            FileTransfers.FileTransferManager.Instance.PopulatePreviousTasks();
        }

        private void autoResumeToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.autoResumeToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AUTO_RESUME_SETTING, false);
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
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.ENTER_TO_SEND);
            HikeInstantiation.SendEnterToSendStatusToServer();
        }

        private void enterToSendToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.enterToSendToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.ENTER_TO_SEND, false);
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
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.SEND_NUDGE);
        }

        private void nudgeSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.nudgeSettingToggle.Content = AppResources.Off;
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.SEND_NUDGE, false);
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
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.BLACK_THEME, true);
            Analytics.SendAnalyticsEvent(HikeConstants.ServerJsonKeys.ST_CONFIG_EVENT, HikeConstants.DARK_MODE_CLICKED, 1);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

        private void blackSettingToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.blackSettingToggle.Content = AppResources.Off;
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.BLACK_THEME);
            Analytics.SendAnalyticsEvent(HikeConstants.ServerJsonKeys.ST_CONFIG_EVENT, HikeConstants.DARK_MODE_CLICKED, 0);

            if (!_isPopUpDisplayed)
            {
                MessageBox.Show(AppResources.CloseApp_Txt, AppResources.RestartApp_Txt, MessageBoxButton.OK);
                _isPopUpDisplayed = true;
            }
        }

    }
}