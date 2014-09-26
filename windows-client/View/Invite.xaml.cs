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
        //    HikeInstantiation.AnalyticsInstance.addEvent(Analytics.INVITE_SOCIAL);
        //    string inviteToken = null;
        //    //HikeInstantiation.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
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
            string inviteToken = string.Empty;
            //HikeInstantiation.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            string inviteMsg = string.Format(AppResources.Email_Invite_Txt, inviteToken);
            EmailHelper.SendEmail(AppResources.Invite_Email_Subject, inviteMsg);
        }

        private void Messaging_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Analytics.SendClickEvent(HikeConstants.INVITE_SMS_SCREEN_FROM_INVITE);
            string uri = "/View/InviteUsers.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void Twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isTwitterPost)
            {
                if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.TW_LOGGED_IN)) // already logged in
                {
                    isTwitterPost = true;
                    sendInvite();
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.SOCIAL] = HikeConstants.ServerJsonKeys.TWITTER;
                    NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                }
            }
            //else
            //    isTwitterPost = false;

            //JObject ojj = new JObject();
            //ojj["id"] = (string)HikeInstantiation.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN]; ;
            //ojj["token"] = (string)HikeInstantiation.appSettings[HikeConstants.AppSettings.TWITTER_TOKEN_SECRET];
            //ojj["post"] = false;
            //AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), HikeConstants.TWITTER, true);
        }

        private void Facebook_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isFacebookPost)
            {
                if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettings.FB_LOGGED_IN)) // already logged in
                {
                    isFacebookPost = true;
                    sendInvite();
                }
                else
                {
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.SOCIAL] = HikeConstants.ServerJsonKeys.FACEBOOK;
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

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    SystemTray.ProgressIndicator = new ProgressIndicator();
                    SystemTray.ProgressIndicator.IsVisible = true;
                    SystemTray.ProgressIndicator.IsIndeterminate = true;
                });

            JObject statusJSON = new JObject();
            statusJSON["fb"] = isFacebookPost;
            statusJSON["twitter"] = isTwitterPost;

            AccountUtils.SocialInvite(statusJSON, new AccountUtils.postResponseFunction(SocialInviteResponse));
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.NavigationKeys.FROM_SOCIAL_PAGE)) // shows page is navigated from social page
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.FROM_SOCIAL_PAGE);
                object oo;

                FreeSMS.SocialState ss = FreeSMS.SocialState.DEFAULT;

                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.NavigationKeys.SOCIAL_STATE, out oo))
                {
                    ss = (FreeSMS.SocialState)oo;
                    PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.SOCIAL_STATE);
                }

                switch (ss)
                {
                    case FreeSMS.SocialState.FB_LOGIN:
                        JObject oj = new JObject();
                        oj["id"] = (string)HikeInstantiation.AppSettings[HikeConstants.AppSettings.FB_USER_ID];
                        oj["token"] = (string)HikeInstantiation.AppSettings[HikeConstants.AppSettings.FB_ACCESS_TOKEN];
                        oj["post"] = false;//so that hike promotional post is not posted on fb
                        AccountUtils.SocialPost(oj, new AccountUtils.postResponseFunction(SocialPostFB), HikeConstants.ServerJsonKeys.FACEBOOK, true);
                        break;

                    case FreeSMS.SocialState.TW_LOGIN:
                        JObject ojj = new JObject();
                        ojj["id"] = (string)HikeInstantiation.AppSettings[HikeConstants.AppSettings.TWITTER_TOKEN]; ;
                        ojj["token"] = (string)HikeInstantiation.AppSettings[HikeConstants.AppSettings.TWITTER_TOKEN_SECRET];
                        ojj["post"] = false;
                        AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), HikeConstants.ServerJsonKeys.TWITTER, true);
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
            string title = isFacebookPost ? AppResources.Facebook_Caps : AppResources.Twitter_Caps;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (SystemTray.ProgressIndicator != null)
                        SystemTray.ProgressIndicator.IsIndeterminate = false;
                });

            if (isFacebookPost && obj != null && HikeConstants.ServerJsonKeys.OK == (string)obj[HikeConstants.ServerJsonKeys.STAT])
            {
                status = (string)obj[HikeConstants.ServerJsonKeys.FACEBOOK];
                isFacebookPost = false;
            }

            if (isTwitterPost && obj != null && HikeConstants.ServerJsonKeys.OK == (string)obj[HikeConstants.ServerJsonKeys.STAT])
            {
                status = (string)obj[HikeConstants.ServerJsonKeys.TWITTER];
                isTwitterPost = false;
            }

            if (status.Equals(HikeConstants.ServerJsonKeys.NO_TOKEN) || status.Equals(HikeConstants.ServerJsonKeys.INVALID_TOKEN))
            {
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.SOCIAL] = isTwitterPost ? HikeConstants.ServerJsonKeys.TWITTER : HikeConstants.ServerJsonKeys.FACEBOOK;
                NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
                return;
            }

            if (status.Equals(HikeConstants.ServerJsonKeys.FAILURE))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Invite_Not_Sent + " " + AppResources.Please_Retry_Later, title, MessageBoxButton.OK);
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Invite_Posted_Successfully, title, MessageBoxButton.OK);
                });
            }
        }

        private bool isTwitterPost;
        private bool isFacebookPost;
    }
}