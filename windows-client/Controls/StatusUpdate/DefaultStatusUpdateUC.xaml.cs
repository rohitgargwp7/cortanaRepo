using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace windows_client.Controls.StatusUpdate
{
    public partial class DefaultStatusUpdateUC : StatusUpdateBox
    {
        public DefaultStatusUpdateUC(string heyText, EventHandler<System.Windows.Input.GestureEventArgs> ViewFriendsButton_Tap, EventHandler<System.Windows.Input.GestureEventArgs> postStatus_Tap)
            : base(string.Empty, null, string.Empty, string.Empty)
        {
            InitializeComponent();

            txtEmptyStatusFriendBlk1.Text = heyText; 

            ViewFriendsButton.Tap += ViewFriendsButton_Tap;
            postStatusButton.Tap += postStatus_Tap;
        }
    }
}
