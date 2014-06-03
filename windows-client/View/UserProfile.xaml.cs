﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.DbUtils;
using System.IO;
using System.Windows.Media.Imaging;
using windows_client.utils;
using windows_client.Languages;
using Microsoft.Phone.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using windows_client.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Phone.UserData;
using System.ComponentModel;
using windows_client.Misc;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading.Tasks;

namespace windows_client.View
{
    public partial class UserProfile : PhoneApplicationPage, HikePubSub.Listener
    {
        private string msisdn;
        private PhotoChooserTask photoChooserTask;
        bool isProfilePicTapped = false;
        BitmapImage profileImage = null;
        byte[] fullViewImageBytes = null;
        byte[] thumbnailBytes = null;
        string nameToShow = null;
        string firstName = null;
        bool isOnHike = false;
        private ObservableCollection<BaseStatusUpdate> statusList = new ObservableCollection<BaseStatusUpdate>();
        private ApplicationBar appBar;
        ApplicationBarIconButton editProfileAppBarButton;
        ApplicationBarIconButton changePhotoAppBarButton;
        ApplicationBarIconButton addToContactsAppBarButton;
        bool isInvited;
        bool isInAddressBook;
        bool toggleToInvitedScreen;
        bool isBlocked;
        bool isStatusLoaded;
        FriendsTableUtils.FriendStatusEnum friendStatus;
        private long timeOfJoin;

        public UserProfile()
        {
            InitializeComponent();
            registerListeners();

            statusLLS.ItemsSource = statusList;
        }

        #region Listeners

        private void registerListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.STATUS_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.STATUS_DELETED, this);
            App.HikePubSubInstance.addListener(HikePubSub.FRIEND_RELATIONSHIP_CHANGE, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
        }

