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
using System.Windows.Documents;
using windows_client.Model;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage, HikePubSub.Listener
    {
        public FreeSMS()
        {
            InitializeComponent();
            initpageBasedOnState();
            App.HikePubSubInstance.addListener(HikePubSub.INVITEE_NUM_CHANGED, this);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.INVITEE_NUM_CHANGED, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Free Smms ::  OnRemovedFromJournal , Exception : " + ex.StackTrace);
            }
            base.OnRemovedFromJournal(e);
        }

        private void initpageBasedOnState()
        {
            initializeCredits();
            if (Utils.isDarkTheme())
            {
                hikeToSMSGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0x12, 0x12, 0x12));
                earnFreeSmsTxt.Foreground = new SolidColorBrush(Color.FromArgb(255, 0xa3, 0xa3, 0xa3));
            }
            else
            {
                hikeToSMSGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0xf2, 0xf2, 0xf2));
                earnFreeSmsTxt.Foreground = new SolidColorBrush(Color.FromArgb(255, 0x55, 0x55, 0x55));
            }
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
                Analytics.SendClickEvent(HikeConstants.INVITE_SMS_SCREEN_FROM_CREDIT);
                NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FREE SMS SCREEN :: Exception while navigating to Invite screen : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("Free Sms::  initializeCredits , Exception : " + ex.StackTrace);
                }
            }
            long val = ((long)creditsRemaining * 435) / max;
        }

        private void startChat_Click(object sender, RoutedEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.COMPOSE);
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        public enum SocialState
        {
            FB_LOGIN, FB_LOGOUT, TW_LOGIN, TW_LOGOUT, DEFAULT
        }
    }
}