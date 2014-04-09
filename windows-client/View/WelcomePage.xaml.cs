﻿using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using Microsoft.Phone.Shell;
using System.Net.NetworkInformation;
using Microsoft.Phone.Tasks;
using windows_client.DbUtils;
using System.Diagnostics;
using windows_client.Languages;


namespace windows_client
{
    public partial class WelcomePage : PhoneApplicationPage
    {
        private bool isClicked = false;
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;


        public WelcomePage()
        {
            InitializeComponent();
            App.createDatabaseAsync();
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.WelcomePage_Accept_AppBar;
            nextIconButton.Click += new EventHandler(getStarted_click);
            appBar.Buttons.Add(nextIconButton);
            welcomePage.ApplicationBar = appBar;

            // if addbook is not scanned and state is not scanning
            if (!App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED) && ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING)
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));

            if (!App.IS_MARKETPLACE)
            {
                serverTxtBlk.Visibility = System.Windows.Visibility.Visible;
                if (!AccountUtils.IsProd)
                    serverTxtBlk.Text = "staging";
                else
                    serverTxtBlk.Text = "production";
            }
        }

        private void signupPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //if (AccountUtils.IsProd) // server is prod , change to staging
            //{
            //    AccountUtils.IsProd = false;
            //    serverTxtBlk.Text = "staging";
            //}
            //else
            //{
            //    AccountUtils.IsProd = true;
            //    serverTxtBlk.Text = "production";
            //}
        }

        private void getStarted_click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            if (!App.IS_MARKETPLACE) // this is done to save the server info
                App.appSettings.Save();

            #region SERVER INFO
            string env = (AccountUtils.IsProd) ? "PRODUCTION" : "STAGING";
            Debug.WriteLine("SERVER SETTING : " + env);
            Debug.WriteLine("HOST : " + AccountUtils.HOST);
            Debug.WriteLine("PORT : " + AccountUtils.PORT);
            Debug.WriteLine("MQTT HOST : " + AccountUtils.MQTT_HOST);
            Debug.WriteLine("MQTT PORT : " + AccountUtils.MQTT_PORT);
            #endregion
            NetworkErrorTxtBlk.Opacity = 0;
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                NetworkErrorTxtBlk.Opacity = 1;
                return;
            }

            nextIconButton.IsEnabled = false;
            try
            {
                if (App.appSettings.Contains(App.IS_DB_CREATED)) // if db is created then only delete tables.
                    MiscDBUtil.clearDatabase();
                //App.clearAllDatabasesAsync(); // this is async function and runs on the background thread.
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WelcomePage.xaml :: getStarted_click, Exception : " + ex.StackTrace);
            }
            isClicked = true;
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(registerPostResponse_Callback));
        }

        private void registerPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if ((obj == null))
            {
                //logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NetworkErrorTxtBlk.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    nextIconButton.IsEnabled = true;
                });
                isClicked = false;
                return;
            }
            /* This case is when you are on wifi and need to go to fallback screen to register.*/
            if (HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
            }
            /* account creation successfull */
            else
            {
                utils.Utils.savedAccountCredentials(obj);
                if (App.MSISDN.StartsWith(HikeConstants.INDIA_COUNTRY_CODE))
                    App.WriteToIsoStorageSettings(App.COUNTRY_CODE_SETTING, HikeConstants.INDIA_COUNTRY_CODE);
                nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
                /* scan contacts and post addressbook on server*/
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            }

            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (App.IS_TOMBSTONED)
            {
                if (this.State.ContainsKey("NetworkErrorTxtBlk.Opacity"))
                    NetworkErrorTxtBlk.Opacity = (int)this.State["NetworkErrorTxtBlk.Opacity"];
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            string uri = e.Uri.ToString();
            Debug.WriteLine("Welcome :: " + uri);
            if (!uri.Contains("View")) // this means app is not navigating to a new page so save for tombstone
            {
                this.State["NetworkErrorTxtBlk.Opacity"] = (int)NetworkErrorTxtBlk.Opacity;
            }
            else
                App.IS_TOMBSTONED = false;
        }

        private void Privacy_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask webBrowserTask = new WebBrowserTask();
            webBrowserTask.Uri = new Uri(HikeConstants.TERMS_LINK, UriKind.Absolute);
            try
            {
                webBrowserTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("WelcomePAge.xaml :: Privacy_Tap, Exception : " + ex.StackTrace);
            }
        }
    }
}