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
using System.Collections.Generic;
using windows_client.Misc;
using System.Text;

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
        private bool _hasCustomPhoto;
        private bool _onHike;
        private bool _isInvited;
        private byte[] _avatar;
        private bool _isFav;

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

        public bool IsInvited
        {
            get
            {
                return _isInvited;
            }
            set
            {
                NotifyPropertyChanging("IsInvited");
                _isInvited = value;
                NotifyPropertyChanged("IsInvited");
                NotifyPropertyChanged("InvitedStringVisible");
                NotifyPropertyChanged("InviteButtonVisible");
            }
        }

        public bool IsFav
        {
            get
            {
                return _isFav;
            }
            set
            {
                if (value != _isFav)
                {
                    _isFav = value;
                }
            }
        }   // this is used in inviteUsers page , when you show hike users

        public bool IsInvite
        {
            get;
            set;
        }

        public bool IsEnabled
        {
            get
            {
                if (_isFav && !IsInvite)
                    return false;
                return true;
            }
        }

        public Visibility InvitedStringVisible
        {
            get
            {
                if (IsInvited)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility InviteButtonVisible
        {
            get
            {
                if (IsInvited)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public ContactInfo()
        {
            _name = null;
        }

        public ContactInfo(string number, string name, string phoneNum)
            : this(null, number, name, false, phoneNum, false)
        {
        }

        public ContactInfo(string id, string number, string name, string phoneNum)
            : this(id, number, name, false, phoneNum, false)
        {
        }

        public ContactInfo(string id, string number, string name, bool onHike, string phoneNum) :
            this(id, number, name, onHike, phoneNum, false)
        {
        }

        public ContactInfo(string number, string name, bool onHike) :
            this(null, number, name, onHike, number, false)
        {
        }

        public ContactInfo(string id, string msisdn, string name, bool onhike, string phoneNo, bool hasCustomPhoto)
        {
            this.Id = id;
            this.Msisdn = msisdn;
            this.Name = name;
            this.OnHike = onhike;
            this.PhoneNo = phoneNo;
            this.HasCustomPhoto = hasCustomPhoto;
            this.IsInvited = false;
        }

        public ContactInfo(ContactInfo contact)
        {
            this._msisdn = contact._msisdn;
            this._name = contact._name;
            this._onHike = contact._onHike;
            this._phoneNo = contact._phoneNo;
            this._isInvited = contact._isInvited;
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
            result = prime * result + ((string.IsNullOrWhiteSpace(Name) == null) ? 0 : Name.GetHashCode());
            result = prime * result + ((PhoneNo == null) ? 0 : PhoneNo.GetHashCode());
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
                    catch { }
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
                if (value != _avatar)
                    _avatar = value;
            }
        }

        public BitmapImage AvatarImage
        {
            get
            {
                try
                {
                    if (_avatar == null)
                    {
                        if (Utils.isGroupConversation(Msisdn))
                            return UI_Utils.Instance.getDefaultGroupAvatar(Msisdn);
                        return UI_Utils.Instance.getDefaultAvatar(Msisdn);
                    }
                    else
                    {
                        MemoryStream memStream = new MemoryStream(_avatar);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BitmapImage empImage = new BitmapImage();
                        empImage.SetSource(memStream);
                        return empImage;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in Avatar Image : {0}", e.ToString());
                    return null;
                }
            }
        }
    }
}
