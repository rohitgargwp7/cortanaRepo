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
    public class ConversationListObject : INotifyPropertyChanged
    {
        #region member variables

        private string _msisdn;
        private string _contactName;
        private string _lastMessage;
        private string _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;
        private byte[] _avatar;

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

        public string MSISDN
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

        public byte[] Avatar
        {
            get
            {
                return _avatar;
            }
            set
            {
                if (_avatar != value)
                {
                    _avatar = value;
                    NotifyPropertyChanged("Avatar");
                }
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

        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, string relativeTime, byte[] avatarImage)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = relativeTime;
            this._isOnhike = isOnhike;
            this._avatar = avatarImage;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, string relativeTime, byte[] avatarImage)
            : this(msisdn, contactName, lastMessage, false, relativeTime, avatarImage)
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
            return (_msisdn == o.MSISDN);
        }
        #endregion

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
