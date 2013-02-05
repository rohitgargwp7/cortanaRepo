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

namespace windows_client.View
{
    public partial class NUX_InviteFriends : PhoneApplicationPage
    {
        private List<ContactInfo> contactsToBeInvited = new List<ContactInfo>();
        private ApplicationBarIconButton sendInviteIconButton;
        private bool isFirstLaunch = true;
        private bool isClicked = true;
       
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
            sendInviteIconButton.Click += new EventHandler(btnInvite_Click);
            sendInviteIconButton.IsEnabled = false;
            appBar.Buttons.Add(sendInviteIconButton);
            this.ApplicationBar = appBar;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (isFirstLaunch)
            {
                isFirstLaunch = false;
                SmileyParser.Instance.initializeSmileyParser();

                List<ContactInfo> listContactInfo;
                if (App.appSettings.TryGetValue(HikeConstants.CLOSE_FRIENDS_NUX, out listContactInfo) && listContactInfo.Count > 2)
                {
                    lstBoxInvite.ItemsSource = listContactInfo;
                }
                else
                {
                    App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                    if (NavigationService != null)
                        NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                }
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
        }

        private void btnInvite_Click(object sender, EventArgs e)
        {
            if (isClicked)
            {
                isClicked = false;
                if (contactsToBeInvited.Count == 0)
                {
                    sendInviteIconButton.IsEnabled = false;
                    return;
                }
                string inviteToken = "";
                int count = 0;

                App.MqttManagerInstance.connect();
                NetworkManager.turnOffNetworkManager = false;
                
                foreach (ContactInfo cinfo in contactsToBeInvited)
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
                if (count > 0)
                {
                    MessageBoxResult msgBoxResult = MessageBox.Show(string.Format(AppResources.InviteUsers_TotalInvitesSent_Txt, count), AppResources.InviteUsers_FriendsInvited_Txt, MessageBoxButton.OK);
                    if (msgBoxResult == MessageBoxResult.OK)
                    {
                        App.RemoveKeyFromAppSettings(HikeConstants.CLOSE_FRIENDS_NUX);
                        App.WriteToIsoStorageSettings(App.PAGE_STATE, App.PageState.CONVLIST_SCREEN);
                        NavigationService.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));
                    }
                }
            }
        }

        private void CheckBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            CheckBox c = sender as CheckBox;
            ContactInfo cn = c.DataContext as ContactInfo;
            if (cn == null)
                return;
            if ((bool)c.IsChecked && !contactsToBeInvited.Contains(cn))
            {
                contactsToBeInvited.Add(cn);
            }
            else if (!(bool)c.IsChecked && contactsToBeInvited.Contains(cn))
            {
                contactsToBeInvited.Remove(cn);
            }
            sendInviteIconButton.IsEnabled = contactsToBeInvited.Count > 0;
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
        }
    }
}