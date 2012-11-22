using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Notification;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace windows_client.utils
{
    public class PushHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile PushHelper instance = null;

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


        public void registerPushnotifications()
        {
            HttpNotificationChannel pushChannel;
            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
            try
            {
                bool secure_push;
                // If the channel was not found, then create a new connection to the push service.
                if (pushChannel == null)
                {
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
                    bool isChannelSecure;
                    App.appSettings.TryGetValue(HikeConstants.IS_SECURE_CHANNEL, out isChannelSecure);
                    if (!isChannelSecure && App.appSettings.TryGetValue(HikeConstants.SECURE_PUSH, out secure_push) && secure_push)
                    {
                        //if channel was not secure and we are ready for secured push. close this channel and new channel 
                        //would be created on next app launch
                        if (pushChannel.IsShellTileBound)
                            pushChannel.UnbindToShellTile();
                        if (pushChannel.IsShellToastBound)
                            pushChannel.UnbindToShellToast();
                        pushChannel.Close();
                        pushChannel.Dispose();
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
                            Debug.WriteLine(pushChannel.ChannelUri.ToString());
                            AccountUtils.postPushNotification(pushChannel.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
                        }
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
                AccountUtils.postPushNotification(e.ChannelUri.ToString(), new AccountUtils.postResponseFunction(postPushNotification_Callback));
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



    }
}
