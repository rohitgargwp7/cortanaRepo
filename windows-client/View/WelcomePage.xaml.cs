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
            AccountUtils.registerAccount(null, null, new AccountUtils.postResponseFunction(registerPostResponse_Callback));           
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

            appSettings[HikeMessengerApp.ACCEPT_TERMS] = "y";
            /* This case is when you are on wifi and need to go to fallback screen to register.*/
            if ("fail" == (string)obj["stat"])
            {
                nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
            }
            else // account creation successfull
            {
                string token = (string)obj["token"];
                string msisdn = (string)obj["msisdn"];
                string uid = (string)obj["uid"];
                string sc = (string)obj[NetworkManager.SMS_CREDITS];
                int smsCredits = Int32.Parse(sc);
                utils.Utils.savedAccountCredentials(new AccountUtils.AccountInfo(token, msisdn, uid, smsCredits));
                nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
            }
            /*This is used to avoid cross thread invokation*/
            Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(nextPage); });
        }
    }
}