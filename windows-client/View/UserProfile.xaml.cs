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
        private string msisdn;
        private PhotoChooserTask photoChooserTask;
        bool isProfilePicTapped = false;
        BitmapImage profileImage = null;
        byte[] fullViewImageBytes = null;
        byte[] largeImageBytes = null;
        bool isFirstLoad = true;
        bool isOwnProfile = false;
        string nameToShow = null;
        bool isOnHike = false;
        private ObservableCollection<StatusUpdateBox> statusList = new ObservableCollection<StatusUpdateBox>();

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
                if (sm.Msisdn != msisdn) // only add status msg if its from self
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    statusList.Insert(0, StatusUpdateHelper.Instance.createStatusUIObject(profileImage, sm, statusBox_Tap));
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
                isFirstLoad = false;
                bool fromChatThread = false;

                bool isFriend = false;
                Object objMsisdn;

                #region UserInfoFromChatThread
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE, out objMsisdn))
                {
                    Object[] objArray = objMsisdn as Object[];
                    profileImage = objArray[0] as BitmapImage;
                    msisdn = objArray[1] as string;
                    ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    if (contactInfo == null || string.IsNullOrEmpty(contactInfo.Name))
                        nameToShow = msisdn;
                    else
                        nameToShow = contactInfo.Name;

                    if (contactInfo != null)
                        isOnHike = contactInfo.OnHike;
                    else
                    {
                        ConversationListObject convList;
                        if (App.ViewModel.ConvMap.TryGetValue(msisdn, out convList))
                            isOnHike = convList.IsOnhike;
                        else
                            isOnHike = false;
                    }

                    fromChatThread = true;
                    avatarImage.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(UserImage_Tap);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE);
                }
                #endregion
                #region UserInfoFromGroupChat

                else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE, out objMsisdn))
                {
                    Object[] objArray = objMsisdn as Object[];
                    profileImage = objArray[0] as BitmapImage;
                    msisdn = objArray[1] as string;
                    ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    if (contactInfo == null || string.IsNullOrEmpty(contactInfo.Name))
                        nameToShow = msisdn;
                    else
                        nameToShow = contactInfo.Name;

                    if (contactInfo != null)
                        isOnHike = contactInfo.OnHike;
                    else
                    {
                        ConversationListObject convList;
                        if (App.ViewModel.ConvMap.TryGetValue(msisdn, out convList))
                            isOnHike = convList.IsOnhike;
                        else
                            isOnHike = false;
                    }
                    avatarImage.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(UserImage_Tap);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE);
                }
                #endregion
                #region userOwnProfile
                else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_PROFILE, out objMsisdn))
                {
                    Object[] objArray = objMsisdn as Object[];
                    profileImage = objArray[0] as BitmapImage;
                    msisdn = objArray[1] as string;

                    App.appSettings.TryGetValue(App.ACCOUNT_NAME, out nameToShow);
                    changePic.Visibility = Visibility.Visible;

                    this.ApplicationBar = new ApplicationBar();
                    this.ApplicationBar.Mode = ApplicationBarMode.Default;
                    this.ApplicationBar.IsVisible = true;
                    this.ApplicationBar.IsMenuEnabled = true;

                    ApplicationBarIconButton postStatusButton = new ApplicationBarIconButton();
                    postStatusButton.IconUri = new Uri("/View/images/icon_status.png", UriKind.Relative);
                    postStatusButton.Text = AppResources.Conversations_PostStatus_AppBar;
                    postStatusButton.Click += new EventHandler(AddStatus_Tap);
                    postStatusButton.IsEnabled = true;
                    this.ApplicationBar.Buttons.Add(postStatusButton);

                    ApplicationBarIconButton editProfile_button = new ApplicationBarIconButton();
                    editProfile_button.IconUri = new Uri("/View/images/settings.png", UriKind.Relative);
                    editProfile_button.Text = AppResources.Conversations_EditProfile_Txt;
                    editProfile_button.Click += new EventHandler(EditProfile_Tap);
                    editProfile_button.IsEnabled = true;
                    this.ApplicationBar.Buttons.Add(editProfile_button);

                    photoChooserTask = new PhotoChooserTask();
                    photoChooserTask.ShowCamera = true;
                    photoChooserTask.PixelHeight = HikeConstants.PROFILE_PICS_SIZE;
                    photoChooserTask.PixelWidth = HikeConstants.PROFILE_PICS_SIZE;
                    photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

                    avatarImage.Tap += onProfilePicButtonTap;
                    isOwnProfile = true;
                    isOnHike = true;
                    isFriend = true;
                    fromChatThread = true;
                    PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_PROFILE);
                }
                #endregion

                avatarImage.Source = profileImage;

                txtUserName.Text = nameToShow;
                if (!fromChatThread)
                {
                    this.ApplicationBar = new ApplicationBar();
                    this.ApplicationBar.Mode = ApplicationBarMode.Default;
                    this.ApplicationBar.IsVisible = true;
                    this.ApplicationBar.IsMenuEnabled = true;

                    ApplicationBarIconButton chatIconButton = new ApplicationBarIconButton();
                    chatIconButton.IconUri = new Uri("/View/images/icon_message.png", UriKind.Relative);
                    chatIconButton.Text = AppResources.Send_Txt;
                    chatIconButton.Click += new EventHandler(GoToChat_Tap);
                    chatIconButton.IsEnabled = true;
                    this.ApplicationBar.Buttons.Add(chatIconButton);
                }
                if (!isOnHike)
                {
                    txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
                    // gridOnlineBusyIcon.Visibility = Visibility.Collapsed;
                    txtSmsUserNameBlk1.Text = nameToShow;
                    gridHikeUser.Visibility = Visibility.Collapsed;
                }
                else
                {
                    if (isOwnProfile)
                        this.txtProfileHeader.Text = AppResources.MyProfileheader_Txt;

                    txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                    loadStatuses();
                    //todo:do on basis of friend invite
                    if (statusList.Count > 0 || isOwnProfile)
                    {
                        gridSmsUser.Visibility = Visibility.Collapsed;
                        btnInvite.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(Invite_Tap);
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
                }
            }
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        #region CHANGE PROFILE PIC

        private void onProfilePicButtonTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                if (!isProfilePicTapped)
                {
                    photoChooserTask.Show();
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
                MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, AppResources.No_Network_Txt, MessageBoxButton.OK);
                isProfilePicTapped = false;
                return;
            }
            //progressBarTop.IsEnabled = true;
            //   shellProgress.IsVisible = true;
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
                //  shellProgress.IsVisible = false;
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
                //shellProgress.IsVisible = false;
                isProfilePicTapped = false;
            });
        }

        #endregion

        #region STATUS MESSAGES

        private void loadStatuses()
        {
            List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
            if (statusMessagesFromDB == null)
                return;

            for (int i = 0; i < statusMessagesFromDB.Count; i++)
            {
                statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(profileImage, statusMessagesFromDB[i], statusBox_Tap));
            }
            this.statusLLS.ItemsSource = statusList;
        }

        private void statusBox_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            StatusUpdateBox stsBox = statusLLS.SelectedItem as StatusUpdateBox;
            if (stsBox == null)
                return;

            if (stsBox.Msisdn == App.MSISDN)
                return;

            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(stsBox.Msisdn);
            if (contactInfo == null)
            {
                contactInfo = new ContactInfo();
                contactInfo.Msisdn = stsBox.Msisdn;
                contactInfo.OnHike = true;
            }

            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = contactInfo;
            NavigationService.Navigate(new Uri("/View/NewChatThread.xaml", UriKind.Relative));
        }

        #endregion

        #region TAP EVENTS

        private void Invite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.IsEnabled)
                return;
            btn.Content = AppResources.Invited;

            long time = TimeUtils.getCurrentTimeStamp();
            string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ConvMessage convMessage = new ConvMessage(string.Format(AppResources.sms_invite_message, inviteToken), msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = true;
            convMessage.IsInvite = true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(convMessage.IsSms ? false : true));
            // btn.IsEnabled = false;
        }

        private void AddAsFriend_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.IsEnabled)
                return;
            if (msisdn == App.MSISDN)
                return;
            btn.Content = AppResources.Invited;

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
            JObject data = new JObject();
            data["id"] = msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            App.HikePubSubInstance.publish(HikePubSub.ADD_REMOVE_FAV, null);
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_CONTEXT_MENU_GROUP_INFO);

            // btn.IsEnabled = false;
        }

        private void GoToChat_Tap(object sender, EventArgs e)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
            if (contactInfo == null)
            {
                contactInfo = new ContactInfo();
                contactInfo.Msisdn = msisdn;
                ConversationListObject co;
                if (App.ViewModel.ConvMap.TryGetValue(msisdn, out co))
                    contactInfo.OnHike = co.IsOnhike;
            }

            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = contactInfo;
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

        private void UserImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.SEE_LARGE_PROFILE_PIC);
            object[] fileTapped = new object[1];
            fileTapped[0] = msisdn;
            PhoneApplicationService.Current.State["displayProfilePic"] = fileTapped;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
        }
        #endregion
    }
}