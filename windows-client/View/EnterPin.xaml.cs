﻿using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using Microsoft.Phone.Shell;

namespace windows_client
{
    public partial class EnterPin : PhoneApplicationPage
    {
        bool isTSorFirstLaunch = false;
        string pinEntered;
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public EnterPin()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterPinPage_Loaded);

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "Next";
            nextIconButton.Click += new EventHandler(btnEnterPin_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterPin.ApplicationBar = appBar;
            isTSorFirstLaunch = true;
        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            pinEntered = txtBxEnterPin.Text.Trim();
            if (string.IsNullOrEmpty(pinEntered))
                return;
            nextIconButton.IsEnabled = false;
            string unAuthMsisdn = (string)App.appSettings[App.MSISDN_SETTING];
            pinErrorTxt.Opacity = 0;
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            AccountUtils.registerAccount(pinEntered, unAuthMsisdn, new AccountUtils.postResponseFunction(pinPostResponse_Callback));
        }

        private void pinPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || "fail" == (string)obj["stat"])
            {
                // logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    pinErrorTxt.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                });
                if (!string.IsNullOrWhiteSpace(pinEntered))
                    nextIconButton.IsEnabled = true;
                return;
            }

            utils.Utils.savedAccountCredentials(obj);

            /*Before calling setName function , simply scan the addressbook*/
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            });
        }

        void EnterPinPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Focus();
        }

        private void txtBxEnterPin_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Hint = "Pin";
            txtBxEnterPin.Foreground = textBoxBackground;
        }

        private void btnWrongMsisdn_Click(object sender, RoutedEventArgs e)
        {
            goBackLogic();
        }

        private void goBackLogic()
        {
            App.appSettings.Remove(App.MSISDN_SETTING);
            App.appSettings.Save();

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                Uri nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                NavigationService.Navigate(nextPage);
            }
        }

        private void txtBxEnterPin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtBxEnterPin.Text))
                nextIconButton.IsEnabled = true;
            else
                nextIconButton.IsEnabled = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (isTSorFirstLaunch) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;
                if (this.State.TryGetValue("txtBxEnterPin", out obj))
                {
                    txtBxEnterPin.Text = (string)obj;
                    txtBxEnterPin.Select(txtBxEnterPin.Text.Length, 0);
                    obj = null;
                }

                if (this.State.TryGetValue("pinErrorTxt.Opacity", out obj))
                {
                    pinErrorTxt.Opacity = (int)obj;
                    pinErrorTxt.Text = (string)this.State["pinErrorTxt.Text"];
                }
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (!string.IsNullOrWhiteSpace(txtBxEnterPin.Text))
                this.State["txtBxEnterPin"] = txtBxEnterPin.Text;
            else
                this.State.Remove("txtBxEnterPin");

            if (pinErrorTxt.Opacity == 1)
            {
                this.State["pinErrorTxt.Text"] = pinErrorTxt.Text;
                this.State["pinErrorTxt.Opacity"] = pinErrorTxt.Opacity;
            }
            else
            {
                this.State.Remove("pinErrorTxt.Text");
                this.State.Remove("pinErrorTxt.Opacity");
            }
        }

    }
}