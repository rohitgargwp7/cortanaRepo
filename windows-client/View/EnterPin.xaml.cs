using System;
using System.Windows;
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
            this.Loaded += new RoutedEventHandler(EnterPinPage_Loaded);
        }

        private void btnEnterPin_Click(object sender, RoutedEventArgs e)
        {  
            string pinEntered =  txtBxEnterPin.Text;
            string unAuthMsisdn = (string)appSettings["msisdn"];
            pinErrorTxt.Visibility = Visibility.Collapsed;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            AccountUtils.registerAccount(pinEntered, unAuthMsisdn, new AccountUtils.postResponseFunction(pinPostResponse_Callback)); 
        }

        private void pinPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || "fail" == (string)obj["stat"])
            {
                logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    pinErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                return;
            }
            appSettings[App.PIN_SETTING] = "y";
            appSettings.Save();
            utils.Utils.savedAccountCredentials(obj);
           
            /*Before calling setName function , simply scan the addressbook*/
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
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

        void EnterPinPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Focus();
        }
 
    }
}