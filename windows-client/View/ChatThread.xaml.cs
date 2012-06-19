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
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Windows.Controls.Primitives;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        private ObservableCollection<ConvMessage> chatThreadPageCollection = new ObservableCollection<ConvMessage>();

        ConvMessage selectedConvMsg;
        private int mCredits;
        private HikePubSub mPubSub;
        private string mContactNumber;
        private string mContactName;
        private long mTextLastChanged = 0;

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
            mContactName = (string)PhoneApplicationService.Current.State["name"];
            if (mContactNumber == null)
            {
                // some error handling
                return;
            }
            PhoneApplicationService.Current.State.Remove("msisdn");
            PhoneApplicationService.Current.State.Remove("name");
            loadMessages();
        }

        private void loadMessages()
        {
            bool isPublish = false;
            hikeLabel.Text = mContactName;
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                return;
            }

            JArray ids = new JArray();
            long[] dbIds = new long[messagesList.Count];

            for (int i = 0, k = 0; i < messagesList.Count; i++)
            {
                if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    isPublish = true;
                    ids.Add(Convert.ToString(messagesList[i].MessageId));
                    dbIds[k] = messagesList[i].MessageId;
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                    k++;
                }
                this.ChatThreadPageCollection.Add(messagesList[i]);
                msgMap.Add(messagesList[i].MessageId, messagesList[i]);
            }
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[messagesList.Count - 1]);

            if (isPublish)
            {
                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, mContactNumber);
                obj.Add(HikeConstants.DATA, ids);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                mPubSub.publish(HikePubSub.MSG_READ, mContactNumber);
            }
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

        public void test()
        {

        }
        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Action x = test;

            /* Create the typing notification*/
            long lastChanged = TimeUtils.getCurrentTimeStamp();
            if (mTextLastChanged == 0)
            {
                // we're currently not in 'typing' mode
                mTextLastChanged = lastChanged;

                JObject obj = new JObject();
                try
                {
                    obj.Add(HikeConstants.TYPE, NetworkManager.START_TYPING);
                    obj.Add(HikeConstants.TO, mContactNumber);
                }
                catch (Exception ex)
                {
                    //logger.("ConvMessage", "invalid json message", e);
                }

                // fire an event
                mPubSub.publish(HikePubSub.MQTT_PUBLISH_LOW, obj);
            }
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
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_READ;
                    MessagesTableUtils.updateMsgStatus(convMessage.MessageId, (int)ConvMessage.State.RECEIVED_READ);
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serializeDeliveryReportRead()); // handle return to sender
                    mPubSub.publish(HikePubSub.MSG_READ, convMessage.Msisdn);
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

            else if (HikePubSub.END_TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hikeLabel.Text = mContactName;
                        // handle auto removing
                    });
                }
            }
            else if (HikePubSub.TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        hikeLabel.Text = mContactName + " is typing.";
                        // handle auto removing
                    });
                }
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

        private void MenuItem_Click_Copy(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_Forward(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;

            if (selectedListBoxItem == null)
            {
                return;
            }
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;
            MessagesTableUtils.deleteMessage(msg.MessageId);
            //update Conversation list class
            this.ChatThreadPageCollection.Remove(msg);

            ConversationListObject obj = MessageList.ConvMap[msg.Msisdn];
            /* Remove the message from conversation list */
            if (this.ChatThreadPageCollection.Count > 0)
            {               
                obj.LastMessage = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].Message;
            }
            else // no message is left simply remove the object from Conversation list 
            {
                App.ViewModel.MessageListPageCollection.Remove(obj);
            }

            int idx = App.ViewModel.MessageListPageCollection.IndexOf(obj);
            ConversationListObject obj2 = App.ViewModel.MessageListPageCollection[idx];

        }

    }
}