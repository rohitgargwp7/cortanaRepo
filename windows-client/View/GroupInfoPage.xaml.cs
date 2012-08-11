using System.Collections.Generic;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using windows_client.Model;
using windows_client.DbUtils;

namespace windows_client.View
{
    public partial class GroupInfoPage : PhoneApplicationPage
    {
        private List<GroupMembers> activeGroupMembers;

        public GroupInfoPage()
        {
            InitializeComponent();
            initPageBasedOnState();
        }

        private void initPageBasedOnState()
        {
            string groupdId = PhoneApplicationService.Current.State["objFromChatThreadPage"] as string;
            activeGroupMembers = GroupTableUtils.getGroupMembers(groupdId);
            this.groupChatParticipants.ItemsSource = activeGroupMembers;
        }

    }
}