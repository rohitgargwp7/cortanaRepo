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
            bool isPushEnabled = true;
            App.appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
            this.pushNotifications.IsChecked = isPushEnabled;
            if (isPushEnabled)
                this.pushNotifications.Content = "On";
            else
                this.pushNotifications.Content = "Off";
        }

        private void pushNotifications_Checked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = "On";
        }

        private void pushNotifications_Unchecked(object sender, RoutedEventArgs e)
        {
            this.pushNotifications.Content = "Off";

        }

        public void postPushNotification_Callback(JObject obj)
        {
        }

        public void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            AccountUtils.postPushNotification(e.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }

        public void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
                    );
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            bool isPushEnabled = (bool)this.pushNotifications.IsChecked;

            App.appSettings[App.IS_PUSH_ENABLED] = isPushEnabled;
            App.appSettings.Save();
            try
            {

                HttpNotificationChannel pushChannel;
                pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
                if (pushChannel != null)
                {
                    if (isPushEnabled)
                    {
                        // The channel was already open, so just register for all the events.
                        pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                        pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                        // Register for this notification only if you need to receive the notifications while your application is running.
                        //pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);
                        if (pushChannel.ChannelUri == null)
                            return;
                        if (!pushChannel.IsShellTileBound)
                            pushChannel.BindToShellTile();
                        if (!pushChannel.IsShellToastBound)
                            pushChannel.BindToShellToast();

                        System.Diagnostics.Debug.WriteLine(pushChannel.ChannelUri.ToString());
                        AccountUtils.postPushNotification(pushChannel.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));

                    }
                    else
                    {
                        if (pushChannel.IsShellTileBound)
                            pushChannel.UnbindToShellTile();
                        if (pushChannel.IsShellToastBound)
                            pushChannel.UnbindToShellToast();
                    }
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
            }
        }

    }
}