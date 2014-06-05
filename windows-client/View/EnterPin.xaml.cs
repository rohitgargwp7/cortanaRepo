using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Shell;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Windows.Threading;
using windows_client.Languages;
using System.Windows.Media;

namespace windows_client
{
    public partial class EnterPin : PhoneApplicationPage
    {
        bool isNextClicked = false;
        string pinEntered;
        private ApplicationBar appBar;
        ApplicationBarIconButton nextIconButton;
        private DispatcherTimer progressTimer;
        private int timerValue = 180;
        private readonly string CallMeTimer = "CallMeTimer";

        public EnterPin()
        {
            InitializeComponent();

            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.Click += new EventHandler(btnEnterPin_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            ApplicationBar = appBar;
            if (!App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED) && ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING)
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));
        }

        private void btnEnterPin_Click(object sender, EventArgs e)
        {
            if (isNextClicked)
                return;
            pinEntered = txtBxEnterPin.Text.Trim();
            if (string.IsNullOrEmpty(pinEntered))
                return;
            isNextClicked = true;
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                pinErrorTxt.Text = AppResources.Connectivity_Issue;
                pinErrorTxt.Opacity = 1;
                isNextClicked = false;
                return;
            }
            txtBxEnterPin.IsReadOnly = true;
            nextIconButton.IsEnabled = false;
            string unAuthMsisdn = (string)App.appSettings[App.MSISDN_SETTING];
            pinErrorTxt.Opacity = 0;
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            AccountUtils.registerAccount(pinEntered, unAuthMsisdn, new AccountUtils.postResponseFunction(pinPostResponse_Callback));
        }

        private void pinPostResponse_Callback(JObject obj)
        {
            Uri nextPage = null;

            if (obj == null || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                // logger.Info("HTTP", "Unable to create account");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    pinErrorTxt.Text = AppResources.EnterPin_PinError_TxtBlk;
                    pinErrorTxt.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    txtBxEnterPin.IsReadOnly = false;
                    if (!string.IsNullOrWhiteSpace(pinEntered))
                        nextIconButton.IsEnabled = true;
                    txtBxEnterPin.Select(txtBxEnterPin.Text.Length, 0);
                    isNextClicked = false;
                });
                return;
            }

            utils.Utils.savedAccountCredentials(obj);
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
            if (!string.IsNullOrWhiteSpace(txtBxEnterPin.Text) && txtBxEnterPin.Text.Length > 2)
                nextIconButton.IsEnabled = true;
            else
                nextIconButton.IsEnabled = false;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (progressTimer != null)
            {
                progressTimer.Stop();
                progressTimer = null;
            }

            PhoneApplicationService.Current.State[CallMeTimer] = timerValue;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            progressTimer = new DispatcherTimer();
            progressTimer.Interval = TimeSpan.FromSeconds(1);
            progressTimer.Tick += new EventHandler(enableCallMeOption);
            progressTimer.Start();

            if (PhoneApplicationService.Current.State.ContainsKey(CallMeTimer))
            {
                timerValue = (int)PhoneApplicationService.Current.State[CallMeTimer];
                PhoneApplicationService.Current.State.Remove(CallMeTimer);
            }

            callMeButton.Content = timerValue > 0 ? String.Format(AppResources.EnterPin_CallMe_Btn_Timer, (timerValue / 60).ToString("00") + ":" + (timerValue % 60).ToString("00")) : AppResources.EnterPin_CallMe_Btn;
            callMeButton.IsEnabled = timerValue == 0;

            if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;
                if (this.State.TryGetValue("txtBxEnterPin", out obj))
                {
                    txtBxEnterPin.Text = (string)obj;
                    txtBxEnterPin.Select(txtBxEnterPin.Text.Length, 0);
                    obj = null;
                }

                if (this.State.TryGetValue("pinErrorTxt.Opacity", out obj))
                {
                    pinErrorTxt.Opacity = (double)obj;
                    pinErrorTxt.Text = (string)this.State["pinErrorTxt.Text"];
                    obj = null;
                }
                if (this.State.TryGetValue("callMe.Opacity", out obj))
                {
                    callMe.Opacity = (int)this.State["callMe.Opacity"];
                    obj = null;
                }
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                txtBxEnterPin.Hint = AppResources.EnterPin_PinHint;
            });
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

                if (pinErrorTxt.Opacity == 1)
                {
                    this.State["pinErrorTxt.Text"] = pinErrorTxt.Text;
                    this.State["pinErrorTxt.Opacity"] = pinErrorTxt.Opacity;
                }
                else
                {
                    this.State.Remove("pinErrorTxt.Text");
                    this.State.Remove("pinErrorTxt.Opacity");
                }
                if (callMe.Opacity == 1)
                {
                    this.State["callMe.Opacity"] = (int)callMe.Opacity;
                }
                else
                    this.State.Remove("callMe.Opacity");
            }
            else
                App.IS_TOMBSTONED = false;
        }

        private void enableCallMeOption(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (timerValue > 0)
                {
                    timerValue--;
                    callMeButton.Content = String.Format(AppResources.EnterPin_CallMe_Btn_Timer, (timerValue / 60).ToString("00") + ":" + (timerValue % 60).ToString("00"));
                }
                if (timerValue == 0 && callMeButton.IsEnabled == false)
                {
                    callMeButton.Content = AppResources.EnterPin_CallMe_Btn;
                    callMeButton.IsEnabled = true;
                    return;
                }
            });
        }

        private void callMe_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (callMe.Opacity == 1)
            {
                string msisdn;
                App.appSettings.TryGetValue<string>(App.MSISDN_SETTING, out msisdn);
                AccountUtils.postForCallMe(msisdn, new AccountUtils.postResponseFunction(callMePostResponse_Callback));
                MessageBox.Show(AppResources.EnterPin_CallingMsg_MsgBox);
            }
        }

        private void callMePostResponse_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                stat = statusToken.ToString();
            }
            if (stat != HikeConstants.OK)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.EnterPin_CallErrorMsg_MsgBox);
                });

            }
        }

    }
}