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
using System.Net.NetworkInformation;
using System.Diagnostics;
using windows_client.Languages;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        public bool isClicked = false;
        private string ac_name;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;

        public EnterName()
        {
            InitializeComponent();
            App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, Utils.getAppVersion());// new install so write version
            App.RemoveKeyFromAppSettings(App.ACCOUNT_NAME);
            App.RemoveKeyFromAppSettings(App.SET_NAME_FAILED);
            if (!App.appSettings.Contains(App.IS_ADDRESS_BOOK_SCANNED) && !App.isABScanning)
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));

            this.Loaded += new RoutedEventHandler(EnterNamePage_Loaded);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.SETNAME_SCREEN);
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Done_Btn;
            nextIconButton.Click += new EventHandler(btnEnterPin_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterName.ApplicationBar = appBar;
        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            isClicked = true;
            nameErrorTxt.Visibility = Visibility.Collapsed;
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                msgTxtBlk.Opacity = 0;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                nameErrorTxt.Text = AppResources.Connectivity_Issue;
                nameErrorTxt.Visibility = Visibility.Visible;
                App.RemoveKeyFromAppSettings(App.ACCOUNT_NAME);
                App.WriteToIsoStorageSettings(App.SET_NAME_FAILED, true);
                return;
            }
            if (App.appSettings.Contains(App.CONTACT_SCANNING_FAILED))
            {
                App.RemoveKeyFromAppSettings(App.CONTACT_SCANNING_FAILED);
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
            }

            txtBxEnterName.IsReadOnly = true;
            nextIconButton.IsEnabled = false;
            ac_name = txtBxEnterName.Text.Trim();
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            msgTxtBlk.Opacity = 1;
            msgTxtBlk.Text = AppResources.EnterName_ScanningContacts_Txt;
            AccountUtils.setName(ac_name, new AccountUtils.postResponseFunction(setName_Callback));
        }

        private void setName_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.OK != (string)obj[HikeConstants.STAT])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    txtBxEnterName.IsReadOnly = false; ;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    if (!string.IsNullOrWhiteSpace(ac_name))
                        nextIconButton.IsEnabled = true;
                    msgTxtBlk.Opacity = 0;
                    nameErrorTxt.Text = AppResources.EnterName_NameErrorTxt;
                    nameErrorTxt.Visibility = Visibility.Visible;
                    App.RemoveKeyFromAppSettings(App.ACCOUNT_NAME);
                    App.WriteToIsoStorageSettings(App.SET_NAME_FAILED, true);
                });
                return;
            }
            App.WriteToIsoStorageSettings(App.ACCOUNT_NAME, ac_name);
            App.RemoveKeyFromAppSettings(App.SET_NAME_FAILED);
            if (App.appSettings.Contains(App.IS_ADDRESS_BOOK_SCANNED)) // shows that addressbook is already scanned
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    processEnterName();
                });
            }
        }
        bool isCalled = false;
        public void processEnterName()
        {
            if (isCalled)
                return;
            isCalled = true;
            txtBxEnterName.IsReadOnly = false;

            App.WriteToIsoStorageSettings(App.SHOW_FAVORITES_TUTORIAL, true);
            App.WriteToIsoStorageSettings(App.SHOW_NUDGE_TUTORIAL, true);
            
            Uri nextPage;
            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
            if (string.IsNullOrEmpty(country_code) || country_code == "+91")
            {
                App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, true);
            }
            else
            {
                App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, false);
            }
            if ("+91" != country_code)
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                nextPage = nextPage = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
            }
            else
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.WALKTHROUGH_SCREEN);
                nextPage = new Uri("/View/Walkthrough.xaml", UriKind.Relative);
                PhoneApplicationService.Current.State["FromNameScreen"] = true;
            }
            App.WriteToIsoStorageSettings(HikeConstants.IS_NEW_INSTALLATION, true);
            nameErrorTxt.Visibility = Visibility.Collapsed;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            Thread.Sleep(2 * 1000);
            try
            {
                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            }
            catch(Exception e)
            {
                Debug.WriteLine("Exception handled in page EnterName Screen : " + e.StackTrace);
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;
                if (this.State.TryGetValue("txtBxEnterName", out obj))
                {
                    txtBxEnterName.Text = (string)obj;
                    txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                    obj = null;
                }

                if (this.State.TryGetValue("nameErrorTxt.Visibility", out obj))
                {
                    nameErrorTxt.Visibility = (Visibility)obj;
                    nameErrorTxt.Text = (string)this.State["nameErrorTxt.Text"];
                }
            }
            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
            msisdn = msisdn.Substring(msisdn.Length - 10);
            StringBuilder userMsisdn = new StringBuilder();
            userMsisdn.Append(msisdn.Substring(0, 3)).Append("-").Append(msisdn.Substring(3, 3)).Append("-").Append(msisdn.Substring(6)).Append("!");

            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);


            txtBlckPhoneNumber.Text = " " + (country_code == null ? "+91" : country_code) + "-" + userMsisdn.ToString();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.COUNTRY_SELECTED))
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.COUNTRY_SELECTED);
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            string uri = e.Uri.ToString();
            if (!uri.Contains("View"))
            {

                if (!string.IsNullOrWhiteSpace(txtBxEnterName.Text))
                    this.State["txtBxEnterName"] = txtBxEnterName.Text;
                else
                    this.State.Remove("txtBxEnterName");

                if (msgTxtBlk.Opacity == 1)
                {
                    this.State["nameErrorTxt.Text"] = nameErrorTxt.Text;
                    this.State["nameErrorTxt.Visibility"] = nameErrorTxt.Visibility;
                }
                else
                {
                    this.State.Remove("nameErrorTxt.Text");
                    this.State.Remove("nameErrorTxt.Visibility");
                }
            }
            else
                App.IS_TOMBSTONED = false;
        }

        void EnterNamePage_Loaded(object sender, RoutedEventArgs e)
        {
            txtBxEnterName.Focus();
            this.Loaded -= EnterNamePage_Loaded;
        }

        private void txtBxEnterName_GotFocus(object sender, RoutedEventArgs e)
        {
            txtBxEnterName.Hint = AppResources.EnterName_Name_Hint;
            txtBxEnterName.Foreground = UI_Utils.Instance.SignUpForeground;
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(txtBxEnterName.Text))
            {
                nextIconButton.IsEnabled = true;
                txtBxEnterName.Foreground = UI_Utils.Instance.SignUpForeground;
            }
            else
                nextIconButton.IsEnabled = false;
        }

        private void txtBxEnterName_LostFocus(object sender, RoutedEventArgs e)
        {
            this.txtBxEnterName.Background = UI_Utils.Instance.White;
        }
    }
}