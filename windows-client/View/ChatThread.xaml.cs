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
        private ObservableCollection<ConvMessage> chatThreadPageCollection = new ObservableCollection<ConvMessage>();

        private int mCredits;
        private HikePubSub mPubSub;
        private string mContactNumber;

        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> message mapping

        public Dictionary<long, ConvMessage> MsgMap
        {
            get
            {
                return msgMap;
            }
        }

        public ChatThread()
        {
            InitializeComponent();
            this.myListBox.ItemsSource = chatThreadPageCollection;
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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("fromSelectUserPage"))
            {
                PhoneApplicationService.Current.State.Remove("fromSelectUserPage");
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
            mContactNumber = (string)PhoneApplicationService.Current.State["msisdn"];
            if (mContactNumber == null)
            {
                // some error handling
                return;
            }
            PhoneApplicationService.Current.State.Remove("msisdn");
            loadMessages();
        }

        private void loadMessages()
        {
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                return;
            }

            for (int i = 0; i < messagesList.Count; i++)
            {
                this.ChatThreadPageCollection.Add(messagesList[i]);
                msgMap.Add(messagesList[i].MessageId, messagesList[i]);
            }
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[messagesList.Count - 1]);
            //this.myListBox.UpdateLayout();
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

        private void sendMsgBtn_Click(object sender, RoutedEventArgs e)
        {
            string message = sendMsgTxtbox.Text.Trim();
            if (String.IsNullOrEmpty(message))
            {
                return;
            }
            /* if ((!mConversation.OnHike && mCredits <= 0) || message == "")
             {
                 return;
             }
             */
            sendMsgTxtbox.Text = "";
            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            this.ChatThreadPageCollection.Add(convMessage);
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[ChatThreadPageCollection.Count - 1]);
            sendMessage(convMessage);
        }

        private void sendMessage(ConvMessage convMessage)
        {
            mPubSub.publish(HikePubSub.SEND_NEW_MSG, convMessage);
            if (sendMsgTxtbox.Text != "")
                sendMsgBtn.IsEnabled = true;
        }

        private void blockUnblockUser_Click(object sender, EventArgs e)
        {
            ConvMessage c = ChatThreadPageCollection[ChatThreadPageCollection.Count - 1];
        }

        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrEmpty(sendMsgTxtbox.Text.Trim()))
            {
                sendMsgBtn.IsEnabled = false;
                return;
            }
            sendMsgBtn.IsEnabled = true;
        }

        #region Pubsub Event

        /* this function is running on pubsub thread and not UI thread*/
        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                /* Check if this is the same user for which this message is recieved*/
                if (convMessage.Msisdn == mContactNumber)
                {
                    // Update UI
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.ChatThreadPageCollection.Add(convMessage);
                        this.myListBox.UpdateLayout();
                        this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                    });
                }
            }
            else if (HikePubSub.SERVER_RECEIVED_MSG == type)
            {
                long msgId = (long)obj;
                ConvMessage msg = msgMap[msgId];
                if (msg != null)
                {
                    msg.MessageStatus = ConvMessage.State.SENT_CONFIRMED;
                }
            }
            else if (HikePubSub.MESSAGE_DELIVERED == type)
            {
                long msgId = (long)obj;
                ConvMessage msg = msgMap[msgId];
                if (msg != null)
                {
                    msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.myListBox.UpdateLayout();
                        this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                    });
                
                }
            }
            else if (HikePubSub.MESSAGE_DELIVERED_READ == type)
            {
                long[] ids = (long[])obj;
                // TODO we could keep a map of msgId -> conversation objects somewhere to make this faster
                for (int i = 0; i < ids.Length; i++)
                {
                    ConvMessage msg = msgMap[ids[i]];
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_DELIVERED_READ;
                    }
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.myListBox.UpdateLayout();
                    this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                });
            }
           
        }

        #endregion

        public ObservableCollection<ConvMessage> ChatThreadPageCollection
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