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
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public EnterName()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterNamePage_Loaded);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.SETNAME_SCREEN);
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = "done";
            nextIconButton.Click += new EventHandler(btnEnterPin_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterName.ApplicationBar = appBar;

        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            nextIconButton.IsEnabled = false;
            ac_name = txtBxEnterName.Text.Trim();
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            enterNameBtn.Opacity = 1;
            enterNameBtn.Text = "Scanning contacts.";
            AccountUtils.setName(ac_name, new AccountUtils.postResponseFunction(setName_Callback));
        }

        private void setName_Callback(JObject obj)
        {
            if (obj == null || "ok" != (string)obj["stat"])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                        progressBar.Visibility = System.Windows.Visibility.Collapsed;
                        progressBar.IsEnabled = false;
                        if (!string.IsNullOrWhiteSpace(ac_name))
                            nextIconButton.IsEnabled = true;
                        enterNameBtn.Text = "Error !! Name not set.... Try Again";
                });
                return;
            }
            App.WriteToIsoStorageSettings(App.ACCOUNT_NAME, ac_name);
        }

        public void processEnterName()
        {
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
            Uri nextPage = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
            enterNameBtn.Text = "Getting you in";
            Thread.Sleep(2 * 1000);
            PhoneApplicationService.Current.State[HikeConstants.IS_NEW_INSTALLATION] = true;
            NavigationService.Navigate(nextPage);
            progressBar.Visibility = System.Windows.Visibility.Collapsed;
            progressBar.IsEnabled = false;
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
            txtBxEnterName.Hint = "Name";
            txtBxEnterName.Foreground = textBoxBackground;
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtBxEnterName.Text))
                nextIconButton.IsEnabled = true;
            else
                nextIconButton.IsEnabled = false;
        }
    }
}