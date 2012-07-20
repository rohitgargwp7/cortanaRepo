using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows;


namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        public static MyProgressIndicator progress = null;
        public static bool canGoBack = true;
        public List<ContactInfo> allContactsList = null;

       // private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 239, 239, 239));

        public SelectUserToMsg()
        {
            InitializeComponent();
            progressBar.Visibility = System.Windows.Visibility.Visible;
            progressBar.IsEnabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_LoadAllContacts);
            bw.RunWorkerAsync();
        }
        
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void bw_LoadAllContacts(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                allContactsList = UsersTableUtils.getAllContacts();
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    contactsListBox.ItemsSource = allContactsList;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;

                });
            }
        }

        private void enterNameTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            string charsEnetered = enterNameTxt.Text.ToLower();
            if (String.IsNullOrEmpty(charsEnetered))
            {
                contactsListBox.ItemsSource = allContactsList;
                return;
            }
            List<ContactInfo> contactsList = getContactInfoFromNameOrPhone(charsEnetered);
            if (contactsList == null || contactsList.Count == 0)
            {
                contactsListBox.ItemsSource = null;
                return;
            }
            contactsListBox.ItemsSource = contactsList;
        }

        private List<ContactInfo> getContactInfoFromNameOrPhone(string charsEnetered)
        {
            if (allContactsList == null || allContactsList.Count == 0)
                return null;
            List<ContactInfo> contactsList = new List<ContactInfo>();
            for (int i = 0; i < allContactsList.Count; i++)
            {
                if (allContactsList[i].Name.ToLower().Contains(charsEnetered) || allContactsList[i].Msisdn.Contains(charsEnetered) || allContactsList[i].PhoneNo.Contains(charsEnetered))
                {
                    contactsList.Add(allContactsList[i]);
                }
            }
            return contactsList;
        }

        private void contactSelected_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactInfo contact = contactsListBox.SelectedItem as ContactInfo;
            if (contact == null)
                return;
            PhoneApplicationService.Current.State["objFromSelectUserPage"] = contact;
            PhoneApplicationService.Current.State["fromSelectUserPage"] = true;
            string uri = "/View/ChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void refreshContacts_Click(object sender, EventArgs e)
        {
            if (progress == null)
            {
                progress = new MyProgressIndicator();
            }

            progress.Show();
            canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(ContactUtils.makePatchRequest_Callback));
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (canGoBack)
            {
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
        }

        private void enterNameTxt_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
           // enterNameTxt.Background = textBoxBackground;
        }
    }
}