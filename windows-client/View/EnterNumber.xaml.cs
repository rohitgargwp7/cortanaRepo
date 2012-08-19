using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using Microsoft.Phone.Shell;

namespace windows_client
{
    public partial class EnterNumber : PhoneApplicationPage
    {
        string phoneNumber;
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 227, 227, 223));
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public EnterNumber()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterNumberPage_Loaded);

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "Next";
            nextIconButton.Click += new EventHandler(enterPhoneBtn_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterNumber.ApplicationBar = appBar;

        }

        private void enterPhoneBtn_Click(object sender, EventArgs e)
        {
            phoneNumber = txtEnterPhone.Text.Trim();
            if (String.IsNullOrEmpty(phoneNumber))
                return;
            nextIconButton.IsEnabled = false;
            enterPhoneBtn.Opacity = 1;
            enterPhoneBtn.Text = "Verifying your number";
            msisdnErrorTxt.Visibility = Visibility.Collapsed;
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            AccountUtils.validateNumber(phoneNumber, new AccountUtils.postResponseFunction(msisdnPostResponse_Callback));         
        }

        private void msisdnPostResponse_Callback(JObject obj)
        {
            if (obj == null)
            {
                //logger.Info("HTTP", "Unable to Validate Phone Number.");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    msisdnErrorTxt.Visibility = Visibility.Visible;
                    progressBar.Visibility = Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
                if (!string.IsNullOrWhiteSpace(phoneNumber))
                    nextIconButton.IsEnabled = true;
                return;
            }
            string unauthedMSISDN = (string)obj["msisdn"];
            if (unauthedMSISDN == null)
            {
                //logger.Info("SignupTask", "Unable to send PIN to user");
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
            App.appSettings[App.MSISDN_SETTING] = unauthedMSISDN;
            App.appSettings.Save();
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
            enterPhoneBtn.Opacity = 0;
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            Uri nextPage = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }
        void EnterNumberPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Focus();
        }

        private void txtEnterPhone_GotFocus(object sender, RoutedEventArgs e)
        {
            txtEnterPhone.Text = "";
            txtEnterPhone.Background = textBoxBackground;
            txtEnterPhone.Hint = "Phone Number";
        }

        private void txtEnterPhone_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtEnterPhone.Text))
                nextIconButton.IsEnabled = true;
            else
                nextIconButton.IsEnabled = false;
        }
    }
}