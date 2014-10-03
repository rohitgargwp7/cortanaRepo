using System;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO.IsolatedStorage;
using windows_client.ViewModel;
using windows_client.Mqtt;
using windows_client.View;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using windows_client.Misc;
using System.Globalization;
using Newtonsoft.Json.Linq;
using CommonLibrary;
using CommonLibrary.Constants;
namespace windows_client.utils
{
    public class HikeInstantiation : HikeInitManager
    {
        public static readonly IsolatedStorageSettings AppSettings = IsolatedStorageSettings.ApplicationSettings;

        #region DATA

        public static bool IsViewModelLoaded = false;
        public static NewChatThread NewChatThreadPageObj = null;
        public static string MSISDN;

        private static object lockObj = new object();

        #endregion

        #region PROPERTIES

        private static PageState ps = PageState.WELCOME_SCREEN;
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

        private static HikeMqttManager mMqttManager;
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

        private static HikePubSub mPubSubInstance;
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

        private static HikeViewModel _viewModel;
        public static HikeViewModel ViewModel
        {
            get
            {
                return _viewModel;
            }
        }

        private static bool _isTombstoneLaunch = false;
        public static bool IsTombstoneLaunch
        {
            get { return _isTombstoneLaunch; }
            set
            {
                if (value != _isTombstoneLaunch)
                    _isTombstoneLaunch = value;
            }
        }

        private static string _currentVersion;
        public static string CurrentVersion
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

        private static string _latestVersion;
        public static string LatestVersion
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

        private static bool _isNewInstall = true;
        public static bool IsNewInstall
        {
            set
            {
                if (value != _isNewInstall)
                    _isNewInstall = value;
            }
            get
            {
                return _isNewInstall;
            }
        }

        public static DbConversationListener DbConversationListenerInstance;

        #endregion

        #region PAGE STATE

        public enum PageState
        {
            /// <summary>
            /// Welcome Screen
            /// </summary>
            WELCOME_SCREEN,

            /// <summary>
            /// Enter number Screen
            /// </summary>
            PHONE_SCREEN,

            /// <summary>
            /// Enter Pin Screen
            /// </summary>
            PIN_SCREEN,

            /// <summary>
            /// Status tutorial screen.
            /// </summary>
            TUTORIAL_SCREEN_STATUS,

            /// <summary>
            /// Sticker tutorial screen.
            /// </summary>
            TUTORIAL_SCREEN_STICKERS,

            /// <summary>
            /// Enter name screen.
            /// </summary>
            SETNAME_SCREEN,

            /// <summary>
            /// ConversationList Screen
            /// </summary>
            CONVLIST_SCREEN,

            /// <summary>
            /// Upgrade page screen
            /// </summary>
            UPGRADE_SCREEN,

            //the below pages have been removed after 2.2, but still we need these to handle them in case of upgrade.
            //app settings is storing pagestate in form of object and not value, hence while converting on upgrade
            //app settings is getting corrupted which throws the ap to welcome page
            WELCOME_HIKE_SCREEN,
            NUX_SCREEN_FRIENDS,
            NUX_SCREEN_FAMILY
        }

        #endregion

        /// <summary>
        /// Instntiate hike classes useful for app functioning
        /// </summary>
        /// <param name="initInUpgradePage">is upgrade page called</param>
        public static void InstantiateClasses(bool initInUpgradePage)
        {
            #region Hidden Mode
            if (IsNewInstall || Utils.compareVersion(_currentVersion, "2.6.5.0") < 0)
                WriteToIsoStorageSettings(AppSettingsKeys.HIDDEN_TOOLTIP_STATUS, ToolTipMode.HIDDEN_MODE_GETSTARTED);
            #endregion
            #region Upgrade Pref Contacts Fix
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.6.2.0") < 0)
                HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.CONTACTS_TO_SHOW);
            #endregion
            #region ProTips 2.3.0.0
            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.3.0.0") < 0)
            {
                try
                {
                    var proTip = new ProTip();
                    HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.PRO_TIP, out proTip);

                    if (proTip != null)
                    {
                        HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.PRO_TIP);
                        HikeInstantiation.AppSettings[AppSettingsKeys.PRO_TIP] = proTip._id;
                    }
                }
                catch { }

                HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.PRO_TIP_DISMISS_TIME);
                ProTipHelper.Instance.ClearOldProTips();
            }
            #endregion
            #region LAST SEEN BYTE TO BOOL FIX

            if (!IsNewInstall && Utils.compareVersion(_currentVersion, "2.2.2.0") < 0)
            {
                try
                {
                    byte value;
                    if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.LAST_SEEN_SEETING, out value))
                    {
                        HikeInstantiation.AppSettings.Remove(AppSettingsKeys.LAST_SEEN_SEETING);
                        HikeInstantiation.AppSettings.Save();

                        if (value <= 0)
                            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.LAST_SEEN_SEETING, false);
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
                HikeInstantiation.AppSettings.Remove(AppSettingsKeys.TIP_MARKED_KEY);
                HikeInstantiation.AppSettings.Remove(AppSettingsKeys.TIP_SHOW_KEY);
                HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.CHAT_THREAD_COUNT_KEY);
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
                    RemoveKeyFromAppSettings(AppSettingsKeys.SHOW_STATUS_UPDATES_TUTORIAL);
                    ps = PageState.CONVLIST_SCREEN;
                    RemoveKeyFromAppSettings(AppSettingsKeys.SHOW_BASIC_TUTORIAL);
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.PAGE_STATE, ps);
                }
            }
            #endregion
            #region GROUP CACHE

            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.GROUPS_CACHE)) // this will happen just once and no need to check version as this will work  for all versions
            {
                GroupManager.Instance.GroupParticpantsCache = (Dictionary<string, List<GroupParticipant>>)HikeInstantiation.AppSettings[AppSettingsKeys.GROUPS_CACHE];
                GroupManager.Instance.SaveGroupParticpantsCache();
                RemoveKeyFromAppSettings(AppSettingsKeys.GROUPS_CACHE);
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
                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.APP_LAUNCH_COUNT, 1);
            }
            #endregion
            #region VIEW MODEL

            IsViewModelLoaded = false;
            if (_viewModel == null)
            {
                _latestVersion = Utils.getAppVersion(); // this will get the new version we have installed
                List<ConversationListObject> convList = null;

                if (!IsNewInstall)// this has to be called for no new install case
                    convList = GetConversations();
                else // new install case
                {
                    convList = null;
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.FILE_SYSTEM_VERSION, _latestVersion);// new install so write version
                }

                if (convList == null || convList.Count == 0)
                    _viewModel = new HikeViewModel();
                else
                    _viewModel = new HikeViewModel(convList);

                if (!IsNewInstall && Utils.compareVersion(_latestVersion, _currentVersion) == 1) // shows this is update
                {
                    if (!initInUpgradePage)
                    {
                        AppSettings[AppSettingsKeys.APP_UPDATE_POSTPENDING] = true;
                        AppSettings[AppSettingsKeys.NEW_UPDATE] = true;
                        WriteToIsoStorageSettings(AppSettingsKeys.FILE_SYSTEM_VERSION, _latestVersion);
                        if (Utils.compareVersion(_currentVersion, "1.5.0.0") != 1) // if current version is less than equal to 1.5.0.0 then upgrade DB
                            MqttDBUtils.MqttDbUpdateToLatestVersion();
                    }
                }

                st.Stop();
                msec = st.ElapsedMilliseconds;
                Debug.WriteLine("APP: Time to Instantiate View Model : {0}", msec);
                IsViewModelLoaded = true;

                // setting it a default counter of 2 to show notification counter for new user on conversation page
                if (IsNewInstall && !AppSettings.Contains(AppSettingsKeys.PRO_TIP_COUNT))
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.PRO_TIP_COUNT, 1);
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
                WriteToIsoStorageSettings(AppSettingsKeys.REMOVE_EMMA, true);
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
                RemoveKeyFromAppSettings(AppSettingsKeys.SHOW_CHAT_FTUE);
            #endregion
            #region Enter to send

            if (!IsNewInstall)
            {
                if (Utils.compareVersion(_currentVersion, "2.4.0.0") < 0)
                {
                    AppSettings[AppSettingsKeys.HIKEJINGLE_PREF] = (bool)true;
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.ENTER_TO_SEND, false);
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
                HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.AUTO_SAVE_MEDIA);
            }
            #endregion
            DbConversationListenerInstance = new DbConversationListener();
        }

        /// <summary>
        /// This function handles any upgrade process in Conversations and AppSettings only
        /// </summary>
        /// <returns></returns>
        private static List<ConversationListObject> GetConversations()
        {
            List<ConversationListObject> convList = null;
            AppSettings.TryGetValue<string>(AppSettingsKeys.FILE_SYSTEM_VERSION, out _currentVersion);
            if (_currentVersion == null)
                _currentVersion = "1.0.0.0";

            // this will ensure that we will show tutorials in case of app upgrade from any version to version later that 1.5.0.8
            if (Utils.compareVersion(_currentVersion, "1.5.0.8") != 1) // current version is less than equal to 1.5.0.8
            {
                WriteToIsoStorageSettings(AppSettingsKeys.SHOW_NUDGE_TUTORIAL, true);
            }

            if (_currentVersion == "1.0.0.0")  // user is upgrading from version 1.0.0.0 to latest
            {
                /*
                 * 1. Read from individual files.
                 * 2. Overite old files as they are written in a wrong format
                 */
                convList = ConversationTableUtils.GetAllConversations_Ver1000(); // this function will read according to the old logic of Version 1.0.0.0
                ConversationTableUtils.saveConvObjectListIndividual(convList);
                HikeInstantiation.AppSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = (convList != null) ? convList.Count : 0;
                // there was no country code in first version, and as first version was released in India , we are setting value to +91 
                HikeInstantiation.AppSettings[AppSettingsKeys.COUNTRY_CODE_SETTING] = HikeConstants.INDIA_COUNTRY_CODE;
                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.SHOW_FREE_SMS_SETTING, true);
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
                HikeInstantiation.AppSettings[HikeViewModel.NUMBER_OF_CONVERSATIONS] = convList != null ? convList.Count : 0;

                string country_code = null;
                HikeInstantiation.AppSettings.TryGetValue<string>(AppSettingsKeys.COUNTRY_CODE_SETTING, out country_code);
                if (string.IsNullOrEmpty(country_code) || country_code == HikeConstants.INDIA_COUNTRY_CODE)
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.SHOW_FREE_SMS_SETTING, true);
                else
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.SHOW_FREE_SMS_SETTING, false);
                return convList;
            }

            else // this corresponds to the latest version and is called everytime except update launch
            {
                int convs = 0;
                AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                convList = ConversationTableUtils.getAllConvs();

                int convListCount = convList == null ? 0 : convList.Count;
                // This shows something failed while reading from Convs , so move to backup plan i.e read from individual files
                if (convListCount != convs)
                    convList = ConversationTableUtils.GetConvsFromIndividualFiles();

                return convList;
            }
        }

        /// <summary>
        /// Post device locale info to server
        /// </summary>
        public static void PostLocaleInfo()
        {
            string savedLocale;
            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.CURRENT_LOCALE, out savedLocale) ||
                savedLocale != CultureInfo.CurrentCulture.TwoLetterISOLanguageName)
            {
                string currentLocale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.CURRENT_LOCALE, currentLocale);
                JObject obj = new JObject();
                obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG);
                JObject data = new JObject();
                data.Add(ServerJsonKeys.LOCALE, currentLocale);
                obj.Add(ServerJsonKeys.DATA, data);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
        }

        /// <summary>
        /// Create database for hike
        /// </summary>
        public static void CreateDatabaseAsync()
        {
            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.IS_DB_CREATED)) // shows db are created
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
                        if (!store.DirectoryExists(FTBasedConstants.SHARED_FILE_LOCATION))
                        {
                            store.CreateDirectory(FTBasedConstants.SHARED_FILE_LOCATION);
                        }
                        if (!store.DirectoryExists(FTBasedConstants.FILE_TRANSFER_TEMP_LOCATION))
                        {
                            store.CreateDirectory(FTBasedConstants.FILE_TRANSFER_TEMP_LOCATION);
                        }
                    }
                    // Create the database if it does not exist.
                    Stopwatch st = Stopwatch.StartNew();
                    using (HikeChatsDb db = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }

                    using (HikeUsersDb db = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }

                    using (HikeMqttPersistenceDb db = new HikeMqttPersistenceDb(HikeConstants.DBStrings.MqttDBConnectionstring))
                    {
                        if (db.DatabaseExists() == false)
                            db.CreateDatabase();
                    }
                    WriteToIsoStorageSettings(AppSettingsKeys.IS_DB_CREATED, true);
                    st.Stop();
                    long msec = st.ElapsedMilliseconds;
                    Debug.WriteLine("APP: Time to create Dbs : {0}", msec);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: createDatabaseAsync : createDatabaseAsync , Exception : " + ex.StackTrace);
                    RemoveKeyFromAppSettings(AppSettingsKeys.IS_DB_CREATED);
                }

            };
            bw.RunWorkerAsync();
        }

        /// <summary>
        /// This function should always be used to store values to isolated storage
        /// Its a thread safe implemenatation to save values
        /// </summary>
        /// <param name="kvlist">List of key value pair.</param>
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
                    HikeInstantiation.AppSettings[key] = value;
                }
                HikeInstantiation.AppSettings.Save();
            }
        }

        /// <summary>
        /// This function should always be used to store values to isolated storage
        /// Its a thread safe implemenatation to save values.
        /// </summary>
        /// <param name="key">Key to be added.</param>
        /// <param name="value">Value for the passed key.</param>
        public static void WriteToIsoStorageSettings(string key, object value)
        {
            lock (lockObj)
            {
                try
                {
                    HikeInstantiation.AppSettings[key] = value;
                    HikeInstantiation.AppSettings.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: WriteToIsoStorageSettings, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Clear app settings. This function should always be used to clear values to isolated storage
        /// Its a thread safe implemenatation to clear values
        /// </summary>
        public static void ClearAppSettings()
        {
            lock (lockObj)
            {
                try
                {
                    HikeInstantiation.AppSettings.Clear();
                    HikeInstantiation.AppSettings.Save();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("App :: ClearHikeInstantiation.appSettings, Exception : " + ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// Remove key from app settings. This function should always be used to remove values to isolated storage
        /// Its a thread safe implemenatation to remove values
        /// </summary>
        /// <param name="key">Key to be removed.</param>
        public static void RemoveKeyFromAppSettings(string key)
        {
            lock (lockObj)
            {
                try
                {
                    // if key exists then only remove and save it
                    if (HikeInstantiation.AppSettings.Remove(key))
                        HikeInstantiation.AppSettings.Save();
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
            if (!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.ENTER_TO_SEND, out enterToSend))
                enterToSend = true;

            Analytics.SendAnalyticsEvent(ServerJsonKeys.ST_CONFIG_EVENT, HikeConstants.AnalyticsKeys.ANALYTICS_ENTER_TO_SEND, enterToSend);
        }
    }
}
