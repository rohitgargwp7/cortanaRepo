﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
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
using Microsoft.Phone.Tasks;
using System.IO;
using windows_client.Controls;

namespace windows_client.View
{
    public partial class NewChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
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

        private bool isFirstMsg = false; // this is used in GC , when you want to show joined msg for SMS and DND users.
        private bool isFirstLaunch = true;
        private bool isGroupAlive = true;
        private bool isGroupChat = false;
        private bool mUserIsBlocked;
        private bool isOnHike;
        private bool animatedOnce = false;
        private bool endTypingSent = true;
        private bool isTypingNotificationActive = false;
        private bool isTypingNotificationEnabled = true;
        private bool isReshowTypingNotification = false;
        private bool isContextMenuTapped = false;

        private Dictionary<long, Attachment> attachments = null; //this map is required for mapping attachment object with convmessage only for
        //messages stored in db, other messages would have their attachment object set

        private static long tempMsgId = -2;

        private int mCredits;
        private long lastTextChangedTime;

        private HikePubSub mPubSub;
        private IScheduler scheduler = Scheduler.NewThread;

        private ApplicationBar appBar;
        ApplicationBarMenuItem menuItem1;
        ApplicationBarMenuItem inviteMenuItem = null;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton emoticonsIconButton = null;
        ApplicationBarIconButton fileTransferIconButton = null;
        private PhotoChooserTask photoChooserTask;


        private ObservableCollection<MyChatBubble> chatThreadPageCollection = new ObservableCollection<MyChatBubble>();
        private Dictionary<long, SentChatBubble> msgMap = new Dictionary<long, SentChatBubble>(); // this holds msgId -> sent message bubble mapping
        //private Dictionary<ConvMessage, SentChatBubble> _convMessageSentBubbleMap = new Dictionary<ConvMessage, SentChatBubble>(); // this holds msgId -> sent message bubble mapping

        private List<ConvMessage> incomingMessages = new List<ConvMessage>();
        List<GroupMembers> groupMemberList = null;

        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _nonAttachmentMenu;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentUploading;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentUploaded;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentCanceledOrFailed = null;



        #endregion

        #region UI VALUES

