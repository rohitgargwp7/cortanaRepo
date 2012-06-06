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
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Phone.Shell;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        private ObservableCollection<ChatThreadPage> chatThreadPageCollection = null;

        private bool isNewConversation = false;
        private string mContactNumber;
        private int mCredits;
        private HikePubSub mPubSub;
        private Conversation mConversation;
        private string name;

        public ChatThread()
        {
            InitializeComponent();

            mPubSub = App.HikePubSubInstance;
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

        private void loadMessages()
        {
            //mContactNumber = "+" + mContactNumber.Trim();
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null)
            {
                isNewConversation = true;
                return;
            }
            this.ChatThreadPageCollection = new ObservableCollection<ChatThreadPage>();
            for (int i = 0; i < messagesList.Count; i++)
            {
                this.ChatThreadPageCollection.Add(new ChatThreadPage(messagesList[i].Message));
            }
            this.myListBox.ItemsSource = chatThreadPageCollection;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            MessageListPage obj = (MessageListPage)PhoneApplicationService.Current.State["messageListPageObject"];
            if (obj == null)
            {
                // some error handling
                return;
            }
            mContactNumber = obj.MSISDN;
            name = obj.ContactName;
            /*NavigationContext.QueryString.TryGetValue("msisdn", out mContactNumber);
            NavigationContext.QueryString.TryGetValue("name", out name);*/
            if (mContactNumber == null)
            {
                // move to error page
                return;
            }
            loadMessages();
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
           /* if ((!mConversation.OnHike && mCredits <= 0) || message == "")
            {
                return;
            }
            */
            sendMsgTxtbox.Text = "";
            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            if (this.ChatThreadPageCollection == null)
                this.ChatThreadPageCollection = new ObservableCollection<ChatThreadPage>();
            this.ChatThreadPageCollection.Add(new ChatThreadPage(convMessage.Message));
            //convMessage.Conversation = mConversation;
            sendMessage(convMessage);
        }

        private void sendMessage(ConvMessage convMessage)
        {
            addToMessageList(convMessage);
            object[] vals = new object[2];
            vals[0] = (ConvMessage)convMessage;
            vals[1] = (bool)isNewConversation;
            mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
            if(sendMsgTxtbox.Text != "")
                sendMsgBtn.IsEnabled = true;
        }

        private void blockUnblockUser_Click(object sender, EventArgs e)
        {
            
        }


        public void addToMessageList(ConvMessage conv)
        {
            MessageListPage obj = new MessageListPage(conv.Msisdn, name, conv.Message, TimeUtils.getRelativeTime(TimeUtils.getCurrentTimeStamp()));
            App.ViewModel.MessageListPageCollection.Remove(obj);
            App.ViewModel.MessageListPageCollection.Insert(0, obj);
        }

        public ObservableCollection<ChatThreadPage> ChatThreadPageCollection
        {
            get
            {
                return chatThreadPageCollection;
            }
            set
            {
                chatThreadPageCollection = value;
                NotifyPropertyChanged("ChatThreadPageCollection");
            }
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
    }
}