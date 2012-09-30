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
        private bool _enableInviteBtn = false;
        public bool EnableInviteBtn
        {
            get
            {
                return _enableInviteBtn;
            }
            set
            {
                if (_enableInviteBtn != value)
                {
                    _enableInviteBtn = value;
                    this.inviteBtn.IsEnabled = value;
                }
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
            photoChooserTask.PixelHeight = 95;
            photoChooserTask.PixelWidth = 95;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

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
            TiltEffect.TiltableItems.Add(typeof(TextBlock));
        }


        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_ID_FROM_CHATTHREAD);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_NAME_FROM_CHATTHREAD);
        }

        private void initPageBasedOnState()
        {
            groupId = PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] as string;
            groupName = PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] as string;

            GroupInfo gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;
            this.groupNameTxtBox.Text = groupName;
            List<GroupParticipant> hikeUsersList = new List<GroupParticipant>();
            List<GroupParticipant> smsUsersList = GetHikeAndSmsUsers(Utils.GroupCache[groupId], hikeUsersList);
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
            }
            if (smsUsersList != null && smsUsersList.Count > 0)
                EnableInviteBtn = true;
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
        }
        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.UPDATE_UI == type)
            {
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                if (msisdn != groupId)
                    return;
                byte[] _avatar = (byte[])vals[1];
                if (_avatar == null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        groupImage.Source = UI_Utils.Instance.DefaultAvatarBitmapImage;
                        return;
                    });
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MemoryStream memStream = new MemoryStream(_avatar);
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage empImage = new BitmapImage();
                    empImage.SetSource(memStream);
                    groupImage.Source = empImage;
                });
            }
            else if (HikePubSub.PARTICIPANT_JOINED_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != groupId)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupMembersOC.Clear();
                    for (int i = 0; i < Utils.GroupCache[groupId].Count; i++)
                    {
                        GroupParticipant gp = Utils.GroupCache[groupId][i];
                        if (!gp.HasLeft)
                            groupMembersOC.Add(gp);
                        if (!gp.IsOnHike && !EnableInviteBtn)
                            EnableInviteBtn = true;
                    }
                    groupName = ConversationsList.ConvMap[groupId].NameToShow;
                    groupNameTxtBox.Text = groupName;
                    PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                });
            }
            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                ConvMessage cm = (ConvMessage)obj;
                string eventGroupId = cm.Msisdn;
                if (eventGroupId != groupId)
                    return;
                string leaveMsisdn = cm.GroupParticipant;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    for (int i = 0; i < groupMembersOC.Count; i++)
                    {
                        if (groupMembersOC[i].Msisdn == leaveMsisdn)
                        {
                            groupMembersOC.RemoveAt(i);
                            groupName = ConversationsList.ConvMap[groupId].NameToShow;
                            groupNameTxtBox.Text = groupName; 
                            PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = groupName;
                            return;
                        }
                    }
                });
            }
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
        }
        #endregion

        #region SET GROUP PIC

        private void onGroupProfileTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!isProfilePicTapped)
                {
                    photoChooserTask.Show();
                    isProfilePicTapped = true;
                }
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show("Connection Problem. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            progressBar.IsEnabled = true;
            progressBar.Opacity = 1;
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                grpImage = new BitmapImage(uri);
                grpImage.CreateOptions = BitmapCreateOptions.None;
                grpImage.UriSource = uri;
                grpImage.ImageOpened += imageOpenedHandler;
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                isProfilePicTapped = false;
                progressBar.IsEnabled = false;
                progressBar.Opacity = 0;
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, 45, 45, 0, 95);
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
                    groupImage.Source = grpImage;
                    groupImage.Height = 90;
                    groupImage.Width = 90;
                    object[] vals = new object[3];
                    vals[0] = groupId;
                    vals[1] = buffer;
                    vals[2] = null;
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                }
                else
                {
                    MessageBox.Show("Cannot change Group Image. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                }
                progressBar.IsEnabled = false;
                progressBar.Opacity = 0;
                isProfilePicTapped = false;
            });
        }
        #endregion

        private void inviteSMSUsers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //TODO start this loop from end, after sorting is done on onHike status
            for (int i = 0; i < Utils.GroupCache[groupId].Count; i++)
            {
                GroupParticipant gp = Utils.GroupCache[groupId][i];
                if (!gp.IsOnHike)
                {
                    long time = utils.TimeUtils.getCurrentTimeStamp();
                    ConvMessage convMessage = new ConvMessage(App.invite_message, gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsInvite = true;
                    App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                }
            }

        }

        private void AddParticipants_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] = Utils.GetActiveGroupParticiants(groupId);
            //NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
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
                        result = MessageBox.Show("Connection Problem. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                        return;
                    }
                    progressBar.Opacity = 1;
                    progressBar.IsEnabled = true;
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
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = true;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupNameTxtBox.IsReadOnly = false;
                    saveIconButton.IsEnabled = true;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
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