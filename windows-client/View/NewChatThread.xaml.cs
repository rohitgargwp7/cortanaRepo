using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
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
using Microsoft.Phone.Tasks;
using System.IO;
using System.IO.IsolatedStorage;
using windows_client.Controls;
using System.Text;
using System.Linq;
using System.Globalization;
using Microsoft.Devices;
using Microsoft.Xna.Framework.Media;
using System.Device.Location;
using windows_client.Misc;
using Microsoft.Phone.UserData;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using windows_client.Languages;
using System.Net.NetworkInformation;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;
using System.Collections.ObjectModel;
using windows_client.ViewModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using windows_client.FileTransfers;
using System.Windows.Input;
using Windows.Storage;
using windows_client.Model.Sticker;
using windows_client.utils.ServerTips;
using System.Windows.Resources;

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
        private readonly string HOLD_AND_TALK = AppResources.Hold_And_Talk;
        private readonly string RELEASE_TO_SEND = AppResources.Release_To_Send;
        private readonly string MESSAGE_TOO_SHORT = AppResources.Message_Too_Short;
        private readonly string MESSAGE_CANCELLED = AppResources.Message_Cancelled;

        private const int maxSmsCharLength = 140;
        private string groupOwner = null;
        public string mContactNumber;
        private string mContactName = null;
        private string lastText = String.Empty;
        private string hintText = string.Empty;
        private bool enableSendMsgButton = false;
        private long _lastUpdatedLastSeenTimeStamp = 0;
        private bool _isMute = false;
        private bool isFirstLaunch = true;
        private bool isGroupAlive = true;
        public bool isGroupChat = false;
        private bool mUserIsBlocked;
        private bool isOnHike;
        private bool animatedOnce = false;
        private bool isTypingNotificationActive = false;
        private bool isTypingNotificationEnabled = true;
        private bool isReshowTypingNotification = false;
        private bool showNoSmsLeftOverlay = false;
        private JObject groupCreateJson = null;
        private bool _isNewPin = false;
        private bool _isPinAlter = true;            //this value is that state of Pin doesn't change while tapping header
        private ConvMessage lastPinConvMsg;
        private bool _isOnPage = true;

        bool isDisplayPicSet = false;

        /// <summary>
        /// this map is required for mapping attachment object with convmessage only for 
        /// messages stored in db, other messages would have their attachment object set
        /// </summary>
        private Dictionary<long, Attachment> attachments = null;

        private int mCredits;
        private long lastTextChangedTime;
        private long lastTypingNotificationSentTime;
        private long lastTypingNotificationShownTime;

        private HikePubSub mPubSub;
        DispatcherTimer _h2hOfflineTimer;
        public IScheduler scheduler = Scheduler.NewThread;
        private ConvMessage convTypingNotification;
        ContactInfo contactInfo = null; // this will be used if someone adds an unknown number to addressbook
        private byte[] avatar;
        private BitmapImage avatarImage;
        private ApplicationBar appBar;
        ApplicationBarMenuItem muteGroupMenuItem;
        ApplicationBarMenuItem inviteMenuItem = null;
        ApplicationBarMenuItem clearChatItem;
        ApplicationBarMenuItem pinHistoryMenuItem;
        public ApplicationBarMenuItem addUserMenuItem;
        ApplicationBarMenuItem infoMenuItem;
        ApplicationBarMenuItem emailConversationMenuItem;
        ApplicationBarMenuItem blockMenuItem;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton emoticonsIconButton = null;
        ApplicationBarIconButton stickersIconButton = null;
        ApplicationBarIconButton fileTransferIconButton = null;
        private CameraCaptureTask cameraCaptureTask;
        private object statusObject = null;
        private int _unreadMessageCounter = 0;
        private bool _isHikeBot = false;
        private LastSeenHelper _lastSeenHelper;
        Boolean _isSendAllAsSMSVisible = false;
        private GroupInfo _groupInfo;


        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> sent message bubble mapping

        public ObservableCollection<ConvMessage> ocMessages;

        bool isMessageLoaded;
        public bool IsSMSOptionValid = true;

        public int ResolutionId
        {
            get
            {
                if (Utils.CurrentResolution == Utils.Resolutions.WVGA)
                    return 7;
                else if (Utils.CurrentResolution == Utils.Resolutions.WXGA)
                    return 8;
                else
                    return 9;
            }
        }

        Pivot pivotStickers = null;

        MediaElement mediaElement;
        ConvMessage currentAudioMessage;

        private List<long> idsToUpdate = null; // this will store ids in perception fix case

        public Dictionary<long, ConvMessage> OutgoingMsgsMap      /* This map will contain only outgoing messages */
        {
            get
            {
                return msgMap;
            }
        }

        public bool IsMute
        {
            get
            {
                return _isMute;
            }
            set
            {
                if (value != _isMute)
                    _isMute = value;
            }
        }

        ConvMessage _lastUnDeliveredMessage = null, _tap2SendAsSMSMessage = null;
        public LruCache<string, BitmapImage> lruStickerCache;

        public Dictionary<string, List<ConvMessage>> dictDownloadingSticker = new Dictionary<string, List<ConvMessage>>();

        long lastMessageId = -1;
        bool hasMoreMessages;
        const int INITIAL_FETCH_COUNT = 31;
        const int SUBSEQUENT_FETCH_COUNT = 11;

        // this variable stores the status of last SENT msg
        ConvMessage.State refState = ConvMessage.State.UNKNOWN;

        /// <summary>
        /// defined as the number of active participants in the current group. Excludes sms users.
        /// </summary>
        int _activeUsers = 1;

        #endregion

        #region PAGE BASED FUNCTIONS

        public NewChatThread()
        {
            InitializeComponent();

            _dt = new DispatcherTimer();
            _dt.Interval = TimeSpan.FromMilliseconds(33);

            if (_microphone != null)
            {
                // Event handler for getting audio data when the buffer is full
                _microphone.BufferReady += new EventHandler<EventArgs>(microphone_BufferReady);
            }

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromSeconds(1);
            _progressTimer.Tick += new EventHandler(showWalkieTalkieProgress);

            _lastSeenHelper = new LastSeenHelper();
            _lastSeenHelper.UpdateLastSeen += LastSeenResponseReceived;

            App.ViewModel.RequestLastSeenEvent += RequestLastSeenHandler;
            FileTransfers.FileTransferManager.Instance.UpdateTaskStatusOnUI += FileTransferStatusUpdated;

            ocMessages = new ObservableCollection<ConvMessage>();
            lruStickerCache = new LruCache<string, BitmapImage>(10, 0);

            App.ViewModel.ShowTypingNotification += ShowTypingNotification;
            App.ViewModel.AutohideTypingNotification += AutoHidetypingNotification;

            if (!App.appSettings.TryGetValue(App.SEND_NUDGE, out isNudgeOn))
                isNudgeOn = true;

            if (isNudgeOn)
            {
                llsMessages.DoubleTap -= MessageList_DoubleTap;
                llsMessages.DoubleTap += MessageList_DoubleTap;
            }

            if (App.ViewModel.IsDarkMode)
                darkModeLayer.Visibility = Visibility.Visible;

            TipManager.Instance.ChatScreenTipChanged -= Instance_ShowServerTip;
            TipManager.Instance.ChatScreenTipChanged += Instance_ShowServerTip;
        }

        private void Instance_ShowServerTip(object sender, EventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                ShowServerTips();
            });
        }

        void gcPin_PinContentTapped(object sender, EventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.GC_PIN] = mContactNumber;
            NavigationService.Navigate(new Uri("/View/PinHistory.xaml", UriKind.Relative));
        }

        private void gcPin_LostFocus(object sender, EventArgs e)
        {
            if (_isPinAlter)
            {
                if (_isNewPin)
                    NewPin_Close();
            }
            else
            {
                _isPinAlter = true;
                gcPin.newPinTextBox.Focus();
            }
        }

        void gcPin_RightIconClicked(object sender, EventArgs e)
        {
            gcPin.IsShow(false, false);
            tipControl.Visibility = Visibility.Visible;

            if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
            {
                JObject metadata = App.ViewModel.ConvMap[mContactNumber].MetaData;
                metadata[HikeConstants.PINID] = null;
                App.ViewModel.ConvMap[mContactNumber].MetaData = metadata;
                ConversationTableUtils.updateConversation(App.ViewModel.ConvMap[mContactNumber]);
            }
        }

        void RequestLastSeenHandler(object sender, EventArgs e)
        {
            _lastSeenHelper.UpdateLastSeen -= LastSeenResponseReceived;
            _lastSeenHelper.UpdateLastSeen += LastSeenResponseReceived;
            if (!mUserIsBlocked && !isGroupChat)
                GetUserLastSeen();
        }

        bool isNudgeOn = true;

        void LastSeenResponseReceived(object sender, LastSeenEventArgs e)
        {
            if (e != null)
            {
                if (e.ContactNumber == mContactNumber)
                {
                    _lastSeenHelper.UpdateLastSeen -= LastSeenResponseReceived;

                    long actualTimeStamp = e.TimeStamp;

                    if (e.TimeStamp == -1)
                    {
                        FriendsTableUtils.SetFriendLastSeenTSToFile(mContactNumber, 0);
                        _lastUpdatedLastSeenTimeStamp = 0;
                    }
                    else if (e.TimeStamp == 0)
                    {
                        FriendsTableUtils.SetFriendLastSeenTSToFile(mContactNumber, TimeUtils.getCurrentTimeStamp());
                        _lastUpdatedLastSeenTimeStamp = TimeUtils.getCurrentTimeStamp();

                        if (_isSendAllAsSMSVisible)
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                if (ocMessages == null)
                                    return;

                                if (_isSendAllAsSMSVisible)
                                {
                                    ocMessages.Remove(_tap2SendAsSMSMessage);
                                    _isSendAllAsSMSVisible = false;

                                    UpdateLastSentMessageStatusOnUI();
                                }
                            });
                        }
                    }
                    else
                    {
                        long timedifference;
                        if (App.appSettings.TryGetValue(HikeConstants.AppSettings.TIME_DIFF_EPOCH, out timedifference))
                            actualTimeStamp = e.TimeStamp - timedifference;

                        FriendsTableUtils.SetFriendLastSeenTSToFile(mContactNumber, actualTimeStamp);
                        _lastUpdatedLastSeenTimeStamp = actualTimeStamp;
                    }

                    if (_lastUpdatedLastSeenTimeStamp != 0)
                        UpdateLastSeenOnUI(_lastSeenHelper.GetLastSeenTimeStampStatus(actualTimeStamp), true); //show last seen tip if not shown
                    else
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;
                        });
                    }
                }
            }
            else
            {
                // update old last seen from file
                _lastUpdatedLastSeenTimeStamp = FriendsTableUtils.GetFriendLastSeenTSFromFile(mContactNumber);

                if (_lastUpdatedLastSeenTimeStamp != 0)
                    UpdateLastSeenOnUI(_lastSeenHelper.GetLastSeenTimeStampStatus(_lastUpdatedLastSeenTimeStamp));
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;
                    });
                }
            }
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
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GROUP_CHAT) || this.State.ContainsKey(HikeConstants.GROUP_CHAT))
            {
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.GROUP_CHAT, out statusObject))
                    this.State[HikeConstants.GROUP_CHAT] = statusObject;
                else
                    statusObject = this.State[HikeConstants.GROUP_CHAT];

                PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE) || this.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE))
            {
                //contactInfo
                if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.OBJ_FROM_STATUSPAGE, out statusObject))
                    this.State[HikeConstants.OBJ_FROM_STATUSPAGE] = statusObject;
                else
                    statusObject = this.State[HikeConstants.OBJ_FROM_STATUSPAGE];

                PhoneApplicationService.Current.State.Remove(HikeConstants.OBJ_FROM_STATUSPAGE);
            }

            //whenever chat thread is relaunched, last page is chat thread, we need to remove from backstack
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_CHAT_RELAUNCH))
            {
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                PhoneApplicationService.Current.State.Remove(HikeConstants.IS_CHAT_RELAUNCH);
            }

            while (NavigationService.BackStack.Count() > 1)
                NavigationService.RemoveBackEntry();
        }

        bool _isDraftMessage;

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

            this.llsMessages.ItemsSource = ocMessages;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                // whenever CT is opened , mark last msg as read if received read
                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber) && App.ViewModel.ConvMap[mContactNumber].MessageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    App.ViewModel.ConvMap[mContactNumber].MessageStatus = ConvMessage.State.RECEIVED_READ;
                    ConversationTableUtils.updateLastMsgStatus(App.ViewModel.ConvMap[mContactNumber].LastMsgId, mContactNumber, (int)ConvMessage.State.RECEIVED_READ);
                }

                attachments = MiscDBUtil.getAllFileAttachment(mContactNumber);

                if (isGroupChat && isGroupAlive && _groupInfo == null)
                    _groupInfo = GroupTableUtils.getGroupInfoForId(mContactNumber);

                loadMessages(INITIAL_FETCH_COUNT, true);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    var worker = new BackgroundWorker();

                    worker.DoWork += (se, ee) =>
                    {
                        StartForceSMSTimer(false);
                    };

                    worker.RunWorkerAsync();
                });

                if (isGC)
                {
                    ConvMessage groupCreateCM = new ConvMessage(groupCreateJson, true, false);
                    groupCreateCM.GroupParticipant = groupOwner;
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        groupCreateCM.CurrentOrientation = this.Orientation;
                        sendMsg(groupCreateCM, true);
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, groupCreateJson); // inform others about group
                    });

                }
                App.appSettings.TryGetValue(App.SMS_SETTING, out mCredits);
                registerListeners();
                NetworkManager.turnOffNetworkManager = false;
                App.MqttManagerInstance.connect();
            };
            emotListRecent.ItemsSource = imagePathsForListRecent;
            emotList0.ItemsSource = imagePathsForList0;
            emotList1.ItemsSource = imagePathsForList1;
            emotList2.ItemsSource = imagePathsForList2;
            emotList3.ItemsSource = imagePathsForList3;

            bw.RunWorkerAsync();

            cameraCaptureTask = new CameraCaptureTask();
            cameraCaptureTask.Completed += new EventHandler<PhotoResult>(photoChooserTask_Completed);

            if (App.ViewModel.ConvMap.ContainsKey(mContactNumber) && !string.IsNullOrWhiteSpace(App.ViewModel.ConvMap[mContactNumber].DraftMessage))
            {
                _isDraftMessage = true;
                sendMsgTxtbox.Text = App.ViewModel.ConvMap[mContactNumber].DraftMessage;
                //change image as text changed event is not raised
                actionIcon.Source = UI_Utils.Instance.SendMessageImage;
                _isSendActivated = true;
            }

            IsSMSOptionValid = IsSMSOptionAvalable();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _isOnPage = true;

            App.ViewModel.ResumeBackgroundAudio();//in case of video playback

            if (e.NavigationMode == NavigationMode.Back)
            {
                if (_microphone != null)
                {
                    _microphone.BufferReady -= microphone_BufferReady;
                    _microphone.BufferReady += microphone_BufferReady;
                }
            }

            if (_dt != null)
            {
                _dt.Tick -= dt_Tick;
                _dt.Tick += dt_Tick;
                _dt.Start();
            }

            if (isFirstLaunch)
            {
                if (App.newChatThreadPage != null)
                {
                    App.newChatThreadPage.stickerPallet.Children.Remove(App.newChatThreadPage.pivotStickers);
                }
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, ee) =>
                {
                    HikeViewModel.StickerHelper.InitialiseLowResStickers();
                };
                bw.RunWorkerCompleted += (s, ee) =>
                {
                    CreateStickerPivot();
                    CreateStickerCategories();
                };
                bw.RunWorkerAsync();
                App.newChatThreadPage = this;
            }

            // Launch states
            #region PUSH NOTIFICATION
            // push notification , needs to be handled just once.
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.LAUNCH_FROM_PUSH_MSISDN))
            {
                string msisdn = (PhoneApplicationService.Current.State[HikeConstants.LAUNCH_FROM_PUSH_MSISDN] as string).Trim();
                PhoneApplicationService.Current.State.Remove(HikeConstants.LAUNCH_FROM_PUSH_MSISDN);
                if (Char.IsDigit(msisdn[0]))
                    msisdn = "+" + msisdn;

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
                        contact.Name = Utils.IsHikeBotMsg(msisdn) ? Utils.GetHikeBotName(msisdn) : null;
                        contact.OnHike = true; // this is assumed bcoz there is very less chance for an sms user to send push
                    }

                    this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = statusObject = contact;
                }
                ManagePage();
                //whenever launched from push, there should be no backstack. Navigation to conversation page is handled in onBackKeyPress
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();

                isFirstLaunch = false;
            }
            #endregion
            #region SHARE PICKER
            // share picker , needs to be handled just once
            else if (PhoneApplicationService.Current.State.ContainsKey("SharePicker")) // this will be removed after sending msg
            {
                ManagePageStateObjects();
                ManagePage();
                //whenever launched from file picker, there should be no backstack. Navigation to conversation page is handled in onBackKeyPress
                while (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
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
            else if (isFirstLaunch) // case is first launch and normal launch i.e no tombstone
            {
                ManagePageStateObjects();
                ManagePage();
                isFirstLaunch = false;
            }
            else //removing here because it may be case that user pressed back without selecting any user
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
                this.UpdateLayout();
            }

            /* This is called only when you add more participants to group */
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.IS_EXISTING_GROUP))
            {
                PhoneApplicationService.Current.State.Remove(HikeConstants.IS_EXISTING_GROUP);
                this.State[HikeConstants.GROUP_CHAT] = PhoneApplicationService.Current.State[HikeConstants.GROUP_CHAT];
                PhoneApplicationService.Current.State.Remove(HikeConstants.GROUP_CHAT);
                processGroupJoin(false);
            }

            #endregion

            //File transfer states
            #region AUDIO FT
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED) ||
                PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_RECORDED) ||
                PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_SHARED))
            {
                TransferFile();
            }
            #endregion
            #region SHARE LOCATION
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARED_LOCATION))
            {
                shareLocation();
            }
            #endregion
            #region SHARE CONTACT
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CONTACT_SELECTED))
            {
                ContactTransfer();
            }
            #endregion
            #region MULTIPLE IMAGES
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.MULTIPLE_IMAGES))
            {
                MultipleImagesTransfer();
            }
            #endregion

            #region Server Tips

            ShowServerTips();

            #endregion

            if (isGroupChat)
            {
                #region GC_PINS_EVENTS_ASSIGN
                gcPin.RightIconClicked -= gcPin_RightIconClicked;
                gcPin.RightIconClicked += gcPin_RightIconClicked;
                gcPin.NewPinLostFocus -= gcPin_LostFocus;
                gcPin.NewPinLostFocus += gcPin_LostFocus;
                gcPin.PinContentTapped -= gcPin_PinContentTapped;
                gcPin.PinContentTapped += gcPin_PinContentTapped;
                #endregion

                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                {
                    JObject metadata = App.ViewModel.ConvMap[mContactNumber].MetaData;

                    if (metadata != null)
                    {
                        if (metadata.Value<bool>(HikeConstants.READPIN) == false)
                        {
                            metadata[HikeConstants.UNREADPINS] = metadata.Value<int>(HikeConstants.UNREADPINS) - 1;
                            metadata[HikeConstants.READPIN] = true;

                            App.ViewModel.ConvMap[mContactNumber].MetaData = metadata;
                            ConversationTableUtils.updateConversation(App.ViewModel.ConvMap[mContactNumber]);
                        }

                        gcPin.SetUnreadPinCount(metadata.Value<int>(HikeConstants.UNREADPINS));
                    }
                }
            }

            if (_patternNotLoaded)
                CreateBackgroundImage();
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            _isOnPage = false;

            if (_microphone != null)
                _microphone.BufferReady -= microphone_BufferReady;

            if (e.IsNavigationInitiator)
            {
                if (mediaElement != null)
                {
                    if (currentAudioMessage != null)
                    {
                        currentAudioMessage.IsStopped = true;
                        currentAudioMessage.IsPlaying = false;
                        currentAudioMessage.PlayProgressBarValue = 0;
                        currentAudioMessage = null;
                    }

                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                    mediaElement.Stop();
                    App.ViewModel.ResumeBackgroundAudio();
                    mediaElement.Source = null;
                }
            }
            else
            {
                if (mediaElement != null)
                {
                    if (currentAudioMessage != null)
                    {
                        PhoneApplicationService.Current.State[HikeConstants.PLAYER_TIMER] = mediaElement.Position;
                        mediaElement.Pause();
                        App.ViewModel.ResumeBackgroundAudio();

                        currentAudioMessage.IsStopped = false;
                        currentAudioMessage.IsPlaying = false;
                    }
                }
            }

            if (_dt != null)
                _dt.Stop();

            if (!string.IsNullOrEmpty(mContactNumber) && App.ViewModel.ConvMap.ContainsKey(mContactNumber) && App.ViewModel.ConvMap[mContactNumber].DraftMessage != sendMsgTxtbox.Text.Trim())
            {
                App.ViewModel.ConvMap[mContactNumber].DraftMessage = sendMsgTxtbox.Text.Trim();
                ConversationTableUtils.saveConvObject(App.ViewModel.ConvMap[mContactNumber], mContactNumber.Replace(":", "_"));//to update file in case of tombstoning
                ConversationTableUtils.saveConvObjectList();
            }
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            App.IS_TOMBSTONED = false;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            try
            {
                //remove new group pic key
                PhoneApplicationService.Current.State.Remove(App.HAS_CUSTOM_IMAGE);

                App.ViewModel.RequestLastSeenEvent -= RequestLastSeenHandler;

                base.OnRemovedFromJournal(e);
                removeListeners();
                RemoveEmmaBot();
                if (mediaElement != null)
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                    mediaElement.Stop();
                    App.ViewModel.ResumeBackgroundAudio();

                    if (currentAudioMessage != null)
                    {
                        currentAudioMessage.IsStopped = true;
                        currentAudioMessage.IsPlaying = false;
                        currentAudioMessage.PlayProgressBarValue = 0;
                        currentAudioMessage.PlayTimeText = currentAudioMessage.DurationText;
                        currentAudioMessage = null;
                    }
                }

                try
                {
                    if (_recorderState == RecorderState.RECORDING || _recorderState == RecorderState.PLAYING)
                        stopWalkieTalkieRecording();
                    _dt.Stop();
                    _microphone.BufferReady -= this.microphone_BufferReady;
                    _buffer = null;
                    _stream.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread.xaml :: OnRemovedFromJournal, Exception : " + ex.StackTrace);
                }

                FileTransfers.FileTransferManager.Instance.UpdateTaskStatusOnUI -= FileTransferStatusUpdated;

                stickerPallet.Children.Remove(pivotStickers);
                StickerPivotHelper.Instance.ClearData();
                ClearPageResources();
                if (App.newChatThreadPage == this)
                    App.newChatThreadPage = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (_isContextMenuOpen)
            {
                base.OnBackKeyPress(e);
                return;
            }

            if (gridDownloadStickers.Visibility == Visibility.Visible)
            {
                ShowDownloadOverlay(false);
                e.Cancel = true;
                return;                 // So that Sticker and emoji's panel doesn't collapse
            }

            if (emoticonPanel.Visibility == Visibility.Visible)
            {
                emoticonPanel.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
            {
                CancelBackgroundChange();
                e.Cancel = true;
                return;
            }

            if (attachmentMenu.Visibility == Visibility.Visible)
            {
                attachmentMenu.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }

            if (_isNewPin)
            {
                NewPin_Close();
                e.Cancel = true;
                return;
            }

            if (mediaElement != null)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                mediaElement.Stop();
                App.ViewModel.ResumeBackgroundAudio();
            }

            if (!NavigationService.CanGoBack)// if no page to go back in this case back would go to conversation list
            {
                e.Cancel = true;

                //current uri mapping is new chat thread so update mapping to conversation list
                //do this whenever you have to navigate to newly created conversation list page
                App page = (App)Application.Current;
                ((UriMapper)(page.RootFrame.UriMapper)).UriMappings[0].MappedUri = new Uri("/View/ConversationsList.xaml", UriKind.Relative);
                page.RootFrame.Navigate(new Uri("/View/ConversationsList.xaml", UriKind.Relative));

                return;
            }

            App.ViewModel.SelectedBackground = null;

            base.OnBackKeyPress(e);
        }

        private void ClearPageResources()
        {
            ocMessages.Clear();
            lruStickerCache.Clear();
        }

        public void RemoveEmmaBot()
        {
            if (_isHikeBot && mContactNumber == HikeConstants.FTUE_HIKEBOT_MSISDN && App.appSettings.Contains(HikeConstants.AppSettings.REMOVE_EMMA))
            {
                ConversationListObject convObj;
                if (App.ViewModel.ConvMap.TryGetValue(mContactNumber, out convObj))
                {
                    Logging.LogWriter.Instance.WriteToLog(string.Format("CONVERSATION DELETION:Removed emma bot,msisdn:{0}, name:{1}", convObj.Msisdn, convObj.ContactName));
                    App.ViewModel.ConvMap.Remove(convObj.Msisdn);
                    App.ViewModel.MessageListPageCollection.Remove(convObj); // removed from observable collection
                    mPubSub.publish(HikePubSub.DELETE_CONVERSATION, convObj.Msisdn);
                    mPubSub.publish(HikePubSub.DELETE_STATUS_AND_CONV, convObj);//to update ui of conversation list page
                }
            }
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
                    GroupManager.Instance.LoadGroupParticipants(mContactNumber);
                    isGroupChat = true;
                    BlockTxtBlk.Text = AppResources.SelectUser_BlockedGroupMsg_Txt;
                    _groupInfo = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    if (_groupInfo != null)
                    {
                        groupOwner = _groupInfo.GroupOwner;
                        isGroupAlive = _groupInfo.GroupAlive;
                    }
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
                string id = (string)PhoneApplicationService.Current.State[App.NEW_GROUP_ID];
                mContactNumber = id;

                mContactName = (string)PhoneApplicationService.Current.State[App.GROUP_NAME];
                groupOwner = App.MSISDN;

                if (PhoneApplicationService.Current.State.ContainsKey(App.HAS_CUSTOM_IMAGE))
                    isDisplayPicSet = true;

                processGroupJoin(true);

                isOnHike = true;
                isGroupChat = true;

                ConversationListObject convObj = new ConversationListObject();
                convObj.Msisdn = mContactNumber;
                convObj.ContactName = mContactName;
                convObj.IsOnhike = true;

                userImage.Source = UI_Utils.Instance.getDefaultGroupAvatar(mContactNumber, false);

                HandleNewGroup(convObj);
            }

            #endregion

            #region OBJECT FROM SELECT USER PAGE

            else if (this.State.ContainsKey(HikeConstants.OBJ_FROM_SELECTUSER_PAGE))
            {
                ContactInfo obj = (ContactInfo)this.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE];
                if (obj.HasCustomPhoto) // represents group chat
                {
                    GroupManager.Instance.LoadGroupParticipants(obj.Msisdn);
                    isGroupChat = true;
                    BlockTxtBlk.Text = AppResources.SelectUser_BlockedGroupMsg_Txt;
                    _groupInfo = GroupTableUtils.getGroupInfoForId(obj.Msisdn);
                    if (_groupInfo != null)
                    {
                        groupOwner = _groupInfo.GroupOwner;
                        isGroupAlive = _groupInfo.GroupAlive;
                    }
                    ConversationListObject cobj;
                    if (App.ViewModel.ConvMap.TryGetValue(obj.Msisdn, out cobj))
                        IsMute = cobj.IsMute;
                }
                mContactNumber = obj.Msisdn;
                if (obj.Name != null)
                    mContactName = obj.Name;
                else
                    mContactName = obj.Msisdn;

                isAddUser = !obj.IsInAddressBook;
                isOnHike = obj.OnHike;

                avatarImage = UI_Utils.Instance.GetBitmapImage(mContactNumber, isOnHike);
                userImage.Source = avatarImage;
            }
            #endregion
            #region OBJECT FROM STATUS PAGE
            else if (this.State.ContainsKey(HikeConstants.OBJ_FROM_STATUSPAGE))
            {
                object obj = this.State[HikeConstants.OBJ_FROM_STATUSPAGE];
                if (obj is ConversationListObject) //from timeline
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
                    if (App.IS_TOMBSTONED || co.Avatar == null) // in this case avatar needs to be re calculated
                    {
                        co.Avatar = MiscDBUtil.getThumbNailForMsisdn(mContactNumber);
                    }

                    avatarImage = co.AvatarImage;
                    userImage.Source = co.AvatarImage;
                }
                else // from user profile
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
                        avatarImage = UI_Utils.Instance.getDefaultAvatar(mContactNumber, false);
                    else
                        avatarImage = UI_Utils.Instance.createImageFromBytes(avatar);

                    userImage.Source = avatarImage;
                }
            }
            #endregion

            mUserIsBlocked = groupOwner != null ? App.ViewModel.BlockedHashset.Contains(groupOwner) : App.ViewModel.BlockedHashset.Contains(mContactNumber);
            userName.Text = mContactName;

            _isHikeBot = Utils.IsHikeBotMsg(mContactNumber);

            initAppBar(isAddUser);

            if (isGroupChat)
            {
                Logging.LogWriter.Instance.WriteToLog(string.Format("Chat thread opened for groupid:{0}, gpname,{1}", mContactNumber, mContactName));
                #region GCPIN
                openNewPinGrid.Visibility = Visibility.Visible;

                //Checking if GC_Pin is present or not
                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                {
                    JObject metadata = App.ViewModel.ConvMap[mContactNumber].MetaData;

                    if (metadata != null)
                    {
                        JToken pinId = null;
                        if (metadata.TryGetValue(HikeConstants.PINID, out pinId) && pinId != null) //to be Checked if value is null && load Last Pin Message
                        {
                            BackgroundWorker latestPinBW = new BackgroundWorker();
                            latestPinBW.RunWorkerCompleted += latestPinBW_RunWorkerCompleted;
                            latestPinBW.DoWork += (s, e) =>
                                {
                                    lastPinConvMsg = MessagesTableUtils.getMessagesForMsgId(metadata.Value<long>(HikeConstants.PINID));

                                    if (lastPinConvMsg != null && !lastPinConvMsg.IsSent)
                                    {
                                        var gp = GroupManager.Instance.GetGroupParticipant(null, lastPinConvMsg.GroupParticipant, mContactNumber);
                                        lastPinConvMsg.GroupMemberName = gp.FirstName;
                                    }
                                };
                            latestPinBW.RunWorkerAsync();
                        }
                    }
                }
                #endregion

                chatThemeTipTxt.Text = AppResources.ChatThemeMessage_GrpMessage;

                GroupManager.Instance.LoadGroupParticipants(mContactNumber);

                if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                    lastSeenTxt.Text = String.Format(AppResources.People_In_Group, GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(gp => gp.HasLeft == false).Count() + 1);
                else
                    lastSeenTxt.Text = String.Empty;

                if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                    _activeUsers = GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(g => g.HasLeft == false && g.IsOnHike == true).Count();

                sendMsgTxtbox.Hint = hintText = ON_GROUP_TEXT;
            }
            else
                sendMsgTxtbox.Hint = hintText = isOnHike ? ON_HIKE_TEXT : ON_SMS_TEXT;

            if (!isOnHike)
            {
                initInviteMenuItem();
                appBar.MenuItems.Add(inviteMenuItem);
            }
            else
                chatThemeTip.Visibility = Visibility.Visible;

            initBlockUnblockState();

            ChatBackgroundHelper.Instance.SetSelectedBackgorundFromMap(mContactNumber);

            if (!mUserIsBlocked)
            {
                UpdateChatStatus();
                this.ApplicationBar = appBar;

                if (!isGroupChat)
                {
                    GetOnHikeStatus();
                    GetUserLastSeen();
                }
            }
            else
                this.ApplicationBar = appBar;


            if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                _unreadCount = App.ViewModel.ConvMap[mContactNumber].UnreadCounter;

            chatBackgroundList.ItemsSource = ChatBackgroundHelper.Instance.BackgroundList;
            chatBackgroundList.SelectedItem = ChatBackgroundHelper.Instance.BackgroundList.Where(c => c == App.ViewModel.SelectedBackground).First();

            ChangeBackground(false);
        }

        void latestPinBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (lastPinConvMsg != null)
            {
                if (!String.IsNullOrEmpty(lastPinConvMsg.MetaDataString) && lastPinConvMsg.MetaDataString.Contains(HikeConstants.LONG_MESSAGE))
                {
                    string message = MessagesTableUtils.ReadLongMessageFile(lastPinConvMsg.Timestamp, lastPinConvMsg.Msisdn);

                    if (message.Length > 0)
                        lastPinConvMsg.Message = message;
                }

                gcPin.UpdateContent(lastPinConvMsg.GCPinMessageSenderName, lastPinConvMsg.Message);
                gcPin.IsShow(false, true);
            }
        }

        private async void HandleNewGroup(ConversationListObject convObj)
        {
            await Task.Delay(1);

            if (isDisplayPicSet)
            {
                var fullViewImageBytes = MiscDBUtil.getLargeImageForMsisdn(mContactNumber);

                byte[] thumbnailBytes = null;

                if (fullViewImageBytes != null)
                {
                    try
                    {
                        MemoryStream memStream = new MemoryStream(fullViewImageBytes);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage grpImage = new BitmapImage();
                        grpImage.SetSource(memStream);
                        userImage.Source = grpImage;

                        WriteableBitmap writeableBitmap = new WriteableBitmap(grpImage);
                        using (var msLargeImage = new MemoryStream())
                        {
                            writeableBitmap.SaveJpeg(msLargeImage, 83, 83, 0, 95);
                            thumbnailBytes = msLargeImage.ToArray();
                        }

                        object[] vals = new object[3];
                        vals[0] = mContactNumber;
                        vals[1] = fullViewImageBytes;
                        vals[2] = thumbnailBytes;
                        mPubSub.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);

                        convObj.Avatar = thumbnailBytes;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NewChatThread ::  HandleNewGroup , Exception : " + ex.StackTrace);
                        userImage.Source = UI_Utils.Instance.getDefaultGroupAvatar((string)App.appSettings[App.MSISDN_SETTING], false);
                    }
                }
            }

            /* This is done so that after Tombstone when this page is launched, no group is created again and again */
            this.State.Add(HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE, convObj);
            PhoneApplicationService.Current.State.Remove(App.NEW_GROUP_ID);
            PhoneApplicationService.Current.State.Remove(App.GROUP_NAME);
            PhoneApplicationService.Current.State.Remove(App.HAS_CUSTOM_IMAGE);
        }

        int _unreadCount = 0;

        private void UpdateChatStatus()
        {
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
                            foreach (GroupParticipant gp in GroupManager.Instance.GroupParticpantsCache[mContactNumber])
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
        }

        private void GetOnHikeStatus()
        {
            if (!isOnHike)
            {
                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += (ss, ee) =>
                {
                    long timeOfJoin;
                    FriendsTableUtils.GetFriendInfo(mContactNumber, out timeOfJoin);

                    if (timeOfJoin == 0)
                        AccountUtils.GetOnhikeDate(mContactNumber, new AccountUtils.postResponseFunction(GetHikeStatus_Callback));
                    else
                    {
                        isOnHike = true;
                        UpdateUiForHikeUser();
                    }
                };

                worker.RunWorkerAsync();

                spContactTransfer.IsEnabled = false;
            }
        }

        BackgroundWorker _lastSeenWorker;
        private void GetUserLastSeen()
        {
            if (!App.appSettings.Contains(App.LAST_SEEN_SEETING))
            {
                if (_lastSeenWorker == null)
                {
                    _lastSeenWorker = new BackgroundWorker();

                    _lastSeenWorker.DoWork += (ss, ee) =>
                    {
                        var fStatus = FriendsTableUtils.GetFriendStatus(mContactNumber);
                        if (fStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT && !isGroupChat && isOnHike)
                            _lastSeenHelper.requestLastSeen(mContactNumber);
                        else
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;
                            });
                        }
                    };
                }

                if (!_lastSeenWorker.IsBusy)
                    _lastSeenWorker.RunWorkerAsync();
            }
            else
            {
                lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;
            }
        }

        private void UpdateUiForHikeUser()
        {
            if (statusObject is ContactInfo)
            {
                ContactInfo cinfo = (ContactInfo)statusObject;
                cinfo.OnHike = true;
            }
            else if (statusObject is ConversationListObject)
            {
                ConversationListObject co = (ConversationListObject)statusObject;
                co.IsOnhike = true;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!isGroupChat)
                    sendMsgTxtbox.Hint = hintText = ON_HIKE_TEXT;

                spContactTransfer.IsEnabled = true;
                chatPaint.Opacity = 1;

                if (isGroupChat)
                    newPin.Opacity = 1;

                if (appBar.MenuItems.Contains(inviteMenuItem))
                    appBar.MenuItems.Remove(inviteMenuItem);

                if (ocMessages != null && ocMessages.Count > 0)
                {
                    foreach (var msg in ocMessages)
                    {
                        if (msg.IsSms)
                            msg.IsSms = false;
                    }
                }

                showNoSmsLeftOverlay = false;
                ToggleAlertOnNoSms(false);

                lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;

                if (isOnHike)
                    chatThemeTip.Visibility = Visibility.Visible;

                this.ApplicationBar = appBar;
            });

            ContactUtils.UpdateGroupParticpantsCacheWithContactOnHike(mContactNumber, true);
        }

        public void GetHikeStatus_Callback(JObject obj)
        {
            if (obj != null && HikeConstants.FAIL != (string)obj[HikeConstants.STAT])
            {
                var isonhike = (bool)obj["onhike"];
                if (isonhike != isOnHike)
                {
                    isOnHike = isonhike;
                    JObject j = (JObject)obj["profile"];
                    long time = (long)j["jointime"];
                    FriendsTableUtils.SetJoiningTime(mContactNumber, time);
                    UpdateUiForHikeUser();
                }
            }
        }

        bool IsSMSOptionAvalable()
        {
            bool showFreeSMS = true;
            App.appSettings.TryGetValue<bool>(App.SHOW_FREE_SMS_SETTING, out showFreeSMS);

            if (!showFreeSMS) // if setting is off return false
                return showFreeSMS; // == false

            if (Utils.isGroupConversation(mContactNumber))//groupchat
            {
                GroupManager.Instance.LoadGroupParticipants(mContactNumber);

                if (GroupManager.Instance.GroupParticpantsCache != null && GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                {
                    showFreeSMS = (from groupParticipant in GroupManager.Instance.GroupParticpantsCache[mContactNumber]
                                   where groupParticipant.Msisdn.Contains(HikeConstants.INDIA_COUNTRY_CODE)
                                   select groupParticipant).Count() == 0 ? false : true;
                }
            }
            else if (!mContactNumber.Contains(HikeConstants.INDIA_COUNTRY_CODE)) //Indian receiver
                showFreeSMS = false;

            return showFreeSMS;
        }

        private void processGroupJoin(bool isNewgroup)
        {
            List<ContactInfo> contactsForGroup = this.State[HikeConstants.GROUP_CHAT] as List<ContactInfo>;
            List<GroupParticipant> usersToAdd = new List<GroupParticipant>();

            if (isNewgroup) // if new group add all members to the group
            {
                List<GroupParticipant> l = new List<GroupParticipant>(contactsForGroup.Count);

                for (int i = 0; i < contactsForGroup.Count; i++)
                {
                    GroupParticipant gp = new GroupParticipant(mContactNumber, contactsForGroup[i].Name, contactsForGroup[i].Msisdn, contactsForGroup[i].OnHike);
                    gp.IsInAddressBook = contactsForGroup[i].IsInAddressBook;
                    l.Add(gp);
                    usersToAdd.Add(gp);
                }

                GroupManager.Instance.GroupParticpantsCache[mContactNumber] = l;
            }
            else // existing group so just add members
            {
                for (int i = 0; i < contactsForGroup.Count; i++)
                {
                    GroupParticipant gp = null;
                    bool addNewparticipant = true;
                    List<GroupParticipant> gl = GroupManager.Instance.GroupParticpantsCache[mContactNumber];

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
                        GroupManager.Instance.GroupParticpantsCache[mContactNumber].Add(gp);
                    }

                    gp.IsInAddressBook = contactsForGroup[i].IsInAddressBook;
                    usersToAdd.Add(gp);
                }
            }

            if (usersToAdd.Count == 0)
                return;

            GroupManager.Instance.GroupParticpantsCache[mContactNumber].Sort();
            usersToAdd.Sort();
            GroupManager.Instance.SaveGroupParticpantsCache(mContactNumber);

            groupCreateJson = createGroupJsonPacket(HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN, usersToAdd, isNewgroup);

            if (!isNewgroup)
            {
                _groupInfo = GroupTableUtils.getGroupInfoForId(mContactNumber);
                if (_groupInfo != null && string.IsNullOrEmpty(_groupInfo.GroupName)) // set groupname if not alreay set
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
                    GroupInfo gi = new GroupInfo(mContactNumber, mContactName, groupOwner, true);
                    GroupTableUtils.addGroupInfo(gi);
                };
                bw.RunWorkerAsync();
            }
            else
            {
                lastSeenTxt.Text = String.Format(AppResources.People_In_Group, GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(gp => gp.HasLeft == false).Count() + 1);

                ConvMessage cm = new ConvMessage(groupCreateJson, true, true);
                cm.CurrentOrientation = this.Orientation;
                sendMsg(cm, true);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, groupCreateJson); // inform others about group
            }
        }

        private JObject createGroupJsonPacket(string type, List<GroupParticipant> usersToAdd, bool isNewGroup)
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

                    obj[HikeConstants.DATA] = array;
                }
                Debug.WriteLine("GROUP JSON : " + obj.ToString());

                if (isNewGroup)
                {
                    JObject metaData = new JObject();
                    metaData.Add(HikeConstants.NAME, mContactName);

                    if (isDisplayPicSet)
                        metaData.Add(HikeConstants.REQUEST_DISPLAY_PIC, true);

                    obj.Add(HikeConstants.METADATA, metaData);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("ConvMessage" + "invalid json message" + e.Message);
            }
            return obj;
        }

        ConvMessage _unreadMsg = null;

        private void loadMessages(int messageFetchCount, bool isInitialLaunch)
        {
            int i;
            bool isPublish = false;
            hasMoreMessages = false;

            if (isInitialLaunch && _unreadCount > 0)
            {
                var msgStr = _unreadCount > 1 ? String.Format(AppResources.Unread_Messages_Txt, _unreadCount) : String.Format(AppResources.Unread_Message_Txt, _unreadCount);
                _unreadMsg = new ConvMessage(msgStr, mContactNumber, 0, ConvMessage.State.UNKNOWN);
                _unreadMsg.GrpParticipantState = ConvMessage.ParticipantInfoState.UNREAD_NOTIFICATION;
                messageFetchCount = messageFetchCount <= _unreadCount ? _unreadCount + 10 : messageFetchCount;
            }

            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber, lastMessageId < 0 ? long.MaxValue : lastMessageId, messageFetchCount);

            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (!isGroupChat && ocMessages.Count == 0 && isNudgeOn)
                        nudgeTut.Visibility = Visibility.Visible;

                    if (clearChatItem != null && clearChatItem.IsEnabled)
                        clearChatItem.IsEnabled = false;

                    if (emailConversationMenuItem != null && emailConversationMenuItem.IsEnabled)
                        emailConversationMenuItem.IsEnabled = false;

                    if (isGroupChat && pinHistoryMenuItem != null && pinHistoryMenuItem.IsEnabled)
                        pinHistoryMenuItem.IsEnabled = false;

                    progressBar.Opacity = 0;
                    progressBar.IsEnabled = false;
                    forwardAttachmentMessage();
                    this.UpdateLayout();
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

                    if (refState == ConvMessage.State.SENT_DELIVERED_READ && (messagesList[i].MessageStatus < ConvMessage.State.SENT_DELIVERED_READ || messagesList[i].MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE))
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

                    if (messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO || messagesList[i].GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                        ids.Add(Convert.ToString(messagesList[i].MappedMessageId));

                    dbIds.Add(messagesList[i].MessageId);
                    messagesList[i].MessageStatus = ConvMessage.State.RECEIVED_READ;
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    AddMessageToOcMessages(cm, true, false);
                });

                if (isInitialLaunch && i == _unreadCount - 1 && _unreadMsg != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            if (_unreadMsg != null && llsMessages != null)
                            {
                                AddMessageToOcMessages(_unreadMsg, true, false);

                                if (ocMessages.Contains(_unreadMsg))
                                    llsMessages.ScrollTo(_unreadMsg);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("_unreadMsg cannot be scrolled: " + ex.StackTrace);
                        }
                    });
                }
            }

            #region perception fix update db
            if (idsToUpdate != null && idsToUpdate.Count > 0)
            {
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (ss, ee) =>
                {
                    if (!isGroupChat)//todo:why gc check
                    {
                        MessagesTableUtils.updateAllMsgStatus(mContactNumber, idsToUpdate.ToArray(), (int)ConvMessage.State.SENT_DELIVERED_READ);
                        idsToUpdate = null;
                    }
                };
                bw.RunWorkerAsync();
            }
            #endregion

            if (isInitialLaunch && statusObject != null && statusObject is ConversationListObject && !string.IsNullOrEmpty(((ConversationListObject)statusObject).TypingNotificationText))
            {
                ShowTypingNotification();
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
                isMessageLoaded = true;
                progressBar.Opacity = 0;
                progressBar.IsEnabled = false;
                NetworkManager.turnOffNetworkManager = false;

                if (_isHikeBot && mContactNumber == HikeConstants.FTUE_HIKEBOT_MSISDN)
                {
                    if (ocMessages.Count > 0)
                        llsMessages.ScrollTo(ocMessages[0]);
                }

                UpdateLastSentMessageStatusOnUI();
            });
        }

        private void updateLastMsgColor(string msisdn)
        {
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                App.ViewModel.ConvMap[msisdn].MessageStatus = ConvMessage.State.RECEIVED_READ; // this is to notify ConvList.
                ConversationTableUtils.updateLastMsgStatus(App.ViewModel.ConvMap[mContactNumber].LastMsgId, msisdn, (int)ConvMessage.State.RECEIVED_READ);
            }
        }

        private void initBlockUnblockState()
        {
            if (mUserIsBlocked)
            {
                showOverlay(true);
                appBar.IsMenuEnabled = false;
            }
            else
            {
                showOverlay(false);
            }
        }

        #region APP BAR

        /* Should run on UI thread, based on mUserIsBlocked*/
        private void initAppBar(bool isAddUser)
        {
            appBar = new ApplicationBar() { ForegroundColor = Colors.White, BackgroundColor = Colors.Black };
            appBar.StateChanged += appBar_StateChanged;

            //add icon for send
            sendIconButton = new ApplicationBarIconButton();
            sendIconButton.IconUri = new Uri("/View/images/AppBar/icon_send.png", UriKind.Relative);
            sendIconButton.Text = AppResources.Send_Txt;
            sendIconButton.Click += new EventHandler(sendMsgBtn_Click);
            sendIconButton.IsEnabled = false;
            appBar.Buttons.Add(sendIconButton);

            //add icon for sticker
            stickersIconButton = new ApplicationBarIconButton();
            stickersIconButton.IconUri = new Uri("/View/images/AppBar/icon_sticker.png", UriKind.Relative);
            stickersIconButton.Text = AppResources.Sticker_Txt;
            stickersIconButton.Click += new EventHandler(emoticonButton_Click);
            stickersIconButton.IsEnabled = true;
            appBar.Buttons.Add(stickersIconButton);

            //add icon for smiley
            emoticonsIconButton = new ApplicationBarIconButton();
            emoticonsIconButton.IconUri = new Uri("/View/images/AppBar/icon_emoticon.png", UriKind.Relative);
            emoticonsIconButton.Text = AppResources.Smiley_Txt;
            emoticonsIconButton.Click += new EventHandler(emoticonButton_Click);
            emoticonsIconButton.IsEnabled = true;
            appBar.Buttons.Add(emoticonsIconButton);

            //add file transfer button
            fileTransferIconButton = new ApplicationBarIconButton();
            fileTransferIconButton.IconUri = new Uri("/View/images/AppBar/icon_attachment.png", UriKind.Relative);
            fileTransferIconButton.Text = AppResources.Attach_Txt;
            fileTransferIconButton.Click += new EventHandler(fileTransferButton_Click);
            fileTransferIconButton.IsEnabled = true;
            appBar.Buttons.Add(fileTransferIconButton);

            if (isGroupChat)
            {
                infoMenuItem = new ApplicationBarMenuItem();
                infoMenuItem.Text = AppResources.GroupInfo_Txt;
                infoMenuItem.Click += infoMenuItem_Click;
                infoMenuItem.IsEnabled = !mUserIsBlocked && isGroupAlive;
                appBar.MenuItems.Add(infoMenuItem);

                emailConversationMenuItem = new ApplicationBarMenuItem();
                emailConversationMenuItem.Text = AppResources.EmailChat_Txt;
                emailConversationMenuItem.Click += emailConversationMenuItem_Click;
                appBar.MenuItems.Add(emailConversationMenuItem);

                muteGroupMenuItem = new ApplicationBarMenuItem();
                muteGroupMenuItem.Text = IsMute ? AppResources.SelectUser_UnMuteGrp_Txt : AppResources.SelectUser_MuteGrp_Txt;
                muteGroupMenuItem.Click += new EventHandler(muteUnmuteGroup_Click);
                appBar.MenuItems.Add(muteGroupMenuItem);

                clearChatItem = new ApplicationBarMenuItem();
                clearChatItem.Text = AppResources.Clear_Chat_Txt;
                clearChatItem.Click += clearChatItem_Click;
                appBar.MenuItems.Add(clearChatItem);

                ApplicationBarMenuItem leaveMenuItem = new ApplicationBarMenuItem();
                leaveMenuItem.Text = AppResources.SelectUser_LeaveGrp_Txt;
                leaveMenuItem.Click += new EventHandler(leaveGroup_Click);
                appBar.MenuItems.Add(leaveMenuItem);

                pinHistoryMenuItem = new ApplicationBarMenuItem();
                pinHistoryMenuItem.Text = AppResources.PinHistory_Header_Txt;
                pinHistoryMenuItem.Click += gcPin_PinContentTapped;
                appBar.MenuItems.Add(pinHistoryMenuItem);

                return;
            }

            if (!_isHikeBot)
            {
                if (isAddUser)
                {
                    addUserMenuItem = new ApplicationBarMenuItem();
                    addUserMenuItem.Text = AppResources.Save_Contact_Txt;
                    addUserMenuItem.Click += new EventHandler(addUser_Click);
                    appBar.MenuItems.Add(addUserMenuItem);
                }

                ApplicationBarMenuItem callMenuItem = new ApplicationBarMenuItem();
                callMenuItem.Text = AppResources.Call_Txt;
                callMenuItem.Click += new EventHandler(callUser_Click);
                appBar.MenuItems.Add(callMenuItem);

                infoMenuItem = new ApplicationBarMenuItem();
                infoMenuItem.Text = AppResources.User_Info_Txt;
                infoMenuItem.Click += infoMenuItem_Click;
                appBar.MenuItems.Add(infoMenuItem);
            }

            emailConversationMenuItem = new ApplicationBarMenuItem();
            emailConversationMenuItem.Text = AppResources.EmailChat_Txt;
            emailConversationMenuItem.Click += emailConversationMenuItem_Click;
            appBar.MenuItems.Add(emailConversationMenuItem);

            clearChatItem = new ApplicationBarMenuItem();
            clearChatItem.Text = AppResources.Clear_Chat_Txt;
            clearChatItem.Click += clearChatItem_Click;
            appBar.MenuItems.Add(clearChatItem);

            blockMenuItem = new ApplicationBarMenuItem();
            blockMenuItem.Text = AppResources.Block_Txt;
            blockMenuItem.Click += blockMenuItem_Click;
            appBar.MenuItems.Add(blockMenuItem);
        }


        private void emailConversationMenuItem_Click(object sender, EventArgs e)
        {
            Analytics.SendAnalyticsEvent(HikeConstants.ST_UI_EVENT,HikeConstants.ANALYTICS_EMAIL,HikeConstants.ANALYTICS_EMAIL_MENU,mContactNumber);
            EmailHelper.FetchAndEmail(mContactNumber, mContactName, isGroupChat);
        }

        void appBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (e.IsMenuVisible)
            {
                if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                    CancelBackgroundChange();
            }
        }

        void infoMenuItem_Click(object sender, EventArgs e)
        {
            GoToProfileScreen();
        }

        void blockMenuItem_Click(object sender, EventArgs e)
        {
            ContactInfo cInfo = new ContactInfo(mContactNumber, mContactName, isOnHike);
            App.ViewModel.BlockedHashset.Add(mContactNumber);

            if (App.ViewModel.FavList != null)
            {
                var list = App.ViewModel.FavList.Where(f => f.Msisdn == mContactNumber).ToList();

                if (list.Count > 0)
                {
                    foreach (var co in list)
                        App.ViewModel.FavList.Remove(co);

                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.DeleteFavourite(mContactNumber);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count - list.Count);
                }
            }

            userImage.Source = UI_Utils.Instance.getDefaultAvatar(mContactNumber, false);
            App.ViewModel.DeleteImageForMsisdn(mContactNumber);

            FriendsTableUtils.SetFriendStatus(mContactNumber, FriendsTableUtils.FriendStatusEnum.NOT_SET);
            App.HikePubSubInstance.publish(HikePubSub.BLOCK_USER, cInfo);

            mUserIsBlocked = true;
            initBlockUnblockState();
        }

        private void initInviteMenuItem()
        {
            inviteMenuItem = new ApplicationBarMenuItem();
            inviteMenuItem.Text = AppResources.SelectUser_InviteUsr_Txt;
            inviteMenuItem.Click += new EventHandler(inviteUserBtn_Click);
            if (mUserIsBlocked)
                inviteMenuItem.IsEnabled = false;
        }

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

        void clearChatItem_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(AppResources.clear_Chat_Body, AppResources.Confirmation_HeaderTxt, MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                ocMessages.Clear();

                if (JumpToBottomGrid.Visibility == Visibility.Visible)
                    JumpToBottomGrid.Visibility = Visibility.Collapsed;

                if (!isGroupChat && isNudgeOn)
                    nudgeTut.Visibility = Visibility.Visible;

                if (clearChatItem != null && clearChatItem.IsEnabled)
                    clearChatItem.IsEnabled = false;

                if (isGroupChat && pinHistoryMenuItem != null && pinHistoryMenuItem.IsEnabled)
                    pinHistoryMenuItem.IsEnabled = false;

                if (emailConversationMenuItem != null && emailConversationMenuItem.IsEnabled)
                    emailConversationMenuItem.IsEnabled = false;

                ClearChat();

                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                {
                    ConversationListObject obj = App.ViewModel.ConvMap[mContactNumber];
                    obj.LastMessage = String.Empty;
                    obj.MessageStatus = ConvMessage.State.UNKNOWN;
                }
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

            MessageBoxResult mr = MessageBox.Show(AppResources.Leave_Group_Body, AppResources.Leave_Group_Caption, MessageBoxButton.OKCancel);
            if (mr != MessageBoxResult.OK)
                return;
            /*
            * 1. Delete from DB (pubsub)
            * 2. Remove from ConvList page
            * 3. Remove from stealth list 
            * 4. GoBack
            */
            JObject jObj = new JObject();
            jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
            jObj[HikeConstants.TO] = mContactNumber;
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, jObj);

            ConversationListObject cObj = App.ViewModel.ConvMap[mContactNumber];

            App.ViewModel.MessageListPageCollection.Remove(cObj); // removed from observable collection

            App.ViewModel.ConvMap.Remove(mContactNumber);
            Logging.LogWriter.Instance.WriteToLog(string.Format("CONVERSATION DELETION:user left group,msisdn:{0}, name:{1}", cObj.Msisdn, cObj.ContactName));

            mPubSub.publish(HikePubSub.GROUP_LEFT, mContactNumber);

            if (cObj.IsHidden)
                App.ViewModel.SendRemoveStealthPacket(cObj);

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
            }
            else // GC is unmute , request to mute
            {
                IsMute = true;
                obj[HikeConstants.TYPE] = "mute";
                App.ViewModel.ConvMap[mContactNumber].MuteVal = ocMessages.Count;
                ConversationTableUtils.saveConvObject(App.ViewModel.ConvMap[mContactNumber], mContactNumber.Replace(":", "_"));
                muteGroupMenuItem.Text = AppResources.SelectUser_UnMuteGrp_Txt;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, obj);
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
                    sendIconButton.IsEnabled = sendMsgTxtbox.Text.Length > 0;
                    stickersIconButton.IsEnabled = true;
                    emoticonsIconButton.IsEnabled = true;
                    enableSendMsgButton = true;
                    isTypingNotificationEnabled = true;
                    if (inviteMenuItem != null)
                        inviteMenuItem.IsEnabled = true;
                }

                mUserIsBlocked = false;
                showOverlay(false);
                appBar.IsMenuEnabled = true;

                if (!isGroupChat)
                    lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;

                UpdateChatStatus();
                GetOnHikeStatus();

                // no need to call last seen as friend is removed on blocking
                //GetUserLastSeen();

                this.ApplicationBar = appBar;
            }
        }

        private void inviteUserBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (!isGroupChat && isOnHike)
                    return;
                if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
                {
                    long time = TimeUtils.getCurrentTimeStamp();
                    if (isGroupChat)
                    {
                        foreach (GroupParticipant gp in GroupManager.Instance.GroupParticpantsCache[mContactNumber])
                        {
                            if (!gp.IsOnHike)
                            {
                                ConvMessage convMessage = new ConvMessage(AppResources.sms_invite_message, gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                                convMessage.IsInvite = true;
                                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                            }
                        }

                        MessageBoxResult result = MessageBox.Show(AppResources.GroupInfo_InviteSent_MsgBoxText_Txt, AppResources.GroupInfo_InviteSent_MsgBoxHeader_Txt, MessageBoxButton.OK);
                    }
                    else
                    {
                        //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
                        ConvMessage convMessage = new ConvMessage(AppResources.sms_invite_message, mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                        convMessage.IsSms = true;
                        convMessage.IsInvite = true;
                        sendMsg(convMessage, false);
                    }
                    if (showNoSmsLeftOverlay || isGroupChat)
                        showOverlay(false);
                    if (isGroupChat)
                        App.appSettings.Remove(HikeConstants.SHOW_GROUP_CHAT_OVERLAY);
                }
                else
                {
                    string msisdns = string.Empty;
                    JObject obj = new JObject();
                    JArray numlist = new JArray();
                    JObject data = new JObject();
                    int i;

                    if (isGroupChat)
                    {
                        foreach (GroupParticipant gp in GroupManager.Instance.GroupParticpantsCache[mContactNumber])
                        {
                            if (!gp.IsOnHike)
                            {
                                msisdns += gp.Msisdn + ";";
                                numlist.Add(gp.Msisdn);
                            }
                        }
                    }
                    else
                        msisdns = mContactNumber;

                    var ts = TimeUtils.getCurrentTimeStamp();
                    var smsString = AppResources.sms_invite_message;

                    if (!isGroupChat)
                    {
                        obj[HikeConstants.TO] = msisdns;
                        data[HikeConstants.MESSAGE_ID] = ts.ToString();
                        data[HikeConstants.HIKE_MESSAGE] = smsString;
                        data[HikeConstants.TIMESTAMP] = ts;
                        obj[HikeConstants.DATA] = data;
                        obj[HikeConstants.TYPE] = NetworkManager.INVITE;
                    }
                    else
                    {
                        data[HikeConstants.MESSAGE_ID] = ts.ToString();
                        data[HikeConstants.INVITE_LIST] = numlist;
                        obj[HikeConstants.TIMESTAMP] = ts;
                        obj[HikeConstants.DATA] = data;
                        obj[HikeConstants.TYPE] = NetworkManager.MULTIPLE_INVITE;
                    }

                    obj[HikeConstants.SUB_TYPE] = HikeConstants.NO_SMS;

                    App.MqttManagerInstance.mqttPublishToServer(obj);

                    SmsComposeTask smsComposeTask = new SmsComposeTask();
                    smsComposeTask.To = msisdns;
                    smsComposeTask.Body = smsString;
                    smsComposeTask.Show();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread :: inviteUserBtn_Click : Exception Occored:{0}", ex.StackTrace);
            }
        }

        public void AddNewMessageToUI(ConvMessage convMessage, bool insertAtTop, bool isReceived = false)
        {
            if (isTypingNotificationActive)
            {
                HideTypingNotification();
                isReshowTypingNotification = true;
            }

            AddMessageToOcMessages(convMessage, insertAtTop, isReceived);

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

        List<ConvMessage> listDownload = new List<ConvMessage>();

        private void AddMessageToOcMessages(ConvMessage convMessage, bool insertAtTop, bool isReceived)
        {
            if (ocMessages == null)
                return;

            if (clearChatItem != null && !clearChatItem.IsEnabled)
                clearChatItem.IsEnabled = true;

            if (isGroupChat && pinHistoryMenuItem != null && !pinHistoryMenuItem.IsEnabled)
                pinHistoryMenuItem.IsEnabled = true;

            if (nudgeTut.Visibility == Visibility.Visible)
                nudgeTut.Visibility = Visibility.Collapsed;

            if (emailConversationMenuItem != null && !emailConversationMenuItem.IsEnabled)
                emailConversationMenuItem.IsEnabled = true;

            if (_isSendAllAsSMSVisible && ocMessages != null && convMessage.IsSent)
            {
                ocMessages.Remove(_tap2SendAsSMSMessage);
                _isSendAllAsSMSVisible = false;
            }

            int insertPosition = 0;
            if (!insertAtTop)
                insertPosition = ocMessages.Count;
            try
            {
                #region NO_INFO
                //TODO : Create attachment object if it requires one
                if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    if (convMessage.HasAttachment)
                    {
                        if (convMessage.FileAttachment == null && attachments.ContainsKey(convMessage.MessageId))
                        {
                            convMessage.FileAttachment = attachments[convMessage.MessageId];
                            attachments.Remove(convMessage.MessageId);

                            // due to perception fix message status would have been changed to read. Need to change it back to unconfirmed.
                            if (convMessage.IsSent)
                            {
                                if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.CANCELED || convMessage.FileAttachment.FileState == Attachment.AttachmentState.FAILED)
                                    convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                                else if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                                    convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                            }
                        }

                        if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.CANCELED && convMessage.FileAttachment.FileState != Attachment.AttachmentState.FAILED)
                        {
                            if ((!convMessage.IsSent && convMessage.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                                || (convMessage.MessageId > 0
                                    && ((!convMessage.IsSms && (convMessage.MessageStatus < ConvMessage.State.SENT_DELIVERED_READ || convMessage.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE))
                                        || (convMessage.IsSms && (convMessage.MessageStatus < ConvMessage.State.SENT_CONFIRMED || convMessage.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE)))))
                            {
                                msgMap.Add(convMessage.MessageId, convMessage);

                                FileInfoBase fInfo;
                                if (FileTransferManager.Instance.GetAttachmentStatus(convMessage.MessageId.ToString(), convMessage.IsSent, out fInfo))
                                    UpdateFileTransferProgresInConvMessage(fInfo, convMessage, true);
                            }
                        }

                        if (convMessage.FileAttachment == null)
                        {
                            //Done to avoid crash. Code should never reach here
                            Debug.WriteLine("Fileattachment object is null for convmessage with attachment");
                            return;
                        }
                    }

                    if (!string.IsNullOrEmpty(convMessage.MetaDataString) && convMessage.MetaDataString.Contains(HikeConstants.STICKER_ID))
                    {
                        JObject meataDataJson = JObject.Parse(convMessage.MetaDataString);
                        convMessage.StickerObj = new StickerObj((string)meataDataJson[HikeConstants.CATEGORY_ID], (string)meataDataJson[HikeConstants.STICKER_ID], null, true);
                        GetHighResStickerForUi(convMessage);
                    }
                    else
                    {
                        if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.LONG_MESSAGE))
                        {
                            string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);
                            if (message.Length > 0)
                                convMessage.Message = message;
                        }
                    }

                    if (convMessage.IsSent)
                    {
                        if (convMessage.MessageId > 0 && ((!convMessage.IsSms && (convMessage.MessageStatus < ConvMessage.State.SENT_DELIVERED_READ || convMessage.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE))
                            || (convMessage.IsSms && (convMessage.MessageStatus < ConvMessage.State.SENT_CONFIRMED || convMessage.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || convMessage.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED || convMessage.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE)))
                            && !msgMap.ContainsKey(convMessage.MessageId))
                            msgMap.Add(convMessage.MessageId, convMessage);
                    }
                    else
                    {
                        if (isGroupChat)
                        {
                            var gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, mContactNumber);
                            convMessage.GroupMemberName = gp.Name;
                            convMessage.IsInAddressBook = gp.IsInAddressBook;
                        }
                    }

                    convMessage.IsSms = !isOnHike;
                    convMessage.CurrentOrientation = this.Orientation;
                    ocMessages.Insert(insertPosition, convMessage);
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
                    ocMessages.Insert(insertPosition, convMessageNew);
                    insertPosition++;
                    if (vals.Length == 2)
                    {
                        ConvMessage dndChatBubble = new ConvMessage(vals[1], this.Orientation, convMessage);
                        dndChatBubble.NotificationType = ConvMessage.MessageType.WAITING;
                        ocMessages.Insert(insertPosition, dndChatBubble);
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

                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, vars[0], convMessage.Msisdn);
                        string text = AppResources.USER_JOINED_GROUP_CHAT;
                        ConvMessage.MessageType type = ConvMessage.MessageType.HIKE_PARTICIPANT_JOINED;
                        if (vars[1] == "0" && !gp.IsOnHike)
                        {
                            text = AppResources.USER_INVITED;
                            type = ConvMessage.MessageType.SMS_PARTICIPANT_INVITED;
                        }
                        ConvMessage chatBubble = new ConvMessage(String.Format(text, gp.FirstName), this.Orientation, convMessage);
                        chatBubble.NotificationType = type;
                        ocMessages.Insert(insertPosition, chatBubble);
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
                        GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, msisdn, convMessage.Msisdn);

                        string text = String.Format(AppResources.USER_JOINED_GROUP_CHAT, gp.FirstName);
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
                            ocMessages.Insert(insertPosition, chatBubble);
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
                    ocMessages.Insert(insertPosition, wchatBubble);
                }
                #endregion
                #region USER_JOINED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED || convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_REJOINED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.USER_JOINED_HIKE;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region HIKE_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.HIKE_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.USER_JOINED_HIKE;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region SMS_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.SMS_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.SMS_PARTICIPANT_INVITED;
                    ocMessages.Insert(insertPosition, chatBubble);
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
                    ocMessages.Insert(insertPosition, chatBubble);
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
                        ocMessages.Insert(insertPosition, chatBubble);
                        insertPosition++;
                    }
                }
                #endregion
                #region PARTICIPANT_LEFT
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PARTICIPANT_LEFT)
                {
                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    ConvMessage chatBubble = new ConvMessage(String.Format(AppResources.USER_LEFT, name), this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.PARTICIPANT_LEFT;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region GROUP END
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_END)
                {
                    ConvMessage chatBubble = new ConvMessage(AppResources.GROUP_CHAT_END, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.GROUP_END;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region CREDITS REWARDS
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CREDITS_GAINED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.REWARD;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region INTERNATIONAL_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.INTERNATIONAL_USER_BLOCKED;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region INTERNATIONAL_GROUPCHAT_USER
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.INTERNATIONAL_GROUP_USER)
                {
                    ConvMessage chatBubble = new ConvMessage(AppResources.SMS_INDIA, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.INTERNATIONAL_USER_BLOCKED;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                    string name = convMessage.Message.Substring(0, convMessage.Message.IndexOf(' '));
                    ConvMessage chatBubbleLeft = new ConvMessage(String.Format(AppResources.USER_LEFT, name), this.Orientation, convMessage);
                    chatBubbleLeft.NotificationType = ConvMessage.MessageType.PARTICIPANT_LEFT;
                    ocMessages.Insert(insertPosition, chatBubbleLeft);
                    insertPosition++;
                }
                #endregion
                #region GROUP NAME CHANGED
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.GROUP_NAME_CHANGED;
                    ocMessages.Insert(insertPosition, chatBubble);
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
                            ocMessages.Insert(insertPosition, convMessage);
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
                            ocMessages.Insert(insertPosition, convMessage);
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
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;
                }
                #endregion
                #region CHAT_BACKGROUND
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED)
                {
                    ConvMessage chatBubble = new ConvMessage(convMessage.Message, this.Orientation, convMessage);
                    chatBubble.NotificationType = ConvMessage.MessageType.CHAT_BACKGROUND;
                    ocMessages.Insert(insertPosition, chatBubble);
                    insertPosition++;

                    if (!insertAtTop)
                        ScrollToBottom();
                }
                #endregion
                #region GCPIN_MESSAGE
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                {
                    if (convMessage.IsSent)
                        convMessage.StatusUpdateImage = UI_Utils.Instance.GetBitmapImage(App.MSISDN);
                    else if (isGroupChat)
                    {
                        var gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, mContactNumber);
                        convMessage.GroupMemberName = gp.FirstName;
                        convMessage.IsInAddressBook = gp.IsInAddressBook;
                        convMessage.StatusUpdateImage = UI_Utils.Instance.GetBitmapImage(gp.Msisdn);
                    }

                    if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.LONG_MESSAGE))
                    {
                        string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);
                        if (message.Length > 0)
                            convMessage.Message = message;
                    }

                    ocMessages.Insert(insertPosition, convMessage);
                    insertPosition++;

                    if (!insertAtTop)
                        ScrollToBottom();
                }
                #endregion
                #region Unread Messages
                else if (convMessage == _unreadMsg)
                {
                    ocMessages.Insert(insertPosition, _unreadMsg);
                    insertPosition++;
                }
                #endregion

                if (!insertAtTop && !isReceived)
                    ScrollToBottom(true);

            }
            catch (Exception e)
            {
                Debug.WriteLine("NEW CHAT THREAD :: " + e.StackTrace);
            }
        }

        #endregion

        #endregion

        #endregion

        #region REGISTER/DEREGISTER LISTENERS

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MSG_WRITTEN_SOCKET, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.UPDATE_PROFILE_ICON, this);
            mPubSub.addListener(HikePubSub.GROUP_END, this);
            mPubSub.addListener(HikePubSub.GROUP_ALIVE, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
            mPubSub.addListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
            mPubSub.addListener(HikePubSub.LAST_SEEN, this);
            mPubSub.addListener(HikePubSub.CHAT_BACKGROUND_REC, this);
            mPubSub.addListener(HikePubSub.DELETE_FROM_NEWCHATTHREAD_OC, this);
        }

        private void removeListeners()
        {
            try
            {
                mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
                mPubSub.removeListener(HikePubSub.SERVER_RECEIVED_MSG, this);
                mPubSub.removeListener(HikePubSub.MSG_WRITTEN_SOCKET, this);
                mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED, this);
                mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
                mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
                mPubSub.removeListener(HikePubSub.USER_JOINED, this);
                mPubSub.removeListener(HikePubSub.USER_LEFT, this);
                mPubSub.removeListener(HikePubSub.UPDATE_PROFILE_ICON, this);
                mPubSub.removeListener(HikePubSub.GROUP_END, this);
                mPubSub.removeListener(HikePubSub.GROUP_ALIVE, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_LEFT_GROUP, this);
                mPubSub.removeListener(HikePubSub.PARTICIPANT_JOINED_GROUP, this);
                mPubSub.removeListener(HikePubSub.LAST_SEEN, this);
                mPubSub.removeListener(HikePubSub.CHAT_BACKGROUND_REC, this);
                mPubSub.removeListener(HikePubSub.DELETE_FROM_NEWCHATTHREAD_OC, this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml ::  removeListeners , Exception : " + ex.StackTrace);
            }
        }

        #endregion

        #region PAGE EVENTS

        private int lastMessageCount = 0;

        private void userHeader_Tap(object sender, EventArgs e)
        {
            if (openChatBackgroundButton.Opacity == 0)
                return;

            if (_isHikeBot)
                return;

            GoToProfileScreen();
        }

        private void GoToProfileScreen()
        {
            if (isGroupChat)
            {
                if (mUserIsBlocked || !isGroupAlive)
                    return;

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
            if (openChatBackgroundButton.Opacity == 0)
                return;

            // Don't open image for hike bot.
            if (_isHikeBot)
                return;

            object[] fileTapped = new object[1];
            fileTapped[0] = mContactNumber;
            PhoneApplicationService.Current.State["displayProfilePic"] = fileTapped;
            NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
        }

        bool _isSendActivated = false;
        private void sendMsgTxtbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(sendMsgTxtbox.Text) && _isSendActivated)
            {
                _isSendActivated = false;
                sendIconButton.IsEnabled = false;
                actionIcon.Source = UI_Utils.Instance.WalkieTalkieImage;
            }

            if (lastText.Equals(sendMsgTxtbox.Text))
                return;

            lastText = sendMsgTxtbox.Text.Trim();
            if (String.IsNullOrEmpty(lastText))
                return;

            if (!_isSendActivated)
            {
                _isSendActivated = true;
                actionIcon.Source = UI_Utils.Instance.SendMessageImage;
            }

            if (_isDraftMessage)
                _isDraftMessage = false;
            else
            {
                lastTextChangedTime = TimeUtils.getCurrentTimeStamp();
                sendStartTypingNotification();
            }

            if (!isOnHike && lastText.Length > 130)
            {
                spSmsCharCounter.Visibility = Visibility.Visible;
                int numberOfMessages = (lastText.Length / maxSmsCharLength) + 1;

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

                txtMsgCharCount.Text = string.Format(AppResources.CT_CharCount_Sms_User, lastText.Length, numberOfMessages * maxSmsCharLength);
            }
            else
            {
                spSmsCharCounter.Visibility = Visibility.Collapsed;
            }

            sendIconButton.IsEnabled = enableSendMsgButton;
        }

        private void sendMsgBtn_Click(object sender, EventArgs e)
        {
            SendMsg();
        }

        private void SendMsg()
        {
            if (mUserIsBlocked)
                return;

            this.Focus();
            string message = sendMsgTxtbox.Text.Trim();
            sendMsgTxtbox.Text = string.Empty;
            lastText = string.Empty;
            _isSendActivated = false;
            sendIconButton.IsEnabled = false;
            actionIcon.Source = UI_Utils.Instance.WalkieTalkieImage;

            if (emoticonPanel.Visibility == Visibility.Collapsed)
                sendMsgTxtbox.Focus();

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            if (String.IsNullOrEmpty(message))
                return;

            attachmentMenu.Visibility = Visibility.Collapsed;

            if (message == String.Empty || (!isOnHike && mCredits <= 0))
                return;

            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            convMessage.IsSms = !isOnHike;
            sendMsg(convMessage, false);

            spSmsCharCounter.Visibility = Visibility.Collapsed;
        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;

            if ((!isOnHike && mCredits <= 0))
                return;

            if (e.TaskResult == TaskResult.OK)
            {
                Uri uri = new Uri(e.OriginalFileName);
                BitmapImage image = new BitmapImage();
                image.SetSource(e.ChosenPhoto);
                try
                {
                    SendImage(image, "image_" + TimeUtils.getCurrentTimeStamp().ToString(), Attachment.AttachemntSource.CAMERA);
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

        private bool SendImage(BitmapImage image, string fileName, Attachment.AttachemntSource source)
        {
            if (!isGroupChat || isGroupAlive)
            {
                byte[] thumbnailBytes;
                byte[] fileBytes;

                WriteableBitmap writeableBitmap = new WriteableBitmap(image);
                int thumbnailWidth, thumbnailHeight, imageWidth, imageHeight;
                Utils.AdjustAspectRatio(image.PixelWidth, image.PixelHeight, true, out thumbnailWidth, out thumbnailHeight);
                Utils.AdjustAspectRatio(image.PixelWidth, image.PixelHeight, false, out imageWidth, out imageHeight);

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

                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, imageWidth, imageHeight, 0, 65);
                    fileBytes = msLargeImage.ToArray();
                }

                if (!StorageManager.StorageManager.Instance.IsDeviceMemorySufficient(fileBytes.Length))
                {
                    MessageBox.Show(AppResources.Memory_Limit_Reached_Body, AppResources.Memory_Limit_Reached_Header, MessageBoxButton.OK);
                    return false;
                }

                ConvMessage convMessage = new ConvMessage(String.Empty, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                convMessage.FileAttachment = new Attachment(fileName, thumbnailBytes, Attachment.AttachmentState.NOT_STARTED, source, fileBytes.Length);
                convMessage.FileAttachment.ContentType = HikeConstants.IMAGE;
                convMessage.Message = AppResources.Image_Txt;

                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = fileBytes;
                mPubSub.publish(HikePubSub.ATTACHMENT_SENT, vals);

                return true;
            }
            else
                return false;
        }

        private void SendPinMsg(ConvMessage convMessage)
        {
            JObject metaData = new JObject();
            metaData[HikeConstants.GC_PIN] = 1;
            convMessage.MetaDataString = metaData.ToString(Newtonsoft.Json.Formatting.None);
            convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.PIN_MESSAGE;

            gcPin.UpdateContent(convMessage.GCPinMessageSenderName, convMessage.DispMessage);

            AddNewMessageToUI(convMessage, false);
            object[] vals = new object[3];
            vals[0] = convMessage;
            vals[1] = false;
            mPubSub.publish(HikePubSub.MESSAGE_SENT, vals);
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
            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            sendMsgTxtbox.Background = UI_Utils.Instance.TextBoxBackground;
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

        private void OnEmoticonBackKeyPress(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (sendMsgTxtbox.Text.Length > 0)
            {
                MatchCollection matchCollection = SmileyParser.Instance.EmoticonRegex.Matches(sendMsgTxtbox.Text);
                if (matchCollection.Count > 0)
                {
                    string lastEmoticon = matchCollection[matchCollection.Count - 1].ToString();

                    if (sendMsgTxtbox.Text.Substring(sendMsgTxtbox.Text.Length - lastEmoticon.Length).Equals(lastEmoticon))
                        sendMsgTxtbox.Text = sendMsgTxtbox.Text.Substring(0, sendMsgTxtbox.Text.Length - lastEmoticon.Length);
                    else
                        sendMsgTxtbox.Text = sendMsgTxtbox.Text.Substring(0, sendMsgTxtbox.Text.Length - 1);
                }
                else
                    sendMsgTxtbox.Text = sendMsgTxtbox.Text.Substring(0, sendMsgTxtbox.Text.Length - 1);
            }
        }

        private void llsMessages_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (isMessageLoaded && llsMessages.ItemsSource != null && llsMessages.ItemsSource.Count > 0 && hasMoreMessages)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    if ((e.Container.Content as ConvMessage).Equals(llsMessages.ItemsSource[0]))
                    {
                        shellProgress.Visibility = Visibility.Visible;

                        BackgroundWorker bw = new BackgroundWorker();
                        bw.DoWork += (s1, ev1) =>
                        {
                            loadMessages(SUBSEQUENT_FETCH_COUNT, false);
                        };
                        bw.RunWorkerAsync();
                        bw.RunWorkerCompleted += (s1, ev1) =>
                        {
                            shellProgress.Visibility = Visibility.Collapsed;
                        };
                    }
                }
            }
        }


        private void ChatMessage_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_hyperlinkedClicked)
            {
                _hyperlinkedClicked = false;
                return;
            }

            ConvMessage msg = (sender as Grid).DataContext as ConvMessage;
            ChatMessageSelected(msg);
        }

        private void ChatMessageSelected(ConvMessage msg)
        {
            if (msg != null)
            {
                if ((msg.IsSent && msg.MessageStatus == ConvMessage.State.SENT_CONFIRMED) || msg.GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION)
                {
                    if (_isSendAllAsSMSVisible)
                    {
                        if (mCredits > 0)
                        {
                            var result = MessageBox.Show(AppResources.H2HOfline_Confirmation_Message, AppResources.H2HOfline_Confirmation_Message_Heading, MessageBoxButton.OKCancel);

                            if (result == MessageBoxResult.OK)
                            {
                                SendForceSMS();
                                UpdateLastSentMessageStatusOnUI();

                                if (_lastUnDeliveredMessage != null)
                                    ocMessages.Remove(_tap2SendAsSMSMessage);
                            }
                            //    else
                            //        FileAttachmentMessage_Tap(sender, e);
                            //}
                            //
                        }
                        else
                            MessageBox.Show(AppResources.H2HOfline_0SMS_Message, AppResources.H2HOfline_Confirmation_Message_Heading, MessageBoxButton.OK);

                        llsMessages.SelectedItem = null;
                    }
                    else
                        FileAttachmentMessage_Tap(msg); // normal flow if all are sent files and send all sms option is not visible
                }
                else
                    FileAttachmentMessage_Tap(msg); // normal flow for recieved files
            }
        }

        ViewportControl llsViewPort;
        private void ViewPortLoaded(object sender, RoutedEventArgs e)
        {
            llsViewPort = sender as ViewportControl;
        }

        #endregion

        #region CONTEXT MENU

        bool _isContextMenuOpen = false;

        private void ContextMenu_Unloaded(object sender, RoutedEventArgs e)
        {
            ContextMenu contextMenu = sender as ContextMenu;
            contextMenu.ClearValue(FrameworkElement.DataContextProperty);
            _isContextMenuOpen = false;
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            _isContextMenuOpen = true;
        }

        private void MenuItem_Click_DirectMessage(object sender, RoutedEventArgs e)
        {
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            var msisdn = convMessage.GroupParticipant;

            if (!App.ViewModel.IsHiddenModeActive && App.ViewModel.ConvMap.ContainsKey(msisdn) && App.ViewModel.ConvMap[msisdn].IsHidden)
                return;

            ConversationListObject co = Utils.GetConvlistObj(msisdn);

            if (co != null)
            {
                if (co.Avatar == null)
                    co.Avatar = MiscDBUtil.getThumbNailForMsisdn(msisdn);

                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_CONVERSATIONS_PAGE] = co;
            }
            else
            {
                ContactInfo cn = ContactUtils.GetContactInfo(msisdn);

                if (cn == null)
                {
                    cn = new ContactInfo(msisdn, convMessage.GroupMemberName, true);
                    App.ViewModel.ContactsCache[msisdn] = cn;
                }

                PhoneApplicationService.Current.State[HikeConstants.OBJ_FROM_SELECTUSER_PAGE] = cn;
            }
            PhoneApplicationService.Current.State[HikeConstants.IS_CHAT_RELAUNCH] = true;
            string uri = "/View/NewChatThread.xaml?" + msisdn;
            NavigationService.Navigate(new Uri(uri, UriKind.Relative));
        }

        private void MenuItem_Click_Forward(object sender, RoutedEventArgs e)
        {
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);

            if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.STICKER_ID))
            {
                Object[] obj = new Object[1];
                obj[0] = convMessage.MetaDataString;
                StickerObj sticker = new StickerObj(convMessage.StickerObj.Category, convMessage.StickerObj.Id, null, false);

                if (HikeViewModel.StickerHelper.CheckLowResStickerExists(convMessage.StickerObj.Category, convMessage.StickerObj.Id))
                    HikeViewModel.StickerHelper.RecentStickerHelper.AddSticker(sticker);

                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = obj;//done this way to distinguish it from message
            }
            else if (convMessage.FileAttachment == null)
            {
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = convMessage.Message;
            }
            else
            {
                if (!StorageManager.StorageManager.Instance.IsDeviceMemorySufficient(convMessage.FileAttachment.FileSize))
                {
                    MessageBox.Show(AppResources.Memory_Limit_Reached_Body, AppResources.Memory_Limit_Reached_Header, MessageBoxButton.OK);
                    return;
                }

                //done this way as on locking it is unable to serialize convmessage or attachment object
                object[] attachmentForwardMessage = new object[7];
                attachmentForwardMessage[0] = convMessage.FileAttachment.ContentType;
                attachmentForwardMessage[1] = mContactNumber;
                attachmentForwardMessage[2] = convMessage.MessageId;
                attachmentForwardMessage[3] = convMessage.MetaDataString;
                attachmentForwardMessage[4] = convMessage.FileAttachment.FileKey;
                attachmentForwardMessage[5] = convMessage.FileAttachment.Thumbnail;
                attachmentForwardMessage[6] = convMessage.FileAttachment.FileName;

                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = attachmentForwardMessage;
            }

            NavigationService.Navigate(new Uri("/View/ForwardTo.xaml", UriKind.Relative));
        }

        private void MenuItem_Click_Copy(object sender, RoutedEventArgs e)
        {
            ConvMessage chatBubble = ((sender as MenuItem).DataContext as ConvMessage);
            if (chatBubble.FileAttachment == null)
                Clipboard.SetText(chatBubble.Message);
            else if (!String.IsNullOrEmpty(chatBubble.FileAttachment.FileKey))
                Clipboard.SetText(HikeConstants.FILE_TRANSFER_COPY_BASE_URL + "/" + chatBubble.FileAttachment.FileKey);
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            ConvMessage msg = ((sender as MenuItem).DataContext as ConvMessage);

            if (msg == null)
                return;

            if (currentAudioMessage != null && msg == currentAudioMessage && msg.IsPlaying)
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                currentAudioMessage = null;
                mediaElement.Stop();
                App.ViewModel.ResumeBackgroundAudio();
            }

            if (msg.FileAttachment != null && msg.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                msg.SetAttachmentState(Attachment.AttachmentState.CANCELED);

            if (_unreadMsg != null && ocMessages.IndexOf(_unreadMsg) == ocMessages.IndexOf(msg) - 1)
                ocMessages.Remove(_unreadMsg);

            bool delConv = false;
            ocMessages.Remove(msg);

            if (ocMessages.Count == 0)
            {
                if (clearChatItem != null && clearChatItem.IsEnabled)
                    clearChatItem.IsEnabled = false;

                if (emailConversationMenuItem != null && emailConversationMenuItem.IsEnabled)
                    emailConversationMenuItem.IsEnabled = false;

                if (isGroupChat && pinHistoryMenuItem != null && pinHistoryMenuItem.IsEnabled)
                    pinHistoryMenuItem.IsEnabled = false;
            }

            if (!isGroupChat && ocMessages.Count == 0 && isNudgeOn)
                nudgeTut.Visibility = Visibility.Visible;

            if (_isSendAllAsSMSVisible && _lastUnDeliveredMessage == msg)
            {
                ocMessages.Remove(_tap2SendAsSMSMessage);
                _isSendAllAsSMSVisible = false;
                ShowForceSMSOnUI();
            }
            else if (msg == _lastReceivedSentMessage)
            {
                if (_readByMessage != null)
                    ocMessages.Remove(_readByMessage);

                UpdateLastSentMessageStatusOnUI();
            }

            ConversationListObject obj = App.ViewModel.ConvMap[mContactNumber];

            ConvMessage lastMessageBubble = null;
            if (isTypingNotificationActive && ocMessages.Count > 1)
            {
                lastMessageBubble = ocMessages[ocMessages.Count - 2];
            }
            else if (!isTypingNotificationActive && ocMessages.Count > 0)
            {
                lastMessageBubble = ocMessages[ocMessages.Count - 1];
            }

            if (lastMessageBubble != null)
            {
                //This updates the Conversation list.
                if (lastMessageBubble.FileAttachment != null)
                {
                    if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                        obj.LastMessage = AppResources.Image_Txt;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                        obj.LastMessage = AppResources.Audio_Txt;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                        obj.LastMessage = AppResources.Video_Txt;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        obj.LastMessage = AppResources.ContactTransfer_Text;
                    else if (lastMessageBubble.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                        obj.LastMessage = AppResources.Location_Txt;
                    else
                        obj.LastMessage = AppResources.UnknownFile_txt;

                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                }
                else if (lastMessageBubble.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO 
                    || lastMessageBubble.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                {
                    obj.LastMessage = lastMessageBubble.Message;
                    obj.MessageStatus = lastMessageBubble.MessageStatus;
                    obj.TimeStamp = lastMessageBubble.Timestamp;
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
            else if (isGroupChat)
            {
                obj.LastMessage = String.Empty;
                obj.MessageStatus = ConvMessage.State.UNKNOWN;
            }
            else
            {
                Logging.LogWriter.Instance.WriteToLog(string.Format("CONVERSATION DELETION:No message left,msisdn:{0}, name:{1}", obj.Msisdn, obj.ContactName));

                // no message is left, simply remove the object from Conversation list 
                App.ViewModel.MessageListPageCollection.Remove(obj); // removed from observable collection
                App.ViewModel.ConvMap.Remove(mContactNumber);
                // delete from db will be handled by dbconversation listener
                mPubSub.publish(HikePubSub.DELETE_STATUS_AND_CONV, obj);//to update ui of conversation list page
                delConv = true;
            }

            object[] o = new object[3];
            o[0] = msg.MessageId;
            o[1] = obj;
            o[2] = delConv;
            mPubSub.publish(HikePubSub.MESSAGE_DELETED, o);
        }

        private void MenuItem_Click_Cancel(object sender, RoutedEventArgs e)
        {
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            convMessage.ChangingState = FileTransfers.FileTransferManager.Instance.CancelTask(convMessage.MessageId.ToString());
        }

        private void PauseTransfer(ConvMessage convMessage)
        {
            if (convMessage.ChangingState)
                return;

            convMessage.ChangingState = FileTransfers.FileTransferManager.Instance.PauseTask(convMessage.MessageId.ToString());
        }

        private bool ResumeTransfer(ConvMessage convMessage)
        {
            if (convMessage.ChangingState)
                return false;

            convMessage.ChangingState = true;

            if (FileTransferManager.Instance.IsTransferPossible() && FileTransfers.FileTransferManager.Instance.ResumeTask(convMessage.MessageId.ToString(), convMessage.IsSent))
                return true;

            convMessage.ChangingState = false;
            return false;
        }

        private void MenuItem_Click_SendAsSMS(object sender, RoutedEventArgs e)
        {
            if (mCredits > 0)
            {
                ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
                convMessage.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_CONFIRMED;
                string msisdn = MessagesTableUtils.updateMsgStatus(mContactNumber, convMessage.MessageId, (int)ConvMessage.State.FORCE_SMS_SENT_CONFIRMED);
                ConversationTableUtils.updateLastMsgStatus(convMessage.MessageId, msisdn, (int)ConvMessage.State.FORCE_SMS_SENT_CONFIRMED);

                SendForceSMS(convMessage);
                UpdateLastSentMessageStatusOnUI();

                if (_isSendAllAsSMSVisible && _lastUnDeliveredMessage == convMessage)
                {
                    ocMessages.Remove(_tap2SendAsSMSMessage);
                    _isSendAllAsSMSVisible = false;
                    ShowForceSMSOnUI();
                }
            }
            else
                MessageBox.Show(AppResources.H2HOfline_0SMS_Message, AppResources.H2HOfline_Confirmation_Message_Heading, MessageBoxButton.OK);
        }

        private void MenuItem_Click_View(object sender, RoutedEventArgs e)
        {
            ConvMessage msg = (sender as MenuItem).DataContext as ConvMessage;

            if (msg.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
            {
                App.ViewModel.PauseBackgroundAudio();
                string contactNumberOrGroupId = mContactNumber.Replace(":", "_");
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId + "/" + Convert.ToString(msg.MessageId);
                Utils.PlayFileInMediaPlayer(fileLocation);
            }
            else
                displayAttachment(msg);
        }
        #endregion

        #region EMOTICONS RELATED STUFF

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            object s = e.OriginalSource;
        }

        bool isEmoticonLoaded = false;
        private void emoticonButton_Click(object sender, EventArgs e)
        {
            if (_tipMode == ToolTipMode.STICKERS)
                HideServerTips();

            var appButton = sender as ApplicationBarIconButton;

            if (JumpToBottomGrid.Visibility == Visibility.Collapsed)
                ScrollToBottom(); // So that most recent chat is shown when pressing stickers or emojis

            if (appButton != null)
            {
                if (emoticonPanel.Visibility == Visibility.Collapsed)
                {
                    emoticonPanel.Visibility = Visibility.Visible;

                    if (appButton.Text == AppResources.Sticker_Txt)
                    {
                        ShowStickerPallet();
                    }
                    else
                    {
                        ShowEmoticonPalette();
                    }
                }
                else
                {
                    if (appButton.Text == AppResources.Sticker_Txt)
                    {
                        if (gridStickers.Visibility == Visibility.Collapsed)
                        {
                            ShowStickerPallet();
                        }
                        else
                        {
                            emoticonPanel.Visibility = Visibility.Collapsed;
                        }
                    }
                    else
                    {
                        if (gridEmoticons.Visibility == Visibility.Collapsed)
                        {
                            ShowEmoticonPalette();
                        }
                        else
                        {
                            emoticonPanel.Visibility = Visibility.Collapsed;
                        }
                    }
                }
            }

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            if (recordGrid.Visibility == Visibility.Visible)
            {
                recordGrid.Visibility = Visibility.Collapsed;
                sendMsgTxtbox.Visibility = Visibility.Visible;
            }

            attachmentMenu.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        private void ShowEmoticonPalette()
        {
            gridEmoticons.Visibility = Visibility.Visible;
            gridStickers.Visibility = Visibility.Collapsed;
            if (!isEmoticonLoaded)
            {
                int index = 0;

                if (App.appSettings.TryGetValue(HikeConstants.AppSettings.LAST_SELECTED_EMOTICON_CATEGORY, out index))
                    emoticonPivot.SelectedIndex = index == 0 && imagePathsForListRecent.Count == 0 ? 1 : index;
                else
                    emoticonPivot.SelectedIndex = imagePathsForListRecent.Count > 0 ? 0 : 1;

                isEmoticonLoaded = true;
            }
        }

        private void ShowStickerPallet()
        {
            gridEmoticons.Visibility = Visibility.Collapsed;
            gridStickers.Visibility = Visibility.Visible;

            if (!isStickersLoaded)
            {
                String category;
                if (App.appSettings.TryGetValue(HikeConstants.AppSettings.LAST_SELECTED_STICKER_CATEGORY, out category))
                {
                    if (category == StickerHelper.CATEGORY_RECENT)
                    {
                        if (HikeViewModel.StickerHelper.RecentStickerHelper.RecentStickers.Count > 0)
                            StickerCategoryTapped(StickerHelper.CATEGORY_RECENT);
                        else
                            StickerCategoryTapped(StickerHelper.CATEGORY_HUMANOID);
                    }
                    else
                    {
                        StickerCategoryTapped(category);
                    }
                }
                else
                {
                    if (HikeViewModel.StickerHelper.RecentStickerHelper.RecentStickers.Count > 0)
                        StickerCategoryTapped(StickerHelper.CATEGORY_RECENT);
                    else
                        StickerCategoryTapped(StickerHelper.CATEGORY_HUMANOID);
                }
                isStickersLoaded = true;
            }
            else
            {
                var stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(_selectedCategory);
                if (stickerCategory != null && stickerCategory.ShowDownloadMessage)
                    ShowDownloadOverlay(true);
            }
        }

        private void fileTransferButton_Click(object sender, EventArgs e)
        {
            if (_tipMode == ToolTipMode.ATTACHMENTS)
                HideServerTips();

            if (recordGrid.Visibility == Visibility.Visible)
            {
                recordGrid.Visibility = Visibility.Collapsed;
                sendMsgTxtbox.Visibility = Visibility.Visible;
            }

            if (attachmentMenu.Visibility == Visibility.Collapsed)
                attachmentMenu.Visibility = Visibility.Visible;
            else
                attachmentMenu.Visibility = Visibility.Collapsed;

            if (emoticonPanel.Visibility == Visibility.Visible)
                emoticonPanel.Visibility = Visibility.Collapsed;

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            this.Focus();
        }

        private void sendImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            try
            {
                NavigationService.Navigate(new Uri("/View/ViewPhotoAlbums.xaml", UriKind.RelativeOrAbsolute));
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
            if (_microphone == null)
            {
                MessageBox.Show(AppResources.MicrophoneNotFound_ErrorTxt, AppResources.MicrophoneNotFound_HeaderTxt, MessageBoxButton.OK);
                return;
            }
            attachmentMenu.Visibility = Visibility.Collapsed;
            ShowWalkieTalkie();
        }

        private void sendContact_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (!spContactTransfer.IsHitTestVisible)
                return;

            PhoneApplicationService.Current.State[HikeConstants.SHARE_CONTACT] = true;

            NavigationService.Navigate(new Uri("/View/SelectUser.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendCamcorder_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/RecordVideo.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void sendVideo_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/ViewVideos.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void shareLocation_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            NavigationService.Navigate(new Uri("/View/ShareLocation.xaml", UriKind.Relative));
            attachmentMenu.Visibility = Visibility.Collapsed;
        }

        private void emotListRecent_Tap(object sender, SelectionChangedEventArgs e)
        {
            ListBox llsStickerCategory = (sender as ListBox);
            SmileyParser.Emoticon emoticon = llsStickerCategory.SelectedItem as SmileyParser.Emoticon;
            llsStickerCategory.SelectedItem = null;
            if (emoticon == null)
                return;
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[emoticon.Index];
            sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
            SmileyParser.Instance.AddEmoticons(emoticon);
        }

        private void emotList0_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (emotList0.SelectedIndex < 0)
                return;
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList0.SelectedIndex;
            if (index >= SmileyParser.Instance.emoticonStrings.Length)
                return;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
            emotList0.SelectedIndex = -1;
            SmileyParser.Instance.AddEmoticons(index);
        }

        private void emotList1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (emotList1.SelectedIndex < 0)
                return;
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList1.SelectedIndex + SmileyParser.Instance.emoticon0Size;
            if (index >= SmileyParser.Instance.emoticonStrings.Length)
                return;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
            emotList1.SelectedIndex = -1;
            SmileyParser.Instance.AddEmoticons(index);
        }

        private void emotList2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (emotList2.SelectedIndex < 0)
                return;
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList2.SelectedIndex + SmileyParser.Instance.emoticon0Size + SmileyParser.Instance.emoticon1Size;
            if (index >= SmileyParser.Instance.emoticonStrings.Length)
                return;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
            emotList2.SelectedIndex = -1;
            SmileyParser.Instance.AddEmoticons(index);
        }

        private void emotList3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (emotList3.SelectedIndex < 0)
                return;
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList3.SelectedIndex + SmileyParser.Instance.emoticon0Size + SmileyParser.Instance.emoticon1Size + SmileyParser.Instance.emoticon2Size;
            if (index >= SmileyParser.Instance.emoticonStrings.Length)
                return;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            sendMsgTxtbox.Select(sendMsgTxtbox.Text.Length, 0);
            emotList3.SelectedIndex = -1;
            SmileyParser.Instance.AddEmoticons(index);
        }

        #endregion

        #region HELPER FUNCTIONS

        /// <summary>
        /// this function is called from UI thread only. No need to synch.
        /// </summary>
        private void ScrollToBottom(bool isForceScroll = false)
        {
            try
            {
                if (ocMessages.Count > 0 && (!IsMute || _userTappedJumpToBottom || ocMessages.Count < App.ViewModel.ConvMap[mContactNumber].MuteVal || isForceScroll))
                {
                    _userTappedJumpToBottom = false;

                    JumpToBottomGrid.Visibility = Visibility.Collapsed;
                    _unreadMessageCounter = 0;
                    if (vScrollBar != null && llsViewPort != null && ((vScrollBar.Maximum - vScrollBar.Value) < 2000))
                        llsViewPort.SetViewportOrigin(new System.Windows.Point(0, llsViewPort.Bounds.Height));
                    else
                        llsMessages.ScrollTo(ocMessages[ocMessages.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread::ScrollToBottom , Exception:" + ex.Message);
            }
        }

        void UpdateLastSeenOnUI(string status, bool showTip = false)
        {
            //Update UI
            Deployment.Current.Dispatcher.BeginInvoke(new Action<string, bool>(delegate(string lastSeenStatus, bool isShowTip)
            {
                if (App.newChatThreadPage != null)
                {
                    lastSeenTxt.Text = lastSeenStatus;
                    lastSeenPannel.Visibility = Visibility.Visible;
                }
            }), status, showTip);
        }

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
            if (appBar == null || appBar.MenuItems == null || appBar.MenuItems.Count == 0)
                return;

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
            smsCounterTxtBlk.Visibility = Visibility.Visible;
            scheduler.Schedule(hideSMSCounter, TimeSpan.FromSeconds(2));
        }

        private void hideSMSCounter()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                smsCounterTxtBlk.Visibility = Visibility.Collapsed;
            });
        }

        private void updateChatMetadata()
        {
            if (mCredits <= 0)
            {
                if (!string.IsNullOrEmpty(sendMsgTxtbox.Text))
                {
                    sendMsgTxtbox.Text = String.Empty;
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
                        sendMsgTxtbox.Text = String.Empty;
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
                        sendMsgTxtbox.Tap += SendMsgBtn_Tap;
                        sendMsgTxtbox.IsReadOnly = true;
                    }
                }
                else
                {
                    sendMsgTxtbox.Tap -= SendMsgBtn_Tap;
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
                overlayBorder.Tap += NoFreeSmsOverlay_Tap;
            }
            else
            {
                BlockTxtBlk.Text = AppResources.SelectUser_BlockMsg_Txt;
                btnBlockUnblock.Content = UNBLOCK_USER;
                btnBlockUnblock.Click += blockUnblock_Click;
                btnBlockUnblock.Click -= inviteUserBtn_Click;
                overlayBorder.Tap -= NoFreeSmsOverlay_Tap;
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
            this.Focus();//remove focus from textbox if exsist

            EnableDisableUI(!show);

            if (show)
            {
                overlayBorder.Visibility = Visibility.Visible;
                overlayMessagePanel.Visibility = Visibility.Visible;
            }
            else
            {
                overlayBorder.Visibility = Visibility.Collapsed;
                overlayMessagePanel.Visibility = Visibility.Collapsed;
            }
        }

        private void EnableDisableUI(bool enable)
        {
            HikeTitle.IsHitTestVisible = enable;
            llsMessages.IsHitTestVisible = enable;
            bottomPanel.IsHitTestVisible = enable;
            sendMsgTxtbox.IsHitTestVisible = enable;
            actionIcon.IsHitTestVisible = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay || sendMsgTxtbox.Text.Length <= 0 ? false : enable;

            EnableDisableAppBar(enable);
        }

        private void MsgCharTapped(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!App.appSettings.Contains(App.ENTER_TO_SEND) && (e.Key == Key.Enter || e.PlatformKeyCode == 0x0A))
            {
                SendMsg();
            }
        }

        //hyperlink was clicked in bubble. dont perform actions like h2h offline.
        bool _hyperlinkedClicked = false;

        void Hyperlink_Clicked(object sender, EventArgs e)
        {
            _hyperlinkedClicked = true;

            App.ViewModel.Hyperlink_Clicked(sender as object[]);
        }

        void ViewMoreMessage_Clicked(object sender, EventArgs e)
        {
            App.ViewModel.ViewMoreMessage_Clicked(sender);
        }

        private async void ClearChat()
        {
            gcPin.IsShow(false, false);
            tipControl.Visibility = Visibility.Visible;

            if (App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                App.ViewModel.ConvMap[mContactNumber].MetaData = null;

            await Task.Delay(1);

            MessagesTableUtils.deleteAllMessagesForMsisdn(mContactNumber);
            MiscDBUtil.deleteMsisdnData(mContactNumber);
            MessagesTableUtils.DeleteLongMessages(mContactNumber);
        }

        #endregion

        #region TYPING NOTIFICATIONS

        void ShowTypingNotification(object sender, object[] vals)
        {
            if (!App.appSettings.Contains(App.LAST_SEEN_SEETING) && !isGroupChat && _lastUpdatedLastSeenTimeStamp != 0)
            {
                var fStatus = FriendsTableUtils.GetFriendStatus(mContactNumber);

                if (fStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT) //dont show online if his last seen setting is off
                    UpdateLastSeenOnUI(AppResources.Online);
            }

            string typingNotSenderOrSendee = String.Empty;
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

        void AutoHidetypingNotification(object sender, object[] vals)
        {
            string typingNotSenderOrSendee = isGroupChat ? (string)vals[1] : (string)vals[0];
            if (mContactNumber == typingNotSenderOrSendee)
            {
                long timeElapsed = TimeUtils.getCurrentTimeStamp() - lastTypingNotificationShownTime;
                if (timeElapsed >= HikeConstants.TYPING_NOTIFICATION_AUTOHIDE)
                    HideTypingNotification();
            }
        }

        private void sendStartTypingNotification()
        {
            if (TimeUtils.getCurrentTimeStamp() - lastTypingNotificationSentTime >= HikeConstants.SEND_START_TYPING_TIMER)
            {
                lastTypingNotificationSentTime = TimeUtils.getCurrentTimeStamp();

                JObject obj = new JObject();
                try
                {
                    obj.Add(HikeConstants.TYPE, NetworkManager.START_TYPING);
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
            }
        }

        private void ShowTypingNotification()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ocMessages == null)
                    return;

                if (isTypingNotificationEnabled && !isTypingNotificationActive)
                {
                    if (convTypingNotification == null)
                    {
                        convTypingNotification = new ConvMessage();
                        convTypingNotification.CurrentOrientation = this.Orientation;
                        convTypingNotification.GrpParticipantState = ConvMessage.ParticipantInfoState.TYPING_NOTIFICATION;
                    }
                    ocMessages.Add(convTypingNotification);
                }
                isTypingNotificationActive = true;
                if (JumpToBottomGrid.Visibility == Visibility.Collapsed)
                    ScrollToBottom();
            });
            lastTypingNotificationShownTime = TimeUtils.getCurrentTimeStamp();
        }

        private void HideTypingNotification()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if ((!isTypingNotificationEnabled || isTypingNotificationActive) && ocMessages != null && ocMessages.Contains(convTypingNotification))
                    ocMessages.Remove(convTypingNotification);
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
                List<ConvMessage> listConvMessage = vals[0] is ConvMessage ? new List<ConvMessage>() { (ConvMessage)vals[0] } : (List<ConvMessage>)vals[0];

                bool hasNudge = false;
                /* Check if this is the same user for which this message is recieved*/
                if (listConvMessage.Count > 0 && listConvMessage[0].Msisdn == mContactNumber)
                {
                    long[] msgids = new long[listConvMessage.Count];
                    JArray ids = new JArray();
                    ConvMessage pinMessage = null;
                    HideTypingNotification();

                    for (int i = 0; i < listConvMessage.Count; i++)
                    {
                        ConvMessage convMessage = listConvMessage[i];
                        convMessage.MessageStatus = ConvMessage.State.RECEIVED_READ;

                        msgids[i] = convMessage.MessageId;

                        if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO) // do not notify in case of group end , user left , user joined
                            ids.Add(Convert.ToString(convMessage.MappedMessageId));
                        else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                        {
                            GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, mContactNumber);
                            convMessage.GroupMemberName = gp.FirstName;
                            pinMessage = convMessage;
                            ids.Add(Convert.ToString(convMessage.MappedMessageId));
                        }

                        if (convMessage.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                            updateLastMsgColor(convMessage.Msisdn);

                        hasNudge = convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.POKE) && !_isMute;

                        // Update UI
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {

                            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE)
                            {
                                mContactName = App.ViewModel.ConvMap[convMessage.Msisdn].ContactName;
                                userName.Text = mContactName;
                            }
                            else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED)
                                userImage.Source = App.ViewModel.ConvMap[convMessage.Msisdn].AvatarImage;

                            AddMessageToOcMessages(convMessage, false, true);
                            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                                ShowJumpToBottom(true);

                            if (vals.Length == 3)
                            {
                                try
                                {
                                    if (vals[2] is ConvMessage)
                                    {
                                        ConvMessage cm = (ConvMessage)vals[2];
                                        if (cm != null)
                                        {
                                            AddMessageToOcMessages(cm, false, true);
                                            if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                                                ShowJumpToBottom(true);
                                        }
                                    }
                                }
                                catch { }
                            }
                        });
                    }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (pinMessage != null)
                        {
                            gcPin.UpdateContent(pinMessage.GCPinMessageSenderName, pinMessage.DispMessage);

                            if ( _isOnPage && App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                            {
                                JObject metadata = App.ViewModel.ConvMap[mContactNumber].MetaData;

                                if (metadata != null)
                                {
                                    metadata[HikeConstants.UNREADPINS] = metadata.Value<int>(HikeConstants.UNREADPINS) - 1;
                                    metadata[HikeConstants.READPIN] = true;
                                    App.ViewModel.ConvMap[mContactNumber].MetaData = metadata;
                                    ConversationTableUtils.updateConversation(App.ViewModel.ConvMap[mContactNumber]);
                                }

                                gcPin.SetUnreadPinCount((int)metadata[HikeConstants.UNREADPINS]);
                            }

                            if (!_isNewPin)
                                gcPin.IsShow(false, true);
                        }
                    });

                    if (ids.Count > 0)
                    {
                        JObject jobj = new JObject();
                        jobj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                        jobj.Add(HikeConstants.TO, mContactNumber);
                        jobj.Add(HikeConstants.DATA, ids);
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, jobj);
                    }
                    if (hasNudge)
                    {
                        bool isVibrateEnabled = true;
                        App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);

                        if (isVibrateEnabled)
                        {
                            VibrateController vibrate = VibrateController.Default;
                            vibrate.Start(TimeSpan.FromMilliseconds(HikeConstants.VIBRATE_DURATION));
                        }
                    }
                    // Update status to received read in db.
                    mPubSub.publish(HikePubSub.MESSAGE_RECEIVED_READ, msgids);
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

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (lastSeenTxt.Text != AppResources.Online)
                            {
                                if (_isSendAllAsSMSVisible && ocMessages != null && msg.MessageId >= _lastUnDeliveredMessage.MessageId)
                                {
                                    ocMessages.Remove(_tap2SendAsSMSMessage);
                                    _isSendAllAsSMSVisible = false;
                                }

                                var worker = new BackgroundWorker();

                                worker.DoWork += (s, e) =>
                                {
                                    StartForceSMSTimer(true);
                                };

                                worker.RunWorkerAsync();
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread.xaml :: onEventReceived ,SERVER_RECEIVED_MSG Exception : " + ex.StackTrace);
                }
            }

            #endregion

            #region SOCKET_MESSAGE_WRITE

            else if (HikePubSub.MSG_WRITTEN_SOCKET == type)
            {
                long msgId = (long)obj;
                try
                {
                    ConvMessage msg = null;
                    msgMap.TryGetValue(msgId, out msg);
                    if (msg != null)
                    {
                        msg.MessageStatus = ConvMessage.State.SENT_SOCKET_WRITE;
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
                IList<long> listMesages = vals[0] is long ? new List<long>() { (long)vals[0] } : (IList<long>)vals[0];
                string msisdnToCheck = (string)vals[1];
                if (msisdnToCheck != mContactNumber)
                    return;
                try
                {
                    bool isLastUndeliverdMessageFound = false;
                    foreach (long msgId in listMesages)
                    {
                        ConvMessage msg = null;
                        msgMap.TryGetValue(msgId, out msg);
                        if (msg != null)
                        {
                            if (msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED)
                                msg.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_DELIVERED;
                            else if (msg.MessageStatus < ConvMessage.State.SENT_DELIVERED)
                                msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                            else
                                return;
                        }
                        if (!isLastUndeliverdMessageFound)
                            isLastUndeliverdMessageFound = msg == _lastUnDeliveredMessage;
                    }
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (_isSendAllAsSMSVisible && ocMessages != null && isLastUndeliverdMessageFound)
                        {
                            ocMessages.Remove(_tap2SendAsSMSMessage);
                            _isSendAllAsSMSVisible = false;
                            ShowForceSMSOnUI();
                        }

                    });
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
                IList<long> ids = (IList<long>)vals[0];
                string msisdnToCheck = (string)vals[1];
                if (msisdnToCheck != mContactNumber || ids == null || ids.Count == 0)
                    return;

                JArray readByArray = null;
                if (isGroupChat)
                    readByArray = (JArray)vals[2];

                long maxId = 0;
                // TODO we could keep a map of msgId -> conversation objects somewhere to make this faster
                for (int i = 0; i < ids.Count; i++)
                {
                    try
                    {
                        if (maxId < ids[i])
                            maxId = ids[i];

                        ConvMessage msg = null;
                        msgMap.TryGetValue(ids[i], out msg);
                        if (msg != null)
                        {
                            if (msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED || msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                                msg.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;
                            else
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

                if (isGroupChat && readByArray != null)
                {
                    if (_groupInfo == null)
                        _groupInfo = GroupTableUtils.getGroupInfoForId(mContactNumber);
                    if (_groupInfo != null)
                    {
                        if (_groupInfo.LastReadMessageId == maxId)
                        {
                            for (int i = 0; i < readByArray.Count; i++)
                            {
                                if (!_groupInfo.ReadByArray.Contains(readByArray[i]))
                                    _groupInfo.ReadByArray.Add(readByArray[i]);
                            }
                        }
                        else
                        {
                            _groupInfo.LastReadMessageId = maxId;
                            _groupInfo.ReadByArray = readByArray;
                        }
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

                                if (msg.IsSent && (msg.FileAttachment == null || (msg.FileAttachment.FileState == Attachment.AttachmentState.COMPLETED)))
                                {
                                    idsToUpdate.Add(kv.Key);

                                    if (msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED || msg.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                                        msg.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;
                                    else
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

                if (_isSendAllAsSMSVisible && _lastUnDeliveredMessage.MessageStatus != ConvMessage.State.SENT_CONFIRMED)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (ocMessages == null)
                            return;

                        ocMessages.Remove(_tap2SendAsSMSMessage);
                        _isSendAllAsSMSVisible = false;
                        ShowForceSMSOnUI();
                    });
                }

                UpdateLastSentMessageStatusOnUI();
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
                            foreach (GroupParticipant gp in GroupManager.Instance.GroupParticpantsCache[mContactNumber])
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
                            chatPaint.Opacity = 0.5;

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

                        if (isGroupChat)
                            newPin.Opacity = 1;

                        chatPaint.Opacity = 1;
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

            #region LAST SEEN

            else if (HikePubSub.LAST_SEEN == type && !isGroupChat)
            {
                if (!App.appSettings.Contains(App.LAST_SEEN_SEETING))
                {
                    object[] vals = (object[])obj;
                    string fromMsisdn = (string)vals[0];
                    long lastSeen = (long)vals[1];

                    var fStatus = FriendsTableUtils.GetFriendStatus(mContactNumber);

                    if (fromMsisdn == mContactNumber && fStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT && isOnHike)
                    {
                        if (lastSeen > _lastUpdatedLastSeenTimeStamp || lastSeen == 0)
                        {
                            _lastUpdatedLastSeenTimeStamp = lastSeen == 0 ? TimeUtils.getCurrentTimeStamp() : lastSeen;
                            UpdateLastSeenOnUI(_lastSeenHelper.GetLastSeenTimeStampStatus(lastSeen));
                        }
                        else if (lastSeen == -1)
                        {
                            _lastUpdatedLastSeenTimeStamp = 0;

                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                lastSeenTxt.Text = isOnHike ? AppResources.On_Hike : AppResources.On_SMS;
                            });
                        }

                        if (lastSeen != 0 && !_isSendAllAsSMSVisible)
                            StartForceSMSTimer(false);
                        else if (lastSeen == 0)
                        {
                            UpdateLastSentMessageStatusOnUI();

                            if (_isSendAllAsSMSVisible)
                            {
                                Deployment.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    if (ocMessages == null)
                                        return;

                                    if (_isSendAllAsSMSVisible)
                                    {
                                        ocMessages.Remove(_tap2SendAsSMSMessage);
                                        _isSendAllAsSMSVisible = false;
                                    }
                                });
                            }
                        }
                    }
                }
            }

            #endregion

            #region UPDATE_UI

            else if (HikePubSub.UPDATE_PROFILE_ICON == type)
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

                if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                    _activeUsers = GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(g => g.HasLeft == false && g.IsOnHike == true).Count();

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        mContactName = App.ViewModel.ConvMap[mContactNumber].NameToShow;
                        userName.Text = mContactName;

                        if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                            lastSeenTxt.Text = String.Format(AppResources.People_In_Group, GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(gp => gp.HasLeft == false).Count() + 1);
                        else
                            lastSeenTxt.Text = String.Empty;
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

                if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                    _activeUsers = GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(g => g.HasLeft == false && g.IsOnHike == true).Count();

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        mContactName = App.ViewModel.ConvMap[mContactNumber].NameToShow;
                        userName.Text = mContactName;

                        if (GroupManager.Instance.GroupParticpantsCache.ContainsKey(mContactNumber))
                            lastSeenTxt.Text = String.Format(AppResources.People_In_Group, GroupManager.Instance.GroupParticpantsCache[mContactNumber].Where(gp => gp.HasLeft == false).Count() + 1);
                        else
                            lastSeenTxt.Text = String.Empty;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NEW_CHAT_THREAD :: Exception in participant joined group : " + ex.StackTrace);
                    }
                });
            }

            #endregion

            #region Chat Background Changed
            else if (HikePubSub.CHAT_BACKGROUND_REC == type)
            {
                var sender = (string)obj;

                if (sender == mContactNumber)
                {
                    ChatBackgroundHelper.Instance.SetSelectedBackgorundFromMap(sender);
                    App.ViewModel.LastSelectedBackground = App.ViewModel.SelectedBackground;

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (App.ViewModel.SelectedBackground != null)
                        {
                            ChangeBackground();
                            chatBackgroundList.SelectedItem = ChatBackgroundHelper.Instance.BackgroundList.Where(c => c == App.ViewModel.SelectedBackground).First();
                        }
                    });
                }
            }
            #endregion

            #region Pin Message Deleted From Pin History

            else if (type == HikePubSub.DELETE_FROM_NEWCHATTHREAD_OC )
            {
                if (obj is ConvMessage)
                {
                    ConvMessage msg = obj as ConvMessage;

                    if (msg != null && msg.Msisdn == mContactNumber)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                ocMessages.Remove(msg);

                                //For removing Pin User Control
                                if (isGroupChat && msg.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE && App.ViewModel.ConvMap.ContainsKey(mContactNumber))
                                {
                                    JObject metaData = App.ViewModel.ConvMap[mContactNumber].MetaData;

                                    if (metaData != null && metaData[HikeConstants.PINID].Type == JTokenType.Null)
                                        gcPin.IsShow(false, false);

                                    tipControl.Visibility = Visibility.Visible;
                                }
                                
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Exception thrown in NewChat Thread while deleting Pin from Pin History:" + ex.StackTrace);
                            }
                        });
                    }
                }
            }

            #endregion
        }

        private void groupChatEnd()
        {
            isGroupAlive = false;
            EnableDisableUI(false);
            llsMessages.IsHitTestVisible = true;
            chatPaint.Opacity = 0.5;
            newPin.Opacity = 0.5;
        }

        private void groupChatAlive()
        {
            isGroupAlive = true;
            EnableDisableUI(true);
        }

        #endregion

        #region FileTransfer

        bool _uploadProgressBarIsTapped = false;


        void FileTransferStatusUpdated(object sender, FileTransferSatatusChangedEventArgs e)
        {
            FileInfoBase fInfo = e.FileInfo;
            var id = Convert.ToInt64(fInfo.MessageId);

            if (msgMap.ContainsKey(id))
            {
                var convMessage = msgMap[id];
                UpdateFileTransferProgresInConvMessage(fInfo, convMessage, e.IsStateChanged);
            }
        }

        private void UpdateFileTransferProgresInConvMessage(FileInfoBase fInfo, ConvMessage convMessage, bool isStateChanged)
        {
            if (convMessage == null)
                return;

            convMessage.ProgressBarValue = fInfo.PercentageTransfer;
            convMessage.ProgressText = string.Format("{0}/{1}", Utils.ConvertToStorageSizeString(fInfo.BytesTransfered), Utils.ConvertToStorageSizeString(fInfo.TotalBytes));

            if (isStateChanged)
            {
                Attachment.AttachmentState state = Attachment.AttachmentState.FAILED;

                if (fInfo.FileState == FileTransferState.CANCELED)
                {
                    state = Attachment.AttachmentState.CANCELED;

                    if (convMessage.IsSent)
                        convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                }
                else if (fInfo.FileState == FileTransferState.COMPLETED)
                {
                    state = Attachment.AttachmentState.COMPLETED;

                    if (fInfo is FileUploader)
                    {
                        var fileUploader = fInfo as FileUploader;

                        // Update fileKey for convMessage on UI if already not updated
                        if (fileUploader.IsFileExist)
                            convMessage.FileAttachment.FileKey = fileUploader.FileKey;
                        else
                        {
                            JObject data = fileUploader.SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                            convMessage.FileAttachment.FileKey = data[HikeConstants.FILE_KEY].ToString();
                        }

                        convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                    }
                }
                else if (fInfo.FileState == FileTransferState.PAUSED)
                    state = Attachment.AttachmentState.PAUSED;
                else if (fInfo.FileState == FileTransferState.MANUAL_PAUSED)
                    state = Attachment.AttachmentState.MANUAL_PAUSED;
                else if (fInfo.FileState == FileTransferState.FAILED)
                {
                    state = Attachment.AttachmentState.FAILED;

                    if (fInfo is FileUploader)
                        convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                }
                else if (fInfo.FileState == FileTransferState.STARTED)
                    state = Attachment.AttachmentState.STARTED;
                else if (fInfo.FileState == FileTransferState.NOT_STARTED)
                    state = Attachment.AttachmentState.NOT_STARTED;
                else if (fInfo.FileState == FileTransferState.DOES_NOT_EXIST)
                {
                    if (convMessage.UserTappedDownload)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            MessageBox.Show(AppResources.File_Not_Exist_Message, AppResources.File_Not_Exist_Caption, MessageBoxButton.OK);
                        });
                    }

                    state = Attachment.AttachmentState.FAILED;

                    if (fInfo is FileUploader)
                        convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                }

                if (fInfo.FileState == FileTransferState.COMPLETED)
                    convMessage.ProgressBarValue = 100;

                convMessage.SetAttachmentState(state);

                if (fInfo is FileDownloader)
                {
                    MiscDBUtil.UpdateFileAttachmentState(fInfo.Msisdn, fInfo.MessageId, state);

                    if (fInfo.FileState == FileTransferState.COMPLETED)
                    {
                        msgMap.Remove(convMessage.MessageId);

                        if (convMessage.UserTappedDownload)
                        {
                            Deployment.Current.Dispatcher.BeginInvoke(() =>
                            {
                                displayAttachment(convMessage);
                            });
                        }
                    }
                }
                else
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
            }
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (currentAudioMessage == null || !currentAudioMessage.IsPlaying)
                return;

            if (mediaElement != null && mediaElement.Source != null)
            {
                var pos = mediaElement.Position.TotalSeconds;
                var dur = mediaElement.NaturalDuration.TimeSpan.TotalSeconds;

                if (currentAudioMessage != null && dur != 0)
                {
                    if (pos == dur)
                        currentAudioMessage.PlayProgressBarValue = 0;
                    else
                        currentAudioMessage.PlayProgressBarValue = pos * 100 / dur;

                    string durationText = String.IsNullOrEmpty(currentAudioMessage.DurationText) ? String.Empty : currentAudioMessage.DurationText;

                    currentAudioMessage.PlayTimeText = pos == dur || pos == 0 ? durationText : mediaElement.NaturalDuration.TimeSpan.Subtract(mediaElement.Position).ToString("mm\\:ss");
                }
            }
        }

        private void PauseResume_Tapped(object sender, RoutedEventArgs e)
        {
            _uploadProgressBarIsTapped = true;

            var button = sender as Button;
            if (button != null)
            {
                var id = Convert.ToInt64(button.Tag);
                if (msgMap.ContainsKey(id))
                {
                    var convMessage = msgMap[id];

                    if (convMessage.FileAttachment != null)
                    {
                        if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                            PauseTransfer(convMessage);
                        else if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.PAUSED || convMessage.FileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
                            ResumeTransfer(convMessage);
                    }
                }
                else
                    _uploadProgressBarIsTapped = false;
            }
        }

        private void shareLocation()
        {
            if (!isGroupChat || isGroupAlive)
            {
                object[] locationInfo = (object[])PhoneApplicationService.Current.State[HikeConstants.SHARED_LOCATION];
                PhoneApplicationService.Current.State.Remove(HikeConstants.SHARED_LOCATION);

                byte[] imageThumbnail = null;
                JObject locationJSON = (JObject)locationInfo[0];
                imageThumbnail = (byte[])locationInfo[1];
                var fileData = locationJSON[HikeConstants.FILES_DATA][0];

                string locationJSONString = locationJSON.ToString();

                byte[] locationBytes = (new System.Text.UTF8Encoding()).GetBytes(locationJSONString);

                if (!StorageManager.StorageManager.Instance.IsDeviceMemorySufficient(locationBytes.Length))
                {
                    MessageBox.Show(AppResources.Memory_Limit_Reached_Body, AppResources.Memory_Limit_Reached_Header, MessageBoxButton.OK);
                    return;
                }

                var place = (String)fileData[HikeConstants.LOCATION_TITLE];
                var vicinity = (String)fileData[HikeConstants.LOCATION_ADDRESS];
                var fileName = (String)fileData[HikeConstants.FILE_NAME];

                if (String.IsNullOrEmpty(fileName))
                    fileName = HikeConstants.LOCATION_FILENAME;

                ConvMessage convMessage = new ConvMessage(AppResources.Location_Txt, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation)
                {
                    IsSms = !isOnHike,
                    HasAttachment = true,
                    MetaDataString = locationJSONString
                };

                convMessage.FileAttachment = new Attachment(fileName, imageThumbnail, Attachment.AttachmentState.NOT_STARTED, Attachment.AttachemntSource.OTHER, locationBytes.Length);
                convMessage.FileAttachment.ContentType = HikeConstants.LOCATION_CONTENT_TYPE;

                AddNewMessageToUI(convMessage, false);

                ScrollToBottom();

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = locationBytes;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        /// <summary>
        /// Call this function to transfer files of type audio and video
        /// </summary>
        private void TransferFile()
        {
            bool isAudio = true;
            byte[] fileBytes = null;
            byte[] thumbnail = null;

            string filePath = null;
            int fileSize = 0;
            Attachment.AttachemntSource source = Attachment.AttachemntSource.CAMERA;

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
            {
                fileBytes = (byte[])PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED];

                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED_DURATION))
                {
                    _recordedDuration = (int)PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED_DURATION];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.AUDIO_RECORDED_DURATION);
                }

                source = Attachment.AttachemntSource.CAMERA;
                fileSize = fileBytes.Length;
                isAudio = true;
                PhoneApplicationService.Current.State.Remove(HikeConstants.AUDIO_RECORDED);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_RECORDED))
            {
                fileSize = MiscDBUtil.GetFileSize(HikeConstants.TEMP_VIDEO_NAME);

                if (fileSize == 0)
                {
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_RECORDED);
                    return;
                }

                source = Attachment.AttachemntSource.CAMERA;
                thumbnail = (byte[])PhoneApplicationService.Current.State[HikeConstants.VIDEO_RECORDED];
                filePath = HikeConstants.TEMP_VIDEO_NAME;
                isAudio = false;

                PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_RECORDED);
            }
            else if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.VIDEO_SHARED))
            {
                VideoItem videoShared = (VideoItem)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED];
                thumbnail = videoShared.ThumbnailBytes;

                if (thumbnail != null && thumbnail.Length > HikeConstants.MAX_THUMBNAILSIZE)
                {
                    BitmapImage image = new BitmapImage();
                    UI_Utils.Instance.createImageFromBytes(thumbnail, image);
                    thumbnail = UI_Utils.DiminishThumbnailQuality(image);
                }

                try
                {
                    StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri(videoShared.FilePath, UriKind.Relative));
                    filePath = videoShared.FilePath;
                    fileSize = Convert.ToInt32(streamInfo.Stream.Length);

                    if (fileSize <= 0)
                    {
                        MessageBox.Show(AppResources.CT_FileUnableToSend_Text, AppResources.CT_FileNotSupported_Caption_Text, MessageBoxButton.OK);
                        PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NewChatThread :: AudioFileTransfer , Exception : " + ex.StackTrace);
                }

                source = Attachment.AttachemntSource.GALLERY;
                isAudio = false;
                PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
            }

            if (!StorageManager.StorageManager.Instance.IsDeviceMemorySufficient(fileSize))
            {
                MessageBox.Show(AppResources.Memory_Limit_Reached_Body, AppResources.Memory_Limit_Reached_Header, MessageBoxButton.OK);
                return;
            }

            if (fileSize > HikeConstants.FILE_MAX_SIZE)
            {
                MessageBox.Show(AppResources.CT_FileSizeExceed_Text, AppResources.CT_FileSizeExceed_Caption_Text, MessageBoxButton.OK);
                return;
            }

            if (!isGroupChat || isGroupAlive)
            {
                ConvMessage convMessage = new ConvMessage(String.Empty, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;
                string fileName;

                if (isAudio)
                {
                    fileName = "aud_" + TimeUtils.getCurrentTimeStamp().ToString() + ".mp3";
                    convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.NOT_STARTED, source, fileSize);
                    convMessage.FileAttachment.ContentType = HikeConstants.FILE_TYPE_AUDIO;

                    var fileInfo = new JObject();
                    fileInfo[HikeConstants.FILE_NAME] = convMessage.FileAttachment.FileName;
                    fileInfo[HikeConstants.FILE_KEY] = convMessage.FileAttachment.FileKey;
                    fileInfo[HikeConstants.FILE_CONTENT_TYPE] = convMessage.FileAttachment.ContentType;
                    fileInfo[HikeConstants.FILE_PLAY_TIME] = _recordedDuration.ToString();

                    if (convMessage.FileAttachment.Thumbnail != null)
                        fileInfo[HikeConstants.FILE_THUMBNAIL] = System.Convert.ToBase64String(convMessage.FileAttachment.Thumbnail);

                    convMessage.MetaDataString = fileInfo.ToString(Newtonsoft.Json.Formatting.None);
                    convMessage.Message = AppResources.Audio_Txt;
                }
                else
                {
                    fileName = "vid_" + TimeUtils.getCurrentTimeStamp().ToString() + ".mp4";
                    convMessage.FileAttachment = new Attachment(fileName, thumbnail, Attachment.AttachmentState.NOT_STARTED, source, fileSize);
                    convMessage.FileAttachment.ContentType = HikeConstants.FILE_TYPE_VIDEO;
                    convMessage.Message = AppResources.Video_Txt;
                }

                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[4];
                vals[0] = convMessage;
                vals[1] = fileBytes;
                vals[2] = filePath;
                vals[3] = fileSize;

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

                var bytes = Encoding.UTF8.GetBytes(contactJson.ToString(Newtonsoft.Json.Formatting.None));

                if (!StorageManager.StorageManager.Instance.IsDeviceMemorySufficient(bytes.Length))
                {
                    MessageBox.Show(AppResources.Memory_Limit_Reached_Body, AppResources.Memory_Limit_Reached_Header, MessageBoxButton.OK);
                    return;
                }

                string fileName = string.IsNullOrEmpty(con.Name) ? AppResources.ContactTransfer_Text : con.Name;

                ConvMessage convMessage = new ConvMessage(String.Empty, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                convMessage.HasAttachment = true;

                convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.NOT_STARTED, Attachment.AttachemntSource.OTHER, bytes.Length);
                convMessage.FileAttachment.ContentType = HikeConstants.CT_CONTACT;
                convMessage.Message = AppResources.ContactTransfer_Text;
                convMessage.MetaDataString = contactJson.ToString(Newtonsoft.Json.Formatting.None);

                AddNewMessageToUI(convMessage, false);

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = bytes;
                App.HikePubSubInstance.publish(HikePubSub.ATTACHMENT_SENT, vals);
            }
        }

        private async Task MultipleImagesTransfer()
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.MULTIPLE_IMAGES))
            {
                List<PhotoItem> listPic = PhoneApplicationService.Current.State[HikeConstants.MULTIPLE_IMAGES] as List<PhotoItem>;

                foreach (PhotoItem pic in listPic)
                {
                    //Add delay so that each message has different timestamps and equals function for convmessages runs correctly
                    await Task.Delay(1);

                    if (!SendImage(pic.ImageSource, "image_" + TimeUtils.getCurrentTimeStamp().ToString(), Attachment.AttachemntSource.GALLERY))
                        break;

                    pic.Pic.Dispose();
                }

                PhoneApplicationService.Current.State.Remove(HikeConstants.MULTIPLE_IMAGES);
            }
        }

        private void FileAttachmentMessage_Tap(ConvMessage convMessage)
        {
            if (_uploadProgressBarIsTapped)
            {
                _uploadProgressBarIsTapped = false;
                llsMessages.SelectedItem = null;
                return;
            }

            emoticonPanel.Visibility = Visibility.Collapsed;
            attachmentMenu.Visibility = Visibility.Collapsed;

            if (convMessage == null)
                return;

            if (!isGroupChat && !_isHikeBot && convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
            {
                PhoneApplicationService.Current.State[HikeConstants.USERINFO_FROM_CHATTHREAD_PAGE] = statusObject;
                NavigationService.Navigate(new Uri("/View/UserProfile.xaml", UriKind.Relative));
            }
            if (convMessage.StickerObj != null && !convMessage.StickerObj.IsStickerDownloaded && convMessage.ImageDownloadFailed)
            {
                GetHighResStickerForUi(convMessage);
            }
            else if (convMessage.FileAttachment == null || convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
            {
                if (convMessage.FileAttachment != null)
                {
                    if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION) || convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        displayAttachment(convMessage);
                    else
                        PauseTransfer(convMessage); // only pause for files and not for location/contacts
                }
            }
            else if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED && convMessage.FileAttachment.FileState != Attachment.AttachmentState.STARTED)
            {
                if (!convMessage.IsSent)
                {
                    //Downloads

                    if (NetworkInterface.GetIsNetworkAvailable())
                    {
                        if (FileTransferManager.Instance.IsTransferPossible())
                        {
                            bool taskPlaced = false;

                            if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.FAILED || convMessage.FileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED || convMessage.FileAttachment.FileState == Attachment.AttachmentState.CANCELED)
                                convMessage.ChangingState = taskPlaced = FileTransfers.FileTransferManager.Instance.DownloadFile(convMessage.Msisdn, convMessage.MessageId.ToString(), convMessage.FileAttachment.FileKey, convMessage.FileAttachment.ContentType, convMessage.FileAttachment.FileSize);
                            else if (ResumeTransfer(convMessage))
                                taskPlaced = true;

                            if (taskPlaced)
                                convMessage.UserTappedDownload = true;

                            if (taskPlaced && !msgMap.ContainsKey(convMessage.MessageId))
                                msgMap.Add(convMessage.MessageId, convMessage);
                        }
                        else
                            MessageBox.Show(AppResources.FT_MaxFiles_Txt, AppResources.FileTransfer_LimitReached, MessageBoxButton.OK);
                    }
                    else
                        MessageBox.Show(AppResources.No_Network_Txt, AppResources.FileTransfer_ErrorMsgBoxText, MessageBoxButton.OK);
                }
                else
                {
                    if (!NetworkInterface.GetIsNetworkAvailable())
                    {
                        MessageBox.Show(AppResources.No_Network_Txt, AppResources.FileTransfer_UploadErrorMsgBoxText, MessageBoxButton.OK);
                        return;
                    }

                    // Uploads
                    if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                    {
                        convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + AccountUtils.FILE_TRANSFER_BASE_URL +
                            "/" + convMessage.FileAttachment.FileKey;
                    }
                    else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                    {
                        convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + AccountUtils.FILE_TRANSFER_BASE_URL +
                            "/" + convMessage.FileAttachment.FileKey;
                    }
                    else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                    {
                        byte[] locationInfoBytes = null;
                        MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                            convMessage.MessageId, out locationInfoBytes);
                        string locationInfoString = System.Text.Encoding.UTF8.GetString(locationInfoBytes, 0, locationInfoBytes.Length);
                        convMessage.MetaDataString = locationInfoString;
                    }
                    else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                    {
                        convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + AccountUtils.FILE_TRANSFER_BASE_URL +
                            "/" + convMessage.FileAttachment.FileKey;
                    }

                    // check if the message can be resumed
                    bool transferPlaced = ResumeTransfer(convMessage);

                    // if message cannot be resumed try to fresh upload
                    if (!transferPlaced)
                    {
                        int length = 0;

                        if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT) || convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            length = Encoding.UTF8.GetBytes(convMessage.MetaDataString).Length;
                        else
                            length = MiscDBUtil.GetFileSize(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn.Replace(":", "_") + "/" + convMessage.MessageId);

                        convMessage.ChangingState = transferPlaced = FileTransferManager.Instance.UploadFile(mContactNumber, convMessage.MessageId.ToString(), convMessage.FileAttachment.FileName, convMessage.FileAttachment.ContentType, length, string.Empty);
                    }

                    // if transfer was not placed because of queue limit reached then display limit reached message
                    if (!transferPlaced && !FileTransferManager.Instance.IsTransferPossible())
                        MessageBox.Show(AppResources.FT_MaxFiles_Txt, AppResources.FileTransfer_LimitReached, MessageBoxButton.OK);

                    if (transferPlaced && !msgMap.ContainsKey(convMessage.MessageId))
                        msgMap.Add(convMessage.MessageId, convMessage);
                }
            }
            else
                displayAttachment(convMessage);

            llsMessages.SelectedItem = null;
        }

        public void displayAttachment(ConvMessage convMessage)
        {
            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            string contactNumberOrGroupId = mContactNumber.Replace(":", "_");

            if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
            {
                object[] fileTapped = new object[2];
                fileTapped[0] = convMessage.MessageId;
                fileTapped[1] = contactNumberOrGroupId;
                PhoneApplicationService.Current.State["objectForFileTransfer"] = fileTapped;
                NavigationService.Navigate(new Uri("/View/DisplayImage.xaml", UriKind.Relative));
            }
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
            {
                App.ViewModel.PauseBackgroundAudio();
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId + "/" + Convert.ToString(convMessage.MessageId);
                Utils.PlayFileInMediaPlayer(fileLocation);
            }
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
            {
                string fileLocation = HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId + "/" + Convert.ToString(convMessage.MessageId);

                if (mediaElement != null)
                {
                    if (mediaElement.Source != null)
                    {
                        if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PLAYER_TIMER))
                            PhoneApplicationService.Current.State.Remove(HikeConstants.PLAYER_TIMER);

                        if (mediaElement.Source.OriginalString.Contains(fileLocation)) //handle already playing audio
                        {
                            if (currentAudioMessage != null) // case pause/play the alresdy playing/paused file
                            {
                                if (currentAudioMessage.IsPlaying)
                                {
                                    currentAudioMessage.IsPlaying = false;
                                    mediaElement.Pause();
                                    App.ViewModel.ResumeBackgroundAudio();
                                }
                                else
                                {
                                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                                    CompositionTarget.Rendering += CompositionTarget_Rendering;

                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.IsStopped = false;
                                    App.ViewModel.PauseBackgroundAudio();
                                    mediaElement.Play();
                                }
                            }
                            else // restart audio
                            {
                                if (mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds > 0)
                                {
                                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                                    CompositionTarget.Rendering += CompositionTarget_Rendering;

                                    currentAudioMessage = convMessage;
                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.IsStopped = false;
                                    App.ViewModel.PauseBackgroundAudio();
                                    mediaElement.Play();
                                }
                            }
                        }
                        else // start new audio
                        {
                            try
                            {
                                mediaElement.Source = null;
                                App.ViewModel.PauseBackgroundAudio();
                                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                                {
                                    if (store.FileExists(fileLocation))
                                    {
                                        using (var isfs = new IsolatedStorageFileStream(fileLocation, FileMode.Open, store))
                                        {
                                            this.mediaElement.SetSource(isfs);
                                        }
                                    }
                                }

                                if (currentAudioMessage != null) //stop prev audio in case its running
                                {
                                    currentAudioMessage.IsPlaying = false;
                                    currentAudioMessage.IsStopped = true;
                                    currentAudioMessage.PlayTimeText = currentAudioMessage.DurationText;
                                    currentAudioMessage.PlayProgressBarValue = 0;
                                    currentAudioMessage = null;
                                }

                                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                                CompositionTarget.Rendering += CompositionTarget_Rendering;

                                currentAudioMessage = convMessage;

                                if (currentAudioMessage != null)
                                {
                                    currentAudioMessage.IsStopped = false;
                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.PlayProgressBarValue = 0;
                                }
                            }
                            catch (Exception ex) //Code should never reach here
                            {
                                Debug.WriteLine("NewChatTHread :: Play Audio Attachment :: Exception while playing audio file" + ex.StackTrace);
                            }
                        }
                    }
                    else //restart paused audio - from lock or suspended state
                    {
                        if (currentAudioMessage != null && currentAudioMessage == convMessage)
                        {
                            if (LayoutRoot.FindName("myMediaElement") == null)
                                LayoutRoot.Children.Add(mediaElement);

                            try
                            {
                                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                                {
                                    if (store.FileExists(fileLocation))
                                    {
                                        using (var isfs = new IsolatedStorageFileStream(fileLocation, FileMode.Open, store))
                                        {
                                            this.mediaElement.SetSource(isfs);
                                        }
                                    }
                                }

                                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                                CompositionTarget.Rendering += CompositionTarget_Rendering;
                                App.ViewModel.PauseBackgroundAudio();
                                mediaElement.Play();
                                currentAudioMessage.IsStopped = false;
                                currentAudioMessage.IsPlaying = true;
                            }
                            catch (Exception ex) //Code should never reach here
                            {
                                Debug.WriteLine("NewChatTHread :: Play Audio Attachment :: Exception while playing audio file" + ex.StackTrace);
                            }
                        }
                        else //play new file after resume app
                        {
                            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PLAYER_TIMER))
                                PhoneApplicationService.Current.State.Remove(HikeConstants.PLAYER_TIMER);

                            if (currentAudioMessage != null)
                            {
                                currentAudioMessage.IsStopped = true;
                                currentAudioMessage.IsPlaying = false;
                                currentAudioMessage.PlayTimeText = currentAudioMessage.DurationText;
                                currentAudioMessage.PlayProgressBarValue = 0;
                                currentAudioMessage = null;
                            }

                            currentAudioMessage = convMessage;

                            if (LayoutRoot.FindName("myMediaElement") == null)
                                LayoutRoot.Children.Add(mediaElement);

                            try
                            {
                                using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                                {
                                    if (store.FileExists(fileLocation))
                                    {
                                        using (var isfs = new IsolatedStorageFileStream(fileLocation, FileMode.Open, store))
                                        {
                                            this.mediaElement.SetSource(isfs);
                                        }
                                    }
                                }

                                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                                CompositionTarget.Rendering += CompositionTarget_Rendering;
                                App.ViewModel.PauseBackgroundAudio();
                                mediaElement.Play();

                                if (currentAudioMessage != null)
                                {
                                    currentAudioMessage.IsStopped = false;
                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.PlayProgressBarValue = 0;
                                }
                            }
                            catch (Exception ex) //Code should never reach here
                            {
                                Debug.WriteLine("NewChatTHread :: Play Audio Attachment :: Exception while playing audio file" + ex.StackTrace);
                            }
                        }
                    }
                }
                else // play first audio
                {
                    if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PLAYER_TIMER))
                        PhoneApplicationService.Current.State.Remove(HikeConstants.PLAYER_TIMER);

                    mediaElement = new MediaElement() { Name = "myMediaElement" };
                    mediaElement.MediaEnded -= mediaPlayback_MediaEnded;
                    mediaElement.MediaEnded += mediaPlayback_MediaEnded;
                    mediaElement.MediaFailed -= mediaPlayback_MediaFailed;
                    mediaElement.MediaFailed += mediaPlayback_MediaFailed;
                    mediaElement.CurrentStateChanged -= mediaElement_CurrentStateChanged;
                    mediaElement.CurrentStateChanged += mediaElement_CurrentStateChanged;

                    currentAudioMessage = convMessage;
                    LayoutRoot.Children.Add(mediaElement);

                    try
                    {
                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (store.FileExists(fileLocation))
                            {
                                using (var isfs = new IsolatedStorageFileStream(fileLocation, FileMode.Open, store))
                                {
                                    this.mediaElement.SetSource(isfs);
                                }
                            }
                        }

                        if (currentAudioMessage != null)
                        {
                            currentAudioMessage.IsStopped = false;
                            currentAudioMessage.IsPlaying = true;
                            currentAudioMessage.PlayProgressBarValue = 0;
                        }

                        CompositionTarget.Rendering -= CompositionTarget_Rendering;
                        CompositionTarget.Rendering += CompositionTarget_Rendering;

                        App.ViewModel.PauseBackgroundAudio();
                        mediaElement.Play();
                    }
                    catch (Exception ex) //Code should never reach here
                    {
                        Debug.WriteLine("NewChatTHread :: Play Audio Attachment :: Exception while playing audio file" + ex.StackTrace);
                    }
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

                    double latitude = double.Parse(locationJSON[HikeConstants.LATITUDE].ToString(Newtonsoft.Json.Formatting.None), CultureInfo.InvariantCulture);
                    double longitude = double.Parse(locationJSON[HikeConstants.LONGITUDE].ToString(Newtonsoft.Json.Formatting.None), CultureInfo.InvariantCulture);

                    PhoneApplicationService.Current.State[HikeConstants.LOCATION_MAP_COORDINATE] = new GeoCoordinate(latitude, longitude);

                    this.NavigationService.Navigate(new Uri("/View/ShowLocation.xaml", UriKind.Relative));
                }
                catch (Exception ex) //Code should never reach here
                {
                    Debug.WriteLine("NewChatTHread :: DisplayAttachment :: Exception while parsing location parameters" + ex.StackTrace);
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
            else
            {
                //default file type
                LaunchFile(HikeConstants.FILES_BYTE_LOCATION + "/" + contactNumberOrGroupId, Convert.ToString(convMessage.MessageId), convMessage.FileAttachment.FileName);
            }
        }

        async void LaunchFile(string folderpath, string filePath, string originalFileName)
        {
            try
            {
                string root = ApplicationData.Current.LocalFolder.Path;
                StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(root + @"\" + folderpath);
                StorageFolder tempfolder = await StorageFolder.GetFolderFromPathAsync(root + @"\" + HikeConstants.FILE_TRANSFER_TEMP_LOCATION);
                StorageFile isolatedstorageFile = await folder.GetFileAsync(filePath);
                StorageFile isolatedstorageFileCopy = await isolatedstorageFile.CopyAsync(tempfolder, originalFileName, NameCollisionOption.ReplaceExisting);
                bool success = await Windows.System.Launcher.LaunchFileAsync(isolatedstorageFileCopy);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception,NewChatThread::LaunchUri,message:{0},stacktrace:{1}", ex.Message, ex.StackTrace);
            }
        }

        void mediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            var element = (sender as MediaElement);

            if (element != null && element.CurrentState == MediaElementState.Playing)
            {
                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.PLAYER_TIMER))
                {
                    mediaElement.Position = (TimeSpan)PhoneApplicationService.Current.State[HikeConstants.PLAYER_TIMER];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.PLAYER_TIMER);
                }

            }
        }

        void mediaPlayback_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (currentAudioMessage != null)
            {
                currentAudioMessage.IsPlaying = false;
                currentAudioMessage.IsStopped = true;
                currentAudioMessage.PlayTimeText = currentAudioMessage.DurationText;
                currentAudioMessage.PlayProgressBarValue = 0;
                currentAudioMessage = null;
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            App.ViewModel.ResumeBackgroundAudio();
        }

        void mediaPlayback_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (currentAudioMessage != null)
            {
                currentAudioMessage.IsPlaying = false;
                currentAudioMessage.IsStopped = true;
                currentAudioMessage.PlayTimeText = currentAudioMessage.DurationText;
                currentAudioMessage.PlayProgressBarValue = 0;
                currentAudioMessage = null;
            }

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            App.ViewModel.ResumeBackgroundAudio();
        }

        private void forwardAttachmentMessage()
        {
            if (PhoneApplicationService.Current.State.ContainsKey("SharePicker"))
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

                    SendImage(bitmap, token, Attachment.AttachemntSource.FORWARDED);
                    PhoneApplicationService.Current.State.Remove("SharePicker");
                });
            }
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.CONTACT_SELECTED))
                ContactTransfer();
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED))
                TransferFile();
            if (App.IS_TOMBSTONED && PhoneApplicationService.Current.State.ContainsKey(HikeConstants.SHARED_LOCATION))
            {
                shareLocation();
            }
        }

        #endregion

        #region Emoticons

        private List<SmileyParser.Emoticon> imagePathsForListRecent
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForRecent;
            }
        }

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

        private BitmapImage[] imagePathsForList3
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForList3;
            }
        }

        List<SmileyParser.Emoticon> listTemp = new List<SmileyParser.Emoticon>();

        private void emotHeaderRectRecent_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotRecent.Source = UI_Utils.Instance.RecentIconActive;
            emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
            emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
            emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
            emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
            emoticonPivot.SelectedIndex = 0;

            if (emotListRecent.Items.Count > 6)
                emotListRecent.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            else
                emotListRecent.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
        }

        private void emotHeaderRect0_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
            emotCat0.Source = UI_Utils.Instance.EmotCat1Active;
            emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
            emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
            emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
            emoticonPivot.SelectedIndex = 1;
        }

        private void emotHeaderRect1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
            emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
            emotCat1.Source = UI_Utils.Instance.EmotCat2Active;
            emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
            emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
            emoticonPivot.SelectedIndex = 2;
        }

        private void emotHeaderRect2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
            emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
            emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
            emotCat2.Source = UI_Utils.Instance.EmotCat3Active;
            emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
            emoticonPivot.SelectedIndex = 3;
        }

        private void emotHeaderRect3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
            emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
            emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
            emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
            emotCat3.Source = UI_Utils.Instance.EmotCat4Active;
            emoticonPivot.SelectedIndex = 4;
        }

        private void emoticonPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            App.WriteToIsoStorageSettings(HikeConstants.AppSettings.LAST_SELECTED_EMOTICON_CATEGORY, emoticonPivot.SelectedIndex);

            switch (emoticonPivot.SelectedIndex)
            {
                case 0:
                    if (imagePathsForListRecent.Count > 0)
                    {
                        gridNoRecents.Visibility = Visibility.Collapsed;
                        gridShowRecents.Visibility = Visibility.Visible;
                        emotListRecent.ItemsSource = null;
                        listTemp.Clear();
                        listTemp.AddRange(imagePathsForListRecent);
                        emotListRecent.ItemsSource = listTemp;

                        if (emotListRecent.Items.Count > 6)
                            emotListRecent.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        else
                            emotListRecent.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                    }
                    else
                    {
                        gridNoRecents.Visibility = Visibility.Visible;
                        gridShowRecents.Visibility = Visibility.Collapsed;
                    }

                    emotRecent.Source = UI_Utils.Instance.RecentIconActive;
                    emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
                    emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
                    emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
                    emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
                    break;
                case 1:
                    emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
                    emotCat0.Source = UI_Utils.Instance.EmotCat1Active;
                    emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
                    emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
                    emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
                    break;
                case 2:
                    emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
                    emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
                    emotCat1.Source = UI_Utils.Instance.EmotCat2Active;
                    emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
                    emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
                    break;
                case 3:
                    emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
                    emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
                    emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
                    emotCat2.Source = UI_Utils.Instance.EmotCat3Active;
                    emotCat3.Source = UI_Utils.Instance.EmotCat4Inactive;
                    break;
                case 4:
                    emotRecent.Source = UI_Utils.Instance.RecentIconInActive;
                    emotCat0.Source = UI_Utils.Instance.EmotCat1Inactive;
                    emotCat1.Source = UI_Utils.Instance.EmotCat2Inactive;
                    emotCat2.Source = UI_Utils.Instance.EmotCat3Inactive;
                    emotCat3.Source = UI_Utils.Instance.EmotCat4Active;
                    break;
            }
        }

        #endregion

        #region Nudge

        private void MessageList_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (isGroupChat && !isGroupAlive)
                return;
            if (mUserIsBlocked)
                return;

            if (emoticonPanel.Visibility == Visibility.Visible)
                emoticonPanel.Visibility = Visibility.Collapsed;

            if ((!isOnHike && mCredits <= 0))
                return;
            ConvMessage convMessage = new ConvMessage(string.Format("{0}!", AppResources.Nudge), mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
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

        #endregion

        #region Chat Backgrounds

        async Task SendBackgroundChangedPacket(string bgId)
        {
            string msg = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
            ConvMessage cm = new ConvMessage(msg, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.UNKNOWN);
            cm.GroupParticipant = App.MSISDN;
            cm.GrpParticipantState = ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED;
            cm.MetaDataString = "{\"t\":\"cbg\"}";

            ConversationListObject cobj = MessagesTableUtils.addChatMessage(cm, false, null, App.MSISDN);
            if (cobj != null)
            {
                JObject data = new JObject();
                data[HikeConstants.BACKGROUND_ID] = bgId;
                data[HikeConstants.MESSAGE_ID] = TimeUtils.getCurrentTimeStamp().ToString();

                JObject jo = new JObject();
                jo[HikeConstants.FROM] = App.MSISDN;
                jo[HikeConstants.TO] = mContactNumber;
                jo[HikeConstants.TIMESTAMP] = TimeUtils.getCurrentTimeStamp().ToString();
                jo[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.CHAT_BACKGROUNDS;
                jo[HikeConstants.DATA] = data;

                object[] vs = new object[2];
                vs[0] = cm;
                vs[1] = cobj;
                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED, vs);

                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, jo);
            }
        }

        private void chatPaintCancel_Click(object sender, RoutedEventArgs e)
        {
            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();
            else
            {
                gcPin.EmptyPinText();
                NewPin_Close();
            }
        }

        private void CancelBackgroundChange()
        {
            chatBackgroundPopUp_Closed();

            if (App.ViewModel.SelectedBackground.ID != App.ViewModel.LastSelectedBackground.ID)
            {
                App.ViewModel.SelectedBackground = App.ViewModel.LastSelectedBackground;
                chatBackgroundList.SelectedItem = App.ViewModel.SelectedBackground;
                ChangeBackground();
            }
        }

        private void chatPaintSave_Click(object sender, RoutedEventArgs e)
        {
            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
            {
                chatBackgroundPopUp_Closed();

                if (App.ViewModel.SelectedBackground.ID != App.ViewModel.LastSelectedBackground.ID)
                {
                    ChatBackgroundHelper.Instance.UpdateChatBgMap(mContactNumber, App.ViewModel.SelectedBackground.ID);
                    SendBackgroundChangedPacket(App.ViewModel.SelectedBackground.ID);

                    App.ViewModel.LastSelectedBackground = App.ViewModel.SelectedBackground;
                }
            }
            else
                CreatePin_Click();
        }

        void chatPaint_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_tipMode == ToolTipMode.CHAT_THEMES)
                HideServerTips();

            chatBackgroundPopUp_Opened();
        }

        void chatBackgroundPopUp_Closed()
        {
            chatBackgroundPopUp.Visibility = Visibility.Collapsed;
            chatThemeHeader.Visibility = Visibility.Collapsed;
            userHeader.Visibility = Visibility.Visible;
            openChatBackgroundButton.Opacity = 1;
        }

        void chatBackgroundPopUp_Opened()
        {
            if (!isOnHike && mCredits <= 0)
                return;

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                return;

            if (mUserIsBlocked || (isGroupChat && !isGroupAlive))
                return;

            // delay to update ui for scrolling
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                chatBackgroundList.ScrollIntoView(chatBackgroundList.SelectedItem);
            });

            chatThemeHeader.Visibility = Visibility.Visible;
            chatThemeHeaderTxt.Text = AppResources.ChatThemeHeader_Txt;
            doneButton.Content = AppResources.AppBar_Done_Btn;
            userHeader.Visibility = Visibility.Collapsed;

            App.ViewModel.LastSelectedBackground = App.ViewModel.SelectedBackground;

            openChatBackgroundButton.Opacity = 0;

            chatBackgroundPopUp.Visibility = Visibility.Visible;

            if (recordGrid.Visibility == Visibility.Visible)
            {
                recordGrid.Visibility = Visibility.Collapsed;
                sendMsgTxtbox.Visibility = Visibility.Visible;
            }

            if (emoticonPanel.Visibility == Visibility.Visible)
                emoticonPanel.Visibility = Visibility.Collapsed;

            attachmentMenu.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        void createPin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (attachmentMenu.Visibility == Visibility.Visible)
                attachmentMenu.Visibility = Visibility.Collapsed;

            if (emoticonPanel.Visibility == Visibility.Visible)
                emoticonPanel.Visibility = Visibility.Collapsed;
            
            EnableDisableAppBar(false);
            NewPin_Open();
        }

        private void EnableDisableAppBar(bool enable)
        {
            appBar.IsMenuEnabled = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay ? false : enable;
            stickersIconButton.IsEnabled = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay ? false : enable;
            emoticonsIconButton.IsEnabled = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay ? false : enable;
            sendIconButton.IsEnabled = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay || sendMsgTxtbox.Text.Length <= 0 ? false : enable;
            enableSendMsgButton = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay ? false : enable;
            fileTransferIconButton.IsEnabled = (isGroupChat && !isGroupAlive) || showNoSmsLeftOverlay ? false : enable;
        }

        private void CreatePin_Click()
        {
            if (String.IsNullOrWhiteSpace(gcPin.GetNewPinMessage()))
            {
                MessageBox.Show(AppResources.Pin_Empty_Msg);
                _isNewPin = true;
                gcPin.IsShow(true, false, true);
                userHeader.Visibility = Visibility.Collapsed;
                chatThemeHeader.Visibility = Visibility.Visible;
                chatThemeHeaderTxt.Text = AppResources.PinHeader_Txt;
                doneButton.Content = AppResources.Pin_Done_Txt;
            }
            else
            {
                ConvMessage convMessage = new ConvMessage(gcPin.GetNewPinMessage(), mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                convMessage.IsSms = !isOnHike;
                SendPinMsg(convMessage);

                gcPin.EmptyPinText();

                tipControl.Visibility = Visibility.Collapsed;
                gcPin.IsShow(false, true, true);
                _isNewPin = false;

                chatThemeHeader.Visibility = Visibility.Collapsed;
                userHeader.Visibility = Visibility.Visible;

                EnableDisableAppBar(true);
            }
        }

        void NewPin_Open()
        {
            if (!isOnHike && mCredits <= 0)
                return;

            if (mUserIsBlocked || (isGroupChat && !isGroupAlive))
                return;

            userHeader.Visibility = Visibility.Collapsed;
            chatThemeHeader.Visibility = Visibility.Visible;
            chatThemeHeaderTxt.Text = AppResources.PinHeader_Txt;
            doneButton.Content = AppResources.Pin_Done_Txt;

            tipControl.Visibility = Visibility.Collapsed;
            _isNewPin = true;
            gcPin.IsShow(true, false);
        }

        void NewPin_Close()
        {
            if (_isNewPin)
            {
                if (App.ViewModel.ConvMap.ContainsKey(mContactNumber)
                    && App.ViewModel.ConvMap[mContactNumber].MetaData != null
                    && App.ViewModel.ConvMap[mContactNumber].MetaData[HikeConstants.PINID].Type != JTokenType.Null)
                {
                    gcPin.IsShow(false, true);
                    tipControl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    gcPin.IsShow(false, false);
                    tipControl.Visibility = Visibility.Visible;
                }

                _isNewPin = false;
                _isPinAlter = true;

                userHeader.Visibility = Visibility.Visible;
                chatThemeHeader.Visibility = Visibility.Collapsed;

                EnableDisableAppBar(true);
            }
        }

        BitmapImage _tileBitmap;

        public void ChangeBackground(bool isBubbleColorChanged = true)
        {
            _patternNotLoaded = false;

            if (App.ViewModel.SelectedBackground == null)
                return;

            LayoutRoot.Background = App.ViewModel.SelectedBackground.BackgroundColor;

            if ((isGroupChat && !isGroupAlive) || (!isOnHike && mCredits <= 0))
            {
                chatPaint.Opacity = 0.5;
                newPin.Opacity = 0.5;
            }

            if (App.ViewModel.SelectedBackground.IsDefault && !App.ViewModel.IsDarkMode)
            {
                progressBar.Foreground = UI_Utils.Instance.Black;
                smsCounterTxtBlk.Foreground = txtMsgCharCount.Foreground = txtMsgCount.Foreground = (SolidColorBrush)App.Current.Resources["HikeDarkGrey"];
                nudgeBorder.BorderBrush = UI_Utils.Instance.Black;
                nudgeBorder.Background = UI_Utils.Instance.Transparent;
                nudgeImage.Source = UI_Utils.Instance.BlueSentNudgeImage;
                nudgeText.Foreground = UI_Utils.Instance.Black;
            }
            else
            {
                progressBar.Foreground = smsCounterTxtBlk.Foreground = txtMsgCharCount.Foreground = txtMsgCount.Foreground = App.ViewModel.SelectedBackground.ForegroundColor;
                nudgeBorder.BorderBrush = UI_Utils.Instance.White;
                nudgeBorder.Background = App.ViewModel.SelectedBackground.HeaderBackground;
                nudgeImage.Source = UI_Utils.Instance.NudgeSent;
                nudgeText.Foreground = UI_Utils.Instance.White;
            }

            if (isBubbleColorChanged)
            {
                foreach (var msg in ocMessages)
                    msg.UpdateChatBubbles();
            }

            chatBackground.Opacity = 1;
            headerBackground.Background = UI_Utils.Instance.Transparent;

            if (App.ViewModel.SelectedBackground.IsDefault)
            {
                headerBackground.Background = App.ViewModel.SelectedBackground.HeaderBackground;
                chatBackground.Source = null;
                return;
            }

            CreateBackgroundImage();
        }

        bool _patternNotLoaded = false;
        int chatBgSize = 800, chatBgHeight = 0, chatBgWidth;

        private async void CreateBackgroundImage()
        {
            await Task.Delay(1);

            if (App.ViewModel.SelectedBackground == null)
                return;

            var bg = ChatBackgroundHelper.Instance.ChatBgCache.GetObject(App.ViewModel.SelectedBackground.ID);

            if (bg != null)
            {
                if (App.ViewModel.SelectedBackground.IsTile)
                {
                    _tileBitmap = bg;
                    GenerateTileBackground();
                }
                else
                {
                    chatBackground.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    chatBackground.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    chatBackground.Source = bg;
                }

                PostChangeBackgroundOperations();
                return;
            }

            _tileBitmap = new BitmapImage(new Uri(App.ViewModel.SelectedBackground.ImagePath, UriKind.Relative))
            {
                CreateOptions = BitmapCreateOptions.None
            };

            _tileBitmap.ImageFailed += (s, e) =>
            {
                _patternNotLoaded = true;
            };

            //handle delay creation of bitmap image
            _tileBitmap.ImageOpened += (s, e) =>
            {
                if (App.ViewModel.SelectedBackground == null)
                    return;

                if (App.ViewModel.SelectedBackground.IsTile)
                {
                    GenerateTileBackground();
                }
                else
                {
                    chatBackground.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    chatBackground.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    chatBackground.Source = _tileBitmap;
                }

                ChatBackgroundHelper.Instance.ChatBgCache.AddObject(App.ViewModel.SelectedBackground.ID, _tileBitmap);

                PostChangeBackgroundOperations();
            };
        }

        private void GenerateTileBackground()
        {
            WriteableBitmap source = new WriteableBitmap(_tileBitmap);
            var wb1 = new WriteableBitmap(chatBgSize, chatBgSize);

            //uncomment for transparent pngs
            //wb1.Render(new Canvas() { Background = UI_Utils.Instance.Transparent, Width = chatBgSize, Height = chatBgSize }, null);
            //wb1.Invalidate();

            chatBgHeight = 0;

            for (chatBgWidth = 0; chatBgWidth <= chatBgSize; )
            {
                for (chatBgHeight = 0; chatBgHeight <= chatBgSize; )
                {
                    wb1.Blit(new Rect(chatBgWidth, chatBgHeight, source.PixelWidth, source.PixelHeight), source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
                    chatBgHeight += source.PixelHeight;
                }

                chatBgWidth += source.PixelWidth;
            }

            chatBackground.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            chatBackground.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            chatBackground.Source = wb1;
        }

        private void PostChangeBackgroundOperations()
        {
            _patternNotLoaded = !App.ViewModel.SelectedBackground.IsTile || _tileBitmap.PixelWidth == 0 ? true : false;

            headerBackground.Background = UI_Utils.Instance.Black10;
        }

        private void chatBackgroundList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ListBox).SelectedItem != null)
            {
                var chatBg = (sender as ListBox).SelectedItem as ChatBackground;
                if (chatBg != null && chatBg != App.ViewModel.SelectedBackground)
                {
                    try
                    {
                        App.ViewModel.SelectedBackground = ChatBackgroundHelper.Instance.BackgroundList.Where(b => b.ID == chatBg.ID).First();
                        ChangeBackground();
                    }
                    catch
                    {
                        Debug.WriteLine("Background doesn't exist");
                    }
                }
            }
        }

        #endregion

        private void saveContactTask_Completed(object sender, TaskEventArgs e)
        {
            switch (e.TaskResult)
            {
                case TaskResult.OK:
                    ContactUtils.getContact(mContactNumber, new ContactUtils.contacts_Callback(contactSearchCompleted_Callback));
                    break;
                case TaskResult.Cancel:
                    //MessageBox.Show(AppResources.User_Cancelled_Task_Txt);
                    break;
                case TaskResult.None:
                    //MessageBox.Show(AppResources.NoInfoForTask_Txt);
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

                    ContactInfo cInfo = new ContactInfo(null, cn.DisplayName.Trim(), ph.PhoneNumber, (int)ph.Kind);
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
                            Debug.WriteLine(string.Format("Duplicate Contact !! for Phone Number {0}", cInfo.PhoneNo));
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

            Debug.WriteLine(string.Format("Total duplicate contacts : {0}", duplicates));
            Debug.WriteLine(string.Format("Total contacts with no phone number : {0}", count));

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

            if (contactInfo == null || contactInfo.Msisdn == null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    MessageBox.Show(AppResources.CONTACT_NOT_SAVED_ON_SERVER); // change string to "unable to save contact or invaldie contact"
                });
                return;
            }

            UsersTableUtils.addContact(contactInfo);
            mPubSub.publish(HikePubSub.CONTACT_ADDED, contactInfo);

            Dispatcher.BeginInvoke(() =>
            {
                userName.Text = contactInfo.Name;
                mContactName = contactInfo.Name;
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

            App.ViewModel.UpdateNameOnSaveContact(contactInfo);
        }

        #region Orientation Handling
        private void PhoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            for (int i = 0; i < ocMessages.Count; i++)
            {
                ConvMessage convMessage = ocMessages[i];
                if ((convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO
                    || convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION
                    || convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.MESSAGE_STATUS)
                    && !convMessage.HasAttachment)
                    convMessage.CurrentOrientation = e.Orientation;
            }

            //handled textbox hight to accomodate other data on screen in diff orientations
            if (e.Orientation == PageOrientation.Portrait
                || e.Orientation == PageOrientation.PortraitUp
                || e.Orientation == PageOrientation.PortraitDown)
            {
                sendMsgTxtbox.MaxHeight = 130;
            }
            else if (e.Orientation == PageOrientation.Landscape
                || e.Orientation == PageOrientation.LandscapeLeft
                || e.Orientation == PageOrientation.LandscapeRight)
            {
                sendMsgTxtbox.MaxHeight = 72;
            }
        }

        #endregion

        #region Jump To Latest

        ScrollBar vScrollBar = null;
        private void vScrollBar1_ValueChanged(Object sender, EventArgs e)
        {
            vScrollBar = sender as ScrollBar;
            if (vScrollBar != null && llsMessages.ManipulationState != ManipulationState.Idle)
            {
                if ((vScrollBar.Maximum - vScrollBar.Value) < 200)
                {
                    JumpToBottomGrid.Visibility = Visibility.Collapsed;
                    _unreadMessageCounter = 0;
                }
                else if ((vScrollBar.Maximum - vScrollBar.Value) > 500 && JumpToBottomGrid.Visibility == Visibility.Collapsed)
                {
                    ShowJumpToBottom(false);
                }
            }
        }

        bool _userTappedJumpToBottom = false;
        private void JumpToBottom_Tapped(object sender, RoutedEventArgs e)
        {
            _userTappedJumpToBottom = true;
            ScrollToBottom();
        }

        private void llsMessages_ItemUnRealized(object sender, ItemRealizationEventArgs e)
        {
            if (isMessageLoaded && llsMessages.ItemsSource != null && llsMessages.ItemsSource.Count > 0)
            {
                if (e.ItemKind == LongListSelectorItemKind.Item)
                {
                    ConvMessage convMessage = e.Container.Content as ConvMessage;
                    if (convMessage.Equals(llsMessages.ItemsSource[llsMessages.ItemsSource.Count - 1]))
                    {
                        ShowJumpToBottom(false);
                    }
                }
            }
        }

        private void ShowJumpToBottom(bool increaseUnreadCounter)
        {
            if (_isHikeBot)
                return;
            if (vScrollBar != null && (ocMessages != null && ocMessages.Count > 6) && vScrollBar.Maximum < 1000000)
            {
                //show jump to bottom for mute chat for incoming messages as scroll to bottom wont work for mute gcs
                if ((vScrollBar.Maximum - vScrollBar.Value) > 500 || (IsMute && increaseUnreadCounter))
                {
                    if (increaseUnreadCounter)
                        _unreadMessageCounter += 1;
                    JumpToBottomGrid.Visibility = Visibility.Visible;
                    txtJumpToBttom.Text = _unreadMessageCounter > 0 ? (_unreadMessageCounter == 1 ? AppResources.ChatThread_1NewMessage_txt : string.Format(AppResources.ChatThread_More_NewMessages_txt, _unreadMessageCounter)) : AppResources.ChatThread_JumpToLatest;
                }
                else if (increaseUnreadCounter)
                    ScrollToBottom();
            }
        }

        #endregion

        #region Stickers

        bool isStickersLoaded = false;
        private string _selectedCategory = string.Empty;
        List<StickerCategory> listStickerCategories;

        /// <summary>
        /// get low res sticker from pallette
        /// </summary>
        /// <param name="sticker">low res sticker from pallette</param>
        public void SendSticker(StickerObj sticker)
        {
            ConvMessage conv = new ConvMessage(AppResources.Sticker_Txt, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            conv.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
            conv.StickerObj = new StickerObj(sticker.Category, sticker.Id, null, true);
            conv.MetaDataString = string.Format("{{{0}:'{1}',{2}:'{3}'}}", HikeConstants.STICKER_ID, sticker.Id, HikeConstants.CATEGORY_ID, sticker.Category);
            AddNewMessageToUI(conv, false);
            HikeViewModel.StickerHelper.RecentStickerHelper.AddSticker(sticker);

            mPubSub.publish(HikePubSub.MESSAGE_SENT, conv);
        }

        private void GetHighResStickerForUi(ConvMessage convMessage)
        {
            if (convMessage.StickerObj == null)
                return;
            convMessage.ImageDownloadFailed = false;//to show loading sticker
            string categoryStickerId = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
            BitmapImage image = lruStickerCache.GetObject(categoryStickerId);
            if (image != null)
                convMessage.StickerObj.IsStickerDownloaded = true;
            else
            {
                image = StickerHelper.GetHighResolutionSticker(convMessage.StickerObj);
                if (image == null)
                {
                    List<ConvMessage> listDownloading;
                    if (dictDownloadingSticker.TryGetValue(categoryStickerId, out listDownloading))
                    {
                        listDownloading.Add(convMessage);
                    }
                    else
                    {
                        listDownloading = new List<ConvMessage>();
                        listDownloading.Add(convMessage);
                        dictDownloadingSticker.Add(categoryStickerId, listDownloading);
                        AccountUtils.GetSingleSticker(convMessage, ResolutionId, new AccountUtils.parametrisedPostResponseFunction(StickersRequestCallBack));
                    }
                }
                else
                {
                    lruStickerCache.AddObject(categoryStickerId, image);
                    convMessage.StickerObj.IsStickerDownloaded = true;
                }
            }
        }

        private void PivotStickers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStickerPivot();
        }

        private void UpdateStickerPivot()
        {
            if (listStickerCategories.Count > pivotStickers.SelectedIndex)
            {
                lbStickerCategories.SelectedItem = listStickerCategories[pivotStickers.SelectedIndex];
                //using dispatcher so that it should create a delay for ui update
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    lbStickerCategories.ScrollIntoView(lbStickerCategories.SelectedItem);
                });
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.LAST_SELECTED_STICKER_CATEGORY, listStickerCategories[pivotStickers.SelectedIndex].Category);
            }
        }

        private void StickerCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lbStickerCategory = sender as ListBox;
            if (lbStickerCategory.SelectedItem == null)
                return;

            StickerCategory stickerCategory = lbStickerCategory.SelectedItem as StickerCategory;
            StickerCategoryTapped(stickerCategory);
        }

        private void StickerCategoryTapped(string selectedCategory)
        {
            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(selectedCategory);
            if (stickerCategory != null)
                StickerCategoryTapped(stickerCategory);
        }

        private void StickerCategoryTapped(StickerCategory stickerCategory)
        {
            if (_selectedCategory == stickerCategory.Category)
                return;
            _selectedCategory = stickerCategory.Category;

            StickerPivotItem stickerPivot;

            // Check if sticker category doesn't exist, show humanoid (default) category.
            if (StickerPivotHelper.Instance.dictStickersPivot.ContainsKey(stickerCategory.Category))
                stickerPivot = StickerPivotHelper.Instance.dictStickersPivot[stickerCategory.Category];
            else
                stickerPivot = StickerPivotHelper.Instance.dictStickersPivot[StickerHelper.CATEGORY_HUMANOID];

            // So that after reopening of ct , if pivot index are same we need to update pivot selection explicitly.
            if (pivotStickers.SelectedIndex == stickerPivot.PivotItemIndex)
                UpdateStickerPivot();
            else
                pivotStickers.SelectedIndex = stickerPivot.PivotItemIndex;

            foreach (StickerCategory stCategory in listStickerCategories)
            {
                stCategory.IsSelected = stCategory.Category == _selectedCategory;
            }

            foreach (KeyValuePair<string, StickerPivotItem> kvp in StickerPivotHelper.Instance.dictStickersPivot)
            {
                if (kvp.Key != _selectedCategory)
                {
                    kvp.Value.SetLlsSource(null);
                }
            }
            if (_selectedCategory == StickerHelper.CATEGORY_RECENT)
            {
                stickerPivot.SetLlsSourceList(HikeViewModel.StickerHelper.RecentStickerHelper.RecentStickers);
                if (HikeViewModel.StickerHelper.RecentStickerHelper.RecentStickers.Count == 0)
                    stickerPivot.ShowNoStickers();
                else
                    stickerPivot.ShowStickers();

                return;
            }
            else
                stickerPivot.SetLlsSource(stickerCategory.ListStickers);

            if (stickerCategory.ShowDownloadMessage)
            {
                ShowDownloadOverlay(true);
                if (stickerCategory.ListStickers.Count == 0)
                    stickerPivot.ShowNoStickers();
                else
                    stickerPivot.ShowStickers();
                return;
            }
            if (stickerCategory == null)
            {
                stickerPivot.ShowNoStickers();
            }
            else if (stickerCategory.ListStickers.Count < 10 && stickerCategory.HasMoreStickers)
            {
                downloadStickers_Tap(null, null);
            }
            else if (stickerCategory.ListStickers.Count == 0)
            {
                stickerPivot.ShowNoStickers();
            }
            else
            {
                stickerPivot.ShowStickers();
            }
        }

        public void PostRequestForBatchStickers(StickerCategory stickerCategory)
        {
            JObject json = new JObject();
            json["catId"] = stickerCategory.Category;
            if (!stickerCategory.IsDownLoading && stickerCategory.HasMoreStickers)
            {
                List<string> listStickerIds = new List<string>();
                JArray existingIds = new JArray();
                foreach (StickerObj sticker in stickerCategory.ListStickers)
                {
                    existingIds.Add(sticker.Id);
                }
                json["stIds"] = existingIds;
                json["resId"] = ResolutionId;
                json["nos"] = 10;
                stickerCategory.IsDownLoading = true;
                AccountUtils.GetStickers(json, new AccountUtils.parametrisedPostResponseFunction(StickersRequestCallBack), stickerCategory);
            }
        }

        private void StickersRequestCallBack(JObject json, Object obj)
        {
            StickerCategory stickerCategory = null;//to show batch sticker request
            ConvMessage convMessage = null;//to show single sticker request
            if (obj == null)
                return;
            if (obj is StickerCategory)
                stickerCategory = obj as StickerCategory;
            else if (obj is ConvMessage)
                convMessage = obj as ConvMessage;
            else
                return;
            if ((json == null) || HikeConstants.FAIL == (string)json[HikeConstants.STAT])
            {
                if (stickerCategory != null)
                {
                    stickerCategory.IsDownLoading = false;
                    StickerPivotItem stickerPivot;
                    if (StickerPivotHelper.Instance.dictStickersPivot.TryGetValue(stickerCategory.Category, out stickerPivot))
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            if (stickerCategory.HasNewStickers || (stickerCategory.Category != StickerHelper.CATEGORY_EXPRESSIONS && stickerCategory.Category != StickerHelper.CATEGORY_HUMANOID))
                                stickerPivot.ShowDownloadFailed();
                            stickerPivot.ShowHidMoreProgreesBar(false);
                        });
                    }
                }
                if (convMessage != null)
                {
                    convMessage.ImageDownloadFailed = true;
                    string key = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
                    List<ConvMessage> listDownlaoding;
                    if (dictDownloadingSticker.TryGetValue(key, out listDownlaoding))
                    {
                        foreach (ConvMessage conv in listDownlaoding)
                        {
                            conv.ImageDownloadFailed = true;
                        }
                        dictDownloadingSticker.Remove(key);
                    }
                }

                return;
            }

            try
            {
                string category = (string)json["catId"];
                JObject stickers = (JObject)json["data"];
                bool hasMoreStickers = true;
                if (json["st"] != null)
                {
                    hasMoreStickers = false;
                }
                if (stickerCategory == null)
                {
                    stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);
                    if (stickerCategory == null)
                    {
                        stickerCategory = new StickerCategory(category);
                    }
                }
                IEnumerator<KeyValuePair<string, JToken>> keyVals = stickers.GetEnumerator();
                List<KeyValuePair<string, byte[]>> listHighResStickersBytes = new List<KeyValuePair<string, byte[]>>();
                bool isDisabled = false;
                while (keyVals.MoveNext())
                {
                    try
                    {
                        KeyValuePair<string, JToken> kv = keyVals.Current;
                        string id = (string)kv.Key;
                        if (id == "disabled")
                        {
                            isDisabled = (bool)stickers[kv.Key];
                            continue;
                        }
                        string iconBase64 = stickers[kv.Key].ToString();
                        byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                        listHighResStickersBytes.Add(new KeyValuePair<string, byte[]>(id, imageBytes));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NewChatThread : callBack : Exception : " + ex.Message);
                    }
                }
                stickerCategory.WriteHighResToFile(listHighResStickersBytes);


                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    List<KeyValuePair<string, byte[]>> listLowResStickersBytes = new List<KeyValuePair<string, byte[]>>();
                    listHighResStickersBytes = listHighResStickersBytes.OrderBy(x => x.Key).ToList();
                    foreach (KeyValuePair<string, Byte[]> keyValuePair in listHighResStickersBytes)
                    {
                        string stickerId = keyValuePair.Key;
                        BitmapImage highResImage = UI_Utils.Instance.createImageFromBytes(keyValuePair.Value);
                        if (highResImage == null)
                            continue;

                        if (convMessage != null)
                        {
                            string key = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
                            List<ConvMessage> listDownlaoding;
                            if (dictDownloadingSticker.TryGetValue(key, out listDownlaoding))
                            {
                                foreach (ConvMessage conv in listDownlaoding)
                                {
                                    conv.ImageDownloadFailed = false;
                                    conv.StickerObj.IsStickerDownloaded = true;
                                    conv.StickerObj.StickerImageBytes = keyValuePair.Value;
                                }
                                dictDownloadingSticker.Remove(key);
                            }
                            lruStickerCache.AddObject(key, highResImage);
                        }
                        if (!isDisabled)
                        {
                            Byte[] lowResImageBytes = UI_Utils.Instance.PngImgToJpegByteArray(highResImage);
                            listLowResStickersBytes.Add(new KeyValuePair<string, byte[]>(stickerId, lowResImageBytes));
                            stickerCategory.ListStickers.Add(new StickerObj(category, stickerId, lowResImageBytes, false));
                        }
                    }
                    if (convMessage != null)
                    {
                        if (!isDisabled)
                        {
                            stickerCategory.HasNewStickers = true;
                        }
                    }
                    else
                    {
                        stickerCategory.HasNewStickers = false;
                    }
                    if (!isDisabled)
                        stickerCategory.WriteLowResToFile(listLowResStickersBytes, hasMoreStickers);

                    if (stickerCategory != null)
                    {
                        //stLoading.Visibility = Visibility.Collapsed;
                        StickerPivotItem stickerPivot;
                        if (StickerPivotHelper.Instance.dictStickersPivot.TryGetValue(category, out stickerPivot))
                        {
                            if (category == _selectedCategory)
                                stickerPivot.ShowStickers();
                            stickerPivot.ShowHidMoreProgreesBar(false);
                        }
                        pivotStickers.Visibility = Visibility.Visible;
                    }
                    stickerCategory.IsDownLoading = false;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread::StickersRequestCallBack , Exception:" + ex.Message);
                if (stickerCategory != null)
                {
                    stickerCategory.IsDownLoading = false;
                    StickerPivotItem stickerPivot;
                    if (StickerPivotHelper.Instance.dictStickersPivot.TryGetValue(stickerCategory.Category, out stickerPivot))
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            stickerPivot.ShowHidMoreProgreesBar(false);
                        });
                    }
                }
                if (convMessage != null)
                {
                    convMessage.ImageDownloadFailed = true;
                    string key = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
                    List<ConvMessage> listDownlaoding;
                    if (dictDownloadingSticker.TryGetValue(key, out listDownlaoding))
                    {
                        foreach (ConvMessage conv in listDownlaoding)
                        {
                            conv.ImageDownloadFailed = true;
                        }
                        dictDownloadingSticker.Remove(key);
                    }
                }
            }
        }

        public void ShowDownloadOverlay(bool show)
        {
            EnableDisableUI(!show);
            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory == null)
                return;
            gridDownloadStickers.DataContext = stickerCategory;

            if (show)
            {
                if (_selectedCategory == StickerHelper.CATEGORY_HUMANOID || _selectedCategory == StickerHelper.CATEGORY_EXPRESSIONS)
                {
                    btnDownload.Content = AppResources.Installed_Txt;
                    btnDownload.IsHitTestVisible = false;
                    btnFree.IsHitTestVisible = false;
                }
                overlayBorder.Tap += overlayBorder_Tapped;
                overlayBorder.Visibility = Visibility.Visible;
                gridDownloadStickers.Visibility = Visibility.Visible;
            }
            else
            {
                overlayBorder.Tap -= overlayBorder_Tapped;
                if (btnDownload.IsHitTestVisible == false)
                {
                    btnDownload.IsHitTestVisible = true;
                    btnFree.IsHitTestVisible = true;
                    btnDownload.Content = AppResources.Download_txt;
                    if (stickerCategory.ShowDownloadMessage)
                        stickerCategory.SetDownloadMessage(false);
                }
                overlayBorder.Visibility = Visibility.Collapsed;
                gridDownloadStickers.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handle sticker overlay tapped event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void overlayBorder_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowDownloadOverlay(false);
            StickerCategory s2 = HikeViewModel.StickerHelper.GetStickersByCategory(_selectedCategory);
            if (s2 == null || s2.ListStickers.Count == 0)
            {
                if (StickerPivotHelper.Instance.dictStickersPivot.ContainsKey(_selectedCategory))
                    StickerPivotHelper.Instance.dictStickersPivot[_selectedCategory].ShowNoStickers();
            }
            else
            {
                if (StickerPivotHelper.Instance.dictStickersPivot.ContainsKey(s2.Category))
                    StickerPivotHelper.Instance.dictStickersPivot[s2.Category].ShowStickers();
            }
        }

        private void downloadStickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowDownloadOverlay(false);
            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory.ShowDownloadMessage)
                stickerCategory.SetDownloadMessage(false);
            if (StickerPivotHelper.Instance.dictStickersPivot.ContainsKey(stickerCategory.Category))
            {
                if (stickerCategory.ListStickers.Count > 0)
                {
                    StickerPivotHelper.Instance.dictStickersPivot[stickerCategory.Category].ShowHidMoreProgreesBar(true);
                    StickerPivotHelper.Instance.dictStickersPivot[stickerCategory.Category].ShowStickers();
                }
                else
                    StickerPivotHelper.Instance.dictStickersPivot[stickerCategory.Category].ShowLoadingStickers();
            }
            PostRequestForBatchStickers(stickerCategory);
        }

        private void CreateStickerPivot()
        {
            pivotStickers = StickerPivotHelper.Instance.InitialiseStickerPivot();
            if (!stickerPallet.Children.Contains(pivotStickers))
            {
                pivotStickers.SelectionChanged += PivotStickers_SelectionChanged;
                pivotStickers.MaxHeight = 265;
                stickerPallet.Children.Add(pivotStickers);
            }
        }

        private void CreateStickerCategories()
        {
            if (listStickerCategories == null)
            {
                listStickerCategories = new List<StickerCategory>();
                StickerCategory stickerCategory;
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_RECENT)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_HUMANOID)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_EXPRESSIONS)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_LOVE)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_BOLLYWOOD)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_INDIANS)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_DOGGY)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_TROLL)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_JELLY)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_SPORTS)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_HUMANOID2)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_AVATARS)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_SMILEY_EXPRESSIONS)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_KITTY)) != null)
                {
                    listStickerCategories.Add(stickerCategory);
                }
                lbStickerCategories.ItemsSource = listStickerCategories;
            }
        }

        #endregion

        #region Walkie Talkie

        private void ActionIcon_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(sendMsgTxtbox.Text))
                ShowWalkieTalkie();
            else
                SendMsg();
        }

        private void ShowWalkieTalkie()
        {
            if ((!isOnHike && mCredits <= 0) || (isGroupChat && !isGroupAlive))
                return;

            if (_microphone == null)
            {
                MessageBox.Show(AppResources.MicrophoneNotFound_ErrorTxt, AppResources.MicrophoneNotFound_HeaderTxt, MessageBoxButton.OK);
                return;
            }

            if (attachmentMenu.Visibility == Visibility.Visible)
                attachmentMenu.Visibility = Visibility.Collapsed;

            if (emoticonPanel.Visibility == Visibility.Visible)
                emoticonPanel.Visibility = Visibility.Collapsed;

            if (chatBackgroundPopUp.Visibility == Visibility.Visible)
                CancelBackgroundChange();

            sendIconButton.IsEnabled = false;

            this.Focus(); // remove focus from textbox
            recordGrid.Visibility = Visibility.Visible;
            sendMsgTxtbox.Visibility = Visibility.Collapsed;

            recordButtonGrid.Background = gridBackgroundBeforeRecording;
            recordButton.Text = HOLD_AND_TALK;
            recordButton.Opacity = 0.5;
            walkieTalkieImage.Opacity = 0.5;
        }

        void Hold_To_Record(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            if (mediaElement != null)
            {
                if (currentAudioMessage != null && currentAudioMessage.IsPlaying)
                {
                    currentAudioMessage.IsPlaying = false;
                    mediaElement.Pause();
                    App.ViewModel.ResumeBackgroundAudio();
                }
            }

            try
            {
                //disable lock screen
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }
            catch { }

            WalkieTalkieGrid.Visibility = Visibility.Visible;
            recordButton.Text = RELEASE_TO_SEND;
            cancelRecord.Opacity = 0;
            recordButton.Opacity = 1;
            recordButtonGrid.Background = UI_Utils.Instance.HikeBlue;
            walkieTalkieImage.Opacity = 1;
            recordWalkieTalkieMessage();
        }

        void recordButton_ManipulationCompleted(object sender, System.Windows.Input.ManipulationCompletedEventArgs e)
        {
            if (isRecordingForceStop)
            {
                isRecordingForceStop = false;
                return;
            }

            try
            {
                //re-enable lock screen
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Enabled;
            }
            catch { }

            deleteRecImage.Opacity = 0.5;
            deleteRecText.Opacity = 0.5;
            cancelRecord.Opacity = 1;
            deleteBorder.BorderBrush = UI_Utils.Instance.Transparent;
            WalkieTalkieGrid.Visibility = Visibility.Collapsed;
            recordButton.Text = HOLD_AND_TALK;
            recordButton.Opacity = 0.5;
            recordButtonGrid.Background = gridBackgroundBeforeRecording;

            walkieTalkieImage.Opacity = 0.5;

            stopWalkieTalkieRecording();

            if (_isWalkieTalkieMessgeDelete)
            {
                _isWalkieTalkieMessgeDelete = false;
                _recorderState = RecorderState.NOTHING_RECORDED;

                if (_deleteTimer == null)
                {
                    _deleteTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
                    _deleteTimer.Tick += _deleteTimer_Tick;
                }

                deleteRecTextSuc.Text = MESSAGE_CANCELLED;
                deleteRecImageSuc.Visibility = Visibility.Visible;
                _deleteTimer.Start();
                WalkieTalkieDeleteGrid.Visibility = Visibility.Visible;

                return;
            }

            if (_recordedDuration > 0)
            {
                _recorderState = RecorderState.RECORDED;
                sendWalkieTalkieMessage();
            }
            else
            {
                _recorderState = RecorderState.NOTHING_RECORDED;
                if (_deleteTimer == null)
                {
                    _deleteTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 1) };
                    _deleteTimer.Tick += _deleteTimer_Tick;
                }

                deleteRecTextSuc.Text = MESSAGE_TOO_SHORT;
                deleteRecImageSuc.Visibility = Visibility.Collapsed;
                _deleteTimer.Start();
                WalkieTalkieDeleteGrid.Visibility = Visibility.Visible;
            }
        }

        private void deleteRecImage_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isWalkieTalkieMessgeDelete = false;
            deleteBorder.BorderBrush = UI_Utils.Instance.Transparent;
            deleteRecImage.Opacity = 0.5;
            deleteRecText.Opacity = 0.5;
        }

        void deleteRecImage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isWalkieTalkieMessgeDelete = true;
            deleteBorder.BorderBrush = UI_Utils.Instance.Red;
            deleteRecImage.Opacity = 1;
            deleteRecText.Opacity = 1;
        }

        void cancelRecord_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            sendMsgTxtbox.Visibility = Visibility.Visible;
            recordGrid.Visibility = Visibility.Collapsed;

            if (this.ApplicationBar != null && !sendMsgTxtbox.Text.Trim().Equals(String.Empty))
                (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
        }

        void microphone_BufferReady(object sender, EventArgs e)
        {
            // Retrieve audio data
            _microphone.GetData(_buffer);
            _stream.Write(_buffer, 0, _buffer.Length);
        }

        void dt_Tick(object sender, EventArgs e)
        {
            try
            {
                FrameworkDispatcher.Update();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread.xaml :: dt_Tick, update, Exception : " + ex.StackTrace);
            }
        }

        private void recordWalkieTalkieMessage()
        {
            isRecordingForceStop = false;
            runningTime.Text = "00:00";
            _progressTimer.Start();

            // Get audio data in 1/2 second chunks
            _microphone.BufferDuration = TimeSpan.FromMilliseconds(1000);
            // Allocate memory to hold the audio data
            _buffer = new byte[_microphone.GetSampleSizeInBytes(_microphone.BufferDuration)];
            // Set the stream back to zero in case there is already something in it
            _stream.SetLength(0);

            WriteWavHeader(_stream, _microphone.SampleRate);

            // Start recording
            _microphone.Start();
            timeBar.Opacity = 1;
            maxPlayingTime.Text = " / " + Utils.GetFormattedTimeFromSeconds(HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED);
            sendIconButton.IsEnabled = false;
            _recorderState = RecorderState.RECORDING;
        }

        void showWalkieTalkieProgress(object sender, EventArgs e)
        {
            runningTime.Text = Utils.GetFormattedTimeFromSeconds(_runningSeconds + 1);

            if (_runningSeconds >= HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED)
            {
                cancelRecord.Opacity = 1;
                deleteBorder.BorderBrush = UI_Utils.Instance.Transparent;
                WalkieTalkieGrid.Visibility = Visibility.Collapsed;
                recordButton.Text = HOLD_AND_TALK;
                recordButton.Opacity = 0.5;
                recordButtonGrid.Background = gridBackgroundBeforeRecording;
                walkieTalkieImage.Opacity = 0.5;
                deleteRecImage.Opacity = 0.5;
                deleteRecText.Opacity = 0.5;

                stopWalkieTalkieRecording();
                sendWalkieTalkieMessage();

                isRecordingForceStop = true;
            }

            _runningSeconds++;
        }

        void _deleteTimer_Tick(object sender, EventArgs e)
        {
            _deleteTimer.Stop();
            WalkieTalkieDeleteGrid.Visibility = Visibility.Collapsed;
        }

        private void stopWalkieTalkieRecording()
        {
            _progressTimer.Stop();

            if (_microphone.State == MicrophoneState.Started)
            {
                // In RECORD mode, user clicked the 
                // stop button to end recording
                _microphone.Stop();
                UpdateWavHeader(_stream);
            }

            timeBar.Opacity = 0;

            if (_recorderState == RecorderState.RECORDING)
            {
                _recordedDuration = _runningSeconds;
                runningTime.Text = Utils.GetFormattedTimeFromSeconds(0);
            }

            _runningSeconds = 0;
            _recorderState = RecorderState.RECORDED;
        }

        void sendWalkieTalkieMessage()
        {
            if (_stream != null)
            {
                byte[] audioBytes = _stream.ToArray();
                if (audioBytes != null && audioBytes.Length > 0)
                {
                    PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED] = _stream.ToArray();
                    TransferFile();
                }
            }
        }

        public void WriteWavHeader(Stream stream, int sampleRate)
        {
            const int bitsPerSample = 16;
            const int bytesPerSample = bitsPerSample / 8;
            var encoding = System.Text.Encoding.UTF8;
            // ChunkID Contains the letters "RIFF" in ASCII form (0x52494646 big-endian form).
            stream.Write(encoding.GetBytes("RIFF"), 0, 4);

            // NOTE this will be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);

            // Format Contains the letters "WAVE"(0x57415645 big-endian form).
            stream.Write(encoding.GetBytes("WAVE"), 0, 4);

            // Subchunk1ID Contains the letters "fmt " (0x666d7420 big-endian form).
            stream.Write(encoding.GetBytes("fmt "), 0, 4);

            // Subchunk1Size 16 for PCM.  This is the size of therest of the Subchunk which follows this number.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // AudioFormat PCM = 1 (i.e. Linear quantization) Values other than 1 indicate some form of compression.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // NumChannels Mono = 1, Stereo = 2, etc.
            stream.Write(BitConverter.GetBytes((short)1), 0, 2);

            // SampleRate 8000, 44100, etc.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // ByteRate =  SampleRate * NumChannels * BitsPerSample/8
            stream.Write(BitConverter.GetBytes(sampleRate * bytesPerSample), 0, 4);

            // BlockAlign NumChannels * BitsPerSample/8 The number of bytes for one sample including all channels.
            stream.Write(BitConverter.GetBytes((short)(bytesPerSample)), 0, 2);

            // BitsPerSample    8 bits = 8, 16 bits = 16, etc.
            stream.Write(BitConverter.GetBytes((short)(bitsPerSample)), 0, 2);

            // Subchunk2ID Contains the letters "data" (0x64617461 big-endian form).
            stream.Write(encoding.GetBytes("data"), 0, 4);

            // NOTE to be filled in later
            stream.Write(BitConverter.GetBytes(0), 0, 4);
        }

        public void UpdateWavHeader(Stream stream)
        {
            if (!stream.CanSeek) throw new Exception("Can't seek stream to update wav header");

            var oldPos = stream.Position;

            // ChunkSize  36 + SubChunk2Size
            stream.Seek(4, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 8), 0, 4);

            // Subchunk2Size == NumSamples * NumChannels * BitsPerSample/8 This is the number of bytes in the data.
            stream.Seek(40, SeekOrigin.Begin);
            stream.Write(BitConverter.GetBytes((int)stream.Length - 44), 0, 4);

            stream.Seek(oldPos, SeekOrigin.Begin);
        }

        private readonly SolidColorBrush gridBackgroundBeforeRecording = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xf2, 0x43, 0x4b, 0x5c));
        private Microphone _microphone = Microphone.Default;     // Object representing the physical microphone on the device
        private byte[] _buffer;                                  // Dynamic buffer to retrieve audio data from the microphone
        private MemoryStream _stream = new MemoryStream();       // Stores the audio data for later playback
        private bool isRecordingForceStop = false;
        private DispatcherTimer _progressTimer;
        private DispatcherTimer _deleteTimer;
        private int _runningSeconds = 0;

        Boolean _isWalkieTalkieMessgeDelete = false;

        private int _recordedDuration = -1;

        private DispatcherTimer _dt;

        private enum RecorderState
        {
            NOTHING_RECORDED = 0,
            RECORDED,
            RECORDING,
            PLAYING,
        }

        private RecorderState _recorderState = RecorderState.NOTHING_RECORDED;

        #endregion

        #region H2HOffline

        void StartForceSMSTimer(bool isNewTimer)
        {
            if (!isOnHike || !IsSMSOptionValid || _isSendAllAsSMSVisible || mUserIsBlocked || isGroupChat)
                return;

            if (ocMessages == null)
                return;

            try
            {
                var msgList = (from message in ocMessages
                               where message.MessageStatus == ConvMessage.State.SENT_CONFIRMED
                               select message);

                _lastUnDeliveredMessage = msgList != null && msgList.Count() > 0 ? msgList.Last() : null;

                if (_lastUnDeliveredMessage != null)
                {
                    TimeSpan ts;

                    if (isNewTimer)
                        ts = TimeSpan.FromSeconds(20);
                    else
                    {
                        long ticks = _lastUnDeliveredMessage.Timestamp * 10000000;
                        ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
                        DateTime receivedTime = new DateTime(ticks);
                        receivedTime = receivedTime.ToLocalTime();
                        ts = DateTime.Now.Subtract(receivedTime);
                        ts = ts.TotalSeconds > 20 ? TimeSpan.FromMilliseconds(10) : ts.TotalSeconds > 0 ? ts : TimeSpan.FromMilliseconds(10);
                    }

                    if (ts.TotalSeconds > 0 || isNewTimer)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action<TimeSpan>(delegate(TimeSpan timeSpan)
                        {
                            if (_h2hOfflineTimer == null)
                            {
                                _h2hOfflineTimer = new DispatcherTimer();
                                _h2hOfflineTimer.Tick -= h2hofflineTimer_Tick;
                                _h2hOfflineTimer.Tick += h2hofflineTimer_Tick;
                            }

                            _h2hOfflineTimer.Stop();
                            _h2hOfflineTimer.Interval = timeSpan;
                            _h2hOfflineTimer.Start();
                        }), ts);
                    }
                    else
                        ShowForceSMSOnUI();
                }
            }
            catch
            {
                return;
            }
        }

        void h2hofflineTimer_Tick(object sender, EventArgs e)
        {
            _h2hOfflineTimer.Stop();
            ShowForceSMSOnUI();
        }

        void ShowForceSMSOnUI()
        {
            if (!isOnHike || !IsSMSOptionValid || _isSendAllAsSMSVisible || mUserIsBlocked || isGroupChat)
                return;

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (lastSeenTxt.Text == AppResources.Online || _isSendAllAsSMSVisible)
                    return;

                if (ocMessages == null)
                    return;

                _lastUnDeliveredMessage = null;

                try
                {
                    var msgList = (from message in ocMessages
                                   where message.MessageStatus == ConvMessage.State.SENT_CONFIRMED
                                   select message);

                    _lastUnDeliveredMessage = msgList != null && msgList.Count() > 0 ? msgList.Last() : null;
                }
                catch
                {
                    return;
                }

                if (_lastUnDeliveredMessage != null)
                {
                    var indexToInsert = ocMessages.IndexOf(_lastUnDeliveredMessage) + 1;

                    if (_readByMessage != null && ocMessages.Contains(_readByMessage) && ocMessages.IndexOf(_readByMessage) == indexToInsert)
                        ocMessages.Remove(_readByMessage);


                    if (_tap2SendAsSMSMessage == null)
                    {
                        _tap2SendAsSMSMessage = new ConvMessage();
                        _tap2SendAsSMSMessage.CurrentOrientation = this.Orientation;
                        _tap2SendAsSMSMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION;
                        _tap2SendAsSMSMessage.NotificationType = ConvMessage.MessageType.FORCE_SMS;

                        if (isGroupChat)
                            _tap2SendAsSMSMessage.Message = AppResources.Send_All_As_SMS_Group;
                        else
                            _tap2SendAsSMSMessage.Message = String.Format(AppResources.Send_All_As_SMS, mContactName);
                    }

                    this.ocMessages.Insert(indexToInsert, _tap2SendAsSMSMessage);

                    if (indexToInsert == ocMessages.Count - 1)
                        ScrollToBottom();

                    _isSendAllAsSMSVisible = true;
                }
            });
        }

        void SendForceSMS(ConvMessage message = null)
        {
            if (message != null) //single sms
            {
                JArray messageArr = new JArray();
                JObject fmsg = new JObject();
                fmsg.Add(HikeConstants.HIKE_MESSAGE, message.GetMessageForServer());
                fmsg.Add(HikeConstants.MESSAGE_ID, message.MessageId);
                messageArr.Add(fmsg);

                JObject data = new JObject();
                data.Add(HikeConstants.COUNT, 1);
                data.Add(HikeConstants.MESSAGE_ID, message.MessageId);
                data.Add(HikeConstants.FORCE_SMS_MESSAGE, messageArr);

                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.FORCE_SMS);
                obj.Add(HikeConstants.TO, message.Msisdn);
                obj.Add(HikeConstants.DATA, data);

                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);
            }
            else
            {
                if (ocMessages == null || ocMessages.Count == 0)
                    return;

                var convMsgList = (from convMsg in ocMessages
                                   where convMsg.MessageStatus == ConvMessage.State.SENT_CONFIRMED
                                   select convMsg).ToList();

                if (convMsgList.Count == 0)
                    return;

                JArray messageArr = new JArray();
                JObject fmsg;

                foreach (var msg in convMsgList)
                {
                    fmsg = new JObject();
                    fmsg.Add(HikeConstants.HIKE_MESSAGE, msg.GetMessageForServer());
                    fmsg.Add(HikeConstants.MESSAGE_ID, msg.MessageId);
                    messageArr.Add(fmsg);

                    msg.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_CONFIRMED;
                    string msisdn = MessagesTableUtils.updateMsgStatus(mContactNumber, msg.MessageId, (int)ConvMessage.State.FORCE_SMS_SENT_CONFIRMED);
                }

                message = convMsgList.Last();

                JObject data = new JObject();
                data.Add(HikeConstants.COUNT, convMsgList.Count);
                data.Add(HikeConstants.MESSAGE_ID, message.MessageId);
                data.Add(HikeConstants.FORCE_SMS_MESSAGE, messageArr);

                JObject obj = new JObject();
                obj.Add(HikeConstants.TYPE, HikeConstants.MqttMessageTypes.FORCE_SMS);
                obj.Add(HikeConstants.TO, message.Msisdn);
                obj.Add(HikeConstants.DATA, data);

                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, obj);

                ConversationTableUtils.updateLastMsgStatus(convMsgList.Last().MessageId, convMsgList.Last().Msisdn, (int)ConvMessage.State.FORCE_SMS_SENT_CONFIRMED);
            }
        }

        #endregion

        #region Read By

        ConvMessage _lastReceivedSentMessage = null, _readByMessage = null, _previouslastReceivedSentMessage = null;

        void UpdateLastSentMessageStatusOnUI()
        {
            if (!isGroupChat || !isGroupAlive || _groupInfo == null)//group info is populated in manage page
                return;

            _lastReceivedSentMessage = null;

            try
            {
                var msgList = ocMessages.Where(arg => arg.MessageId == _groupInfo.LastReadMessageId && arg.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO);

                _lastReceivedSentMessage = msgList != null && msgList.Count() > 0 ? msgList.Last() : null;
            }
            catch
            {
                return;
            }

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (ocMessages == null)
                    return;

                if (_lastReceivedSentMessage != null)
                {
                    if (_readByMessage == null)
                    {
                        _readByMessage = new ConvMessage();
                        _readByMessage.CurrentOrientation = this.Orientation;
                        _readByMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.MESSAGE_STATUS;
                        _readByMessage.NotificationType = ConvMessage.MessageType.UNKNOWN;
                        _readByMessage.MessageStatus = ConvMessage.State.UNKNOWN;
                    }

                    var msg = Utils.GetMessageStatus(_lastReceivedSentMessage.MessageStatus, _groupInfo.ReadByArray, _activeUsers, mContactNumber);

                    if (String.IsNullOrEmpty(msg))
                    {
                        Debug.WriteLine("empty status,readbyarray:" + _groupInfo.ReadByArray);
                        return;
                    }
                    _readByMessage.Message = msg;

                    ocMessages.Remove(_readByMessage);
                    var indexToInsert = ocMessages.IndexOf(_lastReceivedSentMessage) + 1;
                    ocMessages.Insert(indexToInsert, _readByMessage);

                    if (_previouslastReceivedSentMessage != _lastReceivedSentMessage)
                    {
                        if (indexToInsert == ocMessages.Count - 1)
                            ScrollToBottom();

                        _previouslastReceivedSentMessage = _lastReceivedSentMessage;
                    }
                }
                else
                {
                    if (_readByMessage != null && ocMessages.Contains(_readByMessage))
                        ocMessages.Remove(_readByMessage);
                }
            });
        }
        #endregion

        #region SERVER TIPS

        void InitializeToolTipControl(ImageSource leftIconSource, ImageSource rightIconSource, string headerText, string bodyText,
            bool isRightIconClickedEnabled, bool isFullTipTappedEnabled)
        {
            chatScreenToolTip.LeftIconSource = leftIconSource;
            chatScreenToolTip.RightIconSource = rightIconSource;
            chatScreenToolTip.TipText = bodyText;
            chatScreenToolTip.TipHeaderText = headerText;

            chatScreenToolTip.RightIconClicked -= chatScreenToolTip_RightIconClicked;

            if (isRightIconClickedEnabled)
                chatScreenToolTip.RightIconClicked += chatScreenToolTip_RightIconClicked;

            chatScreenToolTip.FullTipTapped -= chatScreenToolTip_FullTipTapped;

            if (isFullTipTappedEnabled)
                chatScreenToolTip.FullTipTapped += chatScreenToolTip_FullTipTapped;
        }

        ToolTipMode _tipMode;

        void UpdateToolTip(bool isModeChanged)
        {
            chatScreenToolTip.ResetToolTip();

            switch (_tipMode)
            {
                case ToolTipMode.DEFAULT:

                    break;

                case ToolTipMode.STICKERS:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipStickers, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ChatScreenTip.HeaderText, TipManager.ChatScreenTip.BodyText, true, true);
                    break;

                case ToolTipMode.CHAT_THEMES:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipChatTheme, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ChatScreenTip.HeaderText, TipManager.ChatScreenTip.BodyText, true, true);
                    break;

                case ToolTipMode.ATTACHMENTS:

                    InitializeToolTipControl(UI_Utils.Instance.ToolTipAttachment, UI_Utils.Instance.ToolTipCrossIcon, TipManager.ChatScreenTip.HeaderText, TipManager.ChatScreenTip.BodyText, true, true);
                    break;
            }

            if (_tipMode != ToolTipMode.DEFAULT && !chatScreenToolTip.IsShow)
                chatScreenToolTip.IsShow = true;

        }

        private void chatScreenToolTip_RightIconClicked(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.DEFAULT:

                    break;

                case ToolTipMode.CHAT_THEMES:

                    HideServerTips();
                    break;

                case ToolTipMode.ATTACHMENTS:

                    HideServerTips();
                    break;

                case ToolTipMode.STICKERS:

                    HideServerTips();
                    break;
            }
        }

        private void chatScreenToolTip_FullTipTapped(object sender, EventArgs e)
        {
            switch (_tipMode)
            {
                case ToolTipMode.DEFAULT:

                    break;

                case ToolTipMode.CHAT_THEMES:

                    HideServerTips();
                    Analytics.SendClickEvent(HikeConstants.ServerTips.THEME_TIP_TAP_EVENT);
                    chatBackgroundPopUp_Opened();
                    break;

                case ToolTipMode.ATTACHMENTS:

                    HideServerTips();
                    Analytics.SendClickEvent(HikeConstants.ServerTips.ATTACHMENT_TIP_TAP_EVENT);
                    fileTransferButton_Click(null, null);
                    break;

                case ToolTipMode.STICKERS:

                    HideServerTips();

                    Analytics.SendClickEvent(HikeConstants.ServerTips.STICKER_TIP_TAP_EVENT);

                    if (stickersIconButton != null)
                        emoticonButton_Click(stickersIconButton, null);

                    break;
            }
        }

        void ShowServerTips()
        {
            if (TipManager.ChatScreenTip != null && gcPin.Visibility == Visibility.Collapsed)
            {
                _tipMode = TipManager.ChatScreenTip.TipType;
                UpdateToolTip(true);
            }

        }

        void HideServerTips()
        {
            if (TipManager.ChatScreenTip != null && _tipMode == TipManager.ChatScreenTip.TipType)
            {
                if (TipManager.ChatScreenTip != null)
                    TipManager.Instance.RemoveTip(TipManager.ChatScreenTip.TipId);

                chatScreenToolTip.IsShow = false;
                _tipMode = ToolTipMode.DEFAULT;
                UpdateToolTip(true);
            }
        }

        #endregion

        private void PinTemplate_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            PhoneApplicationService.Current.State[HikeConstants.GC_PIN] = mContactNumber;
            NavigationService.Navigate(new Uri("/View/PinHistory.xaml", UriKind.Relative));
        }

        private void chatThemeHeader_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_isNewPin)
                _isPinAlter = false;
        }
    }
}
