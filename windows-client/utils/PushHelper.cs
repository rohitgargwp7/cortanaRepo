using System;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Windows.Threading;
using System.Net.NetworkInformation;
using System.Windows;
using Microsoft.Phone.Reactive;

namespace windows_client.utils
{
    public class PushHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile PushHelper instance = null;
        private readonly int maxPollingTime = 120;
        private int pollingTime = 3; //in seconds
        private readonly int minPollingTime = 3;
        private DispatcherTimer dispatcherTimer;
        private IScheduler scheduler = Scheduler.NewThread; //TODO - we should can tryy pooling of scheduler objects


        private string _latestPushToken;
        private string LatestPushToken
        {
            get
            {
                return _latestPushToken;
            }
            set
            {
                if (value != _latestPushToken)
                {
                    _latestPushToken = value;
                    if (dispatcherTimer != null && dispatcherTimer.IsEnabled)
                        dispatcherTimer.Stop();
                    if (!string.IsNullOrEmpty(_latestPushToken))
                    {
                        AccountUtils.postPushNotification(_latestPushToken,                        //its async call,
                            new AccountUtils.postResponseFunction(postPushNotification_Callback)); //so should be ok to call from setter
                    }
                }
            }
        }

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
            string pushToken;
            App.appSettings.TryGetValue<string>(App.LATEST_PUSH_TOKEN, out pushToken);
            LatestPushToken = pushToken;
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
                if (dispatcherTimer != null)
                {
                    if (dispatcherTimer.IsEnabled)
                        dispatcherTimer.Stop();
                    dispatcherTimer = null;
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
                }
                if (pushChannel.ChannelUri != null)
                {
                    LatestPushToken = pushChannel.ChannelUri.ToString();
                }
                else
                {
                    LatestPushToken = null;
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
            LatestPushToken = e.ChannelUri.ToString();
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
                if (statusToken != null)
                    stat = statusToken.ToString();
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (stat != HikeConstants.OK && NetworkInterface.GetIsNetworkAvailable())
                {
                    if (dispatcherTimer == null)
                    {
                        dispatcherTimer = new DispatcherTimer();
                        dispatcherTimer.Tick += postTokenToServer;
                    }
                    dispatcherTimer.Interval = TimeSpan.FromSeconds(pollingTime);
                    pollingTime *= 2;
                    if (pollingTime > maxPollingTime)
                        pollingTime = minPollingTime;
                    if (!dispatcherTimer.IsEnabled)
                        dispatcherTimer.Start();
                }
                else if (stat == HikeConstants.OK && dispatcherTimer != null)
                {
                    App.WriteToIsoStorageSettings(App.LATEST_PUSH_TOKEN, _latestPushToken);
                    if (dispatcherTimer.IsEnabled)
                        dispatcherTimer.Stop();
                    dispatcherTimer = null; //release strong pointer as it is no longer required
                }
            });
        }

        void postTokenToServer(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_latestPushToken))
                AccountUtils.postPushNotification(_latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }
    }
}
