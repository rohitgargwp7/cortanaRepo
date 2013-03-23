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
using windows_client.Controls.StatusUpdate;
using Microsoft.Phone.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using windows_client.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Phone.UserData;
using System.ComponentModel;

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
        bool isFirstLoad = true;
        string nameToShow = null;
        bool isOnHike = false;
        private ObservableCollection<StatusUpdateBox> statusList = new ObservableCollection<StatusUpdateBox>();
        private ApplicationBar appBar;
        ApplicationBarIconButton editProfile_button;
        bool isInvited;
        bool isInAddressBook;
        bool toggleToInvitedScreen;
        bool isBlocked;
        FriendsTableUtils.FriendStatusEnum friendStatus;
        private long timeOfJoin;

        public UserProfile()
        {
            InitializeComponent();
            registerListeners();
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
                    statusList.Insert(0, StatusUpdateHelper.Instance.createStatusUIObject(sm, false, null, null, enlargePic_Tap));
                    this.statusLLS.ItemsSource = statusList;
                    gridHikeUser.Visibility = Visibility.Visible;
                    gridSmsUser.Visibility = Visibility.Collapsed;
                });
            }
            #endregion
            #region STATUS_DELETED
            if (HikePubSub.STATUS_DELETED == type)
            {
                StatusUpdateBox sb = obj as StatusUpdateBox;
                if (sb == null)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                  {
                      if (msisdn == App.MSISDN)
                      {
                          statusList.Remove(sb);
                      }
                  });

                //todo:handle ui to show zero status
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
                friendStatus = (FriendsTableUtils.FriendStatusEnum)objArray[1];

                switch (friendStatus)
                {
                    #region TWO WAY FRIENDS
                    case FriendsTableUtils.FriendStatusEnum.FRIENDS:

                        List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (statusMessagesFromDB != null)
                            {
                                for (int i = 0; i < statusMessagesFromDB.Count; i++)
                                {
                                    statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], false, null,
                                        null, enlargePic_Tap));
                                }
                            }
                            if (statusList.Count == 0)
                            {
                                ShowEmptyStatus();
                            }
                            else
                            {
                                gridSmsUser.Visibility = Visibility.Collapsed;
                                gridHikeUser.Visibility = Visibility.Visible;
                            }
                            this.statusLLS.ItemsSource = statusList;
                        });

                        break;
                    #endregion
                    #region REQUEST RECIEVED
                    case FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED:
                        if (isInAddressBook)
                        {
                            statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                if (statusMessagesFromDB != null)
                                {
                                    for (int i = 0; i < statusMessagesFromDB.Count; i++)
                                    {
                                        statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], false, null,
                                            null, enlargePic_Tap));
                                    }
                                }
                                if (statusList.Count == 0)
                                {
                                    ShowEmptyStatus();
                                }
                                else
                                {
                                    gridSmsUser.Visibility = Visibility.Collapsed;
                                    gridHikeUser.Visibility = Visibility.Visible;
                                }
                                this.statusLLS.ItemsSource = statusList;
                            });
                        }
                        else
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                ShowAddToContacts();
                            });
                        }
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ShowRequestRecievedPanel();
                        });
                        break;
                    #endregion
                    #region NO ACTION OR UNFRIENDED
                    default:
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ShowAddAsFriends();
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
                        {
                            AccountUtils.GetOnhikeDate(msisdn, new AccountUtils.postResponseFunction(GetHikeStatus_Callback));
                        }
                        else
                        {
                            txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, TimeUtils.GetOnHikeSinceDisplay(timeOfJoin));
                        }
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isFirstLoad)
            {
                object o;
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
                    GroupParticipant gp = o as GroupParticipant;
                    msisdn = gp.Msisdn;
                    nameToShow = gp.Name;

                    if (App.MSISDN == gp.Msisdn) // represents self page
                    {
                        InitSelfProfile();
                    }
                    else
                    {
                        InitAppBar();
                        profileImage = UI_Utils.Instance.GetBitmapImage(gp.Msisdn);
                        isOnHike = gp.IsOnHike;
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
                avatarImage.Tap += onProfilePicButtonTap;
                txtUserName.Text = nameToShow;

                //if blocked user show block ui and return
                if (msisdn != App.MSISDN && App.ViewModel.BlockedHashset.Contains(msisdn))
                {
                    isBlocked = true;
                    ShowBlockedUser();
                    isFirstLoad = false;
                    if (appBar != null)
                        appBar.IsVisible = false;
                    return;
                }
                if (!isOnHike)//sms user
                {
                    ShowNonHikeUser();
                }
                else
                {
                    InitHikeUserProfile();
                }
                isFirstLoad = false;
            }
            // this is done to update profile name , as soon as it gets updated
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PROFILE_NAME_CHANGED))
                txtUserName.Text = (string)PhoneApplicationService.Current.State[HikeConstants.PROFILE_NAME_CHANGED];
        }

        private void InitChatIconBtn()
        {
            ApplicationBarIconButton chatIconButton = new ApplicationBarIconButton();
            chatIconButton.IconUri = new Uri("/View/images/icon_message.png", UriKind.Relative);
            chatIconButton.Text = AppResources.Send_Txt;
            chatIconButton.Click += new EventHandler(GoToChat_Tap);
            chatIconButton.IsEnabled = true;
            this.appBar.Buttons.Add(chatIconButton);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
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
            try
            {
                if (App.MSISDN == msisdn)
                {
                    if (!isProfilePicTapped)
                    {
                        photoChooserTask.Show();
                        isProfilePicTapped = true;
                    }
                }
                else
                {
                    App.AnalyticsInstance.addEvent(Analytics.SEE_LARGE_PROFILE_PIC_FROM_USERPROFILE);
                    object[] fileTapped = new object[1];
                    fileTapped[0] = msisdn;
                    PhoneApplicationService.Current.State["displayProfilePic"] = fileTapped;
                    NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UserProfile.xaml :: onProfilePicButtonTap, Exception : " + ex.StackTrace);
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
            //progressBarTop.IsEnabled = true;
            shellProgress.IsVisible = true;
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
                shellProgress.IsVisible = false;
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
                    StatusMsgsTable.InsertStatusMsg(sm);
                    App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
                }
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (uploadSuccess)
                {
                    UI_Utils.Instance.BitmapImageCache[HikeConstants.MY_PROFILE_PIC] = profileImage;
                    avatarImage.Source = profileImage;
                    avatarImage.MaxHeight = 83;
                    avatarImage.MaxWidth = 83;
                    object[] vals = new object[3];
                    vals[0] = App.MSISDN;
                    vals[1] = fullViewImageBytes;
                    vals[2] = thumbnailBytes;
                    App.HikePubSubInstance.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                }
                else
                {
                    MessageBox.Show(AppResources.Cannot_Change_Img_Error_Txt, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);
                }
                //progressBarTop.IsEnabled = false;
                shellProgress.IsVisible = false;
                isProfilePicTapped = false;
            });
        }
        #endregion

        #region STATUS MESSAGES

        private void LoadStatuses()
        {
            shellProgress.IsVisible = true;
            List<StatusMessage> statusMessagesFromDB = null;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (ss, ee) =>
            {
                statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
            };
            bw.RunWorkerAsync();
            bw.RunWorkerCompleted += (ss, ee) =>
            {
                if (statusMessagesFromDB != null)
                {
                    for (int i = 0; i < statusMessagesFromDB.Count; i++)
                    {
                        StatusUpdateBox sb = StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], false, null,
                            null, enlargePic_Tap);
                        if (sb != null)
                            statusList.Add(sb);
                    }
                }
                if (statusList.Count == 0)
                {
                    ShowEmptyStatus();
                }
                else
                {
                    gridSmsUser.Visibility = Visibility.Collapsed;
                    gridHikeUser.Visibility = Visibility.Visible;
                }
                this.statusLLS.ItemsSource = statusList;
                shellProgress.IsVisible = false;
            };
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ImageStatusUpdate imgStUp = statusLLS.SelectedItem as ImageStatusUpdate;
            if (imgStUp == null)
                return;
            string[] statusImageInfo = new string[2];
            ImageStatusUpdate statusUpdate = (statusLLS.SelectedItem as ImageStatusUpdate);
            statusImageInfo[0] = statusUpdate.Msisdn;
            statusImageInfo[1] = statusUpdate.serverId;
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

            long time = TimeUtils.getCurrentTimeStamp();
            string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ConvMessage convMessage = new ConvMessage(string.Format(AppResources.sms_invite_message, inviteToken), msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = true;
            convMessage.IsInvite = true;

            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
        }

        private void AddAsFriend_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (msisdn == App.MSISDN)
                return;
            if (isInvited)
                return;

            FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
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
                    App.ViewModel.RemoveFrndReqFromTimeline(msisdn);
                }
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
            btn.Content = AppResources.Invited;
            isInvited = true;
            gridInvite.Visibility = Visibility.Collapsed;

            if (toggleToInvitedScreen)//do not change ui if sms user or if status are shown
                ShowRequestSent();
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
            {
                ShowNonHikeUser();
            }
            else
            {
                txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                InitHikeUserProfile();
            }
        }

        #endregion

        private void InitAppBar()
        {
            this.appBar = new ApplicationBar();
            this.appBar.Mode = ApplicationBarMode.Default;
            this.appBar.IsVisible = true;
            this.appBar.IsMenuEnabled = true;
            UserProfilePage.ApplicationBar = appBar;
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
                nameToShow = name;
            isOnHike = true;
            changePic.Visibility = Visibility.Visible;
            txtOnHikeSmsTime.Visibility = Visibility.Visible;
            this.txtProfileHeader.Text = AppResources.MyProfileheader_Txt;
            ApplicationBarIconButton postStatusButton = new ApplicationBarIconButton();
            postStatusButton.IconUri = new Uri("/View/images/icon_status.png", UriKind.Relative);
            postStatusButton.Text = AppResources.Conversations_PostStatus_AppBar;
            postStatusButton.Click += new EventHandler(AddStatus_Tap);
            postStatusButton.IsEnabled = true;
            this.appBar.Buttons.Add(postStatusButton);

            editProfile_button = new ApplicationBarIconButton();
            editProfile_button.IconUri = new Uri("/View/images/icon_editprofile.png", UriKind.Relative);
            editProfile_button.Text = AppResources.Conversations_EditProfile_Txt;
            editProfile_button.Click += new EventHandler(EditProfile_Tap);
            editProfile_button.IsEnabled = true;
            this.appBar.Buttons.Add(editProfile_button);
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
                BackgroundWorker bw = new BackgroundWorker();
                isInAddressBook = false;
                bw.DoWork += (ss, ee) =>
                {
                    isInAddressBook = UsersTableUtils.getContactInfoFromMSISDN(msisdn) != null;
                };
                bw.RunWorkerAsync();
                bw.RunWorkerCompleted += (ss, ee) =>
                {
                    switch (friendStatus)
                    {
                        #region REQUEST RECIEVED
                        case FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED:
                            ShowRequestRecievedPanel();
                            if (isInAddressBook)
                                LoadStatuses();
                            else
                                ShowAddToContacts();
                            break;
                        #endregion
                        #region UNFRIENDED_BY_YOU
                        case FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU:
                            if (isInAddressBook)
                            {
                                spAddFriend.Visibility = Visibility.Visible;
                                gridInvite.Visibility = Visibility.Visible;
                                LoadStatuses();
                            }
                            else
                                ShowAddToContacts();
                            break;
                        #endregion
                        #region IGNORED
                        case FriendsTableUtils.FriendStatusEnum.IGNORED:
                            if (isInAddressBook)
                                LoadStatuses();
                            else
                                ShowAddToContacts();
                            break;
                        #endregion
                        #region REQUEST SENT

                        case FriendsTableUtils.FriendStatusEnum.REQUEST_SENT:
                            if (isInAddressBook)
                                ShowRequestSent();
                            else
                                ShowAddToContacts();
                            break;
                        #endregion
                        #region NO ACTION OR UNFRIENDED

                        default:
                            if (isInAddressBook)
                                ShowAddAsFriends();
                            else
                                ShowAddToContacts();
                            break;

                        #endregion
                    }
                };
            }
            else
                LoadStatuses();

        }

        public void GetHikeStatus_Callback(JObject obj)
        {
            if (obj != null && HikeConstants.FAIL != (string)obj[HikeConstants.STAT])
            {
                JObject j = (JObject)obj["profile"];
                long time = (long)j["jointime"];
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, TimeUtils.GetOnHikeSinceDisplay(time));
                });
                FriendsTableUtils.SetJoiningTime(msisdn, time);
            }
            // ignore if call is failed
        }

        #region CONTROL UI ON FRIENDSHIP BASIS

        private void ShowAddToContacts()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/menu_contact_icon.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            txtSmsUserNameBlk1.Text = nameToShow;
            txtSmsUserNameBlk2.Text = AppResources.Profile_NotInAddressbook_Txt;
            txtSmsUserNameBlk3.Text = string.Empty;
            btnInvite.Content = AppResources.Profile_AddNow_Btn_Txt;
            btnInvite.Tap += addUser_Click;
            btnInvite.Visibility = Visibility.Visible;
            gridSmsUser.Visibility = Visibility.Visible;
            gridInvite.Visibility = Visibility.Collapsed;
            gridHikeUser.Visibility = Visibility.Collapsed;

        }

        private void ShowRequestSent()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            txtSmsUserNameBlk1.Text = AppResources.Profile_RequestSent_Blk1;
            txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
            txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.Text = nameToShow;
            txtSmsUserNameBlk3.Text = AppResources.Profile_RequestSent_Blk3;
            gridHikeUser.Visibility = Visibility.Collapsed;
            btnInvite.Visibility = Visibility.Collapsed;
        }

        private void ShowAddAsFriends()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            imgInviteLock.Visibility = Visibility.Visible;
            txtSmsUserNameBlk1.Text = AppResources.ProfileToBeFriendBlk1;
            txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
            txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.Text = nameToShow;
            txtSmsUserNameBlk3.Text = AppResources.ProfileToBeFriendBlk3;
            btnInvite.Content = AppResources.btnAddAsFriend_Txt;
            btnInvite.Tap += AddAsFriend_Tap;
            btnInvite.Visibility = Visibility.Visible;
            addToFavBtn.Visibility = Visibility.Collapsed;
            isInvited = false;//resetting so that if not now can be clicked again
            toggleToInvitedScreen = true;
            gridSmsUser.Visibility = Visibility.Visible;
            gridInvite.Visibility = Visibility.Collapsed;
            gridHikeUser.Visibility = Visibility.Collapsed;
        }

        private void ShowRequestRecievedPanel()
        {
            spAddFriendInvite.Visibility = Visibility.Visible;
            txtAddedYouAsFriend.Text = string.Format(AppResources.Profile_AddedYouToFav_Txt_WP8FrndStatus, nameToShow);
            seeUpdatesTxtBlk1.Text = string.Format(AppResources.Profile_YouCanNowSeeUpdates, nameToShow);
            gridInvite.Visibility = Visibility.Visible;
        }

        private void ShowEmptyStatus()
        {
            txtSmsUserNameBlk1.Text = nameToShow;
            txtSmsUserNameBlk2.Text = AppResources.Profile_NoStatus_Txt;
            txtSmsUserNameBlk3.Text = string.Empty;
            txtSmsUserNameBlk1.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.FontWeight = FontWeights.Normal;
            gridHikeUser.Visibility = Visibility.Collapsed;
            btnInvite.Visibility = Visibility.Collapsed;
        }

        private void ShowNonHikeUser()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/user_invite.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            imgInviteLock.Visibility = Visibility.Visible;
            txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
            txtSmsUserNameBlk1.Text = nameToShow;
            txtSmsUserNameBlk2.Text = AppResources.InviteOnHike_Txt;
            txtSmsUserNameBlk3.Text = AppResources.InviteOnHikeUpgrade_Txt;
            txtSmsUserNameBlk1.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.FontWeight = FontWeights.Normal;
            gridHikeUser.Visibility = Visibility.Collapsed;
            btnInvite.Tap += Invite_Tap;
            btnInvite.Content = AppResources.InviteOnHikeBtn_Txt;
            btnInvite.Visibility = Visibility.Visible;
            if (!App.ViewModel.Isfavourite(msisdn))
            {
                addToFavBtn.Visibility = Visibility.Visible;
                addToFavBtn.Content = AppResources.btnAddAsFriend_Txt;
                addToFavBtn.Tap += AddAsFriend_Tap;
            }
        }

        private void ShowBlockedUser()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            txtSmsUserNameBlk1.Text = AppResources.Profile_BlockedUser_Blk1;
            txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
            txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.Text = nameToShow;
            txtOnHikeSmsTime.Visibility = Visibility.Collapsed;
            txtSmsUserNameBlk3.Text = AppResources.Profile_BlockedUser_Blk3;
            addToFavBtn.Content = AppResources.UnBlock_Txt;
            addToFavBtn.Visibility = Visibility.Visible;
            addToFavBtn.Tap += UnblockUser_Tap;
            btnInvite.Visibility = Visibility.Collapsed;
            gridSmsUser.Visibility = Visibility.Visible;
            gridInvite.Visibility = Visibility.Collapsed;
            gridHikeUser.Visibility = Visibility.Collapsed;
        }
        #endregion

        private void Yes_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            if (App.ViewModel.Isfavourite(msisdn)) // if already favourite just ignore
                return;

            ConversationListObject cObj = null;
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                cObj = App.ViewModel.ConvMap[msisdn];
                cObj.ConvBoxObj.FavouriteMenuItem.Header = AppResources.RemFromFav_Txt;
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
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);

            App.ViewModel.RemoveFrndReqFromTimeline(msisdn);
        }

        private void No_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.IGNORED);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            App.ViewModel.PendingRequests.Remove(msisdn);
            MiscDBUtil.SavePendingRequests();
            App.ViewModel.RemoveFrndReqFromTimeline(msisdn);
        }

        #region ADD USER TO CONTATCS
        ContactInfo contactInfo;
        private void addUser_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ContactUtils.saveContact(msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        }

        private void saveContactTask_Completed(object sender, SaveContactResult e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    ContactUtils.getContact(msisdn, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));
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
                nameToShow = contactInfo.Name;
                txtUserName.Text = nameToShow;
                txtAddedYouAsFriend.Text = string.Format(AppResources.Profile_AddedYouToFav_Txt_WP8FrndStatus, nameToShow);
                seeUpdatesTxtBlk1.Text = string.Format(AppResources.Profile_YouCanNowSeeUpdates, nameToShow);
                isOnHike = contactInfo.OnHike;
                btnInvite.Tap -= addUser_Click;
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    App.ViewModel.ConvMap[msisdn].ContactName = contactInfo.Name;
                }
                else
                {
                    ConversationListObject co = App.ViewModel.GetFav(msisdn);
                    if (co != null)
                        co.ContactName = contactInfo.Name;
                }
                if (App.newChatThreadPage != null)
                    App.newChatThreadPage.userName.Text = nameToShow;
                MessageBox.Show(AppResources.CONTACT_SAVED_SUCCESSFULLY);


                switch (friendStatus)
                {
                    #region REQUEST RECIEVED
                    case FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED:
                    case FriendsTableUtils.FriendStatusEnum.IGNORED:
                        LoadStatuses();
                        break;
                    #endregion
                    #region UNFRIENDED_BY_YOU
                    case FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU:
                        spAddFriend.Visibility = Visibility.Visible;
                        gridInvite.Visibility = Visibility.Visible;
                        LoadStatuses();
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
            });
        }
        #endregion

    }
}