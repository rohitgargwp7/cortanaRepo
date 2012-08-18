using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using System.Text;
using Microsoft.Phone.Shell;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private string ac_name;
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 227, 227, 223));
        private ApplicationBar appBar;


        public EnterName()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterNamePage_Loaded);
            App.appSettings[App.PAGE_STATE] = App.PageState.SETNAME_SCREEN;
            App.appSettings.Save();

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            ApplicationBarIconButton composeIconButton = new ApplicationBarIconButton();
            composeIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            composeIconButton.Text = "done";
            composeIconButton.Click += new EventHandler(btnEnterPin_Click);
            composeIconButton.IsEnabled = true;
            appBar.Buttons.Add(composeIconButton);
            enterName.ApplicationBar = appBar;

        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            ac_name = txtBxEnterName.Text;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            enterNameBtn.Opacity = 1;
            enterNameBtn.Text = "Scanning contacts";
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
                enterNameBtn.Text = "Getting you in";
                Thread.Sleep(3 * 1000);
                PhoneApplicationService.Current.State[HikeConstants.IS_NEW_INSTALLATION] = true;
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