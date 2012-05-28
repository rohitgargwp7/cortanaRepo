﻿using System;
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
    [Index(Name = "Conversation_IDX", Columns = "ConversationId, Timestamp Desc")]
    public class ConvMessage : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region Messages Table member

            
        private long _messageId;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public long MessageId
        {
            get
            {
                return _messageId;
            }
            //set
            //{
            //    if (_messageId != value)
            //    {
            //        NotifyPropertyChanging("MessageId");
            //        _messageId = value;
            //        NotifyPropertyChanged("MessageId");
            //    }
            //}
        }

        private String _message;

        [Column]
        public String Message
        {
            get
            {
                return _message;
            }
            //set
            //{
            //    if (_message != value)
            //    {
            //        NotifyPropertyChanging("Message");
            //        _message = value;
            //        NotifyPropertyChanged("Message");
            //    }
            //}
        }

        private State _messageStatus;

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

        /* Adding entries to the beginning of this list is not backwards compatible */
        public enum State
        {
            SENT_UNCONFIRMED =0 ,  /* message sent to server */
            SENT_FAILED, /* message could not be sent, manually retry */
            SENT_CONFIRMED, /* message received by server */
            SENT_DELIVERED, /* message delivered to client device */
            SENT_DELIVERED_READ, /* message viewed by recipient */
            RECEIVED_UNREAD, /* message received, but currently unread */
            RECEIVED_READ, /* message received an read */
            UNKNOWN
        };

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

        private long _mappedMessageId;

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


        private int _conversationId;

        [Column]
        public int ConversationId
        {
            get
            {
                return _conversationId;
            }
            set
            {
                if (_conversationId != value)
                {
                    NotifyPropertyChanging("ConversationId");
                    _conversationId = value;
                    NotifyPropertyChanged("ConversationId");
                }
            }
        }

        private bool _isInvite;

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

        private bool _isSent;
        public bool IsSent
        {
            get
            {
                return _isSent;
            }
        }

        private bool _isSms;
        public bool IsSms
        {
            get
            {
                return _isSms;
            }
        }

        private String _msisdn;
        public String Msisdn
        {
            get
            {
                return _msisdn;
            }
        }


        public ConvMessage(String message, String msisdn, long timestamp, State msgState)
            : this(message, msisdn, timestamp, msgState, -1, -1)
        {
        }

        public ConvMessage(String message, String msisdn, long timestamp, State msgState, long msgid, long mappedMsgId)
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
//            String mappedMsgID = data[HikeConstants.MESSAGE_ID].StringValue;
//            this.MappedMessageId = System.Int64.Parse(mappedMsgID);
	    }

        public ConvMessage()
        { 
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
		    //result = prime * result + ((MessageStatus.GetHashCode() == null) ? 0 : MessageStatus.GetHashCode());
            result = prime * result + MessageStatus.GetHashCode();
            //TODO unsigned right shift
//            Convert.ToUInt32(Timestamp);
            result = prime * result + (int)(Timestamp ^ (Convert.ToUInt32(Timestamp) >> 32));
            
		    return result;
        }

        public String getTimestampFormatted()
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
