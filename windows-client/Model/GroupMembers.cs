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
using Microsoft.Phone.Data.Linq.Mapping;
using windows_client.utils;
using windows_client.View;

namespace windows_client.Model
{
    [Table(Name = "GROUP_MEMBERS_TABLE")]
    [Index(Columns = "GroupId,Msisdn", IsUnique = true, Name = "Grp_Idx")]
    public class GroupMembers : INotifyPropertyChanged, INotifyPropertyChanging
    {
        string _groupId;
        string _msisdn;
        string _name;
        bool _hasLeft;

        //TODO move all colors in a a single file
       
        [Column(IsVersion = true)]
        private Binary version;

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id
        {
            get;
            set;
        }

        [Column]
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
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    NotifyPropertyChanging("Name");
                    _name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        [Column]
        public bool HasLeft
        {
            get
            {
                return _hasLeft;
            }
            set
            {
                if (_hasLeft != value)
                {
                    NotifyPropertyChanging("HasLeft");
                    _hasLeft = value;
                    NotifyPropertyChanged("HasLeft");
                }
            }
        }

        public SolidColorBrush SquareColor
        {
            get 
            {
                if (Utils.getGroupParticipant(Name, Msisdn).IsOnHike)
                {
                    return UI_Utils.Instance.hikeMsgBackground;
                }
                return UI_Utils.Instance.smsBackground;
            }
        }

        public GroupMembers()
        {
        }

        public GroupMembers(string gId, string msisdn, string name, bool hasLeft)
        {
            _groupId = gId;
            _msisdn = msisdn;
            _name = name;
            _hasLeft = hasLeft;
        }

        public GroupMembers(string gId, string msisdn, string name):this(gId,msisdn,name,false)
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
            GroupMembers other = (GroupMembers)obj;

            if (GroupId != other.GroupId)
                return false;
            if (Msisdn != other.Msisdn)
                return false;
            return true;
        }

        public override string ToString()
        {
            if(String.IsNullOrEmpty(_name))
            {
                return _msisdn;
            }
            return _name;
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
