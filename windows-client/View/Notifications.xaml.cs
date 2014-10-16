using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using System.Net.NetworkInformation;
using windows_client.Controls;
using CommonLibrary.Constants;

namespace windows_client.View
{
    public partial class Notifications : PhoneApplicationPage
    {
        bool showStatusUpdatesSettings = false;
        private bool _canGoBack = true;
        private ProgressIndicatorControl _progressIndicator;

        public Notifications()
        {
            InitializeComponent();
            initializeBaseOnState();
        }

        private void initializeBaseOnState()
        {
            bool isPushEnabled = true;

            HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.IS_PUSH_ENABLED, out isPushEnabled);
            this.pushNotifications.IsChecked = isPushEnabled;
            if (isPushEnabled)
                this.pushNotifications.Content = AppResources.On;
            else
                this.pushNotifications.Content = AppResources.Off;

            bool isVibrateEnabled = true;

            HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.VIBRATE_PREF, out isVibrateEnabled);
            this.vibrate.IsChecked = isVibrateEnabled;
            if (isVibrateEnabled)
                this.vibrate.Content = AppResources.On;
            else
                this.vibrate.Content = AppResources.Off;

            bool isHikeJingleEnabled = true;

            HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.HIKEJINGLE_PREF, out isHikeJingleEnabled);
            this.hikeJingle.IsChecked = isHikeJingleEnabled;
            if (isHikeJingleEnabled)
                this.hikeJingle.Content = AppResources.On;
            else
                this.hikeJingle.Content = AppResources.Off;

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

            byte statusSettingsValue;

            if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.STATUS_UPDATE_SETTING, out statusSettingsValue))
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

            bool hideMessagePreview = true;

            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.HIDE_MESSAGE_PREVIEW_SETTING, out hideMessagePreview))
                hideMessagePreview = true;

            hideMessageToggle.IsChecked = hideMessagePreview;
            this.hideMessageToggle.Content = hideMessagePreview ? AppResources.On : AppResources.Off;

            bool contactJoiningNotification = true;
            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.CONTACT_JOINING_NOTIFICATION_SETTING, out contactJoiningNotification))
                contactJoiningNotification = true;

            contactJoiningNotificationToggle.IsChecked = contactJoiningNotification;
            this.contactJoiningNotificationToggle.Content = contactJoiningNotification ? AppResources.On : AppResources.Off;

        }

        private void pushNotifications_Checked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.On;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.IS_PUSH_ENABLED, true);
            PushHelper.Instance.registerPushnotifications(false);
            VoipBackgroundAgentHelper.InitVoipBackgroundAgent();
        }

        private void pushNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = AppResources.Off;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.IS_PUSH_ENABLED, false);
            VoipBackgroundAgentHelper.UnsubscibeVoipBackgroundAgent();
            PushHelper.Instance.closePushnotifications();
        }

        private void vibrate_Checked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.On;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.VIBRATE_PREF, true);
        }

        private void hikeJingle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.hikeJingle.Content = AppResources.Off;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIKEJINGLE_PREF, false);
        }
        private void hikeJingle_Checked(object sender, RoutedEventArgs e)
        {
            this.hikeJingle.Content = AppResources.On;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIKEJINGLE_PREF, true);
        }

        private void vibrate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = AppResources.Off;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.VIBRATE_PREF, false);
        }
        private void statusUpdateNotification_Checked(object sender, RoutedEventArgs e)
        {
            this.statusUpdateNotificationToggle.Content = AppResources.On;
            if (showStatusUpdatesSettings)
                listBoxStatusSettings.Visibility = Visibility.Visible;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.STATUS_UPDATE_SETTING, (byte)1);
            JObject obj = new JObject();

            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(ServerJsonKeys.PUSH_SU, 0);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

            HikeInstantiation.ViewModel.StatusNotificationSettingsChanged();
        }

        private void statusUpdateNotification_Unchecked(object sender, RoutedEventArgs e)
        {
            this.statusUpdateNotificationToggle.Content = AppResources.Off;
            listBoxStatusSettings.Visibility = Visibility.Collapsed;
            listBoxStatusSettings.SelectedIndex = 0;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.STATUS_UPDATE_SETTING, (byte)0);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(ServerJsonKeys.PUSH_SU, -1);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

            HikeInstantiation.ViewModel.StatusNotificationSettingsChanged();
        }

        private void lpkStatusSettings_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.STATUS_UPDATE_SETTING, (byte)(listBoxStatusSettings.SelectedIndex + 1));

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(ServerJsonKeys.PUSH_SU, listBoxStatusSettings.SelectedIndex);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
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

        private async void btnGoToLockSettings_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Launch URI for the lock screen settings screen.
            var op = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private void hideMessageToggle_Loaded(object sender, RoutedEventArgs e)
        {
            hideMessageToggle.Loaded -= hideMessageToggle_Loaded;
            hideMessageToggle.Click+=hideMessageToggle_Click;
        }

        private void hideMessageToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleSwitch hideMessageToggle = sender as ToggleSwitch;
            string pushToken = String.Empty;
            bool currentStatus = (bool)hideMessageToggle.IsChecked;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                preventCheckedState(currentStatus);
                return;
            }

            LayoutRoot.IsHitTestVisible = false;
            
            if (_progressIndicator == null)
                _progressIndicator = new ProgressIndicatorControl();

            if (currentStatus == true)
                _progressIndicator.Show(LayoutRoot, AppResources.Turning_Message_Preview_On);
            else
                _progressIndicator.Show(LayoutRoot, AppResources.Turning_Message_Preview_Off);

            _canGoBack = false;


            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.LATEST_PUSH_TOKEN))  // added check if there is no push token
                pushToken = (string)HikeInstantiation.AppSettings[AppSettingsKeys.LATEST_PUSH_TOKEN];

            AccountUtils.postHideMessagePreview(pushToken, currentStatus, new AccountUtils.parametrisedPostResponseFunction(postHideMessagePreview_Callback), currentStatus);   
        }

        public void postHideMessagePreview_Callback(JObject obj,Object currentStatus)
        {
            bool currentlyChecked = (bool)currentStatus;
            string stat = "";
            string message = "";
            
            if (obj!=null)
            {
                JToken statusToken;
                obj.TryGetValue(ServerJsonKeys.STAT, out statusToken);
                if (statusToken != null)
                    stat = statusToken.ToString();
            }

            if (stat != ServerJsonKeys.OK)
            {
                message = AppResources.Oops_Something_Wrong_Txt;
                preventCheckedState(currentlyChecked);
                HideOverLay(message);
            }
            else
            {
                if (!currentlyChecked)
                {

                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIDE_MESSAGE_PREVIEW_SETTING, false);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hideMessageToggle.Content = AppResources.Off;
                    });
                }
                else
                {

                    HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.HIDE_MESSAGE_PREVIEW_SETTING);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hideMessageToggle.Content = AppResources.On;
                    });   
                }

                HideOverLay(String.Empty);
            }
        }

        private void contactJoiningNotificationToggle_Loaded(object sender, RoutedEventArgs e)
        {
            contactJoiningNotificationToggle.Loaded -= contactJoiningNotificationToggle_Loaded;
            contactJoiningNotificationToggle.Checked += contactJoiningNotificationToggle_Checked;
            contactJoiningNotificationToggle.Unchecked += contactJoiningNotificationToggle_Unchecked;
        }

        private void contactJoiningNotificationToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.contactJoiningNotificationToggle.Content = AppResources.On;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.CONTACT_JOINING_NOTIFICATION_SETTING, true);
            JObject obj = new JObject();

            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.USER_JOINING_NOTIF, 1);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

            HikeInstantiation.ViewModel.StatusNotificationSettingsChanged();
        }

        private void contactJoiningNotificationToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.contactJoiningNotificationToggle.Content = AppResources.Off;

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.CONTACT_JOINING_NOTIFICATION_SETTING, false);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.USER_JOINING_NOTIF, 0);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

            HikeInstantiation.ViewModel.StatusNotificationSettingsChanged();
        }

        

        public void postAccountJoiningNotification_Callback(JObject obj, Object currentStatus)
        {
            bool currentlyChecked = (bool)currentStatus;
            string stat = "";
            string message = "";

            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(ServerJsonKeys.STAT, out statusToken);
                if (statusToken != null)
                    stat = statusToken.ToString();
            }

            if (stat != ServerJsonKeys.OK)
            {
                message = AppResources.Oops_Something_Wrong_Txt;
                preventCheckedState(currentlyChecked);
                HideOverLay(message);
            }
            else
            {
                if (!currentlyChecked)
                {
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.CONTACT_JOINING_NOTIFICATION_SETTING, false);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        contactJoiningNotificationToggle.Content = AppResources.Off;
                    });
                }
                else
                {
                    HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.CONTACT_JOINING_NOTIFICATION_SETTING);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        contactJoiningNotificationToggle.Content = AppResources.On;
                    });
                }

                HideOverLay(String.Empty);
            }
        }

        void preventCheckedState(bool currentlyChecked)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (currentlyChecked)
                {
                    hideMessageToggle.IsChecked = false;
                    hideMessageToggle.Content = AppResources.Off;
                }
                else
                {
                    hideMessageToggle.IsChecked = true;
                    hideMessageToggle.Content = AppResources.On;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        void HideOverLay(string message)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                LayoutRoot.IsHitTestVisible = true;
                _progressIndicator.Hide(LayoutRoot);

                if (!String.IsNullOrEmpty(message))
                    MessageBox.Show(message,AppResources.Please_Try_Again_Txt,MessageBoxButton.OK);
                
                _canGoBack = true;
            });
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!_canGoBack)
            {
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

    }
    
}