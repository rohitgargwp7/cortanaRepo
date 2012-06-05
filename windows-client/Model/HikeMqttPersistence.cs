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
using System.Data.Linq.Mapping;
using System.ComponentModel;

namespace windows_client.Model
{
    [Table(Name = "mqtt_messages")]
    public class HikeMqttPersistence : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private long _mqttId;
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public long MqttId
        {
            get
            {
                return _mqttId;
            }
            set
            {
                if (_mqttId != value)
                {
                    NotifyPropertyChanging("MqttId");
                    _mqttId = value;
                    NotifyPropertyChanged("MqttId");
                }
            }
        }

        private long _messageId;
        [Column]
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

        private long _timestamp;

        [Column]
        public long Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (_timestamp != value)
                {
                    NotifyPropertyChanging("Timestamp");
                    _timestamp = value;
                    NotifyPropertyChanged("Timestamp");
                }
            }
        }

        public HikeMqttPersistence(long messageId, byte[] message, long timestamp)
        {
            this._messageId = messageId;
            this._message = message;
            this._timestamp = timestamp;
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
