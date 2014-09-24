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
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Media;
namespace windows_client.utils
{
    public class HikeInstantiation
    {
        public static readonly IsolatedStorageSettings appSettings = IsolatedStorageSettings.ApplicationSettings;

        #region Hike Specific Constants

        public static readonly string PAGE_STATE = "page_State";
        public static readonly string ACCOUNT_NAME = "accountName";
        public static readonly string ACCOUNT_GENDER = "accountGender";
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
        public static readonly string LAST_SEEN_SEETING = "lstSeenSet";
        public static readonly string USE_LOCATION_SETTING = "locationSet";
        public static readonly string AUTO_DOWNLOAD_SETTING = "autoDownload";
        public static readonly string AUTO_RESUME_SETTING = "autoResume";

        public static readonly string HIDE_MESSAGE_PREVIEW_SETTING = "hideMessagePreview";

        public static readonly string ENTER_TO_SEND = "enterToSend";
        public static readonly string SEND_NUDGE = "sendNudge";
        public static readonly string DISPLAYPIC_FAV_ONLY = "dpFavorites";
        public static readonly string SHOW_NUDGE_TUTORIAL = "nudgeTute";
        public static readonly string SHOW_STATUS_UPDATES_TUTORIAL = "statusTut";
        public static readonly string SHOW_BASIC_TUTORIAL = "basicTut";
        public static readonly string HIDE_CRICKET_MOODS = "cmoods";
        public static readonly string LATEST_PUSH_TOKEN = "pushToken";
        public static readonly string MsgsDBConnectionstring = "Data Source=isostore:/HikeChatsDB.sdf";
        public static readonly string UsersDBConnectionstring = "Data Source=isostore:/HikeUsersDB.sdf";
        public static readonly string MqttDBConnectionstring = "Data Source=isostore:/HikeMqttDB.sdf";
        public static readonly string APP_UPDATE_POSTPENDING = "updatePost";
        public static readonly string AUTO_SAVE_MEDIA = "autoSavePhoto";

        public static readonly string CHAT_THREAD_COUNT_KEY = "chatThreadCountKey";
        public static readonly string TIP_MARKED_KEY = "tipMarkedKey";
        public static readonly string TIP_SHOW_KEY = "tipShowKey";
        public static readonly string PRO_TIP = "proTip";
        public static readonly string PRO_TIP_COUNT = "proTipCount";
        public static readonly string PRO_TIP_DISMISS_TIME = "proTipDismissTime";
        public static readonly string PRO_TIP_LAST_DISMISS_TIME = "proTipLastDismissTime";

        public static readonly string INVITED = "invited";
        public static readonly string INVITED_JOINED = "invitedJoined";

        public static readonly string GROUPS_CACHE = "GroupsCache";
        public static readonly string IS_DB_CREATED = "is_db_created";
        public static readonly string IS_PUSH_ENABLED = "is_push_enabled";
        public static readonly string IP_LIST = "ip_list";

        public static string EMAIL = "email";
        public static string GENDER = "gender";
        public static string NAME = "name";
        public static string DOB = "dob";
        public static string YEAR = "year";
        public static string SCREEN = "screen";
        public static readonly string VIBRATE_PREF = "vibratePref";
        public static readonly string HIKEJINGLE_PREF = "jinglePref";
        public static readonly string APP_ID_FOR_LAST_UPDATE = "appID";
        public static readonly string LAST_ANALYTICS_POST_TIME = "analyticsTime";

        public static readonly string CURRENT_LOCALE = "curLocale";

        public static readonly string GROUP_NAME = "groupName";
        public static readonly string HAS_CUSTOM_IMAGE = "hasCustomImage";
        public static readonly string NEW_GROUP_ID = "newGroupId";

        #endregion

        #region Hike specific instances and functions

        #region instances
        private static string _currentVersion;
        private static string _latestVersion;
        public static bool IS_VIEWMODEL_LOADED = false;
        public static bool IS_MARKETPLACE = false; // change this to toggle debugging
        private static bool _IsNewInstall = true;
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
        private static PageState ps = PageState.WELCOME_SCREEN;

        private static object lockObj = new object();

        #endregion

        #region PROPERTIES

        public static PageState PageStateVal
        {
            set
            {
                if (value != ps)
                    ps = value;
            }
            get
            {
                return ps;
            }
        }

        public static bool IsAppLaunched
        {
            set
            {
                if (value != _isAppLaunched)
                    _isAppLaunched = value;
            }
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
            set
            {
                _currentVersion = value;
            }
            get
            {
                return _currentVersion;
            }
        }

        public static string LATEST_VERSION
        {
            set
            {
                if (value != _latestVersion)
                    _latestVersion = value;
            }
            get
            {
                return _latestVersion;
            }
        }

