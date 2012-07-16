using System.ComponentModel;
using windows_client.Model;
using WP7Contrib.Collections;
using System.Collections.Generic;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged
    {
        public List<ContactInfo> allContactsList = new List<ContactInfo>();

        private ObservableCollection<ConversationListObject> _messageListPageCollection = null;

        public ObservableCollection<ConversationListObject> MessageListPageCollection
        {
            get
            {
                return _messageListPageCollection;
            }
            set
            {
                _messageListPageCollection = value;
                NotifyPropertyChanged("MessageListPageCollection");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }
}
