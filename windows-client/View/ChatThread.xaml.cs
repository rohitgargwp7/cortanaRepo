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
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Phone.Reactive;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        #region CONSTANTS

        private readonly string ON_HIKE_TEXT = "Free Message...";
        private readonly string ON_SMS_TEXT = "SMS Message...";
        private readonly string ZERO_CREDITS_MSG = "0 Free SMS left...";

        #endregion

        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private ObservableCollection<ConvMessage> chatThreadPageCollection = new ObservableCollection<ConvMessage>();

        private bool mUserIsBlocked;
        private bool isOnHike;
        private int mCredits;
        private HikePubSub mPubSub;
        private string mContactNumber;
        private string mContactName;
        private long mTextLastChanged = 0;
        private bool animatedOnce = false;

        private IScheduler scheduler = Scheduler.NewThread;
        private long lastTextChangedTime;
        private bool endTypingSent = true;

        private ApplicationBar appBar;
        ApplicationBarIconButton inviteUsrIconButton = null;

        private const double LandscapeShift = -259d;
        private const double LandscapeShiftWithBar = -328d;
        private const double Epsilon = 0.00000001d;
        private const double PortraitShift = -339d;
        private const double PortraitShiftWithBar = -408d;

        private readonly SolidColorBrush enableButtonColor = new SolidColorBrush(Color.FromArgb(255, 116, 181, 220));
        private readonly SolidColorBrush disableButtonColor = new SolidColorBrush(Color.FromArgb(0, 242, 242, 242));

        public static readonly DependencyProperty TranslateYProperty = DependencyProperty.Register("TranslateY", typeof(double), typeof(ChatThread), new PropertyMetadata(0d, OnRenderXPropertyChanged));

        #region handling keyboard focus
        public double TranslateY
        {
            get { return (double)GetValue(TranslateYProperty); }
            set { SetValue(TranslateYProperty, value); }
        }

        private static void OnRenderXPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ChatThread)d).UpdateTopMargin((double)e.NewValue);
        }

        private void ChatThreadPage_Loaded(object sender, RoutedEventArgs e)
        {
            BindToKeyboardFocus();
        }

        private void BindToKeyboardFocus()
        {
            PhoneApplicationFrame frame = Application.Current.RootVisual as PhoneApplicationFrame;
            if (frame != null)
            {
                var group = frame.RenderTransform as TransformGroup;
                if (group != null)
                {
                    var translate = group.Children[0] as TranslateTransform;
                    var translateYBinding = new Binding("Y");
                    translateYBinding.Source = translate;
                    SetBinding(TranslateYProperty, translateYBinding);
                }
            }
        }

        private void UpdateTopMargin(double translateY)
        {
            if (IsClose(translateY, LandscapeShift) || IsClose(translateY, PortraitShift)
            || IsClose(translateY, LandscapeShiftWithBar) || IsClose(translateY, PortraitShiftWithBar))
            {
                LayoutRoot.Margin = new Thickness(0, -translateY, 0, 0);
            }
        }

        private bool IsClose(double a, double b)
        {
            return Math.Abs(a - b) < Epsilon;
        }

        private void TextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            LayoutRoot.Margin = new Thickness();
        }
        #endregion

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

        private void initAppBar()
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Minimized;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            ApplicationBarMenuItem menuItem1 = new ApplicationBarMenuItem();
            menuItem1.Text = "Block User";
            menuItem1.Click += new EventHandler(blockUnblock_Click);
            appBar.MenuItems.Add(menuItem1);
        }

        private void initAppBarIconButton()
        {
            if (inviteUsrIconButton != null)
                return;
            inviteUsrIconButton = new ApplicationBarIconButton();
            inviteUsrIconButton.IconUri = new Uri("images\\appbar.favs.addto.rest.png", UriKind.Relative);
            inviteUsrIconButton.Text = "invite";
            inviteUsrIconButton.Click += new EventHandler(inviteUserBtn_Click);
            inviteUsrIconButton.IsEnabled = true;
        }

        public ChatThread()
        {
            InitializeComponent();
            this.myListBox.ItemsSource = chatThreadPageCollection;
            mPubSub = App.HikePubSubInstance;
            registerListeners();
            initPageBasedOnState();
            initAppBar();
            chatThreadMainPage.ApplicationBar = appBar;
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
            hikeLabel.Text = mContactName;
            this.Loaded += new RoutedEventHandler(ChatThreadPage_Loaded);
        }

        #region register broadcast listeners
        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.ICON_CHANGED, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.UPDATE_UI, this);

        }
        #endregion

        #region remove broadcast listeners
        private void removeListeners()
        {
        }
        #endregion

        private void initPageBasedOnState()
        {
            if (PhoneApplicationService.Current.State.ContainsKey("objFromConversationPage")) // represents chatthread is called from convlist page
            {
                ConversationListObject obj = (ConversationListObject)PhoneApplicationService.Current.State["objFromConversationPage"];
                mContactNumber = obj.MSISDN;
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

            if (mContactNumber == null)
            {
                // some error handling
                return;
            }
            mUserIsBlocked = UsersTableUtils.isUserBlocked(mContactNumber);
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
            List<long> dbIds = new List<long>();
            for (int i = 0; i < messagesList.Count; i++)
            {
                if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    isPublish = true;
                    ids.Add(Convert.ToString(messagesList[i].MappedMessageId));
                    dbIds.Add(messagesList[i].MessageId);
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                this.ChatThreadPageCollection.Add(messagesList[i]);
                if (messagesList[i].IsSent)
                    msgMap.Add(messagesList[i].MessageId, messagesList[i]);
                else
                    incomingMessages.Add(messagesList[i]);
            }
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[messagesList.Count - 1]);

            if (isPublish)
            {
                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, mContactNumber);
                obj.Add(HikeConstants.DATA, ids);

                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, dbIds.ToArray());
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                mPubSub.publish(HikePubSub.MSG_READ, mContactNumber);
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
            if ((!isOnHike && mCredits <= 0) || message == "")
            {
                return;
            }

            sendMsgTxtbox.Text = "";
            sendMsgBtn.Foreground = disableButtonColor;
            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = !isOnHike;
            this.ChatThreadPageCollection.Add(convMessage);
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[ChatThreadPageCollection.Count - 1]);
            mPubSub.publish(HikePubSub.SEND_NEW_MSG, convMessage);
            if (sendMsgTxtbox.Text != "")
            {
                sendMsgBtn.IsEnabled = true;
                sendMsgBtn.Foreground = enableButtonColor;
            }
        }

        private void sendEndTypingNotification()
        {
            long currentTime = TimeUtils.getCurrentTimeStamp();
            if (currentTime - lastTextChangedTime >= 5 && endTypingSent == false)
            {
                JObject obj = new JObject();
                try
                {
                    obj.Add(HikeConstants.TYPE, NetworkManager.END_TYPING);
                    obj.Add(HikeConstants.TO, mContactNumber);
                }
                catch (Exception ex)
                {
                    //logger.("ConvMessage", "invalid json message", e);
                }
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                endTypingSent = true;
            }
        }

        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {

            /* Create the typing notification*/
            lastTextChangedTime = TimeUtils.getCurrentTimeStamp();

            scheduler.Schedule(sendEndTypingNotification, TimeSpan.FromSeconds(5));

            if (endTypingSent)
            {
                endTypingSent = false;

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
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
            else
                if (String.IsNullOrEmpty(sendMsgTxtbox.Text.Trim()))
                {
                    sendMsgBtn.IsEnabled = false;
                    sendMsgBtn.Foreground = disableButtonColor;
                    return;
                }
            sendMsgBtn.IsEnabled = true;
            sendMsgBtn.Foreground = enableButtonColor;
        }

        #region Pubsub Event

        /* this function is running on pubsub thread and not UI thread*/
        public void onEventReceived(string type, object obj)
        {
            #region MSG RECEIVED

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
                    });
                }
            }
            else if (HikePubSub.UPDATE_UI == type)
            {
                for (int i = 0; i < incomingMessages.Count; i++)
                {
                    ConvMessage c = incomingMessages[i];
                    if (!c.IsSent)
                        c.NotifyPropertyChanged("Msisdn");
                }
            }

            # endregion

            #region SERVER RECEIVED MSG

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

            #region MSG DELIVERED

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

            #region MSG DELIVERED READ
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

            #region SMS CREDITS CHANGED

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

            #region USER LEFT/JOINED

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

            #region TYPING CONVERSATION

            else if (HikePubSub.TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        typingNotification.Visibility = Visibility.Visible;

                        //hikeLabel.Text = mContactName;// +" is typing.";
                        // handle auto removing
                    });
                }
            }

            #endregion

            #region END TYPING CONVERSATION

            else if (HikePubSub.END_TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        typingNotification.Visibility = Visibility.Collapsed;
                        //hikeLabel.Text = mContactName;
                    });
                }
            }

            #endregion
        }


        #endregion

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
                appBar.Buttons.Remove(inviteUsrIconButton);
                inviteUsrIconButton = null;
            }
            else
            {
                initAppBarIconButton();
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
                sendMsgBtn.IsEnabled = false;
                sendMsgBtn.Foreground = disableButtonColor;
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
            if (mUserIsBlocked)
            {
                mPubSub.publish(HikePubSub.UNBLOCK_USER, mContactNumber);
                mUserIsBlocked = false;
            }
            else
            {
                mPubSub.publish(HikePubSub.BLOCK_USER, mContactNumber);
                mUserIsBlocked = true;
                //showOverlay(true); true means show block animation
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
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;
            MessagesTableUtils.deleteMessage(msg.MessageId); // delete msg with given msgId from messages table
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

                // delete the conversation from DB.
                ConversationTableUtils.deleteConversation(obj.MSISDN);
            }
        }


        private void sendMsgTxtbox_GotFocus(object sender, RoutedEventArgs e)
        {
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

    }
}