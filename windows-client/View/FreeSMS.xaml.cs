using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.Diagnostics;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage, HikePubSub.Listener
    {
        bool canGoBack = true;
        private readonly SolidColorBrush connStatusNotConnectedBlack = new SolidColorBrush(Color.FromArgb(255, 0xa5, 0xa5, 0xa5));
        private readonly SolidColorBrush connStatusConnectedBlack = new SolidColorBrush(Color.FromArgb(255, 0x63, 0x63, 0x63));
        private readonly SolidColorBrush connStatusNotConnectedWhite = new SolidColorBrush(Color.FromArgb(255, 0xb4, 0xb4, 0xb4));
        private readonly SolidColorBrush connStatusConnectedWhite = new SolidColorBrush(Color.FromArgb(255, 0x48, 0x48, 0x48));

        private bool _isFacebookConnected = false;
        private bool IsFacebookConnected
        {
            get
            {
                return _isFacebookConnected;
            }
            set
            {
                if (value != _isFacebookConnected)
                {
                    _isFacebookConnected = value;
                    showFacebook(_isFacebookConnected);
                }
            }
        }

        private bool _isTwitterConnected = false;
        private bool IsTwitterConnected
        {
            get
            {
                return _isTwitterConnected;
            }
            set
            {
                if (value != _isTwitterConnected)
                {
                    _isTwitterConnected = value;
                    showTwitter(_isTwitterConnected);
                }
            }
        }

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
                {
                    IsFacebookConnected = true;
                }
                else
                {
                    IsFacebookConnected = false;
                }
                if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN))
                {
                    IsTwitterConnected = true;
                }
                else
                {
                    IsTwitterConnected = false;
                }
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
                inviteNow.IsEnabled = true;
                canGoBack = true;
            }
            else
            {
                shellProgress.IsVisible = true;
                inviteNow.IsEnabled = false;
                canGoBack = false;
            }
        }

        private void initpageBasedOnState()
        {
            initializeCredits();
            if (Utils.isDarkTheme())
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
                bottomLine.Fill = UI_Utils.Instance.Black;
                fbConnStatus.Foreground = twConnStatus.Foreground = connStatusNotConnectedBlack;
                upperbar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x1a, 0x1a, 0x1a));
                lowerbar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x25, 0x25, 0x25));
            }
            else
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0xfa, 0xfa, 0xfa));
                bottomLine.Fill = new SolidColorBrush(Color.FromArgb(255, 0xcd, 0xcd, 0xcd));
                fbConnStatus.Foreground = twConnStatus.Foreground = connStatusNotConnectedWhite;
                upperbar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xce, 0xce, 0xce));
                lowerbar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));
            }
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
                    initializeCredits();
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
                MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkFacebook_MsgBxCaptn, MessageBoxButton.OKCancel);
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
                    IsFacebookConnected = true;
                    ChangeElementsState(true);
                    MessageBox.Show(AppResources.FreeSMS_FbPostSuccess_MsgBx, AppResources.FreeSMS_FbPost_MsgBxCaption, MessageBoxButton.OK);
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
                    IsFacebookConnected = false;
                    ChangeElementsState(true);
                    MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkFBSuccess_MsgBxCaptn, MessageBoxButton.OK);
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
                MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkTwitter_MsgBxCaptn, MessageBoxButton.OKCancel);
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
                    IsTwitterConnected = true;
                    ChangeElementsState(true);
                    MessageBox.Show(AppResources.FreeSMS_TwPostSuccess_MsgBx, AppResources.FreeSMS_TwPost_MsgBxCaption, MessageBoxButton.OK);
                });
            }
        }

        public void SocialDeleteTW(JObject obj)
        {
            if (obj != null && "ok" == (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    IsTwitterConnected = false;
                    ChangeElementsState(true);
                    MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkTwSuccess_MsgBxCaptn, MessageBoxButton.OK);
                });
            }
        }

        private void initializeCredits()
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
            long val = ((long)creditsRemaining * 435) / max;
            creditsRemainingBar.Width = val;
            if (435 - creditsRemainingBar.Width > 0)
            {
                maxCreditsBar.Width = 435 - creditsRemainingBar.Width;
            }
            else
            {
                maxCreditsBar.Width = 0;
                creditsRemainingBar.Width = 435;
            }
            maxCreditsTxtBlck.Text = max.ToString() + "+";
        }

        private void showFacebook(bool isConnected)
        {
            if (isConnected)
            {
                fbConnStatus.Text = AppResources.FreeSMS_fbOrTwitter_Connected;
                fbConnImage.Visibility = Visibility.Visible;
                if (Utils.isDarkTheme())
                {
                    fbConnStatus.Foreground = connStatusConnectedBlack;
                }
                else
                {
                    fbConnStatus.Foreground = connStatusConnectedWhite;
                }
            }
            else
            {
                fbConnStatus.Text = AppResources.FreeSMS_fbConnStatus_TxtBlk;
                fbConnImage.Visibility = Visibility.Collapsed;
                if (Utils.isDarkTheme())
                {
                    fbConnStatus.Foreground = connStatusNotConnectedBlack;
                }
                else
                {
                    fbConnStatus.Foreground = connStatusNotConnectedWhite;
                }
            }
        }

        private void showTwitter(bool isConnected)
        {
            if (isConnected)
            {
                twConnStatus.Text = AppResources.FreeSMS_fbOrTwitter_Connected;
                twConnImage.Visibility = Visibility.Visible;
                if (Utils.isDarkTheme())
                {
                    twConnStatus.Foreground = connStatusConnectedBlack;
                }
                else
                {
                    twConnStatus.Foreground = connStatusConnectedWhite;
                }

            }
            else
            {
                twConnStatus.Text = AppResources.FreeSMS_twConnStatus_TxtBlk;
                twConnImage.Visibility = Visibility.Collapsed;
                if (Utils.isDarkTheme())
                {
                    twConnStatus.Foreground = connStatusNotConnectedBlack;
                }
                else
                {
                    twConnStatus.Foreground = connStatusNotConnectedWhite;
                }
            }
        }

        private void twitter_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (canGoBack)
            {
                if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN)) // already logged in
                {
                    MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkTwitter_MsgBxCaptn, MessageBoxButton.OKCancel);
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
        }

        private void facebook_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (canGoBack)
            {
                if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN)) // already logged in
                {
                    MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkFacebook_MsgBxCaptn, MessageBoxButton.OKCancel);
                    if (res == MessageBoxResult.Cancel)
                        return;
                }
                PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = false;
                NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
            }
        }
    }
}