using System;
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

        public PinHistory()
        {
            InitializeComponent();

            mPubSub = App.HikePubSubInstance;
            registerListeners();
        }

        private void registerListeners()
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
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            
            if (PhoneApplicationService.Current.State.ContainsKey(HikeConstants.GC_PIN))
                _grpMsisdn = PhoneApplicationService.Current.State[HikeConstants.GC_PIN] as string;

            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;


            BackgroundWorker bw = new BackgroundWorker();
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            bw.DoWork += (s, ev) =>
                {
                    loadPinMessages();
                };
            bw.RunWorkerAsync();
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
                    metadata[HikeConstants.READ] = true;
                }

                App.ViewModel.ConvMap[_grpMsisdn].MetaData = metadata;
            }

            
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_pinMessages == null)
            {
                nopinImage.Visibility = Visibility.Visible;
                pinLongList.Visibility = Visibility.Collapsed;
            }

            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;
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
                        convMessage.GroupMemberName = gp.Name;
                    }

                    if (convMessage.MetaDataString != null && convMessage.MetaDataString.Contains("lm"))
                    {
                        string message = MessagesTableUtils.ReadLongMessageFile(convMessage.Timestamp, convMessage.Msisdn);
                        
                        if (message.Length > 0)
                            convMessage.Message = message;
                    }
                }
            }

            if (App.ViewModel.ConvMap.ContainsKey(_grpMsisdn) && App.ViewModel.ConvMap[_grpMsisdn].MetaData != null)
                App.ViewModel.ConvMap[_grpMsisdn].MetaData[HikeConstants.UNREADPINS] = 0;
        }
    }
}