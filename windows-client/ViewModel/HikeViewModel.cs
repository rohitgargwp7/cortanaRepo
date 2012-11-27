using System.ComponentModel;
using windows_client.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Reactive;
using System;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged, HikePubSub.Listener
    {
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

        public HikeViewModel(List<ConversationListObject> convList)
        {
            _messageListPageCollection = new ObservableCollection<ConversationListObject>(convList);
            _convMap = new Dictionary<string, ConversationListObject>(convList.Count);
            for (int i = 0; i < convList.Count; i++)
                _convMap[convList[i].Msisdn] = convList[i];
            RegisterListeners();
        }

        public HikeViewModel()
        {
            _messageListPageCollection = new ObservableCollection<ConversationListObject>();
            _convMap = new Dictionary<string, ConversationListObject>();
            RegisterListeners();
        }

        private void RefreshNewConversationObject()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    if (App.ViewModel.MessageListPageCollection.Count > 0)
                    {
                        ConversationListObject c = App.ViewModel.MessageListPageCollection[0];
                        if (c == null)
                            return;
                        App.ViewModel.MessageListPageCollection.RemoveAt(0);
                        App.ViewModel.MessageListPageCollection.Insert(0, c);
                    }
                }
                catch (ArgumentOutOfRangeException)
                { }
            });
        }

        private void RegisterListeners()
        {
            App.HikePubSubInstance.addListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.addListener(HikePubSub.USER_LEFT, this);
            App.HikePubSubInstance.addListener(HikePubSub.UPDATE_UI, this);
        }

        private void RemoveListeners()
        {
            App.HikePubSubInstance.removeListener(HikePubSub.MESSAGE_RECEIVED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_JOINED, this);
            App.HikePubSubInstance.removeListener(HikePubSub.USER_LEFT, this);
            App.HikePubSubInstance.removeListener(HikePubSub.UPDATE_UI, this);
        }

        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_RECEIVED
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;
                ConversationListObject mObj = (ConversationListObject)vals[1];
                if (mObj == null)
                    return;
                if (App.ViewModel.ConvMap.ContainsKey(mObj.Msisdn))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!App.ViewModel.MessageListPageCollection.Remove(mObj))
                            scheduler.Schedule(App.ViewModel.RefreshNewConversationObject, TimeSpan.FromMilliseconds(5));
                    });
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
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
                catch (KeyNotFoundException)
                {
                }
            }
            #endregion
            #region UPDATE_UI
            else if (HikePubSub.UPDATE_UI == type)
            {
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                if (!App.ViewModel.ConvMap.ContainsKey(msisdn))
                    return;

                ConversationListObject convObj = App.ViewModel.ConvMap[msisdn];
                byte[] _avatar = (byte[])vals[1];
                try
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        convObj.Avatar = _avatar;
                    });
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

    }
}
