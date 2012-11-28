using System;
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
using System.Text;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Media;
using System.Device.Location;
using windows_client.Misc;

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
        public string mContactNumber;
        private string mContactName = null;
        private string lastText = "";

        private bool isMute = false;
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
        private JObject groupCreateJson = null;
        private Dictionary<long, Attachment> attachments = null; //this map is required for mapping attachment object with convmessage only for
        //messages stored in db, other messages would have their attachment object set

        private static long tempMsgId = -2;

        private int msgBubbleCount = 0;
        private int mCredits;
        private long lastTextChangedTime;
        private long lastTypingNotificationShownTime;

        private HikePubSub mPubSub;
        private IScheduler scheduler = Scheduler.NewThread;

        private ApplicationBar appBar;
        ApplicationBarMenuItem blockUnblockMenuItem;
        ApplicationBarMenuItem muteGroupMenuItem;
        ApplicationBarMenuItem inviteMenuItem = null;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton emoticonsIconButton = null;
        ApplicationBarIconButton fileTransferIconButton = null;
        private PhotoChooserTask photoChooserTask;
        private BingMapsTask bingMapsTask = null;

        private ObservableCollection<MyChatBubble> chatThreadPageCollection = new ObservableCollection<MyChatBubble>();
        private Dictionary<long, SentChatBubble> msgMap = new Dictionary<long, SentChatBubble>(); // this holds msgId -> sent message bubble mapping
        //private Dictionary<ConvMessage, SentChatBubble> _convMessageSentBubbleMap = new Dictionary<ConvMessage, SentChatBubble>(); // this holds msgId -> sent message bubble mapping

        private List<ConvMessage> incomingMessages = new List<ConvMessage>();
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _nonAttachmentMenu;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentUploading;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentUploadedorDownloaded;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentUploadCanceledOrFailed = null;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentDownloading;
        private Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> _attachmentDownloadCanceledOrFailed = null;
        #endregion

        #region UI VALUES

        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
        private Thickness imgMargin = new Thickness(0, 5, 0, 15);
        private Image typingNotificationImage;
        private ApplicationBarMenuItem groupInfoMenuItem;
        #endregion

        private BitmapImage[] imagePathsForList0
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForList0;
            }
        }

        private BitmapImage[] imagePathsForList1
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForList1;
            }
        }

        private BitmapImage[] imagePathsForList2
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForList2;
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
                    //_attachmentUploading.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentUploading;
            }
            set
            {
                _attachmentUploading = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentUploadedOrDownloaded
        {
            get
            {
                if (_attachmentUploadedorDownloaded == null)
                {
                    _attachmentUploadedorDownloaded = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentUploadedorDownloaded.Add("copy", MenuItem_Click_Copy);
                    _attachmentUploadedorDownloaded.Add("forward", MenuItem_Click_Forward);
                    _attachmentUploadedorDownloaded.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentUploadedorDownloaded;
            }
            set
            {
                _attachmentUploadedorDownloaded = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentUploadCanceledOrFailed
        {
            get
            {
                if (_attachmentUploadCanceledOrFailed == null)
                {
                    _attachmentUploadCanceledOrFailed = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentUploadCanceledOrFailed.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentUploadCanceledOrFailed;
            }
            set
            {
                _attachmentUploadCanceledOrFailed = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentDownloading
        {
            get
            {
                if (_attachmentDownloading == null)
                {
                    _attachmentDownloading = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentDownloading.Add("copy", MenuItem_Click_Copy);
                    _attachmentDownloading.Add("cancel", MenuItem_Click_Cancel);
                }
                return _attachmentDownloading;
            }
            set
            {
                _attachmentDownloading = value;
            }
        }

        public Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>> AttachmentDownloadCanceledOrFailed
        {
            get
            {
                if (_attachmentDownloadCanceledOrFailed == null)
                {
                    _attachmentDownloadCanceledOrFailed = new Dictionary<string, EventHandler<Microsoft.Phone.Controls.GestureEventArgs>>();
                    _attachmentDownloadCanceledOrFailed.Add("copy", MenuItem_Click_Copy);
                    _attachmentDownloadCanceledOrFailed.Add("delete", MenuItem_Click_Delete);
                }
                return _attachmentDownloadCanceledOrFailed;
            }
            set
            {
                _attachmentDownloadCanceledOrFailed = value;
            }
        }

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
            bool isGC = false;
            mPubSub = App.HikePubSubInstance;
            initPageBasedOnState();
            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;
            if (this.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                this.State.Remove(HikeConstants.GROUP_CHAT);
                isGC = true;
            }
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                Stopwatch st = Stopwatch.StartNew();
                attachments = MiscDBUtil.getAllFileAttachment(mContactNumber);
                //attachments = new Dictionary<long, Attachment>();
                loadMessages();
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to load chat messages for msisdn {0} : {1}", mContactNumber, msec);
                if (isGC)
                {
                    ConvMessage groupCreateCM = new ConvMessage(groupCreateJson, true, false);
                    groupCreateCM.GroupParticipant = groupOwner;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        sendMsg(groupCreateCM, true);
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, groupCreateJson); // inform others about group
                    });

                }
                App.appSettings.TryGetValue(App.SMS_SETTING, out mCredits);
                registerListeners();
                NetworkManager.turnOffNetworkManager = false;
                App.MqttManagerInstance.connect();
            };
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
            bw.RunWorkerAsync();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = true;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            #region PUSH NOTIFICATION
            // push notification , needs to be handled just once.
            if (this.NavigationContext.QueryString.ContainsKey("msisdn"))
            {
                string msisdn = (this.NavigationContext.QueryString["msisdn"] as string).Trim();
                this.NavigationContext.QueryString.Clear();
                if (Char.IsDigit(msisdn[0]))
                    msisdn = "+" + msisdn;

                //MessageBox.Show(msisdn, "NEW CHAT", MessageBoxButton.OK);
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                    this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = App.ViewModel.ConvMap[msisdn];
                else if (Utils.isGroupConversation(msisdn))
                {
                    ConversationListObject co = new ConversationListObject();
                    co.ContactName = "New Group";
                    co.Msisdn = msisdn;
                    co.IsOnhike = true;
                    this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = co;
                }
                else
                {
                    ContactInfo contact = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    if (contact == null)
                    {
                        contact = new ContactInfo();
                        contact.Msisdn = msisdn;
                        contact.Name = null;
                        contact.OnHike = true; // this is assumed bcoz there is very less chance for an sms user to send push
                    }
                    this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = contact;
                }
                ManagePage();
                isFirstLaunch = false;
            }
            #endregion
            #region SHARE PICKER
            // share picker , needs to be handled just once
            else if (PhoneApplicationService.Current.State.ContainsKey("SharePicker")) // this will be removed after sending msg
            {
                ManagePageStateObjects();
                ManagePage();
                isFirstLaunch = false;
            }
            #endregion
            #region TOMBSTONE HANDLING
            else if (App.IS_TOMBSTONED)
            {
                if (isFirstLaunch) // if first time launching after tombstone
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
            #region NORMAL LAUNCH
            else if (App.APP_LAUNCH_STATE == App.LaunchState.NORMAL_LAUNCH) // non tombstone case
            //else
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

            #endregion
            App.newChatThreadPage = this;

            #region AUDIO FT
            if (!App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
            {
                AudioFileTransfer();
            }
            #endregion
            #region SHARE LOCATION
            if (!App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARED_LOCATION))
            {
                shareLocation();
            }
            #endregion
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);
            if (!string.IsNullOrWhiteSpace(sendMsgTxtbox.Text))
                this.State["sendMsgTxtbox.Text"] = sendMsgTxtbox.Text;
            else
                this.State.Remove("sendMsgTxtbox.Text");
            App.IS_TOMBSTONED = false;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);
            removeListeners();

            if (App.newChatThreadPage == this)
                App.newChatThreadPage = null;
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (emoticonPanel.Visibility == Visibility.Visible)
            {
                emoticonPanel.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }
            if (attachmentMenu.Visibility == Visibility.Visible)
            {
                attachmentMenu.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }

            if (App.APP_LAUNCH_STATE != App.LaunchState.NORMAL_LAUNCH) //  in this case back would go to conversation list
            {
                Uri nUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                NavigationService.Navigate(nUri);
            }
            base.OnBackKeyPress(e);
        }

        #endregion

        #region INIT PAGE BASED ON STATE

        private void initPageBasedOnState()
        {
            GroupInfo gi = null;
            bool isAddUser = false;
            #region OBJECT FROM CONVLIST PAGE

            if (this.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE)) // represents NewChatThread is called from convlist page
            {
                ConversationListObject convObj = (ConversationListObject)this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                mContactNumber = convObj.Msisdn;

                if (Utils.isGroupConversation(mContactNumber)) // represents group chat
                {
                    GroupManager.Instance.LoadGroupParticipants(mContactNumber);
                    isGroupChat = true;
                    BlockTxtBlk.Text = "You have blocked this group. Unblock to continue hiking";
                    gi = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    if (gi != null)
                        groupOwner = gi.GroupOwner;
                    if (gi != null && !gi.GroupAlive)
                        isGroupAlive = false;
                    isMute = convObj.IsMute;
                }

                if (convObj.ContactName != null)
                    mContactName = convObj.ContactName;
                else
                {
                    mContactName = convObj.Msisdn;
                    isAddUser = true;
                }

                isOnHike = convObj.IsOnhike;
                if (App.IS_TOMBSTONED) // in this case avatar needs to be re calculated
                {
                    convObj.Avatar = MiscDBUtil.getThumbNailForMsisdn(mContactNumber);
                }
                userImage.Source = convObj.AvatarImage;
            }

            #endregion
            #region OBJECT FROM SELECT GROUP PAGE

            else if (this.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                // here always create a new group
                string uid = (string)App.appSettings[App.UID_SETTING];
                mContactNumber = uid + ":" + TimeUtils.getCurrentTimeStamp();
                groupOwner = App.MSISDN;
                processGroupJoin(true);
                isOnHike = true;
                isGroupChat = true;
                userImage.Source = UI_Utils.Instance.DefaultGroupImage;

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
                if (obj.Name != null)
                    mContactName = obj.Name;
                else
                {
                    mContactName = obj.Msisdn;
                    isAddUser = true;
                }

                isOnHike = obj.OnHike;

                /* Check if it is a forwarded msg */
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
                {
                    NavigationService.RemoveBackEntry(); // remove last chat thread page
                    if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is string)
                    {
                        sendMsgTxtbox.Text = (string)PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                        PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
                    }
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
                //    userImage.Source = UI_Utils.Instance.Instance.DefaultAvatarBitmapImage;
            }
            #endregion

            userName.Text = mContactName;
            if (groupOwner != null)
                mUserIsBlocked = UsersTableUtils.isUserBlocked(groupOwner);
            else
                mUserIsBlocked = UsersTableUtils.isUserBlocked(mContactNumber);
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
            initBlockUnblockState();
        }

        private void processGroupJoin(bool isNewgroup)
        {
            List<ContactInfo> contactsForGroup = this.State[HikeConstants.GROUP_CHAT] as List<ContactInfo>;
            List<GroupParticipant> usersToAdd = new List<GroupParticipant>(5); // this is used to select only those contacts which should be later added.

            if (isNewgroup) // if new group add all members to the group
            {
                List<GroupParticipant> l = new List<GroupParticipant>(contactsForGroup.Count);
                for (int i = 0; i < contactsForGroup.Count; i++)
                {
                    GroupParticipant gp = new GroupParticipant(mContactNumber, contactsForGroup[i].Name, contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike);
                    l.Add(gp);
                    usersToAdd.Add(gp);
                }
                GroupManager.Instance.GroupCache[mContactNumber] = l;
            }
            else // existing group so just add members
            {
                for (int i = 0; i < contactsForGroup.Count; i++)
                {
                    GroupParticipant gp = null;
                    bool addNewparticipant = true;
                    List<GroupParticipant> gl = GroupManager.Instance.GroupCache[mContactNumber];
                    if (gl == null)
                        gl = new List<GroupParticipant>();

                    for (int j = 0; j < gl.Count; j++)
                    {
                        if (gl[j].Msisdn == contactsForGroup[i].Msisdn) // participant exists and has left earlier
                        {
                            gl[j].HasLeft = false;
                            gl[j].Name = contactsForGroup[i].Name;
                            gl[j].IsOnHike = contactsForGroup[i].OnHike;
                            addNewparticipant = false;
                            gp = gl[j];
                            break;
                        }
                    }
                    if (addNewparticipant)
                    {
                        gp = new GroupParticipant(mContactNumber, contactsForGroup[i].Name, contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike);
                        GroupManager.Instance.GroupCache[mContactNumber].Add(gp);
                    }
                    usersToAdd.Add(gp);
                }
            }

            GroupManager.Instance.GroupCache[mContactNumber].Sort();
            usersToAdd.Sort();
            GroupManager.Instance.SaveGroupCache(mContactNumber);
            //App.WriteToIsoStorageSettings(App.GROUPS_CACHE, GroupManager.Instance.GroupCache);
            groupCreateJson = createGroupJsonPacket(HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN, usersToAdd);
            if (isNewgroup)
                mContactName = GroupManager.Instance.defaultGroupName(mContactNumber);
            else
            {
                GroupInfo gif = GroupTableUtils.getGroupInfoForId(mContactNumber);
                if (gif != null && string.IsNullOrEmpty(gif.GroupName))
                    mContactName = GroupManager.Instance.defaultGroupName(mContactNumber);
            }
            userName.Text = mContactName;
            if (isNewgroup)
            {
                BackgroundWorker bw = new BackgroundWorker();

                bw.DoWork += (ss, ee) =>
                {
                    GroupInfo gi = new GroupInfo(mContactNumber, null, groupOwner, true);
                    GroupTableUtils.addGroupInfo(gi);
                };
                bw.RunWorkerAsync();
            }
            else
            {
                ConvMessage cm = new ConvMessage(groupCreateJson, true, true);
                sendMsg(cm, true);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, groupCreateJson); // inform others about group
            }
        }

        private JObject createGroupJsonPacket(string type, List<GroupParticipant> usersToAdd)
        {
            JObject obj = new JObject();
            try
            {
                obj[HikeConstants.TYPE] = type;
                obj[HikeConstants.TO] = mContactNumber;
                if (type == (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN))
                {
                    JArray array = new JArray();
                    for (int i = 0; i < usersToAdd.Count; i++)
                    {
                        JObject nameMsisdn = new JObject();
                        nameMsisdn[HikeConstants.NAME] = usersToAdd[i].Name;
                        nameMsisdn[HikeConstants.MSISDN] = usersToAdd[i].Msisdn;
                        array.Add(nameMsisdn);
                    }
                    //if (isNewGroup) // if new group add owners info also
                    //{
                    //    JObject nameMsisdn = new JObject();
                    //    nameMsisdn[HikeConstants.NAME] = (string)App.appSettings[App.ACCOUNT_NAME];
                    //    nameMsisdn[HikeConstants.MSISDN] = App.MSISDN;
                    //    array.Add(nameMsisdn);
                    //}

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
                groupInfoMenuItem = new ApplicationBarMenuItem();
                groupInfoMenuItem.Text = "group info";
                groupInfoMenuItem.Click += new EventHandler(groupInfo_Click);
                appBar.MenuItems.Add(groupInfoMenuItem);

                ApplicationBarMenuItem leaveMenuItem = new ApplicationBarMenuItem();
                leaveMenuItem.Text = "leave group";
                leaveMenuItem.Click += new EventHandler(leaveGroup_Click);
                appBar.MenuItems.Add(leaveMenuItem);

                //TODO : Uncomment this when mute has to be supported.
                //muteGroupMenuItem = new ApplicationBarMenuItem();
                //muteGroupMenuItem.Text = isMute ? "unmute group" : "mute group";
                //muteGroupMenuItem.Click += new EventHandler(muteUnmuteGroup_Click);
                //appBar.MenuItems.Add(muteGroupMenuItem);

                if (groupOwner != null)
                {
                    if (groupOwner != App.MSISDN) // represents current user is not group owner
                    {
                        blockUnblockMenuItem = new ApplicationBarMenuItem();
                        if (mUserIsBlocked)
                        {
                            groupInfoMenuItem.IsEnabled = false;
                            blockUnblockMenuItem.Text = UNBLOCK_USER + " group owner";
                        }
                        else
                        {
                            blockUnblockMenuItem.Text = BLOCK_USER + " group owner";
                        }
                        blockUnblockMenuItem.Click += new EventHandler(blockUnblock_Click);
                        appBar.MenuItems.Add(blockUnblockMenuItem);
                    }
                }
            }
            else
            {

                blockUnblockMenuItem = new ApplicationBarMenuItem();
                if (mUserIsBlocked)
                {
                    blockUnblockMenuItem.Text = UNBLOCK_USER;
                }
                else
                {
                    blockUnblockMenuItem.Text = BLOCK_USER;
                }
                blockUnblockMenuItem.Click += new EventHandler(blockUnblock_Click);
                appBar.MenuItems.Add(blockUnblockMenuItem);

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
            if (mUserIsBlocked)
                inviteMenuItem.IsEnabled = false;
        }

        #endregion

        #endregion

        #region BACKGROUND WORKER

        private void loadMessages()
        {
            int i;
            bool isPublish = false;
            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber);
            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    Scroller.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    forwardAttachmentMessage();
                });
                NetworkManager.turnOffNetworkManager = false;
                return;
            }

            JArray ids = new JArray();
            List<long> dbIds = new List<long>();
            int count = 0;
            for (i = 0; i < messagesList.Count; i++)
            {
                count++;
                if (count % 5 == 0)
                    Thread.Sleep(5);
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
            if (App.IS_TOMBSTONED) // tombstone , chat thread not created , add GC members.
            {
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
                {
                    this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.IS_EXISTING_GROUP);
                    processGroupJoin(false);
                }
            }
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                forwardAttachmentMessage();
                Scroller.Opacity = 1;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                ScrollToBottom();
                scheduler.Schedule(ScrollToBottomFromUI, TimeSpan.FromMilliseconds(5));
                NetworkManager.turnOffNetworkManager = false;
            });
        }

        private void forwardAttachmentMessage()
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG) &&
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is object[])
            {
                object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                MyChatBubble chatBubble = (MyChatBubble)attachmentData[0];
                string sourceMsisdn = (string)attachmentData[1];

                string sourceFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn + "/" + chatBubble.MessageId;

                string messageText = "";
                if (chatBubble.FileAttachment.ContentType.Contains("image"))
                    messageText = "image";
                else if (chatBubble.FileAttachment.ContentType.Contains("audio"))
                    messageText = "audio";
                else if (chatBubble.FileAttachment.ContentType.Contains("video"))
                    messageText = "video";

                ConvMessage convMessage = new ConvMessage(messageText, mContactNumber,
                    TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.MessageId = TempMessageId;
                convMessage.FileAttachment = chatBubble.FileAttachment;
                convMessage.IsSms = !isOnHike;
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;

                //SentChatBubble newChatBubble = new SentChatBubble(convMessage, false);
                SentChatBubble newChatBubble = SentChatBubble.getSplitChatBubbles(convMessage, false);

                newChatBubble.SetSentMessageStatusForUploadedAttachments();

                newChatBubble.setAttachmentState(Attachment.AttachmentState.COMPLETED);
                addNewAttachmentMessageToUI(newChatBubble);
                msgMap.Add(convMessage.MessageId, newChatBubble);

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = sourceFilePath;
                mPubSub.publish(HikePubSub.FORWARD_ATTACHMENT, vals);
                PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey("SharePicker"))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    string token = PhoneApplicationService.Current.State["SharePicker"] as string;
                    BitmapImage bitmap = bitmap = new BitmapImage(); ;
                    try
                    {
                        MediaLibrary library = new MediaLibrary();
                        Picture picture = library.GetPictureFromToken(token);
                        // Create a WriteableBitmap object and add it to the Image control Source property.
                        bitmap.CreateOptions = BitmapCreateOptions.None;
                        bitmap.SetSource(picture.GetImage());
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Chat Thread :: Exception : " + ex.StackTrace);
                    }
                    SendImage(bitmap, token);
                    PhoneApplicationService.Current.State.Remove("SharePicker");
                });
            }
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
                AudioFileTransfer();
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARED_LOCATION))
            {
                shareLocation();
            }

        }

        private void ScrollToBottomFromUI()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ScrollToBottom();
            });
        }

        private void ScrollToBottom()
        {
            MessageList.UpdateLayout();
            Scroller.UpdateLayout();
            if (!isMute || msgBubbleCount < App.ViewModel.ConvMap[mContactNumber].MuteVal)
                Scroller.ScrollToVerticalOffset(Scroller.ScrollableHeight);
        }

        private void updateLastMsgColor(string msisdn)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                    App.ViewModel.ConvMap[msisdn].MessageStatus = ConvMessage.State.RECEIVED_READ; // this is to notify ConvList.
            });
        }

        private void initBlockUnblockState()
        {
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
            mPubSub.addListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
        }

        private void removeListeners()
        {
            try
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
                mPubSub.removeListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
            }
            catch { }
        }
        #endregion

        #region APPBAR CLICK EVENTS

        private void callUser_Click(object sender, EventArgs e)
        {
            PhoneCallTask phoneCallTask = new PhoneCallTask();
            phoneCallTask.PhoneNumber = mContactNumber;
            phoneCallTask.DisplayName = mContactName;
            try
            {
                phoneCallTask.Show();
            }
            catch { }
        }

        private void addUser_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(mContactNumber);
        }

        private void leaveGroup_Click(object sender, EventArgs e)
        {
            if (!App.ViewModel.ConvMap.ContainsKey(mContactNumber))
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
            ConversationListObject cObj = App.ViewModel.ConvMap[mContactNumber];
            App.ViewModel.MessageListPageCollection.Remove(cObj);
            App.ViewModel.ConvMap.Remove(mContactNumber);

            mPubSub.publish(HikePubSub.GROUP_LEFT, mContactNumber);
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else // case when this page is opened through push notification or share picker
            {
                Uri nUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                NavigationService.Navigate(nUri);
            }
        }

        private void muteUnmuteGroup_Click(object sender, EventArgs e)
        {
            JObject obj = new JObject();
            JObject o = new JObject();
            o["id"] = mContactNumber;
            obj[HikeConstants.DATA] = o;
            if (isMute) // GC is muted , request to unmute
            {
                isMute = false;
                obj[HikeConstants.TYPE] = "unmute";
                App.ViewModel.ConvMap[mContactNumber].MuteVal = -1;
                muteGroupMenuItem.Text = "mute group";
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
            else // GC is unmute , request to mute
            {
                isMute = true;
                obj[HikeConstants.TYPE] = "mute";
                App.ViewModel.ConvMap[mContactNumber].MuteVal = msgBubbleCount;
                muteGroupMenuItem.Text = "unmute group";
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
            }

        }

        private void groupInfo_Click(object sender, EventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.GROUP_INFO);
            PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] = mContactNumber;
            PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = mContactName;
            Dispatcher.BeginInvoke(() =>
            {
                NavigationService.Navigate(new Uri("/View/GroupInfoPage.xaml", UriKind.Relative));
            });
        }

        private void blockUnblock_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked) // UNBLOCK REQUEST
            {
                if (isGroupChat)
                {
                    mPubSub.publish(HikePubSub.UNBLOCK_GROUPOWNER, groupOwner);
                    blockUnblockMenuItem.Text = BLOCK_USER + " group owner";
                    groupInfoMenuItem.IsEnabled = true;
                }
                else
                {
                    mPubSub.publish(HikePubSub.UNBLOCK_USER, mContactNumber);
                    emoticonsIconButton.IsEnabled = true;
                    sendIconButton.IsEnabled = true;
                    isTypingNotificationEnabled = true;
                    blockUnblockMenuItem.Text = BLOCK_USER;
                    if (inviteMenuItem != null)
                        inviteMenuItem.IsEnabled = true;
                }
                mUserIsBlocked = false;
                showOverlay(false);
            }
            else     // BLOCK REQUEST
            {
                this.Focus();
                sendMsgTxtbox.Text = "";
                if (isGroupChat)
                {
                    mPubSub.publish(HikePubSub.BLOCK_GROUPOWNER, groupOwner);
                    blockUnblockMenuItem.Text = UNBLOCK_USER + " group owner";
                    groupInfoMenuItem.IsEnabled = false;
                }
                else
                {
                    mPubSub.publish(HikePubSub.BLOCK_USER, mContactNumber);
                    emoticonsIconButton.IsEnabled = false;
                    sendIconButton.IsEnabled = false;
                    isTypingNotificationEnabled = false;
                    blockUnblockMenuItem.Text = UNBLOCK_USER;
                    if (inviteMenuItem != null)
                        inviteMenuItem.IsEnabled = false;
                }
                emoticonPanel.Visibility = Visibility.Collapsed;
                mUserIsBlocked = true;
                showOverlay(true); //true means show block animation
            }
        }

        private void FileAttachmentMessage_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (!isContextMenuTapped)
            {
                MyChatBubble chatBubble = (sender as MyChatBubble);
                if (chatBubble.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return;
                if (chatBubble.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED && chatBubble.FileAttachment.FileState != Attachment.AttachmentState.STARTED)
                {
                    if (chatBubble is ReceivedChatBubble)
                    {
                        chatBubble.setAttachmentState(Attachment.AttachmentState.STARTED);
                        FileTransfer.Instance.downloadFile(chatBubble, mContactNumber.Replace(":", "_"));
                        MessagesTableUtils.addUploadingOrDownloadingMessage(chatBubble.MessageId);
                    }
                    else if (chatBubble is SentChatBubble)
                    {
                        //resend message
                        chatBubble.setAttachmentState(Attachment.AttachmentState.STARTED);
                        ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.UNKNOWN);
                        convMessage.IsSms = !isOnHike;
                        convMessage.HasAttachment = true;
                        convMessage.MessageId = chatBubble.MessageId;
                        convMessage.FileAttachment = chatBubble.FileAttachment;
                        if (convMessage.FileAttachment.ContentType.Contains("image"))
                        {
                            convMessage.Message = String.Format(HikeConstants.FILES_MESSAGE_PREFIX, "photo") + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains("audio"))
                        {
                            convMessage.Message = String.Format(HikeConstants.FILES_MESSAGE_PREFIX, "voice message") + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains("location"))
                        {
                            convMessage.Message = String.Format(HikeConstants.FILES_MESSAGE_PREFIX, "location") + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains("video"))
                        {
                            convMessage.Message = String.Format(HikeConstants.FILES_MESSAGE_PREFIX, "video") + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }

                        byte[] locationInfoBytes = null;
                        MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                            convMessage.MessageId, out locationInfoBytes);
                        string locationInfoString = System.Text.Encoding.UTF8.GetString(locationInfoBytes, 0, locationInfoBytes.Length);
                        convMessage.MetaDataString = locationInfoString;
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
            string contactNumberOrGroupId = mContactNumber.Replace(":", "_");
            if (shouldUpdateAttachment)
            {
                MiscDBUtil.saveAttachmentObject(chatBubble.FileAttachment, mContactNumber, chatBubble.MessageId);
            }
            if (chatBubble.FileAttachment.ContentType.Contains("image"))
            {
                object[] fileTapped = new object[2];
                fileTapped[0] = chatBubble.MessageId;
                fileTapped[1] = contactNumberOrGroupId;
                PhoneApplicationService.Current.State["objectForFileTransfer"] = fileTapped;
                NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
            }
            else if (chatBubble.FileAttachment.ContentType.Contains("audio") | chatBubble.FileAttachment.ContentType.Contains("video"))
            {
                MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId + "/" + Convert.ToString(chatBubble.MessageId);
                mediaPlayerLauncher.Media = new Uri(fileLocation, UriKind.Relative);
                mediaPlayerLauncher.Location = MediaLocationType.Data;
                mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                try
                {
                    mediaPlayerLauncher.Show();
                }
                catch
                {
                }
            }
            else if (chatBubble.FileAttachment.ContentType.Contains("location"))
            {
                string filePath = HikeConstants.FILES_BYTE_LOCATION + "/" + mContactNumber + "/" + Convert.ToString(chatBubble.MessageId);
                byte[] filebytes;
                MiscDBUtil.readFileFromIsolatedStorage(filePath, out filebytes);

                UTF8Encoding enc = new UTF8Encoding();
                string locationInfo = enc.GetString(filebytes, 0, filebytes.Length);
                JObject locationJSON = JObject.Parse(locationInfo);
                if (this.bingMapsTask == null)
                    bingMapsTask = new BingMapsTask();
                double latitude = Convert.ToDouble(locationJSON[HikeConstants.LATITUDE].ToString());
                double longitude = Convert.ToDouble(locationJSON[HikeConstants.LONGITUDE].ToString());
                double zoomLevel = Convert.ToDouble(locationJSON[HikeConstants.ZOOM_LEVEL].ToString());
                bingMapsTask.Center = new GeoCoordinate(latitude, longitude);
                bingMapsTask.ZoomLevel = zoomLevel;
                bingMapsTask.Show();
                return;
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
         * If readFromDB is true & message state is SENT_UNCONFIRMED, then trying image is set else 
         * it is scheduled
         */
        private void AddMessageToUI(ConvMessage convMessage, bool readFromDB)
        {
            try
            {
                #region NO_INFO
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

                        if (convMessage.FileAttachment == null)
                        {
                            //Done to avoid crash. Code should never reach here
                            Debug.WriteLine("Fileattachment object is null for convmessage with attachment");
                            return;
                        }
                        switch (convMessage.FileAttachment.FileState)
                        {
                            case Attachment.AttachmentState.CANCELED:
                                contextMenuDictionary = AttachmentUploadCanceledOrFailed;
                                break;
                            case Attachment.AttachmentState.COMPLETED:
                                contextMenuDictionary = AttachmentUploadedOrDownloaded;
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
                        //chatBubble = new SentChatBubble(convMessage, readFromDB);
                        chatBubble = SentChatBubble.getSplitChatBubbles(convMessage, readFromDB);
                        if (convMessage.MessageId < -1 || convMessage.MessageStatus < ConvMessage.State.SENT_DELIVERED_READ)
                            msgMap.Add(convMessage.MessageId, (SentChatBubble)chatBubble);
                        else if (convMessage.MessageId == -1)
                            msgMap.Add(TempMessageId, (SentChatBubble)chatBubble);
                    }
                    else
                    {
                        chatBubble = ReceivedChatBubble.getSplitChatBubbles(convMessage, isGroupChat, GroupManager.Instance.getGroupParticipant(null, convMessage.GroupParticipant, mContactNumber).FirstName);
                    }
                    this.MessageList.Children.Add(chatBubble);
                    if (chatBubble.splitChatBubbles != null && chatBubble.splitChatBubbles.Count > 0)
                    {
                        for (int i = 0; i < chatBubble.splitChatBubbles.Count; i++)
                        {
                            this.MessageList.Children.Add(chatBubble.splitChatBubbles[i]);
                        }
                    }
                    ScrollToBottom();
                    if (convMessage.FileAttachment != null)
                    {
                        chatBubble.setTapEvent(new EventHandler<GestureEventArgs>(FileAttachmentMessage_Tap));
                    }
                }
                #endregion
                #region MEMBERS JOINED GROUP CHAT

                // SHOW Group Chat joined / Added msg along with DND msg 
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.MEMBERS_JOINED)
                {
                    string[] vals = convMessage.Message.Split(';');

                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.HIKE_PARTICIPANT_JOINED, vals[0]);
                    this.MessageList.Children.Add(chatBubble);
                    if (vals.Length == 2)
                    {
                        MyChatBubble dndChatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.WAITING, vals[1]);
                        this.MessageList.Children.Add(dndChatBubble);
                    }
                    ScrollToBottom();

                }
                #endregion
                #region PARTICIPANT_JOINED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_JOINED)
                {
                    string[] vals = Utils.splitUserJoinedMessage(convMessage.Message);
                    if (vals == null || vals.Length == 0)
                        return;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        string[] vars = vals[i].Split(HikeConstants.DELIMITERS, StringSplitOptions.RemoveEmptyEntries); // msisdn:0 or msisdn:1

                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, vars[0], convMessage.Msisdn);
                        string text = HikeConstants.USER_JOINED_GROUP_CHAT;
                        NotificationChatBubble.MessageType type = NotificationChatBubble.MessageType.HIKE_PARTICIPANT_JOINED;
                        if (vars[1] == "0" && !gp.IsOnHike)
                        {
                            text = HikeConstants.USER_INVITED;
                            type = NotificationChatBubble.MessageType.SMS_PARTICIPANT_INVITED;
                        }
                        MyChatBubble chatBubble = new NotificationChatBubble(type, gp.FirstName + text);
                        this.MessageList.Children.Add(chatBubble);
                        ScrollToBottom();
                    }
                }
                #endregion
                #region GROUP_JOINED_OR_WAITING

                // This function is called after first normal message of Group Creation
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_JOINED_OR_WAITING)
                {
                    string[] vals = Utils.splitUserJoinedMessage(convMessage.Message);
                    if (vals == null || vals.Length == 0)
                        return;
                    List<string> waitingParticipants = null;
                    for (int i = 0; i < vals.Length; i++)
                    {
                        string[] vars = vals[i].Split(HikeConstants.DELIMITERS, StringSplitOptions.RemoveEmptyEntries); // msisdn:0 or msisdn:1
                        string msisdn = vars[0];
                        string showIcon = vars[1];
                        // every participant is either on DND or not on DND
                        GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, msisdn, convMessage.Msisdn);

                        string text = gp.FirstName + HikeConstants.USER_JOINED_GROUP_CHAT;
                        NotificationChatBubble.MessageType type = NotificationChatBubble.MessageType.SMS_PARTICIPANT_OPTED_IN;
                        if (showIcon == "0") // DND USER and not OPTED IN add to custom msg i.e waiting etc
                        {
                            if (waitingParticipants == null)
                                waitingParticipants = new List<string>();
                            waitingParticipants.Add(gp.FirstName);
                        }
                        else // if not DND show joined 
                        {
                            MyChatBubble chatBubble = new NotificationChatBubble(type, text);
                            this.MessageList.Children.Add(chatBubble);
                            ScrollToBottom();
                        }
                    }
                    if (waitingParticipants == null)
                        return;
                    StringBuilder msgText = new StringBuilder();
                    if (waitingParticipants.Count == 1)
                        msgText.Append(waitingParticipants[0]);
                    else if (waitingParticipants.Count == 2)
                        msgText.Append(waitingParticipants[0] + " and " + waitingParticipants[1]);
                    else
                    {
                        for (int i = 0; i < waitingParticipants.Count; i++)
                        {
                            msgText.Append(waitingParticipants[i]);
                            if (i == waitingParticipants.Count - 2)
                                msgText.Append(" and ");
                            else if (i < waitingParticipants.Count - 2)
                                msgText.Append(",");
                        }
                    }
                    MyChatBubble wchatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.WAITING, string.Format(HikeConstants.WAITING_TO_JOIN, msgText.ToString()));
                    this.MessageList.Children.Add(wchatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region USER_JOINED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.USER_JOINED_HIKE, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region HIKE_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.HIKE_USER)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.USER_JOINED_HIKE, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region SMS_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.SMS_USER)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.SMS_PARTICIPANT_INVITED, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region USER_OPT_IN
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
                {
                    NotificationChatBubble.MessageType type = NotificationChatBubble.MessageType.SMS_PARTICIPANT_OPTED_IN;
                    if (Utils.isGroupConversation(mContactNumber))
                    {
                        type = NotificationChatBubble.MessageType.SMS_PARTICIPANT_OPTED_IN;
                    }
                    MyChatBubble chatBubble = new NotificationChatBubble(type, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region DND_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
                {
                    //if (!Utils.isGroupConversation(mContactNumber))
                    {
                        MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.WAITING, convMessage.Message);
                        this.MessageList.Children.Add(chatBubble);
                        ScrollToBottom();
                    }
                }
                #endregion
                #region PARTICIPANT_LEFT
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT)
                {
                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.PARTICIPANT_LEFT, name + HikeConstants.USER_LEFT);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region GROUP END
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_END)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.GROUP_END, HikeConstants.GROUP_CHAT_END);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();

                }
                #endregion
                #region CREDITS REWARDS
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.REWARD, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region INTERNATIONAL_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_USER)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.INTERNATIONAL_USER_BLOCKED, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion
                #region INTERNATIONAL_GROUPCHAT_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_GROUP_USER)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.INTERNATIONAL_USER_BLOCKED, HikeConstants.SMS_INDIA);
                    this.MessageList.Children.Add(chatBubble);

                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    MyChatBubble chatBubbleLeft = new NotificationChatBubble(NotificationChatBubble.MessageType.PARTICIPANT_LEFT, name + HikeConstants.USER_LEFT);
                    this.MessageList.Children.Add(chatBubbleLeft);

                    ScrollToBottom();
                }
                #endregion
                #region GROUP NAME CHANGED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE)
                {
                    MyChatBubble chatBubble = new NotificationChatBubble(NotificationChatBubble.MessageType.REWARD, convMessage.Message);
                    this.MessageList.Children.Add(chatBubble);
                    ScrollToBottom();
                }
                #endregion

                msgBubbleCount++;
            }
            catch (Exception e)
            {
                Debug.WriteLine("NEW CHAT THREAD :: " + e.StackTrace);
            }
        }

        private void inviteUserBtn_Click(object sender, EventArgs e)
        {
            if (isOnHike)
                return;
            long time = TimeUtils.getCurrentTimeStamp();
            string inviteToken = "";
            App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
            ConvMessage convMessage = new ConvMessage(string.Format(App.sms_invite_message, inviteToken), mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED);
            convMessage.MessageId = TempMessageId;
            convMessage.IsSms = true;
            convMessage.IsInvite = true;
            sendMsg(convMessage, false);
        }

        #endregion

        #region PAGE EVENTS

        private bool isEmptyString = true;

        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lastText.Equals(sendMsgTxtbox.Text))
                return;
            if (String.IsNullOrEmpty(sendMsgTxtbox.Text.Trim()))
            {
                isEmptyString = true;
                return;
            }
            if (isEmptyString)
            {
                this.sendMsgTxtbox.Foreground = UI_Utils.Instance.Black;
                isEmptyString = false;
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
            else if (e.TaskResult == TaskResult.Cancel)
            {
                if (e.Error != null)
                    MessageBox.Show("You cannot select photo while phone is connected to computer.", "", MessageBoxButton.OK);
            }
        }

        //TODO remove these bools in release build. these are used because imageOpenHandler is called twice i debug
        private static bool abc = true;
        private static bool isReleaseMode = true;

        private void adjustAspectRatio(int width, int height, bool isThumbnail, out int adjustedWidth, out int adjustedHeight)
        {
            int maxHeight, maxWidth;
            if (isThumbnail)
            {
                maxHeight = HikeConstants.ATTACHMENT_THUMBNAIL_MAX_HEIGHT;
                maxWidth = HikeConstants.ATTACHMENT_THUMBNAIL_MAX_WIDTH;
            }
            else
            {
                maxHeight = HikeConstants.ATTACHMENT_MAX_HEIGHT;
                maxWidth = HikeConstants.ATTACHMENT_MAX_WIDTH;
            }

            if (height > width)
            {
                adjustedHeight = maxHeight;
                adjustedWidth = (width * adjustedHeight) / height;
            }
            else
            {
                adjustedWidth = maxWidth;
                adjustedHeight = (height * adjustedWidth) / width;
            }
        }


        void imageOpenedHandler(object sender, RoutedEventArgs e)
        {
            if (isReleaseMode || abc)
            {
                BitmapImage image = (BitmapImage)sender;
                SendImage(image, image.UriSource.ToString());
            }
            abc = !abc;
        }

        private void SendImage(BitmapImage image, string fileName)
        {
            if (!isGroupChat || isGroupAlive)
            {
                byte[] thumbnailBytes;
                byte[] fileBytes;

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.MessageId = TempMessageId;

                WriteableBitmap writeableBitmap = new WriteableBitmap(image);
                int thumbnailWidth, thumbnailHeight, imageWidth, imageHeight;
                adjustAspectRatio(image.PixelWidth, image.PixelHeight, true, out thumbnailWidth, out thumbnailHeight);
                adjustAspectRatio(image.PixelWidth, image.PixelHeight, false, out imageWidth, out imageHeight);

                using (var msSmallImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msSmallImage, thumbnailWidth, thumbnailHeight, 0, 50);
                    thumbnailBytes = msSmallImage.ToArray();
                }
                if (fileName.StartsWith("{")) // this is from share picker
                {
                    fileName = "PhotoChooser-" + fileName.Substring(1, fileName.Length - 2) + ".jpg";
                }
                else
                    fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

                convMessage.FileAttachment = new Attachment(fileName, thumbnailBytes, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = "image";
                convMessage.Message = "image";

                SentChatBubble chatBubble = new SentChatBubble(convMessage, thumbnailBytes);
                msgMap.Add(convMessage.MessageId, chatBubble);

                addNewAttachmentMessageToUI(chatBubble);

                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, imageWidth, imageHeight, 0, 65);
                    fileBytes = msLargeImage.ToArray();
                }
                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = fileBytes;
                vals[2] = chatBubble;
                mPubSub.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }


        private void sendMsg(ConvMessage convMessage, bool isNewGroup)
        {
            if (isNewGroup) // this is used for new group as well as when you add members to existing group
            {
                PhoneApplicationService.Current.State[mContactNumber] = mContactName;
                JObject metaData = new JObject();
                metaData[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN_NEW;
                convMessage.MetaDataString = metaData.ToString(Newtonsoft.Json.Formatting.None);
            }
            if (isTypingNotificationActive)
            {
                HideTypingNotification();
                isReshowTypingNotification = true;
            }
            AddMessageToUI(convMessage, false);
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
            this.MessageList.Margin = UI_Utils.Instance.ChatThreadKeyPadUpMargin;
            ScrollToBottom();
            if (this.emoticonPanel.Visibility == Visibility.Visible)
                this.emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void sendMsgTxtbox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.MessageList.Margin = UI_Utils.Instance.ChatThreadKeyPadDownMargin;
        }


        #endregion

        #region CONTEXT MENU

        private void MenuItem_Click_Forward(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            MyChatBubble chatBubble = ((sender as MenuItem).DataContext as MyChatBubble);
            if (chatBubble.FileAttachment == null)
            {
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = chatBubble.Text;
                NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
            }
            else
            {
                object[] attachmentForwardMessage = new object[2];
                attachmentForwardMessage[0] = chatBubble;
                attachmentForwardMessage[1] = mContactNumber;
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = attachmentForwardMessage;
                NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
            }
        }

        private void MenuItem_Click_Copy(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            MyChatBubble chatBubble = ((sender as MenuItem).DataContext as MyChatBubble);
            if (chatBubble.FileAttachment == null)
                Clipboard.SetText(chatBubble.Text);
            else if (!String.IsNullOrEmpty(chatBubble.FileAttachment.FileKey))
                Clipboard.SetText(HikeConstants.FILE_TRANSFER_COPY_BASE_URL + "/" + chatBubble.FileAttachment.FileKey);
        }

        private void MenuItem_Click_Delete(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            MyChatBubble msg = ((sender as MenuItem).DataContext as MyChatBubble);
            if (msg == null)
            {
                return;
            }
            if (msg.FileAttachment != null && msg.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                msg.FileAttachment.FileState = Attachment.AttachmentState.CANCELED;
            bool delConv = false;
            this.MessageList.Children.Remove(msg);
            ConversationListObject obj = App.ViewModel.ConvMap[mContactNumber];

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
                {
                    if (lastMessageBubble.FileAttachment.ContentType.Contains("image"))
                        obj.LastMessage = "image";
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains("audio"))
                        obj.LastMessage = "audio";
                    if (lastMessageBubble.FileAttachment.ContentType.Contains("video"))
                        obj.LastMessage = "video";
                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                }
                else if (lastMessageBubble is NotificationChatBubble)
                {
                    obj.LastMessage = (lastMessageBubble as NotificationChatBubble).UserName.Text;
                    obj.MessageStatus = ConvMessage.State.UNKNOWN;
                    obj.TimeStamp = lastMessageBubble.TimeStampLong;
                }
                else
                {
                    obj.LastMessage = lastMessageBubble.Text;
                    //obj.MessageStatus = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].MessageStatus;
                    //obj.TimeStamp = this.ChatThreadPageCollection[ChatThreadPageCollection.Count - 1].TimeStampLong;
                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                    obj.TimeStamp = lastMessageBubble.TimeStampLong;
                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                }
            }
            else
            {
                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj);
                App.ViewModel.ConvMap.Remove(mContactNumber);
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
            if (chatBubble.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
            {
                chatBubble.setAttachmentState(Attachment.AttachmentState.CANCELED);
                MiscDBUtil.saveAttachmentObject(chatBubble.FileAttachment, mContactNumber, chatBubble.MessageId);
            }
        }

        #endregion

        #region EMOTICONS RELATED STUFF

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            object s = e.OriginalSource;
        }

        private void emoticonButton_Click(object sender, EventArgs e)
        {
            if (emoticonPanel.Visibility == Visibility.Collapsed)
                emoticonPanel.Visibility = Visibility.Visible;
            else
                emoticonPanel.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        private void fileTransferButton_Click(object sender, EventArgs e)
        {
            if (attachmentMenu.Visibility == Visibility.Collapsed)
                attachmentMenu.Visibility = Visibility.Visible;
            else
                attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                photoChooserTask.Show();
                attachmentMenu.Visibility = Visibility.Collapsed;
            }
            catch
            {
            }
        }

        private void sendAudio_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/RecordMedia.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void shareLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/ShareLocation.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;

            //GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
            //watcher.MovementThreshold = 20;
            ////watcher.StatusChanged += new EventHandler<GeoPositionStatusChangedEventArgs>(watcher_StatusChanged);
            //watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            //watcher.Start();
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => MyPositionChanged(e));
        }

        void MyPositionChanged(GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            BingMapsTask bingMapsTask = new BingMapsTask();
            //Omit the Center property to use the user's current location.
            bingMapsTask.Center = new GeoCoordinate(e.Position.Location.Latitude, e.Position.Location.Longitude);
            //            bingMapsTask.SearchTerm = "coffee";
            bingMapsTask.ZoomLevel = 24;
            bingMapsTask.Show();

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
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList1.SelectedIndex + SmileyParser.Instance.emoticon0Size;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            int index = emotList2.SelectedIndex + SmileyParser.Instance.emoticon0Size + SmileyParser.Instance.emoticon1Size;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
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
                if (inviteMenuItem != null && appBar.MenuItems.Contains(inviteMenuItem))
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
            smsCounterTxtBlk.Text = Convert.ToString(mCredits) + " SMS Left";
            smscounter.Visibility = Visibility.Visible;
            scheduler.Schedule(hideSMSCounter, TimeSpan.FromSeconds(2));
        }

        private void hideSMSCounter()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                smscounter.Visibility = Visibility.Collapsed;
            });
        }

        private void updateChatMetadata()
        {
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
                overlayRectangle.Visibility = System.Windows.Visibility.Visible;
                overlayRectangle.Opacity = 0.85;
                HikeTitle.IsHitTestVisible = false;
                MessageList.IsHitTestVisible = false;
                bottomPanel.IsHitTestVisible = false;
                OverlayMessagePanel.Visibility = Visibility.Visible;
                emoticonsIconButton.IsEnabled = false;
                sendIconButton.IsEnabled = false;
                fileTransferIconButton.IsEnabled = false;
            }
            else
            {
                overlayRectangle.Visibility = System.Windows.Visibility.Collapsed;
                HikeTitle.IsHitTestVisible = true;
                MessageList.IsHitTestVisible = true;
                bottomPanel.IsHitTestVisible = true;
                OverlayMessagePanel.Visibility = Visibility.Collapsed;
                if (isGroupChat && !isGroupAlive)
                {
                    emoticonsIconButton.IsEnabled = false;
                    sendIconButton.IsEnabled = false;
                    fileTransferIconButton.IsEnabled = false;
                }
                else
                {
                    emoticonsIconButton.IsEnabled = true;
                    sendIconButton.IsEnabled = true;
                    fileTransferIconButton.IsEnabled = true;
                }
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
            object[] publishData = new object[2];
            publishData[0] = obj;
            publishData[1] = 0; //qos
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, publishData);
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
            lastTypingNotificationShownTime = TimeUtils.getCurrentTimeStamp();
            scheduler.Schedule(autoHideTypingNotification, TimeSpan.FromSeconds(HikeConstants.TYPING_NOTIFICATION_AUTOHIDE));
        }

        private void autoHideTypingNotification()
        {
            long timeElapsed = TimeUtils.getCurrentTimeStamp() - lastTypingNotificationShownTime;
            if (timeElapsed >= HikeConstants.TYPING_NOTIFICATION_AUTOHIDE)
                HideTypingNotification();
        }

        private void HideTypingNotification()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if ((!isTypingNotificationEnabled || isTypingNotificationActive) && this.MessageList.Children.Contains(typingNotificationImage))
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

                    // notify only if msg is not a notification msg
                    if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
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
                        AddMessageToUI(convMessage, false);
                        if (vals.Length == 3)
                        {
                            ConvMessage cm = (ConvMessage)vals[2];
                            if (cm != null)
                                AddMessageToUI(cm, false);
                        }
                    });
                }
                else // this is to show toast notification
                {
                    ConversationListObject val;
                    if (App.ViewModel.ConvMap.TryGetValue(convMessage.Msisdn, out val) && val.IsMute) // of msg is for muted conv, ignore msg
                        return;
                    ConversationListObject cObj = vals[1] as ConversationListObject;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ToastPrompt toast = new ToastPrompt();
                        if (cObj.ContactName != null)
                            toast.Title = cObj.ContactName;
                        else
                            toast.Title = cObj.Msisdn;
                        toast.Message = cObj.LastMessage;
                        toast.ImageSource = new BitmapImage(new Uri("ApplicationIcon.png", UriKind.RelativeOrAbsolute));
                        toast.Show();

                    });
                    bool isVibrateEnabled = true;
                    App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
                    if (isVibrateEnabled)
                    {
                        VibrateController vibrate = VibrateController.Default;
                        vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                    }
                }
            }

            # endregion

            #region SERVER_RECEIVED_MSG

            else if (HikePubSub.SERVER_RECEIVED_MSG == type)
            {
                long msgId = (long)obj;
                try
                {
                    SentChatBubble msg = null;
                    msgMap.TryGetValue(msgId, out msg);
                    if (msg != null)
                    {
                        //msg.MessageStatus = ConvMessage.State.SENT_CONFIRMED;
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
                object[] vals = (object[])obj;
                long msgId = (long)vals[0];
                string msisdnToCheck = (string)vals[1];
                if (msisdnToCheck != mContactNumber)
                    return;
                try
                {
                    SentChatBubble msg = null;
                    msgMap.TryGetValue(msgId, out msg);
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
                object[] vals = (object[])obj;
                long[] ids = (long[])vals[0];
                string msisdnToCheck = (string)vals[1];
                if (msisdnToCheck != mContactNumber)
                    return;
                // TODO we could keep a map of msgId -> conversation objects somewhere to make this faster
                for (int i = 0; i < ids.Length; i++)
                {
                    try
                    {
                        SentChatBubble msg = null;
                        msgMap.TryGetValue(ids[i], out msg);
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
                        showSMSCounter();
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
                object[] vals = (object[])obj;
                string typingNotSenderOrSendee = "";
                if (isGroupChat)
                {
                    typingNotSenderOrSendee = (string)vals[1];
                }
                else
                {
                    typingNotSenderOrSendee = (string)vals[0];
                }
                if (mContactNumber == typingNotSenderOrSendee)
                {
                    ShowTypingNotification();
                }
            }

            #endregion

            #region END_TYPING_CONVERSATION

            else if (HikePubSub.END_TYPING_CONVERSATION == type)
            {
                object[] vals = (object[])obj;
                string typingNotSenderOrSendee = "";
                if (isGroupChat)
                {
                    typingNotSenderOrSendee = (string)vals[1];
                }
                else
                {
                    typingNotSenderOrSendee = (string)vals[0];
                }
                if (mContactNumber == typingNotSenderOrSendee)
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

            #region PARTICIPANT_LEFT_GROUP

            else if (HikePubSub.PARTICIPANT_LEFT_GROUP == type)
            {
                ConvMessage cm = (ConvMessage)obj;
                if (mContactNumber != cm.Msisdn)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        mContactName = App.ViewModel.ConvMap[mContactNumber].NameToShow;
                        userName.Text = mContactName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NEW_CHAT_THREAD :: Exception in participant left group : " + ex.StackTrace);
                    }
                });
            }

            #endregion

            #region PARTICIPANT_JOINED_GROUP

            else if (HikePubSub.PARTICIPANT_JOINED_GROUP == type)
            {
                JObject json = (JObject)obj;
                string eventGroupId = (string)json[HikeConstants.TO];
                if (eventGroupId != mContactNumber)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        mContactName = App.ViewModel.ConvMap[mContactNumber].NameToShow;
                        userName.Text = mContactName;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NEW_CHAT_THREAD :: Exception in participant joined group : " + ex.StackTrace);
                    }
                });
            }

            #endregion
        }

        private void groupChatEnd()
        {
            sendMsgTxtbox.IsHitTestVisible = false;
            appBar.IsMenuEnabled = false;
            sendIconButton.IsEnabled = false;
            emoticonsIconButton.IsEnabled = false;
            fileTransferIconButton.IsEnabled = false;
        }

        #endregion

        private void shareLocation()
        {
            if (!isGroupChat || isGroupAlive)
            {
                object[] locationInfo = (object[])PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION];
                PhoneApplicationService.Current.State.Remove(HikeConstants.SHARED_LOCATION);

                byte[] imageThumbnail = null;
                JObject locationJSON = (JObject)locationInfo[0];
                imageThumbnail = (byte[])locationInfo[1];

                string fileName = "location_" + TimeUtils.getCurrentTimeStamp().ToString();

                string locationJSONString = locationJSON.ToString();
                //byte[] locationBytes = new byte[locationJSONString.Length * sizeof(char)];
                //System.Buffer.BlockCopy(locationJSONString.ToCharArray(), 0, locationBytes, 0, locationBytes.Length);

                byte[] locationBytes = (new System.Text.UTF8Encoding()).GetBytes(locationJSONString);

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.MessageId = TempMessageId;

                convMessage.FileAttachment = new Attachment(fileName, imageThumbnail, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = "hikemap/location";
                convMessage.Message = "location";
                convMessage.MetaDataString = locationJSONString;

                SentChatBubble chatBubble = new SentChatBubble(convMessage, imageThumbnail);
                msgMap.Add(convMessage.MessageId, chatBubble);

                addNewAttachmentMessageToUI(chatBubble);
                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = locationBytes;
                vals[2] = chatBubble;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        private void AudioFileTransfer()
        {
            if (!isGroupChat || isGroupAlive)
            {
                byte[] audioBytes = (byte[])PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED];
                PhoneApplicationService.Current.State.Remove(HikeConstants.AUDIO_RECORDED);

                string fileName = "aud_" + TimeUtils.getCurrentTimeStamp().ToString();

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.MessageId = TempMessageId;

                convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = "audio/voice";
                convMessage.Message = "audio";

                SentChatBubble chatBubble = new SentChatBubble(convMessage, null);
                msgMap.Add(convMessage.MessageId, chatBubble);

                addNewAttachmentMessageToUI(chatBubble);
                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = audioBytes;
                vals[2] = chatBubble;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        // this should be called when one gets tap here msg.
        private void smsUser_Click(object sender, EventArgs e)
        {
            SmsComposeTask sms = new Microsoft.Phone.Tasks.SmsComposeTask();
            sms.To = mContactNumber; // set phone number
            sms.Body = ""; // set body
            try
            {
                sms.Show();
            }
            catch { }
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

        private void MessageList_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!isGroupChat)
            {
                if (mUserIsBlocked)
                    return;

                emoticonPanel.Visibility = Visibility.Collapsed;

                if ((!isOnHike && mCredits <= 0))
                    return;
                ConvMessage convMessage = new ConvMessage("Nudge!", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                convMessage.IsSms = !isOnHike;
                convMessage.MessageId = TempMessageId;
                convMessage.HasAttachment = false;
                convMessage.MetaDataString = "{poke:1}";
                sendMsg(convMessage, false);
                VibrateController vibrate = VibrateController.Default;
                vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
            }
        }
    }
}