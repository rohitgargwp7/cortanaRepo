using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;
using Newtonsoft.Json.Linq;
using windows_client.DbUtils;
using windows_client.Model;
using windows_client.utils;
using Coding4Fun.Phone.Controls;
using System.Collections.ObjectModel;

namespace windows_client.View
{
    public partial class ChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        #region CONSTANTS AND PAGE OBJECTS

        private readonly string ON_HIKE_TEXT = "Free Message...";
        private readonly string ON_SMS_TEXT = "SMS Message...";
        private readonly string ZERO_CREDITS_MSG = "0 Free SMS left...";
        private readonly string BLOCK_USER = "BLOCK";
        private readonly string UNBLOCK_USER = "UNBLOCK";

        private string groupOwner = null;
        private string mContactNumber;
        private string mContactName = null;
        private string lastText = "";

        private bool isGroupChat = false;
        private bool mUserIsBlocked;
        private bool isOnHike;
        private bool animatedOnce = false;
        private bool endTypingSent = true;

        private int mCredits;
        private long lastTextChangedTime;

        ConversationListObject cObj = null; // used for toast
        private HikePubSub mPubSub;
        private IScheduler scheduler = Scheduler.NewThread;

        private ApplicationBar appBar;
        ApplicationBarMenuItem menuItem1;
        ApplicationBarMenuItem inviteMenuItem = null;

        private ObservableCollection<ConvMessage> chatThreadPageCollection = new ObservableCollection<ConvMessage>();
        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> message mapping
        private List<ConvMessage> incomingMessages = new List<ConvMessage>();

        private GroupInfo gi = null;

        #endregion

        #region UI VALUES

