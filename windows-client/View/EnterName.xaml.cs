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
using System.Windows.Navigation;

namespace windows_client
{
    public partial class EnterName : PhoneApplicationPage
    {
        private bool reloadImage = true;
        private string ac_name;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;
        public ApplicationBarIconButton cameraIconButton;
        BitmapImage profileImage = null;
        PhotoChooserTask photoChooserTask = null;

        public EnterName()
        {
            InitializeComponent();

            HikeInstantiation.appSettings[HikeConstants.FILE_SYSTEM_VERSION] = Utils.getAppVersion();// new install so write version
            HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.PAGE_STATE, HikeInstantiation.PageState.SETNAME_SCREEN);

            appBar = new ApplicationBar()
            {
                ForegroundColor = (Color)App.Current.Resources["AppBarWhiteForegroundColor"],
                BackgroundColor = (Color)App.Current.Resources["AppBarWhiteBackgroundColor"]
            };

            cameraIconButton = new ApplicationBarIconButton();
            cameraIconButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            cameraIconButton.Text = AppResources.ChangePic_AppBar_Txt;
            cameraIconButton.Click += cameraIconButton_Click;
            appBar.Buttons.Add(cameraIconButton);
            ApplicationBar = appBar;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
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
                Analytics.SendClickEvent(HikeConstants.FTUE_SET_PROFILE_IMAGE);
                photoChooserTask.Show();
                nextIconButton.IsEnabled = false;
                txtBxEnterName.IsEnabled = false;
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

            HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.ACCOUNT_NAME, ac_name);

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
            else if (String.IsNullOrEmpty(ac_name))
            {
                isClicked = false;
                msgTxtBlk.Opacity = 0;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                nameErrorTxt.Text = AppResources.Empty_Field_Error;
                nameErrorTxt.Opacity = 1;
                return;
            }

            nameErrorTxt.Opacity = 0;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            HikeInstantiation.appSettings[HikeConstants.IS_NEW_INSTALLATION] = true;

            progressBar.Opacity = 1;

            JObject obj = new JObject();

            obj.Add(HikeInstantiation.NAME, (string)HikeInstantiation.appSettings[HikeInstantiation.ACCOUNT_NAME]);
            obj.Add(HikeInstantiation.SCREEN, "signup");

