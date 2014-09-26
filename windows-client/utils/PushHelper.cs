using System;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;
using System.Linq;
using windows_client.Model;

namespace windows_client.utils
{
    public class PushHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile PushHelper instance = null;
        private readonly int maxPollingTime = 120;
        private volatile int pollingTime; //in seconds
        private readonly int minPollingTime = 3;
        private volatile IScheduler scheduler; //TODO MG - we should can try pooling of scheduler objects
        private volatile IDisposable httpPostScheduled;

        private bool retryForPushChannelException = true;

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

                    //remove events as it may be called
                    pushChannel.ChannelUriUpdated -= PushChannel_ChannelUriUpdated;
                    pushChannel.ErrorOccurred -= PushChannel_ErrorOccurred;

                    pushChannel.Close();
                    pushChannel.Dispose();
                }
                LatestPushToken = null;
                scheduler = null;
            }
            catch (InvalidOperationException e)
            {
                Debug.WriteLine("Push Helper :: closePushnotifications : " + e.StackTrace);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Push Helper :: closePushnotifications : " + e.StackTrace);
            }
        }

        public void registerPushnotifications(bool forcePushToken)
        {
            string pushToken;
            if (forcePushToken)//have to push token to server forcefully
            {

                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.LATEST_PUSH_TOKEN, string.Empty);
                _latestPushToken = string.Empty;
            }
            else
            {

                HikeInstantiation.AppSettings.TryGetValue<string>(HikeConstants.AppSettings.LATEST_PUSH_TOKEN, out pushToken);
                _latestPushToken = pushToken;
            }

            HttpNotificationChannel pushChannel;
            pollingTime = minPollingTime;
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
                }
                else
                {
                    // The channel was already open, so just register for all the events.

                    //if previously events are attached remove events as it may be called again
                    pushChannel.ChannelUriUpdated -= PushChannel_ChannelUriUpdated;
                    pushChannel.ChannelUriUpdated += PushChannel_ChannelUriUpdated;

                    pushChannel.ErrorOccurred -= PushChannel_ErrorOccurred;
                    pushChannel.ErrorOccurred += PushChannel_ErrorOccurred;
                }

                // Bind this new channel for toast events.
                if (!pushChannel.IsShellTileBound)
                    pushChannel.BindToShellTile();

                if (!pushChannel.IsShellToastBound)
                    pushChannel.BindToShellToast();

                if (pushChannel.ChannelUri != null)
                {
                    LatestPushToken = pushChannel.ChannelUri.ToString();
                }
                else
                    LatestPushToken = null;
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
            string pushToken = e.ChannelUri.ToString();

            if (string.IsNullOrEmpty(pushToken))
            {
                Analytics.SendAnalyticsEvent(HikeConstants.ST_NETWORK_EVENT, HikeConstants.NULL_PUSH_TOKEN);
            }
            else
                LatestPushToken = pushToken;
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            try
            {
                Analytics.SendAnalyticsEvent(HikeConstants.ST_NETWORK_EVENT, HikeConstants.EXCEPTION_PUSH_TOKEN, (int)e.ErrorType);
            }
            catch (InvalidCastException ex)
            {
                Debug.WriteLine("PushHelper::ErrorOccured,Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace);
            }
            if ((e.ErrorType == ChannelErrorType.ChannelOpenFailed ||
                e.ErrorType == ChannelErrorType.PayloadFormatError) &&
                retryForPushChannelException)
            {
                registerPushnotifications(false);
                retryForPushChannelException = false;
            }
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

                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.LATEST_PUSH_TOKEN, _latestPushToken);
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

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.UID_SETTING) && !string.IsNullOrEmpty(_latestPushToken) && NetworkInterface.GetIsNetworkAvailable())
                AccountUtils.postPushNotification(_latestPushToken, new AccountUtils.postResponseFunction(postPushNotification_Callback));
        }

        public void ClearTile()
        {
            ShellTile flipTile = ShellTile.ActiveTiles.FirstOrDefault();

            if (flipTile != null)
            {
                IconicTileData newTileData = new IconicTileData()
                {
                    Count = 0,
                    Title = String.Empty,
                    WideContent1 = String.Empty,
                    WideContent2 = String.Empty,
                    WideContent3 = String.Empty
                };

                flipTile.Update(newTileData);
            }
        }
    }
}
