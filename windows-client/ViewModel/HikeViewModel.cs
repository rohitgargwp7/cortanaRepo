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


namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged, HikePubSub.Listener
    {
        // this key will track number of conversations in the app. If this does not match the convs at load time , simply move to backup plan.
        public static string NUMBER_OF_CONVERSATIONS = "NoConvs";

        public static string NUMBER_OF_FAVS = "NoFavs";

        private ObservableCollection<ConversationListObject> _pendingReq = null;

        private ObservableCollection<ConversationListObject> _favList = null;

        private IScheduler scheduler = Scheduler.NewThread;

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


        private ObservableCollection<ConversationBox> _messageListPageCollection;

        public ObservableCollection<ConversationBox> MessageListPageCollection
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

        public bool IsPendingListLoaded
        {
            get;
            set;
        }

        public ObservableCollection<ConversationListObject> PendingRequests
        {
            get
            {
                return _pendingReq;
            }
            set
            {
                if (value != _pendingReq)
                    _pendingReq = value;
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
            _pendingReq = new ObservableCollection<ConversationListObject>();
            _favList = new ObservableCollection<ConversationListObject>();

            List<ConversationBox> listConversationBox = new List<ConversationBox>();
            // this order should be maintained as _convMap should be populated before loading fav list
            for (int i = 0; i < convList.Count; i++)
            {
                ConversationListObject convListObj = convList[i];
                _convMap[convListObj.Msisdn] = convListObj;
                convListObj.ConvBoxObj = new ConversationBox(convListObj);//context menu wil bind on page load
                listConversationBox.Add(convListObj.ConvBoxObj);
            }
            _messageListPageCollection = new ObservableCollection<ConversationBox>(listConversationBox);
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
            _messageListPageCollection = new ObservableCollection<ConversationBox>();
            _convMap = new Dictionary<string, ConversationListObject>();
            _favList = new ObservableCollection<ConversationListObject>();
            _pendingReq = new ObservableCollection<ConversationListObject>();
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
                MiscDBUtil.LoadPendingRequests();
                IsPendingListLoaded = true;
            }
            if (_pendingReq == null)
                return false;
            for (int i = 0; i < _pendingReq.Count; i++)
            {
                if (_pendingReq[i].Msisdn == ms)
                    return true;
            }
            return false;
        }

        public ConversationListObject GetPending(string mContactNumber)
        {
            if (_pendingReq.Count == 0)
                return null;
            for (int i = 0; i < _pendingReq.Count; i++)
            {
                if (_pendingReq[i].Msisdn == mContactNumber)
                    return _pendingReq[i];
            }
            return null;

        }

        private void RegisterListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
        }

        private void RemoveListeners()
        {
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
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

                        if (mObj.ConvBoxObj == null)
                        {
                            mObj.ConvBoxObj = new ConversationBox(mObj);
                            if (App.ViewModel.ConversationListPage != null)
                                ContextMenuService.SetContextMenu(mObj.ConvBoxObj, App.ViewModel.ConversationListPage.createConversationContextMenu(mObj));
                        }
                        App.ViewModel.MessageListPageCollection.Remove(mObj.ConvBoxObj);
                        App.ViewModel.ConvMap[mObj.Msisdn] = mObj;
                        App.ViewModel.MessageListPageCollection.Insert(0, mObj.ConvBoxObj);
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
                catch (KeyNotFoundException)
                {
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
        }
    }
}
