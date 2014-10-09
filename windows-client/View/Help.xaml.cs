using CommonLibrary.Constants;
using CommonLibrary.Utils;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using System;
using System.Diagnostics;
using System.Text;
using windows_client.Languages;
using windows_client.utils;

namespace windows_client.View
{
    public partial class Help : PhoneApplicationPage
    {
        public Help()
        {
            InitializeComponent();

            if (HikeInstantiation.ViewModel.IsDarkMode)
                madeInIndia.Source = UI_Utils.Instance.MadeInIndiaBlack;
            else
                madeInIndia.Source = UI_Utils.Instance.MadeInIndiaWhite;

            applicationVersion.Text = string.Format(AppResources.Help_AppVersionTitle, Utility.GetAppVersion());
        }

        private void FAQs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(ServerUrls.FAQS_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HElp.xaml ::  FAQs_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void ContactUs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            string msisdn = (string)HikeInstantiation.AppSettings[AppSettingsKeys.MSISDN_SETTING];
            StringBuilder emailBodyText = new StringBuilder();
            emailBodyText.Append("\n\n\n\n\n").Append(AppResources.Help_EmailHikeVersion).Append(Utility.GetAppVersion()).Append(
                "\n").Append(AppResources.Help_EmailOSVersion).Append(Utils.getOSVersion()).Append("\n").Append(AppResources.Help_EmailPhoneNo).
                Append(msisdn).Append("\n").Append(
                AppResources.Help_EmailDeviceModel).Append(Utils.getDeviceModel()).Append(
                "\n").Append(AppResources.Help_EmailCarrier).Append(DeviceNetworkInformation.CellularMobileOperator);
            EmailHelper.SendEmail(AppResources.Help_EmailSubject, emailBodyText.ToString(), ServerUrls.CONTACT_US_EMAIL);
        }

        private void Legal_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(ServerUrls.TERMS_AND_CONDITIONS, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HElp.xaml ::  Legal_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void Walkthrough_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Walkthrough.xaml", UriKind.Relative));
        }

        private void rateAndReview_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
            try
            {
                marketplaceReviewTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HElp.xaml ::  rateAndReview_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void SystemHealth_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(ServerUrls.SYSTEM_HEALTH_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("HElp.xaml ::  SystemHealth_Tap , Exception : " + ex.StackTrace);
            }
        }
    }
}