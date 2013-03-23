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
        string _moodInfo;
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
            : this(msisdn, msg, type, mappedId, ts, true, id, null, true)
        {
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, long id, bool isUnRead)
            : this(msisdn, msg, type, mappedId, ts, true, id, null, isUnRead)
        {
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, bool showOnTimeline,
            long msgId, string moodInfo, bool isUnread)
        {
            _msisdn = msisdn;
            _message = msg;
            _type = type;
            _serverId = mappedId;
            _timestamp = ts;
            _msgId = msgId;
            _moodInfo = moodInfo;
            _isUnread = isUnread;
            _showOnTimeline = showOnTimeline;
        }

        public StatusMessage()
        {
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
        public string MoodInfo
        {
            get
            {
                return _moodInfo;
            }
            set
            {
                if (_moodInfo != value)
                {
                    NotifyPropertyChanging("MoodInfo");
                    _moodInfo = value;
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

        public int TimeOfDay
        {
            get
            {
                if (_moodInfo == null || string.IsNullOrWhiteSpace(_moodInfo))
                    return 0;
                else
                {
                    string[] vals = _moodInfo.Split(':');
                    return Int32.Parse(vals[1]);
                }
            }
        }

        public int MoodId
        {
            get
            {
                if (_moodInfo == null || string.IsNullOrWhiteSpace(_moodInfo))
                    return 0;
                else
                {
                    string[] vals = _moodInfo.Split(':');
                    return Int32.Parse(vals[0]);
                }
            }
        }

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
                catch (Exception ex)
                {
                    Debug.WriteLine("StatusMessage ::  NotifyPropertyChanging : NotifyPropertyChanging, Exception : " + ex.StackTrace);
                }
            }
        }
        #endregion
    }
}
