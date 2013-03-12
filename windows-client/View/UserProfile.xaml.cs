﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using System.Windows.Documents;
using Microsoft.Phone.Tasks;
using System.Net.NetworkInformation;
using Newtonsoft.Json.Linq;
using windows_client.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Phone.UserData;

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
        bool toggleToInvitedScreen;
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
        }

        private void removeListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.STATUS_RECEIVED, this);
                App.HikePubSubInstance.removeListener(HikePubSub.STATUS_DELETED, this);
            }
            catch { }
        }

        public void onEventReceived(string type, object obj)
        {
            #region STATUS UPDATE RECEIVED
            if (HikePubSub.STATUS_RECEIVED == type)
            {
                StatusMessage sm = obj as StatusMessage;
                if (sm.Msisdn != msisdn) // only add status msg if user is viewing the profile of the same person whose atatus has been updated
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    statusList.Insert(0, StatusUpdateHelper.Instance.createStatusUIObject(sm, null, null, enlargePic_Tap));
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
                        InitiateForSelfProfile();
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
                    InitiateForSelfProfile();
                }
                #endregion
                #region USER INFO FROM TIMELINE
                else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_TIMELINE, out o))
                {
                    InitAppBar();

                    StatusUpdateBox sb = o as StatusUpdateBox;
                    if (sb != null)
                    {

                        if (sb.Msisdn == App.MSISDN)
                            InitiateForSelfProfile();
                        else
                        {
                            msisdn = sb.Msisdn;
                            profileImage = sb.UserImage;
                            nameToShow = sb.UserName;
                            isOnHike = true;//check as it can be false also
                            InitChatIconBtn();
                        }
                    }
                }
                #endregion

                avatarImage.Source = profileImage;
                avatarImage.Tap += (new EventHandler<System.Windows.Input.GestureEventArgs>(onProfilePicButtonTap));
                txtUserName.Text = nameToShow;

                if (!isOnHike)
                {
                    txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
                    txtSmsUserNameBlk1.Text = nameToShow;
                    gridHikeUser.Visibility = Visibility.Collapsed;
                    btnInvite.Tap += Invite_Tap;
                    if (!App.ViewModel.Isfavourite(msisdn))
                        addToFavBtn.Visibility = Visibility.Visible;
                }
                else
                {
                    txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                    InitiateOnFriendBasis();
                }
                isFirstLoad = false;

            }
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
            //progressBarTop.IsEnabled = true;
            shellProgress.IsVisible = true;
            if (e.TaskResult == TaskResult.OK)
            {
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
                }
                catch { }

                MiscDBUtil.saveStatusImage(App.MSISDN, serverId, fullViewImageBytes);
                StatusMessage sm = new StatusMessage(App.MSISDN, AppResources.PicUpdate_StatusTxt, StatusMessage.StatusType.PROFILE_PIC_UPDATE,
                    serverId, TimeUtils.getCurrentTimeStamp(), -1);
                App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
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

        private void loadStatuses()
        {
            List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
            if (statusMessagesFromDB != null)
            {
                for (int i = 0; i < statusMessagesFromDB.Count; i++)
                {
                    statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], null, null, enlargePic_Tap));
                }
                this.statusLLS.ItemsSource = statusList;
            }
            if (statusList.Count == 0)
            {
                imgInviteLock.Visibility = Visibility.Collapsed;
                txtSmsUserNameBlk1.Text = nameToShow;
                txtSmsUserNameBlk2.Text = AppResources.Profile_NoStatus_Txt;
                txtSmsUserNameBlk3.Text = string.Empty;
                gridHikeUser.Visibility = Visibility.Collapsed;
                btnInvite.Visibility = Visibility.Collapsed;
            }
            else
                gridSmsUser.Visibility = Visibility.Collapsed;
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ImageStatusUpdate imgStUp = statusLLS.SelectedItem as ImageStatusUpdate;
            if (imgStUp == null)
                return;
            PhoneApplicationService.Current.State[HikeConstants.STATUS_IMAGE_TO_DISPLAY] = imgStUp;
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
            if (!btn.IsEnabled)
                return;
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
            if (toggleToInvitedScreen)
                ToggleFriendRequestPending();
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


        #endregion

        private void InitAppBar()
        {
            this.appBar = new ApplicationBar();
            this.appBar.Mode = ApplicationBarMode.Default;
            this.appBar.IsVisible = true;
            this.appBar.IsMenuEnabled = true;
            UserProfilePage.ApplicationBar = appBar;

        }

        private void InitiateForSelfProfile()
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

        private void InitiateOnFriendBasis()
        {
            FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.FriendStatusEnum.NOT_SET;
            if (App.MSISDN != msisdn)
            {
                friendStatus = FriendsTableUtils.GetFriendStatus(msisdn);
            }

            if (friendStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT || App.MSISDN == msisdn)
            {
                bool inAddressBook = true;
                if (App.MSISDN != msisdn && friendStatus != FriendsTableUtils.FriendStatusEnum.FRIENDS)
                {
                    inAddressBook = App.ViewModel.ContactsCache.ContainsKey(msisdn) || UsersTableUtils.getContactInfoFromMSISDN(msisdn) != null;
                }
                if (inAddressBook)
                {
                    loadStatuses();
                }
                else
                {
                    BitmapImage locked = new BitmapImage(new Uri("/View/images/menu_contact_icon.png", UriKind.Relative));
                    imgInviteLock.Source = locked;
                    txtSmsUserNameBlk1.Text = nameToShow;
                    txtSmsUserNameBlk2.Text = AppResources.Profile_NotInAddressbook_Txt;
                    txtSmsUserNameBlk3.Text = string.Empty;
                    gridHikeUser.Visibility = Visibility.Collapsed;
                    btnInvite.Content = AppResources.Profile_AddNow_Btn_Txt;
                    //btnInvite.Tap += addUser_Click; todo:
                }
            }
            else if (friendStatus == FriendsTableUtils.FriendStatusEnum.REQUEST_SENT)
            {
                ToggleFriendRequestPending();
            }
            else
            {
                BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
                imgInviteLock.Source = locked;
                txtSmsUserNameBlk1.Text = AppResources.ProfileToBeFriendBlk1;
                txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
                txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
                txtSmsUserNameBlk2.Text = nameToShow;
                txtSmsUserNameBlk3.Text = AppResources.ProfileToBeFriendBlk3;
                gridHikeUser.Visibility = Visibility.Collapsed;
                btnInvite.Content = AppResources.btnAddAsFriend_Txt;
                btnInvite.Tap += AddAsFriend_Tap;
                toggleToInvitedScreen = true;
            }
            if (friendStatus == FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED)
            {
                spAddFriendInvite.Visibility = Visibility.Visible;
                txtAddedYouAsFriend.Text = string.Format(AppResources.Profile_AddedYouToFav_Txt_WP8FrndStatus, nameToShow);
                seeUpdatesTxtBlk1.Text = string.Format(AppResources.Profile_YouCanNowSeeUpdates, nameToShow);
                gridInvite.Visibility = Visibility.Visible;
            }

            if (friendStatus == FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU)
            {
                spAddFriend.Visibility = Visibility.Visible;
                gridInvite.Visibility = Visibility.Visible;
            }
        }

        private void ToggleFriendRequestPending()
        {
            BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
            imgInviteLock.Source = locked;
            txtSmsUserNameBlk1.Text = AppResources.Profile_RequestSent_Blk1;
            txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
            txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
            txtSmsUserNameBlk2.Text = nameToShow;
            txtSmsUserNameBlk3.Text = AppResources.Profile_RequestSent_Blk3;
            gridHikeUser.Visibility = Visibility.Collapsed;
            btnInvite.Background = UI_Utils.Instance.ButtonGrayBackground;
            btnInvite.Foreground = UI_Utils.Instance.ButtonGrayForeground;
            btnInvite.Content = AppResources.Profile_CancelRequest_BtnTxt;
            //todo: add event

        }
        private void yes_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.FRIENDS);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            RemoveFrndReqFromTimeline();
            if (App.ViewModel.Isfavourite(msisdn)) // if already favourite just ignore
                return;

            ConversationListObject cObj = null;
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                cObj = App.ViewModel.ConvMap[msisdn];
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


        }

        private void no_Click(object sender, System.Windows.Input.GestureEventArgs e)
        {
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.IGNORED);
            spAddFriendInvite.Visibility = Visibility.Collapsed;
            RemoveFrndReqFromTimeline();
            App.ViewModel.PendingRequests.Remove(msisdn);
            MiscDBUtil.SavePendingRequests();
        }

        private void RemoveFrndReqFromTimeline()
        {
            foreach (StatusUpdateBox sb in App.ViewModel.StatusList)
            {
                if ((sb is FriendRequestStatus) && sb.Msisdn == msisdn)
                {
                    App.ViewModel.StatusList.Remove(sb);
                    break;
                }
            }
        }

        //#region ADD USER TO CONTATCS
        //private void addUser_Click(object sender, System.Windows.Input.GestureEventArgs e)
        //{
        //    ContactUtils.saveContact(msisdn, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
        //}

        //private void saveContactTask_Completed(object sender, SaveContactResult e)
        //{
        //    switch (e.TaskResult)
        //    {
        //        case TaskResult.OK:
        //            ContactUtils.getContact(msisdn, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));
        //            break;
        //        case TaskResult.Cancel:
        //            MessageBox.Show(AppResources.User_Cancelled_Task_Txt);
        //            break;
        //        case TaskResult.None:
        //            MessageBox.Show(AppResources.NoInfoForTask_Txt);
        //            break;
        //    }
        //}

        //public void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        //{
        //    try
        //    {
        //        Dictionary<string, List<ContactInfo>> contactListMap = GetContactListMap(e.Results);
        //        if (contactListMap == null)
        //        {
        //            MessageBox.Show(AppResources.NO_CONTACT_SAVED);
        //            return;
        //        }
        //        AccountUtils.updateAddressBook(contactListMap, null, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
        //    }
        //    catch (System.Exception)
        //    {
        //        //That's okay, no results//
        //    }
        //}

        //private Dictionary<string, List<ContactInfo>> GetContactListMap(IEnumerable<Contact> contacts)
        //{
        //    int count = 0;
        //    int duplicates = 0;
        //    Dictionary<string, List<ContactInfo>> contactListMap = null;
        //    if (contacts == null)
        //        return null;
        //    contactListMap = new Dictionary<string, List<ContactInfo>>();
        //    foreach (Contact cn in contacts)
        //    {
        //        CompleteName cName = cn.CompleteName;

        //        foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
        //        {
        //            if (string.IsNullOrWhiteSpace(ph.PhoneNumber)) // if no phone number simply ignore the contact
        //            {
        //                count++;
        //                continue;
        //            }
        //            ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber);
        //            int idd = cInfo.GetHashCode();
        //            cInfo.Id = Convert.ToString(Math.Abs(idd));
        //            if (contactListMap.ContainsKey(cInfo.Id))
        //            {
        //                if (!contactListMap[cInfo.Id].Contains(cInfo))
        //                    contactListMap[cInfo.Id].Add(cInfo);
        //                else
        //                {
        //                    duplicates++;
        //                    Debug.WriteLine("Duplicate Contact !! for Phone Number {0}", cInfo.PhoneNo);
        //                }
        //            }
        //            else
        //            {
        //                List<ContactInfo> contactList = new List<ContactInfo>();
        //                contactList.Add(cInfo);
        //                contactListMap.Add(cInfo.Id, contactList);
        //            }
        //        }
        //    }
        //    Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
        //    Debug.WriteLine("Total contacts with no phone number : {0}", count);
        //    return contactListMap;
        //}

        //public void updateAddressBook_Callback(JObject obj)
        //{
        //    if ((obj == null) || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
        //    {
        //        Dispatcher.BeginInvoke(() =>
        //        {
        //            MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
        //        });
        //        return;
        //    }
        //    JObject addressbook = (JObject)obj["addressbook"];
        //    if (addressbook == null)
        //    {
        //        Dispatcher.BeginInvoke(() =>
        //        {
        //            MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
        //        });
        //        return;
        //    }
        //    IEnumerator<KeyValuePair<string, JToken>> keyVals = addressbook.GetEnumerator();
        //    ContactInfo contactInfo = null;
        //    KeyValuePair<string, JToken> kv;
        //    int count = 0;
        //    while (keyVals.MoveNext())
        //    {
        //        kv = keyVals.Current;
        //        JArray entries = (JArray)addressbook[kv.Key];
        //        for (int i = 0; i < entries.Count; ++i)
        //        {
        //            JObject entry = (JObject)entries[i];
        //            string msisdn = (string)entry["msisdn"];
        //            if (string.IsNullOrWhiteSpace(msisdn))
        //                continue;

        //            bool onhike = (bool)entry["onhike"];
        //            contactInfo.Msisdn = msisdn;
        //            contactInfo.OnHike = onhike;
        //            count++;
        //        }
        //    }
        //    UsersTableUtils.addContact(contactInfo);
        //    Dispatcher.BeginInvoke(() =>
        //    {
        //        nameToShow = contactInfo.Name;
        //        isOnHike = contactInfo.OnHike;
        //        loadStatuses();
        //        MessageBox.Show(AppResources.CONTACT_SAVED_SUCCESSFULLY);
        //    });
        //}
        //#endregion

    }
}