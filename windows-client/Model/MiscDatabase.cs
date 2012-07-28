using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.ComponentModel;
using Microsoft.Phone.Data.Linq.Mapping;


namespace windows_client.Model
{

    [Table(Name = "blocked")]
    public class Blocked : INotifyPropertyChanged, INotifyPropertyChanging
    {

        private string _msisdn;

        [Column(IsPrimaryKey = true, CanBeNull = false)]
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

        public Blocked(string msisdn)
        {
            this.Msisdn = msisdn;
        }

        public Blocked()
        { 
        }

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
