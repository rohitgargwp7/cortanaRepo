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
using System.Linq;
using System.IO;

namespace windows_client
{
    public partial class App : Application
    {
        public static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        #region Hike Specific Constants

        public static NewChatThread newChatThreadPage = null;
        public static readonly string PAGE_STATE = "page_State";
        public static readonly string ACCOUNT_NAME = "accountName";
        public static readonly string MSISDN_SETTING = "msisdn";
        public static readonly string REQUEST_ACCOUNT_INFO_SETTING = "raiSettings";
        public static readonly string IS_ADDRESS_BOOK_SCANNED = "isabscanned";
        public static readonly string TOKEN_SETTING = "token";
        public static readonly string UID_SETTING = "uid";
        public static readonly string SMS_SETTING = "smscredits";
        public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
        public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
        public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        public static readonly string invite_message = "Hey! I\'m using hike to send SMS for free. Download it at http://get.hike.in and start messaging me and other friends for free!";
        public static readonly string INVITED = "invited";
        public static readonly string INVITED_JOINED = "invitedJoined";
        public static readonly string TOTAL_CREDITS_PER_MONTH = "tc";
        public static readonly string GROUPS_CACHE = "GroupsCache";
        public static readonly string IS_DB_CREATED = "is_db_created";
        public static readonly string IS_PUSH_ENABLED = "is_push_enabled";
        public static string CONTACT_SCANNING_FAILED = "contactScanningFailed";
        public static string SET_NAME_FAILED = "setNameFailed";
        public static string EMAIL = "email";
        public static string GENDER = "gender";

        #endregion

        #region Hike specific instances and functions

        #region instances
        private static bool _isLaunch = false;
        public static bool isConvCreated = false;
        public static string MSISDN;
        public static bool ab_scanned = false;
        public static bool isABScanning = false;
        private static HikePubSub mPubSubInstance;
        private static HikeViewModel _viewModel;
        private static DbConversationListener dbListener;
        private static HikeMqttManager mMqttManager;
        private static NetworkManager networkManager;
        private static Dictionary<string, GroupParticipant> groupsCache = null;
        private static object lockObj = new object();

        #endregion

        #region PROPERTIES

        public static bool IsAppLaunched
        {
            get
            {
                return _isLaunch;
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
            if (appSettings.Contains(App.TOKEN_SETTING))
            {
                AccountUtils.Token = (string)appSettings[App.TOKEN_SETTING];
                App.MSISDN = (string)appSettings[App.MSISDN_SETTING];
                AccountUtils.UID = (string)appSettings[App.UID_SETTING];
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            _isLaunch = true;
            Stopwatch st = Stopwatch.StartNew();
            instantiateClasses();
            loadPage();
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("App : Time to load page : {0}", msec);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            if (!isConvCreated)
            {
                instantiateClasses();
                PageState ps = PageState.WELCOME_SCREEN;
                appSettings.TryGetValue<PageState>(App.PAGE_STATE, out ps);
                if (ps == PageState.CONVLIST_SCREEN) //  this confirms tombstone
                {
                    ConversationsList.LoadMessages();
                    if (ConversationsList.ConvMap == null)
                        ConversationsList.ConvMap = new Dictionary<string, ConversationListObject>();
                    SmileyParser.loadEmoticons();
                }
            }
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            //SerializeConversations();
            if (Utils.GroupCache == null)
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            updateConversations();
            if (Utils.GroupCache == null)
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
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
            WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            // Running on a device / emulator without debugging
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
            if (!App.appSettings.Contains(App.GROUPS_CACHE))
            {
                Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
                WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
            }

            else
                Utils.GroupCache = (Dictionary<string, List<GroupParticipant>>)App.appSettings[App.GROUPS_CACHE];

            Stopwatch st = Stopwatch.StartNew();
            if (App.HikePubSubInstance == null)
                App.HikePubSubInstance = new HikePubSub(); // instantiate pubsub
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Pubsub : {0}", msec);

            st.Reset();
            st.Start();
            if (App.DbListener == null)
                App.DbListener = new DbConversationListener();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate DbListeners : {0}", msec);

            st.Reset();
            st.Start();
            App.NetworkManagerInstance = NetworkManager.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Network Manager : {0}", msec);

            st.Reset();
            st.Start();
            if (App.MqttManagerInstance == null)
                App.MqttManagerInstance = new HikeMqttManager();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate MqttManager : {0}", msec);

            st.Reset();
            st.Start();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate UI_Utils : {0}", msec);

            if (_viewModel == null)
                _viewModel = new HikeViewModel();
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

        public static void clearAllDatabasesAsync()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                MiscDBUtil.clearDatabase();
            };
            bw.RunWorkerAsync();
        }

        private void updateConversations()
        {
            //bool shouldUpdate = false;
            //using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            //{

            //    for (int i = 0; i < App.ViewModel.ConvMsisdnsToUpdate.Count; i++)
            //    {
            //        string msisdn = App.ViewModel.ConvMsisdnsToUpdate[i];
            //        if (ConversationsList.ConvMap.ContainsKey(msisdn))
            //        {
            //            ConversationListObject obj = ConversationsList.ConvMap[msisdn];
            //            IQueryable<ConversationListObject> q = DbCompiledQueries.GetConvForMsisdn(context, obj.Msisdn);
            //            ConversationListObject cObj = q.FirstOrDefault();
            //            if (cObj == null)
            //                return;
            //            cObj.MessageStatus = obj.MessageStatus;
            //            cObj.LastMessage = obj.LastMessage;
            //            cObj.TimeStamp = obj.TimeStamp;
            //            shouldUpdate = true;
            //        }
            //    }
            //    if (shouldUpdate)
            //        MessagesTableUtils.SubmitWithConflictResolve(context);
            //}
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
    }
}