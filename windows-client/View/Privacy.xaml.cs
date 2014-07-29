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

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage
    {
        public Privacy()
        {
            InitializeComponent();

            bool showlastSeen = true;
            if (!App.appSettings.TryGetValue(App.LAST_SEEN_SEETING, out showlastSeen))
                showlastSeen = true;
            lastSeenTimeStampToggle.IsChecked = showlastSeen;
            this.lastSeenTimeStampToggle.Content = showlastSeen ? AppResources.On : AppResources.Off;

            // dont show reset and change password option if any tooltip is being shown on home screen
            if (!App.appSettings.Contains(HikeConstants.HIDDEN_TOOLTIP_STATUS))
                hiddenModeGrid.Visibility = Visibility.Visible;
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

        private void ResetHiddenMode_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_MODE_RESET_TIME, TimeUtils.getCurrentTimeStamp());
            App.ViewModel.ResetHiddenModeTapped();

            while (NavigationService.BackStack.Count() > 1)
                NavigationService.RemoveBackEntry();

            NavigationService.GoBack();
        }

        bool _isChangePassword;
        bool _isConfirmPassword;
        string _password;
        string _tempPassword;

        private void ChangePassword_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (App.appSettings.TryGetValue(HikeConstants.HIDDEN_MODE_PASSWORD, out _password))
            {
                passwordOverlay.Text = AppResources.EnterPassword_Txt;
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
                if (_isChangePassword)
                {
                    _isChangePassword = false;
                    
                    if (_password == popup.Password)
                    {
                        popup.Text = AppResources.EnterNewPassword_Txt;
                        popup.Password = String.Empty;
                    }
                    else
                        popup.IsShow = false;
                }
                else if (_isConfirmPassword)
                {
                    if (_tempPassword.Equals(popup.Password))
                    {
                        _password = popup.Password;
                        App.WriteToIsoStorageSettings(HikeConstants.HIDDEN_MODE_PASSWORD, _password);
                    }

                    _isConfirmPassword = false;
                    _isChangePassword = false;
                    popup.IsShow = false;
                }
                else
                {
                    _isConfirmPassword = true;
                    _isChangePassword = false;
                    _tempPassword = popup.Password;
                    popup.Text = AppResources.ConfirmPassword_Txt;
                    popup.Password = String.Empty;
                }
            }
        }
    }
}