        private static readonly SolidColorBrush whiteBackground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private static readonly SolidColorBrush blackBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        private static readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
        private static readonly SolidColorBrush smsBackground = new SolidColorBrush(Color.FromArgb(255, 219, 242, 207));
        private static readonly SolidColorBrush hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 177, 224, 251));
        private static Thickness imgMargin = new Thickness(0, 5, 0, 0);

        #endregion

        #region PROPERTY

        private BitmapImage[] imagePathsForList0
        {
            get
            {
                return SmileyParser._emoticonImagesForList0;
            }
        }

        private BitmapImage[] imagePathsForList1
        {
            get
            {
                return SmileyParser._emoticonImagesForList1;
            }
        }

        private BitmapImage[] imagePathsForList2
        {
            get
            {
                return SmileyParser._emoticonImagesForList2;
            }
        }

        public List<ConvMessage> IncomingMessages /* This List will contain only incoming messages */
        {
            get
            {
                return incomingMessages;
            }
        }

        public Dictionary<long, ConvMessage> OutgoingMsgsMap      /* This map will contain only outgoing messages */
        {
            get
            {
                return msgMap;
            }
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

        #endregion

        #region PAGE BASED FUNCTIONS

        public ChatThread()
        {
            InitializeComponent();
            mPubSub = App.HikePubSubInstance;
            initPageBasedOnState();
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsEnabled = true;
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += new DoWorkEventHandler(bw_DoWork);
            bw.RunWorkerAsync();
            emotList0.ItemsSource = imagePathsForList0;
            emotList1.ItemsSource = imagePathsForList1;
            emotList2.ItemsSource = imagePathsForList2;
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

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();
        }

        #endregion

        #region INIT PAGE BASED ON STATE

        private void initPageBasedOnState()
        {
            bool isAddUser = false;

            #region OBJECT FROM CONVLIST PAGE

            if (PhoneApplicationService.Current.State.ContainsKey("objFromConversationPage")) // represents chatthread is called from convlist page
            {
                ConversationListObject convObj = (ConversationListObject)PhoneApplicationService.Current.State["objFromConversationPage"];
                PhoneApplicationService.Current.State.Remove("objFromConversationPage");
                mContactNumber = convObj.Msisdn;

                if (Utils.isGroupConversation(mContactNumber)) // represents group chat
                {
                    isGroupChat = true;
                }

                if (convObj.ContactName != null)
                    mContactName = convObj.ContactName;
                else
                {
                    mContactName = convObj.Msisdn;
                    isAddUser = true;
                }

                isOnHike = convObj.IsOnhike;
            }

            #endregion

            #region OBJECT FROM SELECT USER PAGE
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
            #endregion

            #region OBJECT FROM SELECT GROUP PAGE

            else if (PhoneApplicationService.Current.State.ContainsKey("groupChat"))
            {
                string groupId;
                //add members to existing group
                if (PhoneApplicationService.Current.State.ContainsKey("groupInfoFromGroupProfile"))
                {
                    gi = PhoneApplicationService.Current.State["groupInfoFromGroupProfile"] as GroupInfo;
                    groupId = gi.GroupId;
                    groupOwner = gi.GroupOwner;
                    PhoneApplicationService.Current.State.Remove("groupInfoFromGroupProfile");
                }
                else
                {
                    // here always create a new group
                    string uid = AccountUtils.Token;
                    groupId = mContactNumber = uid + ":" + TimeUtils.getCurrentTimeStamp();
                    groupOwner = App.MSISDN;
                }
                mContactNumber = groupId;
                List<ContactInfo> contactsForGroup = PhoneApplicationService.Current.State["groupChat"] as List<ContactInfo>;

                List<GroupMembers> memberList = new List<GroupMembers>(contactsForGroup.Count);
                for (int i = 0; i < contactsForGroup.Count; i++)
                {
                    GroupMembers gm = new GroupMembers(groupId, contactsForGroup[i].Msisdn, contactsForGroup[i].Name);
                    memberList.Add(gm);
                    if (!Utils.GroupCache.ContainsKey(contactsForGroup[i].Msisdn))
                    {
                        Utils.GroupCache.Add(contactsForGroup[i].Msisdn, new GroupParticipant(contactsForGroup[i].Name, contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike));
                    }
                }
                JObject obj = createGroupJsonPacket(HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN, memberList);

                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += new DoWorkEventHandler(createGroup_Async);
                bw.RunWorkerAsync(memberList);

                mContactName = string.IsNullOrEmpty(mContactName) ? Utils.defaultGroupName(memberList) : mContactName;
                isOnHike = true;
                isGroupChat = true;

                ConvMessage cm = new ConvMessage(obj, true);
                sendMsg(cm, true);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
             
                PhoneApplicationService.Current.State.Remove("groupChat");
            }

            #endregion

            userImage.Source = UI_Utils.Instance.getBitMapImage(mContactNumber);
            userName.Text = mContactName;
            initAppBar(isGroupChat, isAddUser);
            if (!isOnHike)
            {
                sendMsgTxtbox.Hint = ON_SMS_TEXT;
                initInviteMenuItem();
                appBar.MenuItems.Add(inviteMenuItem);
            }
            else
            {
                sendMsgTxtbox.Hint = ON_HIKE_TEXT;
            }
        }

        private void createGroup_Async(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            List<GroupMembers> memberList = (List<GroupMembers>)e.Argument;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                GroupTableUtils.addGroupMembers(memberList);
                if (gi == null)
                {
                    gi = new GroupInfo(mContactNumber, null, groupOwner, true);
                    GroupTableUtils.addGroupInfo(gi);
                }
            }

        }

        private JObject createGroupJsonPacket(string type, List<GroupMembers> grpList)
        {
            JObject obj = new JObject();
            try
            {
                obj[HikeConstants.TYPE] = type;
                obj[HikeConstants.TO] = grpList[0].GroupId;
                if (type == (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN))
                {
                    JArray array = new JArray();
                    for (int i = 0; i < grpList.Count; i++)
                    {
                        JObject nameMsisdn = new JObject();
                        nameMsisdn[HikeConstants.NAME] = grpList[i].Name;
                        nameMsisdn[HikeConstants.MSISDN] = grpList[i].Msisdn;
                        array.Add(nameMsisdn);
                    }
                    obj[HikeConstants.DATA] = array;
                }
                Debug.WriteLine("GROUP JSON : " + obj.ToString());
            }
            catch (Exception e)
            {
                Debug.WriteLine("ConvMessage", "invalid json message", e);
            }
            return obj;
        }

        #region APP BAR

        /* Should run on UI thread, based on mUserIsBlocked*/
        private void initAppBar(bool isGroupChat, bool isAddUser)
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

            //add icon for smiley
            ApplicationBarIconButton emoticonsIconButton = new ApplicationBarIconButton();
            emoticonsIconButton.IconUri = new Uri("/View/images/icon_emoticon.png", UriKind.Relative);
            emoticonsIconButton.Text = "smiley";
            emoticonsIconButton.Click += new EventHandler(emoticonButton_Click);
            emoticonsIconButton.IsEnabled = true;
            appBar.Buttons.Add(emoticonsIconButton);

            if (isGroupChat)
            {
                ApplicationBarMenuItem groupInfoMenuItem = new ApplicationBarMenuItem();
                groupInfoMenuItem.Text = "group info";
                groupInfoMenuItem.Click += new EventHandler(groupInfo_Click);
                appBar.MenuItems.Add(groupInfoMenuItem);

                ApplicationBarMenuItem leaveMenuItem = new ApplicationBarMenuItem();
                leaveMenuItem.Text = "leave group";
                leaveMenuItem.Click += new EventHandler(leaveGroup_Click);
                appBar.MenuItems.Add(leaveMenuItem);

                if (groupOwner == null) // case where someone else created the group
                {
                    gi = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    groupOwner = gi != null ?  gi.GroupOwner : null;
                }
                if (groupOwner != null)
                {
                    if (groupOwner != App.MSISDN) // represents current user is not group owner
                    {
                        menuItem1 = new ApplicationBarMenuItem();
                        if (mUserIsBlocked)
                        {
                            menuItem1.Text = UNBLOCK_USER + " group owner";
                        }
                        else
                        {
                            menuItem1.Text = BLOCK_USER + " group owner";
                        }
                        menuItem1.Click += new EventHandler(blockUnblock_Click);
                        appBar.MenuItems.Add(menuItem1);
                    }
                }
            }
            else
            {

                menuItem1 = new ApplicationBarMenuItem();
                if (mUserIsBlocked)
                {
                    menuItem1.Text = UNBLOCK_USER;
                }
                else
                {
                    menuItem1.Text = BLOCK_USER;
                }
                menuItem1.Click += new EventHandler(blockUnblock_Click);
                appBar.MenuItems.Add(menuItem1);

                if (isAddUser)
                {
                    ApplicationBarMenuItem menuItem2 = new ApplicationBarMenuItem();
                    menuItem2.Text = "add user";
                    menuItem2.Click += new EventHandler(addUser_Click);
                    appBar.MenuItems.Add(menuItem2);
                }
            }
            chatThreadMainPage.ApplicationBar = appBar;
        }

        private void initInviteMenuItem()
        {
            inviteMenuItem = new ApplicationBarMenuItem();
            inviteMenuItem.Text = "invite user";
            inviteMenuItem.Click += new EventHandler(inviteUserBtn_Click);
        }

        #endregion

        #endregion

        #region BACKGROUND WORKER

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                Stopwatch st = Stopwatch.StartNew();
                loadMessages();
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to load chat messages for msisdn {0} : {1}", mContactNumber, msec);
                initBlockUnblockState();
                mCredits = (int)App.appSettings[App.SMS_SETTING];
                registerListeners();
            }

        }

        private void loadMessages()
        {
            int i;
            int limit = 6;
            bool isPublish = false;
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            //int messageCount = messagesList == null ? 0 : messagesList.Count;

            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.myListBox.ItemsSource = chatThreadPageCollection;
                    progressBar.Visibility = System.Windows.Visibility.Collapsed;
                    progressBar.IsEnabled = false;
                });
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
                this.ChatThreadPageCollection.Add(cm);
            }

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
                this.ChatThreadPageCollection.Insert(0, c);
                if (count % 5 == 0)
                    Thread.Sleep(5);
                if (messagesList[i].IsSent)
                    msgMap.Add(messagesList[i].MessageId, messagesList[i]);
                else
                    incomingMessages.Insert(0, messagesList[i]);
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.myListBox.ItemsSource = chatThreadPageCollection;
                this.myListBox.UpdateLayout();
                this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                progressBar.Visibility = System.Windows.Visibility.Collapsed;
                progressBar.IsEnabled = false;
            });

            if (isPublish)
            {
                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, mContactNumber);
                obj.Add(HikeConstants.DATA, ids);

                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, dbIds.ToArray()); // this is to notify DB
                mPubSub.publish(HikePubSub.MSG_READ, mContactNumber); // this is to notify ConvList. This can be done on UI directly
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                isPublish = false;
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

        #endregion

        #region REGISTER/DEREGISTER LISTENERS

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
            mPubSub.addListener(HikePubSub.GROUP_NAME_CHANGED, this);
            mPubSub.addListener(HikePubSub.GROUP_END, this);
        }

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
            mPubSub.removeListener(HikePubSub.GROUP_NAME_CHANGED, this);
            mPubSub.removeListener(HikePubSub.GROUP_END, this);
        }
        #endregion

        #region APPBAR CLICK EVENTS

        private void addUser_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(mContactNumber);
        }

        private void leaveGroup_Click(object sender, EventArgs e)
        {
            /*
             * 1. Delete from DB (pubsub)
             * 2. Remove from ConvList page
             * 3. GoBack
             */
            JObject jObj = new JObject();
            jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
            jObj[HikeConstants.TO] = mContactNumber;

            mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);
            mPubSub.publish(HikePubSub.GROUP_LEFT, mContactNumber);
            ConversationListObject cObj = ConversationsList.ConvMap[mContactNumber];
            App.ViewModel.MessageListPageCollection.Remove(cObj);
            ConversationsList.ConvMap.Remove(mContactNumber);
            NavigationService.GoBack();
        }

        private void groupInfo_Click(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State["objFromChatThreadPage"] = gi;
            NavigationService.Navigate(new Uri("/View/GroupInfoPage.xaml", UriKind.Relative));
        }

        private void blockUnblock_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked) // UNBLOCK REQUEST
            {
                if (isGroupChat)
                {
                    object[] vals = new object[2];
                    vals[0] = mContactNumber;
                    vals[1] = groupOwner;
                    mPubSub.publish(HikePubSub.UNBLOCK_GROUPOWNER, vals);
                }
                else
                    mPubSub.publish(HikePubSub.UNBLOCK_USER,mContactNumber);
                mUserIsBlocked = false;
                menuItem1.Text = BLOCK_USER;
                showOverlay(false);
            }
            else     // BLOCK REQUEST
            {
                if (isGroupChat)
                {
                    object[] vals = new object[2];
                    vals[0] = mContactNumber;
                    vals[1] = groupOwner;
                    mPubSub.publish(HikePubSub.BLOCK_GROUPOWNER, vals);
                }
                else
                    mPubSub.publish(HikePubSub.BLOCK_USER,mContactNumber);
                mUserIsBlocked = true;
                menuItem1.Text = UNBLOCK_USER;
                showOverlay(true); //true means show block animation
            }
        }

        private void inviteUserBtn_Click(object sender, EventArgs e)
        {

            if (isOnHike)
                return;

            long time = utils.TimeUtils.getCurrentTimeStamp();
            ConvMessage convMessage = new ConvMessage(App.invite_message, mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsInvite = true;
            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
        }

        #endregion

        #region PAGE EVENTS

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

        private void sendMsgBtn_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked)
                return;
            string message = sendMsgTxtbox.Text.Trim();
            sendMsgTxtbox.Text = "";

            if (String.IsNullOrEmpty(message))
                return;

            emoticonPanel.Visibility = Visibility.Collapsed;

            if ((!isOnHike && mCredits <= 0) || message == "")
                return;

            endTypingSent = true;
            sendTypingNotification(false);

            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.IsSms = !isOnHike;
            sendMsg(convMessage,false);
        }
        private void splitUserJoinedMessage(ConvMessage convMessage)
        {
            if (convMessage.GrpParticipantState != ConvMessage.ParticipantInfoState.NO_INFO)
            {
                string[] names = convMessage.Message.Split(',');
                int i = 0;
                for (; i < names.Length - 2; i++)
                {
                    ConvMessage c = new ConvMessage(names[i].Trim() + " has joined the Group Chat", convMessage.Msisdn, convMessage.Timestamp, convMessage.MessageStatus);
                    this.ChatThreadPageCollection.Add(c);
                }
                convMessage.Message = names[i].Trim() + " has joined the Group Chat";
                this.ChatThreadPageCollection.Add(convMessage);
            }
            else
            {
                this.ChatThreadPageCollection.Add(convMessage);
            }
        }

        private void sendMsg(ConvMessage convMessage,bool isNewGroup)
        {
            //user joined
            if (isNewGroup)
            {
                PhoneApplicationService.Current.State[mContactNumber] = mContactName;
                JObject metaData = new JObject();
                metaData[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN;
                convMessage.SetMetaData = new MessageMetadata(metaData);
                splitUserJoinedMessage(convMessage);
            }
            else
            {
                this.ChatThreadPageCollection.Add(convMessage);
            }
            this.myListBox.UpdateLayout();
            this.myListBox.ScrollIntoView(chatThreadPageCollection[ChatThreadPageCollection.Count - 1]);

            object[] vals = new object[2];
            vals[0] = convMessage;
            vals[1] = isNewGroup;
           
            mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
        }

        private void sendMsgTxtbox_GotFocus(object sender, RoutedEventArgs e)
        {
            sendMsgTxtbox.Background = textBoxBackground;
        }

        void toast_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State["objFromConversationPage"] = cObj;
            NavigationService.Navigate(new Uri("/View/ChatThread.xaml?Id=1", UriKind.Relative));
        }

        #endregion

        #region CONTEXT MENU

        private void MenuItem_Tap_Copy(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBoxItem selectedListBoxItem = this.myListBox.ItemContainerGenerator.ContainerFromItem((sender as MenuItem).DataContext) as ListBoxItem;

            if (selectedListBoxItem == null)
            {
                return;
            }
            ConvMessage msg = selectedListBoxItem.DataContext as ConvMessage;
            Clipboard.SetText(msg.Message);
        }

        private void MenuItem_Tap_Forward(object sender, System.Windows.Input.GestureEventArgs e)
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

        private void MenuItem_Tap_Delete(object sender, System.Windows.Input.GestureEventArgs e)
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
                //This updates the Conversation list.
                obj.LastMessage = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].Message;
                obj.MessageStatus = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].MessageStatus;
                obj.TimeStamp = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].Timestamp;
            }
            else
            {
                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj);
                ConversationsList.ConvMap.Remove(msg.Msisdn);
                delConv = true;
            }
            object[] o = new object[3];
            o[0] = msg.MessageId;
            o[1] = obj;
            o[2] = delConv;
            mPubSub.publish(HikePubSub.MESSAGE_DELETED, o);
        }

        #endregion

        #region EMOTICONS RELATED STUFF

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            object s = e.OriginalSource;
        }

        private void optionsList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int selectedIndex = optionsList.SelectedIndex;
            emoticonPivot.SelectedIndex = selectedIndex;
            //if (selectedIndex == 1)
            //{
            //    emotList1.Visibility = Visibility.Visible;
            //}
            //else if (selectedIndex == 2)
            //{
            //    emotList2.Visibility = Visibility.Visible;
            //}

        }

        private void emoticonButton_Click(object sender, EventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Visible;
        }

        private void chatListBox_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void emoticonPanel_LostFocus(object sender, RoutedEventArgs e)
        {
            //emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void RichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            var richTextBox = sender as RichTextBox;
            if (richTextBox.Tag == null)
                return;
            string messageString = richTextBox.Tag.ToString();

            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(messageString);
            Paragraph p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;

            for (int i = 0; i < matchCollection.Count; i++)
            {
                String emoticon = matchCollection[i].ToString();

                //Regex never returns an empty string. Still have added an extra check
                if (String.IsNullOrEmpty(emoticon))
                    continue;

                int index = matchCollection[i].Index;
                endIndex = index - 1;

                if (index > 0)
                {
                    Run r = new Run();
                    r.Text = messageString.Substring(startIndex, endIndex - startIndex + 1);
                    p.Inlines.Add(r);
                }

                startIndex = index + emoticon.Length;

                //TODO check if imgPath is null or not
                Image img = new Image();
                img.Source = SmileyParser.lookUpFromCache(emoticon);
                img.Height = 40;
                img.Width = 40;
                img.Margin = imgMargin;

                InlineUIContainer ui = new InlineUIContainer();
                ui.Child = img;
                p.Inlines.Add(ui);
            }
            if (startIndex < messageString.Length)
            {
                Run r2 = new Run();
                r2.Text = messageString.Substring(startIndex, messageString.Length - startIndex);
                p.Inlines.Add(r2);
            }

            richTextBox.Blocks.Clear();
            richTextBox.Blocks.Add(p);

        }

        private void emotList0_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList0.SelectedIndex;
            sendMsgTxtbox.Text += SmileyParser.emoticonStrings[index];
            emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList1.SelectedIndex + 80;
            sendMsgTxtbox.Text += SmileyParser.emoticonStrings[index];
            emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList2.SelectedIndex + 110;
            sendMsgTxtbox.Text += SmileyParser.emoticonStrings[index];
            emoticonPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region HELPER FUNCTIONS

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
                if (appBar.MenuItems.Contains(inviteMenuItem))
                    appBar.MenuItems.Remove(inviteMenuItem);
            }
            else
            {
                if (inviteMenuItem == null)
                    initInviteMenuItem();
                if (!appBar.MenuItems.Contains(inviteMenuItem))
                    appBar.MenuItems.Add(inviteMenuItem);
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

        #endregion

        #region TYPING NOTIFICATIONS

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

        #endregion

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

        #region PUBSUB EVENTS

        /* this function is running on pubsub thread and not UI thread*/
        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_RECEIVED

            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                /* Check if this is the same user for which this message is recieved*/
                if (convMessage.Msisdn == mContactNumber)
                {
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_READ;
                    if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO) // do not notify in case of group end , user left , user joined
                    {
                        mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, new long[] { convMessage.MessageId });
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serializeDeliveryReportRead()); // handle return to sender
                    }
                    mPubSub.publish(HikePubSub.MSG_READ, convMessage.Msisdn);

                    // Update UI
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //user left
                        this.ChatThreadPageCollection.Add(convMessage);
                        this.myListBox.UpdateLayout();
                        this.myListBox.ScrollIntoView(chatThreadPageCollection[chatThreadPageCollection.Count - 1]);
                        //set typing notification as false
                        typingNotification.Opacity = 0;
                    });
                }
                else
                {
                    cObj = vals[1] as ConversationListObject;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ToastPrompt toast = new ToastPrompt();
                        if (cObj.ContactName != null)
                            toast.Title = cObj.ContactName;
                        else
                            toast.Title = cObj.Msisdn;
                        toast.Message = convMessage.Message;
                        toast.ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute));
                        toast.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(toast_Tap);
                        toast.Show();
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
                    //logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
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
                        if ((int)msg.MessageStatus < (int)ConvMessage.State.SENT_DELIVERED)
                            msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                    }
                }
                catch (KeyNotFoundException e)
                {
                    //logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
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
                        //logger.Info("CHATTHREAD", "Message Delivered Read Exception " + e);
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
                        c.NotifyPropertyChanged("AvatarImage");
                }
            }

            #endregion

            #region GROUP NAME CHANGED

            else if (HikePubSub.GROUP_NAME_CHANGED == type)
            {
                object[] vals = (object[])obj;
                string groupId = (string)vals[0];
                string groupName = (string)vals[1];
                if (mContactNumber == groupId)
                {
                    mContactName = groupName;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        userName.Text = mContactName;
                    });
                }
            }

            #endregion

            #region GROUP END

            else if (HikePubSub.GROUP_END == type)
            {
                string groupId = (string)obj;

                if (mContactNumber == groupId)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        groupChatEnd();
                    });
                }
            }

            #endregion
        }

        private void groupChatEnd()
        {
            /* Madhur Complete this.*/
            sendMsgTxtbox.IsEnabled = false;
            appBar.IsMenuEnabled = false;
        }

        #endregion

    }
}