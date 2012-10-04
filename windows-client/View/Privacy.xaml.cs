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

namespace windows_client.View
{
    public partial class Privacy : PhoneApplicationPage, HikePubSub.Listener
    {
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

        private void RegisterListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.ACCOUNT_DELETED, this);
        }

        private void Unlink_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to unlink your Hike account from this device?", "Unlink Account", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
                progress = new MyProgressIndicator("Unlinking Account...");

            progress.Show();
            AccountUtils.unlinkAccount(new AccountUtils.postResponseFunction(unlinkAccountResponse_Callback));
        }

        private void unlinkAccountResponse_Callback(JObject obj)
        {
            if (obj == null || "fail" == (string)obj["stat"])
            {
                Debug.WriteLine("Unlink Account", "Could not unlink account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show("hike couldn't unlink your account. Please try again.", "Account not unlinked", MessageBoxButton.OKCancel);
                    progress.Hide();
                    progress = null;
                });
                return;
            }
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            App.ClearAppSettings();
            App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            App.HikePubSubInstance.publish(HikePubSub.DELETE_ACCOUNT, null);
        }

        private void Delete_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure you want to delete your Hike account permanently?", "Delete Account", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;
            if (progress == null)
            {
                progress = new MyProgressIndicator("Deleting Account...");
            }
            progress.Show();
            AccountUtils.deleteAccount(new AccountUtils.postResponseFunction(deleteAccountResponse_Callback));
        }

        private void deleteAccountResponse_Callback(JObject obj)
        {
            if (obj == null || "fail" == (string)obj["stat"])
            {
                Debug.WriteLine("Delete Account", "Could not delete account !!");
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show("hike couldn't delete your account. Please try again.", "Account not deleted", MessageBoxButton.OKCancel);
                    progress.Hide();
                    progress = null;
                });
                return;
            }
            NetworkManager.turnOffNetworkManager = true;
            App.MqttManagerInstance.disconnectFromBroker(false);
            App.ClearAppSettings();
            App.WriteToIsoStorageSettings(App.IS_DB_CREATED, true);
            App.HikePubSubInstance.publish(HikePubSub.DELETE_ACCOUNT, null);
        }

        public void onEventReceived(string type, object obj)
        {
            #region ACCOUNT_DELETED
            if (HikePubSub.ACCOUNT_DELETED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.MessageListPageCollection.Clear();
                    ConversationsList.ConvMap.Clear();
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