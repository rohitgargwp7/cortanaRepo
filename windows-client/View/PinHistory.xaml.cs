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

            progressBar.Opacity = 1;
            progressBar.IsEnabled = true;

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
                ConvMessage convMessage = (ConvMessage)vals[0];

                if (convMessage.Msisdn == _grpMsisdn)
                {
                    if (vals.Length == 3 && vals[0] is ConvMessage)
                    {
                        try
                        {
                            if (_pinMessages == null)
                                return;

                            Deployment.Current.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                _pinMessages.Insert(0, convMessage);

                                if (_pinMessages.Count>0)
                                    pinLongList.ScrollTo(_pinMessages[0]);
                            }));
                        }
                        catch
                        {
                           
                        }
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

            loadPinMessages();
            pinLongList.ItemsSource = _pinMessages;
        }

        public void loadPinMessages()
        {
            List<ConvMessage> pinMessages = MessagesTableUtils.getPinMessagesForMsisdn(_grpMsisdn);

            if (pinMessages != null)
            {
                _pinMessages = new ObservableCollection<ConvMessage>(pinMessages);

                foreach (ConvMessage convMessage in _pinMessages)
                {
                    GroupParticipant gp;
                    if (convMessage.IsSent)
                        convMessage.GroupMemberName = "You";
                    else
                    {
                        gp = GroupManager.Instance.GetGroupParticipant(null, convMessage.GroupParticipant, _grpMsisdn);
                        convMessage.GroupMemberName = gp.Name;
                    }
                }
            }
            else
            {
                nopin_image.Visibility = Visibility.Visible;
                pinLongList.Visibility = Visibility.Collapsed;
            }

            progressBar.Opacity = 0;
            progressBar.IsEnabled = false;
        }

        private void MenuItem_Click_Delete(object sender, RoutedEventArgs e)
        {
            ConvMessage msg = ((sender as MenuItem).DataContext as ConvMessage);

            if (msg == null)
                return;

            _pinMessages.Remove(msg);
        }
    }
}