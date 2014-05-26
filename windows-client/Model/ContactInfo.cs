﻿using System;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Windows.Media.Imaging;
using windows_client.utils;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using windows_client.Misc;
using System.Text;
using windows_client.DbUtils;
using windows_client.Languages;
using windows_client.View;

namespace windows_client.Model
{
    [Table(Name = "users")]
    [Index(Columns = "Msisdn", IsUnique = false, Name = "Msisdn_Idx")]
    public class ContactInfo : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<ContactInfo>
    {
        private int _dbId;
        private string _id;
        private string _name;
        private string _msisdn;
        private string _phoneNo;
        private bool _hasCustomPhoto;//used to show group chat in select user page
        private bool _onHike;
        private byte[] _avatar;
        private bool _isFav;
        private int? _phoneNoKind;

        # region Users Table Members

        [Column(IsVersion = true)]
        private Binary version;

        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int DbId
        {
            get
            {
                return _dbId;
            }
            set
            {
                if (_dbId != value)
                {
                    _dbId = value;
                }
            }
        }
        [Column(UpdateCheck = UpdateCheck.Never)]
        public string Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    NotifyPropertyChanging("Id");
                    _id = value;
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

        [Column(CanBeNull = false)]
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
        public bool OnHike
        {
            get
            {
                return _onHike;
            }
            set
            {
                if (_onHike != value)
                {
                    NotifyPropertyChanging("OnHike");
                    _onHike = value;
                    NotifyPropertyChanged("OnHike");
                }
            }
        }

        [Column]
        public string PhoneNo
        {
            get
            {
                return _phoneNo;
            }
            set
            {
                NotifyPropertyChanging("PhoneNo");
                _phoneNo = value;
                NotifyPropertyChanged("PhoneNo");
            }
        }

        [Column]
        public bool HasCustomPhoto
        {
            get
            {
                return _hasCustomPhoto;
            }
            set
            {
                NotifyPropertyChanging("HasCustomPhoto");
                _hasCustomPhoto = value;
                NotifyPropertyChanged("HasCustomPhoto");
            }
        }

        [Column(CanBeNull = true)]
        public int? PhoneNoKind
        {
            get
            {
                return _phoneNoKind;
            }
            set
            {
                NotifyPropertyChanging("PhoneNoKind");
                _phoneNoKind = value;
                NotifyPropertyChanged("PhoneNoKind");
            }
        }

        //used for block unblock also
        public bool IsFav
        {
            get
            {
                return _isFav;
            }
            set
            {
                if (value != _isFav)
                    _isFav = value;
            }
        }

