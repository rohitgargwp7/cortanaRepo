using System.ComponentModel;
using windows_client.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Reactive;
using System;
using windows_client.DbUtils;
using windows_client.View;
using Microsoft.Phone.Controls;
using System.Diagnostics;
using windows_client.Languages;
using windows_client.utils;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using System.Device.Location;
using Newtonsoft.Json.Linq;
using System.Linq;
using Microsoft.Phone.Shell;
using System.Windows.Documents;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Media;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Microsoft.Phone.Net.NetworkInformation;
using Coding4Fun.Phone.Controls;
using System.Windows.Media;
using FileTransfer;
using CommonLibrary.Constants;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged, HikePubSub.Listener
    {
        // this key will track number of conversations in the app. If this does not match the convs at load time , simply move to backup plan.
        public static string NUMBER_OF_CONVERSATIONS = "NoConvs";

        public static string NUMBER_OF_FAVS = "NoFavs";

        static StickerHelper _stickerHelper;
        public static StickerHelper StickerHelper
        {
            get
            {
                if (_stickerHelper == null)
                    _stickerHelper = new StickerHelper();

                return _stickerHelper;
            }
            private set
            {
                _stickerHelper = value;
            }
        }

        private Dictionary<string, ConversationListObject> _pendingReq = null;

        private ObservableCollection<ConversationListObject> _favList = null;

        ObservableCollection<BaseStatusUpdate> _statusList = new ObservableCollection<BaseStatusUpdate>();
        public ObservableCollection<BaseStatusUpdate> StatusList
        {
            get
            {
                return _statusList;
            }
        }

        private Dictionary<string, ConversationListObject> _convMap;

        public Dictionary<string, ConversationListObject> ConvMap
        {
            get
            {
                return _convMap;
            }
            set
            {
                if (value != _convMap)
                    _convMap = value;
            }
        }

        private ObservableCollection<ConversationListObject> _messageListPageCollection;

        public ObservableCollection<ConversationListObject> MessageListPageCollection
        {
            get
            {
                return _messageListPageCollection;
            }
            set
            {
                _messageListPageCollection = value;
                NotifyPropertyChanged("MessageListPageCollection");
            }
        }

        private ConversationsList conversationListPage;

        public ConversationsList ConversationListPage
        {
            get
            {
                return conversationListPage;
            }
            set
            {
                if (value != conversationListPage)
                    conversationListPage = value;
            }
        }

        private HashSet<string> _blockedHashSet = null;

        private object readWriteLock = new object();

        private bool isBlockedSetLoaded = false;

        public HashSet<string> BlockedHashset
        {
            get
            {
                // optimization to avoid lock acquire again and again
                if (isBlockedSetLoaded)
                    return _blockedHashSet;
                lock (readWriteLock)
                {
                    if (_blockedHashSet == null)
                    {
                        _blockedHashSet = new HashSet<string>();
                        List<Blocked> blockList = UsersTableUtils.getBlockList();
                        if (blockList != null)
                        {
                            for (int i = 0; i < blockList.Count; i++)
                                _blockedHashSet.Add(blockList[i].Msisdn);
                        }
                        isBlockedSetLoaded = true;
                    }
                    return _blockedHashSet;
                }
            }
        }

        /// <summary>
        /// use this function to clear blocklist rather than BlockedHashset.clear()
        /// </summary>
        public void ClearBLockedHashSet()
        {
            lock (readWriteLock)
            {
                if (_blockedHashSet != null)
                {
                    _blockedHashSet.Clear();
                    _blockedHashSet = null;
                }
                isBlockedSetLoaded = false;
            }
        }
        public bool IsPendingListLoaded
        {
            get;
            set;
        }

        public Dictionary<string, ConversationListObject> PendingRequests
        {
            get
            {
                if (IsPendingListLoaded)
                    return _pendingReq;
                LoadPendingRequests();
                return _pendingReq;
            }
        }

        public void LoadPendingRequests()
        {
            try
            {
                MiscDBUtil.LoadPendingRequests(_pendingReq);
            }
            catch (Exception e)
            {
                Debug.WriteLine("HikeViewModel :: LoadPendingRequests : Exception : " + e.StackTrace);
            }
        }

        public IScheduler scheduler = Scheduler.NewThread;

        public ObservableCollection<ConversationListObject> FavList
        {
            get
            {
                return _favList;
            }
        }

        public HikeViewModel(List<ConversationListObject> convList)
        {
            _convMap = new Dictionary<string, ConversationListObject>(convList.Count);

            List<ConversationListObject> listConversationBox = new List<ConversationListObject>();
            // this order should be maintained as _convMap should be populated before loading fav list
            for (int i = 0; i < convList.Count; i++)
            {
                ConversationListObject convListObj = convList[i];
                _convMap[convListObj.Msisdn] = convListObj;
                listConversationBox.Add(convListObj);
            }
            _messageListPageCollection = new ObservableCollection<ConversationListObject>(listConversationBox);

            LoadViewModelObjects();
        }

        public HikeViewModel()
        {
            _messageListPageCollection = new ObservableCollection<ConversationListObject>();
            _convMap = new Dictionary<string, ConversationListObject>();

            LoadViewModelObjects();
        }

        private void LoadViewModelObjects()
        {
            _pendingReq = new Dictionary<string, ConversationListObject>();
            _favList = new ObservableCollection<ConversationListObject>();

            MiscDBUtil.LoadFavourites(_favList, _convMap);
            int count = 0;
            HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);

            if (count != _favList.Count) // values are not loaded, move to backup plan
            {
                _favList.Clear();
                MiscDBUtil.LoadFavouritesFromIndividualFiles(_favList, _convMap);
            }

            RegisterListeners();

            LoadCurrentLocation();
            ClearTempTransferData();

            MiscDBUtil.LoadPendingUploadPicRequests();

            ChatBackgroundHelper.Instance.Instantiate();
            FileTransferManager.Instance.PopulatePreviousTasks();
            FileTransferManager.Instance.TriggerPubSub += Instance_TriggerPubSub;

            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.BLACK_THEME))
                IsDarkMode = true;

            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.HIDDEN_MODE_ACTIVATED))
                IsHiddenModeActive = true;
        }

        void Instance_TriggerPubSub(object sender, FileTransferSatatusChangedEventArgs e)
        {
            if (e.IsStateChanged)
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.FILE_STATE_CHANGED, e.FileInfo);
        }

        /// <summary>
        /// called on app start, app resume from tombstone state and when user turn on location sharing in app settings
        /// </summary>
        public void LoadCurrentLocation()
        {
            if (!HikeInstantiation.AppSettings.Contains(AppSettingsKeys.USE_LOCATION_SETTING) && !HikeInstantiation.AppSettings.Contains(AppSettingsKeys.LOCATION_DEVICE_COORDINATE))
            {
                BackgroundWorker getCoordinateWorker = new BackgroundWorker();

                getCoordinateWorker.DoWork += async delegate
                {
                    Geolocator geolocator = new Geolocator();
                    if (geolocator.LocationStatus == PositionStatus.Disabled)
                        return;

                    geolocator.DesiredAccuracyInMeters = 10;
                    geolocator.MovementThreshold = 5;
                    geolocator.DesiredAccuracy = PositionAccuracy.High;

                    IAsyncOperation<Geoposition> locationTask = geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5));

                    try
                    {
                        Geoposition currentPosition = await locationTask;

                        var latitutde = Math.Round(currentPosition.Coordinate.Latitude, 6);
                        var longitute = Math.Round(currentPosition.Coordinate.Longitude, 6);
                        var newCoordinate = new GeoCoordinate(latitutde, longitute);

                        HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.LOCATION_DEVICE_COORDINATE, newCoordinate);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Location exception GetCurrentCoordinate HikeViewModel : " + ex.StackTrace);
                    }
                    finally
                    {
                        if (locationTask != null)
                        {
                            if (locationTask.Status == AsyncStatus.Started)
                                locationTask.Cancel();

                            locationTask.Close();
                        }

                    }
                };

                getCoordinateWorker.RunWorkerAsync();
            }
        }

        public async void ClearTempTransferData()
        {
            await Task.Delay(1);
            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (store.DirectoryExists(FTBasedConstants.FILE_TRANSFER_TEMP_LOCATION))
                    {
                        string[] fileNames = store.GetFileNames(FTBasedConstants.FILE_TRANSFER_TEMP_LOCATION + "/*");
                        foreach (string fileName in fileNames)
                        {
                            store.DeleteFile(FTBasedConstants.FILE_TRANSFER_TEMP_LOCATION + "/" + fileName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception,ViewModel:ClearTempTransferData,Message:{0}, Stacktrace:{1}", ex.Message, ex.StackTrace);
            }

        }

        public bool Isfavourite(string mContactNumber)
        {
            if (_favList.Count == 0)
                return false;
            for (int i = 0; i < _favList.Count; i++)
            {
                if (_favList[i].Msisdn == mContactNumber)
                    return true;
            }
            return false;
        }

        public ConversationListObject GetFav(string mContactNumber)
        {
            if (_favList.Count == 0)
                return null;
            for (int i = 0; i < _favList.Count; i++)
            {
                if (_favList[i].Msisdn == mContactNumber)
                    return _favList[i];
            }
            return null;
        }

        public bool IsPending(string ms)
        {
            if (!IsPendingListLoaded)
            {
                MiscDBUtil.LoadPendingRequests(_pendingReq);
            }
            if (_pendingReq == null)
                return false;
            if (_pendingReq.ContainsKey(ms))
                return true;
            return false;
        }

        public ConversationListObject GetPending(string mContactNumber)
        {
            if (_pendingReq.Count == 0)
                return null;
            if (_pendingReq.ContainsKey(mContactNumber))
                return _pendingReq[mContactNumber];
            return null;
        }

        private void RegisterListeners()
        {
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.BLOCK_USER, this);
            HikeInstantiation.HikePubSubInstance.addListener(HikePubSub.TYPING_CONVERSATION, this);
        }

        private void RemoveListeners()
        {
            HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
            HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
            HikeInstantiation.HikePubSubInstance.removeListener(HikePubSub.TYPING_CONVERSATION, this);
        }

        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        object[] vals = (object[])obj;

                        bool showPush = true;
                        if (vals.Length == 3 && vals[2] is bool)
                            showPush = (Boolean)vals[2];

                        ConversationListObject mObj = (ConversationListObject)vals[1];
                        if (mObj == null)
                            return;

                        HikeInstantiation.ViewModel.ConvMap[mObj.Msisdn] = mObj;
                        int index = HikeInstantiation.ViewModel.MessageListPageCollection.IndexOf(mObj);

                        if (index < 0)//not present in oc
                        {
                            HikeInstantiation.ViewModel.MessageListPageCollection.Insert(0, mObj);
                        }
                        else if (index > 0)
                        {
                            HikeInstantiation.ViewModel.MessageListPageCollection.RemoveAt(index);
                            HikeInstantiation.ViewModel.MessageListPageCollection.Insert(0, mObj);
                        }//if already at zero, do nothing

                        if (showPush &&
                            ((HikeInstantiation.NewChatThreadPageObj == null && mObj.IsHidden && !IsHiddenModeActive)
                            || (HikeInstantiation.NewChatThreadPageObj != null && HikeInstantiation.NewChatThreadPageObj.mContactNumber != mObj.Msisdn)))
                        {
                            if (mObj.IsMute) // of msg is for muted forwardedMessage, ignore msg
                                return;

                            ToastPrompt toast = new ToastPrompt();
                            toast.Tag = mObj.Msisdn;

                            if (mObj.IsHidden)
                                toast.Title = String.Empty;
                            else
                                toast.Title = (mObj.ContactName != null ? mObj.ContactName : mObj.Msisdn) + (mObj.IsGroupChat ? " :" : " -");

                            // Cannot use convMesssage.Message or CObj.LAstMessage because for gc it does not have group member name.
                            toast.Message = mObj.ToastText;
                            toast.Foreground = UI_Utils.Instance.White;
                            toast.Background = (SolidColorBrush)App.Current.Resources["InAppToastBgBrush"];
                            toast.ImageSource = UI_Utils.Instance.HikeToastImage;
                            toast.VerticalContentAlignment = VerticalAlignment.Center;
                            toast.MaxHeight = 60;
                            toast.Tap += HikeInstantiation.ViewModel.Toast_Tap;
                            toast.Show();
                        }
                    });
            }
            #endregion
            #region USER_LEFT USER_JOINED
            else if ((HikePubSub.USER_LEFT == type) || (HikePubSub.USER_JOINED == type))
            {
                string msisdn = (string)obj;
                try
                {
                    ConversationListObject convObj = HikeInstantiation.ViewModel.ConvMap[msisdn];
                    convObj.IsOnhike = HikePubSub.USER_JOINED == type;
                }

                catch (Exception ex)
                {
                    Debug.WriteLine("HikeViewModel:: onEventReceived, Exception : " + ex.StackTrace);
                }
            }
            #endregion
            #region BLOCK_USER
            else if ((HikePubSub.BLOCK_USER == type))
            {
                try
                {
                    string msisdn = null;
                    if (obj is ContactInfo)
                        msisdn = (obj as ContactInfo).Msisdn;
                    else
                        msisdn = (string)obj;

                    if (!IsPendingListLoaded)
                        LoadPendingRequests();
                    #region handle pending request
                    if (_pendingReq != null && _pendingReq.Remove(msisdn))
                    {
                        try
                        {
                            MiscDBUtil.SavePendingRequests();
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("HikeViewModel :: SavePendingRequests : Exception : " + e.StackTrace);
                        }
                    }
                    #endregion
                }
                catch (Exception e)
                {
                    Debug.WriteLine("HikeViewModel :: OnEventReceived : BLOCK USER , Exception : ", e.StackTrace);
                }
            }
            #endregion
            #region START TYPING NOTIFICATION
            else if (type == HikePubSub.TYPING_CONVERSATION)
            {
                object[] vals = (object[])obj;

                if (ShowTypingNotification != null)
                    ShowTypingNotification(null, vals);

                TypingNotification tn = new TypingNotification(vals);
                scheduler.Schedule(tn.AutoHideAfterTyping, TimeSpan.FromSeconds(HikeConstants.TYPING_NOTIFICATION_AUTOHIDE));
            }
            #endregion
        }

        public event EventHandler<Object[]> ShowTypingNotification;
        public event EventHandler<Object[]> AutohideTypingNotification;

        public void CallAutohide(object[] vals)
        {
            if (AutohideTypingNotification != null)
            {
                AutohideTypingNotification(null, vals);
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

        public void ClearViewModel()
        {
            if (_pendingReq != null)
                _pendingReq.Clear();
            if (_favList != null)
                _favList.Clear();
            if (_messageListPageCollection != null)
                _messageListPageCollection.Clear();
            if (_convMap != null)
                _convMap.Clear();
            if (_statusList != null)
                _statusList.Clear();
            StickerHelper = null;
        }

        private Dictionary<string, ContactInfo> _contactsCache = new Dictionary<string, ContactInfo>();
        public Dictionary<string, ContactInfo> ContactsCache
        {
            get
            {
                return _contactsCache;
            }
        }

        public void UpdateNameOnSaveContact(ContactInfo contactInfo)
        {
            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(contactInfo.Msisdn)) // update convlist
            {
                try
                {
                    var cObj = ConvMap[contactInfo.Msisdn];
                    cObj.ContactName = contactInfo.Name;
                    ConversationTableUtils.updateConversation(cObj);

                    if (cObj.IsFav)
                    {
                        MiscDBUtil.SaveFavourites(cObj);
                        MiscDBUtil.SaveFavourites();
                    }

                    cObj = HikeInstantiation.ViewModel.GetPending(contactInfo.Msisdn);
                    if (cObj != null)
                    {
                        cObj.ContactName = contactInfo.Name;
                        MiscDBUtil.SavePendingRequests();
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("SAVE CONTACT NAME :: Update contact name exception " + e.StackTrace);
                }
            }
            else // fav and pending case
            {
                ConversationListObject c = HikeInstantiation.ViewModel.GetFav(contactInfo.Msisdn);

                if (c != null) // this user is in favs
                {
                    c.ContactName = contactInfo.Name;
                    MiscDBUtil.SaveFavourites(c);
                    MiscDBUtil.SaveFavourites();
                }
                else
                {
                    c = HikeInstantiation.ViewModel.GetPending(contactInfo.Msisdn);
                    if (c != null)
                    {
                        c.ContactName = contactInfo.Name;
                        MiscDBUtil.SavePendingRequests();
                    }
                }
            }

            ContactUtils.UpdateGroupParticpantsCacheWithContactName(contactInfo.Msisdn, contactInfo.Name);
        }

        /// <summary>
        /// Remove image for deleted contacts on resync.
        /// </summary>
        /// <param name="deletedContacts">deleted contacts</param>
        /// <param name="updatedContacts">added or updated contacts</param>
        public void DeleteImageForDeletedContacts(List<ContactInfo> deletedContacts, List<ContactInfo> updatedContacts)
        {
            if (deletedContacts == null)
                return;

            Dictionary<string, int> deletedContactMap = new Dictionary<string, int>();

            foreach (var contact in deletedContacts)
            {
                if (!deletedContactMap.ContainsKey(contact.Msisdn))
                    deletedContactMap.Add(contact.Msisdn, 0);
            }

            if (updatedContacts != null)
            {
                foreach (var contact in updatedContacts)
                {
                    if (deletedContactMap.ContainsKey(contact.Msisdn))
                        deletedContactMap[contact.Msisdn]++;
                }
            }

            foreach (var msisdn in deletedContactMap.Keys)
            {
                if (deletedContactMap[msisdn] == 0)
                {
                    if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                    {
                        var fStatus = FriendsTableUtils.GetFriendStatus(msisdn);
                        if (fStatus <= FriendsTableUtils.FriendStatusEnum.REQUEST_SENT)
                        {
                            MiscDBUtil.DeleteImageForMsisdn(msisdn);

                            HikeInstantiation.ViewModel.ConvMap[msisdn].Avatar = null;
                            HikeInstantiation.HikePubSubInstance.publish(HikePubSub.UPDATE_PROFILE_ICON, msisdn);
                        }
                    }
                }
            }
        }

        #region ChatBackground

        ChatBackground _lastSelectedBackground;
        public ChatBackground LastSelectedBackground
        {
            get
            {
                return _lastSelectedBackground;
            }
            set
            {
                if (value != _lastSelectedBackground)
                {
                    if (_lastSelectedBackground != null)
                        _lastSelectedBackground.TickImageVisibility = Visibility.Collapsed;

                    _lastSelectedBackground = value;

                    if (_lastSelectedBackground != null)
                        _lastSelectedBackground.TickImageVisibility = Visibility.Visible;
                }
            }
        }

        ChatBackground _selectedBackground;
        public ChatBackground SelectedBackground
        {
            get
            {
                return _selectedBackground;
            }
            set
            {
                if (value != _selectedBackground)
                {
                    if (_selectedBackground != null)
                        _selectedBackground.IsSelected = false;

                    _selectedBackground = value;

                    if (_selectedBackground != null)
                        _selectedBackground.IsSelected = true;
                }
            }
        }

        #endregion

        public void RemoveFrndReqFromTimeline(string msisdn, FriendsTableUtils.FriendStatusEnum friendStatus)
        {
            if (friendStatus == FriendsTableUtils.FriendStatusEnum.FRIENDS)
            {
                StatusMessage sm = new StatusMessage(msisdn, String.Empty, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
                HikeInstantiation.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
            }

            foreach (BaseStatusUpdate sb in HikeInstantiation.ViewModel.StatusList)
            {
                if (sb is FriendRequestStatusUpdate)
                {
                    if (sb.Msisdn == msisdn)
                    {
                        HikeInstantiation.ViewModel.StatusList.Remove(sb);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void ForwardMessage(List<ContactInfo> contactsForForward)
        {
            if (!PhoneApplicationService.Current.State.ContainsKey(HikeConstants.NavigationKeys.FORWARD_MSG))
                return;

            contactsForForward = contactsForForward.Distinct(new ContactInfo.MsisdnComparer()).ToList();

            Analytics.SendAnalyticsEvent(ServerJsonKeys.ST_UI_EVENT, HikeConstants.AnalyticsKeys.FWD_TO_MULTIPLE, contactsForForward.Count);

            if (PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.FORWARD_MSG] is string)
            {
                foreach (var contact in contactsForForward)
                {
                    var msg = (string)PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.FORWARD_MSG];

                    var msisdn = contact.Msisdn;
                    ConvMessage convMessage = new ConvMessage(msg, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsSms = !contact.OnHike;
                    convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;

                    if (HikeInstantiation.NewChatThreadPageObj != null && HikeInstantiation.NewChatThreadPageObj.mContactNumber == msisdn)
                        HikeInstantiation.NewChatThreadPageObj.AddNewMessageToUI(convMessage, false);

                    HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MESSAGE_SENT, convMessage);
                }
            }
            else if (PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.FORWARD_MSG] is object[])
            {
                object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.FORWARD_MSG];
                if (attachmentData.Length == 1)
                {
                    foreach (var contact in contactsForForward)
                    {
                        var msisdn = contact.Msisdn;
                        ConvMessage convMessage = new ConvMessage(AppResources.Sticker_Txt, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                        convMessage.IsSms = !contact.OnHike;
                        convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
                        convMessage.MetaDataString = attachmentData[0] as string;

                        if (HikeInstantiation.NewChatThreadPageObj != null && HikeInstantiation.NewChatThreadPageObj.mContactNumber == msisdn)
                            HikeInstantiation.NewChatThreadPageObj.AddNewMessageToUI(convMessage, false);

                        HikeInstantiation.HikePubSubInstance.publish(HikePubSub.MESSAGE_SENT, convMessage);
                    }
                }
                else
                {
                    string contentType = (string)attachmentData[0];
                    string sourceMsisdn = (string)attachmentData[1];
                    long messageId = (long)attachmentData[2];
                    string metaDataString = (string)attachmentData[3];
                    string sourceFilePath = FTBasedConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn.Replace(":", "_") + "/" + messageId;

                    foreach (var contact in contactsForForward)
                    {
                        var msisdn = contact.Msisdn;
                        ConvMessage convMessage = new ConvMessage(String.Empty, msisdn,
                          TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                        convMessage.IsSms = !contact.OnHike;
                        convMessage.HasAttachment = true;
                        convMessage.FileAttachment = new Attachment();
                        convMessage.FileAttachment.ContentType = contentType;
                        convMessage.FileAttachment.FileKey = (string)attachmentData[4];
                        convMessage.FileAttachment.Thumbnail = (byte[])attachmentData[5];
                        convMessage.FileAttachment.FileName = (string)attachmentData[6];
                        convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;

                        if (contentType.Contains(FTBasedConstants.IMAGE))
                            convMessage.Message = AppResources.Image_Txt;
                        else if (contentType.Contains(FTBasedConstants.AUDIO))
                        {
                            convMessage.Message = AppResources.Audio_Txt;
                            convMessage.MetaDataString = metaDataString;
                        }
                        else if (contentType.Contains(FTBasedConstants.VIDEO))
                            convMessage.Message = AppResources.Video_Txt;
                        else if (contentType.Contains(FTBasedConstants.LOCATION))
                        {
                            convMessage.Message = AppResources.Location_Txt;
                            convMessage.MetaDataString = metaDataString;
                        }
                        else if (contentType.Contains(FTBasedConstants.CT_CONTACT))
                        {
                            convMessage.Message = AppResources.ContactTransfer_Text;
                            convMessage.MetaDataString = metaDataString;
                        }
                        else
                        {
                            convMessage.Message = AppResources.UnknownFile_txt;
                        }

                        if (HikeInstantiation.NewChatThreadPageObj != null && HikeInstantiation.NewChatThreadPageObj.mContactNumber == msisdn)
                            HikeInstantiation.NewChatThreadPageObj.AddNewMessageToUI(convMessage, false);

                        object[] vals = new object[2];
                        vals[0] = convMessage;
                        vals[1] = sourceFilePath;
                        HikeInstantiation.HikePubSubInstance.publish(HikePubSub.FORWARD_ATTACHMENT, vals);
                    }
                }

                PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.FORWARD_MSG);
            }
        }

        public void Hyperlink_Clicked(object[] objArray)
        {
            var regexType = (SmileyParser.RegexType)objArray[0];
            var target = (string)objArray[1];

            if (regexType == SmileyParser.RegexType.EMAIL)
                EmailHelper.SendEmail(string.Empty, string.Empty, target);
            else if (regexType == SmileyParser.RegexType.URL)
            {
                var task = new WebBrowserTask() { URL = target };
                try
                {
                    task.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HikeViewModel:: Hyperlink_Clicked : " + ex.StackTrace);
                }
            }
            else if (regexType == SmileyParser.RegexType.PHONE_NO)
            {
                var phoneCallTask = new PhoneCallTask();
                var targetPhoneNumber = target.Replace("-", String.Empty);
                targetPhoneNumber = targetPhoneNumber.Trim();
                targetPhoneNumber = targetPhoneNumber.Replace(" ", String.Empty);
                phoneCallTask.PhoneNumber = targetPhoneNumber;
                try
                {
                    phoneCallTask.Show();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HikeViewModel:: Hyperlink_Clicked : " + ex.StackTrace);
                }
            }
        }

        public void ViewMoreMessage_Clicked(object obj)
        {
            Hyperlink hp = obj as Hyperlink;
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.VIEW_MORE_MESSAGE_OBJ] = hp.TargetName;
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.Relative));
        }

        public void SendRemoveStealthPacket(ConversationListObject cObj)
        {
            HikePubSub mPubSub = HikeInstantiation.HikePubSubInstance;
            JObject hideObj = new JObject();
            hideObj.Add(ServerJsonKeys.TYPE, ServerJsonKeys.STEALTH);
            JObject data = new JObject();
            JArray msisdn = new JArray();
            msisdn.Add(cObj.Msisdn);
            data.Add(ServerJsonKeys.CHAT_DISABLED, msisdn);
            hideObj.Add(ServerJsonKeys.DATA, data);
            mPubSub.publish(HikePubSub.MQTT_PUBLISH, hideObj);
        }

        #region MULTIPLE IMAGE

        public LruCache<long, BitmapImage> lruMultipleImageCache;

        public BitmapImage GetMftImageCache(Picture pic)
        {
            if (pic == null)
                return null;

            if (lruMultipleImageCache == null)
                lruMultipleImageCache = new LruCache<long, BitmapImage>(50, 0);

            long picKey = pic.Date.Ticks;
            BitmapImage image = lruMultipleImageCache.GetObject(picKey);

            if (image == null)
            {
                image = new BitmapImage();
                image.SetSource(pic.GetThumbnail());
                lruMultipleImageCache.AddObject(picKey, image);
            }
            return image;
        }

        public void ClearMFtImageCache()
        {
            if (lruMultipleImageCache != null)
            {
                lruMultipleImageCache.Clear();
                lruMultipleImageCache = null;
            }
        }
        #endregion

        #region NEW GROUP PIC

        public List<GroupPic> PicUploadList = new List<GroupPic>();
        bool _isUploading = false;

        public void AddGroupPicForUpload(string id)
        {
            if (PicUploadList.Count == 10)
            {
                DeleteImageForMsisdn(id);
                return;
            }

            PicUploadList.Add(new GroupPic(id));
            MiscDBUtil.SavePendingUploadPicRequests();
            SendDisplayPic();
        }

        public void SendDisplayPic()
        {
            if (PicUploadList.Count == 0)
                return;

            if (!NetworkInterface.GetIsNetworkAvailable())
                return;

            if (_isUploading)
                return;

            _isUploading = true;

            var group = PicUploadList[0];
            var buffer = MiscDBUtil.getLargeImageForMsisdn(group.GroupId);
            AccountUtils.updateGroupIcon(buffer, new AccountUtils.postPicUploadResponseFunction(updateProfile_Callback), group);
        }

        private void updateProfile_Callback(JObject obj, GroupPic group)
        {
            _isUploading = false;

            if (obj == null || ServerJsonKeys.OK != (string)obj[ServerJsonKeys.STAT])
            {
                if (group.IsRetried)
                    DeleteGroupImageFromList(group);
                else
                    group.IsRetried = true;
            }
            else
            {
                if (PicUploadList.Contains(group))
                    PicUploadList.Remove(group);
            }

            MiscDBUtil.SavePendingUploadPicRequests();

            if (PicUploadList.Count > 0)
                SendDisplayPic();
        }

        private void DeleteGroupImageFromList(GroupPic group)
        {
            DeleteImageForMsisdn(group.GroupId);

            if (PicUploadList.Contains(group))
                PicUploadList.Remove(group);
        }

        public void DeleteImageForMsisdn(string id)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    HikeInstantiation.ViewModel.ConvMap[id].Avatar = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HikeViewModel :: DeleteGroupImage : remove image from ConvObj, Exception : " + ex.StackTrace);
                }
            });

            MiscDBUtil.DeleteImageForMsisdn(id);
        }

        #endregion

        #region Status Update notification toggle
        public event EventHandler<EventArgs> StatusNotificationsStatusChanged;

        public void StatusNotificationSettingsChanged()
        {
            if (HikeInstantiation.ViewModel.StatusNotificationsStatusChanged != null)
                HikeInstantiation.ViewModel.StatusNotificationsStatusChanged(null, null);
        }

        #endregion

        #region Group Owner Changed
        public event EventHandler<object[]> GroupOwnerChangedEvent;
        public void GroupOwnerChanged(object[] objArray)
        {
            if (HikeInstantiation.ViewModel.GroupOwnerChangedEvent != null)
                HikeInstantiation.ViewModel.GroupOwnerChangedEvent(null, objArray);
        }

        #endregion

        public bool IsConversationUpdated { get; set; }

        #region Request Last Seen

        public event EventHandler<EventArgs> RequestLastSeenEvent;

        public void RequestLastSeen()
        {
            if (RequestLastSeenEvent != null)
                RequestLastSeenEvent(null, null);
        }

        #endregion Request Last Seen

        public void Toast_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ToastPrompt toast = sender as ToastPrompt;
            string msisdn = (string)toast.Tag;
            ConversationListObject co = Utils.GetConvlistObj(msisdn);

            if (co == null)
                return;

            // Return if chat is hidden and hidden mode is not active
            if (co.IsHidden && !IsHiddenModeActive)
                return;

            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.IS_CHAT_RELAUNCH] = true;
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.OBJ_FROM_CONVERSATIONS_PAGE] = co;
            string uri = "/View/NewChatThread.xaml?" + msisdn;

            App page = (App)Application.Current;
            page.RootFrame.Navigate(new Uri(uri, UriKind.Relative));
        }

        public async void UpdateUserImageInStatus(string msisdn)
        {
            await Task.Delay(1);

            foreach (var status in StatusList)
            {
                if (status.Msisdn == msisdn)
                    status.UpdateImage();
            }
        }

        /// <summary>
        /// Is dark theme set for the app.
        /// </summary>
        public Boolean IsDarkMode
        {
            get;
            private set;
        }

        /// <summary>
        /// Check if hidden mode is active. True means hidden chats are visible.
        /// </summary>
        public Boolean IsHiddenModeActive
        {
            get;
            private set;
        }

        /// <summary>
        /// Reset hidden mode.
        /// </summary>
        public void ResetHiddenMode()
        {
            IsHiddenModeActive = false;
            HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.HIDDEN_MODE_ACTIVATED);
        }

        /// <summary>
        /// Toggle hidden mode. Save state in app settings.
        /// </summary>
        public void ToggleHiddenMode()
        {
            IsHiddenModeActive = !IsHiddenModeActive;

            if (IsHiddenModeActive)
                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIDDEN_MODE_ACTIVATED, true);
            else
                HikeInstantiation.RemoveKeyFromAppSettings(AppSettingsKeys.HIDDEN_MODE_ACTIVATED);

            foreach (var conv in MessageListPageCollection)
                conv.HiddenModeToggled();
        }

        /// <summary>
        /// Start reset hidden mode timer on home screen.
        /// </summary>
        public void ResetHiddenModeTapped()
        {
            if (HikeInstantiation.ViewModel.StartResetHiddenModeTimer != null)
                HikeInstantiation.ViewModel.StartResetHiddenModeTimer(null, null);
        }

        public event EventHandler<EventArgs> StartResetHiddenModeTimer;

        public string Password { get; set; }

        public static void ClearStickerHelperInstance()
        {
            StickerHelper = null;
        }

        #region Pause/Resume Background Audio

        public bool resumeMediaPlayerAfterDone = false;

        public void PauseBackgroundAudio()
        {
            if (!MediaPlayer.GameHasControl)
            {
                MediaPlayer.Pause();
                resumeMediaPlayerAfterDone = true;
            }
        }

        public void ResumeBackgroundAudio()
        {
            if (resumeMediaPlayerAfterDone)
            {
                MediaPlayer.Resume();
                resumeMediaPlayerAfterDone = false;
            }
        }

        #endregion
    }
}
