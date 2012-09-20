﻿using System.Collections.Generic;
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

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage, HikePubSub.Listener
    {
        private ObservableCollection<GroupParticipant> groupMembersOC = new ObservableCollection<GroupParticipant>();
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;
        private ApplicationBar appBar;
        private ApplicationBarIconButton nextIconButton;
        bool isgroupNameSelfChanged = false;
        string groupName;

        public GroupInfoPage()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;

            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            nextIconButton = new ApplicationBarIconButton();
            nextIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            nextIconButton.Text = "done";
            nextIconButton.Click += new EventHandler(doneBtn_Click);
            nextIconButton.IsEnabled = true;
            appBar.Buttons.Add(nextIconButton);
            groupInfoPage.ApplicationBar = appBar;

            initPageBasedOnState();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 95;
            photoChooserTask.PixelWidth = 95;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            string grpId = groupId.Replace(":", "_");
            byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(groupId);
            if (avatar == null)
                groupImage.Source = UI_Utils.Instance.DefaultAvatarBitmapImage; // TODO : change to default groupImage once done
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
            string groupName = PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] as string;

            GroupInfo gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;
            this.groupNameTxtBox.Text = groupName;

            for (int i = 0; i < Utils.GroupCache[groupId].Count; i++)
            {
                GroupParticipant gp = Utils.GroupCache[groupId][i];
                if (!gp.HasLeft)
                    groupMembersOC.Add(gp);
            }
            this.groupChatParticipants.ItemsSource = groupMembersOC;
            registerListeners();
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
                    }
                });
            }
            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != groupId)
                    return;
                string leaveMsisdn = (string)json[HikeConstants.DATA];

                for (int i = 0; i < groupMembersOC.Count; i++)
                {
                    if (groupMembersOC[i].Msisdn == leaveMsisdn)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            groupMembersOC.RemoveAt(i);
                        });
                    }
                    break;
                }
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
                photoChooserTask.Show();
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
                return;
            }
            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                groupImage.Source = image;
                groupImage.Height = 90;
                groupImage.Width = 90;
            }
            else
            {
                Uri uri = new Uri("/View/images/default_group.png", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
                groupImage.Source = image;
                groupImage.Height = 90;
                groupImage.Width = 90;
            }
        }

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            byte[] buffer = null;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);

            using (var msSmallImage = new MemoryStream())
            {
                writeableBitmap.SaveJpeg(msSmallImage, 45, 45, 0, 95);
                buffer = msSmallImage.ToArray();
            }
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(buffer, new AccountUtils.postResponseFunction(updateProfile_Callback), groupId);

            object[] vals = new object[3];
            vals[0] = groupId;
            vals[1] = buffer;
            vals[2] = null;
            mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
        }

        public void updateProfile_Callback(JObject obj)
        {
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
            PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] = getActiveGroupParticiants();
            //NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
        }

        private List<GroupParticipant> getActiveGroupParticiants()
        {
            List<GroupParticipant> activeGroupMembers = new List<GroupParticipant>(Utils.GroupCache[groupId].Count);
            for (int i = 0; i < Utils.GroupCache[groupId].Count; i++)
            {
                if (!Utils.GroupCache[groupId][i].HasLeft)
                    activeGroupMembers.Add(Utils.GroupCache[groupId][i]);
            }
            return activeGroupMembers;
        }

        private void doneBtn_Click(object sender, EventArgs e)
        {
            this.Focus();
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
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    this.groupNameTxtBox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD];
                    MessageBox.Show("Cannot change GroupName. Try Later!!", "Oops, something went wrong!", MessageBoxButton.OK);
                });
            }
        }
    }
}