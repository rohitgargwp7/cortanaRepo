using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage, HikePubSub.Listener
    {
        private readonly SolidColorBrush rectangleColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private readonly Thickness box4Margin = new Thickness(5, 5, 5, 5);

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
            catch
            {
            }
            base.OnRemovedFromJournal(e);
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
            creditsRemainingBar.Width = (creditsRemaining * 435)/max;
            maxCreditsBar.Width = 435 - creditsRemainingBar.Width;
            maxCreditsTxtBlck.Text = max.ToString() + "+";
            creditsRemaining %= 10000;
            string strCreditsWithZeroes;
            if (creditsRemaining > 999)
                strCreditsWithZeroes = creditsRemaining.ToString("0000");
            else
                strCreditsWithZeroes = creditsRemaining.ToString("000");

            if (Utils.isDarkTheme())
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
                facebookBtn.Background = twitterBtn.Background = new SolidColorBrush(Color.FromArgb(255, 0x1f, 0x1f, 0x1f));
                bottomLine.Fill = UI_Utils.Instance.Black;
                upperBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x1a, 0x1a, 0x1a));
                lowerBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0x25, 0x25, 0x25));
            }
            else
            {
                upperGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0xfa, 0xfa, 0xfa));
                facebookBtn.Background = twitterBtn.Background = new SolidColorBrush(Color.FromArgb(255, 0xf1, 0xf1, 0xf1));
                bottomLine.Fill = new SolidColorBrush(Color.FromArgb(255, 0xce, 0xce, 0xce));
                upperBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xce, 0xce, 0xce));
                lowerBar.Fill = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));
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
                    string credits = (string)App.appSettings[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                    int creditCount = -1;
                    int.TryParse(credits, out creditCount);
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
    }
}