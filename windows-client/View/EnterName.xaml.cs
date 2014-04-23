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
        public bool isClicked = false;
        private string ac_name;
        public ApplicationBar appBar;
        public ApplicationBarIconButton nextIconButton;
        public ApplicationBarIconButton cameraIconButton;
        BitmapImage profileImage = null;
        byte[] _avatar = null;
        byte[] _avImg = null;
        bool isCalled = false;
        PhotoChooserTask photoChooserTask = null;

        public EnterName()
        {
            InitializeComponent();
            App.appSettings[HikeConstants.FILE_SYSTEM_VERSION] = Utils.getAppVersion();// new install so write version
            App.appSettings.Remove(App.ACCOUNT_NAME);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.SETNAME_SCREEN);
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Done_Btn;
            nextIconButton.Click += btnEnterName_Click;
            nextIconButton.IsEnabled = false;
            appBar.Buttons.Add(nextIconButton);

            cameraIconButton = new ApplicationBarIconButton();
            cameraIconButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            cameraIconButton.Text = AppResources.ChangePic_AppBar_Txt;
            cameraIconButton.Click += cameraIconButton_Click;
            appBar.Buttons.Add(cameraIconButton);
            ApplicationBar = appBar;

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }

        void cameraIconButton_Click(object sender, EventArgs e)
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

        private void btnEnterName_Click(object sender, EventArgs e)
        {
            if (isClicked)
                return;
            isClicked = true;
            Focus();
            nameErrorTxt.Opacity = 0;
            if (!NetworkInterface.GetIsNetworkAvailable()) // if no network
            {
                isClicked = false;
                msgTxtBlk.Opacity = 0;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                nameErrorTxt.Text = AppResources.Connectivity_Issue;
                nameErrorTxt.Opacity = 1;
                App.RemoveKeyFromAppSettings(App.ACCOUNT_NAME);
                return;
            }

            txtBxEnterName.IsReadOnly = true;
            txtBxEnterAge.IsReadOnly = true;
            nextIconButton.IsEnabled = false;
            ac_name = txtBxEnterName.Text.Trim();
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            msgTxtBlk.Opacity = 1;
            msgTxtBlk.Text = AppResources.EnterName_ScanningContacts_Txt;

            bool isScanned;

            // if addbook already stored simply call setname api
            if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB || (App.appSettings.TryGetValue(ContactUtils.IS_ADDRESS_BOOK_SCANNED, out isScanned) && isScanned))
            {
                Debug.WriteLine("Btn clicked,Addbook already scanned, posting name to server");
                AccountUtils.setName(ac_name, new AccountUtils.postResponseFunction(setName_Callback));
            }
            // if addbook failed earlier , re attempt for posting add book
            else if (ContactUtils.ContactState == ContactUtils.ContactScanState.ADDBOOK_STORE_FAILED)
            {
                string token = (string)App.appSettings["token"];
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
                        Debug.WriteLine("Setname is called from thread 2 ....");
                        AccountUtils.setName(ac_name, new AccountUtils.postResponseFunction(setName_Callback));
                    }
                };
                bw.RunWorkerAsync();
            }
        }

        private void setName_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.OK != (string)obj[HikeConstants.STAT])
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Debug.WriteLine("Set Name post request returned unsuccessfully .... ");
                    txtBxEnterName.IsReadOnly = false;
                    txtBxEnterAge.IsReadOnly = false;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    if (!string.IsNullOrWhiteSpace(ac_name))
                        nextIconButton.IsEnabled = true;
                    msgTxtBlk.Opacity = 0;
                    nameErrorTxt.Text = AppResources.EnterName_NameErrorTxt;
                    nameErrorTxt.Opacity = 1;
                    App.RemoveKeyFromAppSettings(App.ACCOUNT_NAME);
                    isClicked = false;
                });
                return;
            }
            Debug.WriteLine("Set Name post request returned successfully .... ");
            App.WriteToIsoStorageSettings(App.ACCOUNT_NAME, ac_name);
            if (App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED)) // shows that addressbook is already scanned
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
            txtBxEnterAge.IsReadOnly = false;

            Uri nextPage;
            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);

            if (string.IsNullOrEmpty(country_code) || country_code == HikeConstants.INDIA_COUNTRY_CODE)
                App.appSettings[App.SHOW_FREE_SMS_SETTING] = true;
            else
                App.appSettings[App.SHOW_FREE_SMS_SETTING] = false;

            nextPage = new Uri("/View/TutorialScreen.xaml", UriKind.Relative);
            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.TUTORIAL_SCREEN_STATUS);

            nameErrorTxt.Opacity = 0;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            Thread.Sleep(1 * 500);
            try
            {
                App.appSettings[HikeConstants.IS_NEW_INSTALLATION] = true;
                App.WriteToIsoStorageSettings(App.SHOW_NUDGE_TUTORIAL, true);

                NavigationService.Navigate(nextPage);
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Enter Name ::  processEnterName , processEnterName  , Exception : " + ex.StackTrace);
            }
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

                StringBuilder userMsisdn = new StringBuilder();
                userMsisdn.Append(msisdn.Substring(0, 3)).Append("-").Append(msisdn.Substring(3, 3)).Append("-").Append(msisdn.Substring(6)).Append("!");

                if (!App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED))
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
                        string token = (string)App.appSettings["token"];
                        AccountUtils.postAddressBook(ContactUtils.contactsMap, new AccountUtils.postResponseFunction(postAddressBook_Callback));
                    };

                    bw.RunWorkerAsync();
                }
            }

            if (App.IS_TOMBSTONED) /* ****************************    HANDLING TOMBSTONE    *************************** */
            {
                object obj = null;

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

            if (PhoneApplicationService.Current.State.ContainsKey("fbName"))
            {
                string name = PhoneApplicationService.Current.State["fbName"] as string;
                txtBxEnterName.Text = name;
                nextIconButton.IsEnabled = true;
                spFbConnect.IsEnabled = false;
            }

            if (reloadImage) // this will handle both deactivation and tombstone
            {
                if (PhoneApplicationService.Current.State.ContainsKey("img"))
                {
                    _avatar = (byte[])PhoneApplicationService.Current.State["img"];
                    _avImg = (byte[])PhoneApplicationService.Current.State["img"];
                    MemoryStream memStream = new MemoryStream(_avImg);
                    memStream.Seek(0, SeekOrigin.Begin);
                    if (profileImage == null)
                        profileImage = new BitmapImage();
                    profileImage.SetSource(memStream);
                    shellProgress.IsVisible = true;
                    //send image to server here and insert in db after getting response
                    AccountUtils.updateProfileIcon(_avImg, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
                    reloadImage = false;
                }
                else
                {
                    _avatar = MiscDBUtil.getThumbNailForMsisdn(HikeConstants.MY_PROFILE_PIC);

                    if (_avatar != null)
                    {
                        try
                        {
                            MemoryStream memStream = new MemoryStream(_avatar);
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
                        //AccountUtils.createGetRequest(AccountUtils.BASE + "/account/avatar/" + myMsisdn, GetProfilePic_Callback, true, "");
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
            }
            else
                App.IS_TOMBSTONED = false;
        }

        private void txtBxEnterName_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // remove FB Name if user wants to save his/her custom name
            PhoneApplicationService.Current.State.Remove("fbName");
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
        }

        private void txtBxEnterAge_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            nextIconButton.IsEnabled = !string.IsNullOrWhiteSpace(txtBxEnterName.Text) && !string.IsNullOrWhiteSpace(txtBxEnterAge.Text) ? true : false;
        }

        private void facebook_Tap(object sender, RoutedEventArgs e)
        {
            // if done button is already clicked, simply ignore FB
            if (isClicked)
                return;
            reloadImage = true;
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
            PhoneApplicationService.Current.State["fromEnterName"] = true;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                nextIconButton.IsEnabled = true;
                txtBxEnterName.IsEnabled = true; 
                txtBxEnterAge.IsEnabled = true;
                return;
            }
            //progressBarTop.IsEnabled = true;
            shellProgress.IsVisible = true;
            if (e.TaskResult == TaskResult.OK)
            {
                if (profileImage == null)
                    profileImage = new BitmapImage();
                profileImage.SetSource(e.ChosenPhoto);
                try
                {
                    byte[] fullViewImageBytes = null;
                    WriteableBitmap writeableBitmap = new WriteableBitmap(profileImage);
                    using (var msLargeImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                        _avImg = msLargeImage.ToArray();
                    }
                    using (var msSmallImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msSmallImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                        fullViewImageBytes = msSmallImage.ToArray();
                    }
                    //send image to server here and insert in db after getting response
                    AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("EnterName :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                nextIconButton.IsEnabled = true;
                txtBxEnterName.IsEnabled = true;
                txtBxEnterAge.IsEnabled = true;

                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    avatarImage.ImageSource = profileImage;
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, _avImg, false);
                }
                else
                {
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                nextIconButton.IsEnabled = true;
                txtBxEnterName.IsEnabled = true;
                txtBxEnterAge.IsEnabled = true;
            });
        }
        public void GetProfilePic_Callback(byte[] fullBytes, object fName)
        {
            try
            {
                if (fullBytes != null && fullBytes.Length > 0)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                     {
                         avatarImage.ImageSource = UI_Utils.Instance.createImageFromBytes(fullBytes);
                         MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, fullBytes, false);
                     });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EnterName :: GetProfilePic_Callback, Exception : " + ex.StackTrace);
            }
        }

        /* This is the callback function which is called when server returns the addressbook*/
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
                        nextIconButton.IsEnabled = true;
                        txtBxEnterName.IsReadOnly = false;
                        txtBxEnterAge.IsReadOnly = false;
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
    }
}