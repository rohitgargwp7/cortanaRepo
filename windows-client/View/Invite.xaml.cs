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
using windows_client.utils;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using windows_client.DbUtils;
using System.Net.NetworkInformation;

namespace windows_client.View
{
    public partial class Invite : PhoneApplicationPage
    {

        public Invite()
        {
            InitializeComponent();
        }

        //private void Social_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    App.AnalyticsInstance.addEvent(Analytics.INVITE_SOCIAL);
        //    string inviteToken = null;
        //    //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
        //    ShareLinkTask shareLinkTask = new ShareLinkTask();
        //    shareLinkTask.LinkUri = new Uri("http://get.hike.in/" + inviteToken, UriKind.Absolute);
        //    shareLinkTask.Message = AppResources.Social_Invite_Txt;
        //    try
        //    {
        //        shareLinkTask.Show();
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine("Invite.xaml ::  Social_Tap , Exception : " + ex.StackTrace);
        //    }
        //}

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

        private void Twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isTwitterPost)
            {
                if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN)) // already logged in
                {
                    isTwitterPost = true;
                    sendInvite();
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.TWITTER;
                    NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                }
            }
            //else
            //    isTwitterPost = false;

            //JObject ojj = new JObject();
            //ojj["id"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN]; ;
            //ojj["token"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN_SECRET];
            //ojj["post"] = false;
            //AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), HikeConstants.TWITTER, true);
        }

        private void Facebook_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isFacebookPost)
            {
                if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN)) // already logged in
                {
                    isFacebookPost = true;
                    sendInvite();
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
                    NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                }
            }
            //else
            //    isFacebookPost = false;
        }

        private void sendInvite()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }

            JObject statusJSON = new JObject();
            statusJSON["fb"] = isFacebookPost;
            statusJSON["twitter"] = isTwitterPost;

            AccountUtils.SocialInvite(statusJSON, new AccountUtils.postResponseFunction(SocialInviteResponse));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FROM_SOCIAL_PAGE)) // shows page is navigated from social page
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.FROM_SOCIAL_PAGE);
                object oo;

                FreeSMS.SocialState ss = FreeSMS.SocialState.DEFAULT;

                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.SOCIAL_STATE, out oo))
                {
                    ss = (FreeSMS.SocialState)oo;
                    PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL_STATE);
                }

                switch (ss)
                {
                    case FreeSMS.SocialState.FB_LOGIN:
                        JObject oj = new JObject();
                        oj["id"] = (string)App.appSettings[HikeConstants.AppSettings.FB_USER_ID];
                        oj["token"] = (string)App.appSettings[HikeConstants.AppSettings.FB_ACCESS_TOKEN];
                        oj["post"] = false;//so that hike promotional post is not posted on fb
                        AccountUtils.SocialPost(oj, new AccountUtils.postResponseFunction(SocialPostFB), HikeConstants.FACEBOOK, true);
                        break;

                    case FreeSMS.SocialState.TW_LOGIN:
                        JObject ojj = new JObject();
                        ojj["id"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN]; ;
                        ojj["token"] = (string)App.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN_SECRET];
                        ojj["post"] = false;
                        AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), HikeConstants.TWITTER, true);
                        break;
                }
            }
        }

        public void SocialPostFB(JObject obj)
        {
            isFacebookPost = true;
            isTwitterPost = false;

            sendInvite();
        }

        public void SocialPostTW(JObject obj)
        {
            isTwitterPost = true;
            isFacebookPost = false;

            sendInvite();
        }

        public void SocialInviteResponse(JObject obj)
        {
            string status = "";

            if (isFacebookPost && obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                status = (string)obj[HikeConstants.FACEBOOK];
                isFacebookPost = false;
            }

            if (isTwitterPost && obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                status = (string)obj[HikeConstants.TWITTER];
                isTwitterPost = false;
            }

            if (status.Equals(HikeConstants.NO_TOKEN) || status.Equals(HikeConstants.INVALID_TOKEN))
            {
                PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = isTwitterPost ? HikeConstants.TWITTER : HikeConstants.FACEBOOK;
                NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                return;
            }

            if (status.Equals(HikeConstants.FAILURE))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt);
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Invite_Sent_Successfully);
                });
            }
        }

        private bool isTwitterPost;
        private bool isFacebookPost;
    }
}