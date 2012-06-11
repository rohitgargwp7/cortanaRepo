﻿using System;
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

        private int mCredits;
        private HikePubSub mPubSub;
        private string mContactNumber;


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
            this.ChatThreadPageCollection = new ObservableCollection<ChatThreadPage>();
            for (int i = 0; i < messagesList.Count; i++)
            {
                this.ChatThreadPageCollection.Add(new ChatThreadPage(messagesList[i].Message));
            }
            this.myListBox.ItemsSource = chatThreadPageCollection;
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
            if (this.ChatThreadPageCollection == null)
                this.ChatThreadPageCollection = new ObservableCollection<ChatThreadPage>();
            this.ChatThreadPageCollection.Add(new ChatThreadPage(convMessage.Message, "Right"));
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[ChatThreadPageCollection.Count - 1]);
            sendMessage(convMessage);
        }

        private void sendMessage(ConvMessage convMessage)
        {
            mPubSub.publish(HikePubSub.SEND_NEW_MSG, convMessage); // this is to notify DBListener
            if (sendMsgTxtbox.Text != "")
                sendMsgBtn.IsEnabled = true;
        }

        private void blockUnblockUser_Click(object sender, EventArgs e)
        {

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
                /* Check is this is the same user for which this message is recieved*/
                if (convMessage.Msisdn == mContactNumber)
                {
                    // Update UI
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (this.ChatThreadPageCollection == null)
                            this.ChatThreadPageCollection = new ObservableCollection<ChatThreadPage>();
                        this.ChatThreadPageCollection.Add(new ChatThreadPage(convMessage.Message));
                        this.myListBox.UpdateLayout();
                        this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                    });
                }
            }
            else if (HikePubSub.SERVER_RECEIVED_MSG == type)
            {
                //long msgId = (long)obj;
                //ConvMessage msg = findMessageById(msgId);
                //if (msg != null)
                //{
                //    msg.setState(ConvMessage.State.SENT_CONFIRMED);
                //    runOnUiThread(mUpdateAdapter);
                //}
            }
        }

        #endregion

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