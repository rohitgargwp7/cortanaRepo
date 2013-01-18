using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System.ComponentModel;

namespace windows_client.Controls.StatusUpdate
{
    public partial class FriendRequestStatus : StatusUpdateBox, INotifyPropertyChanged, INotifyPropertyChanging
    {

        private EventHandler<GestureEventArgs> _yesTap;
        private EventHandler<GestureEventArgs> _noTap;
        private string _seeUpdates;

        public EventHandler<GestureEventArgs> YesTap
        {
            get
            {
                return _yesTap;
            }
            set
            {
                if (value != _yesTap)
                {
                    NotifyPropertyChanging("YesTap");
                    _yesTap = value;
                    NotifyPropertyChanged("YesTap");
                }
            }
        }

        public EventHandler<GestureEventArgs> NoTap
        {
            get
            {
                return _noTap;
            }
            set
            {
                if (value != _noTap)
                {
                    NotifyPropertyChanging("NoTap");
                    _noTap = value;
                    NotifyPropertyChanged("NoTap");
                }
            }
        }

        public string SeeUpdates
        {
            get
            {
                return _seeUpdates;
            }
            set
            {
                if (value != _seeUpdates)
                {
                    NotifyPropertyChanging("SeeUpdates");
                    _seeUpdates = value;
                    NotifyPropertyChanged("SeeUpdates");
                }
            }
        }

        public FriendRequestStatus(string userName, BitmapImage userImage)
            : base(userName, userImage)
        {
            this.SeeUpdates = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);
        }

        public FriendRequestStatus(string userName, BitmapImage userImage, 
            EventHandler<GestureEventArgs> yesTap, EventHandler<GestureEventArgs> noTap)
            : base(userName, userImage)
        {
            this.SeeUpdates = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, userName);
            this.YesTap = yesTap;
            this.NoTap = noTap;
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch { }
                });
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;

        // Used to notify that a property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
        #endregion

    }
}
