using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Text;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private string ac_name;
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 227, 227, 223));


        public EnterName()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterNamePage_Loaded);
            App.appSettings[App.PAGE_STATE] = App.PageState.SETNAME_SCREEN;
            App.appSettings.Save();
        }

        private void btnEnterName_Click(object sender, RoutedEventArgs e)
        {
            ac_name = txtBxEnterName.Text;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            enterNameBtn.Content = "Scanning contacts";
            AccountUtils.setName(ac_name, new AccountUtils.postResponseFunction(setName_Callback));
        }

        private void setName_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || "ok" != (string)obj["stat"])
            {
                //logger.Info("HTTP", "Unable to set name");
                // SHOW SOME TRY AGAIN MSG etc
                return;
            }
           
            nextPage = new Uri("/View/ConversationsList.xaml", UriKind.Relative);

            int count = 1;
            while (!App.Ab_scanned && count <=20)
            {
                if (!App.isABScanning)
                {
                    ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
                }
                Thread.Sleep(1 * 1000); //sleep for one second
                count++;
            }
            if (!App.Ab_scanned) // timeout occured
            {
                // SHOW NETWORK ERROR
                return;
            }

            App.appSettings[App.ACCOUNT_NAME] = ac_name;
            App.appSettings[App.PAGE_STATE] = App.PageState.CONVLIST_SCREEN;
            App.appSettings.Save();

            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() => 
            {
                enterNameBtn.Content = "Getting you in";
                Thread.Sleep(3 * 1000);
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

        void EnterNamePage_Loaded(object sender, RoutedEventArgs e)
        {
            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
            msisdn = msisdn.Substring(msisdn.Length - 10);
            StringBuilder userMsisdn = new StringBuilder();
            userMsisdn.Append(msisdn.Substring(0, 3)).Append("-").Append(msisdn.Substring(3, 3)).Append("-").Append(msisdn.Substring(6)).Append("!");
            txtBlckPhoneNumber.Text = userMsisdn.ToString();
            txtBxEnterName.Focus();
        }

        private void txtBxEnterName_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterName.Background = textBoxBackground;
            txtBxEnterName.Hint = "Name";
        }
    }
}