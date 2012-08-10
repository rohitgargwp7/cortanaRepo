﻿using System;
using System.Windows;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;

namespace windows_client.Model
{
    [Table(Name = "messages")]
    [Index(Columns = "Msisdn,Timestamp ASC", IsUnique = false, Name = "Msg_Idx")]
    public class ConvMessage : INotifyPropertyChanged, INotifyPropertyChanging
    {

        private long _messageId; // this corresponds to msgID stored in sender's DB
        private string _msisdn;
        private string _message;
        private State _messageStatus;
        private long _timestamp;
        private long _mappedMessageId; // this corresponds to msgID stored in receiver's DB
        private bool _isInvite;
        private bool _isSent;
        private bool _isSms;
        private string _groupParticipant;
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
        }

        public enum ChatBubbleType
        {
            RECEIVED = 0,
            HIKE_SENT,
            SMS_SENT
        }

        #region Messages Table member

        [Column(IsVersion = true)]
        private Binary version;

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
                }
            }
        }

        [Column(IsDbGenerated = false, UpdateCheck = UpdateCheck.Never)]
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
                    _messageStatus = value;
                    NotifyPropertyChanged("MessageStatus");
                    NotifyPropertyChanged("SdrImage");
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
        [Column]
        public string GroupParticipant
        {
            get
            {
                return _groupParticipant;
            }
            set
            {
                if (_groupParticipant != value)
                {
                    NotifyPropertyChanging("GroupParticipant");
                    _groupParticipant = value;
                    NotifyPropertyChanged("GroupParticipant");
                }
            }
        }

        public ChatBubbleType MsgType
        {
            get
            {
                if (!IsSent)
                    return ChatBubbleType.RECEIVED;
                if (IsSms)
                    return ChatBubbleType.SMS_SENT;
                return ChatBubbleType.HIKE_SENT;
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
                return (_messageStatus == State.SENT_UNCONFIRMED ||
                        _messageStatus == State.SENT_CONFIRMED ||
                        _messageStatus == State.SENT_DELIVERED ||
                        _messageStatus == State.SENT_DELIVERED_READ ||
                        _messageStatus == State.SENT_FAILED);
            }
        }

        public bool IsSms
        {
            get
            {
                return _isSms;
            }
            set
            {
                if (value != _isSms)
                    _isSms = value;
            }
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState)
            : this(message, msisdn, timestamp, msgState, -1, -1)
        {
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState, long msgid, long mappedMsgId)
        {
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
            _msisdn = (string)obj[HikeConstants.FROM]; /*represents msg is coming from another client*/
            JObject data = (JObject)obj[HikeConstants.DATA];
            JToken msg;

            if (data.TryGetValue(HikeConstants.SMS_MESSAGE, out msg))
            {
                _message = msg.ToString();
                _isSms = true;
            }
            else
            {
                _message = (string)data[HikeConstants.HIKE_MESSAGE];
                _isSms = false;
            }

            Timestamp = (long)data[HikeConstants.TIMESTAMP];

            /* prevent us from receiving a message from the future */

            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
            this.Timestamp = (this.Timestamp > now) ? now : this.Timestamp;

            /* if we're deserialized an object from json, it's always unread */
            this.MessageStatus = State.RECEIVED_UNREAD;
            this._messageId = -1;
            string mappedMsgID = (string)data[HikeConstants.MESSAGE_ID];
            this.MappedMessageId = System.Int64.Parse(mappedMsgID);
        }

        public ConvMessage()
        {
        }

        public JObject serialize(bool isHikeMsg)
        {
            JObject obj = new JObject();
            JObject data = new JObject();

            if(isHikeMsg)
                data[HikeConstants.HIKE_MESSAGE] = _message;
            else
                data[HikeConstants.SMS_MESSAGE] = _message;
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
            else if (Message.CompareTo(other.Message) != 0)
                return false;
            if (Msisdn == null)
            {
                if (other.Msisdn != null)
                    return false;
            }
            else if (Msisdn.CompareTo(other.Msisdn) != 0)
                return false;
            if (MessageStatus.Equals(other.MessageStatus))
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

        #region ChatThread Page Bindings for Converters

        public BitmapImage AvatarImage
        {
            get
            {
                return UI_Utils.Instance.getBitMapImage(_msisdn);
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

        public string Alignment
        {
            get
            {
                if (IsSent)
                    return "right";
                else
                    return "left";
            }
        }

        public string BubbleBackground
        {
            get
            {
                if (ChatBubbleType.RECEIVED == MsgType)
                {
                    return "#eeeeec";
                }
                else if (ChatBubbleType.HIKE_SENT == MsgType)
                {
                    return "#B1E0FB";
                }
                else
                {
                    return "#DBF2CF";
                }
            }
        }

        public string ChatBubbleMargin
        {
            get
            {
                if (IsSent)
                    return "15,0,10,10";
                else
                    return "5,0,10,10";
            }
        }

        public string SdrImageVisibility
        {
            get
            {
                if (IsSent)
                    return "Visible";
                else
                    return "Collapsed";
            }
        }

        public string ChatTimeFormat
        {
            get
            {
                return TimeUtils.getTimeString(_timestamp);
            }
        }
        #endregion

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
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

        public JObject serializeDeliveryReportRead()
        {
            JObject obj = new JObject();
            JArray ids = new JArray();
            try
            {
                ids.Add(Convert.ToString(_mappedMessageId));
                obj.Add(HikeConstants.DATA, ids);
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, _msisdn);
            }
            catch (Exception e)
            {

            }
            return obj;
        }
    }
}
