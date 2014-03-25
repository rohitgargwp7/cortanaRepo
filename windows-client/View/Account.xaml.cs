﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.utils;
using System.Windows.Media.Imaging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Microsoft.Phone.Notification;
using windows_client.Languages;
using windows_client.DbUtils;
using windows_client.Controls;
using Facebook;
using windows_client.ViewModel;
using System.Net.NetworkInformation;
using windows_client.FileTransfers;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class Account : PhoneApplicationPage, HikePubSub.Listener
    {
        bool canGoBack = true;
        private ProgressIndicatorControl progress = null; // there should be just one instance of this.

        public Account()
        {
            InitializeComponent();
            if (Utils.isDarkTheme())
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_white.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_white.png", UriKind.Relative));
                this.UnlinkFb.Source = new BitmapImage(new Uri("images/fb_white.png", UriKind.Relative));
                this.UnlinkTwitter.Source = new BitmapImage(new Uri("images/tw_white.png", UriKind.Relative));
            }
            else
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_black.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_black.png", UriKind.Relative));
                this.UnlinkFb.Source = new BitmapImage(new Uri("images/fb_dark.png", UriKind.Relative));
                this.UnlinkTwitter.Source = new BitmapImage(new Uri("images/tw_dark.png", UriKind.Relative));
            }

            if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                gridFB.Visibility = Visibility.Visible;

            if (App.appSettings.Contains(HikeConstants.TW_LOGGED_IN))
                gridTwitter.Visibility = Visibility.Visible;

            RegisterListeners();
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            RemoveListeners();
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
        private void RegisterListeners()
        {

        }

        private void RemoveListeners()
        {
            try
            {

            }
            catch { }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FROM_SOCIAL_PAGE)) // shows page is navigated from social page
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.FROM_SOCIAL_PAGE);
                object oo;
                FreeSMS.SocialState ss = FreeSMS.SocialState.DEFAULT;
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.SOCIAL_STATE, out oo))
                {
                    ss = (FreeSMS.SocialState)oo;
                    PhoneApplicationService.Current.State.Remove(HikeConstants.SOCIAL_STATE);

                    if (ss == FreeSMS.SocialState.FB_LOGOUT)
                        AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteFB), HikeConstants.FACEBOOK, false);
                }
            }
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

            progress.Show(LayoutRoot, AppResources.Privacy_LogoutAccountProgress);
            canGoBack = false;
            AccountUtils.unlinkAccount(new AccountUtils.postResponseFunction(unlinkAccountResponse_Callback));

            if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                LogOutFb();

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
                Caption = AppResources.Privacy_DeleteAccountWarningHeader,
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
                {
                    progress = new ProgressIndicatorControl();
                }
                progress.Show(LayoutRoot, AppResources.Privacy_DeleteAccountProgress);
                canGoBack = false;
                AccountUtils.deleteRequest(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback), AccountUtils.BASE + "/account");
            }
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
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
            if (App.appSettings.Contains(HikeConstants.FB_LOGGED_IN))
                LogOutFb();
            DeleteLocalStorage();
        }

        private void DeleteLocalStorage()
        {
            // this is done so that just after unlink/delete , app can again start add book scan
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING;
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            HikeViewModel.stickerHelper = null;
            App.ClearAppSettings();
            App.appSettings[App.IS_DB_CREATED] = true;
            //so that on signing up again user can see these tutorials 
            App.appSettings[App.SHOW_STATUS_UPDATES_TUTORIAL] = true;
            App.appSettings[App.SHOW_BASIC_TUTORIAL] = true;
            App.appSettings[HikeConstants.SHOW_CHAT_FTUE] = true;
            App.WriteToIsoStorageSettings(HikeConstants.AppSettings.REMOVE_EMMA, true);
            MiscDBUtil.clearDatabase();
            PushHelper.Instance.closePushnotifications();
            SmileyParser.Instance.CleanRecentEmoticons();
            FileTransferManager.Instance.ClearTasks();

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.ClearViewModel();
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

        private void LogOutFb()
        {
            var fb = new FacebookClient();
            var parameters = new Dictionary<string, object>();
            parameters["access_token"] = (string)App.appSettings[HikeConstants.AppSettings.FB_ACCESS_TOKEN];
            parameters["next"] = "https://m.facebook.com/connect/login_success.html";
            var logoutUrl = fb.GetLogoutUrl(parameters);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
              {
                  WebBrowser browser = new WebBrowser();
                  browser.Navigate(logoutUrl);
              });
        }

        public void onEventReceived(string type, object obj)
        {

        }

        private void UnlinkFb_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkFacebook_MsgBxCaptn, MessageBoxButton.OKCancel);
            if (res != MessageBoxResult.OK)
                return;
            shellProgress.IsVisible = true;
            PhoneApplicationService.Current.State[HikeConstants.SOCIAL] = HikeConstants.FACEBOOK;
            NavigationService.Navigate(new Uri("/View/SocialPages.xaml", UriKind.Relative));
        }

        private void UnlinkTwitter_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwConfirm_MsgBx, AppResources.FreeSMS_UnlinkTwitter_MsgBxCaptn, MessageBoxButton.OKCancel);
            if (res != MessageBoxResult.OK)
                return;
            else
            {
                shellProgress.IsVisible = true;
                App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.TWITTER_TOKEN);
                App.RemoveKeyFromAppSettings(HikeConstants.AppSettings.TWITTER_TOKEN_SECRET);
                App.RemoveKeyFromAppSettings(HikeConstants.TW_LOGGED_IN);
                AccountUtils.SocialPost(null, new AccountUtils.postResponseFunction(SocialDeleteTW), HikeConstants.TWITTER, false);
                return;
            }
        }

        public void SocialDeleteTW(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                gridTwitter.Visibility = Visibility.Collapsed;
                shellProgress.IsVisible = false;
                MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkTwSuccess_MsgBxCaptn, MessageBoxButton.OK);
            });
        }

        public void SocialDeleteFB(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                gridFB.Visibility = Visibility.Collapsed;
                shellProgress.IsVisible = false;
                MessageBox.Show(AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, AppResources.FreeSMS_UnlinkFbOrTwSuccess_MsgBx, MessageBoxButton.OK);
            });
        }
    }
}