        private static readonly SolidColorBrush whiteBackground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        private static readonly SolidColorBrush blackBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        private static readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
        private static readonly SolidColorBrush smsBackground = new SolidColorBrush(Color.FromArgb(255, 219, 242, 207));
        private static readonly SolidColorBrush hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 177, 224, 251));
        private static Thickness imgMargin = new Thickness(0, 5, 0, 15);
        private static Image typingNotificationImage;

        #endregion

        #region PROPERTY

        public bool IsFirstMsg
        {
            get
            {
                return isFirstMsg;
            }
            set
            {
                if (value != isFirstMsg)
                    isFirstMsg = value;
            }
        }

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

        public Dictionary<long, SentChatBubble> OutgoingMsgsMap      /* This map will contain only outgoing messages */
        {
            get
            {
                return msgMap;
            }
        }

        public ObservableCollection<MyChatBubble> ChatThreadPageCollection
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

        #region CONTEXT MENU DICTIONARY
        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> NonAttachmentMenu
        {
            get
            {
                if (_nonAttachmentMenu == null)
                {
                    _nonAttachmentMenu = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _nonAttachmentMenu.Add("copy", MenuItem_Click_Copy);
                    _nonAttachmentMenu.Add("forward", MenuItem_Click_Forward);
                    _nonAttachmentMenu.Add("delete", MenuItem_Click_Delete);
                }
                return _nonAttachmentMenu;
            }
            set
            {
                _nonAttachmentMenu = value;
            }
        }


        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentUploading
        {
            get
            {
                if (_attachmentUploading == null)
                {
                    _attachmentUploading = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentUploading.Add("cancel", MenuItem_Click_Cancel);
                    _attachmentUploading.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentUploading;
            }
            set
            {
                _attachmentUploading = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentUploaded
        {
            get
            {
                if (_attachmentUploaded == null)
                {
                    _attachmentUploaded = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentUploaded.Add("forward", MenuItem_Click_Forward);
                    _attachmentUploaded.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentUploaded;
            }
            set
            {
                _attachmentUploaded = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentCanceledOrFailed
        {
            get
            {
                if (_attachmentCanceledOrFailed == null)
                {
                    _attachmentCanceledOrFailed = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentCanceledOrFailed.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentCanceledOrFailed;
            }
            set
            {
                _attachmentCanceledOrFailed = value;
            }
        }

        #endregion

        #endregion

        #region PAGE BASED FUNCTIONS

        public NewChatThread()
        {
            InitializeComponent();
        }

        private void ManagePageStateObjects()
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE))
            {
                this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_SELECTUSER_PAGE))
            {
                this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE];
                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_SELECTUSER_PAGE);
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
        }

        private void ManagePage()
        {
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
            if (typingNotificationImage == null)
            {
                typingNotificationImage = new Image();
                typingNotificationImage.Source = UI_Utils.Instance.TypingNotificationBitmap;
                typingNotificationImage.Height = 28;
                typingNotificationImage.Width = 55;
                typingNotificationImage.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                typingNotificationImage.Visibility = Visibility.Visible;
                typingNotificationImage.Margin = imgMargin;
            }
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            //photoChooserTask.PixelHeight = 400;
            //photoChooserTask.PixelWidth = 400;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }



        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            #region TOMBSTONE HANDLING

            if (!App.isConvCreated)// && !PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
            {
                if (isFirstLaunch) // if first timel launching after tombstone
                {
                    /* Tombstone case and page is opened from select user page*/
                    Debug.WriteLine("CHAT THREAD :: Recovered from Tombstone.");
                    NetworkManager.turnOffNetworkManager = false;
                    App.MqttManagerInstance.connect();
                    object obj = null;
                    if (this.State.TryGetValue("sendMsgTxtbox.Text", out obj))
                    {
                        sendMsgTxtbox.Text = (string)obj;
                        sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
                    }

                    /* This is called only when you add more participants to group */
                    if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
                    {
                        ManagePage();
                    }
                    else
                    {
                        ManagePageStateObjects();
                        ManagePage();
                    }
                }
                /* This is called only when you add more participants to group */
                else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
                {
                    PhoneApplicationService.Current.State.Remove(HikeConstants.IS_EXISTING_GROUP);
                    this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                    processGroupJoin(false);
                }
                isFirstLaunch = false;
            }
            #endregion

            else if (App.isConvCreated) // non tombstone case
            {
                if (isFirstLaunch) // case is first launch and normal launch i.e no tombstone
                {
                    ManagePageStateObjects();
                    ManagePage();
                    isFirstLaunch = false;
                }
                /* This is called only when you add more participants to group */
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
                {
                    PhoneApplicationService.Current.State.Remove(HikeConstants.IS_EXISTING_GROUP);
                    this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                    processGroupJoin(false);
                }
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (!string.IsNullOrWhiteSpace(sendMsgTxtbox.Text))
                this.State["sendMsgTxtbox.Text"] = sendMsgTxtbox.Text;
            else
                this.State.Remove("sendMsgTxtbox.Text");
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

            if (this.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE)) // represents NewChatThread is called from convlist page
            {
                ConversationListObject convObj = (ConversationListObject)this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                mContactNumber = convObj.Msisdn;

                if (Utils.isGroupConversation(mContactNumber)) // represents group chat
                {
                    isGroupChat = true;
                    GroupInfo gi = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    if (!gi.GroupAlive)
                        isGroupAlive = false;
                }

                if (convObj.ContactName != null)
                    mContactName = convObj.ContactName;
                else
                {
                    mContactName = convObj.Msisdn;
                    isAddUser = true;
                }

                isOnHike = convObj.IsOnhike;
                userImage.Source = convObj.AvatarImage;
                isFirstMsg = convObj.IsFirstMsg;
            }

            #endregion
            #region OBJECT FROM SELECT GROUP PAGE

            else if (this.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                // here always create a new group
                string uid = AccountUtils.Token;
                mContactNumber = uid + ":" + TimeUtils.getCurrentTimeStamp();
                groupOwner = App.MSISDN;
                processGroupJoin(true);
                isOnHike = true;
                isGroupChat = true;
                userImage.Source = UI_Utils.Instance.DefaultAvatarBitmapImage; //TODO show new group default image

                /* This is done so that after Tombstone when this page is launched, no group is created again and again */
                ConversationListObject convObj = new ConversationListObject();
                convObj.Msisdn = mContactNumber;
                convObj.ContactName = mContactName;
                convObj.IsOnhike = true;
                this.State.Add(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE, convObj);
            }

            #endregion
            #region OBJECT FROM SELECT USER PAGE

            else if (this.State.ContainsKey(HikeConstants.OBJ_FROM_SELECTUSER_PAGE))
            {
                ContactInfo obj = (ContactInfo)this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE];
                mContactNumber = obj.Msisdn;
                mContactName = obj.Name;
                isOnHike = obj.OnHike;

                /* Check if it is a forwarded msg */
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
                {
                    if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is string)
                    {
                        sendMsgTxtbox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                    }
                    else if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is object[])
                    {
                        object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                        MyChatBubble chatBubble = (MyChatBubble)attachmentData[0];
                        string sourceMsisdn = (string)attachmentData[1];

                        string sourceFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn + "/" + chatBubble.MessageId;

                        ConvMessage convMessage = new ConvMessage(chatBubble.FileAttachment.FileName, mContactNumber,
                            TimeUtils.getCurrentTimeStamp(), ConvMessage.State.UNKNOWN);
                        convMessage.IsSms = !isOnHike;
                        convMessage.HasAttachment = true;
                        convMessage.MessageId = TempMessageId;
                        convMessage.FileAttachment = chatBubble.FileAttachment;

                        SentChatBubble newChatBubble = new SentChatBubble(chatBubble, convMessage.MessageId, isOnHike);
                        addNewAttachmentMessageToUI(newChatBubble);
                        msgMap.Add(convMessage.MessageId, newChatBubble);

                        object[] vals = new object[2];
                        vals[0] = convMessage;
                        vals[1] = sourceFilePath;
                        mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
                    }
                    PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
                    NavigationService.RemoveBackEntry(); // remove last chat thread page
                }
                byte[] avatar = MiscDBUtil.getThumbNailForMsisdn(mContactNumber);

                if (avatar == null)
                {
                    userImage.Source = UI_Utils.Instance.DefaultAvatarBitmapImage;
                }
                else
                {
                    MemoryStream memStream = new MemoryStream(avatar);
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage empImage = new BitmapImage();
                    empImage.SetSource(memStream);
                    userImage.Source = empImage;
                }
                //}
                //else
                //    userImage.Source = UI_Utils.Instance.DefaultAvatarBitmapImage;
            }
            #endregion

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
            if (isGroupChat && !isGroupAlive)
                groupChatEnd();
        }

        private void processGroupJoin(bool isNewgroup)
        {
            List<ContactInfo> contactsForGroup = this.State[HikeConstants.GROUP_CHAT] as List<ContactInfo>;
            this.State.Remove(HikeConstants.GROUP_CHAT);
            groupMemberList = new List<GroupMembers>(contactsForGroup.Count);
            for (int i = 0; i < contactsForGroup.Count; i++)
            {
                if (!contactsForGroup[i].OnHike)
                {
                    isFirstMsg = true;
                    PhoneApplicationService.Current.State["GC_"+mContactNumber] = true;
                }
                GroupMembers gm = new GroupMembers(mContactNumber, contactsForGroup[i].Msisdn, contactsForGroup[i].Name);
                groupMemberList.Add(gm);
                if (Utils.GroupCache == null)
                {
                    Utils.GroupCache = new Dictionary<string, List<GroupParticipant>>();
                    App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
                }
                if (!Utils.GroupCache.ContainsKey(contactsForGroup[i].Msisdn))
                {
                    List<GroupParticipant> l = new List<GroupParticipant>(5);
                    l.Add(new GroupParticipant(Utils.getFirstName(contactsForGroup[i].Name), contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike));
                    Utils.GroupCache.Add(contactsForGroup[i].Msisdn, l);
                }
                else
                {
                    Utils.GroupCache[contactsForGroup[i].Msisdn].Add(new GroupParticipant(Utils.getFirstName(contactsForGroup[i].Name), contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike));
                }
            }
            JObject obj = createGroupJsonPacket(HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN, groupMemberList, isNewgroup);
            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerSupportsCancellation = true;
            if (isNewgroup)
                bw.DoWork += new DoWorkEventHandler(createGroup_Async);
            else
            {
                bw.DoWork += new DoWorkEventHandler(addToGroup_Async);
                groupMemberList.AddRange(groupMemberList);
            }
            bw.RunWorkerAsync(groupMemberList);

            mContactName = string.IsNullOrEmpty(mContactName) ? Utils.defaultGroupName(groupMemberList) : mContactName;

            ConvMessage cm = new ConvMessage(obj, true);
            sendMsg(cm, true);
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // inform others about group
        }

        private void addToGroup_Async(object sender, DoWorkEventArgs e)
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
                GroupInfo gi = new GroupInfo(mContactNumber, null, groupOwner, true);
                GroupTableUtils.addGroupInfo(gi);
            }

        }

        private JObject createGroupJsonPacket(string type, List<GroupMembers> grpList, bool isNewGroup)
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
                    if (isNewGroup) // if new group add owners info also
                    {
                        JObject nameMsisdn = new JObject();
                        nameMsisdn[HikeConstants.NAME] = (string)App.appSettings[App.ACCOUNT_NAME];
                        nameMsisdn[HikeConstants.MSISDN] = App.MSISDN;
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
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            sendIconButton.Text = "send";
            sendIconButton.Click += new EventHandler(sendMsgBtn_Click);
            sendIconButton.IsEnabled = true;
            appBar.Buttons.Add(sendIconButton);

            //add icon for smiley
            emoticonsIconButton = new ApplicationBarIconButton();
            emoticonsIconButton.IconUri = new Uri("/View/images/icon_emoticon.png", UriKind.Relative);
            emoticonsIconButton.Text = "smiley";
            emoticonsIconButton.Click += new EventHandler(emoticonButton_Click);
            emoticonsIconButton.IsEnabled = true;
            appBar.Buttons.Add(emoticonsIconButton);

            //add file transfer button
            fileTransferIconButton = new ApplicationBarIconButton();
            fileTransferIconButton.IconUri = new Uri("/View/images/icon_attachment.png", UriKind.Relative);
            fileTransferIconButton.Text = "attach";
            fileTransferIconButton.Click += new EventHandler(fileTransferButton_Click);
            fileTransferIconButton.IsEnabled = true;
            appBar.Buttons.Add(fileTransferIconButton);


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
                    GroupInfo gi = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    groupOwner = (gi != null) ? gi.GroupOwner : null;
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
                ApplicationBarMenuItem callMenuItem = new ApplicationBarMenuItem();
                callMenuItem.Text = "call";
                callMenuItem.Click += new EventHandler(callUser_Click);
                appBar.MenuItems.Add(callMenuItem);
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
                attachments = MiscDBUtil.getAllFileAttachment(mContactNumber);
                //attachments = new Dictionary<long, Attachment>();
                loadMessages();
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to load chat messages for msisdn {0} : {1}", mContactNumber, msec);
                initBlockUnblockState();

                App.appSettings.TryGetValue(App.SMS_SETTING, out mCredits);
                registerListeners();
            }
        }

        private void loadMessages()
        {
            int i;
            int limit = 6;
            bool isPublish = false;
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Scroller.Opacity = 1;
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
                    if (messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                        ids.Add(Convert.ToString(messagesList[i].MappedMessageId));
                    dbIds.Add(messagesList[i].MessageId);
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                ConvMessage cm = messagesList[i];
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    AddMessageToUI(cm, true);
                });
            }

            int count = 0;
            for (i = messagesList.Count - limit - 1; i >= 0; i--)
            {
                count++;
                messagesList[i].IsSms = !isOnHike;
                if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    isPublish = true;
                    if (messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
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
                        updateLastMsgColor(mContactNumber);
                        isPublish = false;
                    }
                }
                ConvMessage cm = messagesList[i];
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    AddMessageToUI(cm, true);
                });
                if (count % 5 == 0)
                    Thread.Sleep(5);
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                Scroller.Opacity = 1;
                ScrollToBottom();
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
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj); // handle return to sender
                updateLastMsgColor(mContactNumber);
                isPublish = false;
            }
            if (!App.isConvCreated) // tombstone , chat thread not created , add GC members.
            {
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
                {
                    this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.IS_EXISTING_GROUP);
                    processGroupJoin(false);
                }
            }
        }

        private void ScrollToBottom()
        {
            MessageList.UpdateLayout();
            Scroller.UpdateLayout();
            Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
        }

        private void updateLastMsgColor(string msisdn)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ConversationsList.ConvMap[msisdn].MessageStatus = ConvMessage.State.RECEIVED_READ; // this is to notify ConvList.
            });
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

        private void callUser_Click(object sender, EventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();
            phoneCallTask.PhoneNumber = mContactNumber;
            phoneCallTask.DisplayName = mContactName;
            phoneCallTask.Show();
        }

        private void addUser_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(mContactNumber);
        }

        private void leaveGroup_Click(object sender, EventArgs e)
        {
            if (!ConversationsList.ConvMap.ContainsKey(mContactNumber))
                return;
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
            PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] = mContactNumber;
            PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = mContactName;
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
                {
                    mPubSub.publish(HikePubSub.UNBLOCK_USER, mContactNumber);

                    emoticonsIconButton.IsEnabled = true;
                    sendIconButton.IsEnabled = true;

                    isTypingNotificationEnabled = true;
                }
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
                {
                    mPubSub.publish(HikePubSub.BLOCK_USER, mContactNumber);

                    emoticonsIconButton.IsEnabled = false;
                    sendIconButton.IsEnabled = false;

                    isTypingNotificationEnabled = false;
                    emoticonPanel.Visibility = Visibility.Collapsed;
                }

                mUserIsBlocked = true;
                menuItem1.Text = UNBLOCK_USER;
                showOverlay(true); //true means show block animation
            }
        }

        private void FileAttachmentMessage_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        //        private void FileAttachmentMessage_Click(object sender, EventArgs e)
        {
            //            MessageBox.Show("Inside attachment tap");
            if (!isContextMenuTapped)
            {
                MyChatBubble chatBubble = (sender as MyChatBubble);
                if (chatBubble.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED && chatBubble.FileAttachment.FileState != Attachment.AttachmentState.STARTED)
                {
                    if (chatBubble is ReceivedChatBubble)
                    {
                        FileTransfer.Instance.downloadFile(chatBubble, mContactNumber);
                        MessagesTableUtils.addUploadingOrDownloadingMessage(chatBubble.MessageId);

                    }
                    else if (chatBubble is SentChatBubble)
                    {
                        //resend message
                        ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.UNKNOWN);
                        convMessage.IsSms = !isOnHike;
                        convMessage.HasAttachment = true;
                        convMessage.MessageId = chatBubble.MessageId;
                        convMessage.FileAttachment = chatBubble.FileAttachment;
                        convMessage.Message = HikeConstants.FILES_MESSAGE_PREFIX + convMessage.FileAttachment.FileKey;
                        object[] values = new object[2];
                        values[0] = convMessage;
                        values[1] = chatBubble;
                        mPubSub.publish(HikePubSub.ATTACHMENT_RESEND, values);
                    }
                }
                else
                {
                    displayAttachment(chatBubble, false);
                }
            }
            isContextMenuTapped = false;
        }

        public void displayAttachment(MyChatBubble chatBubble, bool shouldUpdateAttachment)
        {
            if (shouldUpdateAttachment)
            {
                MiscDBUtil.saveAttachmentObject(chatBubble.FileAttachment, mContactNumber, chatBubble.MessageId);
            }
            if (chatBubble.FileAttachment.ContentType.Contains("image"))
            {
                object[] fileTapped = new object[2];
                fileTapped[0] = chatBubble.MessageId;
                fileTapped[1] = mContactNumber;
                PhoneApplicationService.Current.State["objectForFileTransfer"] = fileTapped;
                NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
            }
            else if (chatBubble.FileAttachment.ContentType.Contains("audio") | chatBubble.FileAttachment.ContentType.Contains("video"))
            {
                MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + mContactNumber + "/" + Convert.ToString(chatBubble.MessageId);
                mediaPlayerLauncher.Media = new Uri(fileLocation, UriKind.Relative);
                mediaPlayerLauncher.Location = MediaLocationType.Data;
                mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                mediaPlayerLauncher.Show();
            }
        }

        public static long TempMessageId
        {
            get
            {
                return tempMsgId--;
            }
        }

        private void addNewAttachmentMessageToUI(SentChatBubble chatBubble)
        {
            if (isTypingNotificationActive)
            {
                HideTypingNotification();
                isReshowTypingNotification = true;
            }
            this.MessageList.Children.Add(chatBubble);
            chatBubble.setTapEvent(new EventHandler<GestureEventArgs>(FileAttachmentMessage_Tap));
            if (isReshowTypingNotification)
            {
                ShowTypingNotification();
                isReshowTypingNotification = false;
            }
            ScrollToBottom();
        }

        /*
         * If addToLast is true then insert the message in the end, else in the begining 
         */
        private void AddMessageToUI(ConvMessage convMessage, bool addToLast)
        {
            //TODO : Create attachment object if it requires one
            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
            {
                Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> contextMenuDictionary;
                if (convMessage.HasAttachment)
                {
                    if (convMessage.FileAttachment == null && attachments.ContainsKey(convMessage.MessageId))
                    {
                        convMessage.FileAttachment = attachments[convMessage.MessageId];
                        attachments.Remove(convMessage.MessageId);
                    }

                    switch (convMessage.FileAttachment.FileState)
                    {
                        case Attachment.AttachmentState.CANCELED:
                            contextMenuDictionary = AttachmentCanceledOrFailed;
                            break;
                        case Attachment.AttachmentState.COMPLETED:
                            contextMenuDictionary = AttachmentUploaded;
                            break;
                        default:
                            contextMenuDictionary = AttachmentUploading;
                            break;
                    }
                }
                else
                {
                    contextMenuDictionary = NonAttachmentMenu;
                }

                MyChatBubble chatBubble;
                if (convMessage.IsSent)
                {
                    chatBubble = new SentChatBubble(convMessage);
                    if (convMessage.MessageId < -1)
                        msgMap.Add(convMessage.MessageId, (SentChatBubble)chatBubble);
                    else
                        msgMap.Add(TempMessageId, (SentChatBubble)chatBubble);
                    //_convMessageSentBubbleMap.Add(convMessage, (SentChatBubble)chatBubble);
                }
                else
                {
                    chatBubble = new ReceivedChatBubble(convMessage);
                }
                if (addToLast)
                {
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                else
                {
                    this.MessageList.Children.Insert(0, chatBubble);
                }
                if (convMessage.FileAttachment != null)
                {
                    chatBubble.setTapEvent(new EventHandler<GestureEventArgs>(FileAttachmentMessage_Tap));
                }
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
            {
                string[] msisdns = splitUserJoinedMessage(convMessage);
                for (int i = 0; i < msisdns.Length; i++)
                {
                    GroupParticipant gp = Utils.getGroupParticipant("", msisdns[i]);
                    string text = HikeConstants.USER_JOINED;
                    if (!gp.IsOnHike)
                        text = HikeConstants.USER_INVITED;
                    MyChatBubble chatBubble = new NotificationChatBubble(gp.Name + text, true);
                    if (addToLast)
                    {
                        this.MessageList.Children.Add(chatBubble);
                        ScrollToBottom();
                    }
                    else
                    {
                        this.MessageList.Children.Insert(0, chatBubble);
                    }
                }
            }
            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT)
            {
                string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                MyChatBubble chatBubble = new NotificationChatBubble(name + HikeConstants.USER_LEFT, true);
                if (addToLast)
                {
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                else
                {
                    this.MessageList.Children.Insert(0, chatBubble);
                }
            }
            else
            {
                MyChatBubble chatBubble = new NotificationChatBubble(HikeConstants.GROUP_CHAT_END, true);
                if (addToLast)
                {
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                else
                {
                    this.MessageList.Children.Insert(0, chatBubble);
                }
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
            convMessage.MessageId = TempMessageId;
            sendMsg(convMessage, false);
            if (isFirstMsg)
            {
                //TODO : create new convMsg for invited and joined msg, you need group members info here
                //TODO : Show the invited and or waiting msg
                if (ConversationsList.ConvMap.ContainsKey(convMessage.Msisdn))
                {
                    ConversationListObject co = ConversationsList.ConvMap[convMessage.Msisdn]; // change the value in ConvObj also.
                    co.IsFirstMsg = false;
                    //TODO : Update other fields
                    object[] vals = new object[2];
                    vals[0] = convMessage;
                    vals[1] = true;
                    mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
                }
                PhoneApplicationService.Current.State.Remove(convMessage.Msisdn);
                isFirstMsg = false;
            }
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;

            if ((!isOnHike && mCredits <= 0))
                return;

            if (e.TaskResult == TaskResult.OK)
            {
                isReleaseMode = true;
                Uri uri = new Uri(e.OriginalFileName);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;
            }
            else
            {
                isReleaseMode = false;
                Uri uri = new Uri("/View/images/ic_phone_big.png", UriKind.Relative);
                BitmapImage image = new BitmapImage(uri);
                image.CreateOptions = BitmapCreateOptions.None;
                image.UriSource = uri;
                image.ImageOpened += imageOpenedHandler;

            }
        }

        //TODO remove these bools in release build. these are used because imageOpenHandler is called twice i debug
        private static bool abc = true;
        private static bool isReleaseMode = true;


        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            if (isReleaseMode || abc)
            {
                byte[] thumbnailBytes;
                byte[] fileBytes;
                BitmapImage image = (BitmapImage)sender;

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.UNKNOWN);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.MessageId = TempMessageId;

                WriteableBitmap writeableBitmap = new WriteableBitmap(image);

                using (var msSmallImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msSmallImage, 150, 150, 0, 90);
                    thumbnailBytes = msSmallImage.ToArray();
                }

                string fileName = image.UriSource.ToString();
                fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

                convMessage.FileAttachment = new Attachment(fileName, thumbnailBytes, Attachment.AttachmentState.STARTED);
                convMessage.Message = fileName;

                SentChatBubble chatBubble = new SentChatBubble(convMessage, image);
                msgMap.Add(convMessage.MessageId, chatBubble);

                addNewAttachmentMessageToUI(chatBubble);

                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, image.PixelWidth, image.PixelHeight, 0, 100);
                    fileBytes = msLargeImage.ToArray();
                }
                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = fileBytes;
                vals[2] = chatBubble;
                mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
            }
            abc = !abc;
        }

        public static string[] splitUserJoinedMessage(ConvMessage convMessage)
        {
            string[] names = null;
            string[] tokens = null;
            if (convMessage.Message.IndexOf(',') == -1) // only one name in message ex "abc joined the group chat"
            {
                int spaceIndex = convMessage.Message.IndexOf(" ");
                names = new string[1];
                names[0] = convMessage.Message.Substring(0, spaceIndex);
                return names;
            }

            else
                tokens = convMessage.Message.Split(','); // ex : "a,b joined the group chat"
            int i = 0;
            names = new string[tokens.Length];
            for (; i < tokens.Length; i++)
            {
                names[i] = tokens[i].Trim();
            }
            i--;
            names[i] = names[i].Substring(0, names[i].IndexOf(' '));
            return names;
        }

        private void sendMsg(ConvMessage convMessage, bool isNewGroup)
        {
            if (isNewGroup)
            {
                PhoneApplicationService.Current.State[mContactNumber] = mContactName;
                JObject metaData = new JObject();
                metaData[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN;
                convMessage.MetaDataString = metaData.ToString(Newtonsoft.Json.Formatting.None);
            }
            if (isTypingNotificationActive)
            {
                HideTypingNotification();
                isReshowTypingNotification = true;
            }
            AddMessageToUI(convMessage, true);
            if (isReshowTypingNotification)
            {
                ShowTypingNotification();
                isReshowTypingNotification = false;
            }

            object[] vals = new object[2];
            vals[0] = convMessage;
            vals[1] = isNewGroup;

            mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
        }

        private void sendMsgTxtbox_GotFocus(object sender, RoutedEventArgs e)
        {
            sendMsgTxtbox.Background = textBoxBackground;
        }

        #endregion

        #region CONTEXT MENU
        //TODO - 1) replace click events with tap
        // 2) Add delete event

        private void MenuItem_Click_Forward(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            //MessageBox.Show("Inside forward");
            isContextMenuTapped = true;
            MyChatBubble chatBubble = ((sender as MenuItem).DataContext as MyChatBubble);
            if (chatBubble.FileAttachment == null)
            {
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = chatBubble.Text;
                NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            }
            else
            {
                object[] attachmentForwardMessage = new object[2];
                attachmentForwardMessage[0] = chatBubble;
                attachmentForwardMessage[1] = mContactNumber;
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = attachmentForwardMessage;
                NavigationService.Navigate(new Uri("/View/SelectUserToMsg.xaml", UriKind.Relative));
            }
        }

        private void MenuItem_Click_Copy(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            MyChatBubble chatBubble = ((sender as MenuItem).DataContext as MyChatBubble);
            Clipboard.SetText(chatBubble.Text);
        }

        private void MenuItem_Click_Delete(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            //MessageBox.Show("Inside delete");

            MyChatBubble msg = ((sender as MenuItem).DataContext as MyChatBubble);

            if (msg == null)
            {
                return;
            }
            bool delConv = false;
            this.MessageList.Children.Remove(msg);

            ConversationListObject obj = ConversationsList.ConvMap[mContactNumber];
            /* Remove the message from conversation list */
            //if (this.ChatThreadPageCollection.Count > 0)


            MyChatBubble lastMessageBubble = null;
            if (isTypingNotificationActive && this.MessageList.Children.Count > 1)
            {
                lastMessageBubble = this.MessageList.Children[this.MessageList.Children.Count - 2] as MyChatBubble;
            }
            else if (!isTypingNotificationActive && this.MessageList.Children.Count > 0)
            {
                lastMessageBubble = this.MessageList.Children[this.MessageList.Children.Count - 1] as MyChatBubble;
            }

            if (lastMessageBubble != null)
            {
                //This updates the Conversation list.
                if (lastMessageBubble.FileAttachment != null)
                    obj.LastMessage = lastMessageBubble.FileAttachment.FileName;
                else
                    obj.LastMessage = lastMessageBubble.Text;
                //obj.MessageStatus = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].MessageStatus;
                //obj.TimeStamp = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].TimeStampLong;
                obj.MessageStatus = lastMessageBubble.MessageStatus;
                obj.TimeStamp = lastMessageBubble.TimeStampLong;
            }
            else
            {
                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj);
                ConversationsList.ConvMap.Remove(mContactNumber);
                delConv = true;
            }
            object[] o = new object[3];
            o[0] = msg.MessageId;
            o[1] = obj;
            o[2] = delConv;
            mPubSub.publish(HikePubSub.MESSAGE_DELETED, o);
        }

        private void MenuItem_Click_Cancel(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            MyChatBubble chatBubble = ((sender as MenuItem).DataContext as MyChatBubble);
            chatBubble.setAttachmentState(Attachment.AttachmentState.CANCELED);
        }

        #endregion

        #region EMOTICONS RELATED STUFF

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            object s = e.OriginalSource;
        }

        private void emoticonButton_Click(object sender, EventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Visible;
        }

        private void fileTransferButton_Click(object sender, EventArgs e)
        {
            try
            {
                photoChooserTask.Show();
            }
            catch (System.InvalidOperationException ex)
            {
                MessageBox.Show("An error occurred.");
            }
        }


        private void chatListBox_tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void emoticonPanel_LostFocus(object sender, RoutedEventArgs e)
        {
            //emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void emotList0_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList0.SelectedIndex;
            sendMsgTxtbox.Text += SmileyParser.emoticonStrings[index];
            emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList1.SelectedIndex + SmileyParser.emoticon0Size;
            sendMsgTxtbox.Text += SmileyParser.emoticonStrings[index];
            emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList2.SelectedIndex + SmileyParser.emoticon0Size + SmileyParser.emoticon1Size;
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
                //LayoutRoot.Background = blackBackground;
                //HikeTitle.Opacity = 0.25;
                //MessageList.Opacity = 0.25;
                //bottomPanel.Opacity = 0.25;
                overlayRectangle.Visibility = System.Windows.Visibility.Visible;
                overlayRectangle.Opacity = 0.85;
                HikeTitle.IsHitTestVisible = false;
                MessageList.IsHitTestVisible = false;
                bottomPanel.IsHitTestVisible = false;
                OverlayMessagePanel.Visibility = Visibility.Visible;
                emoticonsIconButton.IsEnabled = false;
                sendIconButton.IsEnabled = false;
                //                sendMsgTxtbox.IsHitTestVisible = false;
            }
            else
            {
                //LayoutRoot.Background = whiteBackground;
                //HikeTitle.Opacity = 1;
                //MessageList.Opacity = 1;
                //bottomPanel.Opacity = 1;
                overlayRectangle.Visibility = System.Windows.Visibility.Collapsed;


                HikeTitle.IsHitTestVisible = true;
                MessageList.IsHitTestVisible = true;
                bottomPanel.IsHitTestVisible = true;
                OverlayMessagePanel.Visibility = Visibility.Collapsed;
                emoticonsIconButton.IsEnabled = true;
                sendIconButton.IsEnabled = true;
                //                sendMsgTxtbox.IsHitTestVisible = true;
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

        private void ShowTypingNotification()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (isTypingNotificationEnabled && !isTypingNotificationActive)
                    this.MessageList.Children.Add(typingNotificationImage);
                isTypingNotificationActive = true;
                ScrollToBottom();
            });
        }

        private void HideTypingNotification()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!isTypingNotificationEnabled || isTypingNotificationActive)
                    this.MessageList.Children.Remove(typingNotificationImage);
                if (isTypingNotificationActive)
                    isTypingNotificationActive = false;
            });
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
                    mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, new long[] { convMessage.MessageId });
                    if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO) // do not notify in case of group end , user left , user joined
                    {
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serializeDeliveryReportRead()); // handle return to sender
                    }
                    updateLastMsgColor(convMessage.Msisdn);
                    // Update UI
                    HideTypingNotification();
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        AddMessageToUI(convMessage, true);
                    });
                }
                else // this is to show toast notification
                {
                    ConversationListObject cObj = vals[1] as ConversationListObject;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ToastPrompt toast = new ToastPrompt();
                        if (cObj.ContactName != null)
                            toast.Title = cObj.ContactName;
                        else
                            toast.Title = cObj.Msisdn;
                        toast.Message = convMessage.Message;
                        toast.ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute));
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
                    SentChatBubble msg = msgMap[msgId];
                    if (msg != null)
                    {
                        //                        msg.MessageStatus = ConvMessage.State.SENT_CONFIRMED;
                        msg.SetSentMessageStatus(ConvMessage.State.SENT_CONFIRMED);
                    }
                }
                catch (KeyNotFoundException e)
                {
                    //logger.Info("NewChatThread", "Message Delivered Read Exception " + e);
                }
            }

            #endregion

            #region MESSAGE_DELIVERED

            else if (HikePubSub.MESSAGE_DELIVERED == type)
            {
                long msgId = (long)obj;
                try
                {
                    SentChatBubble msg = msgMap[msgId];
                    if (msg != null)
                    {
                        msg.SetSentMessageStatus(ConvMessage.State.SENT_DELIVERED);
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
                        SentChatBubble msg = msgMap[ids[i]];
                        if (msg != null)
                        {
                            msg.SetSentMessageStatus(ConvMessage.State.SENT_DELIVERED_READ);
                            msgMap.Remove(ids[i]);
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
                            App.WriteToIsoStorageSettings(HikeConstants.Extras.ANIMATED_ONCE, true);
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
                    ShowTypingNotification();
                }
            }

            #endregion

            #region END_TYPING_CONVERSATION

            else if (HikePubSub.END_TYPING_CONVERSATION == type)
            {
                if (mContactNumber == (obj as string))
                {
                    HideTypingNotification();
                }
            }

            #endregion

            #region UPDATE_UI

            else if (HikePubSub.UPDATE_UI == type)
            {
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                if (msisdn != mContactNumber)
                    return;
                byte[] _avatar = (byte[])vals[1];
                if (_avatar == null)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    using (var memStream = new MemoryStream(_avatar))
                    {
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage empImage = new BitmapImage(); // here we can resuse existing image (how ??)
                        empImage.SetSource(memStream);
                        userImage.Source = empImage;
                    }
                });
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
            sendMsgTxtbox.IsHitTestVisible = false;
            appBar.IsMenuEnabled = false;
            sendIconButton.IsEnabled = false;
            emoticonsIconButton.IsEnabled = false;
        }

        #endregion

        // this should be called when one gets tap here msg.
        private void smsUser_Click(object sender, EventArgs e)
        {
            SmsComposeTask sms = new Microsoft.Phone.Tasks.SmsComposeTask();
            sms.To = mContactNumber; // set phone number
            sms.Body = ""; // set body
            sms.Show();
        }

        private void emotHeaderRect0_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderBorder0.Opacity = 1;
            emotHeaderRect0.Opacity = 1;
            emotHeaderBorder1.Opacity = 0;
            emotHeaderRect1.Opacity = 0;
            emotHeaderBorder2.Opacity = 0;
            emotHeaderRect2.Opacity = 0;
            emoticonPivot.SelectedIndex = 0;
        }

        private void emotHeaderRect1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderBorder0.Opacity = 0;
            emotHeaderRect0.Opacity = 0;
            emotHeaderBorder1.Opacity = 1;
            emotHeaderRect1.Opacity = 1;
            emotHeaderBorder2.Opacity = 0;
            emotHeaderRect2.Opacity = 0;
            emoticonPivot.SelectedIndex = 1;

        }

        private void emotHeaderRect2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderBorder0.Opacity = 0;
            emotHeaderRect0.Opacity = 0;
            emotHeaderBorder1.Opacity = 0;
            emotHeaderRect1.Opacity = 0;
            emotHeaderBorder2.Opacity = 1;
            emotHeaderRect2.Opacity = 1;
            emoticonPivot.SelectedIndex = 2;
            string name = this.Name;
        }

        private void MessageList_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void emoticonPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (emoticonPivot.SelectedIndex)
            {
                case 0:
                    emotHeaderBorder0.Opacity = 1;
                    emotHeaderRect0.Opacity = 1;
                    emotHeaderBorder1.Opacity = 0;
                    emotHeaderRect1.Opacity = 0;
                    emotHeaderBorder2.Opacity = 0;
                    emotHeaderRect2.Opacity = 0;
                    break;
                case 1:
                    emotHeaderBorder0.Opacity = 0;
                    emotHeaderRect0.Opacity = 0;
                    emotHeaderBorder1.Opacity = 1;
                    emotHeaderRect1.Opacity = 1;
                    emotHeaderBorder2.Opacity = 0;
                    emotHeaderRect2.Opacity = 0;
                    break;
                case 2:
                    emotHeaderBorder0.Opacity = 0;
                    emotHeaderRect0.Opacity = 0;
                    emotHeaderBorder1.Opacity = 0;
                    emotHeaderRect1.Opacity = 0;
                    emotHeaderBorder2.Opacity = 1;
                    emotHeaderRect2.Opacity = 1;
                    break;
            }
        }
    }
}