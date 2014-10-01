﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.DbUtils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using windows_client.Misc;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace windows_client.View
{
    public partial class PinHistory : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        private HikePubSub mPubSub;
        private string _grpMsisdn;
        ObservableCollection<ConvMessage> _pinMessages;
        long _lastMessageId = -1;                   // The ID of last Pin which was loaded into memory from DB
        private const int INITIAL_FETCH_COUNT = 31; // Initially 31 pins would be loaded into memory
        private const int SUB_FETCH_COUNT = 21;     // After that, 21 pins would be loaded into memory
        bool _hasMoreMessages = false;              // To avoid multiple DB calls returning empty list
        bool _isFirstLaunch = true;                 // For avoiding loading messages, everytime app is suspended and resumed (No Tombstoning)

        public PinHistory()
        {
            InitializeComponent();

            mPubSub = App.HikePubSubInstance;
            RegisterListeners();
            _pinMessages = new ObservableCollection<ConvMessage>();
        }

        private void RegisterListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED, this);
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_RECEIVED == type)
            {
                object[] vals = (object[])obj;

                if (vals.Length == 3 && vals[0] is ConvMessage)
                {
                    ConvMessage convMessage = (ConvMessage)vals[0];
                    if (convMessage.Msisdn == _grpMsisdn && convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                if (_pinMessages==null || _pinMessages.Count == 0)
                                {
                                    pinLongList.Visibility = Visibility.Visible;
                                    nopinImage.Visibility = Visibility.Collapsed;
                                }

                                _pinMessages.Insert(0, convMessage); //Insert an Element into OC in UI thread to reflect the changes.

                                if (_pinMessages.Count > 0)
                                    pinLongList.ScrollTo(_pinMessages[0]);
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine(e.Message);
                            }
                        }));
                    }
                }
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        //Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GC_PIN))
                _grpMsisdn = PhoneApplicationService.Current.State[HikeConstants.GC_PIN] as string;

            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.DoWork += (s, ev) =>
                {
                    if (_isFirstLaunch)
                    {
                        LoadPinMessages(INITIAL_FETCH_COUNT);
                        _isFirstLaunch = false;
                    }
                };
            bw.RunWorkerAsync();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove(HikeConstants.GC_PIN);
            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (App.ViewModel.ConvMap.ContainsKey(_grpMsisdn))
            {
                JObject metadata = App.ViewModel.ConvMap[_grpMsisdn].MetaData;

                if (metadata != null)
                {
                    metadata[HikeConstants.UNREADPINS] = 0;
                    metadata[HikeConstants.READPIN] = true;
                }

                App.ViewModel.ConvMap[_grpMsisdn].MetaData = metadata;
                ConversationTableUtils.updateConversation(App.ViewModel.ConvMap[_grpMsisdn]);
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_pinMessages == null || _pinMessages.Count==0)
            {
                nopinImage.Visibility = Visibility.Visible;
                pinLongList.Visibility = Visibility.Collapsed;
            }

            progressBar.Visibility = Visibility.Collapsed;
            pinLongList.ItemsSource = _pinMessages;
        }

        public void LoadPinMessages(int messageFetchCount)
        {
            List<ConvMessage> pinMessages = MessagesTableUtils.getPinMessagesForMsisdn(_grpMsisdn, _lastMessageId < 0 ? long.MaxValue : _lastMessageId, messageFetchCount);

            if (pinMessages != null && pinMessages.Count > 0)
            {
                _lastMessageId = pinMessages[pinMessages.Count - 1].MessageId;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    AddMessagesToPinOC(pinMessages);
                });

                _hasMoreMessages = true;

                if (pinMessages.Count < messageFetchCount)
                    _hasMoreMessages = false;
            }
            else
                _hasMoreMessages = false;
        }

        private void AddMessagesToPinOC(List<ConvMessage> pinMessages)
        {
            GroupParticipant gp;

            foreach (ConvMessage convMessage in pinMessages)
            {
                if (!convMessage.IsSent)
                {
                    gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, _grpMsisdn);
                    convMessage.GroupMemberName = gp.FirstName;
                }

                if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(HikeConstants.LONG_MESSAGE))
                {
                    string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);

                    if (message.Length > 0)
                        convMessage.Message = message;
                }

                _pinMessages.Add(convMessage);
            }
        }

        private void pinLongList_ItemRealized(object sender, ItemRealizationEventArgs e)
        {
            if (_hasMoreMessages && pinLongList.ItemsSource != null && pinLongList.ItemsSource.Count > 0)
                if (e.ItemKind == LongListSelectorItemKind.Item && (e.Container.Content as ConvMessage).Equals(pinLongList.ItemsSource[pinLongList.ItemsSource.Count - 1]))
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += (s, ev) =>
                    {
                        LoadPinMessages(SUB_FETCH_COUNT);
                    };
                    bw.RunWorkerAsync();
                }
        }
    }
}