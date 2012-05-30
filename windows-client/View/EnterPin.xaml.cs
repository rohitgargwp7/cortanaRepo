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
using Microsoft.Phone.UserData;
using windows_client.Model;

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
            string unAuthMsisdn = (string)appSettings["msisdn"];
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
            appSettings.Save();
            utils.Utils.savedAccountCredentials(obj);
           
            /*Before calling setName function , simply scan the addressbook*/
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() => { NavigationService.Navigate(nextPage); });
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
    }
}