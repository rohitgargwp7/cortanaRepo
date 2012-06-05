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
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage , HikePubSub.Listener
    {
        private string mContactNumber;
        private int mCredits;
        private HikePubSub mPubSub;
        private Conversation mConversation;

        public ChatThread()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;
            //mConversation = ConversationDbUtils.getConversation(mContactNumber, 1000);

            /* register listeners */
            App.HikePubSubInstance.addListener(HikePubSub.TYPING_CONVERSATION, this);
            App.HikePubSubInstance.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
            App.HikePubSubInstance.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_FAILED, this);
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.ICON_CHANGED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);        
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.TYPING_CONVERSATION, this);
            App.HikePubSubInstance.removeListener(HikePubSub.END_TYPING_CONVERSATION, this);
            App.HikePubSubInstance.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_DELIVERED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_FAILED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.ICON_CHANGED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
        }

        public void onEventReceived(string type, object obj)
        {
        }




        private void sendMsgBtn_Click(object sender, RoutedEventArgs e)
        {
            string message = sendMsgTxtbox.Text.Trim();
            if ((!mConversation.OnHike && mCredits <= 0) || message == "")
            {
                return;
            }

            sendMsgTxtbox.Text = "";
            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            //convMessage.Conversation = mConversation;
            sendMessage(convMessage);
        }

        private void sendMessage(ConvMessage convMessage)
        {
            /*TODO :: Add this message to observable collection MessageListPageCollection*/
            mPubSub.publish(HikePubSub.MESSAGE_SENT, convMessage);
            if(sendMsgTxtbox.Text != "")
                sendMsgBtn.IsEnabled = true;
        }

        private void blockUnblockUser_Click(object sender, EventArgs e)
        {

        }
    }
}