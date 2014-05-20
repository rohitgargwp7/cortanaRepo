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
            _titleForMultipleContacts = name;
            _titleFor1Contact = name2;
            this.CollectionChanged += Group_CollectionChanged;
        }

        void Group_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("Title");
            NotifyPropertyChanged("IsNonEmpty");
        }

        string _titleFor1Contact;
        string _titleForMultipleContacts;
        public string Title
        {
            get
            {
                return Items.Count == 1 ? (String.IsNullOrEmpty(_titleFor1Contact) ? string.Empty : _titleFor1Contact)
                    : (String.IsNullOrEmpty(_titleForMultipleContacts) ? string.Empty : String.Format(_titleForMultipleContacts, Items.Count));
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
