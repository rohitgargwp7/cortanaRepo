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
using windows_client.Controls;
using System.Net.NetworkInformation;
using windows_client.utils;

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage
    {
        private bool _canGoBack = true;
        private ProgressIndicatorControl _progressIndicator;

        public Privacy()
        {
            InitializeComponent();

            bool showlastSeen = true;
            if (!App.appSettings.TryGetValue(App.LAST_SEEN_SEETING, out showlastSeen))
                showlastSeen = true;
            lastSeenTimeStampToggle.IsChecked = showlastSeen;
            this.lastSeenTimeStampToggle.Content = showlastSeen ? AppResources.On : AppResources.Off;

            bool value = false;
            if (App.appSettings.Contains(App.HIDE_MESSAGE_PREVIEW_SETTING))
                value = true;

            hideMessageToggle.IsChecked = value;
            this.hideMessageToggle.Content = value ? AppResources.On : AppResources.Off;
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

        private void hideMessageToggle_Loaded(object sender, RoutedEventArgs e)
        {
            hideMessageToggle.Loaded -= hideMessageToggle_Loaded;
            hideMessageToggle.Click+=hideMessageToggle_Click;
        }

        private void hideMessageToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleSwitch hideMessageToggle = sender as ToggleSwitch;

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
            {
                _progressIndicator.Show(LayoutRoot, "Hiding ;)");
            }
            else
            {
                _progressIndicator.Show(LayoutRoot, "Removing Hiding");
            }

            _canGoBack = false;
            AccountUtils.postHideMessagePreview((string)App.appSettings[App.LATEST_PUSH_TOKEN], currentStatus, new AccountUtils.parametrisedPostResponseFunction(postHideMessagePreview_Callback), currentStatus);   
        }

        public void postHideMessagePreview_Callback(JObject obj,Object curr_status)
        {
            bool currently_checked = (bool)curr_status;

            string stat = "";
            string message = "";
            if (obj!=null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                if (statusToken != null)
                    stat = statusToken.ToString();
            }

            if (stat != HikeConstants.OK)
            {
                message = "error";
                preventCheckedState(currently_checked);
                HideOverLay(message);
            }
            else
            {
                
                if (!currently_checked)
                {
                    App.RemoveKeyFromAppSettings(App.HIDE_MESSAGE_PREVIEW_SETTING);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hideMessageToggle.Content = AppResources.Off;
                    });

                    
                }
                else
                {
                    App.WriteToIsoStorageSettings(App.HIDE_MESSAGE_PREVIEW_SETTING, true);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hideMessageToggle.Content = AppResources.On;
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
                    MessageBox.Show(message);
                
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