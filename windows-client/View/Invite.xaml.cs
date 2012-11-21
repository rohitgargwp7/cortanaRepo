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
using Microsoft.Phone.Tasks;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class Invite : PhoneApplicationPage
    {
        string email_invite = "Hi! I’ve started using hike, an awesome new free messaging app. You can message friends on hike and also those who aren’t for free! Messaging has never been simpler. Download the app at http://get.hike.in/{0} to start messaging me for free!";
        string external_invite_message = "I’m using hike, an awesome new free messaging app! Download the app to start messaging me for free! @hikeapp";

        public Invite()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                socialFb.Text = "FB (connected)";
            else
                socialFb.Text = "FB (not connected)";

            if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN))
                socialTw.Text = "TW (connected)";
            else
                socialTw.Text = "TW (not connected)";
        }

        private void Social_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_SOCIAL);
            string inviteToken = null;
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(external_invite_message, inviteToken==null?"":inviteToken);
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri("http://get.hike.in/" + inviteToken, UriKind.Absolute);
            shareLinkTask.Message = inviteMsg;
            try
            {
                shareLinkTask.Show();
            }
            catch
            {
            }
        }

        private void Email_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_EMAIL);
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(email_invite, inviteToken);
            EmailComposeTask f5EmailCompose = new EmailComposeTask();
            f5EmailCompose.Subject = "hike. Fun, free messaging for life";
            f5EmailCompose.Body = inviteMsg;
            try
            {
                f5EmailCompose.Show();
            }
            catch
            {
            }
        }

        private void Messaging_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_MESSAGE);
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(App.sms_invite_message, inviteToken);
            SmsComposeTask sms = new Microsoft.Phone.Tasks.SmsComposeTask();
            sms.Body = inviteMsg;
            try
            {
                sms.Show();
            }
            catch
            {
            }
        }

        private void SocialFb_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["Social_Request"] = false;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        public static void SocialPostFB(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Successfully posted to facebook.", "Facebook Post", MessageBoxButton.OK);
                });
            }
        }

        public static void SocialDeleteFB(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Successfully Logged out.", "Facebook Logout", MessageBoxButton.OK);
                });
            }
        }

        private void SocialTw_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["Social_Request"] = true;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        public static void SocialPostTW(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Successfully posted to twitter.", "Twitter Post", MessageBoxButton.OK);
                });
            }
        }

        public static void SocialDeleteTW(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show("Successfully Logged out.", "Twitter Logout", MessageBoxButton.OK);
                });
            }
        }

    }
}