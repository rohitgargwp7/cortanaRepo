using System;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Net.NetworkInformation;

namespace windows_client.utils
{
    public class PushHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile PushHelper instance = null;
        private string latestPushToken;
        private readonly int pollingTime = 5; //in seconds
        private DispatcherTimer dispatcherTimer;

        public static PushHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new PushHelper();
                        }
                    }
                }
                return instance;
            }
        }

        private PushHelper()
        {
        }

        public void closePushnotifications()
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
                    pushChannel.Dispose();
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
            }
        }


        public void registerPushnotifications()
        {
            HttpNotificationChannel pushChannel;
            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
            try
            {
                // If the channel was not found, then create a new connection to the push service.
                if (pushChannel == null)
                {
                    pushChannel = new HttpNotificationChannel(HikeConstants.pushNotificationChannelName, HikeConstants.PUSH_CHANNEL_CN);

                    // Register for all the events before attempting to open the channel.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                    // Register for this notification only if you need to receive the notifications while your application is running.
                    //pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);
                    pushChannel.Open();
                    // Bind this new channel for toast events.
                    pushChannel.BindToShellToast();
                    pushChannel.BindToShellTile();

                }
                else
                {
                    // The channel was already open, so just register for all the events.
                    pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                    pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                    // Register for this notification only if you need to receive the notifications while your application is running.
                    //pushChannel.ShellToastNotificationReceived += new EventHandler<NotificationEventArgs>(PushChannel_ShellToastNotificationReceived);

                    if (pushChannel.ChannelUri != null)
                    {
                        latestPushToken = pushChannel.ChannelUri.ToString();
                        Debug.WriteLine(latestPushToken);
                        AccountUtils.postPushNotification(latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
                    }
                }
            }
            catch (InvalidOperationException ioe)
            {
                Debug.WriteLine("PUSH Exception :: " + ioe.StackTrace);
            }
            catch (Exception ee)
            {
                Debug.WriteLine("PUSH Exception :: " + ee.StackTrace);
            }
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            if (e.ChannelUri != null)
            {
                latestPushToken = e.ChannelUri.ToString();
                AccountUtils.postPushNotification(latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
            }
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic
            //Dispatcher.BeginInvoke(() =>
            //    MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
            //        e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
            //        );
        }

        public void postPushNotification_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                stat = statusToken.ToString();
            }
            if (stat != HikeConstants.OK && NetworkInterface.GetIsNetworkAvailable())
            {
                if (dispatcherTimer == null)
                {
                    dispatcherTimer = new DispatcherTimer();
                    dispatcherTimer.Tick += postTokenToServer;
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(pollingTime);
                }
                if (!dispatcherTimer.IsEnabled) //ideally we don't need two separate if blocks. added for more safety
                    dispatcherTimer.Start();
            }
            else if (stat == HikeConstants.OK && dispatcherTimer != null)
            {
                if (dispatcherTimer.IsEnabled)
                    dispatcherTimer.Stop();
                dispatcherTimer = null; //release strong pointer as it is no longer required
            }
        }

        void postTokenToServer(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(latestPushToken))
                AccountUtils.postPushNotification(latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }
    }
}
