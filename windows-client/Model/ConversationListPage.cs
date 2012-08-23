using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using windows_client.utils;
using System;
using System.Diagnostics;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.IO;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Windows.Media;

namespace windows_client.Model
{
    [Table(Name = "conversations")]
    public class ConversationListObject : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<ConversationListObject>
    {
        #region member variables

        private string _msisdn;
        private string _contactName;
        private string _lastMessage;
        private long _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;
        private byte[] _avatar;
        #endregion

        [Column(IsVersion = true)]
        private Binary version;
        #region Properties

        [Column]
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
                    NotifyPropertyChanged("NameToShow");
                }
            }
        }

        [Column]
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

        [Column]
        public long TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                if (_timeStamp != value)
                {
                    NotifyPropertyChanging("TimeStamp");
                    _timeStamp = value;
                    NotifyPropertyChanged("TimeStamp");
                    NotifyPropertyChanged("FormattedTimeStamp");
                }
            }
        }

        [Column(IsPrimaryKey = true)]
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
                    NotifyPropertyChanging("Msisdn");
                    _msisdn = value;
                    NotifyPropertyChanged("Msisdn");
                }
            }
        }

        [Column]
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
                    NotifyPropertyChanging("IsOnhike");
                    _isOnhike = value;
                    NotifyPropertyChanged("IsOnhike");
                }
            }
        }


        [Column(DbType="image")]
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
                    NotifyPropertyChanging("Avatar");
                    _avatar = value;
                    NotifyPropertyChanged("Avatar");
                    NotifyPropertyChanged("AvatarImage");
                }
            }
        }

        public string NameToShow
        {
            get
            {
                if (_contactName != null)
                    return _contactName;
                else
                    return _msisdn;
            }
        }

        public string FormattedTimeStamp
        {
            get
            {
                return TimeUtils.getTimeString(_timeStamp);
            }
        }

        public BitmapImage AvatarImage
        {
            get
            {
                try
                {
                    if (_avatar == null)
                    {
                        return UI_Utils.Instance.DefaultAvatarBitmapImage;
                    }
                    else
                    {
                        MemoryStream memStream = new MemoryStream(_avatar);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage empImage = new BitmapImage();
                        empImage.SetSource(memStream);
                        return empImage;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in Avatar Image : {0}", e.ToString());
                    return null;
                }
            }
        }

        public string LastMessageColor
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.RECEIVED_UNREAD:
                        Color currentAccentColorHex =
                        (Color)Application.Current.Resources["PhoneAccentColor"];
                        return currentAccentColorHex.ToString();
                    default: return "gray";
                }
            }
        }

        public Visibility IsLastMessageUnread
        {
            get
            {
                if (ConvMessage.State.RECEIVED_UNREAD == _messageStatus)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public string SdrImage
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.SENT_CONFIRMED: return "images\\ic_sent.png";
                    case ConvMessage.State.SENT_DELIVERED: return "images\\ic_delivered.png";
                    case ConvMessage.State.SENT_DELIVERED_READ: return "images\\ic_read.png";
                    default: return "";
                }
            }
        }

        [Column(IsDbGenerated = false, UpdateCheck = UpdateCheck.Always)]
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
                    NotifyPropertyChanging("MessageStatus");
                    _messageStatus = value;
                    NotifyPropertyChanged("MessageStatus");
                    NotifyPropertyChanged("LastMessageColor");
                }
            }
        }
        
        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, long timestamp, byte[] avatar, ConvMessage.State msgStatus)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = timestamp;
            this._isOnhike = isOnhike;
            this._avatar = avatar;
            this._messageStatus = msgStatus;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, long timestamp, ConvMessage.State msgStatus)
            : this(msisdn, contactName, lastMessage, false, timestamp, null,msgStatus)
        {

        }

        public ConversationListObject()
        {
            
        }

        public int CompareTo(ConversationListObject rhs)
        {
            if (this.Equals(rhs))
            {
                return 0;
            }
            //TODO check is Messages is empty
            return TimeStamp > rhs.TimeStamp ? -1 : 1;
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
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception)
                    {
                    }
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
                try
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
                catch (Exception)
                { }
            }
        }
        #endregion
    }
}
