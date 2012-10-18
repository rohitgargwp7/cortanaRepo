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
using windows_client.utils;
using Microsoft.Phone.Tasks;
using System.Diagnostics;

namespace windows_client.View
{
    public partial class FreeSMS : PhoneApplicationPage,HikePubSub.Listener
    {
        private readonly SolidColorBrush rectangleColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private readonly Thickness box4Margin = new Thickness(5, 5, 5, 5);

        public FreeSMS()
        {
            InitializeComponent();
            initpageBasedOnState();
            App.HikePubSubInstance.addListener(HikePubSub.INVITEE_NUM_CHANGED,this);
        }

        private void initpageBasedOnState()
        {
            int creditsRemaining = (int)App.appSettings[App.SMS_SETTING];
            if (App.appSettings.Contains(HikeConstants.TOTAL_CREDITS_PER_MONTH))
            {
                int max = Int32.Parse((string)App.appSettings[HikeConstants.TOTAL_CREDITS_PER_MONTH]);
                MaxCredits.Text = Convert.ToString( max > 0 ? max:0);
            }
//            int creditsRemaining = 8796;
            TextBlock t3 = null;
            Rectangle r3 = null;
            if (creditsRemaining > 999)
            {
                ColumnDefinition c4 = new ColumnDefinition();
                c4.Width = GridLength.Auto;
                SMSCounterGrid.ColumnDefinitions.Add(c4);

                r3 = new Rectangle();
                r3.Fill = rectangleColor;
                r3.Margin = box4Margin;
                r3.Width = 40;
                Grid.SetColumn(r3, 3);
                SMSCounterGrid.Children.Add(r3);

                t3 = new TextBlock();
                t3.FontSize = 45;
                t3.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                Grid.SetColumn(t3, 3);
                SMSCounterGrid.Children.Add(t3);
            }

            creditsRemaining %= 10000;
            string strCreditsWithZeroes;
            if (creditsRemaining > 999)
                strCreditsWithZeroes = creditsRemaining.ToString("0000");
            else
                strCreditsWithZeroes = creditsRemaining.ToString("000");

            t0.Text = strCreditsWithZeroes[0].ToString();
            t1.Text = strCreditsWithZeroes[1].ToString();
            t2.Text = strCreditsWithZeroes[2].ToString();
            if (t3 != null)
                t3.Text = strCreditsWithZeroes[3].ToString();

            if (Utils.isDarkTheme())
            {
                this.FreeSMSGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0x25, 0x25, 0x25));
                t0.Foreground = t1.Foreground = t2.Foreground = UI_Utils.Instance.Black;
                r0.Fill = r1.Fill = r2.Fill = UI_Utils.Instance.White;
                leftUpper.Fill = rightUpper.Fill = UI_Utils.Instance.Black;
                leftLower.Fill = rightLower.Fill = new SolidColorBrush(Color.FromArgb(255, 0x2c, 0x2c, 0x2c));
            }
            else
            {
                this.FreeSMSGrid.Background = new SolidColorBrush(Color.FromArgb(255, 0xf6, 0xf6, 0xf6));
                t0.Foreground = t1.Foreground = t2.Foreground = UI_Utils.Instance.White;
                r0.Fill = r1.Fill = r2.Fill = new SolidColorBrush(Color.FromArgb(255, 0x2f, 0x2f, 0x2f));
                leftUpper.Fill = rightUpper.Fill = new SolidColorBrush(Color.FromArgb(255, 0xcd, 0xcd, 0xcd));
                leftLower.Fill = rightLower.Fill = new SolidColorBrush(Color.FromArgb(255, 0xee, 0xee, 0xee));
            }
            if (t3 != null)
                t3.Foreground = t0.Foreground;
            if (r3 != null)
                r3.Fill = r0.Fill;


        }

        private void inviteBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Uri("/View/InviteUsers.xaml", UriKind.Relative));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("FREE SMS SCREEN :: Exception while navigating to Invite screen : "+ex.StackTrace);
            }
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.INVITEE_NUM_CHANGED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MaxCredits.Text = (string)App.appSettings[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                });
            }
        }
    }
}