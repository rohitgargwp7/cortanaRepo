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

namespace windows_client.Model
{
    [Table(Name = "GROUP_INFO_TABLE")]
    public class GroupInfo : INotifyPropertyChanged, INotifyPropertyChanging
    {
        string _groupId;
        string _groupName;
        string _groupOwner;
        bool _groupAlive;

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
                    NotifyPropertyChanged("GroupId");
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
                    NotifyPropertyChanged("GroupName");
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
                    NotifyPropertyChanged("GroupOwner");
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
                    NotifyPropertyChanged("GroupAlive");
                }
            }
        }

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
    }
}
