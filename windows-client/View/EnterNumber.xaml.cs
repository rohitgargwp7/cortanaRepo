using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using System.Windows.Media;

namespace windows_client
{
    public partial class EnterNumber : PhoneApplicationPage
    {
        private static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 227, 227, 223));


        public EnterNumber()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterNumberPage_Loaded);
        }

        private void enterPhoneBtn_Click(object sender, RoutedEventArgs e)
        {
            string phoneNumber = txtEnterPhone.Text;
            enterPhoneBtn.Content = "Verifying your number";
            msisdnErrorTxt.Visibility = Visibility.Collapsed;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            AccountUtils.validateNumber(phoneNumber, new AccountUtils.postResponseFunction(msisdnPostResponse_Callback));         
        }

        private void msisdnPostResponse_Callback(JObject obj)
        {
            if (obj == null)
            {
                logger.Info("HTTP", "Unable to Validate Phone Number.");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    msisdnErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                return;
            }
            string unauthedMSISDN = (string)obj["msisdn"];
            if (unauthedMSISDN == null)
            {
                logger.Info("SignupTask", "Unable to send PIN to user");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    msisdnErrorTxt.Text = "Unable to send PIN to user";
                    msisdnErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                return;
            }
            /*If all well*/
            logger.Info("HTTP", "Successfully validated phone number.");
            appSettings[App.MSISDN_SETTING]=unauthedMSISDN;
            appSettings.Save();
            Uri nextPage = new Uri("/View/EnterPin.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation*/
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
            while(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }
        void EnterNumberPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Focus();
        }

        private void txtEnterPhone_GotFocus(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Text = "";
            txtEnterPhone.Background = textBoxBackground;
        }
    }
}