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
    public partial class Notifications : PhoneApplicationPage
    {
        bool showStatusUpdatesSettings = false;
        public Notifications()
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

            bool isHikeJingleEnabled = true;
            App.appSettings.TryGetValue<bool>(App.HIKEJINGLE_PREF, out isHikeJingleEnabled);
            this.hikeJingle.IsChecked = isHikeJingleEnabled;
            if (isHikeJingleEnabled)
                this.hikeJingle.Content = AppResources.On;
            else
                this.hikeJingle.Content = AppResources.Off;

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

            byte statusSettingsValue;
            if (App.appSettings.TryGetValue(App.STATUS_UPDATE_SETTING, out statusSettingsValue))
            {
                if (statusSettingsValue > 0)
                {
                    statusUpdateNotificationToggle.IsChecked = true;
                    statusUpdateNotificationToggle.Content = AppResources.On;
                }
                else
                {
                    statusUpdateNotificationToggle.IsChecked = false;
                    statusUpdateNotificationToggle.Content = AppResources.Off;
                    listBoxStatusSettings.Visibility = Visibility.Collapsed;
                }
            }
            listBoxStatusSettings.ItemsSource = listSettingsValue;
            listBoxStatusSettings.SelectedIndex = statusSettingsValue == 0 ? 0 : statusSettingsValue - 1;
            if (listSettingsValue.Count > 1)
            {
                showStatusUpdatesSettings = true;
                listBoxStatusSettings.Visibility = Visibility.Visible;
            }

            byte chatThemePushValue;
            if (App.appSettings.TryGetValue(App.CHAT_THEME_SETTING, out chatThemePushValue))
            {
                if (chatThemePushValue > 0)
                {
                    chatThemeNotificationToggle.IsChecked = true;
                    chatThemeNotificationToggle.Content = AppResources.On;
                }
                else
                {
                    chatThemeNotificationToggle.IsChecked = false;
                    chatThemeNotificationToggle.Content = AppResources.Off;
                }
            }
        }

        private void pushNotifications_Checked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED, true);
            PushHelper.Instance.registerPushnotifications();
        }

        private void pushNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED, false);
            PushHelper.Instance.closePushnotifications();
        }

        private void vibrate_Checked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, true);
        }

        private void hikeJingle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.hikeJingle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.HIKEJINGLE_PREF, false);
        }
        private void hikeJingle_Checked(object sender, RoutedEventArgs e)
        {
            this.hikeJingle.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.HIKEJINGLE_PREF, true);
        }

        private void vibrate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, false);
        }
        private void statusUpdateNotification_Checked(object sender, RoutedEventArgs e)
        {
            this.statusUpdateNotificationToggle.Content = AppResources.On;
            if (showStatusUpdatesSettings)
                listBoxStatusSettings.Visibility = Visibility.Visible;
            App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)1);
            JObject obj = new JObject();

            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_SU, 0);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void statusUpdateNotification_Unchecked(object sender, RoutedEventArgs e)
        {
            this.statusUpdateNotificationToggle.Content = AppResources.Off;
            listBoxStatusSettings.Visibility = Visibility.Collapsed;
            listBoxStatusSettings.SelectedIndex = 0;
            App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)0);

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_SU, -1);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void lpkStatusSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SETTING, (byte)(listBoxStatusSettings.SelectedIndex + 1));

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_SU, listBoxStatusSettings.SelectedIndex);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void listBoxStatusSettings_Loaded(object sender, RoutedEventArgs e)
        {
            listBoxStatusSettings.Loaded -= listBoxStatusSettings_Loaded;
            listBoxStatusSettings.SelectionChanged += lpkStatusSettings_SelectionChanged;
        }

        private void statusUpdateNotificationToggle_Loaded(object sender, RoutedEventArgs e)
        {
            statusUpdateNotificationToggle.Loaded -= statusUpdateNotificationToggle_Loaded;
            statusUpdateNotificationToggle.Checked += statusUpdateNotification_Checked;
            statusUpdateNotificationToggle.Unchecked += statusUpdateNotification_Unchecked;
        }

        private void chatThemeNotificationToggle_Loaded(object sender, RoutedEventArgs e)
        {
            chatThemeNotificationToggle.Loaded -= chatThemeNotificationToggle_Loaded;
            chatThemeNotificationToggle.Checked += chatThemeNotificationToggle_Checked;
            chatThemeNotificationToggle.Unchecked += chatThemeNotificationToggle_Unchecked;
        }

        void chatThemeNotificationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.chatThemeNotificationToggle.Content = AppResources.Off;
            App.WriteToIsoStorageSettings(App.CHAT_THEME_SETTING, (byte)0);
            
            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_CBG, -1);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        void chatThemeNotificationToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.chatThemeNotificationToggle.Content = AppResources.On;
            App.WriteToIsoStorageSettings(App.CHAT_THEME_SETTING, (byte)1);
            
            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.PUSH_CBG, 0);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }
    }
}