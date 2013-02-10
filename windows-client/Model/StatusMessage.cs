﻿using Microsoft.Phone.Data.Linq.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
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
        string _mappedId;
        bool _isRead;

        public enum StatusType
        {
            ADD_FRIEND,
            TEXT_UPDATE,
            PROFILE_PIC_UPDATE
        }

        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts)
            :this(msisdn, msg, type, mappedId, ts, true)
        {
        }


        public StatusMessage(string msisdn, string msg, StatusType type, string mappedId, long ts, bool isRead)
        {
            _msisdn = msisdn;
            _message = msg;
            _type = type;
            _mappedId = mappedId;
            _timestamp = ts;
            _isRead = isRead;
        }

        public StatusMessage(string msisdn, StatusType type, string mappedId, long ts)
        {
            _msisdn = msisdn;
            _message = null;
            _type = type;
            _mappedId = mappedId;
            _timestamp = ts;
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
        public string MappedId
        {
            get
            {
                return _mappedId;
            }
            set
            {
                if (_mappedId != value)
                {
                    NotifyPropertyChanging("MappedId");
                    _mappedId = value;
                }
            }
        }

        public bool IsRead
        {
            get
            {
                return _isRead;
            }
            set
            {
                _isRead = value;
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
                catch (Exception)
                { }
            }
        }
        #endregion
    }
}
