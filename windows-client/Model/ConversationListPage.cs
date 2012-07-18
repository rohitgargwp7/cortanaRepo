using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    public class ConversationListObject : INotifyPropertyChanged
    {
        #region member variables

        private string _msisdn;
        private string _contactName;
        private string _lastMessage;
        private string _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;

        #endregion

        #region Properties

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

        public BitmapImage AvatarImage
        {
            get
            {
                return UserInterfaceUtils.getBitMapImage(_msisdn);
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

        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, string relativeTime)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = relativeTime;
            this._isOnhike = isOnhike;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, string relativeTime)
            : this(msisdn, contactName, lastMessage, false, relativeTime)
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
