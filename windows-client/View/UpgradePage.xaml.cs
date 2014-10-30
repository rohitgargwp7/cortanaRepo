using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.UserData;
using System;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Navigation;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using windows_client.Languages;
using Microsoft.Phone.Data.Linq;
using System.IO.IsolatedStorage;
using windows_client.Misc;
using System.Diagnostics;
using windows_client.utils.Sticker_Helper;
using windows_client.ViewModel;
using windows_client.Model.Sticker;

namespace windows_client.View
{
    public partial class UpgradePage : PhoneApplicationPage
    {
        ManualResetEvent _contactSyncInProgress = new ManualResetEvent(false);
        BackgroundWorker _backgroundWorker = new BackgroundWorker();

        public UpgradePage()
        {
            InitializeComponent();
            Loaded += UpgradePage_Loaded;
        }

        void UpgradePage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= UpgradePage_Loaded;

            _backgroundWorker.DoWork -= _backgroundWorker_DoWork;
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;

            _backgroundWorker.RunWorkerCompleted += (ss, ee) =>
            {
                if (App.IS_MARKETPLACE)
                {
                    UpgradeApp();
                }
                else
                {
                    if (ee.Error == null)
                        UpgradeApp();
                    else
                        throw new UpgradeNotCompletedException(ee.Error);
                }
            };

            _backgroundWorker.RunWorkerAsync();
        }

        private void UpgradeApp()
        {
            App.appInitialize();

            // Upgrade complete, write the current version
            App.WriteToIsoStorageSettings(HikeConstants.FILE_SYSTEM_VERSION, App.LATEST_VERSION);

            ManageNavigation();
        }

        private void ManageNavigation()
        {
            string targetPage = (string)PhoneApplicationService.Current.State[HikeConstants.PAGE_TO_NAVIGATE_TO];

            if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("msisdn")) // PUSH NOTIFICATION CASE
            {
                if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                    return;
                }

                string msisdn = Utils.GetParamFromUri(targetPage);

                if (!App.appSettings.Contains(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE)
                && (!Utils.isGroupConversation(msisdn) || GroupManager.Instance.GetParticipantList(msisdn) != null))
                {
                    PhoneApplicationService.Current.State[HikeConstants.LAUNCH_FROM_PUSH_MSISDN] = msisdn;

                    NavigationService.Navigate(new Uri("/View/NewChatThread.xaml", UriKind.Relative));
                }
                else
                {
                    App page = (App)Application.Current;
                    ((UriMapper)(page.RootFrame.UriMapper)).UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);

