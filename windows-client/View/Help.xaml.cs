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
using System.Windows.Media.Imaging;
using Microsoft.Phone.Shell;
using windows_client.Model;
using Microsoft.Phone.Net.NetworkInformation;
using windows_client.utils;

namespace windows_client.View
{
    public partial class Help : PhoneApplicationPage
    {
        public Help()
        {
            InitializeComponent();
            if (utils.Utils.isDarkTheme())
            {
                this.made_with_love.Source = new BitmapImage(new Uri("images/made_with_love_dark.png", UriKind.Relative));
            }
            else
            {
                this.made_with_love.Source = new BitmapImage(new Uri("images/made_with_love.png", UriKind.Relative));
            }
            applicationVersion.Text = utils.Utils.getAppVersion();
            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
            if (string.IsNullOrEmpty(country_code) || country_code == "+91")
            {
                walkthroughPanel.Visibility = Visibility.Visible;
            }
            else
            {
                walkthroughPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void FAQs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.FAQS);
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.FAQS_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch { }
        }

        private void ContactUs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.CONTACT_US);
            EmailComposeTask contactUsMail = new EmailComposeTask();
            contactUsMail.To = "support@hike.in";
            contactUsMail.Subject = "Feedback on WP7";

            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];

            //string country_code = "";
            //App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);

            contactUsMail.Body = "\n\n\n\n\nHike Version: " + Utils.getAppVersion() +
                "\nWin OS Version: " + Utils.getOSVersion() + "\nPhone Number: " + msisdn + "\nDevice Model: " + Utils.getDeviceModel() +
                "\nCarrier: " + DeviceNetworkInformation.CellularMobileOperator;
            try
            {
                contactUsMail.Show();
            }
            catch
            {
            }
        }

        private void Legal_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.LEGAL);
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.TERMS_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch { }
        }

        private void Updates_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {

        }

        private void Walkthrough_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.WALKTHROUGH);
            NavigationService.Navigate(new Uri("/View/Walkthrough.xaml", UriKind.Relative));
        }

        private void rateAndReview_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.RATE_APP);
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            try
            {
                marketplaceReviewTask.Show();
            }
            catch { }
        }
    }
}