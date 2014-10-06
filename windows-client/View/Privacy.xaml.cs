﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using windows_client.Languages;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using windows_client.Controls;
using windows_client.Model;
using CommonLibrary.Constants;

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage
    {
        public Privacy()
        {
            InitializeComponent();

            bool value = true;
            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.LAST_SEEN_SEETING, out value))
                value = true;
            lastSeenTimeStampToggle.IsChecked = value;
            this.lastSeenTimeStampToggle.Content = value ? AppResources.Favorites_Txt : AppResources.Nobody_Txt;

            // dont show reset and change password option if any tooltip is being shown on home screen
            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.HIDDEN_MODE_PASSWORD))
                hiddenModeGrid.Visibility = Visibility.Visible;

            value = HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.DISPLAY_PIC_FAV_ONLY, out value);
            profilePictureToggle.IsChecked = value;
            this.profilePictureToggle.Content = value ? AppResources.Favorites_Txt : AppResources.Everyone_Txt;

            value = HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.ACTIVATE_HIDDEN_MODE_ON_EXIT, out value);
            activateHiddenModeOnExitToggle.IsChecked = value;
            this.activateHiddenModeOnExitToggle.Content = value ? AppResources.On : AppResources.Off;
        }

        private void BlockList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/BlockListPage.xaml", UriKind.Relative));
        }

        private void lastSeenTimeStampToggle_Loaded(object sender, RoutedEventArgs e)
        {
            lastSeenTimeStampToggle.Loaded -= lastSeenTimeStampToggle_Loaded;
            lastSeenTimeStampToggle.Checked += lastSeenTimeStampToggle_Checked;
            lastSeenTimeStampToggle.Unchecked += lastSeenTimeStampToggle_Unchecked;
        }

        private void lastSeenTimeStampToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.lastSeenTimeStampToggle.Content = AppResources.Favorites_Txt;
            HikeInstantiation.AppSettings.Remove(AppSettingsKeys.LAST_SEEN_SEETING);
            HikeInstantiation.AppSettings.Save();

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(ServerJsonKeys.LASTSEENONOFF, true);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void lastSeenTimeStampToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.lastSeenTimeStampToggle.Content = AppResources.Nobody_Txt;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.LAST_SEEN_SEETING, false);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(ServerJsonKeys.LASTSEENONOFF, false);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void hideChatOnExitToggle_Loaded(object sender, RoutedEventArgs e)
        {
            activateHiddenModeOnExitToggle.Loaded -= hideChatOnExitToggle_Loaded;
            activateHiddenModeOnExitToggle.Checked += hideChatOnExitToggle_Checked;
            activateHiddenModeOnExitToggle.Unchecked += hideChatOnExitToggle_Unchecked;
        }

        private void hideChatOnExitToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.activateHiddenModeOnExitToggle.Content = AppResources.On;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.ACTIVATE_HIDDEN_MODE_ON_EXIT, true);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.HIDDEN_AUTO_SWITCH, 1);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        private void hideChatOnExitToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.activateHiddenModeOnExitToggle.Content = AppResources.Off;
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.ACTIVATE_HIDDEN_MODE_ON_EXIT);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.HIDDEN_AUTO_SWITCH, 0);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }

        #region Hidden Mode Settings

        private void ResetHiddenMode_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.AnalyticsKeys.ANALYTICS_INIT_RESET_HIDDEN_MODE);

            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIDDEN_MODE_RESET_TIME, TimeUtils.getCurrentTimeStamp());
            HikeInstantiation.ViewModel.ResetHiddenModeTapped();

            while (NavigationService.BackStack.Count() > 1)
                NavigationService.RemoveBackEntry();

            NavigationService.GoBack();
        }

        bool _isChangePassword;
        bool _isConfirmPassword;
        string _tempPassword;

        private void passwordOverlay_PasswordOverlayVisibilityChanged(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                privacySettings.IsHitTestVisible = popup.IsShow ? false : true;
            }
        }

        private void ChangePassword_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string password;
            if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.HIDDEN_MODE_PASSWORD, out password))
            {
                HikeInstantiation.ViewModel.Password = password;
                passwordOverlay.Text = AppResources.Enter_Current_Pwd_Txt;
                passwordOverlay.Password = String.Empty;
                passwordOverlay.IsShow = true;
                _isConfirmPassword = false;
                _isChangePassword = true;
            }
        }

        private void passwordOverlay_PasswordEntered(object sender, EventArgs e)
        {
            var popup = sender as PasswordPopUpUC;
            if (popup != null)
            {
                // Enter old passowrd
                if (_isChangePassword)
                {
                    _isChangePassword = false;

                    if (HikeInstantiation.ViewModel.Password == popup.Password)
                    {
                        popup.Text = AppResources.EnterNewPassword_PasswordChange_Txt;
                        popup.Password = String.Empty;
                    }
                    else
                        popup.IsShow = false;
                }
                else if (_isConfirmPassword) // Confirm new password
                {
                    if (_tempPassword.Equals(popup.Password))
                    {
                        Analytics.SendClickEvent(HikeConstants.AnalyticsKeys.ANALYTICS_PWD_CHANGE_HIDDEN_MODE);

                        _tempPassword = null;
                        HikeInstantiation.ViewModel.Password = popup.Password;
                        HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIDDEN_MODE_PASSWORD, HikeInstantiation.ViewModel.Password);
                    }
                    else
                        MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.Password_Mismatch_Txt, MessageBoxButton.OK);

                    _isConfirmPassword = false;
                    _isChangePassword = false;
                    popup.IsShow = false;
                }
                else // Enter new password.
                {
                    _isConfirmPassword = true;
                    _isChangePassword = false;
                    _tempPassword = popup.Password;
                    popup.Text = AppResources.ConfirmPassword_Txt;
                    popup.Password = String.Empty;
                }
            }
        }

        #endregion

        private void profilePictureToggle_Loaded(object sender, RoutedEventArgs e)
        {
            profilePictureToggle.Loaded -= profilePictureToggle_Loaded;
            profilePictureToggle.Checked += profilePictureToggle_Checked;
            profilePictureToggle.Unchecked += profilePictureToggle_UnChecked;
        }

        private void profilePictureToggle_Checked(object sender, RoutedEventArgs e)
        {
            this.profilePictureToggle.Content = AppResources.Favorites_Txt;
            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.DISPLAY_PIC_FAV_ONLY, true);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.AVATAR, 2);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

        }

        private void profilePictureToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.profilePictureToggle.Content = AppResources.Everyone_Txt;
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.DISPLAY_PIC_FAV_ONLY);

            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.AVATAR, 1);
            obj.Add(ServerJsonKeys.DATA, data);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }
    }
}