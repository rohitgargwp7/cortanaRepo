using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using windows_client.DbUtils;
using System.Collections.ObjectModel;
using System.ComponentModel;
using windows_client.Misc;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using windows_client.utils;
using CommonLibrary.Constants;

namespace windows_client.View
{
    public partial class PinHistory : PhoneApplicationPage, HikePubSub.Listener, INotifyPropertyChanged
    {
        private HikePubSub mPubSub;
        private string _grpMsisdn;
        ObservableCollection<ConvMessage> _pinMessages;

        public PinHistory()
        {
            InitializeComponent();

            mPubSub = HikeInstantiation.HikePubSubInstance;
            RegisterListeners();
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
                                if (_pinMessages == null)
                                {
                                    _pinMessages = new ObservableCollection<ConvMessage>();
                                    pinLongList.ItemsSource = _pinMessages;
                                    pinLongList.Visibility = Visibility.Visible;
                                    nopinImage.Visibility = Visibility.Collapsed;
                                }
                                _pinMessages.Insert(0, convMessage);

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

            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.NavigationKeys.GC_PIN))
                _grpMsisdn = PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.GC_PIN] as string;

            progressBar.Visibility = Visibility.Visible;

            BackgroundWorker bw = new BackgroundWorker();
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.DoWork += (s, ev) =>
                {
                    loadPinMessages();
                };
            bw.RunWorkerAsync();
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.GC_PIN);
            base.OnBackKeyPress(e);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(_grpMsisdn))
            {
                JObject metadata = HikeInstantiation.ViewModel.ConvMap[_grpMsisdn].MetaData;

                if (metadata != null)
                {
                    metadata[HikeConstants.UNREADPINS] = 0;
                    metadata[HikeConstants.READPIN] = true;
                }

                HikeInstantiation.ViewModel.ConvMap[_grpMsisdn].MetaData = metadata;
                ConversationTableUtils.updateConversation(HikeInstantiation.ViewModel.ConvMap[_grpMsisdn]);
            }
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_pinMessages == null)
            {
                nopinImage.Visibility = Visibility.Visible;
                pinLongList.Visibility = Visibility.Collapsed;
            }

            progressBar.Visibility = Visibility.Collapsed;
            pinLongList.ItemsSource = _pinMessages;
        }

        public void loadPinMessages()
        {
            List<ConvMessage> pinMessages = MessagesTableUtils.getPinMessagesForMsisdn(_grpMsisdn);

            if (pinMessages != null)
            {
                _pinMessages = new ObservableCollection<ConvMessage>(pinMessages);
                GroupParticipant gp;

                foreach (ConvMessage convMessage in _pinMessages)
                {
                    if (!convMessage.IsSent)
                    {
                        gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, _grpMsisdn);
                        convMessage.GroupMemberName = gp.FirstName;
                    }

                    if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains(ServerJsonKeys.LONG_MESSAGE))
                    {
                        string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);

                        if (message.Length > 0)
                            convMessage.Message = message;
                    }
                }
            }

            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(_grpMsisdn) && HikeInstantiation.ViewModel.ConvMap[_grpMsisdn].MetaData != null)
            {
                HikeInstantiation.ViewModel.ConvMap[_grpMsisdn].MetaData[HikeConstants.UNREADPINS] = 0;
                ConversationTableUtils.updateConversation(HikeInstantiation.ViewModel.ConvMap[_grpMsisdn]);
            }
        }
    }
}