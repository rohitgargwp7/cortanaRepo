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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            string navigateTo = "/View/ConversationsList.xaml";//default page
            //App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.UPGRADE_SCREEN);
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
                }

                // if current version is less than 2.0.0.0 then upgrade Chats DB to add statusMessages table
                if (Utils.compareVersion(App.CURRENT_VERSION, "2.0.0.0") == -1)
                    StatusMsgsTable.MessagesDbUpdateToLatestVersion();
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.5.0.0") != 1) // if current version is less than equal to 1.5.0.0 then upgrade DB
                    MqttDBUtils.MqttDbUpdateToLatestVersion();
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.7.1.2") != 1)// if current version is less than equal to 1.7.1.2 then show NUX
                {
                    //in case of upgrade if 10 hike users then skip NUX
                    if (UsersTableUtils.GetAllHikeContactsCount() < 10 && UsersTableUtils.GetAllNonHikeContactsCount() > 2)
                    {
                        if (listContactInfo == null)
                        {
                            ContactUtils.getContacts(contactSearchCompletedForNux_Callback);
                            int count = 0;
                            while (listContactInfo == null && count < 120000)//wait for 2 mins
                            {
                                count += 2;
                                Thread.Sleep(2);
                            }
                        }
                        if (listContactInfo != null)
                        {
                            navigateTo = "/View/NUX_InviteFriends.xaml";
                            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.NUX_SCREEN_FRIENDS);
                        }
                        else
                        {
                            navigateTo = "/View/ConversationsList.xaml";
                            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                        }
                    }
                    else
                    {
                        navigateTo = "/View/ConversationsList.xaml";
                        App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                    }
                    Thread.Sleep(2000);//added so that this shows at least for 2 sec
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.APP_LAUNCH_COUNT, 1);
                }
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (a, b) =>
            {
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, App.LATEST_VERSION);

                string targetPage = (string)PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO];
                if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
                {
                    string param = Utils.GetParamFromUri(targetPage);
                    NavigationService.Navigate(new Uri("/View/NewChatThread.xaml?" + param, UriKind.Relative));
                }

                else if (targetPage != null && targetPage.Contains("sharePicker.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
                {

                    if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                        NavigationService.Navigate(nUri);
                        return;
                    }
                    int idx = targetPage.IndexOf("?") + 1;
                    string param = targetPage.Substring(idx);
                    NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml?" + param, UriKind.Relative));
                }
                else
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                }
            };
        }

        public static void contactSearchCompletedForNux_Callback(object sender, ContactsSearchEventArgs e)
        {
            ContactUtils.getContactsListMapInitial(e.Results, out listContactInfo);
        }
    }
}