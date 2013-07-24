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
using Coding4Fun.Phone.Controls;
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
using Microsoft.Phone.BackgroundAudio;
using System.Collections.ObjectModel;
using windows_client.ViewModel;
using System.Text.RegularExpressions;

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
        private PageOrientation pageOrientation = PageOrientation.Portrait;

        private const int maxFileSize = 15728640;//in bytes
        private const int maxSmsCharLength = 140;
        private string groupOwner = null;
        public string mContactNumber;
        private string mContactName = null;
        private string lastText = "";
        private string hintText = string.Empty;
        private bool enableSendMsgButton = false;
        private long _lastUpdatedLastSeenTimeStamp = 0;

        bool afterMute = true;
        bool _isStatusUpdateToolTipShown = false;
        ConvMessage _toolTipMessage;
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
        public ApplicationBarMenuItem addUserMenuItem;
        ApplicationBarMenuItem infoMenuItem;
        ApplicationBarIconButton sendIconButton = null;
        ApplicationBarIconButton emoticonsIconButton = null;
        ApplicationBarIconButton fileTransferIconButton = null;
        private PhotoChooserTask photoChooserTask;
        private CameraCaptureTask cameraCaptureTask;
        private BingMapsTask bingMapsTask = null;
        private object statusObject = null;


        private LastSeenHelper _lastSeenHelper;
        DispatcherTimer _forceSMSTimer;
        Boolean _isShownOnUI = false;
        Boolean _isSendAllAsSMSVisible = false;
        //        private ObservableCollection<MyChatBubble> chatThreadPageCollection = new ObservableCollection<MyChatBubble>();
        private Dictionary<long, ConvMessage> msgMap = new Dictionary<long, ConvMessage>(); // this holds msgId -> sent message bubble mapping
        //private Dictionary<ConvMessage, SentChatBubble> _convMessageSentBubbleMap = new Dictionary<ConvMessage, SentChatBubble>(); // this holds msgId -> sent message bubble mapping

        public bool isMessageLoaded;
        public ObservableCollection<ConvMessage> ocMessages;

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

        public bool IsSMSOptionValid = true;
        Pivot pivotStickers = null;
        #endregion

        #region UI VALUES

        private Thickness imgMargin = new Thickness(24, 5, 0, 15);
        MediaElement mediaElement;
        ConvMessage currentAudioMessage;

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
        private BitmapImage[] imagePathsForList3
        {
            get
            {
                return SmileyParser.Instance._emoticonImagesForList3;
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

        ConvMessage lastUnDeliveredMessage = null, tap2SendAsSMSMessage = null;

        private Dictionary<string, BitmapImage> dictStickerCache;

        #region PAGE BASED FUNCTIONS

        //        private ObservableCollection<UIElement> messagesCollection;
        public NewChatThread()
        {
            InitializeComponent();

            _dt = new DispatcherTimer();
            _dt.Interval = TimeSpan.FromMilliseconds(33);

            _duration = _microphone.BufferDuration;

            // Event handler for getting audio data when the buffer is full
            _microphone.BufferReady += new EventHandler<EventArgs>(microphone_BufferReady);

            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromSeconds(1);
            _progressTimer.Tick += new EventHandler(showWalkieTalkieProgress);

            _lastSeenHelper = new LastSeenHelper();
            _lastSeenHelper.UpdateLastSeen += LastSeenResponseReceived;

            onlineStatus.Source = UI_Utils.Instance.LastSeenClockImage;

            ocMessages = new ObservableCollection<ConvMessage>();
            dictStickerCache = new Dictionary<string, BitmapImage>();

            walkieTalkie.Source = UI_Utils.Instance.WalkieTalkieBigImage;
            deleteRecImageSuc.Source = UI_Utils.Instance.WalkieTalkieDeleteSucImage;

            if (Utils.isDarkTheme())
            {
                deleteRecTextSuc.Foreground = UI_Utils.Instance.DeleteGreyBackground;
                WalkieTalkieDeletedBorder.Opacity = 1;
                WalkieTalkieGridOverlayLayer.Opacity = 1;
            }
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
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
                }
            }
            else
            {
                // update old last seen from file
                _lastUpdatedLastSeenTimeStamp = FriendsTableUtils.GetFriendLastSeenTSFromFile(mContactNumber);

                if (_lastUpdatedLastSeenTimeStamp != 0)
                    UpdateLastSeenOnUI(_lastSeenHelper.GetLastSeenTimeStampStatus(_lastUpdatedLastSeenTimeStamp));
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
                    StartForceSMSTimer(false);
                });
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to load chat messages for msisdn {0} : {1}", mContactNumber, msec);
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
            emotList0.ItemsSource = imagePathsForList0;
            emotList1.ItemsSource = imagePathsForList1;
            emotList2.ItemsSource = imagePathsForList2;
            emotList3.ItemsSource = imagePathsForList3;

            if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))
            {
                ColumnDefinition col = new ColumnDefinition();
                gridEmoticonLabels.ColumnDefinitions.Add(col);
                stickerTab.SetValue(Grid.ColumnProperty, 4);
                btnBackKey.SetValue(Grid.ColumnProperty, 5);
                emotHeaderRect3.Visibility = Visibility.Visible;
            }
            else
            {
                emoticonPivot.Items.RemoveAt(3);
            }

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

            CompositionTarget.Rendering -= CompositionTarget_Rendering;
            CompositionTarget.Rendering += CompositionTarget_Rendering;

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
            if (HikeViewModel.stickerHelper == null)
                HikeViewModel.stickerHelper = new StickerHelper();

            if (e.NavigationMode == NavigationMode.New || App.IS_TOMBSTONED)
            {
                if (App.newChatThreadPage != null)
                    App.newChatThreadPage.gridStickers.Children.Remove(App.newChatThreadPage.pivotStickers);
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += (s, ee) =>
                {
                    HikeViewModel.stickerHelper.InitialiseLowResStickers();
                };
                bw.RunWorkerCompleted += (s, ee) =>
                {
                    CreateStickerPivot();
                    CreateStickerCategoriesPallete();
                };
                bw.RunWorkerAsync();
                App.newChatThreadPage = this;
            }

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

                    mediaElement.Stop();
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

                        currentAudioMessage.IsStopped = false;
                        currentAudioMessage.IsPlaying = false;
                        //currentAudioMessage.PlayProgressBarValue = 0;
                        //currentAudioMessage = null;
                    }
                }
            }

            //if (_recorderState == RecorderState.RECORDING)
            //{
            //    if (_stream != null)
            //    {
            //        byte[] audioBytes = _stream.ToArray();
            //        if (audioBytes != null && audioBytes.Length > 0)
            //            PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED] = _stream.ToArray();
            //    }
            //}

            if (_dt != null)
                _dt.Stop();

            if (!string.IsNullOrWhiteSpace(sendMsgTxtbox.Text))
                this.State["sendMsgTxtbox.Text"] = sendMsgTxtbox.Text;
            else
                this.State.Remove("sendMsgTxtbox.Text");

            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            App.IS_TOMBSTONED = false;
        }

        protected override void OnRemovedFromJournal(System.Windows.Navigation.JournalEntryRemovedEventArgs e)
        {
            try
            {
                base.OnRemovedFromJournal(e);
                removeListeners();

                if (mediaElement != null)
                {
                    mediaElement.Stop();

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
                    Debug.WriteLine("RecordMedia.xaml :: OnRemovedFromJournal, Exception : " + ex.StackTrace);
                }
                gridStickers.Children.Remove(pivotStickers);

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
            if (gridDownloadStickers.Visibility == Visibility.Visible)
                ShowDownloadOverlay(false);
            if (emoticonPanel.Visibility == Visibility.Visible)
            {
                App.ViewModel.HideToolTip(LayoutRoot, 1);
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

            if (mediaElement != null)
                mediaElement.Stop();

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
                if (obj.HasCustomPhoto) // represents group chat
                {
                    obj.Msisdn = obj.Id;//group id
                    GroupManager.Instance.LoadGroupParticipants(obj.Msisdn);
                    isGroupChat = true;
                    BlockTxtBlk.Text = AppResources.SelectUser_BlockedGroupMsg_Txt;
                    gi = GroupTableUtils.getGroupInfoForId(obj.Msisdn);
                    if (gi != null)
                        groupOwner = gi.GroupOwner;
                    if (gi != null && !gi.GroupAlive)
                        isGroupAlive = false;
                    ConversationListObject cobj;
                    if (App.ViewModel.ConvMap.TryGetValue(obj.Msisdn, out cobj))
                        IsMute = cobj.IsMute;
                }
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

                spContactTransfer.IsHitTestVisible = false;
                spContactTransfer.Opacity = 0.4;
            }

            userName.Text = mContactName;

            // if hike bot msg disable appbar, textbox etc
            if (Utils.IsHikeBotMsg(mContactNumber))
            {
                sendMsgTxtbox.IsEnabled = false;
                WalkieTalkieMicIcon.IsHitTestVisible = false;
                return;
            }

            #region LAST SEEN TIMER

            if (!App.appSettings.Contains(App.LAST_SEEN_SEETING))
            {
                BackgroundWorker _worker = new BackgroundWorker();

                _worker.DoWork += (ss, ee) =>
                {
                    var fStatus = FriendsTableUtils.GetFriendStatus(mContactNumber);
                    if (fStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT && !isGroupChat && isOnHike)
                        _lastSeenHelper.requestLastSeen(mContactNumber);
                };

                _worker.RunWorkerAsync();
            }

            #endregion

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

            int chatThreadCount;

            var keyExist = App.appSettings.TryGetValue(App.CHAT_THREAD_COUNT_KEY, out chatThreadCount); //initilaized in upgrade logic
            if (keyExist)
            {
                if (App.ViewModel.DictInAppTip != null)
                {

                    HikeToolTip tip;

                    if (chatThreadCount == 0)
                    {
                        App.ViewModel.DictInAppTip.TryGetValue(0, out tip);

                        if (tip != null && (!tip.IsShown || tip.IsCurrentlyShown))
                            App.ViewModel.DisplayTip(LayoutRoot, 0);
                        else
                            chatThreadCount++;

                        chatThreadMainPage.ApplicationBar = appBar;
                    }
                    else if (chatThreadCount == 1)
                    {
                        App.ViewModel.DictInAppTip.TryGetValue(2, out tip);

                        if (tip != null && (!tip.IsShown || tip.IsCurrentlyShown))
                            App.ViewModel.DisplayTip(LayoutRoot, 2);
                        else
                            chatThreadCount++;

                        chatThreadMainPage.ApplicationBar = appBar;
                    }
                    else
                        showNudgeTute();

                    App.WriteToIsoStorageSettings(App.CHAT_THREAD_COUNT_KEY, chatThreadCount);
                }
                else
                    showNudgeTute();
            }
            else
                chatThreadMainPage.ApplicationBar = appBar;

            IsSMSOptionValid = IsSMSOptionAvalable();
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

                spContactTransfer.IsHitTestVisible = true;
                spContactTransfer.Opacity = 1;

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

            });
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

                showFreeSMS = (from groupParticipant in GroupManager.Instance.GroupCache[mContactNumber]
                               where groupParticipant.Msisdn.Contains("+91")
                               select groupParticipant).Count() == 0 ? false : true;

            }
            else if (!mContactNumber.Contains(HikeConstants.INDIA_COUNTRY_CODE)) //Indian receiver
                showFreeSMS = false;

            return showFreeSMS;
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
        const int INITIAL_FETCH_COUNT = 31;
        const int SUBSEQUENT_FETCH_COUNT = 11;

        // this variable stores the status of last SENT msg
        ConvMessage.State refState = ConvMessage.State.UNKNOWN;

        private void loadMessages(int messageFetchCount)
        {
            int i;
            bool isPublish = false;
            hasMoreMessages = false;

            List<ConvMessage> messagesList = MessagesTableUtils.getMessagesForMsisdn(mContactNumber, lastMessageId < 0 ? long.MaxValue : lastMessageId, messageFetchCount);

            if (messagesList == null) // represents there are no chat messages for this msisdn
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    //messageListBox.Opacity = 1;
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

        private void forwardAttachmentMessage()
        {
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
            {
                if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is object[])
                {
                    object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                    if (attachmentData.Length == 1)
                    {
                        ConvMessage convMessage = new ConvMessage(AppResources.Sticker_Txt, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                        convMessage.IsSms = !isOnHike;
                        convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
                        convMessage.MetaDataString = attachmentData[0] as string;

                        AddNewMessageToUI(convMessage, false);

                        mPubSub.publish(HikePubSub.MESSAGE_SENT, convMessage);
                        PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
                    }
                    else
                    {
                        string contentType = (string)attachmentData[0];
                        string sourceMsisdn = (string)attachmentData[1];
                        long messageId = (long)attachmentData[2];
                        string metaDataString = (string)attachmentData[3];
                        string sourceFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn + "/" + messageId;

                        ConvMessage convMessage = new ConvMessage("", mContactNumber,
                            TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                        convMessage.IsSms = !isOnHike;
                        convMessage.HasAttachment = true;
                        convMessage.FileAttachment = new Attachment();
                        convMessage.FileAttachment.ContentType = contentType;
                        convMessage.FileAttachment.Thumbnail = (byte[])attachmentData[4];
                        convMessage.FileAttachment.FileName = (string)attachmentData[5];
                        convMessage.IsSms = !isOnHike;
                        convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;

                        if (contentType.Contains(HikeConstants.IMAGE))
                            convMessage.Message = AppResources.Image_Txt;
                        else if (contentType.Contains(HikeConstants.AUDIO))
                        {
                            convMessage.Message = AppResources.Audio_Txt;
                            convMessage.MetaDataString = metaDataString;
                        }
                        else if (contentType.Contains(HikeConstants.VIDEO))
                            convMessage.Message = AppResources.Video_Txt;
                        else if (contentType.Contains(HikeConstants.LOCATION))
                        {
                            convMessage.Message = (string)attachmentData[5];
                            convMessage.MetaDataString = metaDataString;
                        }
                        else if (contentType.Contains(HikeConstants.CT_CONTACT))
                        {
                            convMessage.Message = AppResources.ContactTransfer_Text;
                            convMessage.MetaDataString = metaDataString;
                        }

                        AddMessageToOcMessages(convMessage, false);
                        object[] vals = new object[3];
                        vals[0] = convMessage;
                        vals[1] = sourceFilePath;
                        mPubSub.publish(HikePubSub.FORWARD_ATTACHMENT, vals);
                        PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
                    }
                }

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

        Object obj = new object();
        //this function is called from UI thread only. No need to synch.
        private void ScrollToBottom()
        {
            try
            {
                if (this.ocMessages.Count > 0 && (!IsMute || this.ocMessages.Count < App.ViewModel.ConvMap[mContactNumber].MuteVal))
                {
                    llsMessages.ScrollTo(this.ocMessages[this.ocMessages.Count - 1]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("NewChatThread::ScrollToBottom , Exception:" + ex.Message);
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
                    appBar.IsMenuEnabled = false;
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
            mPubSub.addListener(HikePubSub.LAST_SEEN, this);
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
                mPubSub.removeListener(HikePubSub.LAST_SEEN, this);
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
                appBar.IsMenuEnabled = true;
            }
        }

        private void FileAttachmentMessage_Tap(object sender, SelectionChangedEventArgs e)
        {
            emoticonPanel.Visibility = Visibility.Collapsed;
            App.ViewModel.HideToolTip(LayoutRoot, 1);
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
                if (convMessage.StickerObj != null && convMessage.StickerObj.StickerImage == null && convMessage.ImageDownloadFailed)
                {
                    AccountUtils.GetSingleSticker(convMessage, ResolutionId, new AccountUtils.parametrisedPostResponseFunction(StickersRequestCallBack));
                    convMessage.ImageDownloadFailed = false;
                }
                else if (convMessage.FileAttachment == null || convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return;
                else if (convMessage.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED && convMessage.FileAttachment.FileState != Attachment.AttachmentState.STARTED)
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
            else if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
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
                                }
                                else
                                {
                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.IsStopped = false;
                                    mediaElement.Play();
                                }
                            }
                            else // restart audio
                            {
                                if (mediaElement.NaturalDuration.TimeSpan.TotalMilliseconds > 0)
                                {
                                    currentAudioMessage = convMessage;
                                    currentAudioMessage.IsPlaying = true;
                                    currentAudioMessage.IsStopped = false;
                                    mediaElement.Play();
                                }
                            }
                        }
                        else // start new audio
                        {
                            try
                            {
                                mediaElement.Source = null;

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

                    double latitude = Convert.ToDouble(locationJSON[HikeConstants.LATITUDE].ToString());
                    double longitude = Convert.ToDouble(locationJSON[HikeConstants.LONGITUDE].ToString());

                    PhoneApplicationService.Current.State[HikeConstants.LOCATION_COORDINATE] = new GeoCoordinate(latitude, longitude);

                    this.NavigationService.Navigate(new Uri("/View/ShowLocation.xaml", UriKind.Relative));

                    //if (this.bingMapsTask == null)
                    //    bingMapsTask = new BingMapsTask();
                    //double zoomLevel = Convert.ToDouble(locationJSON[HikeConstants.ZOOM_LEVEL].ToString());
                    //bingMapsTask.Center = new GeoCoordinate(latitude, longitude);
                    //bingMapsTask.ZoomLevel = zoomLevel;
                    //bingMapsTask.Show();
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

            mediaElement.Stop();
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

            mediaElement.Stop();
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
            if (_isSendAllAsSMSVisible && ocMessages != null && convMessage.IsSent)
            {
                ocMessages.Remove(tap2SendAsSMSMessage);
                _isSendAllAsSMSVisible = false;
            }

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
                        if (!string.IsNullOrEmpty(convMessage.MetaDataString) && convMessage.MetaDataString.Contains(HikeConstants.STICKER_ID))
                        {
                            JObject meataDataJson = JObject.Parse(convMessage.MetaDataString);
                            convMessage.StickerObj = new Sticker((string)meataDataJson[HikeConstants.CATEGORY_ID], (string)meataDataJson[HikeConstants.STICKER_ID], null);
                            string categoryStickerId = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
                            if (dictStickerCache.ContainsKey(categoryStickerId))
                            {
                                convMessage.StickerObj.StickerImage = dictStickerCache[categoryStickerId];
                            }
                            else
                            {
                                convMessage.StickerObj.StickerImage = StickerCategory.GetHighResolutionSticker(convMessage.StickerObj.Id, convMessage.StickerObj.Category);
                                if (convMessage.StickerObj.StickerImage == null)
                                    AccountUtils.GetSingleSticker(convMessage, ResolutionId, new AccountUtils.parametrisedPostResponseFunction(StickersRequestCallBack));
                                else
                                    dictStickerCache[categoryStickerId] = convMessage.StickerObj.StickerImage;
                            }
                            chatBubble = convMessage;
                            if (!convMessage.IsSent)
                                chatBubble.GroupMemberName = isGroupChat ?
                                   GroupManager.Instance.getGroupParticipant(null, convMessage.GroupParticipant, mContactNumber).FirstName + "-" : string.Empty;
                        }
                        else
                        {
                            if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains("lm"))
                            {
                                string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);
                                if (message.Length > 0)
                                    convMessage.Message = message;
                            }
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
                    }
                    chatBubble.IsSms = !isOnHike;
                    chatBubble.CurrentOrientation = this.Orientation;
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
                else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_JOINED || convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.USER_REJOINED)
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

                    if (App.ViewModel.DictInAppTip != null)
                    {
                        HikeToolTip tip;
                        App.ViewModel.DictInAppTip.TryGetValue(4, out tip);

                        if (!_isStatusUpdateToolTipShown && tip != null && (!tip.IsShown || tip.IsCurrentlyShown))
                        {
                            _toolTipMessage = new ConvMessage();
                            _toolTipMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.IN_APP_TIP;
                            _toolTipMessage.Message = String.Format(AppResources.In_App_Tip_5, mContactName);
                            this.ocMessages.Insert(insertPosition, _toolTipMessage);
                            insertPosition++;
                            _isStatusUpdateToolTipShown = true;

                            tip.IsShown = true;
                            tip.IsCurrentlyShown = true;

                            byte marked;
                            App.appSettings.TryGetValue(App.TIP_MARKED_KEY, out marked);
                            marked |= (byte)(1 << 4);
                            App.WriteToIsoStorageSettings(App.TIP_MARKED_KEY, marked);

                            byte currentShown;
                            App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
                            currentShown |= (byte)(1 << 4);
                            App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);
                        }
                    }
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
                if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian open sms client
                {
                    long time = TimeUtils.getCurrentTimeStamp();
                    if (isGroupChat)
                    {
                        foreach (GroupParticipant gp in GroupManager.Instance.GroupCache[mContactNumber])
                        {
                            if (!gp.IsOnHike)
                            {
                                ConvMessage convMessage = new ConvMessage(Utils.GetRandomInviteString(), gp.Msisdn, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
                                convMessage.IsInvite = true;
                                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(false));
                            }
                        }
                    }
                    else
                    {
                        //App.appSettings.TryGetValue<string>(HikeConstants.INVITE_TOKEN, out inviteToken);
                        ConvMessage convMessage = new ConvMessage(Utils.GetRandomInviteString(), mContactNumber, time, ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
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
                        foreach (GroupParticipant gp in GroupManager.Instance.GroupCache[mContactNumber])
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
                    var randomString = Utils.GetRandomInviteString();

                    if (!isGroupChat)
                    {
                        obj[HikeConstants.TO] = msisdns;
                        data[HikeConstants.MESSAGE_ID] = ts.ToString();
                        data[HikeConstants.HIKE_MESSAGE] = randomString;
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
                    smsComposeTask.Body = randomString;
                    smsComposeTask.Show();
                }
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
            svMessage.ScrollToVerticalOffset(sendMsgTxtbox.GetRectFromCharacterIndex(sendMsgTxtbox.SelectionStart).Top - 30.0);

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

            if (emoticonPanel.Visibility == Visibility.Collapsed)
                sendMsgTxtbox.Focus();

            if (String.IsNullOrEmpty(message))
                return;

            App.ViewModel.HideToolTip(LayoutRoot, 1);

            attachmentMenu.Visibility = Visibility.Collapsed;

            if (message == "" || (!isOnHike && mCredits <= 0))
                return;

            endTypingSent = true;
            sendTypingNotification(false);

            ConvMessage convMessage = new ConvMessage(message, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            convMessage.IsSms = !isOnHike;
            sendMsg(convMessage, false);

            spSmsCharCounter.Visibility = Visibility.Collapsed;

        }

        void photoChooserTask_Completed(object sender, PhotoResult e)
        {
            App.ViewModel.HideToolTip(LayoutRoot, 1);

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
            sendMsgTxtbox.Background = UI_Utils.Instance.TextBoxBackground;
            sendMsgTxtbox.Hint = string.Empty;//done intentionally as hint is shown if text is changed
            sendMsgTxtbox.Hint = hintText;
            //this.messageListBox.Margin = UI_Utils.Instance.ChatThreadKeyPadUpMargin;
            //ScrollToBottom();
            if (this.emoticonPanel.Visibility == Visibility.Visible)
            {
                App.ViewModel.HideToolTip(LayoutRoot, 1);

                this.emoticonPanel.Visibility = Visibility.Collapsed;
            }

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
        #endregion

        #region CONTEXT MENU

        private void MenuItem_Click_Forward(object sender, RoutedEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.STICKER_ID))
            {
                Object[] obj = new Object[1];
                obj[0] = convMessage.MetaDataString;
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = obj;//done this way to distinguish it from message
            }
            else if (convMessage.FileAttachment == null)
            {
                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = convMessage.Message;
            }
            else
            {
                //done this way as on locking it is unable to serialize convmessage or attachment object
                object[] attachmentForwardMessage = new object[6];
                attachmentForwardMessage[0] = convMessage.FileAttachment.ContentType;
                attachmentForwardMessage[1] = mContactNumber;
                attachmentForwardMessage[2] = convMessage.MessageId;
                attachmentForwardMessage[3] = convMessage.MetaDataString;
                attachmentForwardMessage[4] = convMessage.FileAttachment.Thumbnail;
                attachmentForwardMessage[5] = convMessage.FileAttachment.FileName;

                PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] = attachmentForwardMessage;
            }
            NavigationService.Navigate(new Uri("/View/NewSelectUserPage.xaml", UriKind.Relative));

        }

        private void MenuItem_Click_Copy(object sender, RoutedEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage chatBubble = ((sender as MenuItem).DataContext as ConvMessage);
            if (chatBubble.FileAttachment == null)
                Clipboard.SetText(chatBubble.Message);
            else if (!String.IsNullOrEmpty(chatBubble.FileAttachment.FileKey))
                Clipboard.SetText(HikeConstants.FILE_TRANSFER_COPY_BASE_URL + "/" + chatBubble.FileAttachment.FileKey);
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            isContextMenuTapped = true;
            ConvMessage msg = ((sender as MenuItem).DataContext as ConvMessage);

            if (msg == null)
                return;

            if (currentAudioMessage != null && msg == currentAudioMessage && msg.IsPlaying)
            {
                currentAudioMessage = null;
                mediaElement.Stop();
            }

            if (msg.FileAttachment != null && msg.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                msg.SetAttachmentState(Attachment.AttachmentState.CANCELED);

            bool delConv = false;
            this.ocMessages.Remove(msg);

            if (_isSendAllAsSMSVisible && lastUnDeliveredMessage == msg)
            {
                ocMessages.Remove(tap2SendAsSMSMessage);
                _isSendAllAsSMSVisible = false;
                ShowForceSMSOnUI();
            }

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

        private void MenuItem_Click_Cancel(object sender, RoutedEventArgs e)
        {
            ConvMessage convMessage = ((sender as MenuItem).DataContext as ConvMessage);
            if (convMessage.FileAttachment.FileState == Attachment.AttachmentState.STARTED)
            {
                convMessage.SetAttachmentState(Attachment.AttachmentState.CANCELED);
                MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, mContactNumber, convMessage.MessageId);
            }
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

                if (_isSendAllAsSMSVisible && lastUnDeliveredMessage == convMessage)
                {
                    ocMessages.Remove(tap2SendAsSMSMessage);
                    _isSendAllAsSMSVisible = false;
                    ShowForceSMSOnUI();
                }
            }
            else
                MessageBox.Show(AppResources.H2HOfline_0SMS_Message, AppResources.H2HOfline_Confirmation_Message_Heading, MessageBoxButton.OK);
        }

        #endregion

        #region EMOTICONS RELATED STUFF

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            object s = e.OriginalSource;
        }

        private void emoticonButton_Click(object sender, EventArgs e)
        {
            if (recordGrid.Visibility == Visibility.Visible)
            {
                recordGrid.Visibility = Visibility.Collapsed;
                sendMsgTxtbox.Visibility = Visibility.Visible;
            }

            App.ViewModel.HideToolTip(LayoutRoot, 0);
            App.ViewModel.HideToolTip(LayoutRoot, 2);

            if (emoticonPanel.Visibility == Visibility.Collapsed)
            {
                App.ViewModel.DisplayTip(LayoutRoot, 1);
                emoticonPanel.Visibility = Visibility.Visible;

                if (gridStickers.Visibility == Visibility.Visible)
                {
                    if (pivotStickers.SelectedIndex > 0)
                    {
                        string category;
                        if (StickerPivotHelper.Instance.dictPivotCategory.TryGetValue(pivotStickers.SelectedIndex, out category))
                        {
                            CategoryTap(category);
                        }
                    }

                }
            }
            else
            {
                App.ViewModel.HideToolTip(LayoutRoot, 1);
                emoticonPanel.Visibility = Visibility.Collapsed;
            }

            attachmentMenu.Visibility = Visibility.Collapsed;
            this.Focus();
        }

        private void fileTransferButton_Click(object sender, EventArgs e)
        {
            if (recordGrid.Visibility == Visibility.Visible)
            {
                recordGrid.Visibility = Visibility.Collapsed;
                sendMsgTxtbox.Visibility = Visibility.Visible;
            }

            App.ViewModel.HideToolTip(LayoutRoot, 0);
            App.ViewModel.HideToolTip(LayoutRoot, 2);

            if (attachmentMenu.Visibility == Visibility.Collapsed)
                attachmentMenu.Visibility = Visibility.Visible;
            else
                attachmentMenu.Visibility = Visibility.Collapsed;

            if (emoticonPanel.Visibility == Visibility.Visible)
            {
                App.ViewModel.HideToolTip(LayoutRoot, 1);

                emoticonPanel.Visibility = Visibility.Collapsed;
            }
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
            if (!spContactTransfer.IsHitTestVisible)
                return;

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
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList0.SelectedIndex;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList1.SelectedIndex + SmileyParser.Instance.emoticon0Size;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
        }

        private void emotList2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList2.SelectedIndex + SmileyParser.Instance.emoticon0Size + SmileyParser.Instance.emoticon1Size;
            sendMsgTxtbox.Text += SmileyParser.Instance.emoticonStrings[index];
            //emoticonPanel.Visibility = Visibility.Collapsed;
        }
        private void emotList3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            recordGrid.Visibility = Visibility.Collapsed;
            sendMsgTxtbox.Visibility = Visibility.Visible;
            int index = emotList3.SelectedIndex + SmileyParser.Instance.emoticon0Size + SmileyParser.Instance.emoticon1Size + SmileyParser.Instance.emoticon2Size;
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
                Thread.Sleep(500);

                //TODO handle vibration for user profile and GC.
                if ((convMessage.Msisdn == mContactNumber && (convMessage.MetaDataString != null &&
                    convMessage.MetaDataString.Contains(HikeConstants.POKE))) &&
                    convMessage.GrpParticipantState != ConvMessage.ParticipantInfoState.STATUS_UPDATE && (!isGroupChat || !_isMute))
                {
                    bool isVibrateEnabled = true;
                    App.appSettings.TryGetValue<bool>(App.VIBRATE_PREF, out isVibrateEnabled);

                    if (isVibrateEnabled)
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
                    if (App.ViewModel.ConvMap.TryGetValue(convMessage.Msisdn, out val) && val.IsMute) // of msg is for muted forwardedMessage, ignore msg
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

                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            StartForceSMSTimer(true);
                        });
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
                        if (msg.MessageStatus >= ConvMessage.State.FORCE_SMS_SENT_CONFIRMED)
                            msg.MessageStatus = ConvMessage.State.FORCE_SMS_SENT_DELIVERED;
                        else
                            msg.MessageStatus = ConvMessage.State.SENT_DELIVERED;
                    }

                    if (_isSendAllAsSMSVisible && ocMessages != null && msg == lastUnDeliveredMessage)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            ocMessages.Remove(tap2SendAsSMSMessage);
                            _isSendAllAsSMSVisible = false;
                            ShowForceSMSOnUI();
                        });
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
                            if (msg.MessageStatus >= ConvMessage.State.FORCE_SMS_SENT_CONFIRMED)
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

                if (_isSendAllAsSMSVisible && lastUnDeliveredMessage.MessageStatus != ConvMessage.State.SENT_CONFIRMED)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ocMessages.Remove(tap2SendAsSMSMessage);
                        _isSendAllAsSMSVisible = false;
                        ShowForceSMSOnUI();
                    });
                }
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
                if (!App.appSettings.Contains(App.LAST_SEEN_SEETING))
                {
                    var fStatus = FriendsTableUtils.GetFriendStatus(mContactNumber);

                    if (fStatus > FriendsTableUtils.FriendStatusEnum.REQUEST_SENT && !isGroupChat && _lastUpdatedLastSeenTimeStamp != 0) //dont show online if his last seen setting is off
                        UpdateLastSeenOnUI(AppResources.Online);
                }

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
                                userName.FontSize = 50;
                                lastSeenPannel.Visibility = Visibility.Collapsed;
                            });
                        }
                    }
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
                var fileData = locationJSON[HikeConstants.FILES_DATA][0];

                string locationJSONString = locationJSON.ToString();

                byte[] locationBytes = (new System.Text.UTF8Encoding()).GetBytes(locationJSONString);

                var vicinity = fileData[HikeConstants.LOCATION_ADDRESS].ToString();
                string locationMessage = String.Empty;
                string fileName = fileData[HikeConstants.FILE_NAME].ToString();

                locationMessage = fileName;

                if (!String.IsNullOrEmpty(vicinity))
                {
                    if (!String.IsNullOrEmpty(locationMessage))
                        locationMessage += "\n" + vicinity;
                    else
                        locationMessage = vicinity;
                }

                if (String.IsNullOrEmpty(fileName))
                    fileName = AppResources.Location_Txt;

                ConvMessage convMessage = new ConvMessage(locationMessage, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation)
                {
                    IsSms = !isOnHike,
                    HasAttachment = true,
                    MetaDataString = locationJSONString
                };

                convMessage.FileAttachment = new Attachment(fileName, imageThumbnail, Attachment.AttachmentState.STARTED);
                convMessage.FileAttachment.ContentType = "hikemap/location";

                AddNewMessageToUI(convMessage, false);

                ScrollToBottom();

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

                if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.AUDIO_RECORDED_DURATION))
                {
                    _recordedDuration = (int)PhoneApplicationService.Current.State[HikeConstants.AUDIO_RECORDED_DURATION];
                    PhoneApplicationService.Current.State.Remove(HikeConstants.AUDIO_RECORDED_DURATION);
                }

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
                    fileName = "aud_" + TimeUtils.getCurrentTimeStamp().ToString() + ".mp3";
                    convMessage.FileAttachment = new Attachment(fileName, null, Attachment.AttachmentState.STARTED);
                    convMessage.FileAttachment.ContentType = "audio/voice";

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
            emotHeaderRect0.Background = UI_Utils.Instance.TappedCategoryColor;
            emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
            emoticonPivot.SelectedIndex = 0;
        }

        private void emotHeaderRect1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect1.Background = UI_Utils.Instance.TappedCategoryColor;
            emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
            emoticonPivot.SelectedIndex = 1;

        }

        private void emotHeaderRect2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect2.Background = UI_Utils.Instance.TappedCategoryColor;
            emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
            emoticonPivot.SelectedIndex = 2;
        }
        private void emotHeaderRect3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
            emotHeaderRect3.Background = UI_Utils.Instance.TappedCategoryColor;
            emoticonPivot.SelectedIndex = 3;
        }
        private void emoticonPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (emoticonPivot.SelectedIndex)
            {
                case 0:
                    emotHeaderRect0.Background = UI_Utils.Instance.TappedCategoryColor;
                    emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
                    break;
                case 1:
                    emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect1.Background = UI_Utils.Instance.TappedCategoryColor;
                    emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
                    break;
                case 2:
                    emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect2.Background = UI_Utils.Instance.TappedCategoryColor;
                    emotHeaderRect3.Background = UI_Utils.Instance.UntappedCategoryColor;
                    break;
                case 3:
                    emotHeaderRect0.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect1.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect2.Background = UI_Utils.Instance.UntappedCategoryColor;
                    emotHeaderRect3.Background = UI_Utils.Instance.TappedCategoryColor;
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

            if (mUserIsBlocked)
                return;
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
                ConvMessage convMessage = ocMessages[i];
                if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO && !convMessage.HasAttachment)
                    convMessage.CurrentOrientation = e.Orientation;
            }

            //handled textbox hight to accomodate other data on screen in diff orientations
            if (e.Orientation == PageOrientation.Portrait || e.Orientation == PageOrientation.PortraitUp || e.Orientation == PageOrientation.PortraitDown)
            {
                svMessage.MaxHeight = 150;
            }
            else if (e.Orientation == PageOrientation.Landscape || e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight)
            {
                svMessage.MaxHeight = 70;

                App.ViewModel.HideToolTip(LayoutRoot, 0);
                App.ViewModel.HideToolTip(LayoutRoot, 1);
                App.ViewModel.HideToolTip(LayoutRoot, 5);
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

        #region Stickers


        bool isStickersLoaded = false;
        private string _selectedCategory = string.Empty;
        Thickness zeroThickness = new Thickness(0, 0, 0, 0);
        Thickness newCategoryThickness = new Thickness(0, 1, 0, 0);


        private void Stickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBox llsStickerCategory = (sender as ListBox);
            Sticker sticker = llsStickerCategory.SelectedItem as Sticker;
            llsStickerCategory.SelectedItem = null;
            if (sticker == null)
                return;

            ConvMessage conv = new ConvMessage(AppResources.Sticker_Txt, mContactNumber, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED, this.Orientation);
            conv.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
            conv.StickerObj = new Sticker(sticker.Category, sticker.Id, null);
            conv.MetaDataString = string.Format("{{{0}:'{1}',{2}:'{3}'}}", HikeConstants.STICKER_ID, sticker.Id, HikeConstants.CATEGORY_ID, sticker.Category);
            //Stickers_tap is binded to pivot and cached so to update latest object this is done
            App.newChatThreadPage.AddNewMessageToUI(conv, false);

            mPubSub.publish(HikePubSub.MESSAGE_SENT, conv);
        }

        private void StickersTab_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.HideToolTip(LayoutRoot, 1);

            gridEmoticons.Visibility = Visibility.Collapsed;
            gridStickers.Visibility = Visibility.Visible;
            if (!isStickersLoaded)
            {
                Category1_Tap(sender, e);
                isStickersLoaded = true;
            }
        }


        private void PivotStickers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string category;
            if (StickerPivotHelper.Instance.dictPivotCategory.TryGetValue(pivotStickers.SelectedIndex, out category))
            {
                switch (category)
                {
                    case StickerHelper.CATEGORY_DOGGY:
                        Category1_Tap(null, null);
                        break;
                    case StickerHelper.CATEGORY_KITTY:
                        Category2_Tap(null, null);
                        break;
                    case StickerHelper.CATEGORY_EXPRESSIONS:
                        Category3_Tap(null, null);
                        break;
                    case StickerHelper.CATEGORY_BOLLYWOOD:
                        Category4_Tap(null, null);
                        break;
                    case StickerHelper.CATEGORY_TROLL:
                        Category5_Tap(null, null);
                        break;
                }
            }
        }

        private void StickersBack_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            gridEmoticons.Visibility = Visibility.Visible;
            gridStickers.Visibility = Visibility.Collapsed;
        }

        private void Category1_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_selectedCategory == StickerHelper.CATEGORY_DOGGY)
                return;
            _selectedCategory = StickerHelper.CATEGORY_DOGGY;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_DOGGY);
            StickerPivotItem stickerPivot = StickerPivotHelper.Instance.dictStickersPivot[StickerHelper.CATEGORY_DOGGY];
            pivotStickers.SelectedIndex = stickerPivot.PivotItemIndex;

            stCategory1.Background = UI_Utils.Instance.TappedCategoryColor;
            stCategory2.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory3.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory4.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory5.Background = UI_Utils.Instance.UntappedCategoryColor;

            imgDoggy.Source = UI_Utils.Instance.DoggyActive;
            imgKitty.Source = UI_Utils.Instance.KittyInactive;
            imgExpressions.Source = UI_Utils.Instance.ExpressionsInactive;
            imgBolly.Source = UI_Utils.Instance.BollywoodInactive;
            imgTroll.Source = UI_Utils.Instance.TrollInactive;

            stickerPivot.ShowStickers();
            if (stickerCategory.HasMoreStickers)
            {
                downloadStickers_Tap(null, null);
            }
            if (stickerCategory.HasNewMessages)
            {
                stCategory1.BorderThickness = zeroThickness;
                StickerCategory.UpdateHasMoreMessages(_selectedCategory, stickerCategory.HasNewMessages, false);
            }
            if (App.appSettings.Contains(HikeConstants.AppSettings.SHOW_DOGGY_OVERLAY))
            {
                ShowDownloadOverlay(true);
            }
        }

        private void Category2_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_selectedCategory == StickerHelper.CATEGORY_KITTY)
                return;
            _selectedCategory = StickerHelper.CATEGORY_KITTY;

            stCategory1.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory2.Background = UI_Utils.Instance.TappedCategoryColor;
            stCategory3.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory4.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory5.Background = UI_Utils.Instance.UntappedCategoryColor;

            imgDoggy.Source = UI_Utils.Instance.DoggyInactive;
            imgKitty.Source = UI_Utils.Instance.KittyActive;
            imgExpressions.Source = UI_Utils.Instance.ExpressionsInactive;
            imgBolly.Source = UI_Utils.Instance.BollywoodInactive;
            imgTroll.Source = UI_Utils.Instance.TrollInactive;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory.HasNewMessages)
            {
                stCategory2.BorderThickness = zeroThickness;
                StickerCategory.UpdateHasMoreMessages(_selectedCategory, stickerCategory.HasNewMessages, false);
            }
            CategoryTap(StickerHelper.CATEGORY_KITTY);
        }

        private void Category3_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_selectedCategory == StickerHelper.CATEGORY_EXPRESSIONS)
                return;
            _selectedCategory = StickerHelper.CATEGORY_EXPRESSIONS;
            stCategory1.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory2.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory3.Background = UI_Utils.Instance.TappedCategoryColor;
            stCategory4.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory5.Background = UI_Utils.Instance.UntappedCategoryColor;

            imgDoggy.Source = UI_Utils.Instance.DoggyInactive;
            imgKitty.Source = UI_Utils.Instance.KittyInactive;
            imgExpressions.Source = UI_Utils.Instance.ExpressionsActive;
            imgBolly.Source = UI_Utils.Instance.BollywoodInactive;
            imgTroll.Source = UI_Utils.Instance.TrollInactive;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory.HasNewMessages)
            {
                stCategory3.BorderThickness = zeroThickness;
                StickerCategory.UpdateHasMoreMessages(_selectedCategory, stickerCategory.HasNewMessages, false);
            }
            CategoryTap(StickerHelper.CATEGORY_EXPRESSIONS);
        }

        private void Category4_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_selectedCategory == StickerHelper.CATEGORY_BOLLYWOOD)
                return;
            _selectedCategory = StickerHelper.CATEGORY_BOLLYWOOD;
            stCategory1.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory2.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory3.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory5.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory4.Background = UI_Utils.Instance.TappedCategoryColor;

            imgDoggy.Source = UI_Utils.Instance.DoggyInactive;
            imgKitty.Source = UI_Utils.Instance.KittyInactive;
            imgExpressions.Source = UI_Utils.Instance.ExpressionsInactive;
            imgBolly.Source = UI_Utils.Instance.BollywoodActive;
            imgTroll.Source = UI_Utils.Instance.TrollInactive;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory.HasNewMessages)
            {
                stCategory4.BorderThickness = zeroThickness;
                StickerCategory.UpdateHasMoreMessages(_selectedCategory, stickerCategory.HasNewMessages, false);
            }
            CategoryTap(StickerHelper.CATEGORY_BOLLYWOOD);

        }

        private void Category5_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_selectedCategory == StickerHelper.CATEGORY_TROLL)
                return;
            _selectedCategory = StickerHelper.CATEGORY_TROLL;
            stCategory1.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory2.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory3.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory4.Background = UI_Utils.Instance.UntappedCategoryColor;
            stCategory5.Background = UI_Utils.Instance.TappedCategoryColor;

            imgDoggy.Source = UI_Utils.Instance.DoggyInactive;
            imgKitty.Source = UI_Utils.Instance.KittyInactive;
            imgExpressions.Source = UI_Utils.Instance.ExpressionsInactive;
            imgBolly.Source = UI_Utils.Instance.BollywoodInactive;
            imgTroll.Source = UI_Utils.Instance.TrollActive;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
            if (stickerCategory.HasNewMessages)
            {
                stCategory5.BorderThickness = zeroThickness;
                StickerCategory.UpdateHasMoreMessages(_selectedCategory, stickerCategory.HasNewMessages, false);
            }
            CategoryTap(StickerHelper.CATEGORY_TROLL);
        }

        private void CategoryTap(string category)
        {
            StickerPivotItem stickerPivot = StickerPivotHelper.Instance.dictStickersPivot[category];
            pivotStickers.SelectedIndex = stickerPivot.PivotItemIndex;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);

            if (stickerCategory.ShowDownloadMessage)
            {
                ShowDownloadOverlay(true);
                return;
            }
            if (stickerCategory == null)
            {
                stickerPivot.ShowNoStickers();
            }
            else if (stickerCategory.ListStickers.Count == 0 && stickerCategory.HasMoreStickers)
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
                foreach (Sticker sticker in stickerCategory.ListStickers)
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
                            stickerPivot.ShowDownloadFailed();
                            stickerPivot.ShowHidMoreProgreesBar(false);
                        });
                    }
                }
                if (convMessage != null)
                {
                    convMessage.ImageDownloadFailed = true;
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
                    stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);
                    if (stickerCategory == null)
                    {
                        stickerCategory = new StickerCategory(category);
                    }
                }
                IEnumerator<KeyValuePair<string, JToken>> keyVals = stickers.GetEnumerator();
                List<KeyValuePair<string, byte[]>> listHighResStickersBytes = new List<KeyValuePair<string, byte[]>>();

                while (keyVals.MoveNext())
                {
                    try
                    {
                        KeyValuePair<string, JToken> kv = keyVals.Current;
                        string id = (string)kv.Key;
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
                    foreach (KeyValuePair<string, Byte[]> keyValuePair in listHighResStickersBytes)
                    {
                        string stickerId = keyValuePair.Key;
                        BitmapImage highResImage = UI_Utils.Instance.createImageFromBytes(keyValuePair.Value);
                        if (highResImage == null)
                            continue;

                        if (convMessage != null)
                        {
                            string key = convMessage.StickerObj.Category + "_" + convMessage.StickerObj.Id;
                            convMessage.StickerObj.StickerImage = highResImage;
                            convMessage.ImageDownloadFailed = false;
                            if (dictStickerCache.ContainsKey(key))
                                continue;
                            dictStickerCache[key] = highResImage;
                        }

                        Byte[] lowResImageBytes = UI_Utils.Instance.PngImgToJpegByteArray(highResImage);
                        listLowResStickersBytes.Add(new KeyValuePair<string, byte[]>(stickerId, lowResImageBytes));
                        stickerCategory.ListStickers.Add(new Sticker(category, stickerId, UI_Utils.Instance.createImageFromBytes(lowResImageBytes)));
                    }
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
                }
            }
        }

        public void ShowDownloadOverlay(bool show)
        {
            sendIconButton.IsEnabled = !show;
            emoticonsIconButton.IsEnabled = !show;
            fileTransferIconButton.IsEnabled = !show;
            if (show)
            {
                switch (_selectedCategory)
                {
                    case StickerHelper.CATEGORY_DOGGY:
                        downloadDialogueImage.Source = UI_Utils.Instance.DoggyOverlay;
                        btnDownload.Content = AppResources.Installed_Txt;
                        btnDownload.IsHitTestVisible = false;
                        break;
                    case StickerHelper.CATEGORY_KITTY:
                        downloadDialogueImage.Source = UI_Utils.Instance.KittyOverlay;
                        break;
                    case StickerHelper.CATEGORY_BOLLYWOOD:
                        downloadDialogueImage.Source = UI_Utils.Instance.BollywoodOverlay;
                        break;
                    case StickerHelper.CATEGORY_EXPRESSIONS:
                        downloadDialogueImage.Source = UI_Utils.Instance.ExpressionsOverlay;
                        break;
                    case StickerHelper.CATEGORY_TROLL:
                        downloadDialogueImage.Source = UI_Utils.Instance.TrollOverlay;
                        break;
                }
                overlayRectangle.Tap += overlayRectangle_Tap_1;
                overlayRectangle.Visibility = Visibility.Visible;
                gridDownloadStickers.Visibility = Visibility.Visible;
                llsMessages.IsHitTestVisible = bottomPanel.IsHitTestVisible = false;
            }
            else
            {
                overlayRectangle.Tap -= overlayRectangle_Tap_1;
                if (btnDownload.IsHitTestVisible == false)
                {
                    btnDownload.IsHitTestVisible = true;
                    btnDownload.Content = AppResources.Download_txt;
                    App.appSettings.Remove(HikeConstants.AppSettings.SHOW_DOGGY_OVERLAY);
                }
                overlayRectangle.Visibility = Visibility.Collapsed;
                gridDownloadStickers.Visibility = Visibility.Collapsed;
                llsMessages.IsHitTestVisible = bottomPanel.IsHitTestVisible = true;
            }
        }

        private void downloadStickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowDownloadOverlay(false);
            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
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

        private void CreateStickerCategoriesPallete()
        {
            StickerCategory stickerCategory;
            //done thos way to maintain order of insertion
            if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_DOGGY)) != null)
            {
                if (stickerCategory.HasNewMessages)
                    stCategory1.BorderThickness = newCategoryThickness;
            }

            if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_KITTY)) != null)
            {
                stCategory2.Visibility = Visibility.Visible;
                if (stickerCategory.HasNewMessages)
                    stCategory2.BorderThickness = newCategoryThickness;
            }

            if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_EXPRESSIONS)) != null)
            {
                stCategory3.Visibility = Visibility.Visible;
                if (stickerCategory.HasNewMessages)
                    stCategory3.BorderThickness = newCategoryThickness;
            }

            if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_BOLLYWOOD)) != null
                && Utils.IsBollywoodVisible)
            {
                stCategory4.Visibility = Visibility.Visible;
                if (stickerCategory.HasNewMessages)
                    stCategory4.BorderThickness = newCategoryThickness;
                ColumnDefinition colDef = new ColumnDefinition();
                gridStickerPivot.ColumnDefinitions.Add(colDef);
                stCategory5.SetValue(Grid.ColumnProperty, 5);
            }
            else
                stCategory5.SetValue(Grid.ColumnProperty, 4);

            if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_TROLL)) != null)
            {
                stCategory5.Visibility = Visibility.Visible;
                if (stickerCategory.HasNewMessages)
                    stCategory5.BorderThickness = newCategoryThickness;
            }

        }
        private void CreateStickerPivot()
        {
            StickerPivotHelper.Instance.InitialiseStickerPivot(Stickers_Tap);
            pivotStickers = StickerPivotHelper.Instance.StickerPivot;
            pivotStickers.SelectionChanged += PivotStickers_SelectionChanged;
            pivotStickers.Height = 240;
            pivotStickers.SetValue(Grid.RowProperty, 0);
            gridStickers.Children.Add(pivotStickers);
        }
        #endregion

        #region Walkie Talkie

        private void Record_ActionIconTapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.HideToolTip(LayoutRoot, 2);

            App.ViewModel.HideToolTip(LayoutRoot, 1);

            attachmentMenu.Visibility = Visibility.Collapsed;
            emoticonPanel.Visibility = Visibility.Collapsed;

            this.Focus(); // remove focus from textbox
            recordGrid.Visibility = Visibility.Visible;
            sendMsgTxtbox.Visibility = Visibility.Collapsed;

            if (this.ApplicationBar != null)
                (this.ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

            recordButtonGrid.Background = gridBackgroundBeforeRecording;
            recordButton.Text = HOLD_AND_TALK;
            recordButton.Foreground = UI_Utils.Instance.GreyTextForeGround;
            walkieTalkieImage.Source = UI_Utils.Instance.WalkieTalkieGreyImage;
        }

        void Hold_To_Record(object sender, System.Windows.Input.ManipulationStartedEventArgs e)
        {
            if (mediaElement != null)
            {
                if (currentAudioMessage != null && currentAudioMessage.IsPlaying)
                {
                    currentAudioMessage.IsPlaying = false;
                    mediaElement.Pause();
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
            recordButton.Foreground = UI_Utils.Instance.WhiteTextForeGround;
            recordButtonGrid.Background = UI_Utils.Instance.HikeMsgBackground;
            walkieTalkieImage.Source = UI_Utils.Instance.WalkieTalkieWhiteImage;
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

            deleteRecImage.Source = UI_Utils.Instance.DustbinGreyImage;
            deleteRecText.Foreground = UI_Utils.Instance.DeleteGreyBackground;

            cancelRecord.Opacity = 1;
            deleteBorder.BorderBrush = UI_Utils.Instance.BlackBorderBrush;
            WalkieTalkieGrid.Visibility = Visibility.Collapsed;
            recordButton.Text = HOLD_AND_TALK;
            recordButton.Foreground = UI_Utils.Instance.GreyTextForeGround;
            recordButtonGrid.Background = gridBackgroundBeforeRecording;
            walkieTalkieImage.Source = UI_Utils.Instance.WalkieTalkieGreyImage;

            deleteBorder.Background = UI_Utils.Instance.DeleteBlackBackground;

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
                    _deleteTimer = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 1) };
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
            deleteBorder.BorderBrush = UI_Utils.Instance.BlackBorderBrush;
            deleteRecImage.Source = UI_Utils.Instance.DustbinGreyImage;
            deleteRecText.Foreground = UI_Utils.Instance.DeleteGreyBackground;
        }

        void deleteRecImage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _isWalkieTalkieMessgeDelete = true;
            deleteBorder.BorderBrush = UI_Utils.Instance.RedBorderBrush;
            deleteRecImage.Source = UI_Utils.Instance.DustbinWhiteImage;
            deleteRecText.Foreground = UI_Utils.Instance.WhiteTextForeGround;
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
            maxPlayingTime.Text = " / " + formatTime(HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED);
            sendIconButton.IsEnabled = false;
            _recorderState = RecorderState.RECORDING;
        }

        void showWalkieTalkieProgress(object sender, EventArgs e)
        {
            runningTime.Text = formatTime(_runningSeconds + 1);

            if (_runningSeconds >= HikeConstants.MAX_AUDIO_RECORDTIME_SUPPORTED)
            {
                cancelRecord.Opacity = 1;
                deleteBorder.BorderBrush = UI_Utils.Instance.BlackBorderBrush;
                WalkieTalkieGrid.Visibility = Visibility.Collapsed;
                recordButton.Text = HOLD_AND_TALK;
                recordButton.Foreground = UI_Utils.Instance.GreyTextForeGround;
                recordButtonGrid.Background = gridBackgroundBeforeRecording;
                walkieTalkieImage.Source = UI_Utils.Instance.WalkieTalkieGreyImage;
                deleteRecImage.Source = UI_Utils.Instance.DustbinGreyImage;
                deleteRecText.Foreground = UI_Utils.Instance.DeleteGreyBackground;
                deleteBorder.Background = UI_Utils.Instance.DeleteBlackBackground;

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
                runningTime.Text = formatTime(0);
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
                    AudioFileTransfer();
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

        private string formatTime(int seconds)
        {
            int minute = seconds / 60;
            int secs = seconds % 60;
            return minute.ToString("00") + ":" + secs.ToString("00");
        }

        private readonly SolidColorBrush gridBackgroundBeforeRecording = new SolidColorBrush(Colors.Black);
        private Microphone _microphone = Microphone.Default;     // Object representing the physical microphone on the device
        private byte[] _buffer;                                  // Dynamic buffer to retrieve audio data from the microphone
        private MemoryStream _stream = new MemoryStream();       // Stores the audio data for later playback
        private bool isRecordingForceStop = false;
        private DispatcherTimer _progressTimer;
        private DispatcherTimer _deleteTimer;
        private int _runningSeconds = 0;

        // Status images
        private TimeSpan _duration;

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

        void StartForceSMSTimer(bool isNewTimer)
        {
            if (!isOnHike || !IsSMSOptionValid || _isSendAllAsSMSVisible)
                return;

            try
            {
                lastUnDeliveredMessage = (from message in ocMessages
                                          where message.MessageStatus == ConvMessage.State.SENT_CONFIRMED
                                          select message).Last();

                if (lastUnDeliveredMessage != null)
                {
                    TimeSpan ts;

                    if (isNewTimer)
                    {
                        ts = TimeSpan.FromSeconds(20);
                    }
                    else
                    {
                        long ticks = lastUnDeliveredMessage.Timestamp * 10000000;
                        ticks += DateTime.Parse("01/01/1970 00:00:00").Ticks;
                        DateTime receivedTime = new DateTime(ticks);
                        receivedTime = receivedTime.ToLocalTime();
                        ts = DateTime.Now.Subtract(receivedTime);
                        ts = ts.TotalSeconds > 20 ? TimeSpan.FromMilliseconds(10) : ts.TotalSeconds > 0 ? ts : TimeSpan.FromMilliseconds(10);
                    }

                    if (ts.TotalSeconds > 0 || isNewTimer)
                    {
                        if (_forceSMSTimer == null)
                            _forceSMSTimer = new DispatcherTimer();
                        else
                            _forceSMSTimer.Stop();

                        _forceSMSTimer.Interval = ts;

                        _forceSMSTimer.Tick -= _forceSMSTimer_Tick;
                        _forceSMSTimer.Tick += _forceSMSTimer_Tick;

                        _forceSMSTimer.Start();
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


        void _forceSMSTimer_Tick(object sender, EventArgs e)
        {
            _forceSMSTimer.Tick -= _forceSMSTimer_Tick;

            if (!_isShownOnUI)
            {
                _isShownOnUI = true;

                _forceSMSTimer.Stop();

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ShowForceSMSOnUI();
                    });
            }
        }

        void ShowForceSMSOnUI()
        {
            if (_isSendAllAsSMSVisible)
                return;

            lastUnDeliveredMessage = null;

            try
            {
                lastUnDeliveredMessage = (from message in ocMessages
                                          where message.MessageStatus == ConvMessage.State.SENT_CONFIRMED
                                          select message).Last();
            }
            catch
            {
                _isShownOnUI = false;
                return;
            }

            if (lastUnDeliveredMessage != null)
            {
                if (tap2SendAsSMSMessage == null)
                {
                    tap2SendAsSMSMessage = new ConvMessage();
                    tap2SendAsSMSMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION;
                    tap2SendAsSMSMessage.NotificationType = ConvMessage.MessageType.FORCE_SMS;

                    if (isGroupChat)
                        tap2SendAsSMSMessage.Message = AppResources.Send_All_As_SMS_Group;
                    else
                        tap2SendAsSMSMessage.Message = String.Format(AppResources.Send_All_As_SMS, mContactName);
                }

                var indexToInsert = ocMessages.IndexOf(lastUnDeliveredMessage) + 1;
                this.ocMessages.Insert(indexToInsert, tap2SendAsSMSMessage);

                if (indexToInsert == ocMessages.Count - 1)
                    ScrollToBottom();

                _isSendAllAsSMSVisible = true;
            }

            _isShownOnUI = false;
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

        private void overlayRectangle_Tap_1(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ShowDownloadOverlay(false);
            StickerCategory s2 = HikeViewModel.stickerHelper.GetStickersByCategory(_selectedCategory);
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

        private void TipDismiss_Tap(object sender, System.Windows.Input.GestureEventArgs e) // invoked for status update tooltip #4
        {
            if (_toolTipMessage != null)
                this.ocMessages.Remove(_toolTipMessage);

            App.ViewModel.HideToolTip(null, 4);
        }

        void UpdateLastSeenOnUI(string status, bool showTip = false)
        {
            //Update UI
            Deployment.Current.Dispatcher.BeginInvoke(new Action<string, bool>(delegate(string lastSeenStatus, bool isShowTip)
            {
                if (App.newChatThreadPage != null)
                {
                    lastSeenTxt.Text = lastSeenStatus;
                    onlineStatus.Visibility = Visibility.Visible;
                    userName.FontSize = 36;
                    lastSeenPannel.Visibility = Visibility.Visible;

                    if (isShowTip)
                        App.ViewModel.DisplayTip(LayoutRoot, 5);
                }
            }), status, showTip);
        }

        private void ChatMessageSelected(object sender, SelectionChangedEventArgs e)
        {
            ConvMessage msg = llsMessages.SelectedItem as ConvMessage;

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
                                _isShownOnUI = false;

                                SendForceSMS();

                                if (lastUnDeliveredMessage != null)
                                    ocMessages.Remove(tap2SendAsSMSMessage);
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
                        FileAttachmentMessage_Tap(sender, e); // normal flow if all are sent files and send all sms option is not visible
                }
                else
                    FileAttachmentMessage_Tap(sender, e); // normal flow for recieved files
            }
        }
    }

    public class ChatThreadTemplateSelector : TemplateSelector
    {
        #region Properties

        public DataTemplate DtInAppTip
        {
            get;
            set;
        }

        public DataTemplate DtForceSMSNotification
        {
            get;
            set;
        }

        public DataTemplate DtNotificationBubble
        {
            get;
            set;
        }

        public DataTemplate DtTypingNotificationBubble
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleLocation
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleText
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleFile
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleAudioFile
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleNudge
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleContact
        {
            get;
            set;
        }

        public DataTemplate DtRecievedSticker
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleText
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleFile
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleLocation
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleAudioFile
        {
            get;
            set;
        }
        public DataTemplate DtSentBubbleNudge
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleContact
        {
            get;
            set;
        }

        public DataTemplate DtStatusUpdateBubble
        {
            get;
            set;
        }

        public DataTemplate DtSentSticker
        {
            get;
            set;
        }

        #endregion

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Determine which template to return;
            ConvMessage convMesssage = (ConvMessage)item;
            if (App.newChatThreadPage != null)
            {
                if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    if (convMesssage.IsSent)
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                            return DtSentBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtSentSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                            return DtSentBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                            return DtSentBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            return DtSentBubbleLocation;
                        else if (convMesssage.FileAttachment != null)
                            return DtSentBubbleFile;
                        else
                            return DtSentBubbleText;
                    }
                    else
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                            return DtRecievedBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtRecievedSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                            return DtRecievedBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                            return DtRecievedBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            return DtRecievedBubbleLocation;
                        else if (convMesssage.FileAttachment != null)
                            return DtRecievedBubbleFile;
                        else
                            return DtRecievedBubbleText;
                    }
                }
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.IN_APP_TIP)
                    return DtInAppTip;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION)
                    return DtForceSMSNotification;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                    return DtStatusUpdateBubble;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.TYPING_NOTIFICATION)
                    return DtTypingNotificationBubble;
                else
                    return DtNotificationBubble;
            }
            else
                return (new DataTemplate());
        }
    }
}
