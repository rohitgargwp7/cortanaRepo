using System.Data.Linq.Mapping;
using System.ComponentModel;

namespace CommonLibrary.Model
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
    }
}
