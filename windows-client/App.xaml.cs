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

        public static readonly string ACCEPT_TERMS = "acceptterms";
        public static readonly string MSISDN_SETTING = "msisdn";
        public static readonly string NAME_SETTING = "name";
        public static readonly string ADDRESS_BOOK_SCANNED = "abscanned";
        public static readonly string TOKEN_SETTING = "token";
        public static readonly string MESSAGES_SETTING = "messageid";
        public static readonly string PIN_SETTING = "pincode";
        public static readonly string UID_SETTING = "uid";
        public static readonly string SMS_SETTING = "smscredits";

        #endregion

        #region Hike specific instances and functions

        #region instances

        private static bool ab_scanned = false;
        private static HikePubSub mPubSubInstance;
        private static HikeDataContext hikeDataContext;
        private static HikeViewModel _viewModel;
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

        public static HikeDataContext HikeDataContextInstance
        {
            get
            {
                return hikeDataContext;
            }
            set
            {
                if (value != hikeDataContext)
                {
                    hikeDataContext = value;
                }
            }
        }

        public static HikeViewModel ViewModel
        {
            get
            {
                return _viewModel;
            }
            set
            {
                if (value != _viewModel)
                {
                    _viewModel = value;
                }
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
        
        #endregion

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

            string DBConnectionstring = "Data Source=isostore:/HikeDB.sdf";

            // Create the database if it does not exist.

            App.HikeDataContextInstance = new HikeDataContext(DBConnectionstring);

            if (App.HikeDataContextInstance.DatabaseExists() == false)
            {
                // Create the local database.
                App.HikeDataContextInstance.CreateDatabase();
            }

            #endregion

            #region Instantiate app instances

            mPubSubInstance = new HikePubSub(); // instantiate pubsub
            _viewModel = new HikeViewModel();  // instantiate HikeviewModel 
            dbListener = new DbConversationListener();
            mMqttManager = new HikeMqttManager();
            networkManager = NetworkManager.Instance;

            #endregion

        }


        private void loadPage()
        {

            Uri nUri = null;

            if (!appSettings.Contains(App.ACCEPT_TERMS) || "f" == appSettings[App.ACCEPT_TERMS].ToString())
            {
                nUri = new Uri("/View/WelcomePage.xaml", UriKind.Relative);
                /* test function */
            }
            else if (!appSettings.Contains(App.MSISDN_SETTING) || "f" == appSettings[App.MSISDN_SETTING].ToString())
            {
                nUri = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
            }
            else if (!appSettings.Contains(App.PIN_SETTING) || "f" == appSettings[App.PIN_SETTING].ToString())
            {
                nUri = new Uri("/View/EnterPin.xaml", UriKind.Relative);
            }
            else if (!appSettings.Contains(App.NAME_SETTING) || "f" == appSettings[App.NAME_SETTING].ToString())
            {
                nUri = new Uri("/View/EnterName.xaml", UriKind.Relative);
                if (appSettings.Contains(App.ADDRESS_BOOK_SCANNED) && "y" == (string)appSettings[App.ADDRESS_BOOK_SCANNED])
                {
                    ab_scanned = true;
                }
            }
            else
            {
                nUri = new Uri("/View/MessageList.xaml", UriKind.Relative);
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