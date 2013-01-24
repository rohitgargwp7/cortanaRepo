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

namespace windows_client.View
{
    public partial class UserProfile : PhoneApplicationPage, HikePubSub.Listener
    {
        private string msisdn;
        private HikePubSub mPubSub;

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
                    nameToShow = contactInfo.Msisdn;
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
            else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE, out objMsisdn))
            {
                msisdn = objMsisdn as string;
                nameToShow = App.ACCOUNT_NAME;
                isOwnProfile = true;
                isOnHike = true;
                isFriend = true;
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
                chatIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
                chatIconButton.Text = AppResources.Send_Txt;
                chatIconButton.Click += new EventHandler(GoToChat_Tap);
                chatIconButton.IsEnabled = true;
                this.ApplicationBar.Buttons.Add(chatIconButton);
            }
            if (!isOnHike)
            {
                txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
                gridOnlineBusyIcon.Visibility = Visibility.Collapsed;
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
                    List<StatusMessage> listMsgs = StatusMsgsTable.GetStatusMsgsForMsisdn(msisdn);
                    List<StatusUpdateBox> listStatus = new List<StatusUpdateBox>();
                    foreach (StatusMessage stsMessage in listMsgs)
                    {
                        listStatus.Add(new StatusUpdateBox());//todo:change
                    }
                    listBoxStatus.ItemsSource = listStatus;
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
                    //todo:in grp remove tapping of self
                }
                //todo:change color on Basis of Availability

            }

        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

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
            ConversationListObject co;
            App.ViewModel.ConvMap.TryGetValue(msisdn, out co);
            PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_USERPROFILE_PAGE] = co;
            string uri = "/View/NewChatThread.xaml";
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }
    }
}