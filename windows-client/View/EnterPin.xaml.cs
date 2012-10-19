using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.IO.IsolatedStorage;
using Newtonsoft.Json.Linq;
using System.Windows.Media;
using Microsoft.Phone.Shell;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace windows_client
{
    public partial class EnterPin : PhoneApplicationPage
    {
        bool isNextClicked = false;
        string pinEntered;
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;

        public EnterPin()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(EnterPinPage_Loaded);

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_next.png", UriKind.Relative);
            nextIconButton.Text = "Next";
            nextIconButton.Click += new EventHandler(btnEnterPin_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterPin.ApplicationBar = appBar;
        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            if (isNextClicked)
                return;
            isNextClicked = true;
            pinEntered = txtBxEnterPin.Text.Trim();
            if (string.IsNullOrEmpty(pinEntered))
                return;
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                pinErrorTxt.Text = "Connectivity issue.";
                pinErrorTxt.Visibility = System.Windows.Visibility.Visible;
                isNextClicked = false;
                return;
            }
            txtBxEnterPin.IsReadOnly = true;
            nextIconButton.IsEnabled = false;
            string unAuthMsisdn = (string)App.appSettings[App.MSISDN_SETTING];
            pinErrorTxt.Visibility = System.Windows.Visibility.Collapsed;
            progressBar.Opacity = 1;
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
                    pinErrorTxt.Visibility = System.Windows.Visibility.Visible;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    txtBxEnterPin.IsReadOnly = false;
                    if (!string.IsNullOrWhiteSpace(pinEntered))
                        nextIconButton.IsEnabled = true;
                });
                isNextClicked = false;
                return;
            }

            utils.Utils.savedAccountCredentials(obj);

            /*Before calling setName function , simply scan the addressbook*/
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            nextPage = new Uri("/View/EnterName.xaml", UriKind.Relative);
            /*This is used to avoid cross thread invokation exception*/
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                txtBxEnterPin.IsReadOnly = false;
                PhoneApplicationService.Current.State.Remove("EnteredPhone");
                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            });
        }

        void EnterPinPage_Loaded(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Focus();
        }

        private void txtBxEnterPin_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Hint = "Pin";
            txtBxEnterPin.Foreground = UI_Utils.Instance.SignUpForeground;
        }

        private void btnWrongMsisdn_Click(object sender, RoutedEventArgs e)
        {
            goBackLogic();
        }

        private void goBackLogic()
        {
            if (isNextClicked)
                return;
            App.RemoveKeyFromAppSettings(App.MSISDN_SETTING);

            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                try
                {
                    Uri nextPage = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                    NavigationService.Navigate(nextPage);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception handled in page EnterPin Screen : " + e.StackTrace);
                }
            }
        }

        private void txtBxEnterPin_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtBxEnterPin.Text))
            {
                nextIconButton.IsEnabled = true;
                txtBxEnterPin.Foreground = UI_Utils.Instance.SignUpForeground;
            }
            else
                nextIconButton.IsEnabled = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if(NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;
                if (this.State.TryGetValue("txtBxEnterPin", out obj))
                {
                    txtBxEnterPin.Text = (string)obj;
                    txtBxEnterPin.Select(txtBxEnterPin.Text.Length, 0);
                    obj = null;
                }

                if (this.State.TryGetValue("pinErrorTxt.Visibility", out obj))
                {
                    pinErrorTxt.Visibility = (Visibility)obj;
                    pinErrorTxt.Text = (string)this.State["pinErrorTxt.Text"];
                }
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            string uri = e.Uri.ToString();
            if (!uri.Contains("View"))
            {
                if (!string.IsNullOrWhiteSpace(txtBxEnterPin.Text))
                    this.State["txtBxEnterPin"] = txtBxEnterPin.Text;
                else
                    this.State.Remove("txtBxEnterPin");

                if (pinErrorTxt.Visibility == System.Windows.Visibility.Visible)
                {
                    this.State["pinErrorTxt.Text"] = pinErrorTxt.Text;
                    this.State["pinErrorTxt.Visibility"] = pinErrorTxt.Visibility;
                }
                else
                {
                    this.State.Remove("pinErrorTxt.Text");
                    this.State.Remove("pinErrorTxt.Visibility");
                }
            }
            else
                App.IS_TOMBSTONED = false;
        }

        private void txtBxEnterPin_LostFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterPin.Background = UI_Utils.Instance.White;
        }

    }
}