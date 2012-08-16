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
        private List<GroupMembers> activeGroupMembers;
        private ObservableCollection<GroupMembers> groupMembers = new ObservableCollection<GroupMembers>();
        private PhotoChooserTask photoChooserTask;
        private string groupId;
        private HikePubSub mPubSub;
        private GroupInfo gi;

        public GroupInfoPage()
        {
            InitializeComponent();
            initPageBasedOnState();
            mPubSub = App.HikePubSubInstance;
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = 95;
            photoChooserTask.PixelWidth = 95;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            BitmapImage groupProfileBitmap = UI_Utils.Instance.getBitMapImage(groupId);
            if (groupProfileBitmap != null)
            {
                groupImage.Source = groupProfileBitmap;
            }
        }

        private void initPageBasedOnState()
        {
            gi = PhoneApplicationService.Current.State["objFromChatThreadPage"] as GroupInfo;
            groupId = gi.GroupId;
            this.groupName.Text = gi.GroupName; // nope wrong
            activeGroupMembers = GroupTableUtils.getActiveGroupMembers(groupId);
            activeGroupMembers.Sort(Utils.CompareByName<GroupMembers>);
            for (int i = 0; i < activeGroupMembers.Count; i++)
                groupMembers.Add(activeGroupMembers[i]);
            this.groupChatParticipants.ItemsSource = groupMembers;
        }

        #region PUBSUB
        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
        }
        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.UPDATE_UI == type)
            {
                BitmapImage groupProfileBitmap = UI_Utils.Instance.getBitMapImage(groupId);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (groupProfileBitmap != null)
                    {
                        groupImage.Source = groupProfileBitmap;
                    }
                });
            }
            else if (HikePubSub.PARTICIPANT_JOINED_GROUP == type)
            {
                JObject json = (JObject)obj;
                string joinedGroupId = (string)json[HikeConstants.TO];
                if (joinedGroupId == groupId)
                { 
                
                }

            }
            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                JObject json = (JObject)obj;
                string leaveGroupId = (string)json[HikeConstants.TO];
                if (leaveGroupId == groupId)
                {
                    string leaveMsisdn = (string)json[HikeConstants.DATA];
                    int i = 0;
                    for (; i < groupMembers.Count; i++)
                    {
                        if (groupMembers[i].Msisdn == leaveMsisdn) ;
                        break;
                    }
                    groupMembers.RemoveAt(i);
                    activeGroupMembers.RemoveAt(i);
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
            //MemoryStream msLargeImage = new MemoryStream();
            //writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
            MemoryStream msSmallImage = new MemoryStream();
            writeableBitmap.SaveJpeg(msSmallImage, 45, 45, 0, 95);
            buffer = msSmallImage.ToArray();
            //send image to server here and insert in db after getting response
            AccountUtils.updateProfileIcon(buffer, new AccountUtils.postResponseFunction(updateProfile_Callback), groupId);

            object[] vals = new object[3];
            vals[0] = groupId;
            vals[1] = msSmallImage;
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
            for (int i = 0; i < activeGroupMembers.Count; i++)
            {
                if (!Utils.getGroupParticipant(activeGroupMembers[i].Name, activeGroupMembers[i].Msisdn).IsOnHike)
                {
                    long time = utils.TimeUtils.getCurrentTimeStamp();
                    ConvMessage convMessage = new ConvMessage(App.invite_message, activeGroupMembers[i].Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsInvite = true;
                    App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                }
            }

        }

        private void AddParticipants_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["existingGroupMembers"] = activeGroupMembers;
            PhoneApplicationService.Current.State["groupInfoFromGroupProfile"] = gi;
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml?param=grpChat", UriKind.Relative));
        }

    }
}