        bool _isSelected;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("IsSelected");
                }
            }
        }

        String _contactListLabel;
        public String ContactListLabel
        {
            get
            {
                return String.IsNullOrEmpty(_contactListLabel) ? Msisdn : _contactListLabel;
            }
            set
            {
                if (value != _contactListLabel)
                {
                    _contactListLabel = value;
                    NotifyPropertyChanged("ContactListLabel");
                }
            }
        }

        public bool IsEnabled
        {
            get
            {
                if (_isFav)
                    return false;
                return true;
            }
        }

        Visibility _checkBoxVisibility = Visibility.Collapsed;
        public Visibility CheckBoxVisibility
        {
            get
            {
                return _checkBoxVisibility;
            }
            set
            {
                if (value != _checkBoxVisibility)
                {
                    _checkBoxVisibility = value;
                    NotifyPropertyChanged("CheckBoxVisibility");
                }
            }
        }
        
        public ContactInfo()
        {
            _name = null;
        }

        public ContactInfo(string number, string name, string phoneNum, int phoneNoKind)
            : this(null, number, name, false, phoneNum, phoneNoKind, false)
        {
        }

        public ContactInfo(string id, string number, string name, string phoneNum, int phoneNoKind)
            : this(id, number, name, false, phoneNum, phoneNoKind, false)
        {
        }

        public ContactInfo(string id, string number, string name, bool onHike, string phoneNum, int? phoneNoKind) :
            this(id, number, name, onHike, phoneNum, phoneNoKind, false)
        {
        }

        public ContactInfo(string number, string name, bool onHike) :
            this(null, number, name, onHike, number, null, false)
        {
        }

        public ContactInfo(string id, string msisdn, string name, bool onhike, string phoneNo, int? phoneNoKind, bool hasCustomPhoto)
        {
            this.Id = id;
            this.Msisdn = msisdn;
            this.Name = name;
            this.OnHike = onhike;
            this.PhoneNo = phoneNo;
            this.HasCustomPhoto = hasCustomPhoto;
            this.PhoneNoKind = phoneNoKind;
        }

        public ContactInfo(ContactInfo contact)
        {
            this._id = contact.Id;
            this._msisdn = contact._msisdn;
            this._name = contact._name;
            this._onHike = contact._onHike;
            this._phoneNo = contact._phoneNo;
            this._phoneNoKind = contact._phoneNoKind;
        }


        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            ContactInfo other = (ContactInfo)obj;

            // if msisdn of two contacts are equal they should be equal
            // if msisdn is not there then other things should be compared
            if (!string.IsNullOrEmpty(_msisdn))
            {
                if (string.IsNullOrWhiteSpace(other.Msisdn))
                    return false;
                else if (_msisdn == other.Msisdn)
                    return true;
            }
            
            if (string.IsNullOrWhiteSpace(Name))
            {
                if (!string.IsNullOrWhiteSpace(other.Name))
                    return false;
            }
            else if (Name.CompareTo(other.Name) != 0)
                return false;
            if (PhoneNo == null)
            {
                if (other.PhoneNo != null)
                    return false;
            }
            else if (PhoneNo.CompareTo(other.PhoneNo) != 0)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + (string.IsNullOrWhiteSpace(Name) ? 0 : Name.GetHashCode());
            result = prime * result + (PhoneNo == null ? 0 : PhoneNo.GetHashCode());
            return result;
        }

        public int CompareTo(ContactInfo rhs)
        {
            return (this.Name.ToLower().CompareTo(((ContactInfo)rhs).Name.ToLower()));
        }

        public override string ToString()
        {
            return _name;
        }

        public BitmapImage HikeStatusImage
        {
            get
            {
                if (_onHike)
                    return UI_Utils.Instance.OnHikeImage;
                else
                    return UI_Utils.Instance.NotOnHikeImage;
            }
        }

        public string PhoneNoKindText
        {
            get
            {
                return PhoneNoKind == null ? String.Empty : "(" + Enum.GetName(typeof(Microsoft.Phone.UserData.PhoneNumberKind), PhoneNoKind).ToString(CultureInfo.CurrentCulture).ToLower() + ")";
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ContactInfo :: NotifyPropertyChanged : NotifyPropertyChanged, Exception : " + ex.StackTrace);
                    }
                });
            }
        }

        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;
        private ContactInfo contact;

        // Used to notify that a property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
        #endregion

        public class DelContacts
        {
            private string _id;
            private string _msisdn;

            public string Id
            {
                get
                {
                    return _id;
                }
            }
            public string Msisdn
            {
                get
                {
                    return _msisdn;
                }
            }
            public DelContacts(string id, string msisdn)
            {
                _id = id;
                _msisdn = msisdn;
            }
        }

        public byte[] Avatar
        {
            get
            {
                return _avatar;
            }
            set
            {
                _avatar = value;
                NotifyPropertyChanged("AvatarImage");
            }
        }

        public FriendsTableUtils.FriendStatusEnum FriendStatus
        {
            get;
            set;
        }

        public BitmapImage AvatarImage
        {
            get
            {
                try
                {
                    // get image and save in cache too
                    return UI_Utils.Instance.GetBitmapImage(_msisdn);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ContactInfo :: AvatarImage : fetch AvatarImage, Exception : " + ex.StackTrace);
                    return null;
                }
            }
        }

        public class MsisdnComparer : IEqualityComparer<ContactInfo>
        {
            public bool Equals(ContactInfo x, ContactInfo y)
            {
                return x.Msisdn == y.Msisdn;
            }

            public int GetHashCode(ContactInfo obj)
            {
                return obj.Msisdn.GetHashCode();
            }
        }
    }
}
