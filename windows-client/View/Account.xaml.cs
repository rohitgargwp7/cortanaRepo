using System;
using System.Windows;
using Microsoft.Phone.Controls;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using windows_client.Languages;
using windows_client.DbUtils;
using windows_client.Controls;
using windows_client.ViewModel;
using System.Net.NetworkInformation;
using windows_client.Model;
using FileTransfer;
using CommonLibrary.Constants;

namespace windows_client.View
{
    public partial class Account : PhoneApplicationPage
    {
        bool canGoBack = true;
        private ProgressIndicatorControl progress = null; // there should be just one instance of this.

        public Account()
        {
            InitializeComponent();

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettingsKeys.FB_LOGGED_IN))
                gridFB.Visibility = Visibility.Visible;

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettingsKeys.TW_LOGGED_IN))
                gridTwitter.Visibility = Visibility.Visible;

        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!canGoBack)
            {
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        private void Unlink_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!canGoBack)
                return;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.No_Network_Txt, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);
                return;
            }

            MessageBoxResult result = MessageBox.Show(AppResources.Privacy_LogoutConfirmMsgBxText, AppResources.Privacy_LogoutAccountHeader, MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            if (progress == null)
                progress = new ProgressIndicatorControl();

            LayoutRoot.IsHitTestVisible = false;
            progress.Show(LayoutRoot, AppResources.Privacy_LogoutAccountProgress);
            canGoBack = false;
            AccountUtils.unlinkAccount(new AccountUtils.postResponseFunction(unlinkAccountResponse_Callback));

            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettingsKeys.FB_LOGGED_IN))
                LogoutFb(true);

            DeleteLocalStorage();
        }

        private void unlinkAccountResponse_Callback(JObject obj)
        {

        }

        private void Delete_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!canGoBack)
                return;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.No_Network_Txt, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);
                return;
            }

            CustomMessageBox msgBox = new CustomMessageBox()
            {
                Message = AppResources.Privacy_DeleteAccounWarningMsgBxText,
                Caption = AppResources.Privacy_DeleteAccountHeader,
                LeftButtonContent = AppResources.Cancel_Txt,
                RightButtonContent = AppResources.Continue_txt
            };

            msgBox.Dismissed += msgBox_Dismissed;

            msgBox.Show();
        }

        void msgBox_Dismissed(object sender, DismissedEventArgs e)
        {
            if (e.Result == CustomMessageBoxResult.RightButton)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Privacy_DeleteAccounConfirmMsgBxText, AppResources.Privacy_DeleteAccountHeader, MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                    return;

                if (progress == null)
                    progress = new ProgressIndicatorControl();

                LayoutRoot.IsHitTestVisible = false;
                progress.Show(LayoutRoot, AppResources.Privacy_DeleteAccountProgress);
                canGoBack = false;

                AccountUtils.deleteRequest(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback), ServerUrls.BASE + "/account");
            }
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.ServerJsonKeys.FAIL == (string)obj[HikeConstants.ServerJsonKeys.STAT])
            {
                Debug.WriteLine("Delete Account", "Could not delete account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Delete_Account_Failed, AppResources.Delete_Account_Heading, MessageBoxButton.OKCancel);
                    progress.Hide(LayoutRoot);
                    progress = null;
                    canGoBack = true;
                });
                return;
            }
            if (HikeInstantiation.AppSettings.Contains(HikeConstants.AppSettingsKeys.FB_LOGGED_IN))
                LogoutFb(true);
            DeleteLocalStorage();
        }

        private void DeleteLocalStorage()
        {
            // this is done so that just after unlink/delete , app can again start add book scan
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING;
            NetworkManager.turnOffNetworkManager = true;
            HikeInstantiation.MqttManagerInstance.disconnectFromBroker(false);
            HikeViewModel.ClearStickerHelperInstance();

            HikeInstantiation.ClearAppSettings();
            HikeInstantiation.AppSettings[HikeConstants.AppSettingsKeys.IS_DB_CREATED] = true;

            //so that on signing up again user can see these tutorials 
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettingsKeys.REMOVE_EMMA, true);
            HikeInstantiation.WriteToIsoStorageSettings(HikeConstants.AppSettingsKeys.HIDDEN_TOOLTIP_STATUS, ToolTipMode.HIDDEN_MODE_GETSTARTED);
            MiscDBUtil.clearDatabase();
            PushHelper.Instance.closePushnotifications();
            SmileyParser.Instance.CleanRecentEmoticons();
            FileTransferManager.Instance.ClearTasks();
            ServerUrls.AppEnvironment = ServerUrls.DebugEnvironment.STAGING;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                StickerPivotHelper.Instance.ClearData();
                HikeInstantiation.ViewModel.ClearViewModel();
                try
                {
                    progress.Hide(LayoutRoot);
                    progress = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Privacy.xaml :: DeleteLocalStorage,hideProgress, Exception : " + ex.StackTrace);
                }
                try
                {
                    NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Privacy.xaml :: DeleteLocalStorage,Navigate, Exception : " + ex.StackTrace);
                }
            });
        }

        private void UnlinkFb_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!canGoBack)
                return;

            MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkFacebook_MsgBxCaptn, MessageBoxButton.OKCancel);

            if (res != MessageBoxResult.OK)
                return;

            shellProgress.IsIndeterminate = true;
            LogoutFb(false);
        }

        private void UnlinkTwitter_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!canGoBack)
                return;

            MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkTwitter_MsgBxCaptn, MessageBoxButton.OKCancel);

            if (res != MessageBoxResult.OK)
                return;
            else
            {
                shellProgress.IsIndeterminate = true;
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.TWITTER_TOKEN);
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.TWITTER_TOKEN_SECRET);
                HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.TW_LOGGED_IN);
                AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteTW), HikeConstants.ServerJsonKeys.TWITTER, false);
                return;
            }
        }

        public void SocialDeleteTW(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                gridTwitter.Visibility = Visibility.Collapsed;
                shellProgress.IsIndeterminate = false;
                MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkTwSuccess_MsgBxCaptn, MessageBoxButton.OK);
            });
        }

        public void SocialDeleteFB(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                gridFB.Visibility = Visibility.Collapsed;
                shellProgress.IsIndeterminate = false;
                MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, MessageBoxButton.OK);
            });
        }

        public void SocialDeleteFBOnAccountUnlinkDelete(JObject obj)
        {
        }

        private void LogoutFb(bool isAccountDeleteUnlink)
        {
            Deployment.Current.Dispatcher.BeginInvoke(new Action(async delegate
              {
                  await (new WebBrowser()).ClearCookiesAsync();
              }));
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.FB_ACCESS_TOKEN);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.FB_USER_ID);
            HikeInstantiation.RemoveKeyFromAppSettings(HikeConstants.AppSettingsKeys.FB_LOGGED_IN);

            if (isAccountDeleteUnlink)
                AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteFBOnAccountUnlinkDelete), HikeConstants.ServerJsonKeys.FACEBOOK, false);
            else
                AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteFB), HikeConstants.ServerJsonKeys.FACEBOOK, false);
        }
    }
}