        private void removeListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.STATUS_RECEIVED, this);
                App.HikePubSubInstance.removeListener(HikePubSub.STATUS_DELETED, this);
                App.HikePubSubInstance.removeListener(HikePubSub.FRIEND_RELATIONSHIP_CHANGE, this);
                App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
                App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UserProfile.xaml :: removeListeners, Exception : " + ex.StackTrace);
            }
        }

        public void onEventReceived(string type, object obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("UserProfile :: OnEventReceived : Object received is null");
                return;
            }

            #region STATUS UPDATE RECEIVED
            if (HikePubSub.STATUS_RECEIVED == type)
            {
                StatusMessage sm = obj as StatusMessage;
                if (sm.Msisdn != msisdn) // only add status msg if user is viewing the profile of the same person whose atatus has been updated
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (sm.Status_Type == StatusMessage.StatusType.PROFILE_PIC_UPDATE && App.MSISDN != msisdn)
                        avatarImage.Source = UI_Utils.Instance.GetBitmapImage(msisdn);

                    if (isStatusLoaded)
                    {
                        var statusUpdate = StatusUpdateHelper.Instance.CreateStatusUpdate(sm, false);
                        if (statusUpdate != null)
                        {
                            statusList.Insert(0, statusUpdate);

                            if (statusLLS.ItemsSource == null)
                                statusLLS.ItemsSource = statusList;

                            statusLLS.ScrollTo(statusLLS.ItemsSource[0]);

                            gridSmsUser.Visibility = Visibility.Collapsed;
                        }
                    }
                });
            }
            #endregion
            #region STATUS_DELETED
            if (HikePubSub.STATUS_DELETED == type)
            {
                var sb = obj as BaseStatusUpdate;
                if (sb == null)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                  {
                      if (msisdn == App.MSISDN)
                      {
                          statusList.Remove(sb);

                          if (statusList.Count == 0)
                              ShowEmptyStatus();
                      }
                  });
            }
            #endregion
            #region FRIEND_RELATIONSHIP_CHANGE
            else if (type == HikePubSub.FRIEND_RELATIONSHIP_CHANGE)
            {
                Object[] objArray = (Object[])obj;
                string recMsisdn = objArray[0] as string;

                if (recMsisdn != msisdn)
                    return;
                if (isBlocked)
                    return;
                FriendsTableUtils.FriendStatusEnum previousStatus = friendStatus;
                friendStatus = (FriendsTableUtils.FriendStatusEnum)objArray[1];

                switch (friendStatus)
                {
                    #region TWO WAY FRIENDS
                    case FriendsTableUtils.FriendStatusEnum.FRIENDS:

                        List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetPaginatedStatusMsgsForMsisdn(msisdn, long.MaxValue, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            CreateStatusUi(statusMessagesFromDB, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                            isStatusLoaded = true;
                        });

                        break;
                    #endregion
                    #region REQUEST RECIEVED
                    case FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED:
                        statusMessagesFromDB = StatusMsgsTable.GetPaginatedStatusMsgsForMsisdn(msisdn, long.MaxValue, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            CreateStatusUi(statusMessagesFromDB, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                            isStatusLoaded = true;
                            ShowRequestRecievedPanel();

                        });
                        break;
                    #endregion
                    #region NO ACTION OR UNFRIENDED
                    default:
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ShowAddAsFriends();
                            statusList.Clear();
                        });
                        break;

                    #endregion
                    //case FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU: cannot be done from others
                    //case FriendsTableUtils.FriendStatusEnum.IGNORED: cannot be done by others
                    //case FriendsTableUtils.FriendStatusEnum.REQUEST_SENT: cannot be done by others
                }
            }
            #endregion
            #region USER_LEFT
            else if (HikePubSub.USER_LEFT == type)
            {

                string recMsisdn = (string)obj;
                try
                {
                    isOnHike = false;
                    if (isBlocked)
                        return;
                    if (msisdn != recMsisdn)
                        return;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ShowNonHikeUser();
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UserProfile:: onEventReceived, Exception : " + ex.StackTrace);
                }
            }
            #endregion
            #region USER_JOINED
            else if (HikePubSub.USER_JOINED == type)
            {
                string recMsisdn = (string)obj;
                try
                {
                    isOnHike = true;
                    if (isBlocked)
                        return;
                    if (msisdn != recMsisdn)
                        return;
                    timeOfJoin = FriendsTableUtils.GetFriendOnHIke(msisdn);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (timeOfJoin == 0)
                            AccountUtils.GetOnhikeDate(msisdn, new AccountUtils.postResponseFunction(GetHikeStatus_Callback));
                        else
                            txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, TimeUtils.GetOnHikeSinceDisplay(timeOfJoin));

                        ShowAddAsFriends();
                    });
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("UserProfile:: onEventReceived, Exception : " + ex.StackTrace);
                }
            }
            #endregion
        }

        #endregion

        GroupParticipant _groupParticipantObject;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.NavigationMode == NavigationMode.New || App.IS_TOMBSTONED)
            {
                object o;
                #region USER INFO FROM CONVERSATION PAGE
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_CONVERSATION_PAGE, out o))
                {
                    ConversationListObject co = (ConversationListObject)o;
                    if (o != null)
                    {
                        InitAppBar();
                        nameToShow = co.NameToShow;
                        isOnHike = co.IsOnhike;
                        profileImage = co.AvatarImage;
                        msisdn = co.Msisdn;
                        InitChatIconBtn();
                    }
                }
                #endregion
                #region USER INFO FROM CHAT THREAD
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE, out o))
                {
                    if (o is ConversationListObject)
                    {
                        ConversationListObject co = (ConversationListObject)o;
                        nameToShow = co.NameToShow;
                        isOnHike = co.IsOnhike;
                        profileImage = co.AvatarImage;
                        msisdn = co.Msisdn;
                    }
                    else if (o is ContactInfo)
                    {
                        ContactInfo cn = (ContactInfo)o;
                        if (cn.Name != null)
                            nameToShow = cn.Name;
                        else
                            nameToShow = cn.Msisdn;
                        isOnHike = cn.OnHike;
                        profileImage = UI_Utils.Instance.GetBitmapImage(cn.Msisdn);
                        msisdn = cn.Msisdn;
                    }
                }
                #endregion
                #region USER INFO FROM GROUP CHAT
                else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE, out o))
                {
                    _groupParticipantObject = o as GroupParticipant;
                    msisdn = _groupParticipantObject.Msisdn;
                    nameToShow = _groupParticipantObject.Name;

                    if (App.MSISDN == msisdn) // represents self page
                    {
                        InitSelfProfile();
                    }
                    else
                    {
                        InitAppBar();
                        profileImage = UI_Utils.Instance.GetBitmapImage(msisdn);
                        isOnHike = _groupParticipantObject.IsOnHike;
                        InitChatIconBtn();
                    }
                }
                #endregion
                #region USER OWN PROFILE
                else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.USERINFO_FROM_PROFILE))
                {
                    InitSelfProfile();
                }
                #endregion
                #region USER INFO FROM TIMELINE
                else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_TIMELINE, out o))
                {
                    Object[] objArr = o as Object[];
                    if (objArr != null)
                    {
                        msisdn = objArr[0] as string;
                        if (msisdn == App.MSISDN)
                            InitSelfProfile();
                        else
                        {
                            InitAppBar();
                            profileImage = UI_Utils.Instance.GetBitmapImage(msisdn);
                            nameToShow = objArr[1] as string;
                            isOnHike = true;//check as it can be false also
                            InitChatIconBtn();
                        }
                    }
                }
                #endregion

                avatarImage.Source = profileImage;
                txtUserName.Text = nameToShow;
                txtMsisdn.Text = msisdn;

                firstName = Utils.GetFirstName(nameToShow);

                //if blocked user show block ui and return
                if (msisdn != App.MSISDN && App.ViewModel.BlockedHashset.Contains(msisdn))
                {
                    isBlocked = true;
                    ShowBlockedUser();
                    if (appBar != null)
                        appBar.IsVisible = false;
                    return;
                }

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    isInAddressBook = CheckUserInAddressBook();
                };
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += delegate
                {
                    LoadCallCopyOptions();
                };

                if (!isOnHike)//sms user
                    ShowNonHikeUser();
                else
                    InitHikeUserProfile();
            }

            if (App.IS_TOMBSTONED)
                avatarImage.Source = UI_Utils.Instance.GetBitmapImage(msisdn);

            if (e.NavigationMode == NavigationMode.New || App.IS_TOMBSTONED)
                LoadHighResImage();

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_PIC_DOWNLOADED))
            {
                LoadHighResImage();
                PhoneApplicationService.Current.State.Remove(HikeConstants.IS_PIC_DOWNLOADED);
            }

            // this is done to update profile name , as soon as it gets updated
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PROFILE_NAME_CHANGED))
                txtUserName.Text = (string)PhoneApplicationService.Current.State[HikeConstants.PROFILE_NAME_CHANGED];
        }

        async void LoadHighResImage()
        {
            await Task.Delay(1);

            if (MiscDBUtil.hasCustomProfileImage(msisdn))
            {
                var bytes = MiscDBUtil.getLargeImageForMsisdn(msisdn);

                if (bytes != null)
                    avatarImage.Source = UI_Utils.Instance.createImageFromBytes(bytes);
            }
            else
                avatarImage.Source = UI_Utils.Instance.getDefaultAvatar(msisdn, true);
        }

        void LoadCallCopyOptions()
        {
            if (msisdn != App.MSISDN)
            {
                ApplicationBarIconButton callIconButton = new ApplicationBarIconButton();
                callIconButton.IconUri = new Uri("/View/images/AppBar/icon_call.png", UriKind.Relative);
                callIconButton.Text = AppResources.Call_Txt;
                callIconButton.Click += new EventHandler(Call_Click);
                callIconButton.IsEnabled = true;

                if (appBar == null)
                {
                    appBar = new ApplicationBar()
                       {
                           ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                           BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
                           Opacity = 0.95
                       };

                    appBar.StateChanged -= appBar_StateChanged;
                    appBar.StateChanged += appBar_StateChanged;
                }

                appBar.Buttons.Add(callIconButton);
                ApplicationBar = appBar;

                if (!isInAddressBook)
                    ShowAddToContacts();
            }

            ContextMenu menu = new ContextMenu();
            menu.Background = UI_Utils.Instance.Black;
            menu.Foreground = UI_Utils.Instance.White;
            menu.IsZoomEnabled = false;

            MenuItem menuItemCopy = new MenuItem() { Background = UI_Utils.Instance.Black, Foreground = UI_Utils.Instance.White };
            menuItemCopy.Header = AppResources.Copy_txt;
            menuItemCopy.Click += menuItemCopy_Click;

            menu.Items.Add(menuItemCopy);

            if (msisdn == txtUserName.Text)
            {
                ContextMenuService.SetContextMenu(txtUserName, menu);
                txtMsisdn.Visibility = Visibility.Collapsed;
            }
            else
                ContextMenuService.SetContextMenu(txtMsisdn, menu);
        }

        void appBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible)
                ApplicationBar.Opacity = 1;
            else
                ApplicationBar.Opacity = 0.95;
        }

        void menuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtMsisdn.Text);
        }

        void Call_Click(object sender, EventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();
            phoneCallTask.PhoneNumber = txtMsisdn.Text;
            phoneCallTask.DisplayName = txtUserName.Text;
            try
            {
                phoneCallTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UserProfile.xaml ::  menuItemCall_Click , Exception : " + ex.StackTrace);
            }
        }

        private void InitChatIconBtn()
        {
            ApplicationBarIconButton chatIconButton = new ApplicationBarIconButton();
            chatIconButton.IconUri = new Uri("/View/images/AppBar/icon_message.png", UriKind.Relative);
            chatIconButton.Text = AppResources.Send_Txt;
            chatIconButton.Click += new EventHandler(GoToChat_Tap);
            chatIconButton.IsEnabled = true;
            appBar.Buttons.Add(chatIconButton);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_CONVERSATION_PAGE);
            PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE);
            PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE);
            PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_PROFILE);
            PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_TIMELINE);
            PhoneApplicationService.Current.State.Remove(HikeConstants.PROFILE_NAME_CHANGED);
            removeListeners();
        }

        #region CHANGE PROFILE PIC

        private void onProfilePicButtonTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_dontOpenPic)
            {
                _dontOpenPic = false;
                return;
            }

            var id = App.MSISDN == msisdn ? HikeConstants.MY_PROFILE_PIC : msisdn;

            App.AnalyticsInstance.addEvent(Analytics.SEE_LARGE_PROFILE_PIC_FROM_USERPROFILE);
            object[] fileTapped = new object[2];
            fileTapped[0] = id;
            PhoneApplicationService.Current.State["displayProfilePic"] = fileTapped;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            //progressBarTop.IsEnabled = true;
            shellProgress.IsIndeterminate = true;
            if (e.TaskResult == TaskResult.OK)
            {
                profileImage = new BitmapImage();
                profileImage.SetSource(e.ChosenPhoto);
                try
                {
                    WriteableBitmap writeableBitmap = new WriteableBitmap(profileImage);
                    using (var msLargeImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                        thumbnailBytes = msLargeImage.ToArray();
                    }
                    using (var msSmallImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msSmallImage, HikeConstants.PROFILE_PICS_SIZE, HikeConstants.PROFILE_PICS_SIZE, 0, 100);
                        fullViewImageBytes = msSmallImage.ToArray();
                    }
                    //send image to server here and insert in db after getting response
                    AccountUtils.updateProfileIcon(fullViewImageBytes, new AccountUtils.postResponseFunction(updateProfile_Callback), "");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("USER PROFILE :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                isProfilePicTapped = false;
                //progressBarTop.IsEnabled = false;
                shellProgress.IsIndeterminate = false;
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
            }
        }

        public void updateProfile_Callback(JObject obj)
        {
            string serverId = null;
            bool uploadSuccess = false;
            if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
            {
                uploadSuccess = true;
                try
                {
                    serverId = obj["status"].ToObject<JObject>()[HikeConstants.STATUS_ID].ToString();
                    //todo:check
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("UserProfile.xaml :: updateProfile_Callback, serverid parse, Exception : " + ex.StackTrace);
                }
                if (serverId != null)
                {
                    MiscDBUtil.saveStatusImage(App.MSISDN, serverId, fullViewImageBytes);
                    StatusMessage sm = new StatusMessage(App.MSISDN, AppResources.PicUpdate_StatusTxt, StatusMessage.StatusType.PROFILE_PIC_UPDATE,
                        serverId, TimeUtils.getCurrentTimeStamp(), -1, true);
                    StatusMsgsTable.InsertStatusMsg(sm, false);
                    App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
                }
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (uploadSuccess)
                {
                    UI_Utils.Instance.BitmapImageCache[HikeConstants.MY_PROFILE_PIC] = profileImage;
                    avatarImage.Source = profileImage;
                    object[] vals = new object[3];
                    vals[0] = App.MSISDN;
                    vals[1] = fullViewImageBytes;
                    vals[2] = thumbnailBytes;
                    App.HikePubSubInstance.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);

                    if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                    {
                        App.ViewModel.ConvMap[msisdn].Avatar = thumbnailBytes;
                        App.HikePubSubInstance.publish(HikePubSub.UPDATE_PROFILE_ICON, msisdn);
                    }
                    else // update fav and contact section
                    {
                        if (msisdn == null)
                            return;
                        ConversationListObject c = App.ViewModel.GetFav(msisdn);
                        if (c != null) // for favourites
                        {
                            c.Avatar = thumbnailBytes;
                        }
                        else
                        {
                            c = App.ViewModel.GetPending(msisdn);
                            if (c != null) // for pending requests
                            {
                                c.Avatar = thumbnailBytes;
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBarTop.IsEnabled = false;
                shellProgress.IsIndeterminate = false;
                isProfilePicTapped = false;
            });
        }

        #endregion

        #region STATUS MESSAGES

        private void LoadStatuses()
        {
            shellProgress.IsIndeterminate = true;
            List<StatusMessage> statusMessagesFromDB = null;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (ss, ee) =>
            {
                statusMessagesFromDB = StatusMsgsTable.GetPaginatedStatusMsgsForMsisdn(msisdn, long.MaxValue, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (ss, ee) =>
            {
                CreateStatusUi(statusMessagesFromDB, HikeConstants.STATUS_INITIAL_FETCH_COUNT);
                isStatusLoaded = true;
                shellProgress.IsIndeterminate = false;
            };
        }

        long lastStatusId = -1;
        bool hasMoreStatus;

        //to be run on ui thread
        private void CreateStatusUi(List<StatusMessage> statusMessagesFromDB, int messageFetchCount)
        {
            AddStatusToList(statusMessagesFromDB, messageFetchCount);

            if (statusList.Count == 0)
                ShowEmptyStatus();
            else
                gridSmsUser.Visibility = Visibility.Collapsed;

            statusLLS.ItemsSource = statusList;
        }

        private void AddStatusToList(List<StatusMessage> statusMessagesFromDB, int messageFetchCount)
        {
            if (statusMessagesFromDB != null)
            {
                hasMoreStatus = false;
                for (int i = 0; i < statusMessagesFromDB.Count; i++)
                {
                    StatusMessage statusMessage = statusMessagesFromDB[i];
                    if (i == messageFetchCount - 1)
                    {
                        hasMoreStatus = true;
                        lastStatusId = statusMessage.StatusId;
                        break;
                    }
                    var statusUpdate = StatusUpdateHelper.Instance.CreateStatusUpdate(statusMessage, false);
                    if (statusUpdate != null)
                        statusList.Add(statusUpdate);
                }
            }
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ImageStatus statusUpdate = (sender as Grid).DataContext as ImageStatus;
            if (statusUpdate == null)
                return;
            string[] statusImageInfo = new string[2];
            statusImageInfo[0] = statusUpdate.Msisdn;
            statusImageInfo[1] = statusUpdate.ServerId;
            PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY] = statusImageInfo;
            Uri nextPage = new Uri("/View/DisplayImage.xaml", UriKind.Relative);
            NavigationService.Navigate(nextPage);
        }

        #endregion

        #region TAP EVENTS

        private void Invite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.IsEnabled || ((string)btn.Content == AppResources.Invited))
                return;
            btn.Content = AppResources.Invited;
            if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
            {
                long time = TimeUtils.getCurrentTimeStamp();
                ConvMessage convMessage = new ConvMessage(AppResources.sms_invite_message, msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = true;
                convMessage.IsInvite = true;
                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
            }
            else
            {
                string msisdns = string.Empty, toNum = String.Empty;
                JObject obj = new JObject();
                JArray numlist = new JArray();
                JObject data = new JObject();

                var ts = TimeUtils.getCurrentTimeStamp();
                var smsString = AppResources.sms_invite_message;

                obj[HikeConstants.TO] = toNum;
                data[HikeConstants.MESSAGE_ID] = ts.ToString();
                data[HikeConstants.HIKE_MESSAGE] = smsString;
                data[HikeConstants.TIMESTAMP] = ts;
                obj[HikeConstants.DATA] = data;
                obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                obj[HikeConstants.SUB_TYPE] = HikeConstants.NO_SMS;

                App.MqttManagerInstance.mqttPublishToServer(obj);

                SmsComposeTask smsComposeTask = new SmsComposeTask();
                smsComposeTask.To = msisdns;
                smsComposeTask.Body = smsString;
                smsComposeTask.Show();
            }

        }

        private void AddAsFriend_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (msisdn == App.MSISDN)
                return;
            if (isInvited)
                return;

            friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

            if (!App.ViewModel.Isfavourite(msisdn))
            {
                ConversationListObject favObj;
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    favObj = App.ViewModel.ConvMap[msisdn];
                    favObj.IsFav = true;
                }
                else
                    favObj = new ConversationListObject(msisdn, nameToShow, isOnHike, MiscDBUtil.getThumbNailForMsisdn(msisdn));//todo:change
                App.ViewModel.FavList.Insert(0, favObj);
                if (App.ViewModel.IsPending(msisdn))
                {
                    App.ViewModel.PendingRequests.Remove(favObj.Msisdn);
                    MiscDBUtil.SavePendingRequests();
                }
                //on tapping add as friend one can also become two way friend so need to add status is now friend
                App.ViewModel.RemoveFrndReqFromTimeline(msisdn, friendStatus);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(favObj);
                int count = 0;
                App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

                App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
                if (favObj.IsOnhike)
                {
                    ContactInfo c = null;
                    if (App.ViewModel.ContactsCache.ContainsKey(favObj.Msisdn))
                    {
                        c = App.ViewModel.ContactsCache[favObj.Msisdn];
                        App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, c);
                    }
                    else if (favObj.IsOnhike)
                    {
                        App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, msisdn);
                    }
                }
            }
            if (gridAddFriendStrip.Visibility == Visibility.Visible)
                gridAddFriendStrip.Visibility = Visibility.Collapsed;
            else
            {
                btn.Visibility = Visibility.Collapsed;
            }
            isInvited = true;

            if (toggleToInvitedScreen)//do not change ui if sms user or if status are shown
                ShowRequestSent();
            else if (!isStatusLoaded && isOnHike)
            {
                LoadStatuses();
            }
        }

        private void GoToChat_Tap(object sender, EventArgs e)
        {
            ConversationListObject co = Utils.GetConvlistObj(msisdn);
            if (co != null)
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = co;
            else
            {
                ContactInfo contactInfo = null;
                if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                    contactInfo = App.ViewModel.ContactsCache[msisdn];
                else
                {
                    contactInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    if (contactInfo != null)
                        App.ViewModel.ContactsCache[msisdn] = contactInfo;
                }
                if (contactInfo == null)
                {
                    contactInfo = new ContactInfo();
                    contactInfo.Msisdn = msisdn;
                }
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = contactInfo;
            }
            int count = 0;
            IEnumerable<JournalEntry> entries = NavigationService.BackStack;
            foreach (JournalEntry entry in entries)
            {
                count++;
            }
            if (count == 3) // case when userprofile is opened from Group Info page
            {
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry(); // this will remove groupinfo
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry(); // this will remove chat thread
            }
            Debug.WriteLine(count);
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void AddStatus_Tap(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/PostStatus.xaml", UriKind.Relative));
        }

        private void EditProfile_Tap(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.EDIT_PROFILE);
            NavigationService.Navigate(new Uri("/View/EditProfile.xaml", UriKind.Relative));
        }

        private void UnblockUser_Tap(object sender, EventArgs e)
        {
            App.ViewModel.BlockedHashset.Remove(msisdn);
            App.HikePubSubInstance.publish(HikePubSub.UNBLOCK_USER, msisdn);
            addToFavBtn.Visibility = Visibility.Collapsed;
            addToFavBtn.Tap -= UnblockUser_Tap;
            txtOnHikeSmsTime.Visibility = Visibility.Visible;

            if (appBar != null)
                appBar.IsVisible = true;

            isBlocked = false;

            if (!isOnHike)
                ShowNonHikeUser();
            else
            {
                txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                InitHikeUserProfile();
            }

            LoadCallCopyOptions();
        }

        #endregion

        private void InitAppBar()
        {
            appBar = new ApplicationBar()
            {
                ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
                Opacity = 0.95
            };

            appBar.StateChanged -= appBar_StateChanged;
            appBar.StateChanged += appBar_StateChanged;

            ApplicationBar = appBar;
        }

        private void InitSelfProfile()
        {
            InitAppBar();

            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
            photoChooserTask.Completed += photoChooserTask_Completed;

            profileImage = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
            msisdn = App.MSISDN;

            string name;
            App.appSettings.TryGetValue(App.ACCOUNT_NAME, out name);

            if (name != null)
            {
                nameToShow = name;
                firstName = Utils.GetFirstName(nameToShow);
            }

            isOnHike = true;
            txtOnHikeSmsTime.Visibility = Visibility.Visible;

            ApplicationBarIconButton postStatusButton = new ApplicationBarIconButton();
            postStatusButton.IconUri = new Uri("/View/images/AppBar/icon_status.png", UriKind.Relative);
            postStatusButton.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusButton.Click += AddStatus_Tap;
            appBar.Buttons.Add(postStatusButton);

            editProfileAppBarButton = new ApplicationBarIconButton();
            editProfileAppBarButton.IconUri = new Uri("/View/images/AppBar/icon_edit.png", UriKind.Relative);
            editProfileAppBarButton.Text = AppResources.Edit_AppBar_Txt;
            editProfileAppBarButton.Click += EditProfile_Tap;
            appBar.Buttons.Add(editProfileAppBarButton);

            changePhotoAppBarButton = new ApplicationBarIconButton();
            changePhotoAppBarButton.IconUri = new Uri("/View/images/AppBar/icon_camera.png", UriKind.Relative);
            changePhotoAppBarButton.Text = AppResources.ChangePic_AppBar_Txt;
            changePhotoAppBarButton.Click += changePhotoAppBarButton_Click;
            appBar.Buttons.Add(changePhotoAppBarButton);
        }

        void changePhotoAppBarButton_Click(object sender, EventArgs e)
        {
            try
            {
                photoChooserTask.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UserProfile.xaml :: changePhotoAppBarButton_Click, Exception : " + ex.StackTrace);
            }
        }

        private void InitHikeUserProfile()
        {
            friendStatus = FriendsTableUtils.GetFriendInfo(msisdn, out timeOfJoin);
            if (timeOfJoin == 0)
                AccountUtils.GetOnhikeDate(msisdn, new AccountUtils.postResponseFunction(GetHikeStatus_Callback));
            else
                txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, TimeUtils.GetOnHikeSinceDisplay(timeOfJoin));

            //handled self profile
            if (App.MSISDN == msisdn)
            {
                LoadStatuses();
                return;
            }

            if (friendStatus != FriendsTableUtils.FriendStatusEnum.FRIENDS)
            {
                switch (friendStatus)
                {
                    #region REQUEST RECIEVED
                    case FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED:
                        ShowRequestRecievedPanel();
                        LoadStatuses();
                        break;
                    #endregion
                    #region UNFRIENDED_BY_YOU OR IGNORED
                    case FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU:
                    case FriendsTableUtils.FriendStatusEnum.IGNORED:
                        LoadStatuses();
                        spAddFriend.Visibility = Visibility.Visible;
                        gridAddFriendStrip.Visibility = Visibility.Visible;
                        break;
                    #endregion
                    #region REQUEST SENT
                    case FriendsTableUtils.FriendStatusEnum.REQUEST_SENT:
                        ShowRequestSent();
                        break;
                    #endregion
                    #region NO ACTION OR UNFRIENDED
                    default:
                        ShowAddAsFriends();
                        break;

                    #endregion
                }
            }
            else
                LoadStatuses();

        }

        public void GetHikeStatus_Callback(JObject obj)
        {
            if (obj != null && HikeConstants.FAIL != (string)obj[HikeConstants.STAT])
            {
                var isOnHike = (bool)obj["onhike"];
                if (isOnHike)
                {
                    JObject j = (JObject)obj["profile"];
                    long time = (long)j["jointime"];
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, TimeUtils.GetOnHikeSinceDisplay(time));
                    });
                    FriendsTableUtils.SetJoiningTime(msisdn, time);

                    ContactUtils.UpdateGroupCacheWithContactOnHike(msisdn, true);
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        txtOnHikeSmsTime.Text = string.Format(AppResources.OnSms_Txt);
                    });
                }
            }
            // ignore if call is failed
        }

        #region CONTROL UI ON FRIENDSHIP BASIS , UI THREAD ONLY

        private void ShowAddToContacts()
        {
            if (addToContactsAppBarButton == null)
            {
                addToContactsAppBarButton = new ApplicationBarIconButton()
                {
                    Text = AppResources.UserProfile_AddToContacts_Btn,
                    IconUri = new Uri("/view/images/AppBar/appbar_addfriend.png", UriKind.Relative)
                };

                addToContactsAppBarButton.Click += AddUserToContacts_Click;
            }

            if (ApplicationBar == null)
            {
                ApplicationBar = new ApplicationBar()
                   {
                       ForegroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarForeground"]).Color,
                       BackgroundColor = ((SolidColorBrush)App.Current.Resources["ConversationAppBarBackground"]).Color,
                   };
            }

            if (!ApplicationBar.Buttons.Contains(addToContactsAppBarButton))
                ApplicationBar.Buttons.Add(addToContactsAppBarButton);
        }

        private void ShowRequestSent()
        {
            imgInviteLock.Source = UI_Utils.Instance.UserProfileLockImage;
            txtSmsUserNameBlk.Text = AppResources.Profile_RequestSent_Blk1;
            btnInvite.Visibility = Visibility.Collapsed;
            addToFavBtn.Visibility = Visibility.Collapsed;

            if (!isInAddressBook)
                ShowAddToContacts();
        }

        private void ShowAddAsFriends()
        {
            imgInviteLock.Source = UI_Utils.Instance.UserProfileLockImage;
            imgInviteLock.Visibility = Visibility.Visible;

            txtSmsUserNameBlk.Text = AppResources.ProfileToBeFriendBlk1;
            btnInvite.Content = AppResources.Add_To_Fav_Txt;
            btnInvite.Tap += AddAsFriend_Tap;
            btnInvite.Visibility = Visibility.Visible;
            isInvited = false;//resetting so that if not now can be clicked again
            toggleToInvitedScreen = true;
            gridSmsUser.Visibility = Visibility.Visible;
            gridAddFriendStrip.Visibility = Visibility.Collapsed;
            addToFavBtn.Visibility = Visibility.Collapsed;

            if (!isInAddressBook)
                ShowAddToContacts();
        }

        private void ShowRequestRecievedPanel()
        {
            spAddFriendInvite.Visibility = Visibility.Visible;
            txtAddedYouAsFriend.Text = firstName;
            gridAddFriendStrip.Visibility = Visibility.Visible;
            spAddFriend.Visibility = Visibility.Collapsed;
        }

        private void ShowEmptyStatus()
        {
            if (msisdn == App.MSISDN)
                txtSmsUserNameBlk.Text = AppResources.Profile_You_NoStatus_Txt;
            else
                txtSmsUserNameBlk.Text = String.Format(AppResources.Profile_NoStatus_Txt, firstName);

            btnInvite.Visibility = Visibility.Collapsed;
            imgInviteLock.Source = null;//left null so that it occupies blank space
            imgInviteLock.Visibility = Visibility.Visible;
            gridSmsUser.Visibility = Visibility.Visible;
        }

        private void ShowNonHikeUser()
        {
            imgInviteLock.Source = UI_Utils.Instance.UserProfileInviteImage;
            imgInviteLock.Visibility = Visibility.Visible;
            txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
            txtSmsUserNameBlk.Text = String.Format(AppResources.InviteOnHike_Txt, firstName);
            btnInvite.Tap += Invite_Tap;
            btnInvite.Content = AppResources.InviteOnHikeBtn_Txt;
            btnInvite.Visibility = Visibility.Visible;

            if (!App.ViewModel.Isfavourite(msisdn))
            {
                addToFavBtn.Visibility = Visibility.Visible;
                addToFavBtn.Content = AppResources.Add_To_Fav_Txt;
                addToFavBtn.Tap += AddAsFriend_Tap;
            }
        }

        private void ShowBlockedUser()
        {
            imgInviteLock.Source = UI_Utils.Instance.UserProfileLockImage;
            txtSmsUserNameBlk.Text = String.Format(AppResources.Profile_BlockedUser_Blk1, firstName);
            txtOnHikeSmsTime.Visibility = Visibility.Collapsed;
            addToFavBtn.Content = AppResources.UnBlock_Txt;
            addToFavBtn.Visibility = Visibility.Visible;
            addToFavBtn.Tap += UnblockUser_Tap;
            btnInvite.Visibility = Visibility.Collapsed;
            gridSmsUser.Visibility = Visibility.Visible;
            gridAddFriendStrip.Visibility = Visibility.Collapsed;
        }

        #endregion

        private void Yes_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            if (!isStatusLoaded)
                LoadStatuses();
            if (App.ViewModel.Isfavourite(msisdn)) // if already favourite just ignore
                return;

            ConversationListObject cObj = null;
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                cObj = App.ViewModel.ConvMap[msisdn];
                cObj.IsFav = true;
            }
            else
            {
                ContactInfo cn = null;
                if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                    cn = App.ViewModel.ContactsCache[msisdn];
                else
                {
                    cn = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    App.ViewModel.ContactsCache[msisdn] = cn;
                }
                bool onHike = cn != null ? cn.OnHike : true; // by default only hiek user can send you friend request
                cObj = new ConversationListObject(msisdn, nameToShow, onHike, MiscDBUtil.getThumbNailForMsisdn(msisdn));
            }

            App.ViewModel.FavList.Insert(0, cObj);
            if (cObj.IsOnhike)
            {
                ContactInfo c = null;
                if (App.ViewModel.ContactsCache.ContainsKey(cObj.Msisdn))
                {
                    c = App.ViewModel.ContactsCache[cObj.Msisdn];
                    App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, c);
                }
                else if (cObj.IsOnhike)
                {
                    App.HikePubSubInstance.publish(HikePubSub.ADD_FRIENDS, cObj.Msisdn);
                }
            }
            App.ViewModel.PendingRequests.Remove(cObj.Msisdn);
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            MiscDBUtil.SaveFavourites();
            MiscDBUtil.SaveFavourites(cObj);
            MiscDBUtil.SavePendingRequests();
            int count = 0;
            App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

            App.ViewModel.RemoveFrndReqFromTimeline(msisdn, friendStatus);
        }

        private void No_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            spAddFriend.Visibility = Visibility.Visible;
            App.ViewModel.PendingRequests.Remove(msisdn);
            MiscDBUtil.SavePendingRequests();
            App.ViewModel.RemoveFrndReqFromTimeline(msisdn, friendStatus);
        }

        #region ADD USER TO CONTATCS

        ContactInfo contactInfo;

        private void AddUserToContacts_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        }

        private void saveContactTask_Completed(object sender, TaskEventArgs e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    ContactUtils.getContact(msisdn, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));

                    if (ApplicationBar.Buttons.Contains(addToContactsAppBarButton))
                        ApplicationBar.Buttons.Remove(addToContactsAppBarButton);
                    break;
                case TaskResult.Cancel:
                    System.Diagnostics.Debug.WriteLine(AppResources.User_Cancelled_Task_Txt);
                    break;
                case TaskResult.None:
                    System.Diagnostics.Debug.WriteLine(AppResources.NoInfoForTask_Txt);
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
            App.HikePubSubInstance.publish(HikePubSub.CONTACT_ADDED, contactInfo);

            nameToShow = contactInfo.Name;

            Dispatcher.BeginInvoke(() =>
            {
                txtUserName.Text = nameToShow;
                firstName = Utils.GetFirstName(nameToShow);
                isOnHike = contactInfo.OnHike;

                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                    App.ViewModel.ConvMap[msisdn].ContactName = contactInfo.Name;
                else
                {
                    ConversationListObject co = App.ViewModel.GetFav(msisdn);
                    if (co != null)
                        co.ContactName = contactInfo.Name;
                }

                if (App.newChatThreadPage != null && _groupParticipantObject == null)
                    App.newChatThreadPage.userName.Text = nameToShow;

                MessageBox.Show(AppResources.CONTACT_SAVED_SUCCESSFULLY);

                if (friendStatus >= FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED)
                {
                    addToFavBtn.Visibility = Visibility.Collapsed;
                    isStatusLoaded = true;
                }

                isInAddressBook = true;

                if (ApplicationBar.Buttons.Contains(addToContactsAppBarButton))
                    ApplicationBar.Buttons.Remove(addToContactsAppBarButton);

                if (txtMsisdn.Visibility == Visibility.Collapsed && txtMsisdn.Text != txtUserName.Text)
                    txtMsisdn.Visibility = Visibility.Visible;

                if (App.newChatThreadPage != null && App.newChatThreadPage.ApplicationBar.MenuItems != null && App.newChatThreadPage.ApplicationBar.MenuItems.Contains(App.newChatThreadPage.addUserMenuItem))
                    App.newChatThreadPage.ApplicationBar.MenuItems.Remove(App.newChatThreadPage.addUserMenuItem);
            });

            ContactUtils.UpdateGroupCacheWithContactName(msisdn, nameToShow);
        }
        #endregion

        private bool CheckUserInAddressBook()
        {
            bool inAddressBook = false;
            ConversationListObject convObj;
            ContactInfo cinfo;
            if (App.ViewModel.ConvMap.TryGetValue(msisdn, out convObj) && (convObj.ContactName != null))
            {
                inAddressBook = true;
            }
            else if (App.ViewModel.ContactsCache.TryGetValue(msisdn, out cinfo) && cinfo.Name != null)
            {
                inAddressBook = true;
            }
            else if (UsersTableUtils.getContactInfoFromMSISDN(msisdn) != null)
            {
                inAddressBook = true;
            }
            return inAddressBook;
        }

        private void GestureListener_Tap(object sender, GestureEventArgs e)
        {
            TextBlock textBlock = sender as TextBlock;
            ContextMenu contextMenu = ContextMenuService.GetContextMenu(textBlock);

            if (contextMenu != null)
                contextMenu.IsOpen = true;
        }

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;

            contextMenu.ClearValue(FrameworkElement.DataContextProperty);
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            BaseStatusUpdate update = ((sender as MenuItem).DataContext as BaseStatusUpdate);
            if (update != null)
            {
                StatusUpdateHelper.Instance.DeleteMyStatus(update);
            }
        }

        void Hyperlink_Clicked(object sender, EventArgs e)
        {
            App.ViewModel.Hyperlink_Clicked(sender as object[]);
        }

        void ViewMoreMessage_Clicked(object sender, EventArgs e)
        {
            App.ViewModel.ViewMoreMessage_Clicked(sender);
        }

        private void StatusLls_ItemRealised(object sender, ItemRealizationEventArgs e)
        {
            if (isStatusLoaded && statusLLS.ItemsSource != null && statusLLS.ItemsSource.Count > 0 && hasMoreStatus)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as BaseStatusUpdate).Equals(statusLLS.ItemsSource[statusLLS.ItemsSource.Count - 1]))
                    {
                        List<StatusMessage> statusMessagesFromDB = null;
                        shellProgress.IsIndeterminate = true;
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += (s1, ev1) =>
                        {
                            statusMessagesFromDB = StatusMsgsTable.GetPaginatedStatusMsgsForMsisdn(msisdn, lastStatusId, HikeConstants.STATUS_SUBSEQUENT_FETCH_COUNT);
                        };
                        bw.RunWorkerAsync();
                        bw.RunWorkerCompleted += (s1, ev1) =>
                        {
                            AddStatusToList(statusMessagesFromDB, HikeConstants.STATUS_SUBSEQUENT_FETCH_COUNT);
                            shellProgress.IsIndeterminate = false;
                        };
                    }
                }
            }
        }

        bool _dontOpenPic = false;
        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            _dontOpenPic = true;
        }
    }
}