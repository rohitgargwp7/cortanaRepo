using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace windows_client
{
    public partial class EnterPin : PhoneApplicationPage
    {
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 227, 227, 223));

        public EnterPin()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterPinPage_Loaded);
            App.appSettings[App.PAGE_STATE] = App.PageState.PIN_SCREEN;
            App.appSettings.Save();
        }

        private void btnEnterPin_Click(object sender, RoutedEventArgs e)
        {  
            string pinEntered =  txtBxEnterPin.Text;
            string unAuthMsisdn = (string)App.appSettings[App.MSISDN_SETTING];
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
               // logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    pinErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                return;
            }
            
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

        void EnterPinPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Focus();
        }

        private void txtBxEnterPin_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Background = textBoxBackground;
            txtBxEnterPin.Hint = "Pin";
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            goBackLogic(); 
        }
        private void btnWrongMsisdn_Click(object sender, RoutedEventArgs e)
        {
            goBackLogic();          
        }

        private void goBackLogic()
        {
            App.appSettings.Remove(App.MSISDN_SETTING);
            App.appSettings.Save();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                Uri nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                NavigationService.Navigate(nextPage);
            }            
        }
    }
}