using System;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using Microsoft.Phone.Reactive;

namespace windows_client.utils
{
    public class PushHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile PushHelper instance = null;
        private readonly int maxPollingTime = 120;
        private volatile int pollingTime = 3; //in seconds
        private readonly int minPollingTime = 3;
        private volatile IScheduler scheduler; //TODO MG - we should can try pooling of scheduler objects
        private volatile IDisposable httpPostScheduled;

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
                    if (httpPostScheduled != null)
                    {
                        httpPostScheduled.Dispose();
                        httpPostScheduled = null;
                    }
                    postTokenToServer();
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
                LatestPushToken = null;
                scheduler = null;
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
            string pushToken;
            App.appSettings.TryGetValue<string>(App.LATEST_PUSH_TOKEN, out pushToken);
            _latestPushToken = pushToken;
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
            if (stat != HikeConstants.OK)
            {
                if (scheduler == null)
                {
                    scheduler = Scheduler.NewThread;
                }
                httpPostScheduled = scheduler.Schedule(postTokenToServer, TimeSpan.FromSeconds(pollingTime));
                pollingTime *= 2;
                if (pollingTime > maxPollingTime)
                    pollingTime = minPollingTime;
            }
            else if (stat == HikeConstants.OK)
            {
                App.WriteToIsoStorageSettings(App.LATEST_PUSH_TOKEN, _latestPushToken);
                if (httpPostScheduled != null)
                {
                    httpPostScheduled.Dispose();
                    httpPostScheduled = null;
                }
                scheduler = null;
            }
        }

        private void postTokenToServer()
        {
            if (!string.IsNullOrEmpty(_latestPushToken) && NetworkInterface.GetIsNetworkAvailable())
                AccountUtils.postPushNotification(_latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }
    }
}
