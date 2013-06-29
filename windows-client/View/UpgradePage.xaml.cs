using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Controls;
using System.Net.NetworkInformation;
using windows_client.Languages;
using Microsoft.Phone.Data.Linq;

namespace windows_client.View
{
    public partial class UpgradePage : PhoneApplicationPage
    {
        Boolean _isContactsSyncComplete = false;
        private static List<ContactInfo> listContactInfo;
        public UpgradePage()
        {
            InitializeComponent();
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.PAGE_TO_NAVIGATE_TO);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (a, b) =>
            {
                if (Utils.compareVersion(App.LATEST_VERSION, App.CURRENT_VERSION) == 1) // shows this is update
                {
                    App.appSettings[App.APP_UPDATE_POSTPENDING] = true;
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.NEW_UPDATE, true);
                    #region POST APP INFO ON UPDATE
                    // if app info is already sent to server , this function will automatically handle
                    UpdatePostHelper.Instance.postAppInfo();
                    #endregion
                    #region Post App Locale
                    App.PostLocaleInfo();
                    #endregion
                }

                // if current version is less than equal to 1.8.0.0 then upgrade Chats DB to add statusMessages table
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.8.0.0") != 1)
                    StatusMsgsTable.MessagesDbUpdateToLatestVersion();
                if (Utils.compareVersion(App.CURRENT_VERSION, "1.5.0.0") != 1) // if current version is less than equal to 1.5.0.0 then upgrade DB
                    MqttDBUtils.MqttDbUpdateToLatestVersion();
                if (Utils.compareVersion("2.2.0.1", App.CURRENT_VERSION) == 1) // upgrade friend files for last seen time stamp
                {
                    using (HikeUsersDb db = new HikeUsersDb(App.UsersDBConnectionstring))
                    {
                        if (db.DatabaseExists())
                        {
                            DatabaseSchemaUpdater dbUpdater = db.CreateDatabaseSchemaUpdater();
                            int version = dbUpdater.DatabaseSchemaVersion;
                            if (version == 0)
                            {
                                dbUpdater.AddColumn<ContactInfo>("PhoneNoKind");
                                dbUpdater.DatabaseSchemaVersion = 1;

                                try
                                {
                                    dbUpdater.Execute();
                                }
                                catch { }
                            }
                        }
                    } 

                    //ContactUtils.getContacts(new ContactUtils.contacts_Callback(makePatchRequest_Callback));
                    //_isContactsSyncComplete = true;

                    //while (_isContactsSyncComplete)
                    //{
                    //    Thread.Sleep(100);
                    //}

                    if (Utils.compareVersion("2.2.0.0", App.CURRENT_VERSION) == 1) // upgrade friend files for last seen time stamp
                    {
                        FriendsTableUtils.UpdateOldFilesWithDefaultLastSeen();
                        App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.TUTORIAL_SCREEN_STICKERS);
                    }

