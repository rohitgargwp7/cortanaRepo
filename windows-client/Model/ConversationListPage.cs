using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using windows_client.utils;
using System;
using System.Diagnostics;

namespace windows_client.Model
{
    public class ConversationListObject : INotifyPropertyChanged, IComparable<ConversationListObject>
    {
        #region member variables

        private string _msisdn;
        public string _contactName;
        private string _lastMessage;
        private string _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;
        private long _timestampLong;
        #endregion

        #region Properties

        public string ContactName
        {
            get
            {
                if (_contactName != null)
                    return _contactName;
                else
                    return _msisdn;
            }
            set
            {
                if (_contactName != value)
                {
                    _contactName = value;
                    NotifyPropertyChanged("ContactName");
                }
            }
        }

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

                    _lastMessage = value;
                    NotifyPropertyChanged("LastMessage");
                }
            }
        }

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
                   
                    _timeStamp = value;
                
                }
            }
        }

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (_msisdn != value)
                {
                    _msisdn = value;
                }
            }
        }

        public bool IsOnhike
        {
            get
            {
                return _isOnhike;
            }
            set
            {
                if (_isOnhike != value)
                {
                   
                    _isOnhike = value;
                    NotifyPropertyChanged("IsOnhike");
                }
            }
        }

        public long TimestampLong
        {
            get
            {
                return _timestampLong;
            }
            set
            {
                if (_timestampLong != value)
                {
                    _timestampLong = value;
                    NotifyPropertyChanged("IsOnhike");
                }
            }
        }

        public BitmapImage AvatarImage
        {
            get
            {
                BitmapImage img =  UserInterfaceUtils.getBitMapImage(_msisdn);
                return img;
            }
        }
        
        public ConvMessage.State MessageStatus
        {
            get
            {
                return _messageStatus;
            }
            set
            {
                if (_messageStatus != value)
                {
                    //TODO check ((_messageStatus != null) ? _messageStatus : 0) <= value
                    if (_messageStatus != value)
                    {
                        _messageStatus = value;
                        NotifyPropertyChanged("MessageStatus");
                    }
                }
            }
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, string relativeTime, long timestamp)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = relativeTime;
            this._isOnhike = isOnhike;
            this._timestampLong = timestamp;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, string relativeTime, long timestamp)
            : this(msisdn, contactName, lastMessage, false, relativeTime, timestamp)
        {

        }

        public ConversationListObject()
        {
            _msisdn = null;
            _contactName = null;
            _lastMessage = null;
            _timeStamp = null;
            _isOnhike = false;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            ConversationListObject o = obj as ConversationListObject;

            if ((System.Object)o == null)
            {
                return false;
            }
            return (_msisdn == o.Msisdn);
        }
        //public override int GetHashCode()
        //{
        //    const int prime = 31;
        //    int result = 1;
        //    result = prime * result + ((Msisdn == null) ? 0 : Msisdn.GetHashCode());
        //    result = prime * result + ((ContactName == null) ? 0 : ContactName.GetHashCode());
        //    result = prime * result + ((LastMessage == null) ? 0 : LastMessage.GetHashCode());
        //    result = prime * result + ((TimeStamp == null) ? 0 : TimeStamp.GetHashCode());
        //    result = prime * result + ((TimestampLong == null) ? 0 : TimestampLong.GetHashCode());

        //    return result;
        //}

        public int CompareTo(ConversationListObject rhs)
        {
            if (this.Equals(rhs))
            {
                return 0;
            }
            //TODO check is Messages is empty
            return TimestampLong > rhs.TimestampLong ? -1 : 1;
        }


        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
               Deployment.Current.Dispatcher.BeginInvoke(() =>
               {
                   PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
               });
            }
        }
        #endregion

    }
}
