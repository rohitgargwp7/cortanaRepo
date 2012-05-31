using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.UserData;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;


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
        }

        private void getStarted_click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetStarted.Content = "Pulling your digits.";
                progressBar.Visibility = System.Windows.Visibility.Visible;
                progressBar.IsEnabled = true;
                AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(registerPostResponse_Callback));
            }
            catch(Exception ex)
            {
            }
        }

        private void registerPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if ((obj == null))
            {
                logger.Info("HTTP", "Unable to create account");
                // SHOW SOME TRY AGAIN MSG
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
    }
}