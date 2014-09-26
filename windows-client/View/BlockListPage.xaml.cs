using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.Languages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using windows_client.utils;

namespace windows_client.View
{
    public partial class BlockListPage : PhoneApplicationPage, HikePubSub.Listener
    {
        public ObservableCollection<ContactInfo> blockedList = null;

        public BlockListPage()
        {
            InitializeComponent();
            InitAppBar();
        }

        private void InitAppBar()
        {
            ApplicationBar appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["AppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["AppBarBackground"]).Color,
            };

            this.ApplicationBar = appBar;

            ApplicationBarIconButton addIconButton = new ApplicationBarIconButton();
            addIconButton.IconUri = new Uri("/View/images/AppBar/appbar.add.rest.png", UriKind.Relative);//change
            addIconButton.Text = AppResources.SelectUser_AddUser_Txt;
            addIconButton.Click += AddUsers_Tap;
            addIconButton.IsEnabled = true;
            appBar.Buttons.Add(addIconButton);
        }

        #region LISTENERS

        private void registerListeners()
        {
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.BLOCK_USER, this);
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.UNBLOCK_USER, this);
        }

        private void removeListeners()
        {
            try
            {
                HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
                HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.UNBLOCK_USER, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationList ::  removeListeners , Exception : " + ex.StackTrace);
            }
        }

        public void onEventReceived(string type, object obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("BlockListPage :: OnEventReceived : Object received is null");
                return;
            }
            if (type == HikePubSub.BLOCK_USER)
            {
                if (obj is ContactInfo)
                {
                    ContactInfo c = obj as ContactInfo;
                    
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (blockedList == null)
                        {
                            blockedList = new ObservableCollection<ContactInfo>();
                            blockedListBox.ItemsSource = blockedList;
                        }

                        if (!blockedList.Contains(c))
                            blockedList.Add(c);

                        if (blockedList.Count > 0)
                        {
                            txtEmptyScreen.Visibility = Visibility.Collapsed;
                            ContentPanel.Visibility = Visibility.Visible;
                        }
                    });
                }
            }
            else if (type == HikePubSub.UNBLOCK_USER)
            {
                if (obj is ContactInfo)
                {
                    ContactInfo c = obj as ContactInfo;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        blockedList.Remove(c);
                        if (blockedList.Count == 0)
                        {
                            txtEmptyScreen.Visibility = Visibility.Visible;
                            ContentPanel.Visibility = Visibility.Collapsed;
                        }
                    });
                }
            }
        }
        #endregion

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New || HikeInstantiation.IsTombstoneLaunch)
            {
                shellProgress.IsIndeterminate = true;
                registerListeners();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, a) =>
                {
                    List<ContactInfo> allContactsList = UsersTableUtils.getAllContactsByGroup();
                    blockedList = FilterUnBlockedUsers(allContactsList);
                };
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += (s, a) =>
                {
                    blockedListBox.ItemsSource = blockedList;
                    if (blockedList == null || blockedList.Count == 0)
                    {
                        txtEmptyScreen.Visibility = Visibility.Visible;
                        ContentPanel.Visibility = Visibility.Collapsed;
                    }
                    shellProgress.IsIndeterminate = false;
                };
            }
        }

        private ObservableCollection<ContactInfo> FilterUnBlockedUsers(List<ContactInfo> allContactsList)
        {
            HashSet<string> blockedhashSet = HikeInstantiation.ViewModel.BlockedHashset;

            if (blockedhashSet == null || blockedhashSet.Count == 0)
                return null;

            // this is used to avoid removing msisdn from original blocked hashset
            HashSet<string> hashBlocked = new HashSet<string>(blockedhashSet);
            ObservableCollection<ContactInfo> blockedContacts = new ObservableCollection<ContactInfo>();
            
            for (int i = 0; i < (allContactsList != null?allContactsList.Count:0); i++)
            {
                ContactInfo c = allContactsList[i];

                if (!HikeInstantiation.ViewModel.IsHiddenModeActive &&
                    HikeInstantiation.ViewModel.ConvMap.ContainsKey(c.Msisdn) && HikeInstantiation.ViewModel.ConvMap[c.Msisdn].IsHidden)
                    continue;
                
                if (hashBlocked.Contains(c.Msisdn))
                {
                    blockedContacts.Add(c);
                    hashBlocked.Remove(c.Msisdn);
                }
                
                if (hashBlocked.Count == 0)
                    break;
            }
            
            if (hashBlocked.Count > 0)
            {
                foreach (string msisdn in hashBlocked)
                {
                    if (!HikeInstantiation.ViewModel.IsHiddenModeActive &&
                    HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn) && HikeInstantiation.ViewModel.ConvMap[msisdn].IsHidden)
                        continue;

                    blockedContacts.Add(new ContactInfo(msisdn, msisdn, false));
                }
            }
            
            return blockedContacts;
        }

        private void AddUsers_Tap(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_BLOCKED_LIST] = true;
            NavigationService.Navigate(new Uri("/View/SelectUser.xaml", UriKind.Relative));
        }

        private void ContactItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Grid btn = sender as Grid;

            ContactInfo c = btn.DataContext as ContactInfo;

            if (c == null)
                return;

            shellProgress.IsIndeterminate = true;
            
            HikeInstantiation.ViewModel.BlockedHashset.Remove(c.Msisdn);
            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, c);
            
            c.IsSelected = false;
            blockedList.Remove(c);
            
            if (blockedList.Count == 0)
            {
                txtEmptyScreen.Visibility = Visibility.Visible;
                ContentPanel.Visibility = Visibility.Collapsed;
            }

            shellProgress.IsIndeterminate = false;
        }
    }
}