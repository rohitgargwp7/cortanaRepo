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
using windows_client.Misc;
using System.Net.NetworkInformation;
using Microsoft.Phone.Net.NetworkInformation;

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

        public static readonly string TOKEN_SETTING = "token";
        public static readonly string UID_SETTING = "uid";
        public static readonly string SMS_SETTING = "smscredits";
        public static readonly string SHOW_FREE_SMS_SETTING = "freeSMS";
        public static readonly string STATUS_UPDATE_SETTING = "stUpSet";
        public static readonly string STATUS_UPDATE_FIRST_SETTING = "stUpFirSet";
        public static readonly string STATUS_UPDATE_SECOND_SETTING = "stUpSecSet";
        public static readonly string SHOW_FAVORITES_TUTORIAL = "favoritesTute";
        public static readonly string SHOW_NUDGE_TUTORIAL = "nudgeTute";
        public static readonly string LATEST_PUSH_TOKEN = "pushToken";
        public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
        public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
        public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        public static readonly string APP_UPDATE_POSTPENDING = "updatePost";

        public static readonly string INVITED = "invited";
        public static readonly string INVITED_JOINED = "invitedJoined";

        public static readonly string GROUPS_CACHE = "GroupsCache";
        public static readonly string IS_DB_CREATED = "is_db_created";
        public static readonly string IS_PUSH_ENABLED = "is_push_enabled";

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
        private static string _currentVersion = "1.0.0.0";
        private static string _latestVersion;
        public static bool IS_VIEWMODEL_LOADED = false;
        public static bool IS_MARKETPLACE = false; // change this to toggle debugging
        private static bool isNewInstall = true;
        public static NewChatThread newChatThreadPage = null;
        private static bool _isTombstoneLaunch = false;
        private static bool _isAppLaunched = false;
        public static string MSISDN;
        private static HikePubSub mPubSubInstance;
        private static HikeViewModel _viewModel;
        private static DbConversationListener dbListener;
        private static HikeMqttManager mMqttManager;
        private static NetworkManager networkManager;
        private static UI_Utils ui_utils;
        private static Analytics _analytics;
        private static LaunchState _appLaunchState = LaunchState.NORMAL_LAUNCH;
        private static PageState ps = PageState.WELCOME_SCREEN;

        private static object lockObj = new object();
        //public static object AppGlobalLock = new object(); // this lock will be used across system to sync 2 diff threads example network manager and deleting all threads

        #endregion

        #region PROPERTIES

        public static PageState PageStateVal
        {
            get
            {
                return ps;
            }

        }
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

        public static string CURRENT_VERSION
        {
            get
            {
                return _currentVersion;
            }
        }

        public static string LATEST_VERSION
        {
            get
            {
                return _latestVersion;
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
            WELCOME_HIKE_SCREEN,
            SETNAME_SCREEN, // EnterName Screen
            CONVLIST_SCREEN, // ConversationsList Screen
            NUX_SCREEN_FRIENDS,// Nux Screen for friends
            NUX_SCREEN_FAMILY,// Nux Screen for family
            UPGRADE_SCREEN//Upgrade page
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
            if (ps != PageState.WELCOME_SCREEN)
            {
                #region SERVER INFO
                string env = (AccountUtils.IsProd) ? "PRODUCTION" : "STAGING";
                Debug.WriteLine("SERVER SETTING : " + env);
                Debug.WriteLine("HOST : " + AccountUtils.HOST);
                Debug.WriteLine("PORT : " + AccountUtils.PORT);
                Debug.WriteLine("MQTT HOST : " + AccountUtils.MQTT_HOST);
                Debug.WriteLine("MQTT PORT : " + AccountUtils.MQTT_PORT);
                #endregion
            }
            _isAppLaunched = true;
            appInitialize();
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
            catch (Exception ex)
            {
                Debug.WriteLine("App :: Application_Activated : Setting launch state , Exception : " + ex.StackTrace);
            }

            if (_isTombstoneLaunch)
            {
                if (appSettings.TryGetValue<PageState>(App.PAGE_STATE, out ps))
                    isNewInstall = false;
                instantiateClasses(false);
            }
            else
            {
                if (ps == PageState.CONVLIST_SCREEN)
                    MqttManagerInstance.connect();
            }
            NetworkManager.turnOffNetworkManager = false;
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            NetworkManager.turnOffNetworkManager = true;
            App.AnalyticsInstance.saveObject();
            PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState;
            if (IS_VIEWMODEL_LOADED)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && App.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
            }
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            App.AnalyticsInstance.saveObject();
            appDeinitialize();
        }

        private void appInitialize()
        {
            DeviceNetworkInformation.NetworkAvailabilityChanged += OnNetworkChange;
            #region PUSH NOTIFICATIONS STUFF

            bool isPushEnabled = true;
            appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
            if (isPushEnabled)
            {
                PushHelper.Instance.registerPushnotifications();
            }
            #endregion
        }

        private void appDeinitialize()
        {
            DeviceNetworkInformation.NetworkAvailabilityChanged -= OnNetworkChange;
        }

        private void OnNetworkChange(object sender, EventArgs e)
        {
            //reconnect mqtt whenever phone is reconnected without relaunch 
            if (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
            {
                App.MqttManagerInstance.connect();
                bool isPushEnabled = true;
                App.appSettings.TryGetValue<bool>(App.IS_PUSH_ENABLED, out isPushEnabled);
                if (isPushEnabled)
                {
                    PushHelper.Instance.registerPushnotifications();
                }
            }
            else
            {
                App.MqttManagerInstance.setConnectionStatus(Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET);
            }
        }

        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            RootFrame.Navigating -= RootFrame_Navigating;

            if (appSettings.TryGetValue<PageState>(App.PAGE_STATE, out ps))
                isNewInstall = false;

            /*
            * These changes are done from version 2.0.0.0 , in WP8 devices after status upgrade
            */

            // this will get the current version installed already in "_currentVersion"
            appSettings.TryGetValue<string>(HikeConstants.FILE_SYSTEM_VERSION, out _currentVersion);
            _latestVersion = Utils.getAppVersion(); // this will get the new version we are upgrading to

            string targetPage = e.Uri.ToString();
            if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn"))
            {
                PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO] = targetPage;
                _appLaunchState = LaunchState.PUSH_NOTIFICATION_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
            }
            else if (targetPage != null && targetPage.Contains("sharePicker.xaml") && targetPage.Contains("FileId"))
            {
                PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO] = targetPage;
                _appLaunchState = LaunchState.SHARE_PICKER_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
            }
            else
            {
                PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO] = string.Empty;
                _appLaunchState = LaunchState.NORMAL_LAUNCH;
                PhoneApplicationService.Current.State[LAUNCH_STATE] = _appLaunchState; // this will be used in tombstone and dormant state
            }
            e.Cancel = true;

            // if not new install && current version is less than version 2.0.0.0  and upgrade is done for wp8 device
            if (!isNewInstall && Utils.compareVersion(_currentVersion, "2.0.0.0") == -1 && Utils.IsWP8)
            {
                instantiateClasses(true);
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    RootFrame.Navigate(new Uri("/View/UpgradePage.xaml", UriKind.Relative));
                });
            }
            else
            {
                if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
                {
                    instantiateClasses(false);
                    string param = Utils.GetParamFromUri(targetPage);
                    RootFrame.Dispatcher.BeginInvoke(delegate
                    {
                        RootFrame.Navigate(new Uri("/View/NewChatThread.xaml?" + param, UriKind.Relative));
                    });
                }

                else if (targetPage != null && targetPage.Contains("sharePicker.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
                {
                    instantiateClasses(false);
                    if (ps != PageState.CONVLIST_SCREEN)
                    {
                        RootFrame.Dispatcher.BeginInvoke(delegate
                        {
                            Uri nUri = Utils.LoadPageUri(ps);
                            ((App)Application.Current).RootFrame.Navigate(nUri);
                            return;
                        });
                    }
                    int idx = targetPage.IndexOf("?") + 1;
                    string param = targetPage.Substring(idx);
                    RootFrame.Dispatcher.BeginInvoke(delegate
                    {
                        RootFrame.Navigate(new Uri("/View/NewSelectUserPage.xaml?" + param, UriKind.Relative));
                    });
                }
                else
                {
                    instantiateClasses(false);
                    RootFrame.Dispatcher.BeginInvoke(delegate
                    {
                        Uri nUri = Utils.LoadPageUri(ps);
                        ((App)Application.Current).RootFrame.Navigate(nUri);
                    });
                }
            }
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            //MessageBoxResult result = MessageBox.Show("Exception :: ", e.ToString(), MessageBoxButton.OK);
            //if (result == MessageBoxResult.OK)
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            App.AnalyticsInstance.saveObject();
            if (IS_VIEWMODEL_LOADED)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && App.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {

            App.AnalyticsInstance.saveObject();
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            if (!IS_MARKETPLACE)
            {
                //Running on a device / emulator without debugging
                e.Handled = true;
                Error.Exception = e.ExceptionObject;
                Debug.WriteLine("UNHANDLED EXCEPTION : {0}", e.ExceptionObject.StackTrace);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(e.ExceptionObject.ToString(), "Exception", MessageBoxButton.OK);
                    //(RootVisual as Microsoft.Phone.Controls.PhoneApplicationFrame).Source = new Uri("/View/Error.xaml", UriKind.Relative);
                });
            }
            if (IS_VIEWMODEL_LOADED)
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && App.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
            }
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

        private static void instantiateClasses(bool initInUpgradePage)
        {
            #region GROUP CACHE

            if (App.appSettings.Contains(App.GROUPS_CACHE)) // this will happen just once and no need to check version as this will work  for all versions
            {
                GroupManager.Instance.GroupCache = (Dictionary<string, List<GroupParticipant>>)App.appSettings[App.GROUPS_CACHE];
                GroupManager.Instance.SaveGroupCache();
                RemoveKeyFromAppSettings(App.GROUPS_CACHE);
            }

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
            if (ps == PageState.CONVLIST_SCREEN)
            {
                NetworkManager.turnOffNetworkManager = true;
                App.MqttManagerInstance.connect();
            }
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
            #region PUSH HELPER
            st.Reset();
            st.Start();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Push helper : {0}", msec);
            #endregion
            #region SMILEY
            if (ps == PageState.CONVLIST_SCREEN) //  this confirms tombstone
            {
                SmileyParser.Instance.initializeSmileyParser();
            }
            #endregion
            #region RATE MY APP
            if (isNewInstall)
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, 1);
            #endregion
            #region VIEW MODEL

            IS_VIEWMODEL_LOADED = false;
            if (_viewModel == null)
            {
                _latestVersion = Utils.getAppVersion(); // this will get the new version we have installed
                List<ConversationListObject> convList = null;

                if (!isNewInstall)// this has to be called for no new install case
                    convList = GetConversations();
                else // new install case
                {
                    convList = null;
                    App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, _latestVersion);// new install so write version
                }

                if (convList == null || convList.Count == 0)
                    _viewModel = new HikeViewModel();
                else
                    _viewModel = new HikeViewModel(convList);

                if (!initInUpgradePage)
                {
                    if (!isNewInstall && Utils.compareVersion(_latestVersion, _currentVersion) == 1) // shows this is update
                    {
                        appSettings[App.APP_UPDATE_POSTPENDING] = true;
                        appSettings[HikeConstants.AppSettings.NEW_UPDATE] = true;
                        WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, _latestVersion);
                        if (Utils.compareVersion(_currentVersion, "1.5.0.0") != 1) // if current version is less than equal to 1.5.0.0 then upgrade DB
                            MqttDBUtils.MqttDbUpdateToLatestVersion();
                    }
                }
                st.Stop();
                msec = st.ElapsedMilliseconds;
                Debug.WriteLine("APP: Time to Instantiate View Model : {0}", msec);
                IS_VIEWMODEL_LOADED = true;

            }
            #endregion
            #region POST APP INFO ON UPDATE
            // if app info is already sent to server , this function will automatically handle
            UpdatePostHelper.Instance.postAppInfo();
            #endregion
        }

        public static void createDatabaseAsync()
        {
            if (App.appSettings.Contains(App.IS_DB_CREATED)) // shows db are created
                return;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!string.IsNullOrEmpty(MiscDBUtil.THUMBNAILS) && !store.DirectoryExists(MiscDBUtil.THUMBNAILS))
                        {
                            store.CreateDirectory(MiscDBUtil.THUMBNAILS);
                        }
                        if (!string.IsNullOrEmpty(MiscDBUtil.MISC_DIR) && !store.DirectoryExists(MiscDBUtil.MISC_DIR))
                        {
                            store.CreateDirectory(MiscDBUtil.MISC_DIR);
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
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: createDatabaseAsync : createDatabaseAsync , Exception : " + ex.StackTrace);
                    RemoveKeyFromAppSettings(App.IS_DB_CREATED);
                }

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
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: WriteToIsoStorageSettings, Exception : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: ClearAppSettings, Exception : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: RemoveKeyFromAppSettings, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// This function handles any upgrade process in Conversations and AppSettings only
        /// </summary>
        /// <returns></returns>
        private static List<ConversationListObject> GetConversations()
        {
            List<ConversationListObject> convList = null;
            appSettings.TryGetValue<string>(HikeConstants.FILE_SYSTEM_VERSION, out _currentVersion);
            if (_currentVersion == null)
                _currentVersion = "1.0.0.0";

            // this will ensure that we will show tutorials in case of app upgrade from any version to version later that 1.5.0.8
            if (Utils.compareVersion(_currentVersion, "1.5.0.8") != 1) // current version is less than equal to 1.5.0.8
            {
                App.appSettings[App.SHOW_FAVORITES_TUTORIAL] = true;
                WriteToIsoStorageSettings(App.SHOW_NUDGE_TUTORIAL, true);
            }
           
            if (_currentVersion == "1.0.0.0")  // user is upgrading from version 1.0.0.0 to latest
            {
                /*
                 * 1. Read from individual files.
                 * 2. Overite old files as they are written in a wrong format
                 */
                convList = ConversationTableUtils.getAllConversations(); // this function will read according to the old logic of Version 1.0.0.0
                ConversationTableUtils.saveConvObjectListIndividual(convList);
                App.appSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = (convList != null) ? convList.Count : 0;
                // there was no country code in first version, and as first version was released in India , we are setting value to +91 
                App.appSettings[COUNTRY_CODE_SETTING] = "+91";
                App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, true);
                return convList;
            }
            else if (Utils.compareVersion(_currentVersion, "1.5.0.0") != 1) // current version is less than equal to 1.5.0.0 and greater than 1.0.0.0
            {
                /*
                 * 1. Read from Convs File
                 * 2. Store each conv in an individual file.
                 */
                convList = ConversationTableUtils.getAllConvs();
                ConversationTableUtils.saveConvObjectListIndividual(convList);
                App.appSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = convList != null ? convList.Count : 0;

                string country_code = null;
                App.appSettings.TryGetValue<string>(App.COUNTRY_CODE_SETTING, out country_code);
                if (string.IsNullOrEmpty(country_code) || country_code == "+91")
                    App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, true);
                else
                    App.WriteToIsoStorageSettings(App.SHOW_FREE_SMS_SETTING, false);
                return convList;
            }

            else // this corresponds to the latest version and is called everytime except update launch
            {
                int convs = 0;
                appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                convList = ConversationTableUtils.getAllConvs();

                int convListCount = convList == null ? 0 : convList.Count;
                // This shows something failed while reading from Convs , so move to backup plan i.e read from individual files
                if (convListCount != convs)
                    convList = ConversationTableUtils.GetConvsFromIndividualFiles();

                return convList;
            }
        }
    }
}