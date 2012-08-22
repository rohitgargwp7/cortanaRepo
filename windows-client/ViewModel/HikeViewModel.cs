using System.ComponentModel;
using windows_client.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace windows_client.ViewModel
{
    public class HikeViewModel : INotifyPropertyChanged
    {
        private List<string> _convMsisdnsToUpdate = new List<string>();

        public List<string> ConvMsisdnsToUpdate
        {
            get
            {
                return _convMsisdnsToUpdate;
            }
            set
            {
                if (value != _convMsisdnsToUpdate)
                    _convMsisdnsToUpdate = value;
            }
        }

        private ObservableCollection<ConversationListObject> _messageListPageCollection = new ObservableCollection<ConversationListObject>();

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
