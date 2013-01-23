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

namespace windows_client.View
{
    public partial class UserProfile : PhoneApplicationPage
    {
        private string msisdn;
        public UserProfile()
        {
            InitializeComponent();
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            bool fromChatThread = false;
            Object oo;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE, out oo))
            {
                msisdn = oo as string;
                fromChatThread = true;
                PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE);
            }
            else if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE, out oo))
            {
                msisdn = oo as string;
                PhoneApplicationService.Current.State.Remove(HikeConstants.USERINFO_FROM_GROUPCHAT_PAGE);
            }
            ConversationListObject convList;
            App.ViewModel.ConvMap.TryGetValue(msisdn, out convList);
            byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(convList.Msisdn);

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
                avatarImage.Source = UI_Utils.Instance.getDefaultAvatar((string)App.appSettings[App.MSISDN_SETTING]);
            }
            txtUserName.Text = convList.NameToShow;
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
            if (!convList.IsOnhike)
            {
                txtOnHikeSmsTime.Text = AppResources.OnSms_Txt;
                gridOnlineBusyIcon.Visibility = Visibility.Collapsed;
                txtSmsUserName.Text = convList.NameToShow;
                gridHikeUser.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtOnHikeSmsTime.Text = string.Format(AppResources.OnHIkeSince_Txt, DateTime.Now.ToString("MMM yy"));//todo:change date
                gridSmsUser.Visibility = Visibility.Collapsed;
                //todo:change color
            }
            
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
            btn.IsEnabled = false;
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