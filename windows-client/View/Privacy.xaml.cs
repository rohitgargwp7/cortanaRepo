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

    }
    
}