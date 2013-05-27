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
using Microsoft.Phone.Tasks;
using System.IO;
using windows_client.Controls;
using System.Text;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Media;
using System.Device.Location;
using windows_client.Misc;
using Microsoft.Phone.UserData;
using windows_client.Languages;
using System.Net.NetworkInformation;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Collections.ObjectModel;

namespace windows_client.View
{
    public partial class NewChatThread : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {

        #region CONSTANTS AND PAGE OBJECTS

        private readonly string ON_HIKE_TEXT = AppResources.SelectUser_FreeMsg_Txt;
        private readonly string ON_SMS_TEXT = AppResources.SelectUser_SmsMsg_Txt;
        private readonly string ON_GROUP_TEXT = AppResources.SelectUser_GroupMsg_Txt;
        private readonly string ZERO_CREDITS_MSG = AppResources.SelectUser_ZeroCredits_Txt;
        private readonly string UNBLOCK_USER = AppResources.UnBlock_Txt;
        private PageOrientation pageOrientation = PageOrientation.Portrait;

        private const int maxFileSize = 15728640;//in bytes
        private const int maxSmsCharLength = 140;
        private string groupOwner = null;
        public string mContactNumber;
        private string mContactName = null;
        private string lastText = "";
        private string hintText = string.Empty;
        private bool enableSendMsgButton = false;

        bool afterMute = true;

        private bool _isMute = false;
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
        private bool showNoSmsLeftOverlay = false;
        private bool isContextMenuTapped = false;
        private JObject groupCreateJson = null;
        private Dictionary<long, Attachment> attachments = null; //this map is required for mapping attachment object with convmessage only for
        //messages stored in db, other messages would have their attachment object set

        private int mCredits;
        private long lastTextChangedTime;
        private long lastTypingNotificationShownTime;

        private HikePubSub mPubSub;
        private IScheduler scheduler = Scheduler.NewThread;
        private ConvMessage convTypingNotification;
        ContactInfo contactInfo = null; // this will be used if someone adds an unknown number to addressbook
        private byte[] avatar;
        private BitmapImage avatarImage;
        private ApplicationBar appBar;
        ApplicationBarMenuItem muteGroupMenuItem;
        ApplicationBarMenuItem inviteMenuItem = null;
        ApplicationBarMenuItem addUserMenuItem;
        ApplicationBarMenuItem infoMenuItem;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton emoticonsIconButton = null;
        ApplicationBarIconButton fileTransferIconButton = null;
        private PhotoChooserTask photoChooserTask;
        private CameraCaptureTask cameraCaptureTask;
        private BingMapsTask bingMapsTask = null;
        private bool isShowNudgeTute = true;
        private object statusObject = null;

        //        private ObservableCollection<MyChatBubble> chatThreadPageCollection = new ObservableCollection<MyChatBubble>();
        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> sent message bubble mapping
        //private Dictionary<ConvMessage, SentChatBubble> _convMessageSentBubbleMap = new Dictionary<ConvMessage, SentChatBubble>(); // this holds msgId -> sent message bubble mapping

        public bool isMessageLoaded;
        public ObservableCollection<ConvMessage> ocMessages;

        #endregion

        #region UI VALUES

        private readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
        private Thickness imgMargin = new Thickness(24, 5, 0, 15);
        private Image emptyImage;

        #endregion

        private List<long> idsToUpdate = null; // this will store ids in perception fix case

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


        public Dictionary<long, ConvMessage> OutgoingMsgsMap      /* This map will contain only outgoing messages */
        {
            get
            {
                return msgMap;
            }
        }

