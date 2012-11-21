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
            TextBlock t3 = null;
            Rectangle r3 = null;
            //if (creditsRemaining > 999)
            //{
            //    ColumnDefinition c4 = new ColumnDefinition();
            //    c4.Width = GridLength.Auto;
            //    SMSCounterGrid.ColumnDefinitions.Add(c4);

            //    r3 = new Rectangle();
            //    r3.Fill = rectangleColor;
            //    r3.Margin = box4Margin;
            //    r3.Width = 47;
            //    r3.Height = 76;
            //    Grid.SetColumn(r3, 3);
            //    SMSCounterGrid.Children.Add(r3);

            //    t3 = new TextBlock();
            //    t3.FontSize = 45;
            //    t3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            //    t3.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            //    Grid.SetColumn(t3, 3);
            //    SMSCounterGrid.Children.Add(t3);
            //}

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
    }
}