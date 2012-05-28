using System;
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
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;

namespace windows_client
{
    public partial class EnterPin : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public EnterPin()
        {
            InitializeComponent();
        }

        private void btnEnterPin_Click(object sender, RoutedEventArgs e)
        {  
            string pinEntered =  txtBxEnterPin.Text;
            string unAuthMsisdn = this.NavigationContext.QueryString["msisdn"];
            AccountUtils.registerAccount(pinEntered, unAuthMsisdn, new AccountUtils.postResponseFunction(pinPostResponse_Callback)); 
        }

        private void pinPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || "fail" == (string)obj["stat"])
            {
                logger.Info("HTTP", "Unable to create account");
                // SHOW SOME TRY AGAIN MSG OR MOVE TO MSISDN PAGE
                return;
            }
            appSettings[HikeMessengerApp.PIN_SETTING] = "y";
            string token = (string)obj["token"];
            string msisdn = (string)obj["msisdn"];
            string uid = (string)obj["uid"];
            int smsCredits= (int)obj[NetworkManager.SMS_CREDITS];
            utils.Utils.savedAccountCredentials(new AccountUtils.AccountInfo(token,msisdn,uid,smsCredits));
            nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(nextPage); });
        }
    }
}