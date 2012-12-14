using System;
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
using Phone.Controls;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Microsoft.Phone.Notification;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage, HikePubSub.Listener
    {
        bool canGoBack = true;
        public MyProgressIndicator progress = null; // there should be just one instance of this.

        public Privacy()
        {
            InitializeComponent();
            if (Utils.isDarkTheme())
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_white.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_white.png", UriKind.Relative));
            }
            else
            {
                this.unlinkAccount.Source = new BitmapImage(new Uri("images/unlink_account_black.png", UriKind.Relative));
                this.deleteAccount.Source = new BitmapImage(new Uri("images/delete_account_black.png", UriKind.Relative));
            }
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
            App.HikePubSubInstance.addListener(HikePubSub.ACCOUNT_DELETED, this);
        }

        private void RemoveListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.ACCOUNT_DELETED, this);
            }
            catch { }
        }

        private void Unlink_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Privacy_UnlinkConfirmMsgBxText, AppResources.Privacy_UnlinkAccountHeader, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
                progress = new MyProgressIndicator(AppResources.Privacy_UnlinkAccountProgress);

            progress.Show();
            canGoBack = false;
            AccountUtils.unlinkAccount(new AccountUtils.postResponseFunction(unlinkAccountResponse_Callback));
        }

        private void unlinkAccountResponse_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                Debug.WriteLine("Unlink Account", "Could not unlink account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Privacy_UnlinkErrMsgBxText, AppResources.Privacy_UnlinkErrMsgBxCaptn, MessageBoxButton.OKCancel);
                    progress.Hide();
                    progress = null;
                    canGoBack = true;
                });
                return;
            }
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            App.ClearAppSettings();
            App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            App.HikePubSubInstance.publish(HikePubSub.DELETE_ACCOUNT, null);

            HttpNotificationChannel pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
            if (pushChannel != null)
            {
                if (pushChannel.IsShellTileBound)
                    pushChannel.UnbindToShellTile();
                if (pushChannel.IsShellToastBound)
                    pushChannel.UnbindToShellToast();
                pushChannel.Close();
            }

        }

        private void Delete_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.Privacy_DeleteAccounConfirmMsgBxText, AppResources.Privacy_DeleteAccountHeader, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
            {
                progress = new MyProgressIndicator(AppResources.Privacy_DeleteAccountProgress);
            }
            progress.Show();
            canGoBack = false;
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                Debug.WriteLine("Delete Account", "Could not delete account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show("hike couldn't delete your account. Please try again.", "Account not deleted", MessageBoxButton.OKCancel);
                    progress.Hide();
                    progress = null;
                    canGoBack = true;
                });
                return;
            }
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            App.ClearAppSettings();
            App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            App.HikePubSubInstance.publish(HikePubSub.DELETE_ACCOUNT, null);


            //delete push channel
            HttpNotificationChannel pushChannel = HttpNotificationChannel.Find(HikeConstants.pushNotificationChannelName);
            if (pushChannel != null)
            {
                if (pushChannel.IsShellTileBound)
                    pushChannel.UnbindToShellTile();
                if (pushChannel.IsShellToastBound)
                    pushChannel.UnbindToShellToast();
                pushChannel.Close();
            }
        }

        public void onEventReceived(string type, object obj)
        {
            #region ACCOUNT_DELETED
            if (HikePubSub.ACCOUNT_DELETED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.MessageListPageCollection.Clear();
                    App.ViewModel.ConvMap.Clear();
                    try
                    {
                        progress.Hide();
                        progress = null;
                    }
                    catch
                    {
                    }
                    try
                    {
                        NavigationService.Navigate(new Uri("/View/WelcomePage.xaml", UriKind.Relative));
                    }
                    catch { }
                });
                return;
            }
            #endregion
        }
    }
}