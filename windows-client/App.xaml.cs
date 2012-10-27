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
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;

namespace windows_client
{
    public partial class App : Application
    {
        public static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        #region Hike Specific Constants

        public static readonly string LAUNCH_STATE = "app_launch_state";
        public static readonly string PAGE_STATE = "page_State";
        public static readonly string ACCOUNT_NAME = "accountName";
        public static readonly string MSISDN_SETTING = "msisdn";
        public static readonly string COUNTRY_CODE_SETTING = "countryCode";
        public static readonly string REQUEST_ACCOUNT_INFO_SETTING = "raiSettings";
        public static readonly string IS_ADDRESS_BOOK_SCANNED = "isabscanned";
        public static readonly string TOKEN_SETTING = "token";
        public static readonly string UID_SETTING = "uid";
        public static readonly string SMS_SETTING = "smscredits";
        public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
        public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
        public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        public static string sms_invite_message = "Hi! I’m using hike, an awesome new free messaging app from Bharti SoftBank. Download the app at http://get.hike.in/{0} to start messaging me for free!";

        public static readonly string INVITED = "invited";
        public static readonly string INVITED_JOINED = "invitedJoined";

        public static readonly string GROUPS_CACHE = "GroupsCache";
        public static readonly string IS_DB_CREATED = "is_db_created";
        public static readonly string IS_PUSH_ENABLED = "is_push_enabled";
        public static string CONTACT_SCANNING_FAILED = "contactScanningFailed";
        public static string SET_NAME_FAILED = "setNameFailed";
        public static string EMAIL = "email";
        public static string GENDER = "gender";
        public static readonly string VIBRATE_PREF = "vibratePref";
        public static readonly string LAST_UPDATE_CHECK_TIME = "lastUpdateTime";
        public static readonly string LAST_DISMISSED_UPDATE_VERSION = "lastDismissedUpdate";
        public static readonly string LAST_CRITICAL_VERSION = "lastCriticalVersion";
        public static readonly string APP_ID_FOR_LAST_UPDATE = "appID";
        public static readonly string LAST_ANALYTICS_POST_TIME = "analyticsTime";


        #endregion

        #region Hike specific instances and functions

        #region instances

        private static bool isNewInstall = true;
        public static NewChatThread newChatThreadPage = null;
        private static bool _isTombstoneLaunch = false;
        private static bool _isAppLaunched = false;
        public static string MSISDN;
        public static bool ab_scanned = false;
        public static bool isABScanning = false;
        private static HikePubSub mPubSubInstance;
        private static HikeViewModel _viewModel;
        private static DbConversationListener dbListener;
        private static HikeMqttManager mMqttManager;
        private static NetworkManager networkManager;
        private static Dictionary<string, GroupParticipant> groupsCache = null;
        private static UI_Utils ui_utils;
        private static Analytics _analytics;
        private static object lockObj = new object();
        private static LaunchState _appLaunchState = LaunchState.NORMAL_LAUNCH;
        #endregion

        #region PROPERTIES

        public static bool IsAppLaunched
        {
            get
            {
                return _isAppLaunched;
            }
        }

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

        public static UI_Utils UI_UtilsInstance
        {
            get
            {
                return ui_utils;
            }
            set
            {
                if (value != ui_utils)
                {
                    ui_utils = value;
                }
            }
        }

        public static LaunchState APP_LAUNCH_STATE
        {
            get { return _appLaunchState; }
            set
            {
                if (value != _appLaunchState)
                    _appLaunchState = value;
            }
        }

        public static bool IS_TOMBSTONED
        {
            get { return _isTombstoneLaunch; }
            set
            {
                if (value != _isTombstoneLaunch)
                    _isTombstoneLaunch = value;
            }
        }

        public static Analytics AnalyticsInstance
        {
            get
            {
                return _analytics;
            }
            set
            {
                if (value != _analytics)
                {
                    _analytics = value;
                }
            }
        }

        #endregion

        #endregion

        #region PAGE STATE

