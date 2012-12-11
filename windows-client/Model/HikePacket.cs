using System.Data.Linq.Mapping;
using System.ComponentModel;

namespace windows_client.Model
{
    [Table(Name = "mqtt_messages")]
    public class HikePacket
    {
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
                    _messageId = value;
                }
            }
        }

        private long _timestamp;
        [Column(IsPrimaryKey = true)]
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
                    _timestamp = value;
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
                    _message = value;
                }
            }
        }

        public HikePacket(long messageId, byte[] message, long timestamp)
        {
            this.MessageId = messageId;
            this.Timestamp = timestamp;
            this.Message = message;
        }

        public HikePacket()
        { 
        }

        //#region INotifyPropertyChanged Members

        //public event PropertyChangedEventHandler PropertyChanged;

        //// Used to notify that a property changed
        //private void NotifyPropertyChanged(string propertyName)
        //{
        //    if (PropertyChanged != null)
        //    {
        //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        //    }
        //}

        //#endregion

        //#region INotifyPropertyChanging Members

        //public event PropertyChangingEventHandler PropertyChanging;

        //// Used to notify that a property is about to change
        //private void NotifyPropertyChanging(string propertyName)
        //{
        //    if (PropertyChanging != null)
        //    {
        //        PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        //    }
        //}
        //#endregion



    }
}
