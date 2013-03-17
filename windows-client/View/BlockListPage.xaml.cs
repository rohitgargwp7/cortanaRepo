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
        public List<ContactInfo> blockedContacts = null;
        public List<ContactInfo> unblockedContacts = null;
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
        }

        private void removeListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
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
                    unblockedContacts.Remove(c);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        blockedList.Add(c);
                        if (txtEmptyScreen.Visibility == Visibility.Visible)
                        {
                            txtEmptyScreen.Visibility = Visibility.Collapsed;
                            ContentPanel.Visibility = Visibility.Visible;
                        }
                    });
                }
            }
        }
        #endregion

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.BLOCKLIST_PAGE);
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
                    FilterUnBlockedUsers(listBlocked, allContactsList);
                    blockedList = new ObservableCollection<ContactInfo>(blockedContacts);
                };
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += (s, a) =>
                {
                    blockedListBox.ItemsSource = blockedList;
                    if (blockedList.Count == 0)
                    {
                        txtEmptyScreen.Visibility = Visibility.Visible;
                        ContentPanel.Visibility = Visibility.Collapsed;
                    }
                    shellProgress.IsVisible = false;
                };
                isInitialised = true;
            }
        }

        private void FilterUnBlockedUsers(List<Blocked> blockedList, List<ContactInfo> allContactsList)
        {
            blockedContacts = new List<ContactInfo>();
            unblockedContacts = new List<ContactInfo>();
            if (allContactsList == null || allContactsList.Count == 0)
                return;
            Dictionary<string, bool> dictMsisdns = new Dictionary<string, bool>();
            Dictionary<string, bool> dictBlocked = new Dictionary<string, bool>();

            if (blockedList != null)
            {
                foreach (Blocked bl in blockedList)
                {
                    dictBlocked.Add(bl.Msisdn, true);
                }
            }
            for (int i = 0; i < allContactsList.Count; i++)
            {
                ContactInfo c = allContactsList[i];
                if (dictMsisdns.ContainsKey(c.Msisdn))
                    continue;
                dictMsisdns.Add(c.Msisdn, true);
                if (dictBlocked.ContainsKey(c.Msisdn))
                    blockedContacts.Add(c);
                else
                    unblockedContacts.Add(c);
            }
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
            unblockedContacts.Add(c);
            if (blockedList.Count == 0)
            {
                txtEmptyScreen.Visibility = Visibility.Visible;
                ContentPanel.Visibility = Visibility.Collapsed;
            }
            shellProgress.IsVisible = false;
        }

        private void AddUsers_Tap(object sender, EventArgs e)
        {
            //todo:handle gk
            PhoneApplicationService.Current.State[HikeConstants.BLOCKLIST_PAGE] = this;
            NavigationService.Navigate(new Uri("/View/UnblockedUsersPage.xaml", UriKind.Relative));
        }
    }
}