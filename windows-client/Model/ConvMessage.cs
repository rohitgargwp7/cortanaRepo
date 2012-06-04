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
using System.Data.Linq.Mapping;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;


using windows_client.utils;
using Newtonsoft.Json.Linq;

namespace windows_client.Model
{
    [Table(Name = "messages")]
//    [Index(Name = "Conversation_IDX", Columns = "ConversationId, Timestamp Desc")]
    public class ConvMessage : INotifyPropertyChanged, INotifyPropertyChanging
    {

        private long _messageId; // this corresponds to msgID stored in sender's DB
        private string _msisdn; // this corresponds to msgID stored in receiver's DB
        private string _message;
        private State _messageStatus;
        private long _timestamp;
        private long _mappedMessageId;
        private bool _isInvite;
        private bool _isSent;
        private bool _isSms;
        private Conversation mConversation;
        private MessageMetadata metadata;


        /* Adding entries to the beginning of this list is not backwards compatible */
        public enum State
        {
            SENT_UNCONFIRMED = 0,  /* message sent to server */
            SENT_FAILED, /* message could not be sent, manually retry */
            SENT_CONFIRMED, /* message received by server */
            SENT_DELIVERED, /* message delivered to client device */
            SENT_DELIVERED_READ, /* message viewed by recipient */
            RECEIVED_UNREAD, /* message received, but currently unread */
            RECEIVED_READ, /* message received an read */
            UNKNOWN
        };

        #region Messages Table member

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
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

        [Column]
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
        public string Message
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

        [Column]
        public State MessageStatus
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

                    //TODO check ((_messageStatus != null) ? _messageStatus : 0) <= value
                    if (_messageStatus!=value)
                    {
                        _messageStatus = value;
                    }
                    NotifyPropertyChanged("MessageStatus");
                }
            }
        }

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

        [Column]
        public long MappedMessageId
        {
            get
            {
                return _mappedMessageId;
            }
            set
            {
                if (_mappedMessageId != value)
                {
                    NotifyPropertyChanging("MappedMessageId");
                    _mappedMessageId = value;
                    NotifyPropertyChanged("MappedMessageId");
                }
            }
        }

        public bool IsInvite
        {
            get
            {
                return _isInvite;
            }
            set
            {
                if (_isInvite != value)
                {
                    NotifyPropertyChanging("IsInvite");
                    _isInvite = value;
                    NotifyPropertyChanged("IsInvite");
                }
            }
        }

        public bool IsSent
        {
            get
            {
                return _isSent;
            }
        }

        public bool IsSms
        {
            get
            {
                return _isSms;
            }
        }

        public Conversation Conversation
        {
            get
            {
                return mConversation;
            }
            set
            {
                if (value != mConversation)
                    mConversation = value;
            }
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState)
            : this(message, msisdn, timestamp, msgState, -1, -1)
        {
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState, long msgid, long mappedMsgId)
        {
            //TODO check assertion in c#
            //assert(msisdn != null);
            this._msisdn = msisdn;
            this._message = message;
            this._timestamp = timestamp;
            this._messageId = msgid;
            this._mappedMessageId = mappedMsgId;
            _isSent = (msgState == State.SENT_UNCONFIRMED ||
                        msgState == State.SENT_CONFIRMED ||
                        msgState == State.SENT_DELIVERED ||
                        msgState == State.SENT_DELIVERED_READ ||
                        msgState == State.SENT_FAILED);
            MessageStatus = msgState;
        }

        public ConvMessage(JObject obj) 
	    {
//            this._msisdn = obj["(HikeConstants.FROM"].StringValue;
//            IJSonObject data = (new JSonReader()).ReadAsJSonObject(obj[HikeConstants.DATA].StringValue);
            
//            if (data.Contains(HikeConstants.SMS_MESSAGE))
//            {
//                this._message = data[HikeConstants.SMS_MESSAGE].StringValue;
//                this._isSms = true;
//            } else
//            {
//                this._message = data[HikeConstants.HIKE_MESSAGE].StringValue;
//                this._isSms = false;
//            }

//            this.Timestamp = data[HikeConstants.TIMESTAMP].Int64Value;

//            /* prevent us from receiving a message from the future */

//            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds/1000;
////		    long now = System.currentTimeMillis()/1000;
//            this.Timestamp = (this.Timestamp > now) ? now : this.Timestamp;
		  
//            /* if we're deserialized an object from json, it's always unread */
//            this.MessageStatus = State.RECEIVED_UNREAD;
//            this._messageId = -1;
//            string mappedMsgID = data[HikeConstants.MESSAGE_ID].StringValue;
//            this.MappedMessageId = System.Int64.Parse(mappedMsgID);
	    }

        public ConvMessage()
        { 
        }

        public JObject serialize()
        {
            JObject obj = new JObject();
            JObject data = new JObject();

            data[HikeConstants.HIKE_MESSAGE] = _message;
            data[HikeConstants.TIMESTAMP] = _timestamp;
            data[HikeConstants.MESSAGE_ID] = _messageId;

            obj[HikeConstants.TO] = _msisdn;
            obj[HikeConstants.DATA] = data;
            obj[HikeConstants.TYPE] = _isInvite ? NetworkManager.INVITE : NetworkManager.MESSAGE;

            return obj;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            ConvMessage other = (ConvMessage)obj;

            if (IsSent != other.IsSent)
                return false;
            if (Message == null)
            {
                if (other.Message != null)
                    return false;
            }
            else if (Message.CompareTo(other.Message)!=0)
                return false;
            if (Msisdn == null)
            {
                if (other.Msisdn != null)
                    return false;
            }
            else if (Msisdn.CompareTo(other.Msisdn)!=0)
                return false;
            if ( MessageStatus.Equals(other.MessageStatus))
                return false;
            if (Timestamp != other.Timestamp)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
		    const int prime = 31;
		    int result = 1;
		    result = prime * result + (IsSent ? 1231 : 1237);
		    result = prime * result + ((Message == null) ? 0 : Message.GetHashCode());
		    result = prime * result + ((Msisdn == null) ? 0 : Msisdn.GetHashCode());
            result = prime * result + MessageStatus.GetHashCode();
            result = prime * result + (int)(Timestamp ^ (Convert.ToUInt32(Timestamp) >> 32));
            
		    return result;
        }

        public string getTimestampFormatted()
        {
            return TimeUtils.getRelativeTime(Timestamp);
        }



        #endregion

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
