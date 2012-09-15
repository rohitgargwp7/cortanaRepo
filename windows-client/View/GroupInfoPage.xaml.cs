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

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage, HikePubSub.Listener
    {
        private ObservableCollection<GroupParticipant> groupMembersOC = new ObservableCollection<GroupParticipant>();
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;

        public GroupInfoPage()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;
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
            this.groupName.Text = groupName;

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
                Uri uri = new Uri("/View/images/ic_avatar0.png", UriKind.Relative);
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
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
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

    }
}