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

namespace windows_client.View
{
    public partial class UserProfile : PhoneApplicationPage, HikePubSub.Listener
    {
        private bool _isFav;
        private string msisdn;
        private PhotoChooserTask photoChooserTask;
        bool isProfilePicTapped = false;
        BitmapImage profileImage = null;
        byte[] fullViewImageBytes = null;
        byte[] largeImageBytes = null;
        bool isFirstLoad = true;
        string nameToShow = null;
        bool isOnHike = false;
        private ObservableCollection<StatusUpdateBox> statusList = new ObservableCollection<StatusUpdateBox>();
        private ApplicationBar appBar;
        ApplicationBarIconButton editProfile_button;

        public UserProfile()
        {
            InitializeComponent();
            registerListeners();
        }

        #region Listeners

        private void registerListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.STATUS_RECEIVED, this);
        }

        private void removeListeners()
        {
            try
            {
                App.HikePubSubInstance.removeListener(HikePubSub.STATUS_RECEIVED, this);
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
                });
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
                    if (App.ViewModel.Isfavourite(msisdn))
                    {
                        //TODO : Rohit set the text here for add to fav button
                        _isFav = true;
                    }
                    else
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
                        largeImageBytes = msLargeImage.ToArray();
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
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (obj != null && HikeConstants.OK == (string)obj[HikeConstants.STAT])
                {
                    UI_Utils.Instance.BitmapImageCache[HikeConstants.MY_PROFILE_PIC] = profileImage;
                    avatarImage.Source = profileImage;
                    avatarImage.MaxHeight = 83;
                    avatarImage.MaxWidth = 83;
                    object[] vals = new object[3];
                    vals[0] = App.MSISDN;
                    vals[1] = fullViewImageBytes;
                    vals[2] = largeImageBytes;
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
            if (statusMessagesFromDB == null)
            {
                this.statusLLS.ItemsSource = statusList;
                return;
            }

            for (int i = 0; i < statusMessagesFromDB.Count; i++)
            {
                statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i], null, null, enlargePic_Tap));
            }
            this.statusLLS.ItemsSource = statusList;
        }

        private void enlargePic_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ImageStatusUpdate imgStUp = statusLLS.SelectedItem as ImageStatusUpdate;
            if (imgStUp == null)
                return;
            PhoneApplicationService.Current.State[HikeConstants.IMAGE_TO_DISPLAY] = imgStUp.StatusImage;
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
            }
            btn.Content = AppResources.Invited;
            btn.IsEnabled = false;
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
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

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
                gridSmsUser.Visibility = Visibility.Collapsed;
                if (inAddressBook)
                {
                    loadStatuses();
                    if (statusList.Count == 0)
                    {
                        gridHikeUser.Visibility = Visibility.Collapsed;
                        msgGrid.Visibility = Visibility.Visible;
                        msgText.Text = "No statuses";
                        //todo:show screen with no msgs
                    }
                }
                else
                {
                    gridHikeUser.Visibility = Visibility.Collapsed;
                    msgGrid.Visibility = Visibility.Visible;
                    msgText.Text = string.Format("Add {0} to contacts", nameToShow); ;
                    //todo:show add to contacts
                }
            }
            else if (friendStatus == FriendsTableUtils.FriendStatusEnum.REQUEST_SENT)
            {
                BitmapImage locked = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
                imgInviteLock.Source = locked;
                txtSmsUserNameBlk1.Text = AppResources.ProfileToBeFriendBlk1;
                txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
                txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
                txtSmsUserNameBlk2.Text = nameToShow;
                txtSmsUserNameBlk3.Text = AppResources.ProfileToBeFriendBlk3;
                gridHikeUser.Visibility = Visibility.Collapsed;
                btnInvite.Content = AppResources.Invited;
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
                btnInvite.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(AddAsFriend_Tap);
            }
            if (friendStatus == FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED)
            {
                spAddFriendInvite.Visibility = Visibility.Visible;
            }

            if (friendStatus == FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU)
            {
                spAddFriend.Visibility = Visibility.Visible;
            }
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

        //private void AddRemoveFavMenuItem_Click(object sender, EventArgs e)
        //{
        //    if (!_isFav) // add to fav
        //    {
        //        ConversationListObject favObj = null;
        //        if (App.ViewModel.ConvMap.ContainsKey(msisdn))
        //        {
        //            favObj = App.ViewModel.ConvMap[msisdn];
        //            favObj.IsFav = true;
        //        }
        //        else
        //        {
        //            favObj = new ConversationListObject(msisdn, nameToShow, isOnHike, UI_Utils.Instance.BitmapImgToByteArray(profileImage));
        //        }
        //        App.ViewModel.FavList.Insert(0, favObj);
        //        MiscDBUtil.SaveFavourites();
        //        MiscDBUtil.SaveFavourites(favObj);
        //        if (App.ViewModel.IsPending(favObj.Msisdn))
        //        {
        //            App.ViewModel.PendingRequests.Remove(favObj.Msisdn);
        //            MiscDBUtil.SavePendingRequests();
        //        }
        //        addToFavBtn.Content = AppResources.RemFromFav_Txt;

        //        App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
        //        JObject data = new JObject();
        //        data["id"] = msisdn;
        //        JObject obj = new JObject();
        //        obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
        //        obj[HikeConstants.DATA] = data;
        //        App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        //        _isFav = true;
        //        int count = 0;
        //        App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
        //        App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
        //        App.AnalyticsInstance.addEvent(Analytics.ADD_TO_FAVS_APP_BAR_CHATTHREAD);
        //    }
        //    else
        //    {
        //        addToFavBtn.Content = AppResources.Add_To_Fav_Txt;
        //        foreach (ConversationListObject cObj in App.ViewModel.FavList)
        //        {
        //            if (cObj.Msisdn == msisdn)
        //            {
        //                App.ViewModel.FavList.Remove(cObj);
        //                break;
        //            }
        //        }
        //        if (App.ViewModel.ConvMap.ContainsKey(msisdn))
        //            App.ViewModel.ConvMap[msisdn].IsFav = false;
        //        MiscDBUtil.SaveFavourites();
        //        MiscDBUtil.DeleteFavourite(msisdn);
        //        App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);

        //        JObject data = new JObject();
        //        data["id"] = msisdn;
        //        JObject obj = new JObject();
        //        obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
        //        obj[HikeConstants.DATA] = data;
        //        App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
        //        _isFav = false;
        //        int count = 0;
        //        App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
        //        App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - 1);
        //        App.AnalyticsInstance.addEvent(Analytics.REMOVE_FAVS_CONTEXT_MENU_CHATTHREAD);
        //    }
        //}
    }
}