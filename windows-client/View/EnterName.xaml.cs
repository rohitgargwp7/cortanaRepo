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
using windows_client.Model;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private bool reloadImage = true;
        public bool isClicked = false;
        private string ac_name;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;
        BitmapImage profileImage = null;
        byte[] _avatar = null;
        bool isCalled = false;
        PhotoChooserTask photoChooserTask = null;

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
            nextIconButton.Click += new EventHandler(btnEnterName_Click);
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);
            enterName.ApplicationBar = appBar;

            avatarImage.Tap += OnProfilePicButtonTap;
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
            msisdn = msisdn.Substring(msisdn.Length - 10);
            StringBuilder userMsisdn = new StringBuilder();
            userMsisdn.Append(msisdn.Substring(0, 3)).Append("-").Append(msisdn.Substring(3, 3)).Append("-").Append(msisdn.Substring(6)).Append("!");
            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
            txtBlckPhoneNumber.Text = " " + (country_code == null ? "+91" : country_code) + "-" + userMsisdn.ToString();
        }

        private void btnEnterName_Click(object sender, EventArgs e)
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

            nextPage = new Uri("/View/WelcomeScreen.xaml", UriKind.Relative);

            App.WriteToIsoStorageSettings(HikeConstants.IS_NEW_INSTALLATION, true);
            nameErrorTxt.Visibility = Visibility.Collapsed;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            Thread.Sleep(2 * 1000);
            try
            {
                if (_avatar != null)
                {
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, _avatar, false);
                }
                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            }
            catch (Exception e)
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

            if (PhoneApplicationService.Current.State.ContainsKey("fbName"))
            {
                string name = PhoneApplicationService.Current.State["fbName"] as string;
                txtBxEnterName.Text = name;
                txtBxEnterName.Hint = string.Empty;
            }

            if (reloadImage) // this will handle both deactivation and tombstone
            {
                if (PhoneApplicationService.Current.State.ContainsKey("img"))
                {
                    _avatar = (byte[])PhoneApplicationService.Current.State["img"];
                    nextIconButton.IsEnabled = true;
                    reloadImage = false;
                }
                else
                    _avatar = MiscDBUtil.getThumbNailForMsisdn(HikeConstants.MY_PROFILE_PIC);

                if (_avatar != null)
                {
                    try
                    {
                        MemoryStream memStream = new MemoryStream(_avatar);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage empImage = new BitmapImage();
                        empImage.SetSource(memStream);
                        avatarImage.Source = empImage;
                    }
                    catch
                    {
                        avatarImage.Source = UI_Utils.Instance.getDefaultAvatar((string)App.appSettings[App.MSISDN_SETTING]);
                    }
                }
                else
                {
                    avatarImage.Source = UI_Utils.Instance.getDefaultAvatar((string)App.appSettings[App.MSISDN_SETTING]);
                }
            }
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.COUNTRY_SELECTED);
            PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL);
            PhoneApplicationService.Current.State.Remove("fromEnterName");
            PhoneApplicationService.Current.State.Remove("img");
            PhoneApplicationService.Current.State.Remove("fbName");
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
            // remove FB Name if user wants to save his/her custom name
            PhoneApplicationService.Current.State.Remove("fbName");
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

        private void facebook_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            reloadImage = true;
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
            PhoneApplicationService.Current.State["fromEnterName"] = true;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        private void OnProfilePicButtonTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                photoChooserTask.Show();
            }
            catch
            {

            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }
            //progressBarTop.IsEnabled = true;
            shellProgress.IsVisible = true;
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                profileImage = new BitmapImage(uri);
                profileImage.CreateOptions = BitmapCreateOptions.None;
                profileImage.UriSource = uri;
                profileImage.ImageOpened += imageOpenedHandler;
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            byte[] fullViewImageBytes = null;
            BitmapImage image = (BitmapImage)sender;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
            using (var msLargeImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                _avatar = msLargeImage.ToArray();
            }
            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                fullViewImageBytes = msSmallImage.ToArray();
            }
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    avatarImage.Source = profileImage;
                    avatarImage.MaxHeight = 83;
                    avatarImage.MaxWidth = 83;
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, _avatar, false);
                }
                else
                {
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
            });
        }

    }
}