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

            if (App.ViewModel.IsDarkMode)
                madeInIndia.Source = UI_Utils.Instance.MadeInIndiaBlack;
            else
                madeInIndia.Source = UI_Utils.Instance.MadeInIndiaWhite;

            applicationVersion.Text = string.Format(AppResources.Help_AppVersionTitle, utils.Utils.getAppVersion());
        }

        private void FAQs_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.FAQS_LINK, UriKind.Absolute);
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
            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
            StringBuilder emailBodyText = new StringBuilder();
            emailBodyText.Append("\n\n\n\n\n").Append(AppResources.Help_EmailHikeVersion).Append(Utils.getAppVersion()).Append(
                "\n").Append(AppResources.Help_EmailOSVersion).Append(Utils.getOSVersion()).Append("\n").Append(AppResources.Help_EmailPhoneNo).
                Append(msisdn).Append("\n").Append(
                AppResources.Help_EmailDeviceModel).Append(Utils.getDeviceModel()).Append(
                "\n").Append(AppResources.Help_EmailCarrier).Append(DeviceNetworkInformation.CellularMobileOperator);
            EmailHelper.SendEmail(AppResources.Help_EmailSubject, emailBodyText.ToString(),"support@hike.in");
        }

        private void Legal_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.TERMS_LINK, UriKind.Absolute);
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
            webBrowserTask.Uri = new Uri(HikeConstants.SYSTEM_HEALTH_LINK, UriKind.Absolute);
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