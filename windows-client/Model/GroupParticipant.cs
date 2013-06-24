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
using System.Diagnostics;
using System.IO;
using windows_client.Misc;
using System.Text;
using windows_client.Languages;

namespace windows_client.Model
{
    public class GroupParticipant : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<GroupParticipant>, IBinarySerializable
    {
        private string _grpId;
        private string _name; // this is full name
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

        public GroupParticipant(string name, string msisdn, bool isOnHike, bool isDND)
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
                {
                    _name = value;
                    NotifyPropertyChanged("Name");
                    NotifyPropertyChanged("AddUserVisibility");
                }
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
                {
                    NotifyPropertyChanging("IsOnHike");
                    _isOnHike = value;
                    NotifyPropertyChanged("IsOnHike");
                    NotifyPropertyChanged("SquareColor");
                }
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

        public bool IsOwner
        {
            get;
            set;
        }

        public bool IsFav
        {
            get
            {
                if (App.ViewModel.Isfavourite(_msisdn))
                    return true;
                return false;
            }
            set
            {
                NotifyPropertyChanged("IsFav");
                NotifyPropertyChanged("FavMsg");
            }
        }
        public string GroupInfoBlockText
        {
            get
            {
                if (IsOwner)
                {
                    return AppResources.Owner_Txt;
                }
                else if (!_isOnHike)
                {
                    return AppResources.OnSms_Txt;
                }
                return string.Empty;
            }
        }
        public Visibility ShowGroupInfoBLock
        {
            get
            {
                if (IsOwner || !_isOnHike)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Collapsed;
                }
            }
        }
        public string FavMsg
        {
            get
            {
                if (IsFav) // if already favourite
                    return AppResources.RemFromFav_Txt;
                else
                    return AppResources.Add_To_Fav_Txt;
            }
        }

        public Visibility ShowAddTofav
        {
            // it should not be shown for self
            get
            {
                if (_msisdn == App.MSISDN)
                    return Visibility.Collapsed;
                return Visibility.Visible;
            }
        }

        public Visibility RemoveFromGroup
        {
            get;
            set;
        }

        public Visibility AddUserVisibility
        {
            get
            {
                if (_msisdn.Contains(_name))
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public Visibility ContextMenuVisibility
        {
            get
            {
                if (AddUserVisibility == Visibility.Visible || RemoveFromGroup == Visibility.Visible || ShowAddTofav == Visibility.Visible)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public bool ContextMenuIsEnabled
        {
            get
            {
                if (ContextMenuVisibility == Visibility.Visible)
                    return true;
                return false;
            }
        }

        public SolidColorBrush SquareColor
        {
            get
            {
                if (_isOnHike)
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

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            GroupParticipant o = obj as GroupParticipant;

            if ((System.Object)o == null)
            {
                return false;
            }
            return (_msisdn == o.Msisdn);
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                if (_grpId == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_grpId);

                if (_name == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_name);

                if (_msisdn == null)
                    writer.WriteStringBytes(string.Empty);
                else
                    writer.WriteStringBytes(_msisdn);
                writer.Write(_hasLeft);
                writer.Write(_isOnHike);
                writer.Write(_isDND);
                writer.Write(_hasOptIn);
                writer.Write(_isUsed);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GroupParticipant ::  Write : Write, Exception : " + ex.StackTrace);
                throw new Exception("Unable to write to a file...");
            }
        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _grpId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_grpId == string.Empty)
                    _grpId = null;
                count = reader.ReadInt32();
                _name = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_name == string.Empty) // this is done so that we can specifically set null if contact name is not there
                    _name = null;
                count = reader.ReadInt32();
                _msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_msisdn == string.Empty)
                    _msisdn = null;
                _hasLeft = reader.ReadBoolean();
                _isOnHike = reader.ReadBoolean();
                _isDND = reader.ReadBoolean();
                _hasOptIn = reader.ReadBoolean();
                _isUsed = reader.ReadBoolean();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("GroupParticipant ::  Read : Read, Exception : " + ex.StackTrace);
                throw new Exception("Conversation Object corrupt");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (PropertyChanged != null)
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception in property : {0}. Exception : {1}", propertyName, ex.StackTrace);
                    }
                }
            });
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
