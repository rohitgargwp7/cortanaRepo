using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace windows_client.Model
{
    public class ContactGroup<T> : ObservableCollection<T>, INotifyPropertyChanged
    {
        public ContactGroup(string name, string name2)
        {
            Title = name;
            _title2 = name2;
            this.CollectionChanged += Group_CollectionChanged;
        }

        void Group_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Title");
            NotifyPropertyChanged("IsNonEmpty");
        }

        string _title2;
        string _title;
        public string Title
        {
            get
            {
                return String.IsNullOrEmpty(_title) ? String.Empty : Items.Count == 1 ? _title2 : String.Format(_title, Items.Count);
            }
            set
            {
                if (value != _title)
                    _title = value;
            }
        }

        public bool IsNonEmpty
        {
            get
            {
                return Items != null && Items.Count > 0;
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ContactGroup :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }

        #endregion
    }
}