        public enum PageState
        {
            WELCOME_SCREEN, // WelcomePage Screen
            PHONE_SCREEN,   // EnterNumber Screen
            PIN_SCREEN,     // EnterPin Screen
            SETNAME_SCREEN, // EnterName Screen
            CONVLIST_SCREEN, // ConversationsList Screen
            WALKTHROUGH_SCREEN // Walkthrough Screen
        }

        #endregion

        #region APP LAUNCH STATE

        public enum LaunchState
        {
            NORMAL_LAUNCH, // user clicks the app from menu
            PUSH_NOTIFICATION_LAUNCH,   // app is alunched after push notification is clicked
            SHARE_PICKER_LAUNCH  // app is alunched after share is clicked
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
            //CreateURIMapping();
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

            if (appSettings.Contains(TOKEN_SETTING))
                isNewInstall = false;

            /* Load App token if its there*/
            if (appSettings.Contains(TOKEN_SETTING))
            {
                AccountUtils.Token = (string)appSettings[TOKEN_SETTING];
                appSettings.TryGetValue<string>(App.MSISDN_SETTING, out App.MSISDN);
            }
            RootFrame.Navigating += new NavigatingCancelEventHandler(RootFrame_Navigating);
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            #region SERVER INFO
            string env = (AccountUtils.IsProd) ? "PRODUCTION" : "STAGING";
            Debug.WriteLine("SERVER SETTING : " + env);
            Debug.WriteLine("HOST : " + AccountUtils.HOST);
            Debug.WriteLine("PORT : " + AccountUtils.PORT);
            Debug.WriteLine("MQTT HOST : " + AccountUtils.MQTT_HOST);
            Debug.WriteLine("MQTT PORT : " + AccountUtils.MQTT_PORT);
            #endregion

            _isAppLaunched = true;
            instantiateClasses();
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            _isAppLaunched = false; // this means app is activated, could be tombstone or dormant state
            _isTombstoneLaunch = !e.IsApplicationInstancePreserved; //e.IsApplicationInstancePreserved  --> if this is true its dormant else tombstoned
            try
            {
                _appLaunchState = (LaunchState)PhoneApplicationService.Current.State[LAUNCH_STATE];
            }
            catch
            {
            }

