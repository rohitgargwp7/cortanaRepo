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
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using CodeTitans.JSon;

namespace windows_client.Model
{
    [Table(Name = "users")]
    public class ContactInfo : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<ContactInfo>
    {

        //it significantly improves update performance


        # region Users Table Members
        private String _id;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public String Id
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
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private String _name;

        [Column]
        public String Name
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

        private String _msisdn;

        [Column(CanBeNull = false)]
        public String Msisdn
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

        private bool _onHike;

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

        private String _phoneNo;

        [Column]
        public String PhoneNo
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

        private bool _hasCustomPhoto;
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


        public ContactInfo()
        {
        }


        public ContactInfo(String id, String number, String name, String phoneNum)
            : this(id, number, name, false, phoneNum, false)
        {
        }

        public ContactInfo(String id, String number, String name, bool onHike, String phoneNum):            
            this(id, number, name, onHike, phoneNum, false)
        {
        }

        public ContactInfo(String id, String msisdn, String name, bool onhike, String phoneNo, bool hasCustomPhoto)
        {
            this.Id = id;
            this.Msisdn = msisdn;
            this.Name = name;
            this.OnHike = onhike;
            this.PhoneNo = phoneNo;
            this.HasCustomPhoto = hasCustomPhoto;
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
            if (Id == null)
            {
                if (other.Id != null)
                    return false;
            }
            else if (Id.CompareTo(other.Id)!=0)
                return false;
            if (Name == null)
            {
                if (other.Name != null)
                    return false;
            }
            else if (Name.CompareTo(other.Name)!=0)
                return false;
            if (PhoneNo == null)
            {
                if (other.PhoneNo != null)
                    return false;
            }
            else if (PhoneNo.CompareTo(other.PhoneNo)!=0)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
		    const int prime = 31;
		    int result = 1;
		    result = prime * result + ((Id == null) ? 0 : Id.GetHashCode());
		    result = prime * result + ((Name == null) ? 0 : Name.GetHashCode());
		    result = prime * result + ((PhoneNo == null) ? 0 : PhoneNo.GetHashCode());
		    return result;
        }

        public int CompareTo(ContactInfo rhs)
        {
            return (this.Name.ToLower().CompareTo(((ContactInfo)rhs).Name.ToLower()));
        }

        public IJSonObject toJSON()
	    {
            JSonWriter wr = new JSonWriter();
            wr.WriteObjectBegin();
            wr.WriteMember("phone_no", this.PhoneNo);
            wr.WriteMember("name", this.Name);
            wr.WriteMember("id", this.Id);
            wr.WriteObjectEnd();
            JSonReader jr = new JSonReader();
            IJSonObject obj = jr.ReadAsJSonObject(wr.ToString());
            return obj;
	    }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
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
