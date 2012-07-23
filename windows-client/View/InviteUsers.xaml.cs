using System.Collections.Generic;
using Microsoft.Phone.Controls;
using windows_client.DbUtils;
using windows_client.Model;
using System.Windows.Controls;

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

        private void inviteButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            ContactInfo obj = (sender as Button).DataContext as ContactInfo;
            if (obj.IsInvited)
                return;
            obj.IsInvited = true;
            if (obj == null || obj.OnHike || invitedUsers.ContainsKey(obj))
                return;
            invitedUsers.Add(obj, true);
            long time = utils.TimeUtils.getCurrentTimeStamp();
            ConvMessage convMessage = new ConvMessage(App.invite_message,obj.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsInvite=true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
        }
    }
}