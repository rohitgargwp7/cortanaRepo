﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;
using WP7Contrib.Collections;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Reactive;
using System.Threading;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        #region CONSTANTS

        private readonly string ON_HIKE_TEXT = "Free Message...";
        private readonly string ON_SMS_TEXT = "SMS Message...";
        private readonly string ZERO_CREDITS_MSG = "0 Free SMS left...";
        private readonly string BLOCK_USER = "BLOCK";
        private readonly string UNBLOCK_USER = "UNBLOCK";

        #endregion
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private ObservableCollection<ConvMessage> chatThreadPageCollection = new ObservableCollection<ConvMessage>();

        private bool mUserIsBlocked;
        private bool isOnHike;
        private int mCredits;
        private HikePubSub mPubSub;
        private string mContactNumber;
        private string mContactName;
        private bool animatedOnce = false;

        private IScheduler scheduler = Scheduler.NewThread;
        private long lastTextChangedTime;
        private bool endTypingSent = true;
        private string lastText = "";

        private ApplicationBar appBar;
        ApplicationBarMenuItem menuItem1;
        ApplicationBarIconButton inviteUsrIconButton = null;

        private readonly SolidColorBrush whiteBackground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private readonly SolidColorBrush blackBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));


        private List<ConvMessage> incomingMessages = new List<ConvMessage>();
        public List<ConvMessage> IncomingMessages
        {
            get
            {
                return incomingMessages;
            }
        }
        /* This map will contain only outgoing messages */
        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> message mapping

        public Dictionary<long, ConvMessage> MsgMap
        {
            get
            {
                return msgMap;
            }
        }

        BackgroundWorker bw = new BackgroundWorker();

        public ChatThread()
        {
            InitializeComponent();
            this.myListBox.ItemsSource = chatThreadPageCollection;
            mPubSub = App.HikePubSubInstance;
            initPageBasedOnState();

            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync();


        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                // Perform a time consuming operation and report progress.
                initBlockUnblockState();
                mCredits = (int)App.appSettings[App.SMS_SETTING];
                loadMessages();
                registerListeners();
            }

        }
        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if ((e.Cancelled == true))
            {

            }

            else if (!(e.Error == null))
            {

            }

            else
            {

            }
        }

        private void initBlockUnblockState()
        {
            mUserIsBlocked = UsersTableUtils.isUserBlocked(mContactNumber);
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (mUserIsBlocked)
                    {
                        //sendMsgBtn.IsEnabled = false;
                        showOverlay(true);
                    }
                    else
                    {
                        //sendMsgBtn.IsEnabled = true;
                        showOverlay(false);
                    }
                });
        }

        #region REGISTER LISTENERS

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);
        }

        #endregion

        #region REMOVE LISTENERS
        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.removeListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.removeListener(HikePubSub.USER_JOINED, this);
            mPubSub.removeListener(HikePubSub.USER_LEFT, this);
            mPubSub.removeListener(HikePubSub.TYPING_CONVERSATION, this);
            mPubSub.removeListener(HikePubSub.END_TYPING_CONVERSATION, this);
            mPubSub.removeListener(HikePubSub.UPDATE_UI, this);
        }
        #endregion

        /* Should run on UI thread, based on mUserIsBlocked*/
        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            //add icon for send
            ApplicationBarIconButton sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/send_button.png", UriKind.Relative);
            sendIconButton.Text = "send";
            sendIconButton.Click += new EventHandler(sendMsgBtn_Click);
            sendIconButton.IsEnabled = true;
            appBar.Buttons.Add(sendIconButton);

            menuItem1 = new ApplicationBarMenuItem();
            if (mUserIsBlocked)
                menuItem1.Text = UNBLOCK_USER;
            else
                menuItem1.Text = BLOCK_USER;
            menuItem1.Click += new EventHandler(blockUnblock_Click);
            appBar.MenuItems.Add(menuItem1);
            ApplicationBarMenuItem menuItem2 = new ApplicationBarMenuItem();
            menuItem2.Text = "add user";
            menuItem2.Click += new EventHandler(addUser_Click);
            appBar.MenuItems.Add(menuItem2);
            chatThreadMainPage.ApplicationBar = appBar;
        }

        private void addUser_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(mContactNumber);
        }

        private void initAppBarIconButton()
        {
            inviteUsrIconButton = new ApplicationBarIconButton();
            inviteUsrIconButton.IconUri = new Uri("/View/images/appbar.favs.addto.rest.png", UriKind.Relative);
            inviteUsrIconButton.Text = "invite";
            inviteUsrIconButton.Click += new EventHandler(inviteUserBtn_Click);
            inviteUsrIconButton.IsEnabled = true;
        }

        private void initPageBasedOnState()
        {
            if (PhoneApplicationService.Current.State.ContainsKey("objFromConversationPage")) // represents chatthread is called from convlist page
            {
                ConversationListObject obj = (ConversationListObject)PhoneApplicationService.Current.State["objFromConversationPage"];
                mContactNumber = obj.Msisdn;
                mContactName = obj.ContactName;
                isOnHike = obj.IsOnhike;
                PhoneApplicationService.Current.State.Remove("objFromConversationPage");
            }
            else if (PhoneApplicationService.Current.State.ContainsKey("objFromSelectUserPage"))
            {
                ContactInfo obj = (ContactInfo)PhoneApplicationService.Current.State["objFromSelectUserPage"];
                mContactNumber = obj.Msisdn;
                mContactName = obj.Name;
                isOnHike = obj.OnHike;

                /* Check if it is a forwarded msg */
                if (PhoneApplicationService.Current.State.ContainsKey("forwardedText"))
                {
                    sendMsgTxtbox.Text = (string)PhoneApplicationService.Current.State["forwardedText"];
                    PhoneApplicationService.Current.State.Remove("forwardedText");
                }
                PhoneApplicationService.Current.State.Remove("objFromSelectUserPage");
            }

            userName.Text = mContactName;
            initAppBar();
            if (!isOnHike)
            {
                sendMsgTxtbox.Hint = ON_SMS_TEXT;
                initAppBarIconButton();
                appBar.Buttons.Add(inviteUsrIconButton);
            }
            else
            {
                sendMsgTxtbox.Hint = ON_HIKE_TEXT;
            }

            if (mContactNumber == null)
            {
                // some error handling
            }

        }

        private void loadMessages()
        {
            int i;
            int limit = 6;
            bool isPublish = false;
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null || messagesList.Count == 0) // represents there are no chat messages for this msisdn
            {
                return;
            }

            JArray ids = new JArray();
            List<long> dbIds = new List<long>();
            for (i = (messagesList.Count - limit >= 0 ? (messagesList.Count - limit) : 0); i < messagesList.Count; i++)
            {
                messagesList[i].IsSms = !isOnHike;
                if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    isPublish = true;
                    ids.Add(Convert.ToString(messagesList[i].MappedMessageId));
                    dbIds.Add(messagesList[i].MessageId);
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                ConvMessage cm = messagesList[i];
                if (messagesList[i].IsSent)
                    msgMap.Add(messagesList[i].MessageId, messagesList[i]);
                else
                    incomingMessages.Add(messagesList[i]);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.ChatThreadPageCollection.Add(cm);
                });

            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   this.myListBox.UpdateLayout();
                   this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
               });
            int count = 0;
            for (i = messagesList.Count - limit - 1; i >= 0; i--)
            {
                count++;
                messagesList[i].IsSms = !isOnHike;
                if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    isPublish = true;
                    ids.Insert(0, Convert.ToString(messagesList[i].MappedMessageId));
                    dbIds.Insert(0, messagesList[i].MessageId);
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                else
                {
                    /* just publish the message */

                    if (isPublish)
                    {
                        JObject obj = new JObject();
                        obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                        obj.Add(HikeConstants.TO, mContactNumber);
                        obj.Add(HikeConstants.DATA, ids);

                        mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, dbIds.ToArray()); // this is to notify DB
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                        mPubSub.publish(HikePubSub.MSG_READ, mContactNumber); // this is to notify ConvList. This can be done on UI directly
                        isPublish = false;
                    }
                }
                ConvMessage c = messagesList[i];
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.ChatThreadPageCollection.Insert(0, c);
                    this.myListBox.UpdateLayout();
                    this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                });
                if (count % 5 == 0)
                    Thread.Sleep(2);
                if (messagesList[i].IsSent)
                    msgMap.Add(messagesList[i].MessageId, messagesList[i]);
                else
                    incomingMessages.Insert(0, messagesList[i]);
            }
            if (isPublish)
            {
                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, mContactNumber);
                obj.Add(HikeConstants.DATA, ids);

                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, dbIds.ToArray()); // this is to notify DB
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                mPubSub.publish(HikePubSub.MSG_READ, mContactNumber); // this is to notify ConvList. This can be done on UI directly
                isPublish = false;
            }
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
        }
        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        private void sendMsgBtn_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked)
                return;
            string message = sendMsgTxtbox.Text.Trim();
            if (String.IsNullOrEmpty(message))
                return;

            if ((!isOnHike && mCredits <= 0) || message == "")
                return;

            sendMsgTxtbox.Text = "";

            endTypingSent = true;
            sendTypingNotification(false);

            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = !isOnHike;
            this.ChatThreadPageCollection.Add(convMessage);
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[ChatThreadPageCollection.Count - 1]);

            mPubSub.publish(HikePubSub.SEND_NEW_MSG, convMessage);
            if (message != "")
            {
            }
        }

        /// <summary>
        /// Sends start and end typing notifications
        /// </summary>
        /// <param name="notificationType">If it is true then send start typing else send end typing</param>
        private void sendTypingNotification(bool notificationType)
        {
            JObject obj = new JObject();
            try
            {
                if (notificationType)
                {
                    obj.Add(HikeConstants.TYPE, NetworkManager.START_TYPING);

                }
                else
                {
                    obj.Add(HikeConstants.TYPE, NetworkManager.END_TYPING);
                }
                obj.Add(HikeConstants.TO, mContactNumber);
            }
            catch (Exception ex)
            {
                //logger.("ConvMessage", "invalid json message", e);
            }
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            //endTypingSent = !notificationType;
        }

        private void sendEndTypingNotification()
        {
            long currentTime = TimeUtils.getCurrentTimeStamp();
            if (currentTime - lastTextChangedTime >= 5 && endTypingSent == false)
            {
                endTypingSent = true;
                sendTypingNotification(false);
            }
        }

        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lastText.Equals(sendMsgTxtbox.Text))
                return;
            if (String.IsNullOrEmpty(sendMsgTxtbox.Text.Trim()))
            {
                return;
            }

            lastText = sendMsgTxtbox.Text;
            lastTextChangedTime = TimeUtils.getCurrentTimeStamp();
            scheduler.Schedule(sendEndTypingNotification, TimeSpan.FromSeconds(5));

            if (endTypingSent)
            {
                endTypingSent = false;
                sendTypingNotification(true);
            }
        }

        private void updateUIForHikeStatus()
        {
            if (isOnHike)
            {
                sendMsgTxtbox.Hint = ON_HIKE_TEXT;
            }
            else
            {
                updateChatMetadata();
                sendMsgTxtbox.Hint = ON_SMS_TEXT;
            }
        }

        private void changeInviteButtonVisibility()
        {
            if (isOnHike)
            {
                if (appBar.Buttons.Contains(inviteUsrIconButton))
                    appBar.Buttons.Remove(inviteUsrIconButton);
            }
            else
            {
                if (inviteUsrIconButton == null)
                    initAppBarIconButton();
                if (!appBar.Buttons.Contains(inviteUsrIconButton))
                    appBar.Buttons.Add(inviteUsrIconButton);
            }
        }

        private void showSMSCounter()
        {

        }

        private void updateChatMetadata()
        {
            //mMetadataNumChars.setVisibility(View.VISIBLE);
            if (mCredits <= 0)
            {
                if (!string.IsNullOrEmpty(sendMsgTxtbox.Text))
                {
                    sendMsgTxtbox.Text = "";
                }
                sendMsgTxtbox.Hint = ZERO_CREDITS_MSG;
                sendMsgTxtbox.IsEnabled = false;
                //SHOW SOME UI EFFECTS
            }
            else
            {
                if (!sendMsgTxtbox.IsEnabled)
                {
                    if (!string.IsNullOrEmpty(sendMsgTxtbox.Text))
                    {
                        sendMsgTxtbox.Text = "";
                    }
                    sendMsgTxtbox.IsEnabled = true;
                }

                // HIDE UI EFFECTS
                // IF BLOCK OVERLAY IS THERE HIDE IT
                // DO OTHER STUFF TODO 
            }
        }

        private void blockUnblock_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked) // UNBLOCK REQUEST
            {
                mPubSub.publish(HikePubSub.UNBLOCK_USER, mContactNumber);
                mUserIsBlocked = false;
                menuItem1.Text = BLOCK_USER;
                showOverlay(false);
                //sendMsgTxtbox.Foreground = "WhiteSmoke";
            }
            else     // BLOCK REQUEST
            {
                mPubSub.publish(HikePubSub.BLOCK_USER, mContactNumber);
                mUserIsBlocked = true;
                menuItem1.Text = UNBLOCK_USER;
                showOverlay(true); //true means show block animation
            }
        }

        private void showOverlay(bool show)
        {
            if (show)
            {
                LayoutRoot.Background = blackBackground;
                HikeTitle.Opacity = 0.25;
                myListBox.Opacity = 0.25;
                bottomPanel.Opacity = 0.25;
                HikeTitle.IsHitTestVisible = false;
                myListBox.IsHitTestVisible = false;
                bottomPanel.IsHitTestVisible = false;
                OverlayMessagePanel.Visibility = Visibility.Visible;
            }
            else
            {
                LayoutRoot.Background = whiteBackground;
                HikeTitle.Opacity = 1;
                myListBox.Opacity = 1;
                bottomPanel.Opacity = 1;
                HikeTitle.IsHitTestVisible = true;
                myListBox.IsHitTestVisible = true;
                bottomPanel.IsHitTestVisible = true;
                OverlayMessagePanel.Visibility = Visibility.Collapsed;

            }

        }

        private void MenuItem_Click_Copy(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;

            if (selectedListBoxItem == null)
            {
                return;
            }
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;
            Clipboard.SetText(msg.Message);
        }

        private void MenuItem_Click_Forward(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;

            if (selectedListBoxItem == null)
            {
                return;
            }
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;
            PhoneApplicationService.Current.State["forwardedText"] = msg.Message;
            NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;

            if (selectedListBoxItem == null)
            {
                return;
            }
            bool delConv = false;
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;

            //update Conversation list class
            this.ChatThreadPageCollection.Remove(msg);
            ConversationListObject obj = ConversationsList.ConvMap[msg.Msisdn];
            /* Remove the message from conversation list */
            if (this.ChatThreadPageCollection.Count > 0)
            {
                obj.LastMessage = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].Message;
            }
            else
            {
                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj);
                delConv = true;
            }
            object[] o = new object[3];
            o[0] = msg.MessageId;
            o[1] = obj.Msisdn;
            o[2] = delConv;
            mPubSub.publish(HikePubSub.MESSAGE_DELETED, o);
        }

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

        private void inviteUserBtn_Click(object sender, EventArgs e)
        {

            if (isOnHike)
                return;

            long time = utils.TimeUtils.getCurrentTimeStamp();
            ConvMessage convMessage = new ConvMessage(App.invite_message, mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsInvite = true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize());
        }

        #region Pubsub Event

        /* this function is running on pubsub thread and not UI thread*/
        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_RECEIVED

            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                /* Check if this is the same user for which this message is recieved*/
                if (convMessage.Msisdn == mContactNumber)
                {
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_READ;
                    mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, new long[] { convMessage.MessageId });
                    mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serializeDeliveryReportRead()); // handle return to sender
                    mPubSub.publish(HikePubSub.MSG_READ, convMessage.Msisdn);

                    // Update UI
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        this.ChatThreadPageCollection.Add(convMessage);
                        this.myListBox.UpdateLayout();
                        this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                        //set typing notification as false
                        typingNotification.Opacity = 0;
                    });
                }
            }

            # endregion

            #region SERVER_RECEIVED_MSG

            else if (HikePubSub.SERVER_RECEIVED_MSG == type)
            {
                long msgId = (long)obj;
                try
                {
                    ConvMessage msg = msgMap[msgId];
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_CONFIRMED;
                    }
                }
                catch (KeyNotFoundException e)
                {
                    logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
                }
            }

            #endregion

            #region MESSAGE_DELIVERED

            else if (HikePubSub.MESSAGE_DELIVERED == type)
            {
                long msgId = (long)obj;
                try
                {
                    ConvMessage msg = msgMap[msgId];
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                    }
                }
                catch (KeyNotFoundException e)
                {
                    logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
                }
            }

            #endregion

            #region MESSAGE_DELIVERED_READ

            else if (HikePubSub.MESSAGE_DELIVERED_READ == type)
            {
                long[] ids = (long[])obj;
                // TODO we could keep a map of msgId -> conversation objects somewhere to make this faster
                for (int i = 0; i < ids.Length; i++)
                {
                    try
                    {
                        ConvMessage msg = msgMap[ids[i]];
                        if (msg != null)
                        {
                            msg.MessageStatus = ConvMessage.State.SENT_DELIVERED_READ;
                        }
                    }
                    catch (KeyNotFoundException e)
                    {
                        logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
                        continue;
                    }

                }
            }

            #endregion

            #region SMS_CREDIT_CHANGED

            else if (HikePubSub.SMS_CREDIT_CHANGED == type)
            {
                mCredits = (int)obj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    updateChatMetadata();
                    if (!animatedOnce)
                    {
                        if (App.appSettings.Contains(HikeConstants.Extras.ANIMATED_ONCE))
                            animatedOnce = (bool)App.appSettings[HikeConstants.Extras.ANIMATED_ONCE];
                        else
                            animatedOnce = false;
                        if (!animatedOnce)
                        {
                            App.appSettings[HikeConstants.Extras.ANIMATED_ONCE] = true;
                            App.appSettings.Save();
                        }
                    }

                    if ((mCredits % 5 == 0 || !animatedOnce) && !isOnHike)
                    {
                        animatedOnce = true;
                        //showSMSCounter();
                    }
                });

            }

            #endregion

            #region USER_LEFT/JOINED

            else if ((HikePubSub.USER_LEFT == type) || (HikePubSub.USER_JOINED == type))
            {
                string msisdn = (string)obj;
                if (mContactNumber != msisdn)
                {
                    return;
                }
                isOnHike = HikePubSub.USER_JOINED == type;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    changeInviteButtonVisibility();
                    updateUIForHikeStatus();
                });
            }

            #endregion

            #region TYPING_CONVERSATION

            else if (HikePubSub.TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        typingNotification.Opacity = 1;

                        //hikeLabel.Text = mContactName;// +" is typing.";
                        // handle auto removing
                    });
                }
            }

            #endregion

            #region END_TYPING_CONVERSATION

            else if (HikePubSub.END_TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        typingNotification.Opacity = 0;
                        //hikeLabel.Text = mContactName;
                    });
                }
            }

            #endregion

            #region UPDATE_UI

            else if (HikePubSub.UPDATE_UI == type)
            {
                for (int i = 0; i < incomingMessages.Count; i++)
                {
                    ConvMessage c = incomingMessages[i];
                    if (!c.IsSent)
                        c.NotifyPropertyChanged("Msisdn");
                }
            }

            #endregion
        }

        #endregion

        private void sendMsgTxtbox_GotFocus(object sender, RoutedEventArgs e)
        {
            sendMsgTxtbox.Background = textBoxBackground;

        }
    }
}