                    page.RootFrame.Navigate(new Uri("/View/ConversationsList.xaml?id=1", UriKind.Relative));
                }
            }
            else if (targetPage != null && targetPage.Contains("ConversationsList") && targetPage.Contains("isStatus"))// STATUS PUSH NOTIFICATION CASE
            {
                if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                    return;
                }

                PhoneApplicationService.Current.State["IsStatusPush"] = true;

                App page = (App)Application.Current;
                ((UriMapper)(page.RootFrame.UriMapper)).UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);

                page.RootFrame.Navigate(new Uri("/View/ConversationsList.xaml?id=1", UriKind.Relative));
            }
            else if (targetPage != null && targetPage.Contains("ConversationsList.xaml") && targetPage.Contains("FileId")) // SHARE PICKER CASE
            {
                if (App.PageStateVal != App.PageState.CONVLIST_SCREEN)
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                    return;
                }

                int idx = targetPage.IndexOf("?") + 1;
                string param = targetPage.Substring(idx);

                NavigationService.Navigate(new Uri("/View/ForwardTo.xaml?" + param, UriKind.Relative));
            }
            else
            {
                if (App.PageStateVal == App.PageState.CONVLIST_SCREEN)
                {
                    App page = (App)Application.Current;
                    ((UriMapper)(page.RootFrame.UriMapper)).UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);

                    // Hardcoded id=1 to make this url different from default url so that it navigate to page
                    page.RootFrame.Navigate(new Uri("/View/ConversationsList.xaml?id=1", UriKind.Relative));
                }
                else
                {
                    Uri nUri = Utils.LoadPageUri(App.PageStateVal);
                    NavigationService.Navigate(nUri);
                }
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.PAGE_TO_NAVIGATE_TO);
        }

        /// <summary>
        /// Upgrade db and files in background.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (Utils.compareVersion(App.LATEST_VERSION, App.CURRENT_VERSION) == 1) // shows this is update
            {
                App.appSettings[App.APP_UPDATE_POSTPENDING] = true;
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.NEW_UPDATE, true);

                #region POST APP INFO ON UPDATE
                // If app info is already sent to server , this function will automatically handle.
                UpdatePostHelper.Instance.PostAppInfo();
                #endregion

                #region Post App Locale
                App.PostLocaleInfo();
                #endregion
            }

            // If current version is less than equal to 1.5.0.0 then upgrade DB.
            if (Utils.compareVersion("1.5.0.0", App.CURRENT_VERSION) == 1)
                MqttDBUtils.MqttDbUpdateToLatestVersion();

            if (Utils.compareVersion("2.5.3.0", App.CURRENT_VERSION) == 1)
            {
                UpgradeContactsDBForPhoneKind();

                UpgradeConvMessageDBForReadBy();

                FriendsTableUtils.UpdateOldFilesWithCorrectLastSeen();

                // This folder should be created for launching file async(unknown file type).
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(HikeConstants.FILE_TRANSFER_TEMP_LOCATION))
                    {
                        store.CreateDirectory(HikeConstants.FILE_TRANSFER_TEMP_LOCATION);
                    }
                }
            }

            if (Utils.compareVersion("2.6.0.0", App.CURRENT_VERSION) == 1)
            {
                HandleEmptyGroupName();
            }

            if (Utils.compareVersion("2.6.5.0", App.CURRENT_VERSION) == 1)
            {
                ReShuffleStickerCategories();

                HandleContactNamesOnUpgrade();

                DeleteAngryStickerCategory();
            }
            
            if (Utils.compareVersion("2.8.0.8", App.CURRENT_VERSION) == 1)
            {
                UpgradeGroupInfoForReadBy();

                UpdateConverationsIndividually();
            }
            
            Thread.Sleep(2000);
        }

        private void UpdateConverationsIndividually()
        {
            if (App.ViewModel.ConvMap.Count > 0)
            {
                //in previous versions is hidden is not updated in individual files
                foreach (ConversationListObject co in App.ViewModel.ConvMap.Values)
                {
                    ConversationTableUtils.saveConvObject(co, co.Msisdn.Replace(":", "_"));
                }
                //to update metadata in all objects
                ConversationTableUtils.saveConvObjectList();
            }
        }

        /// <summary>
        /// Delete angry category stickers from IS
        /// </summary>
        private void DeleteAngryStickerCategory()
        {
            RecentStickerHelper recentSticker;
            if (HikeViewModel.StickerHelper == null || HikeViewModel.StickerHelper.RecentStickerHelper == null)
            {
                recentSticker = new RecentStickerHelper();
                recentSticker.LoadRecentStickers();
            }
            else
                recentSticker = HikeViewModel.StickerHelper.RecentStickerHelper;

            List<string> listAngrySticker = new List<string>();

            foreach (StickerObj sticker in recentSticker.RecentStickers)
            {
                if (sticker.Category == StickerHelper.CATEGORY_ANGRY)
                {
                    listAngrySticker.Add(sticker.Id);
                }
            }

            // Dont delete angry stickers present in recent
            StickerHelper.DeleteLowResCategory(StickerHelper.CATEGORY_ANGRY, listAngrySticker);

            // Change last selected category to recent
            String category;
            if (App.appSettings.TryGetValue(HikeConstants.AppSettings.LAST_SELECTED_STICKER_CATEGORY, out category) && category == StickerHelper.CATEGORY_ANGRY)
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.LAST_SELECTED_STICKER_CATEGORY, StickerHelper.CATEGORY_RECENT);
        }

        /// <summary>
        /// Handle group member names based on address book
        /// </summary>
        private void HandleContactNamesOnUpgrade()
        {
            GroupManager.Instance.LoadGroupParticpantsCache();

            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            if (hike_contacts_by_id != null)
            {
                bool isFavUpdated = false, isPendingUpdated = false;

                foreach (var id in hike_contacts_by_id.Keys)
                {
                    var list = hike_contacts_by_id[id];
                    foreach (var contactInfo in list)
                    {
                        if (App.ViewModel.ConvMap.ContainsKey(contactInfo.Msisdn)) // update convlist
                        {
                            try
                            {
                                var cObj = App.ViewModel.ConvMap[contactInfo.Msisdn];
                                if (cObj.ContactName != contactInfo.Name)
                                {
                                    cObj.ContactName = contactInfo.Name;
                                    ConversationTableUtils.updateConversation(cObj);

                                    if (cObj.IsFav)
                                    {
                                        MiscDBUtil.SaveFavourites(cObj);
                                        isFavUpdated = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("REFRESH CONTACTS : UPGRADE PAGE :: Update contact name exception " + ex.StackTrace);
                            }
                        }
                        else // fav and pending case
                        {
                            ConversationListObject c = App.ViewModel.GetFav(contactInfo.Msisdn);

                            if (c != null && c.ContactName != contactInfo.Name) // this user is in favs
                            {
                                c.ContactName = contactInfo.Name;
                                MiscDBUtil.SaveFavourites(c);
                                isFavUpdated = true;
                            }
                            else
                            {
                                c = App.ViewModel.GetPending(contactInfo.Msisdn);
                                if (c != null && c.ContactName != contactInfo.Name)
                                {
                                    c.ContactName = contactInfo.Name;
                                    isPendingUpdated = true;
                                }
                            }
                        }

                        if (GroupManager.Instance.GroupParticpantsCache != null)
                        {
                            foreach (string key in GroupManager.Instance.GroupParticpantsCache.Keys)
                            {
                                List<GroupParticipant> l = GroupManager.Instance.GroupParticpantsCache[key];
                                for (int i = 0; i < l.Count; i++)
                                {
                                    if (l[i].Msisdn == contactInfo.Msisdn && l[i].Name != contactInfo.Name)
                                    {
                                        l[i].Name = contactInfo.Name;
                                    }
                                }
                            }
                        }
                    }
                }

                if (isFavUpdated)
                    MiscDBUtil.SaveFavourites();

                if (isPendingUpdated)
                    MiscDBUtil.SavePendingRequests();
            }

            var contactList = UsersTableUtils.getAllContacts();

            foreach (var id in GroupManager.Instance.GroupParticpantsCache.Keys)
            {
                var grp = GroupManager.Instance.GroupParticpantsCache[id];
                foreach (var participant in grp)
                {
                    participant.IsInAddressBook = contactList == null ? false : contactList.Where(c => c.Msisdn == participant.Msisdn).Count() > 0 ? true : false;
                }
            }

            GroupManager.Instance.SaveGroupParticpantsCache();
        }

        /// <summary>
        /// Delete default doggy stickers and new default expression stickers
        /// </summary>
        private void ReShuffleStickerCategories()
        {
            #region changing hardcoded stickers

            // To download default stickers removed from snuggles.
            StickerHelper.UpdateHasMoreMessages(StickerHelper.CATEGORY_DOGGY, true, true);

            // Remove expressions stickers if already downloaded to remove duplicacy.
            StickerHelper.DeleteSticker(StickerHelper.CATEGORY_EXPRESSIONS, StickerHelper.ArrayDefaultExpressionStickers.ToList());

            // If default doggy stickers were in recents, then remove those.
            List<string> listPreviousHardcodedDoggy = new List<string>
                                     {
                                      "001_hi.png",
                                      "002_thumbsup.png",
                                      "003_drooling.png",
                                      "004_devilsmile.png",
                                      "005_sorry.png",
                                      "006_urgh.png",
                                      "007_confused.png",
                                      "008_dreaming.png"
                                     };

            RecentStickerHelper.DeleteSticker(StickerHelper.CATEGORY_DOGGY, listPreviousHardcodedDoggy);

            #endregion
        }

        private void HandleEmptyGroupName()
        {
            bool groupEmptyNameFound = false;

            // Conv map is initialised in app.xaml.cs
            if (App.ViewModel.ConvMap.Count > 0)
            {
                foreach (ConversationListObject convObj in App.ViewModel.ConvMap.Values)
                {
                    if (convObj.IsGroupChat && string.IsNullOrEmpty(convObj.ContactName))
                    {
                        GroupManager.Instance.LoadGroupParticipants(convObj.Msisdn);
                        convObj.ContactName = GroupManager.Instance.defaultGroupName(convObj.Msisdn);
                        ConversationTableUtils.updateGroupName(convObj.Msisdn, convObj.ContactName);
                        groupEmptyNameFound = true;
                    }
                }

                if (groupEmptyNameFound) //update whole file as well
                    ConversationTableUtils.saveConvObjectList();
            }
        }

        /// <summary>
        /// Update DB for read by. Add column to ConvMessage
        /// </summary>
        private void UpgradeConvMessageDBForReadBy()
        {
            using (HikeChatsDb db = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                if (db.DatabaseExists())
                {
                    DatabaseSchemaUpdater dbUpdater = db.CreateDatabaseSchemaUpdater();
                    int version = dbUpdater.DatabaseSchemaVersion;

                    // db was updated on upgrade from 1.8 hence we need to bump db version number
                    // This bug was left out in 2.5.2.0 which led to chat msg issues for 720 lumia users
                    if (version < 3)
                    {
                        dbUpdater.AddColumn<ConvMessage>("ReadByInfo");
                        dbUpdater.DatabaseSchemaVersion = 3;

                        try
                        {
                            dbUpdater.Execute();
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine("db not upgrade in v 2.5.3.0");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update Db. Add PhoneKind to ContactInfo
        /// </summary>
        private void UpgradeContactsDBForPhoneKind()
        {
            bool dbUdated = false;

            StatusMsgsTable.MessagesDbUpdateToLatestVersion();

            using (HikeUsersDb db = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                if (db.DatabaseExists())
                {
                    DatabaseSchemaUpdater dbUpdater = db.CreateDatabaseSchemaUpdater();
                    int version = dbUpdater.DatabaseSchemaVersion;
                    if (version < 2)
                    {
                        dbUpdater.AddColumn<ContactInfo>("PhoneNoKind");
                        dbUpdater.DatabaseSchemaVersion = 2;

                        try
                        {
                            dbUpdater.Execute();
                            dbUdated = true;
                        }
                        catch { }
                    }
                }
            }

            if (dbUdated)
            {
                ContactUtils.getContacts(new ContactUtils.contacts_Callback(updatePhoneKind_Callback));
                _contactSyncInProgress.WaitOne();
            }
        }

        /// <summary>
        /// This callback is on background thread started by getContacts function. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void updatePhoneKind_Callback(object sender, ContactsSearchEventArgs e)
        {
            ContactUtils.ContactState = ContactUtils.ContactScanState.ADDBOOK_NOT_SCANNING;

            Dictionary<string, List<ContactInfo>> new_contacts_by_id = ContactUtils.getContactsListMap(e.Results);
            Dictionary<string, List<ContactInfo>> hike_contacts_by_id = ContactUtils.convertListToMap(UsersTableUtils.getAllContacts());

            /* If no contacts in Phone as well as App , simply return */
            if ((new_contacts_by_id == null || new_contacts_by_id.Count == 0) && hike_contacts_by_id == null)
            {
                _contactSyncInProgress.Set();
                return;
            }

            List<ContactInfo> contactsToBeUpdated = null;

            if (new_contacts_by_id != null) // if there are contacts in phone perform this step
            {
                foreach (string id in new_contacts_by_id.Keys)
                {
                    List<ContactInfo> phList = new_contacts_by_id[id];

                    if (hike_contacts_by_id == null || !hike_contacts_by_id.ContainsKey(id))
                        continue;

                    List<ContactInfo> hkList = hike_contacts_by_id[id];

                    var listToUpdate = ContactUtils.getContactsToUpdateList(phList, hkList);

                    if (listToUpdate != null)
                    {
                        if (contactsToBeUpdated == null)
                            contactsToBeUpdated = new List<ContactInfo>();

                        foreach (var item in listToUpdate)
                            contactsToBeUpdated.Add(item);
                    }
                }
            }

            if (contactsToBeUpdated != null && contactsToBeUpdated.Count > 0)
            {
                UsersTableUtils.updateContacts(contactsToBeUpdated);
                ConversationTableUtils.updateConversation(contactsToBeUpdated);
            }

            _contactSyncInProgress.Set();
        }

        /// <summary>
        /// Update DB for read by and last read message. Add column to GroupInfo
        /// </summary>
        private void UpgradeGroupInfoForReadBy()
        {
            using (HikeChatsDb db = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                if (db.DatabaseExists())
                {
                    DatabaseSchemaUpdater dbUpdater = db.CreateDatabaseSchemaUpdater();
                    int version = dbUpdater.DatabaseSchemaVersion;

                   //db maximum version 3
                    if (version < 4)
                    {
                        dbUpdater.AddColumn<GroupInfo>("ReadByInfo");
                        dbUpdater.AddColumn<GroupInfo>("LastReadMessageId");
                        dbUpdater.DatabaseSchemaVersion = 4;

                        try
                        {
                            dbUpdater.Execute();
                        }
                        catch(Exception ex)
                        {
                            Debug.WriteLine("group info db not upgraded. ex:{0}", ex.Message);
                        }
                    }
                }
            }
        }
    }

    class UpgradeNotCompletedException : Exception
    {
        public UpgradeNotCompletedException(Exception ex)
            : base(ex.ToString())
        {
        }

        public UpgradeNotCompletedException(string msg)
            : base(msg)
        {
        }
    }
}