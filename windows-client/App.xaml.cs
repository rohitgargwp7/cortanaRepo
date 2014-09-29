using System;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.DbUtils;
using windows_client.utils;
using windows_client.ViewModel;
using windows_client.View;
using System.Diagnostics;
using windows_client.Misc;
using Microsoft.Phone.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using FileTransfer;
using CommonLibrary.Constants;

namespace windows_client
{
    public partial class App : Application
    {
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
                //Application.Current.Host.Settings.EnableFrameRateCounter = true;

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
            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.TOKEN_SETTING))
            {
                AccountUtils.Token = (string)HikeInstantiation.AppSettings[AppSettingsKeys.TOKEN_SETTING];
                string msisdn = string.Empty;

                if (HikeInstantiation.AppSettings.TryGetValue<string>(AppSettingsKeys.MSISDN_SETTING, out msisdn))
                    HikeInstantiation.MSISDN = msisdn;
            }

            if (HikeInstantiation.AppSettings.Contains(ServerUrls.APP_ENVIRONMENT_SETTING))
            {
                ServerUrls.DebugEnvironment tmpEnv;
                HikeInstantiation.AppSettings.TryGetValue<ServerUrls.DebugEnvironment>(ServerUrls.APP_ENVIRONMENT_SETTING, out tmpEnv);
                ServerUrls.AppEnvironment = tmpEnv;
            }

            RootFrame.Navigating += new NavigatingCancelEventHandler(RootFrame_Navigating);
            RootFrame.Navigated += RootFrame_Navigated;

            (App.Current.Resources["PhoneSubtleBrush"] as SolidColorBrush).Color = (Color)App.Current.Resources["PhoneSubtleColor"];
            (App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color = (Color)App.Current.Resources["PhoneAccentColor"];
        }

        void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Reset)
            {
                RootFrame.Navigating += RootFrame_CheckForFastResume;
            }
            else if (e.NavigationMode == NavigationMode.Refresh && e.Uri.OriginalString.Contains("ConversationsList"))
                RootFrame.Navigating -= RootFrame_CheckForFastResume;
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            // Activate hidden mode when app is launched if setting is true.
            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.ACTIVATE_HIDDEN_MODE_ON_EXIT))
                HikeInstantiation.AppSettings.Remove(AppSettingsKeys.HIDDEN_MODE_ACTIVATED);
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched 
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            HikeInstantiation.IsTombstoneLaunch = !e.IsApplicationInstancePreserved; //e.IsApplicationInstancePreserved  --> if this is true its dormant else tombstoned

            if (HikeInstantiation.IsTombstoneLaunch)
            {
                HikeInstantiation.PageState pageStateVal;
                if (HikeInstantiation.AppSettings.TryGetValue<HikeInstantiation.PageState>(AppSettingsKeys.PAGE_STATE, out pageStateVal))
                {
                    HikeInstantiation.PageStateVal = pageStateVal;
                    HikeInstantiation.IsNewInstall = false;
                }

                string currentVersion = string.Empty;
                if (HikeInstantiation.AppSettings.TryGetValue<string>(AppSettingsKeys.FILE_SYSTEM_VERSION, out currentVersion))
                    HikeInstantiation.CurrentVersion = currentVersion;

                HikeInstantiation.InstantiateClasses(false);
            }
            else
            {
                if (HikeInstantiation.PageStateVal == HikeInstantiation.PageState.CONVLIST_SCREEN)
                    HikeInstantiation.MqttManagerInstance.connect();

                HikeInstantiation.ViewModel.RequestLastSeen();
            }

            NetworkManager.turnOffNetworkManager = false;
            HikeInstantiation.MqttManagerInstance.IsAppStarted = false;
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            NetworkManager.turnOffNetworkManager = true;
            SendAppBgStatusToServer();

            if (HikeInstantiation.IsViewModelLoaded)
            {
                int convs = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);

                if (convs != 0 && HikeInstantiation.ViewModel.ConvMap.Count == 0)
                    return;

                ConversationTableUtils.saveConvObjectList();
            }

            HikeInstantiation.MqttManagerInstance.disconnectFromBroker(false);
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            SendAppBgStatusToServer();
        }

        /// <summary>
        /// Create Push Channel and listen to network change.
        /// </summary>
        public static void AppInitialize()
        {
            DeviceNetworkInformation.NetworkAvailabilityChanged += OnNetworkChange;

            #region PUSH NOTIFICATIONS STUFF

            bool isPushEnabled = true;
            HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.IS_PUSH_ENABLED, out isPushEnabled);

            if (isPushEnabled)
                PushHelper.Instance.registerPushnotifications(false);
        
            #endregion
        }

        private static void OnNetworkChange(object sender, NetworkNotificationEventArgs e)
        {
            //reconnect mqtt whenever phone is reconnected without relaunch 
            if (e.NotificationType == NetworkNotificationType.InterfaceConnected ||
                e.NotificationType == NetworkNotificationType.InterfaceDisconnected)
            {
                if (Microsoft.Phone.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                {
                    HikeInstantiation.MqttManagerInstance.connect();
                    bool isPushEnabled = true;
                    HikeInstantiation.AppSettings.TryGetValue<bool>(AppSettingsKeys.IS_PUSH_ENABLED, out isPushEnabled);

                    if (isPushEnabled)
                    {
                        PushHelper.Instance.registerPushnotifications(false);
                    }

                    FileTransferManager.Instance.ChangeMaxUploadBuffer(e.NetworkInterface.InterfaceSubtype);
                    FileTransferManager.Instance.StartTask();

                    //upload pending group images when network reconnects
                    if (HikeInstantiation.ViewModel.PendingRequests.Count > 0)
                        HikeInstantiation.ViewModel.SendDisplayPic();

                    HikeInstantiation.ViewModel.RequestLastSeen();
                }
                else
                {
                    if (HikeInstantiation.MqttManagerInstance != null)
                        HikeInstantiation.MqttManagerInstance.setConnectionStatus(Mqtt.HikeMqttManager.MQTTConnectionStatus.NOTCONNECTED_WAITINGFORINTERNET);
                }
            }
        }

        void RootFrame_CheckForFastResume(object sender, NavigatingCancelEventArgs e)
        {
            RootFrame.Navigating -= RootFrame_CheckForFastResume;
            UriMapper mapper = Resources["mapper"] as UriMapper;
            RootFrame.UriMapper = mapper;
            var targetPage = e.Uri.ToString();

            HikeInstantiation.PageState pageStateVal;

            if (HikeInstantiation.AppSettings.TryGetValue<HikeInstantiation.PageState>(AppSettingsKeys.PAGE_STATE, out pageStateVal))
                HikeInstantiation.PageStateVal = pageStateVal;


            if (e.NavigationMode == NavigationMode.New)
            {
                if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
                {
                    if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                        mapper.UriMappings[0].MappedUri = nUri;
                        return;
                    }

                    string msisdn = Utils.GetParamFromUri(targetPage);
                    bool isStealth = Utils.IsUriStealth(targetPage);

                    if ((!isStealth || (isStealth && HikeInstantiation.ViewModel.IsHiddenModeActive))
                        && !HikeInstantiation.AppSettings.Contains(AppSettingsKeys.NEW_UPDATE_AVAILABLE)
                        && (!Utils.isGroupConversation(msisdn) || GroupManager.Instance.GetParticipantList(msisdn) != null))
                    {
                        PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.LAUNCH_FROM_PUSH_MSISDN] = msisdn;
                        mapper.UriMappings[0].MappedUri = new Uri("/View/NewChatThread.xaml", UriKind.Relative);
                    }
                    else
                    {
                        mapper.UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                    }
                }
                else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("isStatus"))// STATUS PUSH NOTIFICATION CASE
                {
                    if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                        mapper.UriMappings[0].MappedUri = nUri;
                        return;
                    }

                    PhoneApplicationService.Current.State["IsStatusPush"] = true;
                }
                else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("FileId")) // SHARE PICKER CASE
                {
                    if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                        mapper.UriMappings[0].MappedUri = nUri;
                        return;
                    }

                    int idx = targetPage.IndexOf("?") + 1;
                    string param = targetPage.Substring(idx);
                    mapper.UriMappings[0].MappedUri = new Uri("/View/ForwardTo.xaml?" + param, UriKind.Relative);
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            RootFrame.Navigating -= RootFrame_Navigating;

            UriMapper mapper = Resources["mapper"] as UriMapper;
            RootFrame.UriMapper = mapper;

            HikeInstantiation.PageState pageStateVal;
            if (HikeInstantiation.AppSettings.TryGetValue<HikeInstantiation.PageState>(AppSettingsKeys.PAGE_STATE, out pageStateVal))
            {
                HikeInstantiation.PageStateVal = pageStateVal;
                HikeInstantiation.IsNewInstall = false;
            }
            /*
            * These changes are done from version 2.0.0.0 , in WP8 devices after status upgrade
            */

            // this will get the current version installed already in "_currentVersion"
            string currentVersion;
            if (HikeInstantiation.AppSettings.TryGetValue<string>(AppSettingsKeys.FILE_SYSTEM_VERSION, out currentVersion))
                HikeInstantiation.CurrentVersion = currentVersion;

            HikeInstantiation.LatestVersion = Utils.getAppVersion(); // this will get the new version we are upgrading to

            string targetPage = e.Uri.ToString();

            if (!String.IsNullOrEmpty(currentVersion) && Utils.compareVersion("2.6.5.0", currentVersion) == 1)
            {
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.PAGE_TO_NAVIGATE_TO] = targetPage;
                HikeInstantiation.InstantiateClasses(true);
                mapper.UriMappings[0].MappedUri = new Uri("/View/UpgradePage.xaml", UriKind.Relative);
            }
            else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
            {
                HikeInstantiation.InstantiateClasses(false);
                AppInitialize();
                if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                    mapper.UriMappings[0].MappedUri = nUri;
                    return;
                }

                // Extract msisdn from server url
                string msisdn = Utils.GetParamFromUri(targetPage);
                bool IsStealth = Utils.IsUriStealth(targetPage);

                if ((!IsStealth || (IsStealth && HikeInstantiation.ViewModel.IsHiddenModeActive))
                    && !HikeInstantiation.AppSettings.Contains(AppSettingsKeys.NEW_UPDATE_AVAILABLE)
                    && (!Utils.isGroupConversation(msisdn) || GroupManager.Instance.GetParticipantList(msisdn) != null))
                {
                    PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.LAUNCH_FROM_PUSH_MSISDN] = msisdn;
                    mapper.UriMappings[0].MappedUri = new Uri("/View/NewChatThread.xaml", UriKind.Relative);
                }
                else
                {
                    mapper.UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                }
            }
            else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("isStatus"))// STATUS PUSH NOTIFICATION CASE
            {
                PhoneApplicationService.Current.State["IsStatusPush"] = true;

                HikeInstantiation.InstantiateClasses(false);
                AppInitialize();
                if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                    mapper.UriMappings[0].MappedUri = nUri;
                    return;
                }
                mapper.UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
            }
            else if (targetPage != null && targetPage.Contains("ConversationsList.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
            {
                HikeInstantiation.InstantiateClasses(false);
                AppInitialize();
                if (HikeInstantiation.PageStateVal != HikeInstantiation.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                    mapper.UriMappings[0].MappedUri = nUri;
                    return;
                }

                int idx = targetPage.IndexOf("?") + 1;
                string param = targetPage.Substring(idx);
                mapper.UriMappings[0].MappedUri = new Uri("/View/ForwardTo.xaml?" + param, UriKind.Relative);
            }
            else
            {
                HikeInstantiation.InstantiateClasses(false);
                AppInitialize();

                Uri nUri = Utils.LoadPageUri(HikeInstantiation.PageStateVal);
                mapper.UriMappings[0].MappedUri = nUri;
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

            if (HikeInstantiation.IsViewModelLoaded)
            {
                int convs = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && HikeInstantiation.ViewModel.ConvMap.Count == 0)
                    return;
                ConversationTableUtils.saveConvObjectList();
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
            if (!HikeInstantiation.IsMarketplace)
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
            if (HikeInstantiation.IsViewModelLoaded)
            {
                int convs = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_CONVERSATIONS, out convs);
                if (convs != 0 && HikeInstantiation.ViewModel.ConvMap.Count == 0)
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

        /// <summary>
        /// Send App Status to server. If app has been sent to bg or has been activated to fg.
        /// </summary>
        public void SendAppBgStatusToServer()
        {
            JObject obj = new JObject();
            obj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.MqttMessageTypes.APP_INFO);
            obj.Add(ServerJsonKeys.TIMESTAMP, TimeUtils.getCurrentTimeStamp());
            obj.Add(ServerJsonKeys.STATUS, "bg");

            if (HikeInstantiation.HikePubSubInstance != null)
            {
                Object[] objArr = new object[2];
                objArr[0] = obj;
                objArr[1] = 0;
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, objArr);
            }
        }

        public static MediaElement GlobalMediaElement
        {
            get { return Current.Resources["GlobalMedia"] as MediaElement; }
        }
    }
}