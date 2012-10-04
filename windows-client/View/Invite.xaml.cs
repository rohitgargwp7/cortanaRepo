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
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(App.external_invite_message, inviteToken);
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri("http://get.hike.in/" + inviteToken, UriKind.Absolute);
            shareLinkTask.Title = "Hike, Free messaging for life.";
            shareLinkTask.Message = inviteMsg;
            shareLinkTask.Show();
        }

        private void Email_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(App.external_invite_message, inviteToken);
            EmailComposeTask f5EmailCompose = new EmailComposeTask();
            f5EmailCompose.Subject = "Hike, Free messaging for life.";
            f5EmailCompose.Body = inviteMsg;
            f5EmailCompose.Show();
        }

        private void Messaging_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(App.external_invite_message, inviteToken);
            SmsComposeTask sms = new Microsoft.Phone.Tasks.SmsComposeTask();
            sms.Body = inviteMsg;
            sms.Show();
        }
    }
}