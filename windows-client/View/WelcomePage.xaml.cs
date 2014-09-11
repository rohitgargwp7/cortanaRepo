using System;
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
using windows_client.Model;


namespace windows_client
{
    public partial class WelcomePage : PhoneApplicationPage
    {
        public WelcomePage()
        {
            InitializeComponent();
            App.createDatabaseAsync();

            // if addbook is not scanned and state is not scanning
            if (!App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED) && ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING)
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));

            if (!App.IS_MARKETPLACE)
            {
                serverTxtBlk.Visibility = System.Windows.Visibility.Visible;
                welcomePivot.Tap -= ChangeEnvironment;
                welcomePivot.Tap += ChangeEnvironment;

                if (AccountUtils.AppEnvironment == AccountUtils.DebugEnvironment.PRODUCTION)
                    serverTxtBlk.Text = "Production";
                else if (AccountUtils.AppEnvironment == AccountUtils.DebugEnvironment.DEV)
                    serverTxtBlk.Text = "Staging 2";
                else
                    serverTxtBlk.Text = "QA Staging";
            }
        }

        private void registerPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if ((obj == null))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    NetworkErrorTxtBlk.Opacity = 1;
                    progressBar.Opacity = 0;
                    getStartedButton.Opacity = 1;
                });

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

        private void Pivot_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (welcomePivot.SelectedIndex)
            {
                case 0:
                    p1.Fill = (SolidColorBrush)App.Current.Resources["HikeBlue"];
                    p2.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    p3.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    break;
                case 1:
                    p1.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    p2.Fill = (SolidColorBrush)App.Current.Resources["HikeBlue"];
                    p3.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];

                    Analytics.SendClickEvent(HikeConstants.FTUE_TUTORIAL_STICKER_VIEWED);
                    break;
                case 2:
                    p1.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    p2.Fill = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    p3.Fill = (SolidColorBrush)App.Current.Resources["HikeBlue"];

                    Analytics.SendClickEvent(HikeConstants.FTUE_TUTORIAL_CBG_VIEWED);
                    break;
            }
        }

        private void getStarted_Click(object sender, RoutedEventArgs e)
        {
            getStartedButton.Opacity = 0;

            if (!App.IS_MARKETPLACE) // this is done to save the server info
                App.appSettings.Save();

            #region SERVER INFO
            string env = AccountUtils.AppEnvironment.ToString();
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
                getStartedButton.Opacity = 1;
                NetworkErrorTxtBlk.Opacity = 1;
                return;
            }

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

            progressBar.Opacity = 1;

            AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(registerPostResponse_Callback));
        }

        private void ChangeEnvironment(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!App.IS_MARKETPLACE)
            {
                if (AccountUtils.AppEnvironment == AccountUtils.DebugEnvironment.STAGING)
                {
                    AccountUtils.AppEnvironment = AccountUtils.DebugEnvironment.DEV;
                    serverTxtBlk.Text = "Staging 2";
                }
                else if (AccountUtils.AppEnvironment == AccountUtils.DebugEnvironment.DEV)
                {
                    AccountUtils.AppEnvironment = AccountUtils.DebugEnvironment.PRODUCTION;
                    serverTxtBlk.Text = "Production";
                }
                else
                {
                    AccountUtils.AppEnvironment = AccountUtils.DebugEnvironment.STAGING;
                    serverTxtBlk.Text = "QA Staging";
                }
                App.WriteToIsoStorageSettings(HikeConstants.ServerUrls.APP_ENVIRONMENT_SETTING, AccountUtils.AppEnvironment);
            }
        }
    }
}