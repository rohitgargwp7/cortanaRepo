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

namespace windows_client.Model
{
    public class ChatThreadPage : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private string _message;
        private string _alignment;
        private string _msgId;

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (value != _message)
                {
                    NotifyPropertyChanging("Message");
                    _message = value;
                    NotifyPropertyChanged("Message");
                }
            }
        }

        public string Alignment
        {
            get
            {
                return _alignment;
            }
            set
            {
                if (value != _alignment)
                {
                    _alignment = value;
                }
            }
        }

        public string MsgId
        {
            get
            {
                return _msgId;
            }
            set
            {
                if (value != _msgId)
                {
                    NotifyPropertyChanging("MsgId");
                    _msgId = value;
                    NotifyPropertyChanged("MsgId");
                }
            }
        }
        public ChatThreadPage(string msg)
        {
            _message = msg;
            _alignment = "Left";
        }

        public ChatThreadPage(string msgId,string msg,string alignment)
        {
            _message = msg;
            _alignment = alignment;
            _msgId = msgId;
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
