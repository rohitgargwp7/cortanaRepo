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
using windows_client.utils;
using System.ComponentModel;

namespace windows_client.Model
{
    public class GroupParticipant : INotifyPropertyChanged, INotifyPropertyChanging,IComparable<GroupParticipant>
    {
        private string _grpId;
        private string _name; // this is  full name
        private string _msisdn;
        private bool _hasLeft;
        private bool _isOnHike;
        private bool _isDND;
        private bool _hasOptIn;
        private bool _isUsed;

        public GroupParticipant()
        { }

        public GroupParticipant(string grpId, string name, string msisdn, bool isOnHike)
        {
            _grpId = grpId;
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = true;
            _hasOptIn = false;
            _hasLeft = false;
        }

        public GroupParticipant(string name, string msisdn, bool isOnHike)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = false;
            _hasOptIn = false;
        }

        public GroupParticipant(string name, string msisdn, bool isOnHike,bool isDND)
        {
            _name = name;
            _msisdn = msisdn;
            _isOnHike = isOnHike;
            _isDND = isDND;
        }

        public string GroupId
        {
            get
            {
                return _grpId;
            }
            set
            {
                if (value != _grpId)
                    _grpId = value;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (value != _name)
                    _name = value;
            }
        }

        public string FirstName
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    return null;
                _name = _name.Trim();
                int idx = _name.IndexOf(" ");
                if (idx != -1)
                    return _name.Substring(0, idx);
                else
                    return _name;
            }
        }

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (value != _msisdn)
                    _msisdn = value;
            }
        }

        public bool IsOnHike
        {
            get
            {
                return _isOnHike;
            }
            set
            {
                if (value != _isOnHike)
                    _isOnHike = value;
            }
        }

        public bool IsDND
        {
            get
            {
                return _isDND;
            }
            set
            {
                if (value != _isDND)
                    _isDND = value;
            }
        }

        public bool HasOptIn
        {
            get
            {
                return _hasOptIn;
            }
            set
            {
                if (value != _hasOptIn)
                    _hasOptIn = value;
            }
        }

        public bool HasLeft
        {
            get
            {
                return _hasLeft;
            }
            set
            {
                if (value != _hasLeft)
                    _hasLeft = value;
            }
        }

        public bool IsUsed
        {
            get
            {
                return _isUsed;
            }
            set
            {
                if (value != _isUsed)
                    _isUsed = value;
            }
        }

        public int IsOwner
        {
            get;
            set;
        }

        public SolidColorBrush SquareColor
        {
            get
            {
                if (Utils.getGroupParticipant(Name, Msisdn, _grpId).IsOnHike)
                {
                    return UI_Utils.Instance.HikeMsgBackground;
                }
                return UI_Utils.Instance.SmsBackground;
            }
        }

        public int CompareTo(GroupParticipant rhs)
        {
            return (this.Name.ToLower().CompareTo(((GroupParticipant)rhs).Name.ToLower()));
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
