
﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.Diagnostics;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage, HikePubSub.Listener
    {
        bool canGoBack = true;
        private readonly SolidColorBrush rectangleColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private readonly Thickness box4Margin = new Thickness(5, 5, 5, 5);

        public enum SocialState
        {
            FB_LOGIN, FB_LOGOUT, TW_LOGIN, TW_LOGOUT, DEFAULT
        }

        public FreeSMS()
        {
            InitializeComponent();
            initpageBasedOnState();
            App.HikePubSubInstance.addListener(HikePubSub.INVITEE_NUM_CHANGED, this);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("FromSocialPage")) // shows page is navigated from social page
            {
                PhoneApplicationService.Current.State.Remove("FromSocialPage");
                ChangeElementsState(false);
                object oo;
                SocialState ss = SocialState.DEFAULT;
                if (PhoneApplicationService.Current.State.TryGetValue("socialState", out oo))
                {
                    ss = (SocialState)oo;
                    PhoneApplicationService.Current.State.Remove("socialState");
                }
                switch (ss)
                {
                    case SocialState.FB_LOGIN:
                        ChangeElementsState(false);
                        JObject oj = new JObject();
                        oj["id"] = (string)App.appSettings["FbUserId"];
                        oj["token"] = (string)App.appSettings["FbAccessToken"];
                        AccountUtils.SocialPost(oj, new AccountUtils.postResponseFunction(SocialPostFB), "fb", true);
                        break;
                    case SocialState.FB_LOGOUT:
                        ChangeElementsState(false);
                        AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteFB), "fb", false);
                        break;
                    case SocialState.TW_LOGIN:
                        JObject ojj = new JObject();
                        ojj["id"] = (string)App.appSettings["TwToken"]; ;
                        ojj["token"] = (string)App.appSettings["TwTokenSecret"];
                        AccountUtils.SocialPost(ojj, new AccountUtils.postResponseFunction(SocialPostTW), "twitter", true);
                        break;
                    default:
                        ChangeElementsState(true);
                        break;
                }
            }
            else
            {
                if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                    facebookBtn.Content = "facebook(c)";
                else
                    facebookBtn.Content = "facebook(nc)";

                if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN))
                    twitterBtn.Content = "twitter(c)";
                else
                    twitterBtn.Content = "twitter(nc)";
            }
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.INVITEE_NUM_CHANGED, this);
            }
            catch
            {
            }
            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!canGoBack)
            {
                e.Cancel = true;
                return;
            }
            base.OnBackKeyPress(e);
        }

        private void ChangeElementsState(bool enable)
        {
            if (enable)
            {
                shellProgress.IsVisible = false;
                twitterBtn.IsEnabled = true;
                facebookBtn.IsEnabled = true;
                inviteNow.IsEnabled = true;
                canGoBack = true;
            }
            else
            {
                shellProgress.IsVisible = true;
                twitterBtn.IsEnabled = false;
                facebookBtn.IsEnabled = false;
                inviteNow.IsEnabled = false;
                canGoBack = false;
            }
        }

        private void initpageBasedOnState()
        {
            int creditsRemaining = 0;
            App.appSettings.TryGetValue(App.SMS_SETTING, out creditsRemaining);
            creditsRemainingTxtBlck.Text = creditsRemaining.ToString();

            int max = 100;
            if (App.appSettings.Contains(HikeConstants.TOTAL_CREDITS_PER_MONTH))
            {
                try
                {
                    max = Int32.Parse((string)App.appSettings[HikeConstants.TOTAL_CREDITS_PER_MONTH]);
                }
                catch { }

            }

            creditsRemainingBar.Width = (creditsRemaining * 435) / max;
            maxCreditsBar.Width = 435 - creditsRemainingBar.Width;
            maxCreditsTxtBlck.Text = max.ToString() + "+";
            TextBlock t3 = null;
            Rectangle r3 = null;

            creditsRemaining %= 10000;
            string strCreditsWithZeroes;
            if (creditsRemaining > 999)
                strCreditsWithZeroes = creditsRemaining.ToString("0000");
            else
                strCreditsWithZeroes = creditsRemaining.ToString("000");

            //t0.Text = strCreditsWithZeroes[0].ToString();
            //t1.Text = strCreditsWithZeroes[1].ToString();
            //t2.Text = strCreditsWithZeroes[2].ToString();
            if (t3 != null)
                t3.Text = strCreditsWithZeroes[3].ToString();

            if (Utils.isDarkTheme())
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
                facebookBtn.Background = twitterBtn.Background = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
                //t0.Foreground = t1.Foreground = t2.Foreground = UI_Utils.Instance.Black;
                //r0.Fill = r1.Fill = r2.Fill = UI_Utils.Instance.White;
                bottomLine.Fill = UI_Utils.Instance.Black;
            }
            else
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0xfa, 0xfa, 0xfa));
                facebookBtn.Background = twitterBtn.Background = new SolidColorBrush(Color.FromArgb(255, 0xf1, 0xf1, 0xf1));
                //t0.Foreground = t1.Foreground = t2.Foreground = UI_Utils.Instance.White;
                //r0.Fill = r1.Fill = r2.Fill = new SolidColorBrush(Color.FromArgb(255, 0x2f, 0x2f, 0x2f));
                bottomLine.Fill = new SolidColorBrush(Color.FromArgb(255, 0xcd, 0xcd, 0xcd));
            }
            //if (t3 != null)
            //    t3.Foreground = t0.Foreground;
            //if (r3 != null)
            //    r3.Fill = r0.Fill;


        }

        private void inviteBtn_Click(object sender, RoutedEventArgs e)
        {
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.INVITEE_NUM_CHANGED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    string credits = (string)App.appSettings[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                    int creditCount = -1;
                    int.TryParse(credits, out creditCount);
                    //if (creditCount > 0)
                    //{
                    //    MaxCredits.Text = credits;
                    //    maxCreditCount.Opacity = 1;
                    //}
                    //else
                    //{
                    //    maxCreditCount.Opacity = 0;
                    //}
                });
            }
        }

        private void InviteBtn_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FREE SMS SCREEN :: Exception while navigating to Invite screen : " + ex.StackTrace);
            }
        }

        private void facebookBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN)) // already logged in
            {
                MessageBoxResult res = MessageBox.Show("Are you sure you want to unlink your account?", "Unlink Facebook", MessageBoxButton.OKCancel);
                if (res == MessageBoxResult.Cancel)
                    return;
            }
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = false;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        public void SocialPostFB(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    facebookBtn.Content = "facebook(c)";
                    ChangeElementsState(true);
                    MessageBox.Show("Successfully posted to facebook.", "Facebook Post", MessageBoxButton.OK);
                });
            }
            else
            {
            }
        }

        public void SocialDeleteFB(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ChangeElementsState(true);
                    facebookBtn.Content = "facebook(nc)";
                    MessageBox.Show("Successfully Unlinked Account.", "Facebook Unlink", MessageBoxButton.OK);
                });
            }
            else
            {
            }
        }

        private void twitterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN)) // already logged in
            {
                MessageBoxResult res = MessageBox.Show("Are you sure you want to unlink your account?", "Unlink Twitter", MessageBoxButton.OKCancel);
                if (res == MessageBoxResult.Cancel)
                    return;
                else
                {
                    App.RemoveKeyFromAppSettings("TwToken");
                    App.RemoveKeyFromAppSettings("TwTokenSecret");
                    App.RemoveKeyFromAppSettings(HikeConstants.TW_LOGGED_IN);
                    AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteTW), "twitter", false);
                    return;
                }
            }
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = true;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        public void SocialPostTW(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ChangeElementsState(true);
                    twitterBtn.Content = "twitter(c)";
                    MessageBox.Show("Successfully posted to twitter.", "Twitter Post", MessageBoxButton.OK);
                });
            }
        }

        public void SocialDeleteTW(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    ChangeElementsState(true);
                    twitterBtn.Content = "twitter(nc)";
                    MessageBox.Show("Successfully Unlinked Account.", "Twitter Logout", MessageBoxButton.OK);
                });
            }
        }

    }
}