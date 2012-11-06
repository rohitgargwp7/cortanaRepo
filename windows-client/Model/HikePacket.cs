﻿using System.Data.Linq.Mapping;
using System.ComponentModel;

namespace windows_client.Model
{
    [Table(Name = "mqtt_messages")]
    public class HikePacket : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private long _messageId;
        [Column(IsPrimaryKey = true)]
        public long MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                if (_messageId != value)
                {
                    NotifyPropertyChanging("MessageId");
                    _messageId = value;
                    NotifyPropertyChanged("MessageId");
                }
            }
        }

        private byte[] _message;
        [Column]
        public byte[] Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (_message != value)
                {
                    NotifyPropertyChanging("Message");
                    _message = value;
                    NotifyPropertyChanged("Message");
                }
            }
        }

        public HikePacket(long messageId, byte[] message)
        {
            this._messageId = messageId;
            this._message = message;
        }

        public HikePacket()
        { 
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
