using System;
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
using windows_client.DbUtils;
using windows_client.Languages;

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
        private bool _onHike;
        private bool _hasCustomPhoto;//for Nux
        private bool _isInvited;
        private byte[] _avatar;
        private bool _isFav;
        private bool _isCloseFriendNux;//for Nux
        private byte _nuxScore;//for Nux

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

        public bool IsCloseFriendNux
        {
            get
            {
                return _isCloseFriendNux;
            }
            set
            {
                _isCloseFriendNux = value;
            }
        }

        public byte NuxMatchScore
        {
            get
            {
                return _nuxScore;
            }
            set
            {
                _nuxScore = value;
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
            this._hasCustomPhoto = contact._hasCustomPhoto;
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
                if (value != _avatar)
                    _avatar = value;
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
                    // donot add this bitmap to map as this may not be used everywhere in the app
                    // also this would fill the map with bitmaps for all the contacts

                    if (_avatar == null)
                    {
                        if (Utils.isGroupConversation(_msisdn))
                            return UI_Utils.Instance.getDefaultGroupAvatar(_msisdn);
                        return UI_Utils.Instance.getDefaultAvatar(_msisdn);
                    }
                    else
                    {
                        return UI_Utils.Instance.createImageFromBytes(_avatar);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ContactInfo :: AvatarImage : fetch AvatarImage, Exception : " + ex.StackTrace);
                    return null;
                }
            }
        }

        public void Write(BinaryWriter writer)
        {
            try
            {
                if (_name == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_name);

                writer.WriteStringBytes(_phoneNo);//cannot be null for nux
                writer.WriteStringBytes(_id);//cannot be null for nux

                writer.Write(_nuxScore);
                writer.Write(_hasCustomPhoto);
                if (_avatar != null)
                {
                    writer.Write(_avatar.Length);
                    writer.Write(_avatar);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("ContactInfo :: Write : Unable To write, Exception : " + ex.StackTrace);
                throw new Exception("Unable to write to a file...");
            }

        }

        public void Read(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _name = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_name == "*@N@*")
                    _name = null;
                count = reader.ReadInt32();
                _phoneNo = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                count = reader.ReadInt32();
                _id = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);

                _nuxScore = reader.ReadByte();
                _hasCustomPhoto = reader.ReadBoolean();
                if (_hasCustomPhoto)
                {
                    count = reader.ReadInt32();
                    _avatar = reader.ReadBytes(count);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ContactInfo :: Read : Read, Exception : " + ex.StackTrace);
                throw new Exception("Conversation Object corrupt");
            }
        }
    }

    public class ContactCompare : IComparer<ContactInfo>
    {
        public int Compare(ContactInfo contactInfo1, ContactInfo contactInfo2)
        {
            if (contactInfo1.HasCustomPhoto == contactInfo2.HasCustomPhoto)
            {
                if (contactInfo1.NuxMatchScore > contactInfo2.NuxMatchScore)
                    return -1;
                else if (contactInfo1.NuxMatchScore < contactInfo2.NuxMatchScore)
                    return 1;
                else
                {
                    return contactInfo1.Name.ToLower().CompareTo(contactInfo2.Name.ToLower());
                }
            }
            else if (contactInfo1.HasCustomPhoto)
            {
                return -1;
            }
            else
            {
                return 1;
            }
        }
    }
}
