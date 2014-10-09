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
using System.Data.Linq;
using Newtonsoft.Json.Linq;

namespace CommonLibrary.Model
{
    [Table(Name = "GROUP_INFO_TABLE")]
    public class GroupInfo : INotifyPropertyChanging
    {
        string _groupId;
        string _groupName;
        string _groupOwner;
        bool _groupAlive;
        string _readByInfo;
        long? _lastReadMessageId;

        public GroupInfo() { }

        public GroupInfo(string grpId, string grpName, string grpOwner, bool isGrpAlive)
        {
            _groupId = grpId;
            _groupName = grpName;
            _groupOwner = grpOwner;
            _groupAlive = isGrpAlive;
        }

        [Column(IsVersion = true)]
        private Binary version;

        [Column(IsPrimaryKey = true)]
        public string GroupId
        {
            get
            {
                return _groupId;
            }
            set
            {
                if (_groupId != value)
                {
                    NotifyPropertyChanging("GroupId");
                    _groupId = value;
                }
            }
        }

        [Column]
        public string GroupName
        {
            get
            {
                return _groupName;
            }
            set
            {
                if (_groupName != value)
                {
                    NotifyPropertyChanging("GroupName");
                    _groupName = value;
                }
            }
        }

        [Column]
        public string GroupOwner
        {
            get
            {
                return _groupOwner;
            }
            set
            {
                if (_groupOwner != value)
                {
                    NotifyPropertyChanging("GroupOwner");
                    _groupOwner = value;
                }
            }
        }

        [Column]
        public bool GroupAlive
        {
            get
            {
                return _groupAlive;
            }
            set
            {
                if (_groupAlive != value)
                {
                    NotifyPropertyChanging("GroupAlive");
                    _groupAlive = value;
                }
            }
        }

        [Column(CanBeNull = true)]
        public long? LastReadMessageId
        {
            get
            {
                return _lastReadMessageId ?? 0;
            }
            set
            {
                if (_lastReadMessageId != value)
                {
                    NotifyPropertyChanging("LastReadMessageId");
                    _lastReadMessageId = value;
                }
            }
        }

        [Column(CanBeNull = true)]
        public string ReadByInfo
        {
            get
            {
                return _readByInfo;
            }
            set
            {
                if (_readByInfo != value)
                {
                    NotifyPropertyChanging("ReadByInfo");
                    _readByInfo = value;
                }
            }
        }

        JArray _readByArray;
        public JArray ReadByArray
        {
            get
            {
                if (_readByArray == null)
                {
                    if (String.IsNullOrEmpty(_readByInfo))
                        return null;
                    else
                        _readByArray = JArray.Parse(_readByInfo);
                }

                return _readByArray;
            }
            set
            {
                if (value != _readByArray)
                    _readByArray = value;
            }
        }

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