        public static bool IsNewInstall
        {
            set
            {
                if (value != _IsNewInstall)
                    _IsNewInstall = value;
            }
            get
            {
                return _IsNewInstall;
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
            TUTORIAL_SCREEN_STATUS,
            TUTORIAL_SCREEN_STICKERS,
            SETNAME_SCREEN, // EnterName Screen
            CONVLIST_SCREEN, // ConversationsList Screen
            UPGRADE_SCREEN,//Upgrade page

            //the below pages have been removed after 2.2, but still we need these to handle them in case of upgrade.
            //app settings is storing pagestate in form of object and not value, hence while converting on upgrade
            //app settings is getting corrupted which throws the ap to welcome page
            WELCOME_HIKE_SCREEN,
            NUX_SCREEN_FRIENDS,// Nux Screen for friends
            NUX_SCREEN_FAMILY// Nux Screen for family
        }

        #endregion


        public static void instantiateClasses(bool initInUpgradePage)
        {
            #region Hidden Mode
            if (IsNewInstall || Utils.compareVersion(_currentVersion, "2.6.5.0") < 0)
                WriteToIsoStorageSettings(HikeConstants.HIDDEN_TOOLTIP_STATUS, ToolTipMode.HIDDEN_MODE_GETSTARTED);
            #endregion
            #region Upgrade Pref Contacts Fix
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.6.2.0") < 0)
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettings.CONTACTS_TO_SHOW);
            #endregion
            #region ProTips 2.3.0.0
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.3.0.0") < 0)
            {
                try
                {
                    var proTip = new ProTip();
                    HikeInstantiation.appSettings.TryGetValue(HikeInstantiation.PRO_TIP, out proTip);

                    if (proTip != null)
                    {
                        HikeInstantiation.RemoveKeyFromAppSettings(HikeInstantiation.PRO_TIP);
                        HikeInstantiation.appSettings[HikeInstantiation.PRO_TIP] = proTip._id;
                    }
                }
                catch { }

                HikeInstantiation.RemoveKeyFromAppSettings(HikeInstantiation.PRO_TIP_DISMISS_TIME);
                ProTipHelper.Instance.ClearOldProTips();
            }
            #endregion
            #region LAST SEEN BYTE TO BOOL FIX

            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.2.2.0") < 0)
            {
                try
                {
                    byte value;
                    if (HikeInstantiation.appSettings.TryGetValue(HikeInstantiation.LAST_SEEN_SEETING, out value))
                    {
                        HikeInstantiation.appSettings.Remove(HikeInstantiation.LAST_SEEN_SEETING);
                        HikeInstantiation.appSettings.Save();

                        if (value <= 0)
                            HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.LAST_SEEN_SEETING, false);
                    }
                }
                catch (InvalidCastException ex)
                {
                    // will not reach here for new user & upgraded user.
                }
            }

            #endregion
            #region IN APP TIPS

            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.7.5.0") < 0)
            {
                HikeInstantiation.appSettings.Remove(HikeInstantiation.TIP_MARKED_KEY);
                HikeInstantiation.appSettings.Remove(HikeInstantiation.TIP_SHOW_KEY);
                HikeInstantiation.RemoveKeyFromAppSettings(HikeInstantiation.CHAT_THREAD_COUNT_KEY);
            }

            #endregion
            #region STCIKERS
            if (IsNewInstall || Utils.compareVersion(_currentVersion, "2.6.2.0") < 0)
            {
                if (!IsNewInstall && Utils.compareVersion("2.2.2.0", _currentVersion) == 1)
                    StickerHelper.DeleteCategory(StickerHelper.CATEGORY_HUMANOID);

                StickerHelper.CreateDefaultCategories();
            }
            #endregion
            #region TUTORIAL
            if (!IsNewInstall && Utils.compareVersion("2.6.0.0", _currentVersion) == 1)
            {
                if (ps == PageState.CONVLIST_SCREEN || ps == PageState.TUTORIAL_SCREEN_STATUS || ps == PageState.TUTORIAL_SCREEN_STICKERS
                    || ps == PageState.WELCOME_HIKE_SCREEN || ps == PageState.NUX_SCREEN_FAMILY || ps == PageState.NUX_SCREEN_FRIENDS)
                {
                    RemoveKeyFromAppSettings(HikeInstantiation.SHOW_STATUS_UPDATES_TUTORIAL);
                    ps = PageState.CONVLIST_SCREEN;
                    RemoveKeyFromAppSettings(HikeInstantiation.SHOW_BASIC_TUTORIAL);
                    HikeInstantiation.WriteToIsoStorageSettings(PAGE_STATE, ps);
                }
            }
            #endregion
            #region GROUP CACHE

