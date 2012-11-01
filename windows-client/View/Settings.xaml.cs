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

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
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
                this.pushNotifications.Content = "On";
            else
                this.pushNotifications.Content = "Off";
            
            bool isVibrateEnabled = true;
            App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
            this.vibrate.IsChecked = isVibrateEnabled;
            if (isVibrateEnabled)
                this.vibrate.Content = "On";
            else
                this.vibrate.Content = "Off";

        }

        private void pushNotifications_Checked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = "On";
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED,true);

            try
            {
                HttpNotificationChannel pushChannel;

                pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);

                if (pushChannel != null)
                {
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                }
                else
                {
                    bool secure_push = false;
                    if (App.appSettings.TryGetValue(HikeConstants.SECURE_PUSH, out secure_push) && secure_push)
                    {
                        pushChannel = new HttpNotificationChannel(HikeConstants.pushNotificationChannelName, HikeConstants.PUSH_CHANNEL_CN);
                        App.WriteToIsoStorageSettings(HikeConstants.IS_SECURE_CHANNEL, true);
                    }
                    else
                    {
                        pushChannel = new HttpNotificationChannel(HikeConstants.pushNotificationChannelName);
                        App.WriteToIsoStorageSettings(HikeConstants.IS_SECURE_CHANNEL, false);
                    }

                    pushChannel = new HttpNotificationChannel(HikeConstants.pushNotificationChannelName, HikeConstants.PUSH_CHANNEL_CN);
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                    pushChannel.Open();
                }
                if (!pushChannel.IsShellTileBound)
                    pushChannel.BindToShellTile();
                if (!pushChannel.IsShellToastBound)
                    pushChannel.BindToShellToast();

                if (pushChannel.ChannelUri == null)
                    return;

                System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                AccountUtils.postPushNotification(pushChannel.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
            }
        }

        private void pushNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = "Off";
            App.WriteToIsoStorageSettings(App.IS_PUSH_ENABLED,false);

            try
            {
                HttpNotificationChannel pushChannel;
                pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
                if (pushChannel != null)
                {
                    if (pushChannel.IsShellTileBound)
                        pushChannel.UnbindToShellTile();
                    if (pushChannel.IsShellToastBound)
                        pushChannel.UnbindToShellToast();
                    pushChannel.Close();
                    App.WriteToIsoStorageSettings(HikeConstants.IS_SECURE_CHANNEL, false);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
            }

        }

        public void postPushNotification_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue("stat", out statusToken);
                stat = statusToken.ToString();
            }
            if (stat != "ok")
            {
                try
                {
                    HttpNotificationChannel pushChannel;
                    pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
                    if (pushChannel != null)
                    {
                        if (pushChannel.IsShellTileBound)
                            pushChannel.UnbindToShellTile();
                        if (pushChannel.IsShellToastBound)
                            pushChannel.UnbindToShellToast();
                        pushChannel.Close();
                    }
                }
                catch (Exception)
                { }
            }
        }

        public void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            if (e.ChannelUri != null)
            {
                AccountUtils.postPushNotification(e.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
            }
        }

        public void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic
            //Dispatcher.BeginInvoke(() =>
            //    MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
            //        e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
            //        );
        }

        private void vibrate_Checked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = "On";
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, true);
        }

        private void vibrate_Unchecked(object sender, RoutedEventArgs e)
        {
            this.vibrate.Content = "Off";
            App.WriteToIsoStorageSettings(App.VIBRATE_PREF, false);
        }
    }
}