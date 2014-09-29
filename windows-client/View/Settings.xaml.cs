using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.utils;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using Microsoft.Phone.Tasks;
using windows_client.Languages;
using windows_client.Controls;
using Microsoft.Phone.Net.NetworkInformation;
using windows_client.Misc;
using System.Threading;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using Microsoft.Phone.UserData;

namespace windows_client.View
{
    public partial class Settings : PhoneApplicationPage
    {
        public Settings()
        {
            InitializeComponent();
            int creditsRemaining = 0;
            App.appSettings.TryGetValue(App.SMS_SETTING, out creditsRemaining);
            smsCounterText.Text = String.Format(AppResources.Settings_SubtitleSMSSettings_Txt, creditsRemaining);

            if (AccountUtils.AppEnvironment != AccountUtils.DebugEnvironment.PRODUCTION)
                gridLogs.Visibility = Visibility.Collapsed;
        }

        private void Preferences_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Preferences.xaml", UriKind.Relative));
        }

        private void Notifications_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Notifications.xaml", UriKind.Relative));
        }

        private void Account_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Account.xaml", UriKind.Relative));
        }

        private void SMS_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/FreeSMS.xaml", UriKind.Relative));
        }

        private void Privacy_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Privacy.xaml", UriKind.Relative));
        }

        private ProgressIndicatorControl progressIndicator;
        private bool _canGoBack = true;

        private void Sync_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.Focus();
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }

            LayoutRoot.IsHitTestVisible = false;

            if (progressIndicator == null)
                progressIndicator = new ProgressIndicatorControl();

            progressIndicator.Show(LayoutRoot, AppResources.SelectUser_RefreshWaitMsg_Txt);

            _canGoBack = false;
            ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
        }

        /* This callback is on background thread started by getContacts function */
        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }

            Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            /* If no contacts in Phone as well as App , simply return */
            if ((new_contacts_by_id == null || new_contacts_by_id.Count == 0) && hike_contacts_by_id == null)
            {
                scanningComplete();
                _canGoBack = true;
                return;
            }

            Dictionary<string, List<ContactInfo>> contacts_to_update_or_add = new Dictionary<string, List<ContactInfo>>();
            List<ContactInfo> contactsToBeUpdated = null;

            if (new_contacts_by_id != null) // if there are contacts in phone perform this step
            {
                foreach (string id in new_contacts_by_id.Keys)
                {
                    List<ContactInfo> phList = new_contacts_by_id[id];
                    if (hike_contacts_by_id == null || !hike_contacts_by_id.ContainsKey(id))
                    {
                        contacts_to_update_or_add.Add(id, phList);
                        continue;
                    }

                    List<ContactInfo> hkList = hike_contacts_by_id[id];
                    if (!ContactUtils.areListsEqual(phList, hkList))
                    {
                        contacts_to_update_or_add.Add(id, phList);
                    }

                    var listToUpdate = ContactUtils.getContactsToUpdateList(phList, hkList);
                    if (listToUpdate != null)
                    {
                        if (contactsToBeUpdated == null)
                            contactsToBeUpdated = new List<ContactInfo>();

                        foreach (var item in listToUpdate)
                            contactsToBeUpdated.Add(item);
                    }

                    hike_contacts_by_id.Remove(id);
                }

                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
            }

            if (contactsToBeUpdated != null && contactsToBeUpdated.Count > 0)
            {
                UsersTableUtils.updateContacts(contactsToBeUpdated);
                ConversationTableUtils.updateConversation(contactsToBeUpdated);
            }

            /* If nothing is changed simply return without sending update request*/
            if (contacts_to_update_or_add.Count == 0 && (hike_contacts_by_id == null || hike_contacts_by_id.Count == 0))
            {
                Thread.Sleep(1000);
                scanningComplete();
                _canGoBack = true;
                return;
            }

            JArray ids_to_delete = new JArray();
            if (hike_contacts_by_id != null)
            {
                foreach (string id in hike_contacts_by_id.Keys)
                {
                    ids_to_delete.Add(id);
                }
            }

            ContactUtils.contactsMap = contacts_to_update_or_add;
            ContactUtils.hike_contactsMap = hike_contacts_by_id;

            App.MqttManagerInstance.disconnectFromBroker(false);
            NetworkManager.turnOffNetworkManager = true;

            /*
             * contacts_to_update : These are the contacts to add
             * ids_json : These are the contacts to delete
             */
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }

            AccountUtils.updateAddressBook(contacts_to_update_or_add, ids_to_delete, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
        }

        private bool _stopContactScanning = false;
        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }

            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                scanningComplete();
                _canGoBack = true;
                return;
            }

            List<ContactInfo.DelContacts> hikeIds = null;
            List<ContactInfo> deletedContacts = null;
            // Code to delete the removed contacts
            if (ContactUtils.hike_contactsMap != null && ContactUtils.hike_contactsMap.Count != 0)
            {
                hikeIds = new List<ContactInfo.DelContacts>(ContactUtils.hike_contactsMap.Count);
                deletedContacts = new List<ContactInfo>(ContactUtils.hike_contactsMap.Count);
                // This loop deletes all those contacts which are removed.
                Dictionary<string, GroupInfo> allGroupsInfo = null;
                GroupManager.Instance.LoadGroupCache();
                List<GroupInfo> gl = GroupTableUtils.GetAllGroups();
                for (int i = 0; i < gl.Count; i++)
                {
                    if (allGroupsInfo == null)
                        allGroupsInfo = new Dictionary<string, GroupInfo>();
                    allGroupsInfo[gl[i].GroupId] = gl[i];
                }

                bool isFavUpdated = false, isPendingUpdated = false;
                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo cinfo = ContactUtils.hike_contactsMap[id][0];
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, cinfo.Msisdn);
                    hikeIds.Add(dCn);
                    deletedContacts.Add(cinfo);
                    if (App.ViewModel.ConvMap.ContainsKey(dCn.Msisdn)) // check convlist map to remove the 
                    {
                        try
                        {
                            // here we are removing name so that Msisdn will be shown instead of Name
                            App.ViewModel.ConvMap[dCn.Msisdn].ContactName = null;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("REFRESH CONTACTS :: Delete contact exception " + e.StackTrace);
                        }
                    }
                    else // if this contact is in favourite or pending and not in convMap update this also
                    {
                        ConversationListObject obj;
                        obj = App.ViewModel.GetFav(cinfo.Msisdn);

                        if (obj == null) // this msisdn is not in favs , check in pending
                        {
                            obj = App.ViewModel.GetPending(cinfo.Msisdn);

                            if (obj != null)
                            {
                                obj.ContactName = null;
                                isPendingUpdated = true;
                            }
                        }
                        else
                        {
                            obj.ContactName = null;
                            MiscDBUtil.SaveFavourites(obj);
                            isFavUpdated = true;
                        }
                    }

                    if (App.ViewModel.ContactsCache.ContainsKey(dCn.Msisdn))
                        App.ViewModel.ContactsCache[dCn.Msisdn].Name = null;
                    cinfo.Name = cinfo.Msisdn;
                    GroupManager.Instance.RefreshGroupCache(cinfo, allGroupsInfo, false);
                }

                if (isFavUpdated)
                    MiscDBUtil.SaveFavourites();

                if (isPendingUpdated)
                    MiscDBUtil.SavePendingRequests();
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap);
            if (_stopContactScanning)
            {
                _stopContactScanning = false;
                return;
            }

            if (hikeIds != null && hikeIds.Count > 0)
            {
                /* Delete ids from hike user DB */
                UsersTableUtils.deleteMultipleRows(hikeIds); // this will delete all rows in HikeUser DB that are not in Addressbook.
            }

            if (deletedContacts != null && deletedContacts.Count > 0)
            {
                Object[] obj = new object[2];
                obj[0] = false;//denotes deleted contact
                obj[1] = deletedContacts;
                App.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            if (updatedContacts != null && updatedContacts.Count > 0)
            {
                UsersTableUtils.updateContacts(updatedContacts);
                ConversationTableUtils.updateConversation(updatedContacts);
                Object[] obj = new object[2];
                obj[0] = true;//denotes updated/added contact
                obj[1] = updatedContacts;
                App.HikePubSubInstance.publish(HikePubSub.ADDRESSBOOK_UPDATED, obj);
            }

            App.ViewModel.DeleteImageForDeletedContacts(deletedContacts, updatedContacts);

            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                scanningComplete();
            });

            _canGoBack = true;
        }

        private void scanningComplete()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                LayoutRoot.IsHitTestVisible = true;
                progressIndicator.Hide(LayoutRoot);
            });
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (!_canGoBack)
            {
                MessageBoxResult mbox = MessageBox.Show(AppResources.Stop_Contact_Scanning, AppResources.Stop_Caption_txt, MessageBoxButton.OKCancel);

                if (mbox == MessageBoxResult.OK)
                {
                    _stopContactScanning = true;
                    scanningComplete();
                    _canGoBack = true;
                }

                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }

        private void Help_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/Help.xaml", UriKind.Relative));
        }

        private void ViewLogs_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/MqttPreferences.xaml", UriKind.Relative));
        }

       
    }
}