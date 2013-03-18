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

namespace windows_client.View
{
    public partial class BlockListPage : PhoneApplicationPage, HikePubSub.Listener
    {
        public ObservableCollection<ContactInfo> blockedList = null;
        private bool isInitialised;

        public BlockListPage()
        {
            InitializeComponent();
            InitAppBar();
        }

        private void InitAppBar()
        {
            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;
            this.ApplicationBar = appBar;

            ApplicationBarIconButton addIconButton = new ApplicationBarIconButton();
            addIconButton.IconUri = new Uri("/View/images/appbar.add.rest.png", UriKind.Relative);//change
            addIconButton.Text = "add";//todo:change
            addIconButton.Click += AddUsers_Tap;
            addIconButton.IsEnabled = true;
            appBar.Buttons.Add(addIconButton);
        }

        #region LISTENERS

        private void registerListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.BLOCK_USER, this);
            App.HikePubSubInstance.addListener(HikePubSub.UNBLOCK_USER, this);
        }

        private void removeListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
                App.HikePubSubInstance.removeListener(HikePubSub.UNBLOCK_USER, this);
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

                        if(!blockedList.Contains(c))
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
            if (!isInitialised)
            {
                shellProgress.IsVisible = true;
                registerListeners();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, a) =>
                {
                    List<ContactInfo> allContactsList = UsersTableUtils.getAllContactsByGroup();
                    List<Blocked> listBlocked = UsersTableUtils.getBlockList();
                    blockedList = FilterUnBlockedUsers(listBlocked, allContactsList);
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
                    shellProgress.IsVisible = false;
                };
                isInitialised = true;
            }
        }

        private ObservableCollection<ContactInfo> FilterUnBlockedUsers(List<Blocked> blockedList, List<ContactInfo> allContactsList)
        {
            if (blockedList == null || blockedList.Count == 0)
                return null;

            HashSet<string> hashBlocked = new HashSet<string>();
            foreach (Blocked bl in blockedList)
                hashBlocked.Add(bl.Msisdn);

            ObservableCollection<ContactInfo> blockedContacts = new ObservableCollection<ContactInfo>();
            for (int i = 0; i < (allContactsList != null?allContactsList.Count:0); i++)
            {
                ContactInfo c = allContactsList[i];
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
                foreach (string msisdn in  hashBlocked)
                    blockedContacts.Add(new ContactInfo(msisdn, msisdn, false));
            }
            return blockedContacts;
        }

        private void Unblock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;

            ContactInfo c = btn.DataContext as ContactInfo;
            if (c == null)
                return;
            shellProgress.IsVisible = true;
            App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, c.Msisdn);
            blockedList.Remove(c);
            if (blockedList.Count == 0)
            {
                txtEmptyScreen.Visibility = Visibility.Visible;
                ContentPanel.Visibility = Visibility.Collapsed;
            }
            shellProgress.IsVisible = false;
        }

        private void AddUsers_Tap(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_BLOCKED_LIST] = true;
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }
    }
}