        public bool IsMute      /* This map will contain only outgoing messages */
        {
            get
            {
                return _isMute;
            }
            set
            {
                if (value != _isMute)
                {
                    _isMute = value;
                    if (_isMute)
                    {
                        gcMuteGrid.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        gcMuteGrid.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        #region PAGE BASED FUNCTIONS

        //        private ObservableCollection<UIElement> messagesCollection;
        public NewChatThread()
        {
            InitializeComponent();
            ocMessages = new ObservableCollection<ConvMessage>();
        }

        private void ManagePageStateObjects()
        {
            //or condition for case of tombstoning
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE) || this.State.ContainsKey(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE))
            {
                Object obj;
                if (!PhoneApplicationService.Current.State.TryGetValue(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE, out obj))
                {
                    obj = this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE];
                }
                if (obj is ConversationListObject)
                    statusObject = this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = obj;

                else // obj is ContactInfo obj
                    statusObject = this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = obj;
                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_SELECTUSER_PAGE) || this.State.ContainsKey(HikeConstants.OBJ_FROM_SELECTUSER_PAGE))
            {
                //contact info object
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.OBJ_FROM_SELECTUSER_PAGE, out statusObject))
                    this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = statusObject;
                else
                    statusObject = this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE];

                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_SELECTUSER_PAGE);
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GROUP_CHAT) || this.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                //list<Contact Info>
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.GROUP_CHAT, out statusObject))
                    this.State[HikeConstants.GROUP_CHAT] = statusObject;
                else
                    statusObject = this.State[HikeConstants.GROUP_CHAT];

                PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE) || this.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE))
            {
                //contactInfo
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.OBJ_FROM_STATUSPAGE, out statusObject))
                    this.State[HikeConstants.OBJ_FROM_STATUSPAGE] = statusObject;
                else
                    statusObject = this.State[HikeConstants.OBJ_FROM_STATUSPAGE];
                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_STATUSPAGE);
                IEnumerable<JournalEntry> entries = NavigationService.BackStack;
                int count = 0;
                foreach (JournalEntry entry in entries)
                    count++;
                if (count > 1) // this represents we came to this page from timeline directly
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
            // whenever CT is opened , mark last msg as read if received read
            if (App.ViewModel.ConvMap.ContainsKey(mContactNumber) && App.ViewModel.ConvMap[mContactNumber].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                App.ViewModel.ConvMap[mContactNumber].MessageStatus = ConvMessage.State.RECEIVED_READ;
            this.llsMessages.ItemsSource = ocMessages;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                Stopwatch st = Stopwatch.StartNew();
                attachments = MiscDBUtil.getAllFileAttachment(mContactNumber);
                loadMessages(INITIAL_FETCH_COUNT);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                   {
                       ScrollToBottom();
                   });
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to load chat messages for msisdn {0} : {1}", mContactNumber, msec);
                if (isGC)
                {
                    ConvMessage groupCreateCM = new ConvMessage(groupCreateJson, true, false);
                    groupCreateCM.CurrentOrientation = this.Orientation;
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
            emptyImage = new Image();
            emptyImage.Source = UI_Utils.Instance.EmptyImage;
            emptyImage.Height = 1;

            bw.RunWorkerAsync();
            photoChooserTask = new PhotoChooserTask();
            photoChooserTask.ShowCamera = false;
            photoChooserTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            cameraCaptureTask = new CameraCaptureTask();
            cameraCaptureTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);
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
                if (msisdn.Contains("hike"))
                    msisdn = "+hike+";
                else if (Char.IsDigit(msisdn[0]))
                    msisdn = "+" + msisdn;

                //MessageBox.Show(msisdn, "NEW CHAT", MessageBoxButton.OK);
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    string id = msisdn.Replace(":", "_");
                    byte[] _avatar = MiscDBUtil.getThumbNailForMsisdn(id);
                    App.ViewModel.ConvMap[msisdn].Avatar = _avatar;
                    this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = statusObject = App.ViewModel.ConvMap[msisdn];
                }
                else if (Utils.isGroupConversation(msisdn))
                {
                    ConversationListObject co = new ConversationListObject();
                    co.ContactName = AppResources.SelectUser_NewGroup_Text;
                    co.Msisdn = msisdn;
                    co.IsOnhike = true;
                    this.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = statusObject = co;
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
                    this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = statusObject = contact;
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
                else //removing here because it may be case that user pressed back without selecting any user
                    PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);

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
            if (App.stickerHelper == null)
            {
                App.stickerHelper = new StickerHelper();
            }
            App.stickerHelper.InitialiseStickers();
            #region AUDIO FT
            if (!App.IS_TOMBSTONED && (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED) ||
                PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_RECORDED)))
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
            #region SHARE CONTACT
            if (!App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CONTACT_SELECTED))
            {
                ContactTransfer();
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
            try
            {
                base.OnRemovedFromJournal(e);
                removeListeners();

                if (App.newChatThreadPage == this)
                    App.newChatThreadPage = null;
                App.stickerHelper = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
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
                    BlockTxtBlk.Text = AppResources.SelectUser_BlockedGroupMsg_Txt;
                    gi = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    if (gi != null)
                        groupOwner = gi.GroupOwner;
                    if (gi != null && !gi.GroupAlive)
                        isGroupAlive = false;
                    IsMute = convObj.IsMute;
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
                avatarImage = convObj.AvatarImage;
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
                userImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(mContactNumber);
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
                avatarImage = UI_Utils.Instance.GetBitmapImage(mContactNumber, isOnHike);
                userImage.Source = avatarImage;
            }
            #endregion
            #region OBJECT FROM STATUS PAGE
            else if (this.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE))
            {
                object obj = this.State[HikeConstants.OBJ_FROM_STATUSPAGE];
                if (obj is ConversationListObject)
                {
                    ConversationListObject co = (ConversationListObject)obj;
                    mContactNumber = co.Msisdn;
                    if (co.ContactName != null)
                        mContactName = co.ContactName;
                    else
                    {
                        mContactName = co.Msisdn;
                        isAddUser = true;
                    }

                    isOnHike = co.IsOnhike;
                    if (App.IS_TOMBSTONED) // in this case avatar needs to be re calculated
                    {
                        co.Avatar = MiscDBUtil.getThumbNailForMsisdn(mContactNumber);
                    }
                    avatarImage = co.AvatarImage;
                    userImage.Source = co.AvatarImage;
                }
                else
                {
                    ContactInfo cn = (ContactInfo)obj;
                    mContactNumber = cn.Msisdn;
                    if (cn.Name != null)
                        mContactName = cn.Name;
                    else
                    {
                        mContactName = cn.Msisdn;
                        isAddUser = true;
                    }

                    isOnHike = cn.OnHike;
                    avatar = MiscDBUtil.getThumbNailForMsisdn(mContactNumber);

                    if (avatar == null)
                    {
                        avatarImage = UI_Utils.Instance.getDefaultAvatar(mContactNumber);
                    }
                    else
                    {
                        avatarImage = UI_Utils.Instance.createImageFromBytes(avatar);
                    }
                    userImage.Source = avatarImage;
                }
            }
            #endregion

            if (!isOnHike)
            {
                spContactTransfer.IsHitTestVisible = false;
                spContactTransfer.Opacity = 0.4;
            }
            userName.Text = mContactName;

            // if hike bot msg disable appbar, textbox etc
            if (Utils.IsHikeBotMsg(mContactNumber))
            {
                sendMsgTxtbox.IsEnabled = false;
                return;
            }

            if (groupOwner != null)
                mUserIsBlocked = App.ViewModel.BlockedHashset.Contains(groupOwner);
            else
                mUserIsBlocked = App.ViewModel.BlockedHashset.Contains(mContactNumber);
            initAppBar(isAddUser);
            if (!isOnHike)
            {
                sendMsgTxtbox.Hint = hintText = ON_SMS_TEXT;
                initInviteMenuItem();
                appBar.MenuItems.Add(inviteMenuItem);
            }
            else
            {
                sendMsgTxtbox.Hint = hintText = ON_HIKE_TEXT;
            }
            if (isGroupChat)
                sendMsgTxtbox.Hint = hintText = ON_GROUP_TEXT;
            initBlockUnblockState();

            if (isGroupChat && !isGroupAlive)
                groupChatEnd();
            else
            {
                App.appSettings.TryGetValue(App.SMS_SETTING, out mCredits);
                if (mCredits <= 0)
                {
                    if (isGroupChat)
                    {
                        if (App.appSettings.Contains(HikeConstants.SHOW_GROUP_CHAT_OVERLAY))
                        {
                            foreach (GroupParticipant gp in GroupManager.Instance.GroupCache[mContactNumber])
                            {
                                if (!gp.IsOnHike)
                                {
                                    ToggleAlertOnNoSms(true);
                                    break;
                                }
                            }
                        }
                    }
                    else if (!isOnHike)
                    {
                        showNoSmsLeftOverlay = true;
                        ToggleAlertOnNoSms(true);
                    }
                }
            }

            if (isShowNudgeTute)
                showNudgeTute();
        }

        private void showNudgeTute()
        {
            if (!isGroupChat && App.appSettings.Contains(App.SHOW_NUDGE_TUTORIAL))
            {
                overlayForNudge.Visibility = Visibility.Visible;
                //overlayForNudge.Opacity = 0.65;
                overlayForNudge.Opacity = 0.3;
                nudgeTuteGrid.Visibility = Visibility.Visible;
                llsMessages.IsHitTestVisible = bottomPanel.IsHitTestVisible = false;
                //SystemTray.IsVisible = false;
            }
            else
            {
                chatThreadMainPage.ApplicationBar = appBar;
            }
        }

        private void dismissNudgeTutorial_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            overlayForNudge.Visibility = Visibility.Collapsed;
            nudgeTuteGrid.Visibility = Visibility.Collapsed;
            llsMessages.IsHitTestVisible = bottomPanel.IsHitTestVisible = true;
            chatThreadMainPage.ApplicationBar = appBar;
            App.RemoveKeyFromAppSettings(App.SHOW_NUDGE_TUTORIAL);
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
            if (usersToAdd.Count == 0)
                return;
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
                if (gif != null && string.IsNullOrEmpty(gif.GroupName)) // set groupname if not alreay set
                {
                    mContactName = GroupManager.Instance.defaultGroupName(mContactNumber);
                    ConversationTableUtils.updateGroupName(mContactNumber, mContactName); // update DB and UI
                }
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
                cm.CurrentOrientation = this.Orientation;
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
        private void initAppBar(bool isAddUser)
        {
            appBar = new ApplicationBar();
            appBar.Mode = ApplicationBarMode.Default;
            appBar.IsVisible = true;
            appBar.IsMenuEnabled = true;

            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/icon_send.png", UriKind.Relative);
            sendIconButton.Text = AppResources.Send_Txt;
            sendIconButton.Click += new EventHandler(sendMsgBtn_Click);
            sendIconButton.IsEnabled = false;
            appBar.Buttons.Add(sendIconButton);

            //add icon for smiley
            emoticonsIconButton = new ApplicationBarIconButton();
            emoticonsIconButton.IconUri = new Uri("/View/images/icon_emoticon.png", UriKind.Relative);
            emoticonsIconButton.Text = AppResources.Smiley_Txt;
            emoticonsIconButton.Click += new EventHandler(emoticonButton_Click);
            emoticonsIconButton.IsEnabled = true;
            appBar.Buttons.Add(emoticonsIconButton);

            //add file transfer button
            fileTransferIconButton = new ApplicationBarIconButton();
            fileTransferIconButton.IconUri = new Uri("/View/images/icon_attachment.png", UriKind.Relative);
            fileTransferIconButton.Text = AppResources.Attach_Txt;
            fileTransferIconButton.Click += new EventHandler(fileTransferButton_Click);
            fileTransferIconButton.IsEnabled = true;
            appBar.Buttons.Add(fileTransferIconButton);


            if (isGroupChat)
            {
                userName.Tap += userHeader_Tap;
                userImage.Tap += userImage_Tap;

                infoMenuItem = new ApplicationBarMenuItem();
                infoMenuItem.Text = AppResources.GroupInfo_Txt;
                infoMenuItem.Click += userHeader_Tap;
                infoMenuItem.IsEnabled = !mUserIsBlocked && isGroupAlive;
                appBar.MenuItems.Add(infoMenuItem);

                muteGroupMenuItem = new ApplicationBarMenuItem();
                muteGroupMenuItem.Text = IsMute ? AppResources.SelectUser_UnMuteGrp_Txt : AppResources.SelectUser_MuteGrp_Txt;
                muteGroupMenuItem.Click += new EventHandler(muteUnmuteGroup_Click);
                appBar.MenuItems.Add(muteGroupMenuItem);

                ApplicationBarMenuItem leaveMenuItem = new ApplicationBarMenuItem();
                leaveMenuItem.Text = AppResources.SelectUser_LeaveGrp_Txt;
                leaveMenuItem.Click += new EventHandler(leaveGroup_Click);
                appBar.MenuItems.Add(leaveMenuItem);
            }
            else
            {
                if (isAddUser)
                {
                    addUserMenuItem = new ApplicationBarMenuItem();
                    addUserMenuItem.Text = AppResources.SelectUser_AddUser_Txt;
                    addUserMenuItem.Click += new EventHandler(addUser_Click);
                    appBar.MenuItems.Add(addUserMenuItem);
                }
                ApplicationBarMenuItem callMenuItem = new ApplicationBarMenuItem();
                callMenuItem.Text = AppResources.Call_Txt;
                callMenuItem.Click += new EventHandler(callUser_Click);
                appBar.MenuItems.Add(callMenuItem);
                userHeader.Tap += userHeader_Tap;

                infoMenuItem = new ApplicationBarMenuItem();
                infoMenuItem.Text = AppResources.OthersProfile_Txt;
                infoMenuItem.Click += userHeader_Tap;
                appBar.MenuItems.Add(infoMenuItem);
            }
        }

        private void initInviteMenuItem()
        {
            inviteMenuItem = new ApplicationBarMenuItem();
            inviteMenuItem.Text = AppResources.SelectUser_InviteUsr_Txt;
            inviteMenuItem.Click += new EventHandler(inviteUserBtn_Click);
            if (mUserIsBlocked)
                inviteMenuItem.IsEnabled = false;
        }

        #endregion

        #endregion

        #region BACKGROUND WORKER

        long lastMessageId = -1;
        bool hasMoreMessages;
        const int INITIAL_FETCH_COUNT = 21;
        const int SUBSEQUENT_FETCH_COUNT = 11;

        // this variable stores the status of last SENT msg
        ConvMessage.State refState = ConvMessage.State.UNKNOWN;

        private void loadMessages(int messageFetchCount)
        {
            int i;
            bool isPublish = false;
            hasMoreMessages = false;

            try
            {
                List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber, lastMessageId < 0 ? long.MaxValue : lastMessageId, messageFetchCount);

                if (messagesList == null) // represents there are no chat messages for this msisdn
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //messageListBox.Opacity = 1;
                        progressBar.Opacity = 0;
                        progressBar.IsEnabled = false;
                        forwardAttachmentMessage();
                    });
                    NetworkManager.turnOffNetworkManager = false;
                    return;
                }

                bool isHandled = false;
                JArray ids = new JArray();
                List<long> dbIds = new List<long>();
                int count = 0;
                for (i = 0; i < messagesList.Count; i++)
                {
                    ConvMessage cm = messagesList[i];
                    Debug.WriteLine(cm.MessageId);
                    if (i == messageFetchCount - 1)
                    {
                        hasMoreMessages = true;
                        lastMessageId = cm.MessageId;
                        break;
                    }
                    count++;
                    if (count % 5 == 0)
                        Thread.Sleep(5);
                    messagesList[i].IsSms = !isOnHike;

                    #region PERCEPTION FIX ZONE

                    // perception fix is only used for msgs of normal type in which SDR applies
                    if (messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                    {
                        if (!isHandled && messagesList[i].IsSent && messageFetchCount == INITIAL_FETCH_COUNT) // this has to be done for first load and not paging
                        {
                            refState = messagesList[i].MessageStatus;
                            isHandled = true;
                        }

                        if (refState == ConvMessage.State.SENT_DELIVERED_READ && messagesList[i].MessageStatus < ConvMessage.State.SENT_DELIVERED_READ)
                        {
                            ConvMessage convMess = messagesList[i];
                            if (convMess.FileAttachment == null || (convMess.FileAttachment.FileState == Attachment.AttachmentState.COMPLETED))
                            {
                                convMess.MessageStatus = ConvMessage.State.SENT_DELIVERED_READ;
                                if (idsToUpdate == null)
                                    idsToUpdate = new List<long>();
                                idsToUpdate.Add(convMess.MessageId);
                            }
                        }
                    }

                    #endregion

                    if (messagesList[i].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                    {
                        isPublish = true;
                        if (messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                            ids.Add(Convert.ToString(messagesList[i].MappedMessageId));
                        dbIds.Add(messagesList[i].MessageId);
                        messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                    }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        AddMessageToOcMessages(cm, true);
                    });
                }

                #region perception fix update db
                if (idsToUpdate != null && idsToUpdate.Count > 0)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += (ss, ee) =>
                    {
                        MessagesTableUtils.updateAllMsgStatus(mContactNumber, idsToUpdate.ToArray(), (int)ConvMessage.State.SENT_DELIVERED_READ);
                        idsToUpdate = null;
                    };
                    bw.RunWorkerAsync();
                }
                #endregion


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
                    isMessageLoaded = true;
                    //Scroller.Opacity = 1;
                    //messageListBox.Opacity = 1;
                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    NetworkManager.turnOffNetworkManager = false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void forwardAttachmentMessage()
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG) &&
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is object[])
            {
                object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                ConvMessage forwardedMsg = (ConvMessage)attachmentData[0];
                string sourceMsisdn = (string)attachmentData[1];

                string sourceFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn + "/" + forwardedMsg.MessageId;

                ConvMessage convMessage = new ConvMessage("", mContactNumber,
                    TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.FileAttachment = forwardedMsg.FileAttachment;
                convMessage.IsSms = !isOnHike;
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;

                if (forwardedMsg.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                    convMessage.Message = AppResources.Image_Txt;
                else if (forwardedMsg.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                    convMessage.Message = AppResources.Audio_Txt;
                else if (forwardedMsg.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                    convMessage.Message = AppResources.Video_Txt;
                else if (forwardedMsg.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                {
                    convMessage.Message = AppResources.Location_Txt;
                    convMessage.MetaDataString = forwardedMsg.MetaDataString;
                }
                else if (forwardedMsg.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                {
                    convMessage.Message = AppResources.ContactTransfer_Text;
                    convMessage.MetaDataString = forwardedMsg.MetaDataString;
                }

                convMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                AddMessageToOcMessages(convMessage, false);
                object[] vals = new object[3];
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
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CONTACT_SELECTED))
                ContactTransfer();
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
                AudioFileTransfer();
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARED_LOCATION))
            {
                shareLocation();
            }

        }

        //this function is called from UI thread only. No need to synch.
        private void ScrollToBottom()
        {
            if (this.ocMessages.Count > 0 && (!IsMute || this.ocMessages.Count < App.ViewModel.ConvMap[mContactNumber].MuteVal))
            {
                llsMessages.ScrollTo(this.ocMessages[this.ocMessages.Count - 1]);
            }
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
            mPubSub.addListener(HikePubSub.GROUP_END, this);
            mPubSub.addListener(HikePubSub.GROUP_ALIVE, this);
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
                mPubSub.removeListener(HikePubSub.GROUP_END, this);
                mPubSub.removeListener(HikePubSub.GROUP_ALIVE, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml ::  removeListeners , Exception : " + ex.StackTrace);
            }
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
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml ::  callUser_Click , Exception : " + ex.StackTrace);
            }
        }

        private void addUser_Click(object sender, EventArgs e)
        {
            ContactUtils.saveContact(mContactNumber, new ContactUtils.contactSearch_Callback(saveContactTask_Completed));
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

            App.ViewModel.MessageListPageCollection.Remove(cObj); // removed from observable collection

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
            if (IsMute) // GC is muted , request to unmute
            {
                IsMute = false;
                obj[HikeConstants.TYPE] = "unmute";
                App.ViewModel.ConvMap[mContactNumber].MuteVal = -1;
                ConversationTableUtils.saveConvObject(App.ViewModel.ConvMap[mContactNumber], mContactNumber.Replace(":", "_"));
                muteGroupMenuItem.Text = AppResources.SelectUser_MuteGrp_Txt;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                afterMute = true;
            }
            else // GC is unmute , request to mute
            {
                IsMute = true;
                obj[HikeConstants.TYPE] = "mute";
                App.ViewModel.ConvMap[mContactNumber].MuteVal = this.ocMessages.Count;
                ConversationTableUtils.saveConvObject(App.ViewModel.ConvMap[mContactNumber], mContactNumber.Replace(":", "_"));
                muteGroupMenuItem.Text = AppResources.SelectUser_UnMuteGrp_Txt;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
                afterMute = false;
            }
        }

        private void blockUnblock_Click(object sender, EventArgs e)
        {


            if (mUserIsBlocked) // UNBLOCK REQUEST
            {
                if (showNoSmsLeftOverlay)
                    ToggleControlsToNoSms(true);
                if (isGroupChat)
                {
                    infoMenuItem.IsEnabled = true;
                    App.ViewModel.BlockedHashset.Remove(groupOwner);
                    mPubSub.publish(HikePubSub.UNBLOCK_GROUPOWNER, groupOwner);
                }
                else
                {
                    App.ViewModel.BlockedHashset.Remove(mContactNumber);
                    mPubSub.publish(HikePubSub.UNBLOCK_USER, mContactNumber);
                    emoticonsIconButton.IsEnabled = true;
                    enableSendMsgButton = true;
                    sendIconButton.IsEnabled = sendMsgTxtbox.Text.Length > 0;
                    isTypingNotificationEnabled = true;
                    if (inviteMenuItem != null)
                        inviteMenuItem.IsEnabled = true;
                }
                mUserIsBlocked = false;
                showOverlay(false);
            }
            //else     // BLOCK REQUEST
            //{
            //    if (showNoSmsLeftOverlay)
            //        ToggleControlsToNoSms(false);
            //    this.Focus();
            //    sendMsgTxtbox.Text = "";
            //    if (isGroupChat)
            //    {
            //        mPubSub.publish(HikePubSub.BLOCK_GROUPOWNER, groupOwner);
            //        blockUnblockMenuItem.Text = UNBLOCK_USER + " " + AppResources.SelectUser_GrpOwner_Txt;
            //    }
            //    else
            //    {
            //        mPubSub.publish(HikePubSub.BLOCK_USER, mContactNumber);
            //        emoticonsIconButton.IsEnabled = false;
            //        sendIconButton.IsEnabled = false;
            //        isTypingNotificationEnabled = false;
            //        blockUnblockMenuItem.Text = UNBLOCK_USER;
            //        if (inviteMenuItem != null)
            //            inviteMenuItem.IsEnabled = false;
            //    }
            //    emoticonPanel.Visibility = Visibility.Collapsed;
            //    attachmentMenu.Visibility = Visibility.Collapsed;
            //    mUserIsBlocked = true;
            //    showOverlay(true); //true means show block animation
            //}
        }

        private void FileAttachmentMessage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;
            attachmentMenu.Visibility = Visibility.Collapsed;
            ConvMessage convMessage = llsMessages.SelectedItem as ConvMessage;
            llsMessages.SelectedItem = null;
            if (convMessage == null)
                return;
            if (!isContextMenuTapped)
            {
                if (!isGroupChat && convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE] = statusObject;
                    NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
                }

                if (convMessage.FileAttachment == null || convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return;
                if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED && convMessage.FileAttachment.FileState != Attachment.AttachmentState.STARTED)
                {
                    if (!convMessage.IsSent)
                    {
                        if (NetworkInterface.GetIsNetworkAvailable())
                        {
                            convMessage.SetAttachmentState(Attachment.AttachmentState.STARTED);
                            FileTransfer.Instance.downloadFile(convMessage, mContactNumber.Replace(":", "_"));
                            MessagesTableUtils.addUploadingOrDownloadingMessage(convMessage.MessageId, convMessage);
                        }
                        else
                        {
                            MessageBox.Show(AppResources.No_Network_Txt, AppResources.FileTransfer_ErrorMsgBoxText, MessageBoxButton.OK);
                        }
                    }
                    else
                    {
                        //resend message
                        //chatBubble.setAttachmentState(Attachment.AttachmentState.STARTED);
                        //ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                        //convMessage.IsSms = !isOnHike;
                        //convMessage.HasAttachment = true;
                        //convMessage.MessageId = chatBubble.MessageId;
                        //convMessage.FileAttachment = chatBubble.FileAttachment;
                        convMessage.SetAttachmentState(Attachment.AttachmentState.STARTED);
                        if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                        {
                            convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                        {
                            convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                        {
                            convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Location_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;

                            byte[] locationInfoBytes = null;
                            MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                                convMessage.MessageId, out locationInfoBytes);
                            string locationInfoString = System.Text.Encoding.UTF8.GetString(locationInfoBytes, 0, locationInfoBytes.Length);
                            convMessage.MetaDataString = locationInfoString;
                        }
                        else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                        {
                            convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                "/" + convMessage.FileAttachment.FileKey;
                        }
                        //else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                        //{
                        //    convMessage.MetaDataString = chatBubble.MetaDataString;
                        //}
                        object[] values = new object[2];
                        values[0] = convMessage;
                        // values[1] = chatBubble;
                        mPubSub.publish(HikePubSub.ATTACHMENT_RESEND, values);
                    }
                }
                else
                {
                    displayAttachment(convMessage, false);
                }
            }
            isContextMenuTapped = false;
        }

        public void displayAttachment(ConvMessage convMessage, bool shouldUpdateAttachment)
        {
            string contactNumberOrGroupId = mContactNumber.Replace(":", "_");
            if (shouldUpdateAttachment)
            {
                MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, mContactNumber, convMessage.MessageId);
            }
            if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
            {
                object[] fileTapped = new object[2];
                fileTapped[0] = convMessage.MessageId;
                fileTapped[1] = contactNumberOrGroupId;
                PhoneApplicationService.Current.State["objectForFileTransfer"] = fileTapped;
                NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
            }
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO) || convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
            {
                MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId + "/" + Convert.ToString(convMessage.MessageId);
                mediaPlayerLauncher.Media = new Uri(fileLocation, UriKind.Relative);
                mediaPlayerLauncher.Location = MediaLocationType.Data;
                mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                try
                {
                    mediaPlayerLauncher.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread.xaml ::  displayAttachment ,Ausio video , Exception : " + ex.StackTrace);
                }
            }
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
            {
                try
                {
                    JObject metadataFromConvMessage = JObject.Parse(convMessage.MetaDataString);
                    JToken tempFileArrayToken;
                    JObject locationJSON;
                    if (metadataFromConvMessage.TryGetValue("files", out tempFileArrayToken) && tempFileArrayToken != null)
                    {
                        JArray tempFilesArray = tempFileArrayToken.ToObject<JArray>();
                        locationJSON = tempFilesArray[0].ToObject<JObject>();
                    }
                    else
                    {
                        locationJSON = JObject.Parse(convMessage.MetaDataString);
                    }
                    if (this.bingMapsTask == null)
                        bingMapsTask = new BingMapsTask();
                    double latitude = Convert.ToDouble(locationJSON[HikeConstants.LATITUDE].ToString());
                    double longitude = Convert.ToDouble(locationJSON[HikeConstants.LONGITUDE].ToString());
                    double zoomLevel = Convert.ToDouble(locationJSON[HikeConstants.ZOOM_LEVEL].ToString());
                    bingMapsTask.Center = new GeoCoordinate(latitude, longitude);
                    bingMapsTask.ZoomLevel = zoomLevel;
                    bingMapsTask.Show();
                }
                catch (Exception ex) //Code should never reach here
                {
                    Debug.WriteLine("NewChatTHread :: DisplayAttachment :: Exception while parsing lacation parameters" + ex.StackTrace);
                }
                return;
            }
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
            {
                JObject contactInfoJobject = JObject.Parse(convMessage.MetaDataString);
                ContactCompleteDetails con = ContactCompleteDetails.GetContactDetails(contactInfoJobject);
                SaveContactTask sct = con.GetSaveCotactTask();
                sct.Show();
            }
        }

        private void AddNewMessageToUI(ConvMessage convMessage, bool insertAtTop)
        {
            if (isTypingNotificationActive)
            {
                HideTypingNotification();
                isReshowTypingNotification = true;
            }
            AddMessageToOcMessages(convMessage, insertAtTop);
            if (isReshowTypingNotification)
            {
                ShowTypingNotification();
                isReshowTypingNotification = false;
            }
        }



        /*
      * If readFromDB is true & message state is SENT_UNCONFIRMED, then trying image is set else 
      * it is scheduled
      */
        private void AddMessageToOcMessages(ConvMessage convMessage, bool insertAtTop)
        {
            int insertPosition = 0;
            if (!insertAtTop)
                insertPosition = this.ocMessages.Count;
            try
            {
                #region NO_INFO
                //TODO : Create attachment object if it requires one
                if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    ConvMessage chatBubble = null;
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

                        chatBubble = MessagesTableUtils.getUploadingOrDownloadingMessage(convMessage.MessageId);
                    }

                    if (chatBubble == null)
                    {
                        if (convMessage.IsSent)
                        {
                            chatBubble = convMessage;//todo:split
                            if (convMessage.MessageId > 0 && ((!convMessage.IsSms && convMessage.MessageStatus < ConvMessage.State.SENT_DELIVERED_READ)
                                || (convMessage.IsSms && convMessage.MessageStatus < ConvMessage.State.SENT_CONFIRMED)))
                                msgMap.Add(convMessage.MessageId, chatBubble);
                        }
                        else
                        {
                            chatBubble = convMessage;
                            chatBubble.GroupMemberName = isGroupChat ?
                                GroupManager.Instance.getGroupParticipant(null, convMessage.GroupParticipant, mContactNumber).FirstName + "-" : string.Empty;
                        }
                    }
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region MEMBERS JOINED GROUP CHAT

                // SHOW Group Chat joined / Added msg along with DND msg 
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.MEMBERS_JOINED)
                {
                    string[] vals = convMessage.Message.Split(';');
                    ConvMessage convMessageNew = new ConvMessage(vals[0], this.Orientation, convMessage);
                    convMessageNew.NotificationType = ConvMessage.MessageType.HIKE_PARTICIPANT_JOINED;
                    this.ocMessages.Insert(insertPosition, convMessageNew);
                    insertPosition++;
                    if (vals.Length == 2)
                    {
                        ConvMessage dndChatBubble = new ConvMessage(vals[1], this.Orientation, convMessage);
                        convMessage.NotificationType = ConvMessage.MessageType.WAITING;
                        this.ocMessages.Insert(insertPosition, dndChatBubble);
                        insertPosition++;
                    }
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
                        string text = AppResources.USER_JOINED_GROUP_CHAT;
                        ConvMessage.MessageType type = ConvMessage.MessageType.HIKE_PARTICIPANT_JOINED;
                        if (vars[1] == "0" && !gp.IsOnHike)
                        {
                            text = AppResources.USER_INVITED;
                            type = ConvMessage.MessageType.SMS_PARTICIPANT_INVITED;
                        }
                        ConvMessage chatBubble = new ConvMessage(gp.FirstName + text, this.Orientation, convMessage);
                        chatBubble.NotificationType = type;
                        this.ocMessages.Insert(insertPosition, chatBubble);
                        insertPosition++;
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

                        string text = gp.FirstName + AppResources.USER_JOINED_GROUP_CHAT;
                        ConvMessage.MessageType type = ConvMessage.MessageType.SMS_PARTICIPANT_OPTED_IN;
                        if (showIcon == "0") // DND USER and not OPTED IN add to custom msg i.e waiting etc
                        {
                            if (waitingParticipants == null)
                                waitingParticipants = new List<string>();
                            waitingParticipants.Add(gp.FirstName);
                        }
                        else // if not DND show joined 
                        {
                            ConvMessage chatBubble = new ConvMessage(text, this.Orientation, convMessage);
                            chatBubble.NotificationType = type;
                            this.ocMessages.Insert(insertPosition, chatBubble);
                            insertPosition++;
                        }
                    }
                    if (waitingParticipants == null)
                        return;
                    StringBuilder msgText = new StringBuilder();
                    if (waitingParticipants.Count == 1)
                        msgText.Append(waitingParticipants[0]);
                    else if (waitingParticipants.Count == 2)
                        msgText.Append(waitingParticipants[0] + AppResources.And_txt + waitingParticipants[1]);
                    else
                    {
                        for (int i = 0; i < waitingParticipants.Count; i++)
                        {
                            msgText.Append(waitingParticipants[i]);
                            if (i == waitingParticipants.Count - 2)
                                msgText.Append(AppResources.And_txt);
                            else if (i < waitingParticipants.Count - 2)
                                msgText.Append(",");
                        }
                    }
                    ConvMessage wchatBubble = new ConvMessage(string.Format(AppResources.WAITING_TO_JOIN, msgText.ToString()), this.Orientation, convMessage);
                    wchatBubble.NotificationType = ConvMessage.MessageType.WAITING;
                    this.ocMessages.Insert(insertPosition, wchatBubble);
                }
                #endregion
                #region USER_JOINED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.USER_JOINED_HIKE;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region HIKE_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.HIKE_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.USER_JOINED_HIKE;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region SMS_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.SMS_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    convMessage.NotificationType = ConvMessage.MessageType.SMS_PARTICIPANT_INVITED;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region USER_OPT_IN
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_OPT_IN)
                {
                    ConvMessage.MessageType type = ConvMessage.MessageType.SMS_PARTICIPANT_OPTED_IN;
                    if (Utils.isGroupConversation(mContactNumber))
                    {
                        type = ConvMessage.MessageType.SMS_PARTICIPANT_OPTED_IN;
                    }
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = type;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region DND_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.DND_USER)
                {
                    //if (!Utils.isGroupConversation(mContactNumber))
                    {
                        ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                        chatBubble.NotificationType = ConvMessage.MessageType.WAITING;
                        this.ocMessages.Insert(insertPosition, chatBubble);
                        insertPosition++;
                    }
                }
                #endregion
                #region PARTICIPANT_LEFT
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT)
                {
                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    ConvMessage chatBubble = new ConvMessage(name + AppResources.USER_LEFT, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.PARTICIPANT_LEFT;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region GROUP END
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_END)
                {
                    ConvMessage chatBubble = new ConvMessage(AppResources.GROUP_CHAT_END, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.GROUP_END;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region CREDITS REWARDS
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.REWARD;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region INTERNATIONAL_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.INTERNATIONAL_USER_BLOCKED;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region INTERNATIONAL_GROUPCHAT_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_GROUP_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(AppResources.SMS_INDIA, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.INTERNATIONAL_USER_BLOCKED;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    ConvMessage chatBubbleLeft = new ConvMessage(name + AppResources.USER_LEFT, this.Orientation, convMessage);
                    chatBubbleLeft.NotificationType = ConvMessage.MessageType.PARTICIPANT_LEFT;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region GROUP NAME CHANGED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.GROUP_NAME_CHANGED;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region STATUS UPDATE
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    JObject jsonObj = JObject.Parse(convMessage.MetaDataString);
                    JObject data = (JObject)jsonObj[HikeConstants.DATA];
                    JToken val;
                    #region HANDLE PIC UPDATE
                    if (data.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                    {
                        try
                        {
                            string serverId = (string)jsonObj[HikeConstants.PROFILE_PIC_ID];
                            byte[] imageBytes = MiscDBUtil.GetProfilePicUpdateForID(convMessage.Msisdn, serverId);
                            convMessage.StatusUpdateImage = UI_Utils.Instance.createImageFromBytes(imageBytes);
                            this.ocMessages.Insert(insertPosition, convMessage);
                            insertPosition++;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Exception while inserting Text Update msg : " + e.StackTrace);
                        }
                    }
                    #endregion
                    #region HANDLE TEXT UPDATE
                    val = null;
                    if (data.TryGetValue(HikeConstants.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        try
                        {
                            this.ocMessages.Insert(insertPosition, convMessage);
                            insertPosition++;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("Exception while inserting Text Update msg : " + e.StackTrace);
                        }
                    }
                    #endregion
                }
                #endregion
                #region GROUP PIC CHANGED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.GROUP_PIC_CHANGED;
                    this.ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                //#region STICKER
                //else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.STICKER)
                //{
                //    this.ocMessages.Insert(insertPosition, convMessage);
                //    insertPosition++;
                //}
                //#endregion

                if (!insertAtTop)
                    ScrollToBottom();

            }
            catch (Exception e)
            {
                Debug.WriteLine("NEW CHAT THREAD :: " + e.StackTrace);
            }
        }

        private void inviteUserBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isGroupChat && isOnHike)
                    return;
                long time = TimeUtils.getCurrentTimeStamp();
                string inviteToken = "";
                if (isGroupChat)
                {
                    foreach (GroupParticipant gp in GroupManager.Instance.GroupCache[mContactNumber])
                    {
                        if (!gp.IsOnHike)
                        {
                            ConvMessage convMessage = new ConvMessage(AppResources.sms_invite_message, gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                            convMessage.IsInvite = true;
                            App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                        }
                    }
                }
                else
                {
                    //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
                    ConvMessage convMessage = new ConvMessage(string.Format(AppResources.sms_invite_message, inviteToken), mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                    convMessage.IsSms = true;
                    convMessage.IsInvite = true;
                    sendMsg(convMessage, false);
                }
                if (showNoSmsLeftOverlay || isGroupChat)
                    showOverlay(false);
                if (isGroupChat)
                    App.appSettings.Remove(HikeConstants.SHOW_GROUP_CHAT_OVERLAY);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread :: inviteUserBtn_Click : Exception Occored:{0}", ex.StackTrace);
            }
        }
        #endregion

        #region PAGE EVENTS

        private bool isEmptyString = true;
        private int lastMessageCount = 0;
        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lastText.Equals(sendMsgTxtbox.Text))
                return;

            //done as scrollviewer applied to textbox doesn't update its position on char enter
            svMessage.UpdateLayout();
            svMessage.ScrollToVerticalOffset(sendMsgTxtbox.GetRectFromCharacterIndex(sendMsgTxtbox.SelectionStart).Top - 50.0);

            string msgText = sendMsgTxtbox.Text.Trim();
            if (String.IsNullOrEmpty(msgText))
            {
                isEmptyString = true;
                sendIconButton.IsEnabled = false;
                return;
            }
            if (isEmptyString)
            {
                this.sendMsgTxtbox.Foreground = UI_Utils.Instance.Black;
                isEmptyString = false;
            }
            lastText = msgText;
            lastTextChangedTime = TimeUtils.getCurrentTimeStamp();
            scheduler.Schedule(sendEndTypingNotification, TimeSpan.FromSeconds(5));

            if (endTypingSent)
            {
                endTypingSent = false;
                sendTypingNotification(true);
            }

            if (!isOnHike && msgText.Length > 130)
            {
                spSmsCharCounter.Visibility = Visibility.Visible;
                int numberOfMessages = (msgText.Length / maxSmsCharLength) + 1;

                if (numberOfMessages > 1)
                {
                    txtMsgCount.Visibility = Visibility.Visible;
                    if (lastMessageCount != numberOfMessages)
                    {
                        txtMsgCount.Text = string.Format(AppResources.CT_MessageCount_Sms_User, numberOfMessages);
                        lastMessageCount = numberOfMessages;
                    }
                }
                else
                {
                    txtMsgCount.Visibility = Visibility.Collapsed;
                }

                txtMsgCharCount.Text = string.Format(AppResources.CT_CharCount_Sms_User, msgText.Length, numberOfMessages * maxSmsCharLength);
            }
            else
            {
                spSmsCharCounter.Visibility = Visibility.Collapsed;
            }
            sendIconButton.IsEnabled = enableSendMsgButton;
        }

        private void sendMsgBtn_Click(object sender, EventArgs e)
        {
            if (mUserIsBlocked)
                return;

            this.Focus();
            string message = sendMsgTxtbox.Text.Trim();
            sendMsgTxtbox.Text = string.Empty;
            lastText = string.Empty;
            sendIconButton.IsEnabled = false;
            sendMsgTxtbox.Focus();

            if (String.IsNullOrEmpty(message))
                return;

            emoticonPanel.Visibility = Visibility.Collapsed;
            attachmentMenu.Visibility = Visibility.Collapsed;


            if (message == "" || (!isOnHike && mCredits <= 0))
                return;

            endTypingSent = true;
            sendTypingNotification(false);

            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            convMessage.IsSms = !isOnHike;
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
                BitmapImage image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);
                try
                {
                    SendImage(image, "image_" + TimeUtils.getCurrentTimeStamp().ToString());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("GROUP INFO :: Exception in photochooser task " + ex.StackTrace);
                }
            }
            else if (e.TaskResult == TaskResult.Cancel)
            {
                if (e.Error != null)
                    MessageBox.Show(AppResources.Cannot_Select_Pic_Phone_Connected_to_PC);
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

        private void SendImage(BitmapImage image, string fileName)
        {
            if (!isGroupChat || isGroupAlive)
            {
                byte[] thumbnailBytes;
                byte[] fileBytes;

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;

                WriteableBitmap writeableBitmap = new WriteableBitmap(image);
                int thumbnailWidth, thumbnailHeight, imageWidth, imageHeight;
                adjustAspectRatio(image.PixelWidth, image.PixelHeight, true, out thumbnailWidth, out thumbnailHeight);
                adjustAspectRatio(image.PixelWidth, image.PixelHeight, false, out imageWidth, out imageHeight);

                using (var msSmallImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msSmallImage, thumbnailWidth, thumbnailHeight, 0, 50);
                    thumbnailBytes = msSmallImage.ToArray();
                }
                if (thumbnailBytes.Length > HikeConstants.MAX_THUMBNAILSIZE)
                {
                    using (var msSmallImage = new MemoryStream())
                    {
                        writeableBitmap.SaveJpeg(msSmallImage, thumbnailWidth, thumbnailHeight, 0, 20);
                        thumbnailBytes = msSmallImage.ToArray();
                    }
                }

                if (fileName.StartsWith("{")) // this is from share picker
                {
                    fileName = "PhotoChooser-" + fileName.Substring(1, fileName.Length - 2) + ".jpg";
                }
                else
                    fileName = fileName.Substring(fileName.LastIndexOf("/") + 1) + ".jpg";

                convMessage.FileAttachment = new Attachment(fileName, thumbnailBytes, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = HikeConstants.IMAGE;
                convMessage.Message = AppResources.Image_Txt;

                AddNewMessageToUI(convMessage, false);

                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, imageWidth, imageHeight, 0, 65);
                    fileBytes = msLargeImage.ToArray();
                }
                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = fileBytes;
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

            AddNewMessageToUI(convMessage, false);

            object[] vals = new object[3];
            vals[0] = convMessage;
            vals[1] = isNewGroup;
            mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
        }

        private void sendMsgTxtbox_GotFocus(object sender, RoutedEventArgs e)
        {
            sendMsgTxtbox.Background = textBoxBackground;
            sendMsgTxtbox.Hint = string.Empty;//done intentionally as hint is shown if text is changed
            sendMsgTxtbox.Hint = hintText;
            //this.messageListBox.Margin = UI_Utils.Instance.ChatThreadKeyPadUpMargin;
            //ScrollToBottom();
            if (this.emoticonPanel.Visibility == Visibility.Visible)
                this.emoticonPanel.Visibility = Visibility.Collapsed;
            if (this.attachmentMenu.Visibility == Visibility.Visible)
                this.attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendMsgTxtbox_LostFocus(object sender, RoutedEventArgs e)
        {
            //this.messageListBox.Margin = UI_Utils.Instance.ChatThreadKeyPadDownMargin;
        }


        #endregion

        #region CONTEXT MENU

        private void MenuItem_Click_Forward(object sender, System.Windows.Input.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            if (convMessage.FileAttachment == null)
            {
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = convMessage.Message;
                NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
            }
            else
            {
                object[] attachmentForwardMessage = new object[2];
                attachmentForwardMessage[0] = convMessage;
                attachmentForwardMessage[1] = mContactNumber;
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = attachmentForwardMessage;
                NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
            }
        }

        private void MenuItem_Click_Copy(object sender, System.Windows.Input.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage chatBubble = ((sender as MenuItem).DataContext as ConvMessage);
            if (chatBubble.FileAttachment == null)
                Clipboard.SetText(chatBubble.Message);
            else if (!String.IsNullOrEmpty(chatBubble.FileAttachment.FileKey))
                Clipboard.SetText(HikeConstants.FILE_TRANSFER_COPY_BASE_URL + "/" + chatBubble.FileAttachment.FileKey);
        }

        private void MenuItem_Click_Delete(object sender, System.Windows.Input.GestureEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage msg = ((sender as MenuItem).DataContext as ConvMessage);
            if (msg == null)
            {
                return;
            }
            if (msg.FileAttachment != null && msg.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                msg.SetAttachmentState(Attachment.AttachmentState.CANCELED);
            bool delConv = false;
            this.ocMessages.Remove(msg);
            ConversationListObject obj = App.ViewModel.ConvMap[mContactNumber];

            ConvMessage lastMessageBubble = null;
            if (isTypingNotificationActive && this.ocMessages.Count > 1)
            {
                lastMessageBubble = this.ocMessages[this.ocMessages.Count - 2];
            }
            else if (!isTypingNotificationActive && this.ocMessages.Count > 0)
            {
                lastMessageBubble = this.ocMessages[this.ocMessages.Count - 1];
            }

            if (lastMessageBubble != null)
            {
                //This updates the Conversation list.
                if (lastMessageBubble.FileAttachment != null)
                {

                    if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                        obj.LastMessage = HikeConstants.IMAGE;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                        obj.LastMessage = HikeConstants.AUDIO;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                        obj.LastMessage = HikeConstants.VIDEO;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        obj.LastMessage = HikeConstants.CONTACT;

                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                }
                else if (lastMessageBubble.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    obj.LastMessage = lastMessageBubble.Message;
                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                    obj.TimeStamp = lastMessageBubble.Timestamp;
                    obj.MessageStatus = lastMessageBubble.MessageStatus;

                }
                else if (lastMessageBubble.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {

                    JObject data = JObject.Parse(lastMessageBubble.MetaDataString)[HikeConstants.DATA] as JObject;
                    JToken val;

                    // Profile Pic update
                    if (data.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                    {
                        obj.LastMessage = "\"" + AppResources.Update_Profile_Pic_txt + "\"";
                    }
                    else // status, moods update
                    {
                        obj.LastMessage = "\"" + lastMessageBubble.Message + "\"";
                    }
                    obj.MessageStatus = ConvMessage.State.RECEIVED_READ;
                }
                else
                {
                    obj.LastMessage = lastMessageBubble.Message;
                    obj.MessageStatus = ConvMessage.State.UNKNOWN;
                    obj.TimeStamp = lastMessageBubble.Timestamp;
                }
            }
            else
            {
                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj); // removed from observable collection
                App.ViewModel.ConvMap.Remove(mContactNumber);
                // delete from db will be handled by dbconversation listener
                delConv = true;
            }
            object[] o = new object[3];
            o[0] = msg.MessageId;
            o[1] = obj;
            o[2] = delConv;
            mPubSub.publish(HikePubSub.MESSAGE_DELETED, o);
        }

        private void MenuItem_Click_Cancel(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
            {
                convMessage.SetAttachmentState(Attachment.AttachmentState.CANCELED);
                MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, mContactNumber, convMessage.MessageId);
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
            attachmentMenu.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        private void fileTransferButton_Click(object sender, EventArgs e)
        {
            if (attachmentMenu.Visibility == Visibility.Collapsed)
                attachmentMenu.Visibility = Visibility.Visible;
            else
                attachmentMenu.Visibility = Visibility.Collapsed;
            emoticonPanel.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        private void sendImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                photoChooserTask.Show();
                attachmentMenu.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml :: sendImage_Tap , Exception : " + ex.StackTrace);
            }
        }
        private void clickPhoto_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {

                cameraCaptureTask.Show();
                attachmentMenu.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml :: clickPhoto_Tap , Exception : " + ex.StackTrace);
            }
        }
        private void sendAudio_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/RecordMedia.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendContact_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.SHARE_CONTACT] = true;

            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendVideo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/RecordVideo.xaml", UriKind.Relative));
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
            if (isGroupChat)
                sendMsgTxtbox.Hint = ON_GROUP_TEXT;
            else if (isOnHike)
            {
                sendMsgTxtbox.Hint = ON_HIKE_TEXT;
            }
            else
            {
                sendMsgTxtbox.Hint = ON_SMS_TEXT;
                updateChatMetadata();
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
            smsCounterTxtBlk.Text = string.Format(AppResources.SMS_Left_Txt, Convert.ToString(mCredits));
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

        private void ToggleAlertOnNoSms(bool onEnter)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ToggleControlsToNoSms(onEnter);
                showOverlay(onEnter);
                if (onEnter)
                {
                    if (!isGroupChat)
                    {
                        sendMsgTxtbox.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(SendMsgBtn_Tap);
                        sendMsgTxtbox.IsReadOnly = true;
                    }
                }
                else
                {
                    sendMsgTxtbox.Tap -= new EventHandler<System.Windows.Input.GestureEventArgs>(SendMsgBtn_Tap);
                    sendMsgTxtbox.IsReadOnly = false;
                }
            });
        }

        private void SendMsgBtn_Tap(object sender, EventArgs e)
        {
            showOverlay(true);
        }

        private void ToggleControlsToNoSms(bool toNoSms)
        {
            if (toNoSms)
            {
                BlockTxtBlk.Text = String.Format(AppResources.NoFreeSmsLeft_Txt, isGroupChat ? "SMS particpants" : mContactName);
                btnBlockUnblock.Content = AppResources.FreeSMS_InviteNow_Btn;
                btnBlockUnblock.Click -= blockUnblock_Click;
                btnBlockUnblock.Click += inviteUserBtn_Click;
                overlayRectangle.Tap += new EventHandler<System.Windows.Input.GestureEventArgs>(NoFreeSmsOverlay_Tap);
            }
            else
            {
                BlockTxtBlk.Text = AppResources.SelectUser_BlockMsg_Txt;
                btnBlockUnblock.Content = UNBLOCK_USER;
                btnBlockUnblock.Click += blockUnblock_Click;
                btnBlockUnblock.Click -= inviteUserBtn_Click;
                overlayRectangle.Tap -= new EventHandler<System.Windows.Input.GestureEventArgs>(NoFreeSmsOverlay_Tap);
            }
        }


        private void NoFreeSmsOverlay_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (isGroupChat)
                App.appSettings.Remove(HikeConstants.SHOW_GROUP_CHAT_OVERLAY);
            showOverlay(false);
        }

        private void showOverlay(bool show)
        {
            if (show)
            {
                overlayRectangle.Visibility = System.Windows.Visibility.Visible;
                overlayRectangle.Opacity = 0.85;
                HikeTitle.IsHitTestVisible = false;
                llsMessages.IsHitTestVisible = false;
                bottomPanel.IsHitTestVisible = false;
                OverlayMessagePanel.Visibility = Visibility.Visible;
                emoticonsIconButton.IsEnabled = false;
                sendIconButton.IsEnabled = enableSendMsgButton = false;
                fileTransferIconButton.IsEnabled = false;
            }
            else
            {
                overlayRectangle.Visibility = System.Windows.Visibility.Collapsed;
                HikeTitle.IsHitTestVisible = true;
                llsMessages.IsHitTestVisible = true;
                bottomPanel.IsHitTestVisible = true;
                OverlayMessagePanel.Visibility = Visibility.Collapsed;
                if (isGroupChat && !isGroupAlive)
                {
                    emoticonsIconButton.IsEnabled = false;
                    sendIconButton.IsEnabled = enableSendMsgButton = false;
                    fileTransferIconButton.IsEnabled = false;
                }
                else if (!showNoSmsLeftOverlay)
                {
                    emoticonsIconButton.IsEnabled = true;
                    enableSendMsgButton = true;
                    sendIconButton.IsEnabled = sendMsgTxtbox.Text.Length > 0;
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
                Debug.WriteLine("NewChatThread.xaml :: sendTypingNotification , Exception : " + ex.StackTrace);
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
                {
                    if (convTypingNotification == null)
                    {
                        convTypingNotification = new ConvMessage();
                        convTypingNotification.CurrentOrientation = this.Orientation;
                        convTypingNotification.GrpParticipantState = ConvMessage.ParticipantInfoState.TYPING_NOTIFICATION;
                    }
                    this.ocMessages.Add(convTypingNotification);
                }
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
                if ((!isTypingNotificationEnabled || isTypingNotificationActive) && this.ocMessages.Contains(convTypingNotification))
                    this.ocMessages.Remove(convTypingNotification);
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

                //TODO handle vibration for user profile and GC.
                if ((convMessage.Msisdn != mContactNumber && (convMessage.MetaDataString != null &&
                    convMessage.MetaDataString.Contains(HikeConstants.POKE))) &&
                    convMessage.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    bool isVibrateEnabled = true;
                    App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
                    ConversationListObject cobj;
                    /* Checks to vibrate:
                     * 1. Vibration is On
                     * 2. Msg is for a group conversation
                     * 3. This group exists
                     * 4. This group is not muted
                     * */
                    if (isVibrateEnabled && (!Utils.isGroupConversation(convMessage.Msisdn) || App.ViewModel.ConvMap.TryGetValue(convMessage.Msisdn, out cobj) && !cobj.IsMute))
                    {
                        VibrateController vibrate = VibrateController.Default;
                        vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                    }
                }

                /* Check if this is the same user for which this message is recieved*/
                if (convMessage.Msisdn == mContactNumber)
                {
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_READ;

                    // Update status to received read in db.
                    mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, new long[] { convMessage.MessageId });

                    if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO) // do not notify in case of group end , user left , user joined
                    {
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serializeDeliveryReportRead()); // handle return to sender
                    }
                    if (convMessage.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                        updateLastMsgColor(convMessage.Msisdn);
                    // Update UI
                    HideTypingNotification();
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE)
                        {
                            mContactName = App.ViewModel.ConvMap[convMessage.Msisdn].ContactName;
                            userName.Text = mContactName;
                        }
                        else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED)
                            userImage.Source = App.ViewModel.ConvMap[convMessage.Msisdn].AvatarImage;

                        AddNewMessageToUI(convMessage, false);
                        if (vals.Length == 3)
                        {
                            ConvMessage cm = (ConvMessage)vals[2];
                            if (cm != null)
                                AddNewMessageToUI(cm, false);
                        }
                    });
                }
                else // this is to show toast notification
                {
                    ConversationListObject val;
                    if (App.ViewModel.ConvMap.TryGetValue(convMessage.Msisdn, out val) && val.IsMute) // of msg is for muted conv, ignore msg
                        return;
                    ConversationListObject cObj = vals[1] as ConversationListObject;
                    if (cObj == null) // this will happen in status update msg
                        return;
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
                }
            }

            # endregion

            #region SERVER_RECEIVED_MSG

            else if (HikePubSub.SERVER_RECEIVED_MSG == type)
            {
                long msgId = (long)obj;
                try
                {
                    ConvMessage msg = null;
                    msgMap.TryGetValue(msgId, out msg);
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_CONFIRMED;
                        if (msg.FileAttachment != null && msg.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                        {
                            msg.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread.xaml :: onEventReceived ,SERVER_RECEIVED_MSG Exception : " + ex.StackTrace);
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
                    ConvMessage msg = null;
                    msgMap.TryGetValue(msgId, out msg);
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread.xaml :: onEventReceived ,MESSAGE_DELIVERED, Exception : " + ex.StackTrace);
                }
            }

            #endregion

            #region MESSAGE_DELIVERED_READ

            else if (HikePubSub.MESSAGE_DELIVERED_READ == type)
            {
                object[] vals = (object[])obj;
                long[] ids = (long[])vals[0];
                string msisdnToCheck = (string)vals[1];
                if (msisdnToCheck != mContactNumber || ids == null || ids.Length == 0)
                    return;
                long maxId = 0;
                // TODO we could keep a map of msgId -> conversation objects somewhere to make this faster
                for (int i = 0; i < ids.Length; i++)
                {
                    try
                    {
                        if (maxId < ids[i])
                            maxId = ids[i];

                        ConvMessage msg = null;
                        msgMap.TryGetValue(ids[i], out msg);
                        if (msg != null)
                        {
                            msg.MessageStatus = ConvMessage.State.SENT_DELIVERED_READ;
                            msgMap.Remove(ids[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NewChatThread.xaml :: onEventReceived ,MESSAGE_DELIVERED_READ Exception : " + ex.StackTrace);
                        continue;
                    }
                }
                #region perception fix

                if (msgMap.Count > 0)
                {

                    try
                    {
                        List<long> idsToUpdate = new List<long>();
                        foreach (var kv in msgMap)
                        {
                            if (kv.Key < maxId)
                            {

                                ConvMessage msg = kv.Value;
                                if (msg.FileAttachment == null || (msg.FileAttachment.FileState == Attachment.AttachmentState.COMPLETED))
                                {
                                    idsToUpdate.Add(kv.Key);
                                    msg.MessageStatus = ConvMessage.State.SENT_DELIVERED_READ;
                                }

                            }

                        }
                        // remove these ids from map
                        foreach (long id in idsToUpdate)
                            msgMap.Remove(id);
                        MessagesTableUtils.updateAllMsgStatus(mContactNumber, idsToUpdate.ToArray(), (int)ConvMessage.State.SENT_DELIVERED_READ);
                        idsToUpdate = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NewChatThread :: OnEventRecieved, perception Fix, Exception:" + ex.StackTrace);
                    }
                }
                #endregion
            }

            #endregion

            #region SMS_CREDIT_CHANGED

            else if (HikePubSub.SMS_CREDIT_CHANGED == type)
            {
                int previousCredits = mCredits;
                mCredits = (int)obj;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (mCredits <= 0)
                    {
                        if (isGroupChat)
                        {
                            App.WriteToIsoStorageSettings(HikeConstants.SHOW_GROUP_CHAT_OVERLAY, true);
                            foreach (GroupParticipant gp in GroupManager.Instance.GroupCache[mContactNumber])
                            {
                                if (!gp.IsOnHike)
                                {
                                    ToggleAlertOnNoSms(true);
                                    this.Focus();
                                    break;
                                }
                            }
                        }
                        else if (!isOnHike)
                        {
                            showNoSmsLeftOverlay = true;
                            ToggleAlertOnNoSms(true);
                            Deployment.Current.Dispatcher.BeginInvoke(() => //using ui thread beacuse I want this to happen after togle alert on no sms
                            {
                                showOverlay(false);//on zero sms user should not immediately see overlay
                                this.Focus();
                            });
                        }
                    }
                    else if (previousCredits <= 0)
                    {
                        showNoSmsLeftOverlay = false;
                        ToggleAlertOnNoSms(false);
                    }

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
                if (statusObject is ContactInfo) // this is done to update user profile
                {
                    ContactInfo cn = (ContactInfo)statusObject;
                    cn.OnHike = isOnHike;
                }
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
                    // this shows that typing notification has come for a group chat , which in current case is not
                    if (vals[1] != null) // vals[1] will be null in 1-1 chat
                        return;
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
                string msisdn = (string)obj;
                if (msisdn != mContactNumber)
                    return;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    userImage.Source = App.ViewModel.ConvMap[msisdn].AvatarImage;
                });
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

            #region GROUP ALIVE

            else if (HikePubSub.GROUP_ALIVE == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    string groupId = (string)obj;
                    if (mContactNumber == groupId)
                    {
                        groupChatAlive();
                    }
                });
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
            isGroupAlive = false;
            sendMsgTxtbox.IsHitTestVisible = false;
            appBar.IsMenuEnabled = false;
            sendIconButton.IsEnabled = enableSendMsgButton = false;
            emoticonsIconButton.IsEnabled = false;
            fileTransferIconButton.IsEnabled = false;
        }

        private void groupChatAlive()
        {
            isGroupAlive = true;
            sendMsgTxtbox.IsHitTestVisible = true;
            appBar.IsMenuEnabled = true;
            enableSendMsgButton = true;
            sendIconButton.IsEnabled = sendMsgTxtbox.Text.Length > 0;
            emoticonsIconButton.IsEnabled = true;
            fileTransferIconButton.IsEnabled = true;
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

                string fileName = "Location";

                string locationJSONString = locationJSON.ToString();

                byte[] locationBytes = (new System.Text.UTF8Encoding()).GetBytes(locationJSONString);

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(),
                    ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;

                convMessage.FileAttachment = new Attachment(fileName, imageThumbnail, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = "hikemap/location";
                convMessage.Message = AppResources.Location_Txt;
                convMessage.MetaDataString = locationJSONString;

                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = locationBytes;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        private void AudioFileTransfer()
        {
            bool isAudio = true;
            byte[] fileBytes = null;
            byte[] thumbnail = null;
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
            {
                fileBytes = (byte[])PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED];
                PhoneApplicationService.Current.State.Remove(HikeConstants.AUDIO_RECORDED);
                isAudio = true;
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_RECORDED))
            {
                thumbnail = (byte[])PhoneApplicationService.Current.State[HikeConstants.VIDEO_RECORDED];
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.TEMP_VIDEO_NAME, out fileBytes);
                PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_RECORDED);
                if (fileBytes == null)
                {
                    return;
                }

                isAudio = false;
            }
            if (fileBytes.Length > maxFileSize)
            {
                MessageBox.Show(AppResources.CT_FileSizeExceed_Text, AppResources.CT_FileSizeExceed_Caption_Text, MessageBoxButton.OK);
                return;
            }
            if (!isGroupChat || isGroupAlive)
            {
                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                string fileName;
                if (isAudio)
                {
                    fileName = "aud_" + TimeUtils.getCurrentTimeStamp().ToString();
                    convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.STARTED);
                    convMessage.FileAttachment.ContentType = "audio/voice";
                    convMessage.Message = AppResources.Audio_Txt;
                }
                else
                {
                    fileName = "vid_" + TimeUtils.getCurrentTimeStamp().ToString();
                    convMessage.FileAttachment = new Attachment(fileName, thumbnail, Attachment.AttachmentState.STARTED);
                    convMessage.FileAttachment.ContentType = "video/mp4";
                    convMessage.Message = AppResources.Video_Txt;
                }
                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = fileBytes;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        private void ContactTransfer()
        {
            Contact contact = (Contact)PhoneApplicationService.Current.State[HikeConstants.CONTACT_SELECTED];
            PhoneApplicationService.Current.State.Remove(HikeConstants.CONTACT_SELECTED);

            if (contact != null)
            {
                ContactCompleteDetails con = ContactCompleteDetails.GetContactDetails(contact);
                JObject contactJson = con.SerialiseToJobject();

                string fileName = string.IsNullOrEmpty(con.Name) ? "Contact" : con.Name;

                ConvMessage convMessage = new ConvMessage("", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;

                convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = HikeConstants.CT_CONTACT;
                convMessage.Message = AppResources.ContactTransfer_Text;
                convMessage.MetaDataString = contactJson.ToString(Newtonsoft.Json.Formatting.None);

                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[3];
                vals[0] = convMessage;
                vals[1] = Encoding.UTF8.GetBytes(contactJson.ToString(Newtonsoft.Json.Formatting.None));
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
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml :: smsUser_Click Exception : " + ex.StackTrace);
            }
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

        //TODO - MG try to use sametap event for header n statusBubble
        private void statusBubble_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            if (!isContextMenuTapped && !isGroupChat)
            {
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE] = statusObject;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
            }
        }

        private void userHeader_Tap(object sender, EventArgs e)
        {
            if (isGroupChat)
            {
                if (mUserIsBlocked || !isGroupAlive)
                    return;
                App.AnalyticsInstance.addEvent(Analytics.GROUP_INFO);
                PhoneApplicationService.Current.State[HikeConstants.GROUP_ID_FROM_CHATTHREAD] = mContactNumber;
                PhoneApplicationService.Current.State[HikeConstants.GROUP_NAME_FROM_CHATTHREAD] = mContactName;
                NavigationService.Navigate(new Uri("/View/GroupInfoPage.xaml", UriKind.Relative));
            }
            else
            {
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE] = statusObject;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
            }
        }

        private void userImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.AnalyticsInstance.addEvent(Analytics.SEE_LARGE_PROFILE_PIC);
            object[] fileTapped = new object[1];
            fileTapped[0] = mContactNumber;
            PhoneApplicationService.Current.State["displayProfilePic"] = fileTapped;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
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
                ConvMessage convMessage = new ConvMessage("Nudge!", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = false;
                convMessage.MetaDataString = "{poke:1}";
                sendMsg(convMessage, false);
                bool isVibrateEnabled = true;
                App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);
                if (isVibrateEnabled)
                {
                    VibrateController vibrate = VibrateController.Default;
                    vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                }
            }
        }


        private void messageListBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.Focus();
        }

        private void saveContactTask_Completed(object sender, SaveContactResult e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    ContactUtils.getContact(mContactNumber, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));
                    break;
                case TaskResult.Cancel:
                    MessageBox.Show(AppResources.User_Cancelled_Task_Txt);
                    break;
                case TaskResult.None:
                    MessageBox.Show(AppResources.NoInfoForTask_Txt);
                    break;
            }
        }

        public void contactSearchCompleted_Callback(object sender, ContactsSearchEventArgs e)
        {
            try
            {
                Dictionary<string, List<ContactInfo>> contactListMap = GetContactListMap(e.Results);
                if (contactListMap == null)
                {
                    MessageBox.Show(AppResources.NO_CONTACT_SAVED);
                    return;
                }
                AccountUtils.updateAddressBook(contactListMap, null, new AccountUtils.postResponseFunction(updateAddressBook_Callback));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml :: contactSearchCompleted_Callback, Exception : " + ex.StackTrace);
            }
        }

        private Dictionary<string, List<ContactInfo>> GetContactListMap(IEnumerable<Contact> contacts)
        {
            int count = 0;
            int duplicates = 0;
            Dictionary<string, List<ContactInfo>> contactListMap = null;
            if (contacts == null)
                return null;
            contactListMap = new Dictionary<string, List<ContactInfo>>();
            foreach (Contact cn in contacts)
            {
                CompleteName cName = cn.CompleteName;

                foreach (ContactPhoneNumber ph in cn.PhoneNumbers)
                {
                    if (string.IsNullOrWhiteSpace(ph.PhoneNumber)) // if no phone number simply ignore the contact
                    {
                        count++;
                        continue;
                    }
                    ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber);
                    int idd = cInfo.GetHashCode();
                    cInfo.Id = Convert.ToString(Math.Abs(idd));
                    contactInfo = cInfo;
                    if (contactListMap.ContainsKey(cInfo.Id))
                    {
                        if (!contactListMap[cInfo.Id].Contains(cInfo))
                            contactListMap[cInfo.Id].Add(cInfo);
                        else
                        {
                            duplicates++;
                            Debug.WriteLine("Duplicate Contact !! for Phone Number {0}", cInfo.PhoneNo);
                        }
                    }
                    else
                    {
                        List<ContactInfo> contactList = new List<ContactInfo>();
                        contactList.Add(cInfo);
                        contactListMap.Add(cInfo.Id, contactList);
                    }
                }
            }
            Debug.WriteLine("Total duplicate contacts : {0}", duplicates);
            Debug.WriteLine("Total contacts with no phone number : {0}", count);
            return contactListMap;
        }

        public void updateAddressBook_Callback(JObject obj)
        {
            if ((obj == null) || HikeConstants.FAIL == (string)obj[HikeConstants.STAT])
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
                });
                return;
            }
            JObject addressbook = (JObject)obj["addressbook"];
            if (addressbook == null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER);
                });
                return;
            }
            IEnumerator<KeyValuePair<string, JToken>> keyVals = addressbook.GetEnumerator();
            KeyValuePair<string, JToken> kv;
            int count = 0;
            while (keyVals.MoveNext())
            {
                kv = keyVals.Current;
                JArray entries = (JArray)addressbook[kv.Key];
                for (int i = 0; i < entries.Count; ++i)
                {
                    JObject entry = (JObject)entries[i];
                    string msisdn = (string)entry["msisdn"];
                    if (string.IsNullOrWhiteSpace(msisdn))
                        continue;

                    bool onhike = (bool)entry["onhike"];
                    contactInfo.Msisdn = msisdn;
                    contactInfo.OnHike = onhike;
                    count++;
                }
            }
            UsersTableUtils.addContact(contactInfo);
            Dispatcher.BeginInvoke(() =>
            {
                userName.Text = contactInfo.Name;
                mContactName = contactInfo.Name;
                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                {
                    App.ViewModel.ConvMap[mContactNumber].ContactName = contactInfo.Name;
                }
                else
                {
                    ConversationListObject co = App.ViewModel.GetFav(mContactNumber);
                    if (co != null)
                        co.ContactName = contactInfo.Name;
                }
                if (count > 1)
                {
                    MessageBox.Show(string.Format(AppResources.MORE_THAN_1_CONTACT_FOUND, mContactNumber));
                }
                else
                {
                    appBar.MenuItems.Remove(addUserMenuItem);
                    MessageBox.Show(AppResources.CONTACT_SAVED_SUCCESSFULLY);
                }
            });
        }

        #region Orientation Handling

        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            for (int i = 0; i < ocMessages.Count; i++)
            {
                ocMessages[i].CurrentOrientation = e.Orientation;
            }
        }
        #endregion

        private void llsMessages_ItemRealized(object sender, ItemRealizationEventArgs e)
        {

            if (isMessageLoaded && llsMessages.ItemsSource != null && llsMessages.ItemsSource.Count > 0 && hasMoreMessages)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as ConvMessage).Equals(llsMessages.ItemsSource[0]))
                    {
                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += (s1, ev1) =>
                        {
                            loadMessages(SUBSEQUENT_FETCH_COUNT);
                        };
                        bw.RunWorkerAsync();
                        bw.RunWorkerCompleted += (s1, ev1) =>
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                shellProgress.IsVisible = false;
                            });
                        };
                    }
                }
            }
        }

        #region Stickers_Selection

        private SolidColorBrush _seletedCategory = new SolidColorBrush(Color.FromArgb(255, 0x1b, 0xa1, 0xe2));
        private SolidColorBrush _categoryBackGround = new SolidColorBrush(Color.FromArgb(255, 0x4d, 0x4d, 0x4d));

        private void StickersTab_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            gridEmoticons.Visibility = Visibility.Collapsed;
            gridStickers.Visibility = Visibility.Visible;
            if (imageBox.ItemsSource == null)
            {
                imageBox.ItemsSource = App.stickerHelper.GetStickersByCategory("category1");
            }
        }
        private void Stickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Sticker sticker = imageBox.SelectedItem as Sticker;
            if (sticker == null)
                return;
            ConvMessage conv = new ConvMessage("Sticker", mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            conv.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
            conv.StickerId = sticker.StickerId;
            AddMessageToOcMessages(conv, false);

            mPubSub.publish(HikePubSub.MESSAGE_SENT, conv);

            emoticonPanel.Visibility = Visibility.Collapsed;

        }

        private void StickersBack_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            gridEmoticons.Visibility = Visibility.Visible;
            gridStickers.Visibility = Visibility.Collapsed;
        }

        private void Category1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            stCategory1.Background = _seletedCategory;
            stCategory2.Background = _categoryBackGround;
            stCategory3.Background = _categoryBackGround;
            stCategory4.Background = _categoryBackGround;

            imageBox.Visibility = Visibility.Visible;
            stLoading.Visibility = Visibility.Collapsed;
        }

        private void Category2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            stCategory1.Background = _categoryBackGround;
            stCategory2.Background = _seletedCategory;
            stCategory3.Background = _categoryBackGround;
            stCategory4.Background = _categoryBackGround;

            imageBox.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Visible;
        }

        private void Category3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            stCategory1.Background = _categoryBackGround;
            stCategory2.Background = _categoryBackGround;
            stCategory3.Background = _seletedCategory;
            stCategory4.Background = _categoryBackGround;

            imageBox.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Visible;
        }

        private void Category4_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            stCategory1.Background = _categoryBackGround;
            stCategory2.Background = _categoryBackGround;
            stCategory3.Background = _categoryBackGround;
            stCategory4.Background = _seletedCategory;

            imageBox.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Visible;
        }

        #endregion

    }
    public class ChatThreadTemplateSelector : TemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Determine which template to return;
            ConvMessage convMesssage = (ConvMessage)item;
            if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
            {
                if (convMesssage.IsSent)
                {
                    if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                        return App.newChatThreadPage.dtSentBubbleNudge;
                    if (!string.IsNullOrEmpty(convMesssage.StickerId))
                        return App.newChatThreadPage.dtSentSticker;
                    else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        return App.newChatThreadPage.dtSentBubbleContact;
                    else if (convMesssage.FileAttachment != null)
                        return App.newChatThreadPage.dtSentBubbleFile;
                    else
                        return App.newChatThreadPage.dtSentBubbleText;
                }
                else
                {
                    if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                        return App.newChatThreadPage.dtRecievedBubbleNudge;
                    if (!string.IsNullOrEmpty(convMesssage.StickerId))
                        return App.newChatThreadPage.dtSentSticker;
                    else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        return App.newChatThreadPage.dtRecievedBubbleContact;
                    else if (convMesssage.FileAttachment != null)
                        return App.newChatThreadPage.dtRecievedBubbleFile;
                    else
                        return App.newChatThreadPage.dtRecievedBubbleText;
                }
            }

            else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                return App.newChatThreadPage.dtStatusUpdateBubble;
            else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.TYPING_NOTIFICATION)
                return App.newChatThreadPage.dtTypingNotificationBubble;
            else
                return App.newChatThreadPage.dtNotificationBubble;
        }
    }

}