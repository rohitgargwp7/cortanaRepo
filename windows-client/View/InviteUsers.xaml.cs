using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;

namespace windows_client.View
{
    public partial class InviteUsers : PhoneApplicationPage
    {
        private Dictionary<ContactInfo, bool> invitedUsers = new Dictionary<ContactInfo, bool>();

        public InviteUsers()
        {
            InitializeComponent();
            inviteListBox.ItemsSource = UsersTableUtils.getAllContactsToInvite();
        }

        private void inviteListBox_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // UPDATE THE UI TO SHOW THIS IS ALREADY INVITED
            ContactInfo obj = inviteListBox.SelectedItem as ContactInfo;
            if (obj == null || obj.OnHike || invitedUsers.ContainsKey(obj))
                return;
            invitedUsers.Add(obj, true);
            long time = utils.TimeUtils.getCurrentTimeStamp();
            ConvMessage convMessage = new ConvMessage(App.invite_message,obj.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsInvite=true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize());
        }
    }
}