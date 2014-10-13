using System;
using CommonLibrary.DbUtils;
using CommonLibrary.Model;
using System.IO.IsolatedStorage;
using CommonLibrary.ViewModel;
using CommonLibrary.Mqtt;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using CommonLibrary.Constants;
using CommonLibrary.Utils;

namespace CommonLibrary.Misc
{
    public class HikeInstantiation : HikeInitManager
    {
        public static readonly IsolatedStorageSettings AppSettings = IsolatedStorageSettings.ApplicationSettings;

        #region DATA

        public static bool IsViewModelLoaded = false;
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

        private static HikeViewModel _viewModel;
        public static HikeViewModel ViewModel
        {
            get
            {
                return _viewModel;
            }
        }

        static string _latestVersion;
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

        #endregion

        /// <summary>
        /// Instntiate hike classes useful for app functioning
        /// </summary>
        public static bool InstantiateClasses()
        {
            _latestVersion = Utility.GetAppVersion();

            AppSettings.TryGetValue<string>(AppSettingsKeys.FILE_SYSTEM_VERSION, out _currentVersion);

            if (_currentVersion == null || Utility.CompareVersion(_currentVersion, LatestVersion) < 0)
                return false;

            AppSettings.TryGetValue(AppSettingsKeys.MSISDN_SETTING, out MSISDN);

            if (String.IsNullOrEmpty(MSISDN))
                return false;

            if (HikeInstantiation.MqttManagerInstance == null)
                HikeInstantiation.MqttManagerInstance = new HikeMqttManager();


            IsViewModelLoaded = false;

            if (_viewModel == null)
            {
                List<ConversationListObject> convList = null;

                convList = GetConversations();

                if (convList == null || convList.Count == 0)
                    _viewModel = new HikeViewModel();
                else
                    _viewModel = new HikeViewModel(convList);

                IsViewModelLoaded = true;
            }

            NetworkManager.turnOffNetworkManager = false;
            HikeInstantiation.MqttManagerInstance.connect();

            return true;
        }

        /// <summary>
        /// Read conversation from Storage file
        /// </summary>
        /// <returns></returns>
        private static List<ConversationListObject> GetConversations()
        {
            int convs = 0;
            AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);

            List<ConversationListObject>  convList = ConversationTableUtils.getAllConvs();
            int convListCount = convList == null ? 0 : convList.Count;
            
            // This shows something failed while reading from Convs , so move to backup plan i.e read from individual files
            if (convListCount != convs)
                convList = ConversationTableUtils.GetConvsFromIndividualFiles();

            return convList;
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
    }
}
