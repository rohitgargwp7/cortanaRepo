using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;


namespace windows_client
{
    public partial class WelcomePage : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // Constructor
        public WelcomePage()
        {
            InitializeComponent();
            Test.Test.AddContactEntries();
            Test.Test.insertMessages();
        }

        private void getStarted_click(object sender, RoutedEventArgs e)
        {
            GetStarted.Content = "Pulling your digits.";
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(registerPostResponse_Callback));
        }

        private void registerPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if ((obj == null))
            {
                logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    GetStarted.Content = "Network Error. Try Again.";
                    GetStarted.Foreground = new SolidColorBrush(Colors.Red);
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                return;
            }
            appSettings[App.ACCEPT_TERMS] = "y";
            /* This case is when you are on wifi and need to go to fallback screen to register.*/
            if ("fail" == (string)obj["stat"])
            {
                nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
            }
            /* account creation successfull */
            else 
            {
                appSettings[App.PIN_SETTING] = "y";
                appSettings.Save();
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