            bool isScanned;
            if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB || (HikeInstantiation.appSettings.TryGetValue(ContactUtils.IS_ADDRESS_BOOK_SCANNED, out isScanned) && isScanned))
            {
                AccountUtils.setProfile(obj, new AccountUtils.postResponseFunction(setProfile_Callback));
            }
            // if addbook failed earlier , re attempt for posting add book
            else if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED)
            {
                string token = (string)HikeInstantiation.appSettings["token"];
                AccountUtils.postAddressBook(ContactUtils.contactsMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
            }
            else // if add book is already in posted state then run Background worker that waits for result
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    Debug.WriteLine("Thread 2 started ....");
                    while (true)
                    {
                        if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB || ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED)
                            break;
                        Thread.Sleep(50);
                    }

                    // if addbook is stored properly in hike db then call for setname function
                    if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB)
                    {
                        AccountUtils.setProfile(obj, new AccountUtils.postResponseFunction(setProfile_Callback));
                    }
                };
                bw.RunWorkerAsync();
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || HikeInstantiation.IS_TOMBSTONED)
            {
                PushHelper.Instance.registerPushnotifications(false);

                if (!HikeInstantiation.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED))
                {
                    if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING)
                        ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.contactSearchCompleted_Callback));

                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += (ss, ee) =>
                    {
                        while (ContactUtils.ContactState != ContactUtils.ContactScanState.ADDBOOK_SCANNED)
                            Thread.Sleep(100);
                        // now addressbook is scanned 
                        Debug.WriteLine("Posting addbook from thread 1.... ");
                        string token = (string)HikeInstantiation.appSettings["token"];
                        AccountUtils.postAddressBook(ContactUtils.contactsMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
                    };

                    bw.RunWorkerAsync();
                }

                object obj = null;
                if (HikeInstantiation.appSettings.TryGetValue(HikeInstantiation.ACCOUNT_NAME, out obj))
                {
                    txtBxEnterName.Text = (string)obj;
                    txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
                }

                if (HikeInstantiation.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
                {
                    if (State.TryGetValue("txtBxEnterName", out obj))
                    {
                        txtBxEnterName.Text = (string)obj;
                        txtBxEnterName.Select(txtBxEnterName.Text.Length, 0);
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
                fbConnectText.Text = AppResources.Connected_Txt;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        txtBxEnterName.Text = name;
                    });
                spFbConnect.MinWidth = 180;
                spFbConnect.IsEnabled = false;
                feelingLazyTxtBlk.Visibility = Visibility.Collapsed;
            }

            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text);

            if (reloadImage) // this will handle both deactivation and tombstone
            {
                if (PhoneApplicationService.Current.State.ContainsKey("img"))
                {
                    fullViewImageBytes = (byte[])PhoneApplicationService.Current.State["img"];
                    avatarImage.Source = UI_Utils.Instance.createImageFromBytes(fullViewImageBytes);
                    reloadImage = false;
                }
                else
                {
                    fullViewImageBytes = MiscDBUtil.getLargeImageForMsisdn(HikeConstants.MY_PROFILE_PIC);

                    if (fullViewImageBytes != null)
                    {
                        try
                        {
                            MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                            memStream.Seek(0, SeekOrigin.Begin);
                            BitmapImage empImage = new BitmapImage();
                            empImage.SetSource(memStream);
                            avatarImage.Source = empImage;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Enter Name ::  OnNavigatedTo , Exception : " + ex.StackTrace);
                            avatarImage.Source = UI_Utils.Instance.getDefaultAvatar((string)HikeInstantiation.appSettings[HikeInstantiation.MSISDN_SETTING], true);
                        }
                    }
                    else
                    {
                        string myMsisdn = (string)HikeInstantiation.appSettings[HikeInstantiation.MSISDN_SETTING];
                        avatarImage.Source = UI_Utils.Instance.getDefaultAvatar(myMsisdn, true);
                    }
                }
            }
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

                PhoneApplicationService.Current.State.Remove("img");
                if (fullViewImageBytes != null)
                    PhoneApplicationService.Current.State["img"] = fullViewImageBytes;
            }
            else
                HikeInstantiation.IS_TOMBSTONED = false;
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove("fbName");
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text);
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
                nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text);
                txtBxEnterName.IsEnabled = true;
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
                        MiscDBUtil.saveLargeImage(HikeConstants.MY_PROFILE_PIC, fullViewImageBytes);
                    }

                    reloadImage = false;

                    avatarImage.Source = UI_Utils.Instance.createImageFromBytes(fullViewImageBytes);
                    progressBar.Opacity = 0;
                    nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text);
                    txtBxEnterName.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EnterName :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                progressBar.Opacity = 0;
                nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text);
                txtBxEnterName.IsEnabled = true;

                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        bool isClicked;

        private void ChangeProfile_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ChangeProfile();
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
                        progressBar.Opacity = 0;
                        nextIconButton.IsEnabled = String.IsNullOrEmpty(ac_name) ? false : true;
                        isClicked = false;
                    }
                });
                return;
            }

            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_POSTED;
            Debug.WriteLine("Post addbook request returned successfully .... ");
            List<ContactInfo> addressbook = AccountUtils.getContactList(jsonForAddressBookAndBlockList, ContactUtils.contactsMap);
            List<string> blockList = AccountUtils.getBlockList(jsonForAddressBookAndBlockList);

            int count = 1;
            // waiting for DB to be created
            while (!HikeInstantiation.appSettings.Contains(HikeInstantiation.IS_DB_CREATED) && count <= 120)
            {
                Thread.Sleep(500);
                count++;
            }
            if (!HikeInstantiation.appSettings.Contains(HikeInstantiation.IS_DB_CREATED)) // if DB is not created for so long
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("Phone DB is not created in time .... ");
                    ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED;
                    if (isClicked)
                    {
                        msgTxtBlk.Opacity = 0;
                        nameErrorTxt.Text = AppResources.Contact_Scanning_Failed_Txt;
                        nameErrorTxt.Opacity = 1;
                        progressBar.IsEnabled = false;
                        progressBar.Opacity = 0;
                        nextIconButton.IsEnabled = String.IsNullOrEmpty(ac_name) ? false : true;
                        isClicked = false;
                    }
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
            HikeInstantiation.WriteToIsoStorageSettings(ContactUtils.IS_ADDRESS_BOOK_SCANNED, true);
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB;
        }

        private void setProfile_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.OK != (string)obj[HikeConstants.STAT])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("Set Name post request returned unsuccessfully .... ");
                    progressBar.Opacity = 0;
                    nextIconButton.IsEnabled = String.IsNullOrEmpty(ac_name) ? false : true;

                    msgTxtBlk.Opacity = 0;
                    nameErrorTxt.Text = AppResources.EnterName_NameErrorTxt;
                    nameErrorTxt.Opacity = 1;
                    isClicked = false;
                });
                return;
            }

            UpdateProfileImage();

            Debug.WriteLine("Set Name post request returned successfully .... ");
        }

        private void UpdateProfileImage()
        {
            if (fullViewImageBytes != null)
            {
                AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    processProfile();
                });
            }
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj == null || HikeConstants.OK != (string)obj[HikeConstants.STAT])
                {
                    progressBar.Opacity = 0;
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                    return;
                }

                var img = UI_Utils.Instance.createImageFromBytes(fullViewImageBytes);
                WriteableBitmap writeableBitmap = new WriteableBitmap(img);

                byte[] thumbnailBytes = null;
                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, 83, 83, 0, 95);
                    thumbnailBytes = msLargeImage.ToArray();
                }

                MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, thumbnailBytes, false);
                processProfile();
            });
        }

        bool isCalled;

        public void processProfile()
        {
            if (isCalled)
                return;
            isCalled = true;

            string country_code = null;
            HikeInstantiation.appSettings.TryGetValue<string>(HikeInstantiation.COUNTRY_CODE_SETTING, out country_code);

            if (string.IsNullOrEmpty(country_code) || country_code == HikeConstants.INDIA_COUNTRY_CODE)
                HikeInstantiation.appSettings[HikeInstantiation.SHOW_FREE_SMS_SETTING] = true;
            else
                HikeInstantiation.appSettings[HikeInstantiation.SHOW_FREE_SMS_SETTING] = false;

            nameErrorTxt.Opacity = 0;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;

            try
            {
                HikeInstantiation.appSettings[HikeConstants.IS_NEW_INSTALLATION] = true;
                HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.SHOW_NUDGE_TUTORIAL, true);

                SmileyParser.Instance.initializeSmileyParser();
                HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.PAGE_STATE, HikeInstantiation.PageState.CONVLIST_SCREEN);

                App page = (App)Application.Current;
                ((UriMapper)(page.RootFrame.UriMapper)).UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                page.RootFrame.Navigate(new Uri("/View/ConversationsList.xaml?id=1", UriKind.Relative));
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Enter Name ::  processEnterName , processEnterName  , Exception : " + ex.StackTrace);
            }
        }
    }
}