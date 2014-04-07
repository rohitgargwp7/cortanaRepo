using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using windows_client.utils;

namespace windows_client.Model
{
    public class BaseStatusUpdate : INotifyPropertyChanged 
    {
        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (value != _userName)
                {
                    _userName = value;
                    NotifyPropertyChanged("UserName");
                }
            }
        }

        private BitmapImage _userImage;
        public virtual BitmapImage UserImage
        {
            get
            {
                return _userImage;
            }
            set
            {
                if (value != _userImage)
                {
                    _userImage = value;
                    NotifyPropertyChanged("UserImage");
                }
            }
        }

        private string _msisdn;
        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (value != _msisdn)
                {
                    _msisdn = value;
                    NotifyPropertyChanged("Msisdn");
                }
            }
        }

        private string _serverId;
        public string ServerId
        {
            get
            {
                return _serverId;
            }
        }

        private bool _isUnread;
        public virtual bool IsUnread
        {
            get
            {
                return _isUnread;
            }
            set
            {
                if (value != _isUnread)
                    _isUnread = value;
            }
        }

        private bool _isShowOnTimeline;
        public bool IsShowOnTimeline
        {
            get
            {
                return _isShowOnTimeline;
            }
            set
            {
                if (value != _isShowOnTimeline)
                    _isShowOnTimeline = value;
            }
        }

        public Visibility DeleteOptionVisibility
        {
            get
            {
                return App.MSISDN == Msisdn ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                if (value != _text)
                {
                    _text = value;
                    NotifyPropertyChanged("Text");
                }
            }
        }

        private long _timestamp;
        public long Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (value != _timestamp)
                {
                    _timestamp = value;
                    NotifyPropertyChanged("TimestampString");
                }
            }
        }

        public string TimestampString
        {
            get
            {
                return TimeUtils.getRelativeTime(Timestamp);
            }
        }

        public BaseStatusUpdate(string userName, BitmapImage userImage, string msisdn, string serverId)
            : this(userName, userImage, msisdn, serverId, true)
        {
        }

        public BaseStatusUpdate(string userName, BitmapImage userImage, string msisdn, string serverId, bool isShowOnTimeline)
        {
            UserName = userName;
            UserImage = userImage;
            Msisdn = msisdn;
            _serverId = serverId;
            IsShowOnTimeline = isShowOnTimeline;
        }

        public BaseStatusUpdate(ConversationListObject c, string serverId)
            : this(c.NameToShow, c.AvatarImage, c.Msisdn, serverId)
        {
        }

        public BaseStatusUpdate()
        {
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            BaseStatusUpdate otherSb = (BaseStatusUpdate)obj;
            if (this._serverId == null || otherSb._serverId == null)
                return false;
            return this._serverId.Equals(otherSb._serverId);
        }

        public void UpdateImage()
        {
            UserImage = UI_Utils.Instance.GetBitmapImage(Msisdn);
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (propertyName != null)
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StatusUpdate :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                    }
                });
            }
        }

        #endregion
    }
}
