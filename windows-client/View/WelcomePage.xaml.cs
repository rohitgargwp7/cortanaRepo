using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using Microsoft.Phone.Shell;


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
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "next";
            nextIconButton.Click += new EventHandler(getStarted_click);
            appBar.Buttons.Add(nextIconButton);
            welcomePage.ApplicationBar = appBar;

        }

        private void getStarted_click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            if(App.appSettings.Contains(App.IS_DB_CREATED)) // if db is created then only delete tables.
                App.clearAllDatabasesAsync(); // this is async function and runs on the background thread.
            isClicked = true;
            progressBar.Visibility = System.Windows.Visibility.Visible;
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
                    //GetStarted.Content = "Network Error. Try Again.";
                    //GetStarted.Foreground = new SolidColorBrush(Colors.Red);
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;
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
                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.IsEnabled = false; 
            });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
    }
}