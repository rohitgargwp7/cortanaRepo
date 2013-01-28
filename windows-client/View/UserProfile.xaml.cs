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

namespace windows_client.View
{
    public partial class UserProfile : PhoneApplicationPage, HikePubSub.Listener
    {
        private string msisdn;
        private HikePubSub mPubSub;
        private PhotoChooserTask photoChooserTask;
        bool isProfilePicTapped = false;
        BitmapImage profileImage = null;
        byte[] fullViewImageBytes = null;
        byte[] largeImageBytes = null;

        public UserProfile()
        {
            InitializeComponent();
            registerListeners();
        }

        #region Listeners

        private void registerListeners()
        {
            //new sattus
            //user changed from sms to hike
        }

        private void removeListeners()
        {
            try
            {
            }
            catch { }
        }

        public void onEventReceived(string type, object obj)
        {
        }
        #endregion

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool fromChatThread = false;
            string nameToShow = null;
            bool isOnHike = false;
            bool isFriend = false;
            bool isOwnProfile = false;
            Object objMsisdn;

            #region UserInfoFromChatThread
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE, out objMsisdn))
            {
                msisdn = objMsisdn as string;
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
                PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE);
            }
            #endregion
            #region UserInfoFromGroupChat

            else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE, out objMsisdn))
            {
                msisdn = objMsisdn as string;
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
                PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE);
            }
            #endregion
            #region userOwnProfile
            else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_PROFILE, out objMsisdn))
            {
                msisdn = objMsisdn as string;
                App.appSettings.TryGetValue(App.ACCOUNT_NAME, out nameToShow);
                changePic.Visibility = Visibility.Visible;

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

            byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(msisdn);

            if (_avatar != null)
            {
                MemoryStream memStream = new MemoryStream(_avatar);
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                avatarImage.Source = empImage;
            }
            else
            {
                avatarImage.Source = UI_Utils.Instance.getDefaultAvatar(msisdn);
            }
            txtUserName.Text = nameToShow;
            if (!fromChatThread)
            {
                this.ApplicationBar = new ApplicationBar();
                this.ApplicationBar.Mode = ApplicationBarMode.Default;
                this.ApplicationBar.IsVisible = true;
                this.ApplicationBar.IsMenuEnabled = true;
                //add icon for send
                ApplicationBarIconButton chatIconButton = new ApplicationBarIconButton();
                chatIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);//todo:change
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
                if (isFriend)
                {
                   loadStatuses();
                    gridSmsUser.Visibility = Visibility.Collapsed;

                }
                else
                {
                    //todo:add lock image  imgInviteLock.Source=
                    txtSmsUserNameBlk1.Text = AppResources.ProfileToBeFriendBlk1;
                    txtSmsUserNameBlk1.FontWeight = FontWeights.Normal;
                    txtSmsUserNameBlk2.FontWeight = FontWeights.SemiBold;
                    txtSmsUserNameBlk2.Text = nameToShow;
                    txtSmsUserNameBlk3.Text = AppResources.ProfileToBeFriendBlk3;
                    gridHikeUser.Visibility = Visibility.Collapsed;
                    btnInvite.Content = AppResources.btnAddAsFriend_Txt;
                }

            }

        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        #region ChangeProfilePic

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
                Uri uri = new Uri(e.OriginalFileName);
                profileImage = new BitmapImage(uri);
                profileImage.CreateOptions = BitmapCreateOptions.None;
                profileImage.UriSource = uri;
                profileImage.ImageOpened += imageOpenedHandler;
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

        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            BitmapImage image = (BitmapImage)sender;
            WriteableBitmap writeableBitmap = new WriteableBitmap(image);
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
                    mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
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

        #region status messages

        private void yes_Click(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.ADD_FAVS_FROM_FAV_REQUEST);
            ConversationListObject fObj = (sender as Button).DataContext as ConversationListObject;

            if (App.ViewModel.Isfavourite(fObj.Msisdn)) // if already favourite just ignore
                return;

            App.ViewModel.PendingRequests.Remove(fObj);
            App.ViewModel.FavList.Insert(0, fObj);

            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.ADD_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);

            MiscDBUtil.SaveFavourites();
            MiscDBUtil.SaveFavourites(fObj);
            MiscDBUtil.SavePendingRequests();
            int count = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
            //if (emptyListPlaceholder.Visibility == System.Windows.Visibility.Visible)
            //{
            //    emptyListPlaceholder.Visibility = System.Windows.Visibility.Collapsed;
            //    favourites.Visibility = System.Windows.Visibility.Visible;
            //    addFavsPanel.Opacity = 1;
            //}
        }

        private void no_Click(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            ConversationListObject fObj = (sender as Button).DataContext as ConversationListObject;
            JObject data = new JObject();
            data["id"] = fObj.Msisdn;
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE;
            obj[HikeConstants.DATA] = data;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            App.ViewModel.PendingRequests.Remove(fObj);
            MiscDBUtil.SavePendingRequests();
            //if (App.ViewModel.FavList.Count == 0 && App.ViewModel.PendingRequests.Count == 0)
            //{
            //    emptyListPlaceholder.Visibility = System.Windows.Visibility.Visible;
            //    favourites.Visibility = System.Windows.Visibility.Collapsed;
            //    addFavsPanel.Opacity = 0;
            //}
        }

        private ObservableCollection<StatusUpdateBox> statusList = new ObservableCollection<StatusUpdateBox>();
        private void loadStatuses()
        {
            this.statusLLS.ItemsSource = statusList;
            List<StatusMessage> statusMessagesFromDB = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
            if (statusMessagesFromDB == null)
                return;
            for (int i = 0; i < statusMessagesFromDB.Count; i++)
            {
                statusList.Add(StatusUpdateHelper.Instance.createStatusUIObject(statusMessagesFromDB[i],
                    new EventHandler<GestureEventArgs>(yes_Click), new EventHandler<GestureEventArgs>(no_Click)));
            }
        }

        #endregion

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

            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = contactInfo;
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}