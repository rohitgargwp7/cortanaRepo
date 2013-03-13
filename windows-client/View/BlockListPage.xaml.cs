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

namespace windows_client.View
{
    public partial class BlockListPage : PhoneApplicationPage
    {
        List<ContactInfo> allContactsList = null;

        public ObservableCollection<ContactInfo> blockedList = null; 

        public BlockListPage()
        {
            InitializeComponent();
            InitAppBar();
            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                allContactsList = UsersTableUtils.getAllContactsByGroup();
                List<Blocked> listBlocked = UsersTableUtils.getBlockList();
                FilterUnBlockedUsers(listBlocked);
                blockedList = new ObservableCollection<ContactInfo>(allContactsList);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, e) =>
            {
                blockedListBox.ItemsSource = blockedList;
                shellProgress.IsVisible = false;
            };
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue("blocked", out obj))
            {
                List<ContactInfo> list = obj as List<ContactInfo>;

                if (blockedList != null)
                {
                    foreach (ContactInfo c in list)
                    {
                        blockedList.Add(c);
                    }
                }
                PhoneApplicationService.Current.State.Remove("blocked");
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove("blocked");
        }

        private void FilterUnBlockedUsers(List<Blocked> blockedList)
        {
            if (blockedList == null || blockedList.Count == 0 || allContactsList == null || allContactsList.Count == 0)
                return;
            Dictionary<string, bool> dictBlocked = new Dictionary<string, bool>();
            foreach (Blocked bl in blockedList)
            {
                dictBlocked.Add(bl.Msisdn, true);
            }
            for (int i = allContactsList.Count - 1; i >= 0; i--)
            {
                if (!dictBlocked.ContainsKey(allContactsList[i].Msisdn))
                    allContactsList.RemoveAt(i);
            }
        }

        private void Unblock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;

            ContactInfo c = btn.DataContext as ContactInfo;
            if (c == null)
                return;
            shellProgress.IsVisible = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, a) =>
            {
                UsersTableUtils.unblock(c.Msisdn);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (s, a) =>
            {
                blockedList.Remove(c);
                shellProgress.IsVisible = false;
            };
        }

        private void AddUsers_Tap(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/UnblockedUsersPage.xaml", UriKind.Relative));
        }
    }
}