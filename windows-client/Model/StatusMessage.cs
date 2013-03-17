using Microsoft.Phone.Data.Linq.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.Model
{
    [Table(Name = "STATUS_UPDATES_TABLE")]
    public class StatusMessage : INotifyPropertyChanging
    {
        string _msisdn;
        string _message; // this will be stored in JSON format
        StatusType _type;
        long _timestamp;
        string _serverId;
        long _msgId; // this is the id of convmsg used to delete status in messagestable
        string _mood;
        bool _showOnTimeline;
        bool _isUnread;

        public enum StatusType
        {
            FRIEND_REQUEST,
            TEXT_UPDATE,
            PROFILE_PIC_UPDATE,
            IS_NOW_FRIEND
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, long id)
            : this(msisdn, msg, type, mappedId, ts, true,id, null, true)
        {
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, long id, bool isUnRead)
            : this(msisdn, msg, type, mappedId, ts,true ,id, null, isUnRead)
        {
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, bool showOnTimeline, long msgId, string mood, bool isUnread)
        {
            _msisdn = msisdn;
            _message = msg;
            _type = type;
            _serverId = mappedId;
            _timestamp = ts;
            _msgId = msgId;
            _mood = mood;
            _isUnread = isUnread;
            _showOnTimeline = showOnTimeline;
        }

        public StatusMessage()
        {
        }

        public StatusMessage(string msisdn, string id1, StatusType statusType, string id2, long p1, bool p2, int p3)
        {
            // TODO: Complete member initialization
            this.Msisdn = msisdn;
            this.id1 = id1;
            this.statusType = statusType;
            this.id2 = id2;
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        [Column(IsVersion = true)]
        private Binary version;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public long StatusId
        {
            get;
            set;
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

        [Column]
        public StatusType Status_Type
        {
            get
            {
                return _type;
            }
            set
            {
                if (_type != value)
                {
                    NotifyPropertyChanging("Status_Type");
                    _type = value;
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
        public string ServerId
        {
            get
            {
                return _serverId;
            }
            set
            {
                if (_serverId != value)
                {
                    NotifyPropertyChanging("ServerId");
                    _serverId = value;
                }
            }
        }

        [Column]
        public long MsgId
        {
            get
            {
                return _msgId;
            }
            set
            {
                if (_msgId != value)
                {
                    NotifyPropertyChanging("MsgId");
                    _msgId = value;
                }
            }
        }

        [Column]
        public string Mood
        {
            get
            {
                return _mood;
            }
            set
            {
                if (_mood != value)
                {
                    NotifyPropertyChanging("Mood");
                    _mood = value;
                }
            }
        }

        [Column]
        public bool ShowOnTimeline
        {
            get
            {
                return _showOnTimeline;
            }
            set
            {
                if (value != _showOnTimeline)
                {
                    NotifyPropertyChanging("ShowOnTimeline");
                    _showOnTimeline = value;
                }
            }
        }

        public bool IsUnread
        {
            get
            {
                return _isUnread;
            }
            set
            {
                _isUnread = value;
            }
        }

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;
        private string id1;
        private StatusType statusType;
        private string id2;
        private long p1;
        private bool p2;
        private int p3;

        // Used to notify that a property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                try
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StatusMessage ::  NotifyPropertyChanging : NotifyPropertyChanging, Exception : " + ex.StackTrace);
                }
            }
        }
        #endregion
    }
}
