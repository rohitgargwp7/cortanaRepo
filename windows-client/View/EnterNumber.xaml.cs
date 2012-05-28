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
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;

namespace windows_client
{
    public partial class EnterNumber : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public EnterNumber()
        {
            InitializeComponent(); 
        }

        private void EnteredNumber_Click(object sender, RoutedEventArgs e)
        {
            //read number from text box
            String phoneNumber = txtEnterPhone.Text;
            AccountUtils.validateNumber(phoneNumber, new AccountUtils.postResponseFunction(msisdnPostResponse_Callback));         
        }

        private void msisdnPostResponse_Callback(JObject obj)
        {
            if (obj == null)
            {
                logger.Info("HTTP", "Unable to Validate Phone Number.");
                // raise an exception , or handle this properly
                return;
            }
            string unauthedMSISDN = (string)obj["msisdn"];
            if (unauthedMSISDN == null)
            {
                logger.Info("SignupTask", "Unable to send PIN to user");
                return;
            }
            /*If all well*/
            logger.Info("HTTP", "Successfully validated phone number.");
            appSettings[HikeMessengerApp.MSISDN_SETTING]=true;
            appSettings.Save();
            Uri nextPage = new Uri("/View/EnterPin.xaml?msisdn="+unauthedMSISDN, UriKind.Relative);
            /*This is used to avoid cross thread invokation*/
            Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(nextPage); });
        }
    }
}