            if (HikeInstantiation.appSettings.Contains(HikeInstantiation.GROUPS_CACHE)) // this will happen just once and no need to check version as this will work  for all versions
            {
                GroupManager.Instance.GroupCache = (Dictionary<string, List<GroupParticipant>>)HikeInstantiation.appSettings[HikeInstantiation.GROUPS_CACHE];
                GroupManager.Instance.SaveGroupCache();
                RemoveKeyFromAppSettings(HikeInstantiation.GROUPS_CACHE);
            }

            #endregion
            #region PUBSUB
            Stopwatch st = Stopwatch.StartNew();
            if (HikeInstantiation.HikePubSubInstance == null)
                HikeInstantiation.HikePubSubInstance = new HikePubSub(); // instantiate pubsub
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Pubsub : {0}", msec);
            #endregion
            #region DBCONVERSATION LISTENER
            st.Reset();
            st.Start();
            if (HikeInstantiation.DbListener == null)
                HikeInstantiation.DbListener = new DbConversationListener();
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate DbListeners : {0}", msec);
            #endregion
            #region NETWORK MANAGER
            st.Reset();
            st.Start();
            HikeInstantiation.NetworkManagerInstance = NetworkManager.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate Network Manager : {0}", msec);
            #endregion
            #region MQTT MANAGER
            st.Reset();
            st.Start();
            if (HikeInstantiation.MqttManagerInstance == null)
                HikeInstantiation.MqttManagerInstance = new HikeMqttManager();
            if (ps == PageState.CONVLIST_SCREEN)
            {
                NetworkManager.turnOffNetworkManager = true;
                HikeInstantiation.MqttManagerInstance.connect();
            }
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate MqttManager : {0}", msec);
            #endregion
            #region UI UTILS
            st.Reset();
            st.Start();
            HikeInstantiation.UI_UtilsInstance = UI_Utils.Instance;
            st.Stop();
            msec = st.ElapsedMilliseconds;
            Debug.WriteLine("APP: Time to Instantiate UI_Utils : {0}", msec);
            #endregion
            #region ANALYTICS
            st.Reset();
            st.Start();
            HikeInstantiation.AnalyticsInstance = Analytics.Instance;
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
            if (IsNewInstall)
            {
                HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, 1);
            }
            #endregion
            #region VIEW MODEL

            IS_VIEWMODEL_LOADED = false;
            if (_viewModel == null)
            {
                _latestVersion = Utils.getAppVersion(); // this will get the new version we have installed
                List<ConversationListObject> convList = null;

                if (!IsNewInstall)// this has to be called for no new install case
                    convList = GetConversations();
                else // new install case
                {
                    convList = null;
                    HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, _latestVersion);// new install so write version
                }

                if (convList == null || convList.Count == 0)
                    _viewModel = new HikeViewModel();
                else
                    _viewModel = new HikeViewModel(convList);

                if (!IsNewInstall && Utils.compareVersion(_latestVersion, _currentVersion) == 1) // shows this is update
                {
                    if (!initInUpgradePage)
                    {
                        appSettings[HikeInstantiation.APP_UPDATE_POSTPENDING] = true;
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

                // setting it a default counter of 2 to show notification counter for new user on conversation page
                if (IsNewInstall && !appSettings.Contains(HikeInstantiation.PRO_TIP_COUNT))
                    HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.PRO_TIP_COUNT, 1);
            }
            #endregion
            #region POST APP INFO ON UPDATE
            // if app info is already sent to server , this function will automatically handle
            UpdatePostHelper.Instance.PostAppInfo();
            #endregion
            #region Post App Locale
            PostLocaleInfo();
            #endregion
            #region HIKE BOT
            if (IsNewInstall)
                WriteToIsoStorageSettings(HikeConstants.AppSettings.REMOVE_EMMA, true);
            else if (Utils.compareVersion(_currentVersion, "2.4.0.0") < 0)
            {
                if (_viewModel != null)
                {
                    foreach (ConversationListObject convlist in _viewModel.ConvMap.Values)
                    {
                        if (Utils.IsHikeBotMsg(convlist.Msisdn))
                        {
                            convlist.ContactName = Utils.GetHikeBotName(convlist.Msisdn);
                            ConversationTableUtils.saveConvObject(convlist, convlist.Msisdn.Replace(":", "_"));
                        }
                    }
                }
            }
            #endregion
            #region CHAT_FTUE
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.6.0.0") < 0)//if it is upgrade
                RemoveKeyFromAppSettings(HikeConstants.SHOW_CHAT_FTUE);
            #endregion
            #region Enter to send

