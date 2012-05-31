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
using System.Collections.Generic;

using windows_client.utils;
namespace windows_client.Model
{
    [Table(Name="conversations")]
    public class Conversation : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<Conversation>
    {

        #region conversations members
        private String _msisdn;

        [Column(IsPrimaryKey = true)]
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

        //private long _convId;
        //[Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        //public long ConvId
        //{
        //    get
        //    {
        //        return _convId;
        //    }
        //    set
        //    {
        //        if (_convId != value)
        //        {
        //            NotifyPropertyChanging("ConvId");
        //            _convId = value;
        //            NotifyPropertyChanged("ConvId");
        //        }
        //    }
        //}


        private String _contactId;
        [Column]
        public String ContactId
        {
            get
            {
                return _contactId;
            }
            set
            {
                if (_contactId != value)
                {
                    NotifyPropertyChanging("ContactId");
                    _contactId = value;
                    NotifyPropertyChanged("ContactId");
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

        private String _metadata;
        [Column]
        public String Metadata
        {
            get
            {
                return _metadata;
            }
            set
            {
                if (_metadata != value)
                {
                    NotifyPropertyChanging("Metadata");
                    _metadata = value;
                    NotifyPropertyChanged("Metadata");
                }
            }
        }
        
        private List<ConvMessage> _messages;
        public List<ConvMessage> Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                if (_messages != value)
                {
                    NotifyPropertyChanging("Messages");
                    _messages = value;
                    NotifyPropertyChanged("Messages");
                }
            }

        }

        private String _contactName;
        public String ContactName
        {
            get
            {
                return _contactName;
            }
            set
            {
                if (_contactName != value)
                {
                    NotifyPropertyChanging("ContactName");
                    _contactName = value;
                    NotifyPropertyChanged("ContactName");
                }
            }
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            Conversation other = (Conversation)obj;
            if (ContactId == null)
            {
                if (other.ContactId != null)
                    return false;
            }
            else if (ContactId.CompareTo(other.ContactId)!=0)
                return false;
            if (ContactName == null)
            {
                if (other.ContactName != null)
                    return false;
            }
            else if (ContactName.CompareTo(other.ContactName)!=0)
                return false;
            //if (ConvId != other.ConvId)
            //    return false;
            if (Messages == null)
            {
                if (other.Messages != null)
                    return false;
            }
            else if (!Messages.Equals(other.Messages))
                return false;
            if (Msisdn == null)
            {
                if (other.Msisdn != null)
                    return false;
            }
            else if (Msisdn.CompareTo(other.Msisdn)!=0)
                return false;
            if (OnHike != other.OnHike)
                return false;
            return true;
        }




        public override int  GetHashCode()
        {
		    const int prime = 31;
		    int result = 1;
		    result = prime * result + ((ContactId == null) ? 0 : ContactId.GetHashCode());
		    result = prime * result + ((ContactName == null) ? 0 : ContactName.GetHashCode());
//		    result = prime * result + (int) (ConvId ^ Convert.ToUInt32(ConvId) >> 32);
		    result = prime * result + ((Messages == null) ? 0 : Messages.GetHashCode());
		    result = prime * result + ((Msisdn == null) ? 0 : Msisdn.GetHashCode());
		    result = prime * result + (OnHike ? 1231 : 1237);
		    return result;
	    }

        public int CompareTo(Conversation rhs)
        {
            if (this.Equals(rhs))
            {
                return 0;
            }
            //TODO check is Messages is empty

            long ts = Messages.Count==0 ? 0 : Messages[(Messages.Count - 1)].Timestamp;
            if (rhs == null)
            {
                return 1;
            }

            long rhsTs = rhs.Messages.Count==0 ? 0 : rhs.Messages[rhs.Messages.Count - 1].Timestamp;

            if (rhsTs != ts)
            {
                return (ts < rhsTs) ? -1 : 1;
            }

            int ret = Msisdn.CompareTo(rhs.Msisdn);
            if (ret != 0)
            {
                return ret;
            }

            //if (ConvId != rhs.ConvId)
            //{
            //    return (ConvId < rhs.ConvId) ? -1 : 1;
            //}

            String cId = (ContactId != null) ? ContactId : "";
            return cId.CompareTo(rhs.ContactId);
        }

        public Conversation()
        {
            this.Messages = new List<ConvMessage>();
        }

//        public Conversation(String msisdn, long convId, String contactId, String contactName, bool onhike)
        public Conversation(String msisdn, String contactId, String contactName, bool onhike)
        {
            this.Msisdn = msisdn;
            //this.ConvId = convId;
            this.ContactId = contactId;
            this.ContactName = contactName;
            this.OnHike = onhike;
            this._messages = new List<ConvMessage>();
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

        public String getLabel()
        {
            return ContactName.CompareTo("") == 0 ? ContactName : Msisdn;
        }

        #endregion

    }
}
