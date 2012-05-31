using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Data.Linq;

namespace windows_client.Model
{
    public class MessageListPage : INotifyPropertyChanged, INotifyPropertyChanging
    {
        //private Image _avatar;
        //public Image Avatar
        //{
        //    get
        //    {
        //        return _avatar;
        //    }
        //    set
        //    {
        //        if (_avatar != value)
        //        {
        //            NotifyPropertyChanging("Avatar");
        //            _avatar = value;
        //            NotifyPropertyChanged("Avatar");
        //        }
        //    }
        //}


        private string _contactName;
        public string ContactName
        {
            get
            {
                return _contactName;
            }
            set
            {
                if (_contactName != value)
                {
                    NotifyPropertyChanging("ContactName");
                    _contactName = value;
                    NotifyPropertyChanged("ContactName");
                }
            }
        }
        
        private string _lastMessage;
        public string LastMessage
        {
            get
            {
                return _lastMessage;
            }
            set
            {
                if (_lastMessage != value)
                {
                    NotifyPropertyChanging("LastMessage");
                    _lastMessage = value;
                    NotifyPropertyChanged("LastMessage");
                }
            }
        }
        
        private string _timeStamp;
        public string TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                if (_timeStamp != value)
                {
                    NotifyPropertyChanging("OnHike");
                    _timeStamp = value;
                    NotifyPropertyChanged("OnHike");
                }
            }
        }

        public MessageListPage(string contactName, string lastMessage, string relativeTime)
        {
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = relativeTime;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
