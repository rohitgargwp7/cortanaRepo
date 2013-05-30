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
using windows_client.Languages;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class Invite : PhoneApplicationPage
    {

        public Invite()
        {
            InitializeComponent();
        }

        private void Social_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_SOCIAL);
            string inviteToken = null;
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri("http://get.hike.in/" + inviteToken, UriKind.Absolute);
            shareLinkTask.Message = AppResources.Social_Invite_Txt;
            try
            {
                shareLinkTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Invite.xaml ::  Social_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void Email_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_EMAIL);
            string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(AppResources.Email_Invite_Txt, inviteToken);
            EmailComposeTask f5EmailCompose = new EmailComposeTask();
            f5EmailCompose.Subject = AppResources.Hike_txt + AppResources.Messaging_Made_Personal_Txt;
            f5EmailCompose.Body = inviteMsg;
            try
            {
                f5EmailCompose.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Invite.xaml ::  Email_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void Messaging_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_MESSAGE);
            string uri = "/View/InviteUsers.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
            //string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            //string inviteMsg = string.Format(AppResources.sms_invite_message, inviteToken);
            //SmsComposeTask sms = new Microsoft.Phone.Tasks.SmsComposeTask();
            //sms.Body = inviteMsg;
            //try
            //{
            //    sms.Show();
            //}
            //catch
            //{
            //}
        }

    }
}