            if (_isTombstoneLaunch)
            {
                instantiateClasses();
            }
            NetworkManager.turnOffNetworkManager = false;
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            NetworkManager.turnOffNetworkManager = true;
            ConversationTableUtils.saveConvObjectList();
            if (Utils.GroupCache == null)
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            App.AnalyticsInstance.saveObject();
            PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState;
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            NetworkManager.turnOffNetworkManager = true;
            ConversationTableUtils.saveConvObjectList();
            if (Utils.GroupCache == null)
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            App.AnalyticsInstance.saveObject();
        }

        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            RootFrame.Navigating -= RootFrame_Navigating;
            string targetPage = e.Uri.ToString();
            //MessageBox.Show(targetPage, "share", MessageBoxButton.OK);
            if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
            {
                _appLaunchState = LaunchState.PUSH_NOTIFICATION_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
                string param = GetParamFromUri(targetPage);
                e.Cancel = true;
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    RootFrame.Navigate(new Uri("/View/NewChatThread.xaml?" + param, UriKind.Relative));
                });
            }

            else if (targetPage != null &&  targetPage.Contains("sharePicker.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
            {
                _appLaunchState = LaunchState.SHARE_PICKER_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
                e.Cancel = true;
                int idx = targetPage.IndexOf("?") + 1;
                string param = targetPage.Substring(idx);
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    RootFrame.Navigate(new Uri("/View/NewSelectUserPage.xaml?" + param, UriKind.Relative));
                });
            }
            else
            {
                _appLaunchState = LaunchState.NORMAL_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
                e.Cancel = true;
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    loadPage();
                });
            }
            
        }

        private string GetParamFromUri(string targetPage)
        {
            int idx = targetPage.IndexOf("msisdn");
            return targetPage.Substring(idx);
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            ConversationTableUtils.saveConvObjectList();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            //MessageBoxResult result = MessageBox.Show("Exception :: ", e.ToString(), MessageBoxButton.OK);
            //if (result == MessageBoxResult.OK)
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            App.AnalyticsInstance.saveObject();
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            ConversationTableUtils.saveConvObjectList();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            App.AnalyticsInstance.saveObject();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            //Running on a device / emulator without debugging
            e.Handled = true;
            Error.Exception = e.ExceptionObject;
            Debug.WriteLine("UNHANDLED EXCEPTION : {0}", e.ExceptionObject.StackTrace);
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
                    createDatabaseAsync();
                    nUri = new Uri("/View/EnterNumber.xaml", UriKind.Relative);
                    break;
                case PageState.PIN_SCREEN:
                    createDatabaseAsync();
                    nUri = new Uri("/View/EnterPin.xaml", UriKind.Relative);
                    break;
                case PageState.SETNAME_SCREEN:
                    createDatabaseAsync();
                    nUri = new Uri("/View/EnterName.xaml", UriKind.Relative);
                    break;
                case PageState.WALKTHROUGH_SCREEN:
                    nUri = new Uri("/View/Walkthrough.xaml", UriKind.Relative);
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

        private static void instantiateClasses()
        {
            #region GROUP CACHE
            if (!App.appSettings.Contains(App.GROUPS_CACHE))
            {
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
            }

            else if (Utils.GroupCache == null)
                Utils.GroupCache = (Dictionary<string, List<GroupParticipant>>)App.appSettings[App.GROUPS_CACHE];
            #endregion
            #region PUBSUB
            Stopwatch st = Stopwatch.StartNew();
            if (App.HikePubSubInstance == null)
                App.HikePubSubInstance = new HikePubSub(); // instantiate pubsub
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Pubsub : {0}", msec);
            #endregion
            #region DBCONVERSATION LISTENER
            st.Reset();
            st.Start();
            if (App.DbListener == null)
                App.DbListener = new DbConversationListener();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate DbListeners : {0}", msec);
            #endregion
            #region NETWORK MANAGER
            st.Reset();
            st.Start();
            App.NetworkManagerInstance = NetworkManager.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Network Manager : {0}", msec);
            #endregion
            #region MQTT MANAGER
            st.Reset();
            st.Start();
            if (App.MqttManagerInstance == null)
                App.MqttManagerInstance = new HikeMqttManager();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate MqttManager : {0}", msec);
            #endregion
            #region UI UTILS
            st.Reset();
            st.Start();
            App.UI_UtilsInstance = UI_Utils.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate UI_Utils : {0}", msec);
            #endregion
            #region ANALYTICS
            st.Reset();
            st.Start();
            App.AnalyticsInstance = Analytics.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Analytics : {0}", msec);
            #endregion
            #region VIEW MODEL

            if (_viewModel == null)
            {
                string current_ver = "1.0.0.0";
                List<ConversationListObject> convList = null;

                // If version exists means build is 1.3.0.0 or later else 1.1.0.0
                if (!isNewInstall && !appSettings.TryGetValue<string>("File_System_Version", out current_ver))
                    convList = ConversationTableUtils.getAllConversations();
                else
                    convList = ConversationTableUtils.getAllConvs();

                if (convList == null || convList.Count == 0 || !App.appSettings.Contains(App.IS_DB_CREATED))
                    _viewModel = new HikeViewModel();
                else
                    _viewModel = new HikeViewModel(convList);

                if (!isNewInstall && !appSettings.TryGetValue<string>("File_System_Version", out current_ver))
                {
                    // save the new single file in isolated storage, delete all old files.
                    ConversationTableUtils.saveConvObjectList(); // this will save the map
                    ConversationTableUtils.deleteAllConversationsOld();

                    // instantiate new directories
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(HikeConstants.ANALYTICS_OBJECT_DIRECTORY))
                        {
                            store.CreateDirectory(HikeConstants.ANALYTICS_OBJECT_DIRECTORY);
                        }
                    }
                }
                if (current_ver == null)
                    current_ver = "1.0.0.0";
                if(!isNewInstall && Utils.compareVersion(Utils.getAppVersion(),current_ver)==1) // this is update
                {
                    App.WriteToIsoStorageSettings("New_Update", true);
                }
            }
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate View Model : {0}", msec);

            #endregion
            #region SMILEY
            PageState ps = PageState.WELCOME_SCREEN;
            appSettings.TryGetValue<PageState>(App.PAGE_STATE, out ps);
            if (ps == PageState.CONVLIST_SCREEN) //  this confirms tombstone
            {
                SmileyParser.Instance.initializeSmileyParser();
            }
            #endregion

            if (!appSettings.Contains("File_System_Version") || (string)appSettings["File_System_Version"] != Utils.getAppVersion())
                App.WriteToIsoStorageSettings("File_System_Version",Utils.getAppVersion());
        }

        public static void createDatabaseAsync()
        {
            if (App.appSettings.Contains(App.IS_DB_CREATED)) // shows db are created
                return;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!string.IsNullOrEmpty(MiscDBUtil.THUMBNAILS) && !store.DirectoryExists(MiscDBUtil.THUMBNAILS))
                    {
                        store.CreateDirectory(MiscDBUtil.THUMBNAILS);
                    }

                    if (!store.DirectoryExists(ConversationTableUtils.CONVERSATIONS_DIRECTORY))
                    {
                        store.CreateDirectory(ConversationTableUtils.CONVERSATIONS_DIRECTORY);
                    }
                    if (!store.DirectoryExists(HikeConstants.SHARED_FILE_LOCATION))
                    {
                        store.CreateDirectory(HikeConstants.SHARED_FILE_LOCATION);
                    }
                    if (!store.DirectoryExists(HikeConstants.ANALYTICS_OBJECT_DIRECTORY))
                    {
                        store.CreateDirectory(HikeConstants.ANALYTICS_OBJECT_DIRECTORY);
                    }
                }
                // Create the database if it does not exist.
                Stopwatch st = Stopwatch.StartNew();
                using (HikeChatsDb db = new HikeChatsDb(MsgsDBConnectionstring))
                {
                    if (db.DatabaseExists() == false)
                        db.CreateDatabase();
                }

                using (HikeUsersDb db = new HikeUsersDb(UsersDBConnectionstring))
                {
                    if (db.DatabaseExists() == false)
                        db.CreateDatabase();
                }

                using (HikeMqttPersistenceDb db = new HikeMqttPersistenceDb(MqttDBConnectionstring))
                {
                    if (db.DatabaseExists() == false)
                        db.CreateDatabase();
                }
                WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("APP: Time to create Dbs : {0}", msec);
            };
            bw.RunWorkerAsync();
        }

        /* This function should always be used to store values to isolated storage
         * Its a thread safe implemenatation to save values.
         * */
        public static void WriteToIsoStorageSettings(List<KeyValuePair<string, object>> kvlist)
        {
            if (kvlist == null)
                return;
            lock (lockObj)
            {
                for (int i = 0; i < kvlist.Count; i++)
                {
                    string key = kvlist[i].Key;
                    object value = kvlist[i].Value;
                    appSettings[key] = value;
                }
                appSettings.Save();
            }
        }

        /* This function should always be used to store values to isolated storage
         * Its a thread safe implemenatation to save values.
         * */
        public static void WriteToIsoStorageSettings(string key, object value)
        {
            lock (lockObj)
            {
                try
                {
                    appSettings[key] = value;
                    appSettings.Save();
                }
                catch
                {
                    Debug.WriteLine("Problem while saving to isolated storage.");
                }
            }
        }

        public static void ClearAppSettings()
        {
            lock (lockObj)
            {
                try
                {
                    appSettings.Clear();
                    appSettings.Save();
                }
                catch
                {
                    Debug.WriteLine("Problem while clearing isolated storage.");
                }
            }
        }

        public static void RemoveKeyFromAppSettings(string key)
        {
            lock (lockObj)
            {
                try
                {
                    appSettings.Remove(key);
                    appSettings.Save();
                }
                catch
                {
                    Debug.WriteLine("Problem while removing key from isolated storage.");
                }
            }
        }
    }
}