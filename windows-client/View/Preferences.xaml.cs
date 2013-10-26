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
    public partial class Preferences : PhoneApplicationPage
    {
        bool showStatusUpdatesSettings = false;
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

            bool showFreeSMS = true;
            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out showFreeSMS);
            this.showFreeSMSToggle.IsChecked = showFreeSMS;
            if (showFreeSMS)
                this.showFreeSMSToggle.Content = AppResources.On;
            else
                this.showFreeSMSToggle.Content = AppResources.Off;

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

            bool showlastSeen = true;
            if (!App.appSettings.TryGetValue(App.LAST_SEEN_SEETING, out showlastSeen))
                showlastSeen = true;
            lastSeenTimeStampToggle.IsChecked = showlastSeen;
            this.lastSeenTimeStampToggle.Content = showlastSeen ? AppResources.On : AppResources.Off;

            bool autoDownload;
            if (!App.appSettings.TryGetValue(App.AUTO_DOWNLOAD_SETTING, out autoDownload))
                autoDownload = true;
            autoDownloadToggle.IsChecked = autoDownload;
            this.autoDownloadToggle.Content = autoDownload ? AppResources.On : AppResources.Off;

            bool autoUpload;
            if (!App.appSettings.TryGetValue(App.AUTO_UPLOAD_SETTING, out autoUpload))
                autoUpload = true;
            autoUploadToggle.IsChecked = autoUpload;
            this.autoUploadToggle.Content = autoUpload ? AppResources.On : AppResources.Off;
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

        private void lastSeenTimeStampToggle_Loaded(object sender, RoutedEventArgs e)
        {
            lastSeenTimeStampToggle.Loaded -= lastSeenTimeStampToggle_Loaded;
            lastSeenTimeStampToggle.Checked += lastSeenTimeStampToggle_Checked;
            lastSeenTimeStampToggle.Unchecked += lastSeenTimeStampToggle_Unchecked;
        }

        private void lastSeenTimeStampToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.lastSeenTimeStampToggle.Content = AppResources.On;
            App.appSettings.Remove(App.LAST_SEEN_SEETING);
            App.appSettings.Save();

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.LASTSEENONOFF, true);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void lastSeenTimeStampToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.lastSeenTimeStampToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.LAST_SEEN_SEETING, false);

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.LASTSEENONOFF, false);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
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

            App.appSettings.Remove(HikeConstants.LOCATION_DEVICE_COORDINATE);
            App.appSettings.Save();
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
            App.appSettings.Remove(App.AUTO_DOWNLOAD_SETTING);
            App.appSettings.Save();
        }

        private void autoDownloadToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.autoDownloadToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.AUTO_DOWNLOAD_SETTING, false);
            App.appSettings.Save();
        }

        private void autoUploadToggle_Loaded(object sender, RoutedEventArgs e)
        {
            autoUploadToggle.Loaded -= autoUploadToggle_Loaded;
            autoUploadToggle.Checked += autoUploadToggle_Checked;
            autoUploadToggle.Unchecked += autoUploadToggle_Unchecked;
        }

        private void autoUploadToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.autoUploadToggle.Content = AppResources.On;
            App.appSettings.Remove(App.AUTO_UPLOAD_SETTING);
            App.appSettings.Save();

            FileTransfers.FileUploader.Instance.StartUpload();
        }

        private void autoUploadToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.autoUploadToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.AUTO_UPLOAD_SETTING, false);
            App.appSettings.Save();
        }

        private async void btnGoToLockSettings_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }
    }
}