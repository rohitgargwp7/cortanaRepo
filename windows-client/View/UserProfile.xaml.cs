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
                isFirstLoad = false;
                bool isFriend = false;
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
                        msisdn = sb.Msisdn;
                        profileImage = sb.UserImage;
                        nameToShow = sb.Name;
                        isOnHike = true;//check as it can be false also
                        isFriend = true;
                        InitChatIconBtn();
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
                    btnInvite.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(Invite_Tap);
                }
                else
                {
                    txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                    loadStatuses();
                    //todo:do on basis of friend invite
                    if (statusList.Count > 0 || App.MSISDN == msisdn)
                    {
                        gridSmsUser.Visibility = Visibility.Collapsed;

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

        private void statusBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StatusUpdateBox stsBox = statusLLS.SelectedItem as StatusUpdateBox;
            if (stsBox == null)
                return;

            if (stsBox.Msisdn == App.MSISDN)
                return;

            ConversationListObject co = Utils.GetConvlistObj(msisdn);
            if (co != null)
                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_STATUSPAGE] = co;
            else
            {
                ContactInfo contactInfo = null;
                if (App.ViewModel.ContactsCache.ContainsKey(stsBox.Msisdn))
                    contactInfo = App.ViewModel.ContactsCache[stsBox.Msisdn];
                else
                {
                    contactInfo = UsersTableUtils.getContactInfoFromMSISDN(stsBox.Msisdn);
                    App.ViewModel.ContactsCache[stsBox.Msisdn] = contactInfo;
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

        #endregion

        #region TAP EVENTS

        private void Invite_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button btn = sender as Button;
            if (!btn.IsEnabled || btn.Content == AppResources.Invited)
                return;
            btn.Content = AppResources.Invited;

            long time = TimeUtils.getCurrentTimeStamp();
            string inviteToken = "";
            //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ConvMessage convMessage = new ConvMessage(string.Format(AppResources.sms_invite_message, inviteToken), msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = true;
            convMessage.IsInvite = true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
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
    }
}