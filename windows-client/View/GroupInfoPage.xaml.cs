using System.Collections.Generic;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using windows_client.Model;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;
using System.Windows;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Net.NetworkInformation;
using System.Windows.Controls;
using windows_client.Misc;
using System.Diagnostics;
using Microsoft.Phone.UserData;
using windows_client.Languages;
using windows_client.ViewModel;

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage, HikePubSub.Listener
    {
        private ObservableCollection<GroupParticipant> groupMembersOC = new ObservableCollection<GroupParticipant>();
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;
        private ApplicationBar appBar;
        private ApplicationBarIconButton saveIconButton;
        bool isgroupNameSelfChanged = false;
        bool isProfilePicTapped = false;
        string groupName;
        byte[] fullViewImageBytes = null;
        byte[] thumbnailBytes = null;
        BitmapImage grpImage = null;
        private int smsUsers = 0;
        private bool imageHandlerCalled = false;
        private ContactInfo contactInfo = null;
        private GroupParticipant gp_obj; // this will be used for adding unknown number to add book
        private GroupInfo gi;
        public bool EnableInviteBtn
        {
            get
            {
                if (smsUsers > 0)
                    return true;
                else
                    return false;
            }
        }

        public GroupInfoPage()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            saveIconButton = new ApplicationBarIconButton();
            saveIconButton.IconUri = new Uri("/View/images/icon_save.png", UriKind.Relative);
            saveIconButton.Text = AppResources.Save_AppBar_Btn;
            saveIconButton.Click += new EventHandler(doneBtn_Click);
            saveIconButton.IsEnabled = true;
            appBar.Buttons.Add(saveIconButton);
            //groupInfoPage.ApplicationBar = appBar;

            initPageBasedOnState();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
            TiltEffect.TiltableItems.Add(typeof(TextBlock));
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            removeListeners();
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_ID_FROM_CHATTHREAD);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_NAME_FROM_CHATTHREAD);
            base.OnRemovedFromJournal(e);
        }

        private void initPageBasedOnState()
        {
            groupId = PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] as string;
            groupName = PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] as string;

            gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;
            if (!App.IS_TOMBSTONED)
                groupImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
            else
            {
                string grpId = groupId.Replace(":", "_");
                byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(grpId);
                if (avatar == null)
                    groupImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(grpId);
                else
                {
                    MemoryStream memStream = new MemoryStream(avatar);
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage empImage = new BitmapImage();
                    empImage.SetSource(memStream);
                    groupImage.Source = empImage;
                }
                if (Utils.isDarkTheme())
                {
                    addUserImage.Source = new BitmapImage(new Uri("images/add_users_dark.png", UriKind.Relative));
                }
                GroupManager.Instance.LoadGroupParticipants(groupId);
            }
            this.groupNameTxtBox.Text = groupName;
            List<GroupParticipant> hikeUsersList = new List<GroupParticipant>();
            List<GroupParticipant> smsUsersList = GetHikeAndSmsUsers(GroupManager.Instance.GroupCache[groupId], hikeUsersList);
            GroupParticipant self = new GroupParticipant(groupId, (string)App.appSettings[App.ACCOUNT_NAME], App.MSISDN, true);
            hikeUsersList.Add(self);
            hikeUsersList.Sort();
            for (int i = 0; i < (hikeUsersList != null ? hikeUsersList.Count : 0); i++)
            {
                GroupParticipant gp = hikeUsersList[i];
                if (gi.GroupOwner == gp.Msisdn)
                    gp.IsOwner = 1;
                if (gi.GroupOwner == (string)App.appSettings[App.MSISDN_SETTING] && gp.Msisdn != gi.GroupOwner) // if this user is owner
                    gp.RemoveFromGroup = Visibility.Visible;
                else
                    gp.RemoveFromGroup = Visibility.Collapsed;
                groupMembersOC.Add(gp);
            }

            for (int i = 0; i < (smsUsersList != null ? smsUsersList.Count : 0); i++)
            {
                GroupParticipant gp = smsUsersList[i];
                if (gi.GroupOwner == (string)App.appSettings[App.MSISDN_SETTING]) // if this user is owner
                    gp.RemoveFromGroup = Visibility.Visible;
                else
                    gp.RemoveFromGroup = Visibility.Collapsed;
                groupMembersOC.Add(gp);
                smsUsers++;
            }

            this.inviteBtn.IsEnabled = EnableInviteBtn;
            this.groupChatParticipants.ItemsSource = groupMembersOC;
            registerListeners();
        }

        private List<GroupParticipant> GetHikeAndSmsUsers(List<GroupParticipant> list, List<GroupParticipant> hikeUsers)
        {
            if (list == null || list.Count == 0)
                return null;
            List<GroupParticipant> smsUsers = null;
            for (int i = 0; i < list.Count; i++)
            {

                if (!list[i].HasLeft && list[i].IsOnHike)
                {
                    hikeUsers.Add(list[i]);
                }
                else if (!list[i].HasLeft && !list[i].IsOnHike)
                {
                    if (smsUsers == null)
                        smsUsers = new List<GroupParticipant>();
                    smsUsers.Add(list[i]);
                }
            }
            return smsUsers;
        }

        #region PUBSUB
        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
            mPubSub.addListener(HikePubSub.GROUP_NAME_CHANGED, this);
            mPubSub.addListener(HikePubSub.GROUP_END, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
        }
        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.UPDATE_UI, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
                mPubSub.removeListener(HikePubSub.GROUP_NAME_CHANGED, this);
                mPubSub.removeListener(HikePubSub.GROUP_END, this);
                mPubSub.removeListener(HikePubSub.USER_JOINED, this);
                mPubSub.removeListener(HikePubSub.USER_LEFT, this);
            }
            catch
            {
            }
        }

        public void onEventReceived(string type, object obj)
        {
            #region UPDATE_UI
            if (HikePubSub.UPDATE_UI == type)
            {
                string msisdn = (string)obj;
                if (msisdn != groupId)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupImage.Source = App.ViewModel.ConvMap[msisdn].AvatarImage;
                });
            }
            #endregion
            #region PARTICIPANT_JOINED_GROUP
            else if (HikePubSub.PARTICIPANT_JOINED_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != groupId)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupMembersOC.Clear();
                    for (int i = 0; i < GroupManager.Instance.GroupCache[groupId].Count; i++)
                    {
                        GroupParticipant gp = GroupManager.Instance.GroupCache[groupId][i];
                        if (!gp.HasLeft)
                            groupMembersOC.Add(gp);
                        if (!gp.IsOnHike && !EnableInviteBtn)
                        {
                            smsUsers++;
                            this.inviteBtn.IsEnabled = true;
                        }
                    }
                    groupName = App.ViewModel.ConvMap[groupId].NameToShow;
                    groupNameTxtBox.Text = groupName;
                    PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                });
            }
            #endregion
            #region PARTICIPANT_LEFT_GROUP
            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                ConvMessage cm = (ConvMessage)obj;
                string eventGroupId = cm.Msisdn;
                if (eventGroupId != groupId)
                    return;
                string leaveMsisdn = cm.GroupParticipant;
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, leaveMsisdn, eventGroupId);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    for (int i = 0; i < groupMembersOC.Count; i++)
                    {
                        if (groupMembersOC[i].Msisdn == leaveMsisdn)
                        {
                            groupMembersOC.RemoveAt(i);
                            groupName = App.ViewModel.ConvMap[groupId].NameToShow; // change name of group
                            groupNameTxtBox.Text = groupName;
                            PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                            if (!gp.IsOnHike)
                            {
                                smsUsers--;
                                if (smsUsers <= 0)
                                    inviteBtn.IsEnabled = false;
                            }
                            return;
                        }
                    }
                });
            }
            #endregion
            #region GROUP_NAME_CHANGED
            else if (HikePubSub.GROUP_NAME_CHANGED == type)
            {
                object[] vals = (object[])obj;
                string grpId = (string)vals[0];
                string groupName = (string)vals[1];
                if (grpId == groupId)
                {
                    if (isgroupNameSelfChanged)
                        return;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.groupNameTxtBox.Text = groupName;
                        PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                    });
                }
            }
            #endregion
            #region GROUP_END
            else if (HikePubSub.GROUP_END == type)
            {
                string gId = (string)obj;
                if (gId == groupId)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        NavigationService.GoBack();
                    });
                }
            }
            #endregion
            #region USER JOINED HIKE
            else if (HikePubSub.USER_JOINED == type)
            {
                string ms = (string)obj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    for (int i = 0; i < groupMembersOC.Count; i++)
                    {
                        if (groupMembersOC[i].Msisdn == ms)
                        {
                            groupMembersOC[i].IsOnHike = true;
                            smsUsers--;
                            if (smsUsers == 0)
                                this.inviteBtn.IsEnabled = false;
                            return;
                        }
                    }
                });
            }
            #endregion
            #region USER LEFT HIKE
            else if (HikePubSub.USER_LEFT == type)
            {
                string ms = (string)obj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    for (int i = 0; i < groupMembersOC.Count; i++)
                    {
                        if (groupMembersOC[i].Msisdn == ms)
                        {
                            smsUsers++;
                            groupMembersOC[i].IsOnHike = false;
                            this.inviteBtn.IsEnabled = true;
                            return;
                        }
                    }
                });
            }

            #endregion
        }
        #endregion

        #region SET GROUP PIC

        private void onGroupProfileTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!isProfilePicTapped)
                {
                    try
                    {
                        photoChooserTask.Show();
                    }
                    catch { }
                    isProfilePicTapped = true;
                }
            }
            catch
            {
            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            shellProgress.IsVisible = true;
            imageHandlerCalled = true;
            if (e.TaskResult == TaskResult.OK)
            {
                //Uri uri = new Uri(e.OriginalFileName);
                grpImage = new BitmapImage();
                grpImage.SetSource(e.ChosenPhoto);
                try
                {
                    WriteableBitmap writeableBitmap = new WriteableBitmap(grpImage);
                    using (var msLargeImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msLargeImage, 83, 83, 0, 95);
                        thumbnailBytes = msLargeImage.ToArray();
                    }
                    using (var msSmallImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msSmallImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                        fullViewImageBytes = msSmallImage.ToArray();
                    }
                    //send image to server here and insert in db after getting response
                    AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), groupId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GROUP INFO :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                isProfilePicTapped = false;
                shellProgress.IsVisible = false;
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    App.ViewModel.ConvMap[groupId].Avatar = fullViewImageBytes;
                    groupImage.Source = grpImage;
                    groupImage.Height = 83;
                    groupImage.Width = 83;
                    object[] vals = new object[3];
                    vals[0] = groupId;
                    vals[1] = fullViewImageBytes;
                    vals[2] = thumbnailBytes;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                    if (App.newChatThreadPage != null)
                        App.newChatThreadPage.userImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
                }
                else
                {
                    MessageBox.Show(AppResources.CannotChangeGrpImg_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBar.IsEnabled = false;
                shellProgress.IsVisible = false;
                isProfilePicTapped = false;
            });
        }
        #endregion

        private void inviteSMSUsers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_SMS_PARTICIPANTS);
            //TODO start this loop from end, after sorting is done on onHike status
            for (int i = 0; i < GroupManager.Instance.GroupCache[groupId].Count; i++)
            {
                GroupParticipant gp = GroupManager.Instance.GroupCache[groupId][i];
                if (!gp.IsOnHike)
                {
                    long time = utils.TimeUtils.getCurrentTimeStamp();
                    ConvMessage convMessage = new ConvMessage(AppResources.sms_invite_message, gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsInvite = true;
                    App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                }
            }
            MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_InviteSent_MsgBoxText_Txt, AppResources.GroupInfo_InviteSent_MsgBoxHeader_Txt, MessageBoxButton.OK);


        }

        private void AddParticipants_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] = GroupManager.Instance.GetActiveGroupParticiants(groupId);
            PhoneApplicationService.Current.State["Group_GroupId"] = groupId;
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            this.Focus();

            if (string.IsNullOrWhiteSpace(this.groupNameTxtBox.Text))
            {
                MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_GrpNameCannotBeEmpty_Txt, AppResources.Error_Txt, MessageBoxButton.OK);
                groupNameTxtBox.Focus();
                return;
            }
            groupName = this.groupNameTxtBox.Text.Trim();
            // if group name is changed
            if (groupName != (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD])
            {
                MessageBoxResult result = MessageBox.Show(string.Format(AppResources.GroupInfo_GrpNameChangedTo_Txt, this.groupNameTxtBox.Text), AppResources.GroupInfo_ChangeGrpName_Txt, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    if (!NetworkInterface.GetIsNetworkAvailable())
                    {
                        result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                        return;
                    }
                    shellProgress.IsVisible = true;
                    //progressBar.IsEnabled = true;
                    groupNameTxtBox.IsReadOnly = true;
                    saveIconButton.IsEnabled = false;
                    AccountUtils.setGroupName(groupName, groupId, new AccountUtils.postResponseFunction(setName_Callback));
                }
                else
                    this.groupNameTxtBox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
            }
        }

        private void setName_Callback(JObject obj)
        {
            if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                ConversationTableUtils.updateGroupName(groupId, groupName);
                GroupTableUtils.updateGroupName(groupId, groupName);
                object[] vals = new object[2];
                vals[0] = groupId;
                vals[1] = groupName;
                isgroupNameSelfChanged = true;
                mPubSub.publish(HikePubSub.GROUP_NAME_CHANGED, vals);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.ConvMap[groupId].ContactName = groupName;
                    if (App.newChatThreadPage != null)
                        App.newChatThreadPage.userName.Text = groupName; // set the name here only to fix bug# 1666
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = true;
                    shellProgress.IsVisible = false;
                    //progressBar.IsEnabled = false;
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = true;
                    shellProgress.IsVisible = false;
                    //progressBar.IsEnabled = false;
                    this.groupNameTxtBox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
                    MessageBox.Show(AppResources.CannotChangeGrpName_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                });
            }
        }

        private void groupNameTxtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            groupInfoPage.ApplicationBar = appBar;

        }

        private void groupNameTxtBox_LostFocus(object sender, RoutedEventArgs e)
        {
            groupInfoPage.ApplicationBar = null;
        }

        private void btnGetSelected_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            groupChatParticipants.SelectedIndex = -1;
            gp_obj = (sender as ListBox).SelectedItem as GroupParticipant;
            if (gp_obj == null)
                return;
            if (!gp_obj.Msisdn.Contains(gp_obj.Name)) // shows name is already stored so return
                return;
            ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(gp_obj.Msisdn);
            if (ci != null)
                return;
            ContactUtils.saveContact(gp_obj.Msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        }

        private void saveContactTask_Completed(object sender, SaveContactResult e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    ContactUtils.getContact(gp_obj.Msisdn, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));
                    break;
                case TaskResult.Cancel:
                    MessageBox.Show(AppResources.User_Cancelled_Task_Txt);
                    break;
                case TaskResult.None:
                    MessageBox.Show(AppResources.NoInfoForTask_Txt);
                    break;
            }
        }

        public void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> contactListMap = GetContactListMap(e.Results);
                if (contactListMap == null)
                {
                    MessageBox.Show(AppResources.NO_CONTACT_SAVED);
                    return;
                }
                AccountUtils.updateAddressBook(contactListMap, null, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
            }
            catch (System.Exception)
            {
                //That's okay, no results//
            }
        }

        private Dictionary<string, List<ContactInfo>> GetContactListMap(IEnumerable<Contact> contacts)
        {
            int count = 0;
            int duplicates = 0;
            Dictionary<string, List<ContactInfo>> contactListMap = null;
            if (contacts == null)
                return null;
            contactListMap = new Dictionary<string, List<ContactInfo>>();
            foreach (Contact cn in contacts)
            {
                CompleteName cName = cn.CompleteName;

                foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                {
                    if (string.IsNullOrWhiteSpace(ph.PhoneNumber)) // if no phone number simply ignore the contact
                    {
                        count++;
                        continue;
                    }
                    ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber);
                    int idd = cInfo.GetHashCode();
                    cInfo.Id = Convert.ToString(Math.Abs(idd));
                    contactInfo = cInfo;
                    if (contactListMap.ContainsKey(cInfo.Id))
                    {
                        if (!contactListMap[cInfo.Id].Contains(cInfo))
                            contactListMap[cInfo.Id].Add(cInfo);
                        else
                        {
                            duplicates++;
                            Debug.WriteLine("Duplicate Contact !! for Phone Number {0}", cInfo.PhoneNo);
                        }
                    }
                    else
                    {
                        List<ContactInfo> contactList = new List<ContactInfo>();
                        contactList.Add(cInfo);
                        contactListMap.Add(cInfo.Id, contactList);
                    }
                }
            }
            Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
            Debug.WriteLine("Total contacts with no phone number : {0}", count);
            return contactListMap;
        }

        public void updateAddressBook_Callback(JObject obj)
        {
            if ((obj == null) || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
                });
                return;
            }
            JObject addressbook = (JObject)obj["addressbook"];
            if (addressbook == null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
                });
                return;
            }
            IEnumerator<KeyValuePair<string, JToken>> keyVals = addressbook.GetEnumerator();
            KeyValuePair<string, JToken> kv;
            int count = 0;
            while (keyVals.MoveNext())
            {
                kv = keyVals.Current;
                JArray entries = (JArray)addressbook[kv.Key];
                for (int i = 0; i < entries.Count; ++i)
                {
                    JObject entry = (JObject)entries[i];
                    string msisdn = (string)entry["msisdn"];
                    if (string.IsNullOrWhiteSpace(msisdn))
                        continue;

                    bool onhike = (bool)entry["onhike"];
                    contactInfo.Msisdn = msisdn;
                    contactInfo.OnHike = onhike;
                    count++;
                }
            }
            UsersTableUtils.addContact(contactInfo);
            Dispatcher.BeginInvoke(() =>
            {
                gp_obj.Name = contactInfo.Name;
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(gp_obj.Name, gp_obj.Msisdn, groupId);
                gp.Name = gp_obj.Name;
                GroupManager.Instance.GetParticipantList(groupId).Sort();
                GroupManager.Instance.SaveGroupCache(groupId);
                string gpName = GroupManager.Instance.defaultGroupName(groupId);
                groupNameTxtBox.Text = gpName;
                if (App.newChatThreadPage != null)
                    App.newChatThreadPage.userName.Text = gpName;
                if (App.ViewModel.ConvMap.ContainsKey(groupId))
                    App.ViewModel.ConvMap[groupId].ContactName = gpName;

                if (App.ViewModel.ConvMap.ContainsKey(gp_obj.Msisdn))
                {
                    App.ViewModel.ConvMap[gp_obj.Msisdn].ContactName = contactInfo.Name;
                }
                else // fav and pending case update
                {
                    ConversationListObject co = App.ViewModel.GetFav(gp_obj.Msisdn);
                    if (co != null)
                        co.ContactName = contactInfo.Name;
                }
                if (count > 1)
                {
                    MessageBox.Show(string.Format(AppResources.MORE_THAN_1_CONTACT_FOUND, gp_obj.Msisdn));
                }
                else
                {
                    MessageBox.Show(AppResources.CONTACT_SAVED_SUCCESSFULLY);
                }
            });
        }

        private void groupMemberImg_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            GroupParticipant gp = groupChatParticipants.SelectedItem as GroupParticipant;

            if (gp == null)
                return;

            string msisdn = gp.Msisdn == App.MSISDN ? HikeConstants.MY_PROFILE_PIC : gp.Msisdn;

            BitmapImage avatarImage = UI_Utils.Instance.getUserProfileThumbnail(msisdn);

            Object[] objArray = new Object[2];
            objArray[0] = gp;
            objArray[1] = avatarImage;

            if (gp.Msisdn == App.MSISDN)
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_PROFILE] = objArray;
            else
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE] = objArray;

            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        private void MenuItem_Tap_AddUser(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }
            ListBoxItem selectedListBoxItem = this.groupChatParticipants.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
                return;

            gp_obj = selectedListBoxItem.DataContext as GroupParticipant;

            if (gp_obj == null)
                return;
            if (!gp_obj.Msisdn.Contains(gp_obj.Name)) // shows name is already stored so return
                return;
            ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(gp_obj.Msisdn);
            if (ci != null)
                return;
            ContactUtils.saveContact(gp_obj.Msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        }

        private void MenuItem_Tap_RemoveMember(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.RemoveFromGrpConfirmation_Txt, AppResources.Remove_From_grp_txt, MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.Cancel)
                return;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }
            ListBoxItem selectedListBoxItem = this.groupChatParticipants.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
                return;

            GroupParticipant gp_obj = selectedListBoxItem.DataContext as GroupParticipant;
            if (gp_obj == null)
                return;


            // send 'gck' packet
            // remove from OC
            // correct group name if required
            // set this guy as left in group cache

            JArray kickOutMsisdns = new JArray();
            kickOutMsisdns.Add(gp_obj.Msisdn);
            JObject data = new JObject();
            data.Add(HikeConstants.MSISDNS, kickOutMsisdns);
            JObject jObj = new JObject();
            jObj.Add(HikeConstants.TO, groupId);
            jObj.Add(HikeConstants.FROM, App.MSISDN);
            jObj.Add(HikeConstants.DATA, data);
            jObj.Add(HikeConstants.TYPE, "gck");
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);

            //GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, gp_obj.Msisdn, groupId);
            //gp.HasLeft = true;
            //gp.IsUsed = false;
            //GroupManager.Instance.SaveGroupCache(groupId);

            //groupMembersOC.Remove(gp_obj);

            //if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set
            //{
            //    string grpName = GroupManager.Instance.defaultGroupName(groupId);
            //    groupNameTxtBox.Text = grpName;
            //    if (App.ViewModel.ConvMap.ContainsKey(groupId))
            //        App.ViewModel.ConvMap[groupId].ContactName = grpName;
            //    if(App.newChatThreadPage != null)
            //        App.newChatThreadPage.userName.Text = grpName;
        }

        private void MenuItem_Tap_AddRemoveFav(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.groupChatParticipants.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;
            if (selectedListBoxItem == null)
            {
                return;
            }
            GroupParticipant gp = selectedListBoxItem.DataContext as GroupParticipant;
            if (gp != null)
            {
                if (gp.IsFav) // already fav , remove request
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Conversations_RemFromFav_Confirm_Txt, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
                    if (result == MessageBoxResult.Cancel)
                        return;
                    gp.IsFav = false;
                    ConversationListObject favObj = App.ViewModel.GetFav(gp.Msisdn);
                    App.ViewModel.FavList.Remove(favObj);
                    if (App.ViewModel.ConvMap.ContainsKey(gp.Msisdn))
                    {
                        (App.ViewModel.ConvMap[gp.Msisdn]).IsFav = false;
                    }
                    JObject data = new JObject();
                    data["id"] = gp.Msisdn;
                    JObject obj = new JObject();
                    obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
                    obj[HikeConstants.DATA] = data;

                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                    App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.DeleteFavourite(gp.Msisdn);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
                    App.AnalyticsInstance.addEvent(Analytics.REMOVE_FAVS_CONTEXT_MENU_GROUP_INFO);
                }
                else // add to fav
                {
                    gp.IsFav = true;
                    ConversationListObject favObj;
                    if (App.ViewModel.ConvMap.ContainsKey(gp.Msisdn))
                    {
                        favObj = App.ViewModel.ConvMap[gp.Msisdn];
                        favObj.IsFav = true;
                    }
                    else
                        favObj = new ConversationListObject(gp.Msisdn, gp.Name, gp.IsOnHike, MiscDBUtil.getThumbNailForMsisdn(gp.Msisdn));
                    App.ViewModel.FavList.Insert(0, favObj);
                    if (App.ViewModel.IsPending(gp.Msisdn))
                    {
                        App.ViewModel.PendingRequests.Remove(favObj.Msisdn);
                        MiscDBUtil.SavePendingRequests();
                    }
                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.SaveFavourites(favObj);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
                    JObject data = new JObject();
                    data["id"] = gp.Msisdn;
                    JObject obj = new JObject();
                    obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
                    obj[HikeConstants.DATA] = data;
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                    App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
                    App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_GROUP_INFO);
                }
            }
        }
    }
}