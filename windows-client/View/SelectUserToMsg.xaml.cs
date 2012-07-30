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
using Microsoft.Phone.UserData;
using System.Threading;
using Newtonsoft.Json.Linq;


namespace windows_client.View
{
    public partial class SelectUserToMsg : PhoneApplicationPage
    {
        public MyProgressIndicator progress = null;
        public bool canGoBack = true;
        public List<ContactInfo> allContactsList = null;

        private readonly SolidColorBrush textBoxBorder = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));

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
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
                Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());
                Dictionary<string, List<ContactInfo>> contacts_to_update = new Dictionary<string, List<ContactInfo>>();
                foreach (string id in new_contacts_by_id.Keys)
                {
                    List<ContactInfo> phList = new_contacts_by_id[id];
                    if (!hike_contacts_by_id.ContainsKey(id))
                    {
                        contacts_to_update.Add(id, phList);
                        continue;
                    }

                    List<ContactInfo> hkList = hike_contacts_by_id[id];
                    if (!ContactUtils.areListsEqual(phList, hkList))
                    {
                        contacts_to_update.Add(id, phList);
                    }
                    hike_contacts_by_id.Remove(id);
                }
                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
                /* If nothing is changed simply return without sending update request*/
                if (contacts_to_update.Count == 0 && hike_contacts_by_id.Count == 0)
                {
                    Thread.Sleep(1000);
                    progress.Hide();
                    canGoBack = true;
                    App.isABScanning = false;
                    return;
                }

                JArray ids_json = new JArray();
                foreach (string id in hike_contacts_by_id.Keys)
                {
                    ids_json.Add(id);
                }
                ContactUtils.contactsMap = contacts_to_update;
                ContactUtils.hike_contactsMap = hike_contacts_by_id;

                App.MqttManagerInstance.disconnectFromBroker(false);
                NetworkManager.turnOffNetworkManager = true;
                AccountUtils.updateAddressBook(contacts_to_update, ids_json, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
            }
            catch (Exception)
            {
            }
        }

        public class DelContacts
        {
            private string _id;
            private string _msisdn;

            public string Id
            {
                get
                {
                    return _id;
                }
            }
            public string Msisdn
            {
                get
                {
                    return _msisdn;
                }
            }
            public DelContacts(string id,string msisdn)
            {
                _id = id;
                _msisdn = msisdn;
            }
        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    progress.Hide();
                    canGoBack = true;
                });
                return;
            }

            List<ContactInfo> updatedContacts = AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap);
            List<DelContacts> hikeIds = new List<DelContacts>();
            foreach (string id in ContactUtils.hike_contactsMap.Keys)
            {
                DelContacts dCn = new DelContacts(id, ContactUtils.hike_contactsMap[id][0].Msisdn);
                hikeIds.Add(dCn);
            }

            if (hikeIds != null && hikeIds.Count > 0)
            {
                /* Delete ids from hike user DB */
                UsersTableUtils.deleteMultipleRows(hikeIds); // this will delete all rows in HikeUser DB that are not in Addressbook.
            }
            if (updatedContacts != null && updatedContacts.Count > 0)
            {
                UsersTableUtils.updateContacts(updatedContacts);
                ConversationTableUtils.updateConversation(updatedContacts);
            }
            //ConversationsList.ReloadConversations();
            allContactsList = UsersTableUtils.getAllContacts();
            App.isABScanning = false;
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;
            
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                contactsListBox.ItemsSource = allContactsList;
                progress.Hide();
                canGoBack = true;
            });
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
            enterNameTxt.BorderBrush = textBoxBorder;
        }

    }
}