using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO.IsolatedStorage;
using windows_client.utils;
using windows_client.ViewModel;
using windows_client.Mqtt;
using windows_client.View;

namespace windows_client
{
    public partial class App : Application
    {
        public static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        #region Hike Specific Constants

        public static readonly string PAGE_STATE = "page_State";
        public static readonly string ACCOUNT_NAME = "accountName";
        public static readonly string MSISDN_SETTING = "msisdn";
        public static readonly string IS_ADDRESS_BOOK_SCANNED = "isabscanned";
        public static readonly string TOKEN_SETTING = "token";
        public static readonly string UID_SETTING = "uid";
        public static readonly string SMS_SETTING = "smscredits";
        public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
        public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
        public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        public static readonly string invite_message = "Hey! I\'m using hike to send SMS for free. Download it at http://get.hike.in and start messaging me and other friends for free!";

        #endregion

        #region Hike specific instances and functions

        #region instances

        private static bool ab_scanned = false;
        public static bool isABScanning = false;
        private static HikePubSub mPubSubInstance;
        private static HikeViewModel _viewModel = new HikeViewModel();
        private static DbConversationListener dbListener;
        private static HikeMqttManager mMqttManager;
        private static NetworkManager networkManager;

        #endregion

        #region instances getters and setters

        public static HikeMqttManager MqttManagerInstance
        {
            get
            {
                return mMqttManager;
            }
            set
            {
                mMqttManager = value;
            }


        }

        public static bool Ab_scanned
        {
            get
            {
                return ab_scanned;
            }
            set
            {
                if (ab_scanned != value)
                {
                    ab_scanned = value;
                }
            }
        }

        public static HikePubSub HikePubSubInstance
        {
            get
            {
                return mPubSubInstance;
            }
            set
            {
                if (value != mPubSubInstance)
                    mPubSubInstance = value;
            }
        }

        public static HikeViewModel ViewModel
        {
            get
            {
                return _viewModel;
            }
        }

        public static DbConversationListener DbListener
        {
            get
            {
                return dbListener;
            }
            set
            {
                if (value != dbListener)
                {
                    dbListener = value;
                }
            }
        }

        public static NetworkManager NetworkManagerInstance
        {
            get
            {
                return networkManager;
            }
            set
            {
                if (value != networkManager)
                {
                    networkManager = value;
                }
            }
        }

        #endregion

        #endregion

        #region Page State

        public enum PageState
        {
            WELCOME_SCREEN, // WelcomePage Screen
            PHONE_SCREEN,   // EnterNumber Screen
            PIN_SCREEN,     // EnterPin Screen
            SETNAME_SCREEN, // EnterName Screen
            CONVLIST_SCREEN, // ConversationsList Screen
        }

        #endregion

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            /* Load App token if its there*/
            if (appSettings.Contains(App.TOKEN_SETTING) && null != appSettings[App.TOKEN_SETTING])
            {
                AccountUtils.Token = (string)appSettings[App.TOKEN_SETTING];
            }

            #region CreateDatabases

            // Create the database if it does not exist.

            using (HikeChatsDb db = new HikeChatsDb(MsgsDBConnectionstring))
            {
                if (db.DatabaseExists() == false)
                {
                    // Create the local database.
                    db.CreateDatabase();
                }
            }

            using (HikeUsersDb db = new HikeUsersDb(UsersDBConnectionstring))
            {
                if (db.DatabaseExists() == false)
                {
                    // Create the local database.
                    db.CreateDatabase();
                }
            }

            using (HikeMqttPersistenceDb db = new HikeMqttPersistenceDb(MqttDBConnectionstring))
            {
                if (db.DatabaseExists() == false)
                {
                    // Create the local database.
                    db.CreateDatabase();
                }
            }

            #endregion

        }


        private void loadPage()
        {
            PageState ps = PageState.WELCOME_SCREEN;
            appSettings.TryGetValue<PageState>(App.PAGE_STATE, out ps);
            Uri nUri = null;

            switch (ps)
            {
                case PageState.WELCOME_SCREEN:
                    nUri = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                    break;
                case PageState.PHONE_SCREEN:
                    nUri = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                    break;
                case PageState.PIN_SCREEN:
                    nUri = new Uri("/View/EnterPin.xaml", UriKind.Relative);
                    break;
                case PageState.SETNAME_SCREEN:
                    nUri = new Uri("/View/EnterName.xaml", UriKind.Relative);
                    if (appSettings.Contains(App.IS_ADDRESS_BOOK_SCANNED))
                    {
                        ab_scanned = true;
                    }
                    break;
                case PageState.CONVLIST_SCREEN:
                    nUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                    break;
                default:
                    nUri = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                    break;
            }
            ((App)Application.Current).RootFrame.Navigate(nUri);
        }
        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            loadPage();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            //loadPage();
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Exception :: ", e.ToString(), MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
                return;
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            // Running on a device / emulator without debugging
            e.Handled = true;
            Error.Exception = e.ExceptionObject;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                (RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).Source = new Uri("/View/Error.xaml", UriKind.Relative);
            });

        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new TransitionFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion
    }
}