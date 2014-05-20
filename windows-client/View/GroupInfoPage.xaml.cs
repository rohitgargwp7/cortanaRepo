﻿using System.Collections.Generic;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using windows_client.Model;
using windows_client.DbUtils;
using Microsoft.Phone.Tasks;
using System.Windows;
using System;
using System.Linq;
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
using System.Windows.Media;
using System.Threading.Tasks;

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage, HikePubSub.Listener
    {
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;

        private ApplicationBar appBar;
        private ApplicationBar editGroupNameAppBar;
        private ApplicationBarIconButton saveIconButton;
        private ApplicationBarIconButton changeImageIconButton;
        private ApplicationBarIconButton editNameIconButton;
        private ApplicationBarIconButton addIconButton;
        private ApplicationBarMenuItem inviteSMSparticipantsMenuItem;

        bool isgroupNameSelfChanged = false;
        bool isProfilePicTapped = false;
        string groupName;
        byte[] fullViewImageBytes = null;
        byte[] thumbnailBytes = null;
        BitmapImage grpImage = null;
        private int smsUsers = 0;
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

        List<ContactGroup<GroupParticipant>> _participantList;

        public GroupInfoPage()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;

            InitAppBar();

            initPageBasedOnState();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }

        #region App Bar

        private void InitAppBar()
        {
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color
            };

            editNameIconButton = new ApplicationBarIconButton();
            editNameIconButton.IconUri = new Uri("/View/images/AppBar/icon_edit.png", UriKind.Relative);
            editNameIconButton.Text = AppResources.Edit_AppBar_Txt;
            editNameIconButton.Click += editNameIconButton_Click;
            appBar.Buttons.Add(editNameIconButton);

            changeImageIconButton = new ApplicationBarIconButton();
            changeImageIconButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            changeImageIconButton.Text = AppResources.ChangePic_AppBar_Txt;
            changeImageIconButton.Click += changeImageIconButton_Click;
            appBar.Buttons.Add(changeImageIconButton);

            addIconButton = new ApplicationBarIconButton();
            addIconButton.IconUri = new Uri("/View/images/AppBar/appbar.add.rest.png", UriKind.Relative);
            addIconButton.Text = AppResources.Add_AppBar_Txt;
            addIconButton.Click += addIconButton_Click;
            appBar.Buttons.Add(addIconButton);

            inviteSMSparticipantsMenuItem = new ApplicationBarMenuItem();
            inviteSMSparticipantsMenuItem.Text = AppResources.GroupInfo_InviteSMSUsers_Menu_Txt;
            inviteSMSparticipantsMenuItem.Click += inviteSMSparticipantsMenuItem_Click;
            appBar.MenuItems.Add(inviteSMSparticipantsMenuItem);
            appBar.IsMenuEnabled = false;

            editGroupNameAppBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color
            };

            saveIconButton = new ApplicationBarIconButton();
            saveIconButton.IconUri = new Uri("/View/images/AppBar/icon_save.png", UriKind.Relative);
            saveIconButton.Text = AppResources.Save_AppBar_Btn;
            saveIconButton.Click += saveGroupName_Click;
            saveIconButton.IsEnabled = false;
            editGroupNameAppBar.Buttons.Add(saveIconButton);

            this.ApplicationBar = appBar;
        }

        void inviteSMSparticipantsMenuItem_Click(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.INVITE_SMS_PARTICIPANTS);
            //TODO start this loop from end, after sorting is done on onHike status
            string msisdns = string.Empty, toNum = String.Empty;
            JObject obj = new JObject();
            JArray numlist = new JArray();
            JObject data = new JObject();
            int i;
            int smsUsersCount = 0;
            for (i = 0; i < GroupManager.Instance.GroupCache[groupId].Count; i++)
            {
                GroupParticipant gp = GroupManager.Instance.GroupCache[groupId][i];
                if (!gp.IsOnHike)
                {
                    msisdns += gp.Msisdn + ";";
                    numlist.Add(gp.Msisdn);
                    toNum = gp.Msisdn;
                    smsUsersCount++;
                }
            }

            var ts = TimeUtils.getCurrentTimeStamp();
            var smsString = AppResources.sms_invite_message;

            if (smsUsersCount == 1)
            {
                obj[HikeConstants.TO] = toNum;
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.HIKE_MESSAGE] = smsString;
                data[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.INVITE;
            }
            else
            {
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.INVITE_LIST] = numlist;
                obj[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.MULTIPLE_INVITE;
            }

            if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
            {
                App.MqttManagerInstance.mqttPublishToServer(obj);
                MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_InviteSent_MsgBoxText_Txt, AppResources.GroupInfo_InviteSent_MsgBoxHeader_Txt, MessageBoxButton.OK);
            }
            else
            {
                obj[HikeConstants.SUB_TYPE] = HikeConstants.NO_SMS;
                App.MqttManagerInstance.mqttPublishToServer(obj);

                SmsComposeTask smsComposeTask = new SmsComposeTask();
                smsComposeTask.To = msisdns;
                smsComposeTask.Body = smsString;
                smsComposeTask.Show();
            }
        }

        private void saveGroupName_Click(object sender, EventArgs e)
        {
            this.Focus();

            if (string.IsNullOrWhiteSpace(this.groupNameTxtBox.Text))
            {
                MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_GrpNameCannotBeEmpty_Txt, AppResources.Error_Txt, MessageBoxButton.OK);
                groupNameTxtBox.Focus();
                return;
            }
            groupName = this.groupNameTxtBox.Text.Trim();
            if (groupName.Length > 30)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_GrpNameMaxLength_Txt, AppResources.Error_Txt, MessageBoxButton.OK);
                groupNameTxtBox.Focus();
                return;
            }
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
                    shellProgress.Visibility = Visibility.Visible;
                    groupNameTxtBox.IsReadOnly = true;
                    saveIconButton.IsEnabled = false;
                    AccountUtils.setGroupName(groupName, groupId, new AccountUtils.postResponseFunction(setName_Callback));
                }
                else
                {
                    saveIconButton.IsEnabled = false;
                    this.groupNameTxtBox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
                }
            }
        }

        void changeImageIconButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isProfilePicTapped)
                {
                    try
                    {
                        photoChooserTask.Show();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Group Info Page ::  onGroupProfileTap , Exception : " + ex.StackTrace);
                    }
                    isProfilePicTapped = true;
                }
            }
            catch
            {
            }
        }

        void addIconButton_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] = GroupManager.Instance.GetActiveGroupParticiants(groupId);
            PhoneApplicationService.Current.State["Group_GroupId"] = groupId;
            NavigationService.Navigate(new Uri("/View/ForwardTo.xaml", UriKind.Relative));
        }

        void editNameIconButton_Click(object sender, EventArgs e)
        {
            groupNameTxtBox.Focus();
            groupNameTxtBox.Select(groupNameTxtBox.Text.Length, 0);
        }

        #endregion

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            removeListeners();
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_ID_FROM_CHATTHREAD);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_NAME_FROM_CHATTHREAD);
            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (ApplicationBar == editGroupNameAppBar)
            {
                this.Focus();
                groupNameTextBlock.Text = groupNameTxtBox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
                HideTextBox();
                ApplicationBar = appBar;
                e.Cancel = true;
                return;
            }

            base.OnBackKeyPress(e);
        }

        private void initPageBasedOnState()
        {
            groupId = PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] as string;
            groupName = PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] as string;

            gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;

            GroupManager.Instance.LoadGroupParticipants(groupId);
            groupData.Text = String.Format(AppResources.People_In_Group, GroupManager.Instance.GroupCache[groupId].Where(gp => gp.HasLeft == false).Count() + 1);

            if (!App.IS_TOMBSTONED && App.ViewModel.ConvMap.ContainsKey(groupId))
                groupImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
            else
            {
                string grpId = groupId.Replace(":", "_");
                groupImage.Source = UI_Utils.Instance.GetBitmapImage(grpId);
            }

            this.groupNameTxtBox.Text = groupNameTextBlock.Text = groupName;

            LoadParticipants();
            LoadHighResImage();

            this.groupChatParticipants.ItemsSource = _participantList;

            registerListeners();
        }

        async void LoadHighResImage()
        {
            await Task.Delay(1);

            if (MiscDBUtil.hasCustomProfileImage(groupId))
            {
                var bytes = MiscDBUtil.getLargeImageForMsisdn(groupId);

                if (bytes != null)
                    groupImage.Source = UI_Utils.Instance.createImageFromBytes(bytes);
            }
            else
                groupImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(groupId, true);
        }

        private void LoadParticipants()
        {
            _participantList = CreateGroups();

            List<GroupParticipant> hikeUsersList = new List<GroupParticipant>();
            List<GroupParticipant> smsUsersList = GetHikeAndSmsUsers(GroupManager.Instance.GroupCache[groupId], hikeUsersList);
            GroupParticipant self = new GroupParticipant(groupId, (string)App.appSettings[App.ACCOUNT_NAME], App.MSISDN, true);
            hikeUsersList.Add(self);
            hikeUsersList.Sort();

            for (int i = 0; i < (hikeUsersList != null ? hikeUsersList.Count : 0); i++)
            {
                GroupParticipant gp = hikeUsersList[i];
                if (gi.GroupOwner == gp.Msisdn)
                    gp.IsOwner = true;

                if (gi.GroupOwner == (string)App.appSettings[App.MSISDN_SETTING] && gp.Msisdn != gi.GroupOwner) // if this user is owner
                    gp.RemoveFromGroup = Visibility.Visible;
                else
                    gp.RemoveFromGroup = Visibility.Collapsed;

                gp.MemberImage = UI_Utils.Instance.GetBitmapImage(gp.Msisdn);

                _participantList[0].Add(gp);
            }

            for (int i = 0; i < (smsUsersList != null ? smsUsersList.Count : 0); i++)
            {
                GroupParticipant gp = smsUsersList[i];

                if (gi.GroupOwner == (string)App.appSettings[App.MSISDN_SETTING]) // if this user is owner
                    gp.RemoveFromGroup = Visibility.Visible;
                else
                    gp.RemoveFromGroup = Visibility.Collapsed;

                gp.MemberImage = UI_Utils.Instance.GetBitmapImage(gp.Msisdn);

                _participantList[1].Add(gp);
                smsUsers++;
            }

            appBar.IsMenuEnabled = EnableInviteBtn;
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
                mPubSub.removeListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this); 
                mPubSub.removeListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
                mPubSub.removeListener(HikePubSub.GROUP_NAME_CHANGED, this);
                mPubSub.removeListener(HikePubSub.GROUP_END, this);
                mPubSub.removeListener(HikePubSub.USER_JOINED, this);
                mPubSub.removeListener(HikePubSub.USER_LEFT, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Group Info Page ::  removeListeners , Exception : " + ex.StackTrace);
            }
        }

        public void onEventReceived(string type, object obj)
        {
            #region PARTICIPANT_JOINED_GROUP
            if (HikePubSub.PARTICIPANT_JOINED_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != groupId)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    LoadParticipants();
                    groupName = App.ViewModel.ConvMap[groupId].NameToShow;
                    groupNameTxtBox.Text = groupNameTextBlock.Text = groupName;
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
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, leaveMsisdn, groupId);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (gp.IsOnHike)
                        _participantList[0].Remove(gp);
                    else
                    {
                        _participantList[1].Remove(gp);
                        smsUsers--;
                        if (smsUsers <= 0)
                            appBar.IsMenuEnabled = false;
                    }

                    groupName = App.ViewModel.ConvMap[groupId].NameToShow; // change name of group
                    groupNameTxtBox.Text = groupNameTextBlock.Text = groupName;
                    PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                });
            }
            #endregion
            #region GROUP_NAME_CHANGED
            else if (HikePubSub.GROUP_NAME_CHANGED == type)
            {
                if (isgroupNameSelfChanged)
                    return;
                string grpId = (string)obj;
                if (grpId == groupId)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.groupNameTxtBox.Text =groupNameTextBlock.Text = App.ViewModel.ConvMap[groupId].ContactName;
                    });
                }
            }
            #endregion
            #region GROUP PIC CHANGED
            else if (HikePubSub.UPDATE_GRP_PIC == type)
            {
                string grpId = (string)obj;
                if (grpId == groupId)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.groupImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
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
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, ms, groupId);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _participantList[1].Remove(gp);
                    _participantList[0].Add(gp);
                    smsUsers--;
                    if (smsUsers == 0)
                        appBar.IsMenuEnabled = false;
                });
            }
            #endregion
            #region USER LEFT HIKE
            else if (HikePubSub.USER_LEFT == type)
            {
                string ms = (string)obj;
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, ms, groupId);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    _participantList[0].Remove(gp);
                    _participantList[1].Add(gp);
                    smsUsers++;
                    gp.IsOnHike = false;
                    appBar.IsMenuEnabled = true;
                });
            }

            #endregion
        }
        #endregion

        #region SET GROUP PIC

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            shellProgress.Visibility = Visibility.Visible;
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
                shellProgress.Visibility = Visibility.Collapsed;
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
                    App.ViewModel.ConvMap[groupId].Avatar = thumbnailBytes;
                    groupImage.Source = grpImage;

                    string msg = string.Format(AppResources.GroupImgChangedByGrpMember_Txt, AppResources.You_Txt);
                    ConvMessage cm = new ConvMessage(msg, groupId, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.RECEIVED_READ, -1, -1, this.Orientation);
                    cm.GrpParticipantState = ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED;
                    cm.GroupParticipant = App.MSISDN;
                    JObject jo = new JObject();
                    jo[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_DISPLAY_PIC;
                    cm.MetaDataString = jo.ToString(Newtonsoft.Json.Formatting.None);
                    ConversationListObject cobj = MessagesTableUtils.addChatMessage(cm, false);
                    if (cobj == null)
                        return;

                    // handle msgs
                    object[] vs = new object[2];
                    vs[0] = cm;
                    vs[1] = cobj;
                    mPubSub.publish(HikePubSub.MESSAGE_RECEIVED, vs);

                    // handle saving image
                    object[] vals = new object[3];
                    vals[0] = groupId;
                    vals[1] = fullViewImageBytes;
                    vals[2] = thumbnailBytes;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                }
                else
                {
                    MessageBox.Show(AppResources.CannotChangeGrpImg_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBar.IsEnabled = false;
                shellProgress.Visibility = Visibility.Collapsed;
                isProfilePicTapped = false;
            });
        }

        #endregion


        #region CHANGE GROUP NAME

        private void setName_Callback(JObject obj)
        {
            if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                //db and ui would be updated after server sends group name change packet 
                isgroupNameSelfChanged = true;
                PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupNameTextBlock.Text = groupName;
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = false;
                    shellProgress.Visibility = Visibility.Collapsed;
                    HideTextBox();
                    ApplicationBar = appBar;
                });
            }
            else
            {
                groupName = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = true;
                    shellProgress.Visibility = Visibility.Collapsed;
                    MessageBox.Show(AppResources.CannotChangeGrpName_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                });
            }
        }

        private void groupNameTxtBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _dontOpenPic = true;

            ApplicationBar = editGroupNameAppBar;
        }

        #endregion

        private void saveContactTask_Completed(object sender, TaskEventArgs e)
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
            catch (Exception ex)
            {
                Debug.WriteLine("Group Info Page ::  contactSearchCompleted_Callback , Exception : " + ex.StackTrace);
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

                    ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber, (int)ph.Kind);
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
                            Debug.WriteLine("Duplicate Contact !! for Phone Number " + cInfo.PhoneNo);
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
            mPubSub.publish(HikePubSub.CONTACT_ADDED, contactInfo);

            Dispatcher.BeginInvoke(() =>
            {
                gp_obj.Name = contactInfo.Name;

                // if default grp name is not set , then only update grp 
                if (gi.GroupName == null)
                {
                    string gpName = GroupManager.Instance.defaultGroupName(groupId);
                    groupNameTxtBox.Text = groupNameTextBlock.Text = gpName;
                    if (App.newChatThreadPage != null)
                        App.newChatThreadPage.userName.Text = gpName;
                    if (App.ViewModel.ConvMap.ContainsKey(groupId))
                        App.ViewModel.ConvMap[groupId].ContactName = gpName;
                }

                // update normal 1-1 chat contact
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

            ContactUtils.UpdateGroupCacheWithContactName(contactInfo.Msisdn, contactInfo.Name);
        }

        private void groupMember_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            GroupParticipant gp = (sender as Grid).DataContext as GroupParticipant;

            if (gp == null)
                return;

            object[] grpMemberObject = new object[3] { gp.Msisdn, gp.Name, gp.IsOnHike };
            PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE] = gp;
            NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
        }

        private void MenuItem_Tap_AddUser(object sender, RoutedEventArgs e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }

            gp_obj = (sender as MenuItem).DataContext as GroupParticipant;
            if (gp_obj == null)
                return;

            if (!gp_obj.Msisdn.Contains(gp_obj.Name)) // shows name is already stored so return
                return;

            ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(gp_obj.Msisdn);
            if (ci != null)
                return;

            ContactUtils.saveContact(gp_obj.Msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        }

        private void MenuItem_Tap_RemoveMember(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(AppResources.RemoveFromGrpConfirmation_Txt, AppResources.Remove_From_grp_txt, MessageBoxButton.OKCancel);
            if (result != MessageBoxResult.OK)
                return;

            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                return;
            }

            GroupParticipant gp_obj = (sender as MenuItem).DataContext as GroupParticipant;
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

        private void MenuItem_Tap_AddRemoveFav(object sender, RoutedEventArgs e)
        {
            GroupParticipant gp = (sender as MenuItem).DataContext as GroupParticipant;
            if (gp != null)
            {
                if (gp.IsFav) // already fav , remove request
                {
                    var text = String.Format(AppResources.Conversations_RemFromFav_Confirm_Txt, gp.Name);
                    MessageBoxResult result = MessageBox.Show(text, AppResources.RemFromFav_Txt, MessageBoxButton.OKCancel);
                    if (result != MessageBoxResult.OK)
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
                    FriendsTableUtils.SetFriendStatus(gp.Msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
                    // if this user is on hike and contact is stored in DB then add it to contacts on hike list
                    if (gp.IsOnHike)//on hike and in address book will be checked by convlist page
                    {
                        App.HikePubSubInstance.publish(HikePubSub.REMOVE_FRIENDS, gp.Msisdn);
                    }
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
                    if (gp.IsOnHike)
                    {
                        ContactInfo c = null;
                        if (App.ViewModel.ContactsCache.ContainsKey(gp.Msisdn))
                        {
                            c = App.ViewModel.ContactsCache[gp.Msisdn];
                            App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, c);
                        }
                        else if (gp.IsOnHike)
                        {
                            App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, gp.Msisdn);
                        }
                    }
                    App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_GROUP_INFO);
                    FriendsTableUtils.SetFriendStatus(favObj.Msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);

                }
            }
        }

        private void groupNameTxtBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var newName = groupNameTxtBox.Text.Trim();

            if (newName != groupName && !String.IsNullOrEmpty(newName))
                saveIconButton.IsEnabled = true;
            else
                saveIconButton.IsEnabled = false;
        }

        bool _dontOpenPic = false;

        private void enlargeImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_dontOpenPic)
            {
                _dontOpenPic = false;
                return;
            }

            PhoneApplicationService.Current.State["displayProfilePic"] = new object[] { groupId };
            Uri nextPage = new Uri("/View/DisplayImage.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _dontOpenPic = true;
        }

        private void MenuItem_InviteUser(object sender, RoutedEventArgs e)
        {
            GroupParticipant gp = (sender as MenuItem).DataContext as GroupParticipant;
            if (gp != null)
            {
                string msisdns = string.Empty, toNum = String.Empty;
                JObject obj = new JObject();
                JArray numlist = new JArray();
                JObject data = new JObject();

                msisdns = gp.Msisdn + ";";
                numlist.Add(gp.Msisdn);
                toNum = gp.Msisdn;

                var ts = TimeUtils.getCurrentTimeStamp();
                var smsString = AppResources.sms_invite_message;

                obj[HikeConstants.TO] = toNum;
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.HIKE_MESSAGE] = smsString;
                data[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.INVITE;

                if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
                {
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_InviteSent_MsgBoxText_Txt, AppResources.GroupInfo_InviteSent_MsgBoxHeader_Txt, MessageBoxButton.OK);
                }
                else
                {
                    obj[HikeConstants.SUB_TYPE] = HikeConstants.NO_SMS;
                    App.MqttManagerInstance.mqttPublishToServer(obj);

                    SmsComposeTask smsComposeTask = new SmsComposeTask();
                    smsComposeTask.To = msisdns;
                    smsComposeTask.Body = smsString;
                    smsComposeTask.Show();
                }
            }
        }

        #region Populate Group participants

        private List<ContactGroup<GroupParticipant>> CreateGroups()
        {
            string[] Groups = new string[]
            {
                AppResources.NewComposeGroup_HikeContacts,
                AppResources.NewComposeGroup_1HikeContact,
                AppResources.NewComposeGroup_SMSContacts,
                AppResources.NewComposeGroup_1SMSContact
            };

            List<ContactGroup<GroupParticipant>> glist = new List<ContactGroup<GroupParticipant>>();

            for (int i = 0; i < Groups.Length; i++, i++)
            {
                ContactGroup<GroupParticipant> g = new ContactGroup<GroupParticipant>(Groups[i], Groups[i + 1]);
                glist.Add(g);
            }

            return glist;
        }

        #endregion

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;

            contextMenu.ClearValue(FrameworkElement.DataContextProperty);
        }

        private void groupNameTextBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowTextBox();
        }

        void ShowTextBox()
        {
            groupNameTextBlock.Visibility = Visibility.Collapsed;
            groupNameTxtBox.Visibility = Visibility.Visible;
            groupNameTxtBox.Select(groupNameTxtBox.Text.Length, 0);
            groupNameTxtBox.Focus();
        }

        void HideTextBox()
        {
            groupNameTextBlock.Visibility = Visibility.Visible;
            groupNameTxtBox.Visibility = Visibility.Collapsed;
        }
    }
}