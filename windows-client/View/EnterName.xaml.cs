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
using System.ComponentModel;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private bool reloadImage = true;
        private string ac_name;
        private string ac_age;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;
        public ApplicationBarIconButton cameraIconButton;
        BitmapImage profileImage = null;
        PhotoChooserTask photoChooserTask = null;

        public EnterName()
        {
            InitializeComponent();
            App.appSettings[HikeConstants.FILE_SYSTEM_VERSION] = Utils.getAppVersion();// new install so write version
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.SETNAME_SCREEN);
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            cameraIconButton = new ApplicationBarIconButton();
            cameraIconButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            cameraIconButton.Text = AppResources.ChangePic_AppBar_Txt;
            cameraIconButton.Click += cameraIconButton_Click;
            appBar.Buttons.Add(cameraIconButton);
            ApplicationBar = appBar;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Done_Btn;
            nextIconButton.Click += Next_Click;
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }

        void cameraIconButton_Click(object sender, EventArgs e)
        {
            ChangeProfile();
        }

        private void ChangeProfile()
        {
            try
            {
                photoChooserTask.Show();
                nextIconButton.IsEnabled = false;
                txtBxEnterName.IsEnabled = false;
                txtBxEnterAge.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EnterName :: OnProfilePicButtonTap, Exception : " + ex.StackTrace);
            }
        }

        private void Next_Click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            isClicked = true;

            Focus();

            nameErrorTxt.Opacity = 0;

            ac_name = txtBxEnterName.Text.Trim();
            ac_age = txtBxEnterAge.Text.Trim();

            App.WriteToIsoStorageSettings(App.ACCOUNT_NAME, ac_name);
            App.WriteToIsoStorageSettings(App.ACCOUNT_AGE, ac_age);

            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                isClicked = false;
                msgTxtBlk.Opacity = 0;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                nameErrorTxt.Text = AppResources.Connectivity_Issue;
                nameErrorTxt.Opacity = 1;
                return;
            }
            else if (String.IsNullOrEmpty(ac_age) || String.IsNullOrEmpty(ac_name))
            {
                isClicked = false;
                msgTxtBlk.Opacity = 0;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                nameErrorTxt.Text = AppResources.Empty_Field_Error;
                nameErrorTxt.Opacity = 1;
                return;
            }

            var nextPage = new Uri("/View/EnterGender.xaml", UriKind.Relative);

            nameErrorTxt.Opacity = 0;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            App.appSettings[HikeConstants.IS_NEW_INSTALLATION] = true;

            NavigationService.Navigate(nextPage);
            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
                PushHelper.Instance.registerPushnotifications(false);

                string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
                msisdn = msisdn.Substring(msisdn.Length - 10);

                object obj = null;
                if (App.appSettings.TryGetValue(App.ACCOUNT_NAME, out obj))
                {
                        txtBxEnterName.Text = (string)obj;
                        txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                }

                if (App.appSettings.TryGetValue(App.ACCOUNT_AGE, out obj))
                {
                    txtBxEnterAge.Text = (string)obj;
                    txtBxEnterAge.Select(txtBxEnterName.Text.Length, 0);
                }

                if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
                {
                    if (State.TryGetValue("txtBxEnterName", out obj))
                    {
                        txtBxEnterName.Text = (string)obj;
                        txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                        obj = null;
                    }

                    if (State.TryGetValue("txtBxEnterAge", out obj))
                    {
                        txtBxEnterAge.Text = (string)obj;
                        txtBxEnterAge.Select(txtBxEnterAge.Text.Length, 0);
                        obj = null;
                    }

                    if (State.TryGetValue("nameErrorTxt.Opacity", out obj))
                    {
                        nameErrorTxt.Opacity = (double)obj;
                        nameErrorTxt.Text = (string)State["nameErrorTxt.Text"];
                    }
                }
            }

            if (PhoneApplicationService.Current.State.ContainsKey("fbName"))
            {
                string name = PhoneApplicationService.Current.State["fbName"] as string;
                txtBxEnterName.Text = name;
                spFbConnect.IsEnabled = false;
            }

            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;

            if (reloadImage) // this will handle both deactivation and tombstone
            {
                if (State.ContainsKey("img"))
                {
                    fullViewImageBytes = (byte[])State["img"];
                    
                    MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                    memStream.Seek(0, SeekOrigin.Begin);

                    if (profileImage == null)
                        profileImage = new BitmapImage();
                    
                    profileImage.SetSource(memStream);
                    
                    reloadImage = false;
                }
                else
                {
                    fullViewImageBytes = MiscDBUtil.getThumbNailForMsisdn(HikeConstants.MY_PROFILE_PIC);

                    if (fullViewImageBytes != null)
                    {
                        try
                        {
                            MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                            memStream.Seek(0, SeekOrigin.Begin);
                            BitmapImage empImage = new BitmapImage();
                            empImage.SetSource(memStream);
                            avatarImage.ImageSource = empImage;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Enter Name ::  OnNavigatedTo , Exception : " + ex.StackTrace);
                            avatarImage.ImageSource = UI_Utils.Instance.getDefaultAvatar((string)App.appSettings[App.MSISDN_SETTING]);
                        }
                    }
                    else
                    {
                        string myMsisdn = (string)App.appSettings[App.MSISDN_SETTING];
                        avatarImage.ImageSource = UI_Utils.Instance.getDefaultAvatar(myMsisdn);
                    }
                }
            }

            txtBxEnterName.Focus();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.COUNTRY_SELECTED);
            PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL);
            PhoneApplicationService.Current.State.Remove("fromEnterName");
            PhoneApplicationService.Current.State.Remove("fbName");
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            string uri = e.Uri.ToString();

            if (!uri.Contains("View"))
            {
                if (!string.IsNullOrWhiteSpace(txtBxEnterName.Text))
                    State["txtBxEnterName"] = txtBxEnterName.Text;
                else
                    State.Remove("txtBxEnterName");

                if (!string.IsNullOrWhiteSpace(txtBxEnterAge.Text))
                    State["txtBxEnterAge"] = txtBxEnterAge.Text;
                else
                    State.Remove("txtBxEnterAge");

                if (msgTxtBlk.Opacity == 1)
                {
                    State["nameErrorTxt.Text"] = nameErrorTxt.Text;
                    State["nameErrorTxt.Opacity"] = nameErrorTxt.Opacity;
                }
                else
                {
                    State.Remove("nameErrorTxt.Text");
                    State.Remove("nameErrorTxt.Opacity");
                }

                State.Remove("img");
                if (fullViewImageBytes != null)
                    State["img"] = fullViewImageBytes;
            }
            else
                App.IS_TOMBSTONED = false;
        }

        public void postAddressBook_Callback(JObject jsonForAddressBookAndBlockList)
        {
            // test this is called
            JObject obj = jsonForAddressBookAndBlockList;
            if (obj == null)
            {
                Debug.WriteLine("Post addbook request returned unsuccessfully .... ");
                ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED;
                
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    // if next button is clicked show the error msg
                    if (isClicked)
                    {
                        msgTxtBlk.Opacity = 0;
                        nameErrorTxt.Text = AppResources.Contact_Scanning_Failed_Txt;
                        nameErrorTxt.Opacity = 1;
                        progressBar.IsEnabled = false;
                        progressBar.Opacity = 0;
                        nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
                        txtBxEnterName.IsEnabled = true;
                        txtBxEnterAge.IsEnabled = true;
                        isClicked = false;
                    }
                });

                return;
            }
            Debug.WriteLine("Post addbook request returned successfully .... ");
            List<ContactInfo> addressbook = AccountUtils.getContactList(jsonForAddressBookAndBlockList, ContactUtils.contactsMap, false);
            List<string> blockList = AccountUtils.getBlockList(jsonForAddressBookAndBlockList);

            int count = 1;
            // waiting for DB to be created
            while (!App.appSettings.Contains(App.IS_DB_CREATED) && count <= 120)
            {
                Thread.Sleep(1 * 500);
                count++;
            }
            if (!App.appSettings.Contains(App.IS_DB_CREATED)) // if DB is not created for so long
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("Phone DB is not created in time .... ");
                    ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED;
                    msgTxtBlk.Text = AppResources.EnterName_Failed_Txt;
                });
                return;
            }

            try
            {
                // if addressbook is null, then also user should be able to move inside app.
                UsersTableUtils.deleteAllContacts();
                UsersTableUtils.deleteBlocklist();
                Stopwatch st = Stopwatch.StartNew();
                UsersTableUtils.addContacts(addressbook); // add the contacts to hike users db.
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to add addressbook {0}", msec);
                UsersTableUtils.addBlockList(blockList);
            }
            catch (Exception e)
            {
                Debug.WriteLine("EnterName :: postAddressBook_Callback : Exception : " + e.StackTrace);
            }
            Debug.WriteLine("Addbook stored in Hike Db .... ");
            App.WriteToIsoStorageSettings(ContactUtils.IS_ADDRESS_BOOK_SCANNED, true);
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB;
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove("fbName");
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
        }

        private void txtBxEnterAge_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
        }

        private void facebook_Tap(object sender, RoutedEventArgs e)
        {
            if (isClicked)
                return;

            reloadImage = true;
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
            PhoneApplicationService.Current.State["fromEnterName"] = true;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        byte[] fullViewImageBytes = null;
        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
                txtBxEnterName.IsEnabled = true; 
                txtBxEnterAge.IsEnabled = true;
                return;
            }

            progressBar.Opacity = 1;
            
            if (e.TaskResult == TaskResult.OK)
            {
                if (profileImage == null)
                    profileImage = new BitmapImage();

                profileImage.SetSource(e.ChosenPhoto);

                try
                {
                    WriteableBitmap writeableBitmap = new WriteableBitmap(profileImage);
                    using (var msLargeImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msLargeImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                        fullViewImageBytes = msLargeImage.ToArray();
                        MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, fullViewImageBytes, false);
                    }

                    reloadImage = false;

                    avatarImage.ImageSource = UI_Utils.Instance.createImageFromBytes(fullViewImageBytes);
                    progressBar.Opacity = 0;
                    nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
                    txtBxEnterName.IsEnabled = true;
                    txtBxEnterAge.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EnterName :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                progressBar.Opacity = 0;
                nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
                txtBxEnterName.IsEnabled = true;
                txtBxEnterAge.IsEnabled = true;

                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        bool isClicked;

        private void txtBxEnterAge_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var txtBox = sender as PhoneTextBox;

            if (txtBox.Text.Length > 0)
            {
                string lastCharacter = txtBox.Text.Substring(txtBox.Text.Length - 1);
                bool isDigit = true;
                double num;
                
                isDigit = Double.TryParse(lastCharacter, out num);
                if (!isDigit)
                {
                    if (string.IsNullOrEmpty(txtBox.Text) || txtBox.Text.Length == 1)
                        txtBox.Text = String.Empty;
                    else
                        txtBox.Text = txtBox.Text.Substring(0, txtBox.Text.Length - 1);
                }

                txtBox.Select(txtBox.Text.Length, 0);
            }
        }

        private void ChangeProfile_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ChangeProfile();
        }
    }
}