using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using windows_client.Languages;
using System.Diagnostics;
using windows_client.Controls;

namespace windows_client.View
{
    public partial class NUX_InviteFriends : PhoneApplicationPage
    {
        private ApplicationBarIconButton sendInviteIconButton;
        private bool isFirstLaunch = true;
        private List<ContactInfo> listContactInfo;
       
        public NUX_InviteFriends()
        {
            InitializeComponent();

            ApplicationBar appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.Opacity = 1;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = false;

            sendInviteIconButton = new ApplicationBarIconButton();
            sendInviteIconButton.IconUri = new Uri("/View/images/icon_tick.png", UriKind.Relative);
            sendInviteIconButton.Text = "Invite";

            appBar.Buttons.Add(sendInviteIconButton);
            this.ApplicationBar = appBar;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isFirstLaunch)
            {
                SmileyParser.Instance.initializeSmileyParser();

                if (App.appSettings.TryGetValue(HikeConstants.CLOSE_FRIENDS_NUX, out listContactInfo) && listContactInfo.Count > 1)
                {
                    listContactInfo.Sort(new ContactCompare());
                    MarkDefaultChecked();
                    lstBoxInvite.ItemsSource = listContactInfo;
                    sendInviteIconButton.Click += btnInviteFriends_Click;
                }
                else if (App.appSettings.TryGetValue(HikeConstants.FAMILY_MEMBERS_NUX, out listContactInfo) && listContactInfo.Count > 1)
                {
                    InitialiseFamilyScreen();
                }
                else
                {
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                    if (NavigationService != null)
                        NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                }
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();

                isFirstLaunch = false;
            }
        }
       
        private void InitialiseFamilyScreen()
        {
            listContactInfo.Sort(new ContactCompare());
            MarkDefaultChecked();
            lstBoxInvite.ItemsSource = listContactInfo;
            txtHeader.Text = AppResources.Nux_YourFamily_Txt;
            txtConnectHike.Text = AppResources.Nux_FamilyMembersConnect_txt;
            sendInviteIconButton.IsEnabled = true;
            sendInviteIconButton.Click += btnInviteFamily_Click;
        }

        private void MarkDefaultChecked()
        {
            if (listContactInfo != null)
            {
                for (int i = 0; i < listContactInfo.Count; i++)
                {
                    if (i == 15)
                        break;
                    ContactInfo cn = listContactInfo[i];
                    cn.IsCloseFriendNux = true;
                }
            }
        }

        private void btnInviteFriends_Click(object sender, EventArgs e)
        {
            sendInviteIconButton.IsEnabled = false;

            string inviteToken = "";
            int count = 0;
            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;
            foreach (ContactInfo cinfo in listContactInfo)
            {
                if (cinfo.IsCloseFriendNux)
                {
                    JObject obj = new JObject();
                    JObject data = new JObject();
                    data[HikeConstants.SMS_MESSAGE] = string.Format(AppResources.sms_invite_message, inviteToken);
                    data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                    data[HikeConstants.MESSAGE_ID] = -1;
                    obj[HikeConstants.TO] = Utils.NormalizeNumber(cinfo.Name);
                    obj[HikeConstants.DATA] = data;
                    obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    count++;
                }
            }
            if (count > 0)
            {
                MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }
            if (App.appSettings.TryGetValue(HikeConstants.FAMILY_MEMBERS_NUX, out listContactInfo) && listContactInfo.Count > 1)
            {
                InitialiseFamilyScreen();
            }
            else
            {
                App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
            }
            sendInviteIconButton.Click -= btnInviteFriends_Click;
        }

        private void btnInviteFamily_Click(object sender, EventArgs e)
        {
            sendInviteIconButton.IsEnabled = false;
            string inviteToken = "";
            int count = 0;

            App.MqttManagerInstance.connect();
            NetworkManager.turnOffNetworkManager = false;

            foreach (ContactInfo cinfo in listContactInfo)
            {
                if (cinfo.IsCloseFriendNux)
                {
                    Debug.WriteLine(cinfo.Name + ":" + cinfo.Msisdn + ",invited number");
                    JObject obj = new JObject();
                    JObject data = new JObject();
                    data[HikeConstants.SMS_MESSAGE] = string.Format(AppResources.sms_invite_message, inviteToken);
                    data[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp();
                    data[HikeConstants.MESSAGE_ID] = -1;
                    obj[HikeConstants.TO] = Utils.NormalizeNumber(cinfo.Name);
                    obj[HikeConstants.DATA] = data;
                    obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    App.MqttManagerInstance.mqttPublishToServer(obj);
                    count++;
                }
            }
            if (count > 0)
            {
                MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
            }

            App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
            NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
        }

    }
}