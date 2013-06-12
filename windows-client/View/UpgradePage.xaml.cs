using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.View
{
    public partial class UpgradePage : PhoneApplicationPage
    {
        private static List<ContactInfo> listContactInfo;
        public UpgradePage()
        {
            InitializeComponent();
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.PAGE_TO_NAVIGATE_TO);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (a, b) =>
            {
                if (Utils.compareVersion(App.LATEST_VERSION, App.CURRENT_VERSION) == 1) // shows this is update
                {
                    App.appSettings[App.APP_UPDATE_POSTPENDING] = true;
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.NEW_UPDATE, true);
                    #region POST APP INFO ON UPDATE
                    // if app info is already sent to server , this function will automatically handle
                    UpdatePostHelper.Instance.postAppInfo();
                    #endregion
                    #region Post App Locale
                    App.PostLocaleInfo();
                    #endregion
                }

                // if current version is less than equal to 1.8.0.0 then upgrade Chats DB to add statusMessages table
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.8.0.0") != 1)
                    StatusMsgsTable.MessagesDbUpdateToLatestVersion();
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.5.0.0") != 1) // if current version is less than equal to 1.5.0.0 then upgrade DB
                    MqttDBUtils.MqttDbUpdateToLatestVersion();
                if (Utils.compareVersion("2.1.0.8", App.CURRENT_VERSION) == 1) // upgrade friend files for last seen time stamp
                {
                    FriendsTableUtils.UpdateOldFilesWithDefaultLastSeen();
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.TUTORIAL_SCREEN_STICKERS);
                    if (Utils.compareVersion("2.1.0.0", App.CURRENT_VERSION) == 1)
                    {
                        App.WriteToIsoStorageSettings(App.SHOW_STATUS_UPDATES_TUTORIAL, true);
                        App.appSettings[HikeConstants.AppSettings.APP_LAUNCH_COUNT] = 1;
                    }
                }
                else
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);

                Thread.Sleep(2000);//added so that this shows at least for 2 sec
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (a, b) =>
            {
                App.appInitialize();
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, App.LATEST_VERSION);

                string targetPage = (string)PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO];
                if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
                {
                    string param = Utils.GetParamFromUri(targetPage);
                    NavigationService.Navigate(new Uri("/View/NewChatThread.xaml?" + param, UriKind.Relative));
                }
                else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("isStatus"))// STATUS PUSH NOTIFICATION CASE
                {
                    PhoneApplicationService.Current.State["IsStatusPush"] = true;
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                }
                else if (targetPage != null && targetPage.Contains("NewSelectUserPage.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
                {

                    if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                        NavigationService.Navigate(nUri);
                        return;
                    }
                    int idx = targetPage.IndexOf("?") + 1;
                    string param = targetPage.Substring(idx);
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml?" + true, UriKind.Relative));
                }
                else
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                }
            };
        }
    }
}