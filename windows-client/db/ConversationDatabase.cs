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
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.ComponentModel;
using System.Collections.ObjectModel;


namespace windows_client.db
{
    /*
     DBConstants.MESSAGE +" STRING, " 
	+ DBConstants.MSG_STATUS+" INTEGER, "  this is to check if msg sent or recieved of the msg sent. 
	+ DBConstants.TIMESTAMP+ INTEGER, 
	+ DBConstants.MESSAGE_ID+ INTEGER PRIMARY KEY AUTOINCREMENT,  
	+ DBConstants.MAPPED_MSG_ID+ INTEGER,  
	+ DBConstants.CONV_ID+ INTEGER 
    */
    [Table(Name = "messages")]
    public class Messages : INotifyPropertyChanged, INotifyPropertyChanging
    {
        #region Messages Table member


        private int _messageId;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public int MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                if (_messageId != value)
                {
                    NotifyPropertyChanging("MessageId");
                    _messageId = value;
                    NotifyPropertyChanged("MessageId");
                }
            }
        }

        private string _message;

        [Column]
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (_message != value)
                {
                    NotifyPropertyChanging("Message");
                    _message = value;
                    NotifyPropertyChanged("Message");
                }
            }
        }

        private int _messageStatus;

        [Column]
        public int MessageStatus
        {
            get
            {
                return _messageStatus;
            }
            set
            {
                if (_messageStatus != value)
                {
                    NotifyPropertyChanging("MessageStatus");
                    _messageStatus = value;
                    NotifyPropertyChanged("MessageStatus");
                }
            }
        }

        private int _timestamp;

        [Column]
        public int Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (_timestamp != value)
                {
                    NotifyPropertyChanging("Timestamp");
                    _timestamp = value;
                    NotifyPropertyChanged("Timestamp");
                }
            }
        }

        private int _mappedMessageId;

        [Column]
        public int MappedMessageId
        {
            get
            {
                return _mappedMessageId;
            }
            set
            {
                if (_mappedMessageId != value)
                {
                    NotifyPropertyChanging("MappedMessageId");
                    _mappedMessageId = value;
                    NotifyPropertyChanged("MappedMessageId");
                }
            }
        }


        private int _conversationId;

        [Column]
        public int ConversationId
        {
            get
            {
                return _conversationId;
            }
            set
            {
                if (_conversationId != value)
                {
                    NotifyPropertyChanging("ConversationId");
                    _conversationId = value;
                    NotifyPropertyChanged("ConversationId");
                }
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
