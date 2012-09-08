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
        private ObservableCollection<GroupMembers> groupMembersOC = new ObservableCollection<GroupMembers>();
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
            byte [] avatar = MiscDBUtil.getThumbNailForMsisdn(groupId);
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

        private void initPageBasedOnState()
        {
            groupId = PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] as string;
            string groupName = PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] as string;
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_ID_FROM_CHATTHREAD);
            PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_NAME_FROM_CHATTHREAD);

            GroupInfo gi = GroupTableUtils.getGroupInfoForId(groupId);
            if (gi == null)
                return;
            this.groupName.Text = groupName;

            activeGroupMembers = GroupTableUtils.getActiveGroupMembers(groupId);
            activeGroupMembers.Sort(Utils.CompareByName<GroupMembers>);
            for (int i = 0; i < activeGroupMembers.Count; i++)
                groupMembersOC.Add(activeGroupMembers[i]);
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
                JToken participantsToken;
                json.TryGetValue(HikeConstants.DATA, out participantsToken);
                if (participantsToken != null)
                {
                    JArray j = participantsToken.ToObject<JArray>();
                    IEnumerable<JToken> participantsList = j.Children<JToken>();

                    using (var participantInfoEnumerator = participantsList.GetEnumerator())
                    {
                        while (participantInfoEnumerator.MoveNext())
                        {
                            // Do something with sequenceEnum.Current.
                            string name = (string)participantInfoEnumerator.Current.ToObject<JObject>()[HikeConstants.NAME];
                            string msisdn = (string)participantInfoEnumerator.Current.ToObject<JObject>()[HikeConstants.MSISDN];
                            GroupMembers groupMembers = new GroupMembers(groupId, msisdn, name);
                            AddUserJoinedToCollection(groupMembers);
                        }
                    }
                }
            }
            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != groupId)
                    return;
                string leaveMsisdn = (string)json[HikeConstants.DATA];
                int i = 0;
                for (; i < activeGroupMembers.Count; i++)
                {
                    if (activeGroupMembers[i].Msisdn == leaveMsisdn)
                        break;
                }
                activeGroupMembers.RemoveAt(i);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    groupMembersOC.RemoveAt(i);
                });
            }
        }
        #endregion

        private void AddUserJoinedToCollection(GroupMembers gMembers)
        {
            int i = 0;
            for (; i < activeGroupMembers.Count; i++)
            {
                if (Utils.CompareByName<GroupMembers>(activeGroupMembers[i], gMembers) > 0)
                    break;
            }
            activeGroupMembers.Insert(i, gMembers);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                groupMembersOC.Insert(i, gMembers);
            });
        }

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
            PhoneApplicationService.Current.State[HikeConstants.EXISTING_GROUP_MEMBERS] = activeGroupMembers;
            PhoneApplicationService.Current.State["isAddNewParticipants"] = true;
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
        }

    }
}