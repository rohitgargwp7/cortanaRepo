using System.ComponentModel;
using windows_client.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Reactive;
using System;
using windows_client.DbUtils;
using windows_client.Controls;
using windows_client.View;
using Microsoft.Phone.Controls;
using windows_client.Controls.StatusUpdate;
using System.Diagnostics;
using windows_client.Languages;
using windows_client.utils;


namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged, HikePubSub.Listener
    {
        // this key will track number of conversations in the app. If this does not match the convs at load time , simply move to backup plan.
        public static string NUMBER_OF_CONVERSATIONS = "NoConvs";

        public static string NUMBER_OF_FAVS = "NoFavs";

        private Dictionary<string, ConversationListObject> _pendingReq = null;

        private ObservableCollection<ConversationListObject> _favList = null;

        private ObservableCollection<StatusUpdateBox> _statusList = new ObservableCollection<StatusUpdateBox>();

        public ObservableCollection<StatusUpdateBox> StatusList
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
            _pendingReq = new Dictionary<string, ConversationListObject>();
            _favList = new ObservableCollection<ConversationListObject>();

            List<ConversationListObject> listConversationBox = new List<ConversationListObject>();
            // this order should be maintained as _convMap should be populated before loading fav list
            for (int i = 0; i < convList.Count; i++)
            {
                ConversationListObject convListObj = convList[i];
                _convMap[convListObj.Msisdn] = convListObj;
                listConversationBox.Add(convListObj);
            }
            _messageListPageCollection = new ObservableCollection<ConversationListObject>(listConversationBox);
            MiscDBUtil.LoadFavourites(_favList, _convMap);
            int count = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            if (count != _favList.Count) // values are not loaded, move to backup plan
            {
                _favList.Clear();
                MiscDBUtil.LoadFavouritesFromIndividualFiles(_favList, _convMap);
            }
            RegisterListeners();
        }

        public HikeViewModel()
        {
            _messageListPageCollection = new ObservableCollection<ConversationListObject>();
            _convMap = new Dictionary<string, ConversationListObject>();
            _favList = new ObservableCollection<ConversationListObject>();
            _pendingReq = new Dictionary<string, ConversationListObject>();
            MiscDBUtil.LoadFavourites(_favList, _convMap);
            int count = 0;
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
            if (count != _favList.Count) // values are not loaded, move to backup plan
            {
                _favList.Clear();
                MiscDBUtil.LoadFavouritesFromIndividualFiles(_favList, _convMap);
            }
            RegisterListeners();
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
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
            App.HikePubSubInstance.addListener(HikePubSub.BLOCK_USER, this);
        }

        private void RemoveListeners()
        {
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
            App.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
        }

        public void onEventReceived(string type, object obj)
        {

            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        object[] vals = (object[])obj;
                        ConversationListObject mObj = (ConversationListObject)vals[1];
                        if (mObj == null)
                            return;
                        App.ViewModel.MessageListPageCollection.Remove(mObj);
                        App.ViewModel.ConvMap[mObj.Msisdn] = mObj;
                        App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                    });
            }
            #endregion
            #region USER_LEFT USER_JOINED
            else if ((HikePubSub.USER_LEFT == type) || (HikePubSub.USER_JOINED == type))
            {
                string msisdn = (string)obj;
                try
                {
                    ConversationListObject convObj = App.ViewModel.ConvMap[msisdn];
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
        }

        private Dictionary<string, ContactInfo> _contactsCache = new Dictionary<string, ContactInfo>();
        public Dictionary<string, ContactInfo> ContactsCache
        {
            get
            {
                return _contactsCache;
            }
        }

        public List<ToolTip> TipList = new List<ToolTip>();

        void LoadTips()
        {
            byte marked;
            App.appSettings.TryGetValue(App.TIP_MARKED_KEY, out marked);

            if (marked == null)
            {
                marked = (byte)0;
                App.WriteToIsoStorageSettings(App.TIP_MARKED_KEY, marked);
            }

            bool isShownVal = (marked & 0x01) == 1;
            TipList.Add(new ToolTip() { Tip = "A", IsShown = isShownVal, IsTop = true, TipMargin = new Thickness(10, 0, 0, 0), FullTipMargin = new Thickness(20, 30, 20, 0) });
            isShownVal = (marked & 0x02) == 1;
            TipList.Add(new ToolTip() { Tip = "B", IsShown = isShownVal, IsTop = false, TipMargin = new Thickness(10, 0, 0, 0), FullTipMargin = new Thickness(20, 30, 20, 0) });
            isShownVal = (marked & 0x04) == 1;
            TipList.Add(new ToolTip() { Tip = "C", IsShown = isShownVal, IsTop = true, TipMargin = new Thickness(10, 0, 0, 0), FullTipMargin = new Thickness(20, 30, 20, 0) });
            isShownVal = (marked & 0x08) == 1;
            TipList.Add(new ToolTip() { Tip = "D", IsShown = isShownVal, IsTop = true, TipMargin = new Thickness(10, 0, 0, 0), FullTipMargin = new Thickness(20, 30, 20, 0) });
            isShownVal = (marked & 0x10) == 1;
            TipList.Add(new ToolTip() { Tip = "E", IsShown = isShownVal, IsTop = true, TipMargin = new Thickness(10, 0, 0, 0), FullTipMargin = new Thickness(20, 30, 20, 0) });
        }

        public void RemoveFrndReqFromTimeline(string msisdn, FriendsTableUtils.FriendStatusEnum friendStatus)
        {
            if (friendStatus == FriendsTableUtils.FriendStatusEnum.FRIENDS)
            {
                StatusMessage sm = new StatusMessage(msisdn, AppResources.Now_Friends_Txt, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1);
                App.HikePubSubInstance.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
                App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
            }
            foreach (StatusUpdateBox sb in App.ViewModel.StatusList)
            {
                if (sb is FriendRequestStatus)
                {
                    if (sb.Msisdn == msisdn)
                    {
                        App.ViewModel.StatusList.Remove(sb);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