            if (!IsNewInstall)
            {
                if (Utils.compareVersion(_currentVersion, "2.4.0.0") < 0)
                {
                    appSettings[HikeInstantiation.HIKEJINGLE_PREF] = (bool)true;
                    HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.ENTER_TO_SEND, false);
                }
                else if (Utils.compareVersion(_currentVersion, "2.5.1.0") < 0)
                {
                    SendEnterToSendStatusToServer();
                }
            }

            #endregion
            #region Auto Save Media Key Removal
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.7.5.0") < 0)
            {
                HikeInstantiation.RemoveKeyFromAppSettings(HikeInstantiation.AUTO_SAVE_MEDIA);
            }
            #endregion
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
                WriteToIsoStorageSettings(HikeInstantiation.SHOW_NUDGE_TUTORIAL, true);
            }

            if (_currentVersion == "1.0.0.0")  // user is upgrading from version 1.0.0.0 to latest
            {
                /*
                 * 1. Read from individual files.
                 * 2. Overite old files as they are written in a wrong format
                 */
                convList = ConversationTableUtils.getAllConversations(); // this function will read according to the old logic of Version 1.0.0.0
                ConversationTableUtils.saveConvObjectListIndividual(convList);
                HikeInstantiation.appSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = (convList != null) ? convList.Count : 0;
                // there was no country code in first version, and as first version was released in India , we are setting value to +91 
                HikeInstantiation.appSettings[COUNTRY_CODE_SETTING] = HikeConstants.INDIA_COUNTRY_CODE;
                HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.SHOW_FREE_SMS_SETTING, true);
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
                HikeInstantiation.appSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = convList != null ? convList.Count : 0;

                string country_code = null;
                HikeInstantiation.appSettings.TryGetValue<string>(HikeInstantiation.COUNTRY_CODE_SETTING, out country_code);
                if (string.IsNullOrEmpty(country_code) || country_code == HikeConstants.INDIA_COUNTRY_CODE)
                    HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.SHOW_FREE_SMS_SETTING, true);
                else
                    HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.SHOW_FREE_SMS_SETTING, false);
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

        public static void PostLocaleInfo()
        {
            string savedLocale;
            if (!HikeInstantiation.appSettings.TryGetValue(HikeInstantiation.CURRENT_LOCALE, out savedLocale) ||
                savedLocale != CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                string currentLocale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                HikeInstantiation.WriteToIsoStorageSettings(HikeInstantiation.CURRENT_LOCALE, currentLocale);
                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG);
                JObject data = new JObject();
                data.Add(HikeConstants.LOCALE, currentLocale);
                obj.Add(HikeConstants.DATA, data);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
        }


        public static void createDatabaseAsync()
        {
            if (HikeInstantiation.appSettings.Contains(HikeInstantiation.IS_DB_CREATED)) // shows db are created
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
                        if (!store.DirectoryExists(HikeConstants.FILE_TRANSFER_TEMP_LOCATION))
                        {
                            store.CreateDirectory(HikeConstants.FILE_TRANSFER_TEMP_LOCATION);
                        }
                    }
                    // Create the database if it does not exist.
                    Stopwatch st = Stopwatch.StartNew();
                    using (HikeChatsDb db = new HikeChatsDb(HikeInstantiation.MsgsDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }

                    using (HikeUsersDb db = new HikeUsersDb(HikeInstantiation.UsersDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }

                    using (HikeMqttPersistenceDb db = new HikeMqttPersistenceDb(HikeInstantiation.MqttDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }
                    WriteToIsoStorageSettings(HikeInstantiation.IS_DB_CREATED, true);
                    st.Stop();
                    long msec = st.ElapsedMilliseconds;
                    Debug.WriteLine("APP: Time to create Dbs : {0}", msec);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: createDatabaseAsync : createDatabaseAsync , Exception : " + ex.StackTrace);
                    RemoveKeyFromAppSettings(HikeInstantiation.IS_DB_CREATED);
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
                    HikeInstantiation.appSettings[key] = value;
                }
                HikeInstantiation.appSettings.Save();
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
                    HikeInstantiation.appSettings[key] = value;
                    HikeInstantiation.appSettings.Save();
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
                    HikeInstantiation.appSettings.Clear();
                    HikeInstantiation.appSettings.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: ClearHikeInstantiation.appSettings, Exception : " + ex.StackTrace);
                }
            }
        }

        public static void RemoveKeyFromAppSettings(string key)
        {
            lock (lockObj)
            {
                try
                {
                    // if key exists then only remove and save it
                    if (HikeInstantiation.appSettings.Remove(key))
                        HikeInstantiation.appSettings.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: RemoveKeyFromHikeInstantiation.appSettings, Exception : " + ex.StackTrace);
                }
            }
        }

        

        public static void SendEnterToSendStatusToServer()
        {
            bool enterToSend;
            if (!HikeInstantiation.appSettings.TryGetValue(ENTER_TO_SEND, out enterToSend))
                enterToSend = true;

            Analytics.SendAnalyticsEvent(HikeConstants.ST_CONFIG_EVENT, HikeConstants.ENTER_TO_SEND, enterToSend);
        }
    }
}
