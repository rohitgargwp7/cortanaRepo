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
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "accept";
            nextIconButton.Click += new EventHandler(getStarted_click);
            appBar.Buttons.Add(nextIconButton);
            welcomePage.ApplicationBar = appBar;
        }

        private void getStarted_click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
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
            catch { }
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
            if ("fail" == (string)obj["stat"])
            {
                nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
            }
            /* account creation successfull */
            else
            {
                utils.Utils.savedAccountCredentials(obj);
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
            Debug.WriteLine("Welcome :: "+uri);
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
            webBrowserTask.Show();
        }
    }
}