using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Languages;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using windows_client.Controls;
using windows_client.Model;

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage
    {
        public Privacy()
        {
            InitializeComponent();

            bool value = true;
            if (!App.appSettings.TryGetValue(HikeConstants.LAST_SEEN_SEETING, out value))
                value = true;
            lastSeenTimeStampToggle.IsChecked = value;
            this.lastSeenTimeStampToggle.Content = value ? AppResources.Favorites_Txt : AppResources.Nobody_Txt;

            // dont show reset and change password option if any tooltip is being shown on home screen
            if (App.appSettings.Contains(HikeConstants.HIDDEN_MODE_PASSWORD))
                hiddenModeGrid.Visibility = Visibility.Visible;

            value = App.appSettings.TryGetValue(HikeConstants.DISPLAY_PIC_FAV_ONLY, out value);
            profilePictureToggle.IsChecked = value;
            this.profilePictureToggle.Content = value ? AppResources.Favorites_Txt : AppResources.Everyone_Txt;

            value = App.appSettings.TryGetValue(HikeConstants.ACTIVATE_HIDDEN_MODE_ON_EXIT, out value);
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
            App.appSettings.Remove(HikeConstants.LAST_SEEN_SEETING);
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
            this.lastSeenTimeStampToggle.Content = AppResources.Nobody_Txt;
            App.WriteToIsoStorageSettings(HikeConstants.LAST_SEEN_SEETING, false);

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.LASTSEENONOFF, false);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
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
            App.WriteToIsoStorageSettings(HikeConstants.ACTIVATE_HIDDEN_MODE_ON_EXIT, true);
        }

        private void hideChatOnExitToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            this.activateHiddenModeOnExitToggle.Content = AppResources.Off;
            App.RemoveKeyFromAppSettings(HikeConstants.ACTIVATE_HIDDEN_MODE_ON_EXIT);
        }

        #region Hidden Mode Settings

        private void ResetHiddenMode_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.ANALYTICS_INIT_RESET_HIDDEN_MODE);

            App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_MODE_RESET_TIME, TimeUtils.getCurrentTimeStamp());
            App.ViewModel.ResetHiddenModeTapped();

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
            if (App.appSettings.TryGetValue(HikeConstants.HIDDEN_MODE_PASSWORD, out password))
            {
                App.ViewModel.Password = password;
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

                    if (App.ViewModel.Password == popup.Password)
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
                        Analytics.SendClickEvent(HikeConstants.ANALYTICS_PWD_CHANGE_HIDDEN_MODE);

                        _tempPassword = null;
                        App.ViewModel.Password = popup.Password;
                        App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_MODE_PASSWORD, App.ViewModel.Password);
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
            App.WriteToIsoStorageSettings(HikeConstants.DISPLAY_PIC_FAV_ONLY, true);

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.AVATAR, 2);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

        }

        private void profilePictureToggle_UnChecked(object sender, RoutedEventArgs e)
        {
            this.profilePictureToggle.Content = AppResources.Everyone_Txt;
            App.RemoveKeyFromAppSettings(HikeConstants.DISPLAY_PIC_FAV_ONLY);

            JObject obj = new JObject();
            obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
            JObject data = new JObject();
            data.Add(HikeConstants.AVATAR, 1);
            obj.Add(HikeConstants.DATA, data);
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        }
    }
}