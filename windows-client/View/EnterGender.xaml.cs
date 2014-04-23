using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.Languages;
using windows_client.DbUtils;
using windows_client;
using System.Windows.Media;
using windows_client.utils;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Text;
using System.IO;

namespace windows_client.View
{
    public partial class EnterGender : PhoneApplicationPage
    {
        ApplicationBarIconButton nextIconButton;
        App.PageState currentPagestate;
        private bool reloadImage = true;
        bool isClicked = false;
        bool isCalled = false;
        private string ac_name;
        private string ac_age;
        byte[] _avatar = null;
        byte[] _avImg = null;

        public EnterGender()
        {
            InitializeComponent();

            ApplicationBar appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
            };
            this.ApplicationBar = appBar;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/AppBar/icon_next.png", UriKind.Relative);
            nextIconButton.Text = AppResources.AppBar_Next_Btn;
            nextIconButton.IsEnabled = false;
            nextIconButton.Click += OnNextClick;
            appBar.Buttons.Add(nextIconButton);
        }

        string gender = String.Empty;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            while (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
                PushHelper.Instance.registerPushnotifications(false);

                string msisdn = (string)App.appSettings[App.MSISDN_SETTING];
                msisdn = msisdn.Substring(msisdn.Length - 10);

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

                object obj = null;

                if (State.TryGetValue("nameErrorTxt.Opacity", out obj))
                {
                    nameErrorTxt.Opacity = (double)obj;
                    nameErrorTxt.Text = (string)State["nameErrorTxt.Text"];
                }

                if (State.TryGetValue("randomText.Text", out obj))
                    randomText.Text = (string)obj;
                else
                    randomText.Text = AppResources.RandomText_DefaultTxt;

                if (App.appSettings.TryGetValue(App.ACCOUNT_GENDER, out obj))
                {
                    gender = (string)obj;
                    if (gender == "m")
                    {
                        boyButtonImage.Source = UI_Utils.Instance.BoySelectedImage;
                        girlButtonImage.Source = UI_Utils.Instance.GirlUnSelectedImage;

                        boyText.Foreground = (SolidColorBrush)App.Current.Resources["HikeBlueHeader"];
                        girlText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];

                        if (randomText.Text == AppResources.RandomText_DefaultTxt)
                            RandomizeString(true);
                    }
                    else
                    {
                        boyButtonImage.Source = UI_Utils.Instance.BoyUnSelectedImage;
                        girlButtonImage.Source = UI_Utils.Instance.GirlSelectedImage;

                        boyText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                        girlText.Foreground = UI_Utils.Instance.Pink;
                    
                        if (randomText.Text == AppResources.RandomText_DefaultTxt)
                            RandomizeString(false);
                    }

                    nextIconButton.IsEnabled = true;
                }
                else
                {
                    boyButtonImage.Source = UI_Utils.Instance.BoyUnSelectedImage;
                    girlButtonImage.Source = UI_Utils.Instance.GirlUnSelectedImage;

                    boyText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                    girlText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];
                }
            }

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            string uri = e.Uri.ToString();
            if (!uri.Contains("View"))
            {
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

                if (!String.IsNullOrEmpty(randomText.Text))
                    State["randomText.Text"] = nameErrorTxt.Text;
                else
                    State.Remove("randomText.Text");
            }
            else
                App.IS_TOMBSTONED = false;
        }


        public void OnNextClick(object sender, EventArgs e)
        {
            girlButton.IsEnabled = false;
            boyButton.IsEnabled = false;

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
                        nextIconButton.IsEnabled = String.IsNullOrEmpty(gender) ? false : true;
                        girlButton.IsEnabled = true;
                        boyButton.IsEnabled = true;
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
                    if (isClicked)
                    {
                        msgTxtBlk.Opacity = 0;
                        nameErrorTxt.Text = AppResources.Contact_Scanning_Failed_Txt;
                        nameErrorTxt.Opacity = 1;
                        progressBar.IsEnabled = false;
                        progressBar.Opacity = 0;
                        nextIconButton.IsEnabled = String.IsNullOrEmpty(gender) ? false : true;
                        girlButton.IsEnabled = true;
                        boyButton.IsEnabled = true;
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
            App.WriteToIsoStorageSettings(ContactUtils.IS_ADDRESS_BOOK_SCANNED, true);
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_STORED_IN_HIKE_DB;
        }

        private void setName_Callback(JObject obj)
        {
            //if (obj == null || HikeConstants.OK != (string)obj[HikeConstants.STAT])
            //{
            //    Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        Debug.WriteLine("Set Name post request returned unsuccessfully .... ");
            //        boyButton.IsEnabled = true;
            //        girlButton.IsEnabled = true;
            //        progressBar.Opacity = 0;
            //        progressBar.IsEnabled = false;

            //        if (!string.IsNullOrWhiteSpace(ac_name))
            //            nextIconButton.IsEnabled = true;
                    
            //        msgTxtBlk.Opacity = 0;
            //        nameErrorTxt.Text = AppResources.EnterName_NameErrorTxt;
            //        nameErrorTxt.Opacity = 1;
            //        isClicked = false;
            //    });
            //    return;
            //}

            //Debug.WriteLine("Set Name post request returned successfully .... ");
            //if (App.appSettings.Contains(ContactUtils.IS_ADDRESS_BOOK_SCANNED)) // shows that addressbook is already scanned
            //{
            //    Deployment.Current.Dispatcher.BeginInvoke(() =>
            //    {
            //        processEnterName();
            //    });
            //}
        }

        public void processEnterName()
        {
            if (isCalled)
                return;
            isCalled = true;

            girlButton.IsEnabled = true;
            boyButton.IsEnabled = true;

            string country_code = null;
            App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);

            if (string.IsNullOrEmpty(country_code) || country_code == HikeConstants.INDIA_COUNTRY_CODE)
                App.appSettings[App.SHOW_FREE_SMS_SETTING] = true;
            else
                App.appSettings[App.SHOW_FREE_SMS_SETTING] = false;

            nameErrorTxt.Opacity = 0;
            msgTxtBlk.Text = AppResources.EnterName_Msg_TxtBlk;
            Thread.Sleep(1 * 500);
            try
            {
                App.appSettings[HikeConstants.IS_NEW_INSTALLATION] = true;
                App.WriteToIsoStorageSettings(App.SHOW_NUDGE_TUTORIAL, true);

                SmileyParser.Instance.initializeSmileyParser();
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);

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

        private void boyButton_Click(object sender, RoutedEventArgs e)
        {
            if (gender == "m")
                return;
            gender = "m";

            boyButtonImage.Source = UI_Utils.Instance.BoySelectedImage;
            girlButtonImage.Source = UI_Utils.Instance.GirlUnSelectedImage;

            boyText.Foreground = (SolidColorBrush)App.Current.Resources["HikeBlueHeader"];
            girlText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];

            App.WriteToIsoStorageSettings(App.ACCOUNT_GENDER, gender);
            nextIconButton.IsEnabled = String.IsNullOrEmpty(gender) ? false : true;

            RandomizeString(true);
        }

        private void girlButton_Click(object sender, RoutedEventArgs e)
        {
            if (gender == "f")
                return;
            gender = "f";

            boyButtonImage.Source = UI_Utils.Instance.BoyUnSelectedImage;
            girlButtonImage.Source = UI_Utils.Instance.GirlSelectedImage;

            boyText.Foreground = (SolidColorBrush)App.Current.Resources["HikeGrey"];
            girlText.Foreground = UI_Utils.Instance.Pink;

            App.WriteToIsoStorageSettings(App.ACCOUNT_GENDER, "f");
            nextIconButton.IsEnabled = String.IsNullOrEmpty(gender) ? false : true;

            RandomizeString(false);
        }

        Random random = new Random();

        void RandomizeString(bool isBoy)
        {
            int index = random.Next(5);

            if (isBoy)
            {
                switch (index)
                {
                    case 0:
                        randomText.Text = AppResources.RandomText_Boy1;
                        break;
                    case 1:
                        randomText.Text = AppResources.RandomText_Boy2;
                        break;
                    case 2:
                        randomText.Text = AppResources.RandomText_Boy3;
                        break;
                    case 3:
                        randomText.Text = AppResources.RandomText_Boy4;
                        break;
                    case 4:
                        randomText.Text = AppResources.RandomText_Boy5;
                        break;
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        randomText.Text = AppResources.RandomText_Girl1;
                        break;
                    case 1:
                        randomText.Text = AppResources.RandomText_Girl2;
                        break;
                    case 2:
                        randomText.Text = AppResources.RandomText_Girl3;
                        break;
                    case 3:
                        randomText.Text = AppResources.RandomText_Girl4;
                        break;
                    case 4:
                        randomText.Text = AppResources.RandomText_Girl5;
                        break;
                }
            }
        }
    }
}