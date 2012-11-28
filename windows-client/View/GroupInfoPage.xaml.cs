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
        byte[] buffer = null;
        BitmapImage grpImage = null;
        private int smsUsers = 0;
        private bool imageHandlerCalled = false;

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
            saveIconButton.Text = "save";
            saveIconButton.Click += new EventHandler(doneBtn_Click);
            saveIconButton.IsEnabled = true;
            appBar.Buttons.Add(saveIconButton);
            //groupInfoPage.ApplicationBar = appBar;

            initPageBasedOnState();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 83;
            photoChooserTask.PixelWidth = 83;
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

            GroupInfo gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;
            if (!App.IS_TOMBSTONED)
                groupImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
            else
            {
                string grpId = groupId.Replace(":", "_");
                byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(grpId);
                if (avatar == null)
                    groupImage.Source = UI_Utils.Instance.DefaultGroupImage;
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
                groupMembersOC.Add(gp);
            }

            for (int i = 0; i < (smsUsersList != null ? smsUsersList.Count : 0); i++)
            {
                GroupParticipant gp = smsUsersList[i];
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
                            if(smsUsers == 0)
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
                MessageBoxResult result = MessageBox.Show("Please try again", "No network connectivity", MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            shellProgress.IsVisible = true;
            imageHandlerCalled = true;
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                grpImage = new BitmapImage(uri);
                grpImage.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                //grpImage.UriSource = uri;
                grpImage.ImageOpened += imageOpenedHandler;
            }
            //else if (e.TaskResult == TaskResult.Cancel)
            //{
            //    isProfilePicTapped = false;
            //    //progressBar.IsEnabled = false;
            //    shellProgress.IsVisible = false;
            //    if (e.Error != null)
            //        MessageBox.Show("You cannot select photo while phone is connected to computer.", "", MessageBoxButton.OK);
            //}
            else
            {
                Uri uri = new Uri("/View/images/tick.png", UriKind.Relative);
                grpImage = new BitmapImage(uri);
                grpImage.CreateOptions = BitmapCreateOptions.None;
                grpImage.UriSource = uri;
                grpImage.ImageOpened += imageOpenedHandler;
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            if (!imageHandlerCalled)
                return;
            imageHandlerCalled = false;
            BitmapImage image = (BitmapImage)sender;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, 83, 83, 0, 95);
                buffer = msSmallImage.ToArray();
            }
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(buffer, new AccountUtils.postResponseFunction(updateProfile_Callback), groupId);
        }

        public void updateProfile_Callback(JObject obj)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && "ok" == (string)obj["stat"])
                {
                    App.ViewModel.ConvMap[groupId].Avatar = buffer;
                    groupImage.Source = grpImage;
                    groupImage.Height = 83;
                    groupImage.Width = 83;
                    object[] vals = new object[3];
                    vals[0] = groupId;
                    vals[1] = buffer;
                    vals[2] = null;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                    if (App.newChatThreadPage != null)
                        App.newChatThreadPage.userImage.Source = App.ViewModel.ConvMap[groupId].AvatarImage;
                }
                else
                {
                    MessageBox.Show("Cannot change Group Image. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
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
                    ConvMessage convMessage = new ConvMessage(App.sms_invite_message, gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsInvite = true;
                    App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                }
            }
            MessageBoxResult result = MessageBox.Show("Your friends have been invited", "Invite Sent", MessageBoxButton.OK);


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
                MessageBoxResult result = MessageBox.Show("Group name cannot be empty!", "Error !!", MessageBoxButton.OK);
                groupNameTxtBox.Focus();
                return;
            }
            groupName = this.groupNameTxtBox.Text.Trim();
            // if group name is changed
            if (groupName != (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD])
            {
                MessageBoxResult result = MessageBox.Show(string.Format("Group name will be changed to '{0}'", this.groupNameTxtBox.Text), "Change Group Name", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    if (!NetworkInterface.GetIsNetworkAvailable())
                    {
                        result = MessageBox.Show("Please try again", "No network connectivity", MessageBoxButton.OK);
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
            if (obj != null && "ok" == (string)obj["stat"])
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
                    if(App.newChatThreadPage != null)
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
                    MessageBox.Show("Cannot change GroupName. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
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
    }
}