                    if (Utils.compareVersion("2.1.0.0", App.CURRENT_VERSION) == 1)
                    {
                        App.WriteToIsoStorageSettings(App.SHOW_STATUS_UPDATES_TUTORIAL, true);
                        App.appSettings[HikeConstants.AppSettings.APP_LAUNCH_COUNT] = 1;
                    }
                }
                else
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);

                Thread.Sleep(2000);//added so that this shows at least for 2 sec
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (a, b) =>
            {
                App.appInitialize();
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, App.LATEST_VERSION);

                string targetPage = (string)PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO];
                
                if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
                {
                    string param = Utils.GetParamFromUri(targetPage);
                    NavigationService.Navigate(new Uri("/View/NewChatThread.xaml?" + param, UriKind.Relative));
                }
                else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("isStatus"))// STATUS PUSH NOTIFICATION CASE
                {
                    PhoneApplicationService.Current.State["IsStatusPush"] = true;
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                }
                else if (targetPage != null && targetPage.Contains("NewSelectUserPage.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
                {
                    if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                    {
                        Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                        NavigationService.Navigate(nUri);
                        return;
                    }

                    int idx = targetPage.IndexOf("?") + 1;
                    string param = targetPage.Substring(idx);
                    NavigationService.Navigate(new Uri("/View/ConversationsList.xaml?" + true, UriKind.Relative));
                }
                else
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                }
            };
        }

        /* This callback is on background thread started by getContacts function */
        public void makePatchRequest_Callback(object sender, ContactsSearchEventArgs e)
        {
            Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            /* If no contacts in Phone as well as App , simply return */
            if ((new_contacts_by_id == null || new_contacts_by_id.Count == 0) && hike_contacts_by_id == null)
            {
                _isContactsSyncComplete = false;
                return;
            }

            Dictionary<string, List<ContactInfo>> contacts_to_update_or_add = new Dictionary<string, List<ContactInfo>>();

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
                        contacts_to_update_or_add.Add(id, phList);
                  
                    hike_contacts_by_id.Remove(id);
                }

                new_contacts_by_id.Clear();
                new_contacts_by_id = null;
            }

            /* If nothing is changed simply return without sending update request*/
            if (contacts_to_update_or_add.Count == 0 && (hike_contacts_by_id == null || hike_contacts_by_id.Count == 0))
            {
                Thread.Sleep(1000);
                _isContactsSyncComplete = false;
                return;
            }

            JArray ids_to_delete = new JArray();
            if (hike_contacts_by_id != null)
            {
                foreach (string id in hike_contacts_by_id.Keys)
                    ids_to_delete.Add(id);
            }

            ContactUtils.contactsMap = contacts_to_update_or_add;
            ContactUtils.hike_contactsMap = hike_contacts_by_id;

            App.MqttManagerInstance.disconnectFromBroker(false);
            NetworkManager.turnOffNetworkManager = true;

            AccountUtils.updateAddressBook(contacts_to_update_or_add, ids_to_delete, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
        }

        public void updateAddressBook_Callback(JObject patchJsonObj)
        {
            if (patchJsonObj == null)
            {
                Thread.Sleep(1000);
                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                _isContactsSyncComplete = false;
                return;
            }

            List<ContactInfo> updatedContacts = ContactUtils.contactsMap == null ? null : AccountUtils.getContactList(patchJsonObj, ContactUtils.contactsMap, true);
            List<ContactInfo.DelContacts> hikeIds = null;

            // Code to delete the removed contacts
            if (ContactUtils.hike_contactsMap != null && ContactUtils.hike_contactsMap.Count != 0)
            {
                hikeIds = new List<ContactInfo.DelContacts>(ContactUtils.hike_contactsMap.Count);
                // This loop deletes all those contacts which are removed.
                foreach (string id in ContactUtils.hike_contactsMap.Keys)
                {
                    ContactInfo.DelContacts dCn = new ContactInfo.DelContacts(id, ContactUtils.hike_contactsMap[id][0].Msisdn);
                    hikeIds.Add(dCn);

                    if (App.ViewModel.ConvMap.ContainsKey(dCn.Msisdn)) // check convlist map to remove the 
                    {
                        try
                        {
                            // here we are removing name so that Msisdn will be shown instead of Name
                            App.ViewModel.ConvMap[dCn.Msisdn].ContactName = null;
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine("REFRESH CONTACTS :: Delete contact exception " + e.StackTrace);
                        }
                    }
                    else // if this contact is in favourite or pending and not in convMap update this also
                    {
                        ConversationListObject obj;
                        obj = App.ViewModel.GetFav(id);
                        if (obj == null) // this msisdn is not in favs , check in pending
                            obj = App.ViewModel.GetPending(id);
                        if (obj != null)
                            obj.ContactName = null;
                    }
                }
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

            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;
            _isContactsSyncComplete = false;
        }
    }
}