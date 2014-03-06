﻿using System.ComponentModel;
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
using System.Diagnostics;
using System.Windows.Controls;
using windows_client.Languages;
using windows_client.utils;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using System.Device.Location;
using Newtonsoft.Json.Linq;
using System.Linq;
using windows_client.Misc;
using System.Threading;
using Microsoft.Phone.Shell;
using System.Windows.Documents;
using Microsoft.Phone.Tasks;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Media;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged, HikePubSub.Listener
    {
        // this key will track number of conversations in the app. If this does not match the convs at load time , simply move to backup plan.
        public static string NUMBER_OF_CONVERSATIONS = "NoConvs";

        public static string NUMBER_OF_FAVS = "NoFavs";

        public static StickerHelper stickerHelper;

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
            LoadToolTipsDict();
            LoadCurrentLocation();
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
            App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);

            if (count != _favList.Count) // values are not loaded, move to backup plan
            {
                _favList.Clear();
                MiscDBUtil.LoadFavouritesFromIndividualFiles(_favList, _convMap);
            }

            RegisterListeners();

            LoadToolTipsDict();
            LoadCurrentLocation();

            ChatBackgroundHelper.Instance.Instantiate();
            FileTransfers.FileTransferManager.Instance.PopulatePreviousTasks();
        }

        /// <summary>
        /// called on app start, app resume from tombstone state and when user turn on location sharing in app settings
        /// </summary>
        public void LoadCurrentLocation()
        {
            if (!App.appSettings.Contains(App.USE_LOCATION_SETTING) && !App.appSettings.Contains(HikeConstants.LOCATION_DEVICE_COORDINATE))
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

                        App.WriteToIsoStorageSettings(HikeConstants.LOCATION_DEVICE_COORDINATE, newCoordinate);
                    }
                    catch (Exception ex)
                    {
                        // Couldn't get current location - location might be disabled in settings
                        //MessageBox.Show("Location might be disabled", "", MessageBoxButton.OK);
                        Debug.WriteLine("Location exception GetCurrentCoordinate HikeViewModel : " + ex.StackTrace);
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
            App.HikePubSubInstance.addListener(HikePubSub.TYPING_CONVERSATION, this);
            App.HikePubSubInstance.addListener(HikePubSub.END_TYPING_CONVERSATION, this);
        }

        private void RemoveListeners()
        {
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
            App.HikePubSubInstance.removeListener(HikePubSub.BLOCK_USER, this);
            App.HikePubSubInstance.removeListener(HikePubSub.TYPING_CONVERSATION, this);
            App.HikePubSubInstance.removeListener(HikePubSub.END_TYPING_CONVERSATION, this);
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

                        App.ViewModel.ConvMap[mObj.Msisdn] = mObj;
                        int index = App.ViewModel.MessageListPageCollection.IndexOf(mObj);
                        if (index < 0)//not present in oc
                        {
                            App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                        }
                        else if (index > 0)
                        {
                            App.ViewModel.MessageListPageCollection.RemoveAt(index);
                            App.ViewModel.MessageListPageCollection.Insert(0, mObj);
                        }//if already at zero, do nothing
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
                    Debug.WriteLine("HikeViewModel :: OnEventReceived : BLOCK USER , Exception : " + e.StackTrace);
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
            #region END TYPING NOTIFICATION
            else if (type == HikePubSub.END_TYPING_CONVERSATION)
            {
                object[] vals = (object[])obj;
                if (HidetypingNotification != null)
                    HidetypingNotification(null, vals);

            }
            #endregion
        }


        public event EventHandler<Object[]> ShowTypingNotification;
        public event EventHandler<Object[]> AutohideTypingNotification;
        public event EventHandler<Object[]> HidetypingNotification;

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
        }

        private Dictionary<string, ContactInfo> _contactsCache = new Dictionary<string, ContactInfo>();
        public Dictionary<string, ContactInfo> ContactsCache
        {
            get
            {
                return _contactsCache;
            }
        }

        #region In Apptips

        List<HikeToolTip> _toolTipsList;
        public Dictionary<int, HikeToolTip> DictInAppTip;

        public void ResetInAppTip(int index)
        {
            //should not reset tips 0 & 2 as chat thread count logic is involved

            if (DictInAppTip == null)
                DictInAppTip = new Dictionary<int, HikeToolTip>();

            int marked;
            App.appSettings.TryGetValue(App.TIP_MARKED_KEY, out marked);
            marked &= (int)~(1 << index);
            App.appSettings[App.TIP_MARKED_KEY] = marked;

            int currentShown;
            App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
            currentShown &= (int)~(1 << index);
            App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);

            if (_toolTipsList == null)
                LoadToolTipsList();

            _toolTipsList[index].IsShown = false;
            _toolTipsList[index].IsCurrentlyShown = false;

            DictInAppTip[index] = _toolTipsList[index];
        }

        void LoadToolTipsList()
        {
            _toolTipsList = new List<HikeToolTip>();

            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_1, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(0, 0, 180, 0), FullTipMargin = new Thickness(10, 0, 10, 0) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_2, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(10, 0, 262, 0), FullTipMargin = new Thickness(10, 0, 10, 0) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_3, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(10, 0, 10, 0), FullTipMargin = new Thickness(10, 0, 10, 70) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_4, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(10, 0, 15, 0), FullTipMargin = new Thickness(10, 0, 10, 55) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_5, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = true, TipMargin = new Thickness(0), FullTipMargin = new Thickness(0) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_6, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = true, TipMargin = new Thickness(120, 0, 10, 0), FullTipMargin = new Thickness(10, 75, 10, 0) });
            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_7, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = true, TipMargin = new Thickness(0), FullTipMargin = new Thickness(0) });

            if (!App.appSettings.Contains(App.ENTER_TO_SEND))
                _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_8_1, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(0, 0, 230, 0), FullTipMargin = new Thickness(10, 0, 10, 70) });
            else
                _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_8_2, HAlingment = HorizontalAlignment.Stretch, IsShown = false, IsCurrentlyShown = false, IsTop = false, TipMargin = new Thickness(0, 0, 230, 0), FullTipMargin = new Thickness(10, 0, 10, 70) });

            _toolTipsList.Add(new HikeToolTip() { Tip = AppResources.In_App_Tip_9, TipImage = UI_Utils.Instance.Circles, IsAnimationEnabled = true, Width = "300", HAlingment = HorizontalAlignment.Right, IsShown = false, IsCurrentlyShown = false, IsTop = true, TipMargin = new Thickness(260, 0, 10, 0), FullTipMargin = new Thickness(0, 80, 20, 0) });
        }

        /// <summary>
        /// Load In App Hardcoded Tooltips
        /// </summary>
        public void LoadToolTipsDict()
        {
            int marked, currentlyShowing;
            App.appSettings.TryGetValue(App.TIP_MARKED_KEY, out marked); //initilaized in upgrade logic
            App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentlyShowing); //initilaized in upgrade logic

            if (marked == 511 && currentlyShowing == 0)//0x1ff
                return;

            if (_toolTipsList == null)
                LoadToolTipsList();

            DictInAppTip = new Dictionary<int, HikeToolTip>();
            bool isShownVal, isCurrentShown;
            int powerOfTwo = 1;

            for (int index = 0; index < _toolTipsList.Count; index++)
            {
                powerOfTwo = 1 << index;
                isShownVal = (marked & powerOfTwo) > 0;
                isCurrentShown = (currentlyShowing & powerOfTwo) > 0;
                _toolTipsList[index].IsShown = isShownVal;
                _toolTipsList[index].IsCurrentlyShown = isCurrentShown;

                if (!isShownVal || isCurrentShown)
                    DictInAppTip[index] = _toolTipsList[index];
            }
        }

        /// <summary>
        /// Inserts the tooltip in grid element
        /// </summary>
        /// <param name="element">The grid element in which you want to insert the tooltip</param>
        /// <param name="index">index of the tooltip you want to insert</param>
        public void DisplayTip(Panel element, int index)
        {
            if (DictInAppTip == null)
                return;

            HikeToolTip tip;
            DictInAppTip.TryGetValue(index, out tip);

            if (tip == null || !(!tip.IsShown || tip.IsCurrentlyShown))
                return;

            tip.IsShown = true;
            tip.IsCurrentlyShown = true;

            int marked;
            App.appSettings.TryGetValue(App.TIP_MARKED_KEY, out marked);
            marked |= (int)(1 << index);
            App.WriteToIsoStorageSettings(App.TIP_MARKED_KEY, marked);

            int currentShown;
            App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
            currentShown |= (int)(1 << index);
            App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);

            if (element != null)
            {
                InAppTipUC inAppTipUC = new Controls.InAppTipUC() { Name = "tip" + index };
                Canvas.SetTop(inAppTipUC, 0);
                Canvas.SetLeft(inAppTipUC, 0);
                Canvas.SetZIndex(inAppTipUC, 3);
                inAppTipUC.Visibility = Visibility.Visible;

                if (index == 0 || index == 1 || index == 2 || index == 5 || index == 7 || index == 8)
                    inAppTipUC.SetValue(Grid.RowSpanProperty, 3);
                else if (index == 3)
                    inAppTipUC.SetValue(Grid.RowSpanProperty, 2);

                if (tip.Width != null)
                    inAppTipUC.Width = Convert.ToDouble(tip.Width);

                inAppTipUC.HorizontalAlignment = tip.HAlingment;

                if (tip.TipImage != null)
                    inAppTipUC.TipImageSource = tip.TipImage;

                inAppTipUC.IsAnimationEnabled = tip.IsAnimationEnabled;

                if (tip.IsTop)
                {
                    inAppTipUC.VerticalAlignment = VerticalAlignment.Top;
                    inAppTipUC.TopPathMargin = tip.TipMargin;
                    inAppTipUC.TopPathVisibility = Visibility.Visible;
                    inAppTipUC.BottomPathVisibility = Visibility.Collapsed;
                }
                else
                {
                    inAppTipUC.VerticalAlignment = VerticalAlignment.Bottom;
                    inAppTipUC.BottomPathMargin = tip.TipMargin;
                    inAppTipUC.TopPathVisibility = Visibility.Collapsed;
                    inAppTipUC.BottomPathVisibility = Visibility.Visible;
                }

                inAppTipUC.Tip = tip.Tip;

                if (index == 8 && App.newChatThreadPage != null && App.newChatThreadPage.IsMute)
                    inAppTipUC.Margin = new Thickness(0, 125, 20, 0);
                else
                    inAppTipUC.Margin = tip.FullTipMargin;

                inAppTipUC.TipIndex = index;

                inAppTipUC.Dismissed += inAppTipUC_Dismissed;

                try
                {
                    element.Children.Add(inAppTipUC);
                }
                catch { }
            }
        }

        /// <summary>
        /// Hide the tool tip
        /// </summary>
        /// <param name="element">remove the tooltip from this parent</param>
        /// <param name="index">tool tip index to be removed</param>
        public void HideToolTip(Panel element, int index)
        {
            if (DictInAppTip == null)
                return;

            HikeToolTip toolTip;
            DictInAppTip.TryGetValue(index, out toolTip);

            if (toolTip == null || !toolTip.IsCurrentlyShown)
                return;

            if (element != null)
            {
                InAppTipUC tip = element.FindName("tip" + index) as InAppTipUC;

                if (tip != null)
                {
                    element.Children.Remove(tip);
                    tip.Visibility = Visibility.Collapsed;

                    toolTip.IsCurrentlyShown = false;

                    int currentShown;
                    App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
                    currentShown &= (int)~(1 << index);
                    App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);
                }
            }
            else
            {
                toolTip.IsCurrentlyShown = false;

                int currentShown;
                App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
                currentShown &= (int)~(1 << index);
                App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);
            }
        }

        /// <summary>
        /// Invoked when the user dismisses a tooltip
        /// </summary>
        /// <param name="sender">Is of type InAppTipUC</param>
        /// <param name="e"></param>
        void inAppTipUC_Dismissed(object sender, EventArgs e)
        {
            if (sender == null)
                return;

            InAppTipUC tip = sender as InAppTipUC;

            if (tip != null)
            {
                tip.Visibility = Visibility.Collapsed;

                HikeToolTip toolTip = DictInAppTip[tip.TipIndex];
                toolTip.IsCurrentlyShown = false;

                int currentShown;
                App.appSettings.TryGetValue(App.TIP_SHOW_KEY, out currentShown);
                currentShown &= (int)~(1 << tip.TipIndex);
                App.WriteToIsoStorageSettings(App.TIP_SHOW_KEY, currentShown);

                toolTip.TriggerUIUpdateOnDismissed();
            }
        }

        #endregion

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
                StatusMessage sm = new StatusMessage(msisdn, AppResources.Now_Friends_Txt, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1);
                App.HikePubSubInstance.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
                App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
            }

            foreach (BaseStatusUpdate sb in App.ViewModel.StatusList)
            {
                if (sb is FriendRequestStatusUpdate)
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

        public void ForwardMessage(List<ContactInfo> contactsForForward)
        {
            if (!PhoneApplicationService.Current.State.ContainsKey(HikeConstants.FORWARD_MSG))
                return;

            contactsForForward = contactsForForward.Distinct(new ContactInfo.MsisdnComparer()).ToList();

            if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is string)
            {
                foreach (var contact in contactsForForward)
                {
                    var msg = (string)PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];

                    var msisdn = contact.Msisdn;
                    ConvMessage convMessage = new ConvMessage(msg, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                    convMessage.IsSms = !contact.OnHike;
                    convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;

                    if (App.newChatThreadPage != null && App.newChatThreadPage.mContactNumber == msisdn)
                        App.newChatThreadPage.AddNewMessageToUI(convMessage, false);

                    App.HikePubSubInstance.publish(HikePubSub.MESSAGE_SENT, convMessage);
                }
            }
            else if (PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG] is object[])
            {
                object[] attachmentData = (object[])PhoneApplicationService.Current.State[HikeConstants.FORWARD_MSG];
                if (attachmentData.Length == 1)
                {
                    foreach (var contact in contactsForForward)
                    {
                        var msisdn = contact.Msisdn;
                        ConvMessage convMessage = new ConvMessage(AppResources.Sticker_Txt, msisdn, TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                        convMessage.IsSms = !contact.OnHike;
                        convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
                        convMessage.MetaDataString = attachmentData[0] as string;

                        if (App.newChatThreadPage != null && App.newChatThreadPage.mContactNumber == msisdn)
                            App.newChatThreadPage.AddNewMessageToUI(convMessage, false);

                        App.HikePubSubInstance.publish(HikePubSub.MESSAGE_SENT, convMessage);
                    }
                }
                else
                {
                    string contentType = (string)attachmentData[0];
                    string sourceMsisdn = (string)attachmentData[1];
                    long messageId = (long)attachmentData[2];
                    string metaDataString = (string)attachmentData[3];
                    string sourceFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + sourceMsisdn.Replace(":", "_") + "/" + messageId;

                    foreach (var contact in contactsForForward)
                    {
                        var msisdn = contact.Msisdn;
                        ConvMessage convMessage = new ConvMessage("", msisdn,
                          TimeUtils.getCurrentTimeStamp(), ConvMessage.State.SENT_UNCONFIRMED);
                        convMessage.IsSms = !contact.OnHike;
                        convMessage.HasAttachment = true;
                        convMessage.FileAttachment = new Attachment();
                        convMessage.FileAttachment.ContentType = contentType;
                        convMessage.FileAttachment.Thumbnail = (byte[])attachmentData[4];
                        convMessage.FileAttachment.FileName = (string)attachmentData[5];
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
                            convMessage.Message = AppResources.Location_Txt;
                            convMessage.MetaDataString = metaDataString;
                        }
                        else if (contentType.Contains(HikeConstants.CT_CONTACT))
                        {
                            convMessage.Message = AppResources.ContactTransfer_Text;
                            convMessage.MetaDataString = metaDataString;
                        }

                        if (App.newChatThreadPage != null && App.newChatThreadPage.mContactNumber == msisdn)
                            App.newChatThreadPage.AddNewMessageToUI(convMessage, false);

                        object[] vals = new object[3];
                        vals[0] = convMessage;
                        vals[1] = sourceFilePath;
                        App.HikePubSubInstance.publish(HikePubSub.FORWARD_ATTACHMENT, vals);
                    }
                }

                PhoneApplicationService.Current.State.Remove(HikeConstants.FORWARD_MSG);
            }
        }
       
        public void Hyperlink_Clicked(object sender)
        {
            var obj = sender as object[];
            Hyperlink caller = obj[0] as Hyperlink;
            var val = (bool)obj[1];

            if (val)
            {
                var task = new WebBrowserTask() { Uri = new Uri(caller.TargetName) };
                task.Show();
            }
            else
            {
                var phoneCallTask = new PhoneCallTask();
                var targetPhoneNumber = caller.TargetName.Replace("-", "");
                targetPhoneNumber = targetPhoneNumber.Trim();
                targetPhoneNumber = targetPhoneNumber.Replace(" ", "");
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
            PhoneApplicationService.Current.State[HikeConstants.VIEW_MORE_MESSAGE_OBJ] = hp.TargetName;
            var currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            currentPage.NavigationService.Navigate(new Uri("/View/ViewMessage.xaml", UriKind.Relative));
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
    }
}
