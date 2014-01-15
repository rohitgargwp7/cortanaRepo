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
using System.Windows.Media.Imaging;
using Microsoft.Phone.Shell;
using windows_client.Model;
using Microsoft.Phone.Net.NetworkInformation;
using windows_client.utils;
using windows_client.Languages;
using System.Text;
using System.Diagnostics;

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
            catch (Exception ex)
            {
                Logging.LogWriter.Instance.WriteToLog("HElp.xaml ::  FAQs_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void ContactUs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.CONTACT_US);
            EmailComposeTask contactUsMail = new EmailComposeTask();
            contactUsMail.To = "support@hike.in";
            contactUsMail.Subject = AppResources.Help_EmailSubject;
            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
            StringBuilder emailBodyText = new StringBuilder();
            emailBodyText.Append("\n\n\n\n\n").Append(AppResources.Help_EmailHikeVersion).Append(Utils.getAppVersion()).Append(
                "\n").Append(AppResources.Help_EmailOSVersion).Append(Utils.getOSVersion()).Append("\n").Append(AppResources.Help_EmailPhoneNo).
                Append(msisdn).Append("\n").Append(
                AppResources.Help_EmailDeviceModel).Append(Utils.getDeviceModel()).Append(
                "\n").Append(AppResources.Help_EmailCarrier).Append(DeviceNetworkInformation.CellularMobileOperator);
            contactUsMail.Body = emailBodyText.ToString();
            try
            {
                contactUsMail.Show();
            }
            catch (Exception ex)
            {
                Logging.LogWriter.Instance.WriteToLog("HElp.xaml ::  ContactUs_Tap , Exception : " + ex.StackTrace);
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
            catch (Exception ex)
            {
                Logging.LogWriter.Instance.WriteToLog("HElp.xaml ::  Legal_Tap , Exception : " + ex.StackTrace);
            }
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
            catch (Exception ex)
            {
                Logging.LogWriter.Instance.WriteToLog("HElp.xaml ::  rateAndReview_Tap , Exception : " + ex.StackTrace);
            }
        }

        private void SystemHealth_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.SYSTEM_HEALTH_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch (Exception ex)
            {
                Logging.LogWriter.Instance.WriteToLog("HElp.xaml ::  SystemHealth_Tap , Exception : " + ex.StackTrace);
            }
        }
    }
}