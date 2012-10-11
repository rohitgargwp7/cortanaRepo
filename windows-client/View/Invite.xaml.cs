﻿using System;
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
        string email_invite = "Hi! I’ve started using hike, an awesome new free messaging app. You can message friends on hike and also those who aren’t for free! Messaging has never been simpler. Download the app at http://get.hike.in/{0} to start messaging me for free!";
        string external_invite_message = "I’m using @hikeapp, an awesome new free messaging app! Download the app at http://get.hike.in/{0} to start messaging me for free!";

        public Invite()
        {
            InitializeComponent();
        }

        private void Social_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(external_invite_message, inviteToken);
            ShareLinkTask shareLinkTask = new ShareLinkTask();
            shareLinkTask.LinkUri = new Uri("http://get.hike.in/" + inviteToken, UriKind.Absolute);
            //shareLinkTask.Title = "hike.";
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
    }
}