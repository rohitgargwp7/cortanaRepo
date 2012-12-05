using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using windows_client.utils;
using System;
using System.Diagnostics;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.IO;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using windows_client.Misc;
using System.Text;

namespace windows_client.Model
{
    [DataContract]
    public class ConversationListObject : INotifyPropertyChanged, INotifyPropertyChanging, IComparable<ConversationListObject>, IBinarySerializable
    {
        private object readWriteLock = new object();
        #region member variables

        private string _msisdn;
        private string _contactName;
        private string _lastMessage;
        private long _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;
        private byte[] _avatar;
        private bool _isFirstMsg = false; // this is used in GC , when you want to show joined msg for SMS and DND users.
        private long _lastMsgId;
        private int _muteVal = -1; // this is used to track mute (added in version 1.5.0.0)
        private BitmapImage empImage = null;
        private bool _isFav;

        #endregion

        #region Properties

        [DataMember]
        public string ContactName
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
                    NotifyPropertyChanged("NameToShow");
                }
            }
        }

        [DataMember]
        public string LastMessage
        {
            get
            {
                return _lastMessage;
            }
            set
            {
                if (_lastMessage != value)
                {
                    NotifyPropertyChanging("LastMessage");
                    _lastMessage = value;
                    NotifyPropertyChanged("LastMessage");
                }
            }
        }

        [DataMember]
        public long TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                if (_timeStamp != value)
                {
                    NotifyPropertyChanging("TimeStamp");
                    _timeStamp = value;
                    NotifyPropertyChanged("TimeStamp");
                    NotifyPropertyChanged("FormattedTimeStamp");
                }
            }
        }

        [DataMember]
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
                    _msisdn = value.Trim();
                    NotifyPropertyChanged("Msisdn");
                }
            }
        }

        [DataMember]
        public bool IsOnhike
        {
            get
            {
                return _isOnhike;
            }
            set
            {
                if (_isOnhike != value)
                {
                    NotifyPropertyChanging("IsOnhike");
                    _isOnhike = value;
                    NotifyPropertyChanged("IsOnhike");
                }
            }
        }

        [DataMember]
        public ConvMessage.State MessageStatus
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
                    NotifyPropertyChanged("LastMessageColor");
                    NotifyPropertyChanged("SDRStatusImage");
                    NotifyPropertyChanged("SDRStatusImageVisible");
                }
            }
        }

        [DataMember]
        public long LastMsgId
        {
            get
            {
                return _lastMsgId;
            }
            set
            {
                if (_lastMsgId != value)
                {
                    _lastMsgId = value;
                }
            }
        }

        public int MuteVal
        {
            get
            {
                return _muteVal;
            }
            set
            {
                if (value != _muteVal)
                    _muteVal = value;
            }
        }

        public bool IsMute
        {
            get
            {
                if (_muteVal > -1)
                    return true;
                else
                    return false;
            }
        }

        public BitmapImage SDRStatusImage
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.SENT_CONFIRMED:
                        return UI_Utils.Instance.Sent;
                    case ConvMessage.State.SENT_DELIVERED:
                        return UI_Utils.Instance.Delivered;
                    case ConvMessage.State.SENT_DELIVERED_READ:
                        return UI_Utils.Instance.Read;
                    case ConvMessage.State.SENT_UNCONFIRMED:
                        return UI_Utils.Instance.Trying;
                    case ConvMessage.State.RECEIVED_UNREAD:
                        return UI_Utils.Instance.Unread;
                    default:
                        return null;
                }
            }
        }

        public Visibility SDRStatusImageVisible
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.SENT_CONFIRMED:
                    case ConvMessage.State.SENT_DELIVERED:
                    case ConvMessage.State.SENT_DELIVERED_READ:
                    case ConvMessage.State.SENT_UNCONFIRMED:
                    case ConvMessage.State.RECEIVED_UNREAD:
                        return Visibility.Visible;
                    default:
                        return Visibility.Collapsed;
                }
            }
        }

        public string NameToShow
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_contactName))
                    return _contactName;
                else
                    return _msisdn;
            }
        }

        public string FormattedTimeStamp
        {
            get
            {
                return TimeUtils.getTimeString(_timeStamp);
            }
        }

        public byte[] Avatar
        {
            get
            {
                return _avatar;
            }
            set
            {
                if (_avatar != value)
                {
                    _avatar = value;
                    empImage = null; // reset to null whenever avatar changes
                    NotifyPropertyChanged("Avatar");
                    NotifyPropertyChanged("AvatarImage");
                }
            }
        }

        public BitmapImage AvatarImage
        {
            get
            {
                try
                {
                    if (empImage != null) // if image is already set return that
                        return empImage;
                    else if (_avatar == null)
                    {
                        if (Utils.isGroupConversation(_msisdn))
                            return UI_Utils.Instance.DefaultGroupImage;
                        return UI_Utils.Instance.DefaultAvatarBitmapImage;
                    }
                    else
                    {
                        MemoryStream memStream = new MemoryStream(_avatar);
                        memStream.Seek(0, SeekOrigin.Begin);
                        empImage = new BitmapImage();
                        empImage.SetSource(memStream);
                        return empImage;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception in Avatar Image : {0}", e.ToString());
                    return null;
                }
            }
        }

        public string LastMessageColor
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.RECEIVED_UNREAD:
                        Color currentAccentColorHex =
                        (Color)Application.Current.Resources["PhoneAccentColor"];
                        return currentAccentColorHex.ToString();
                    default: return "gray";
                }
            }
        }

        public Visibility IsLastMessageUnread
        {
            get
            {
                if (ConvMessage.State.RECEIVED_UNREAD == _messageStatus)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        public string SdrImage
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.SENT_CONFIRMED: return "images\\ic_sent.png";
                    case ConvMessage.State.SENT_DELIVERED: return "images\\ic_delivered.png";
                    case ConvMessage.State.SENT_DELIVERED_READ: return "images\\ic_read.png";
                    default: return "";
                }
            }
        }

        public bool IsFav
        {
            get
            {
                return _isFav;
            }
            set
            {
                if (value != _isFav)
                {
                    _isFav = value;
                    NotifyPropertyChanged("IsFav");
                    NotifyPropertyChanged("FavMsg");
                }
            }
        }

        public string FavMsg
        {
            get
            {
                if (IsFav) // if already favourite
                    return "Remove from Favourites";
                else
                    return "Add to Favourites";
            }
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, long timestamp, byte[] avatar, ConvMessage.State msgStatus, long lastMsgId)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._lastMessage = lastMessage;
            this._timeStamp = timestamp;
            this._isOnhike = isOnhike;
            this._avatar = avatar;
            this._messageStatus = msgStatus;
            this._lastMsgId = lastMsgId;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, long timestamp, ConvMessage.State msgStatus, long lastMsgId)
            : this(msisdn, contactName, lastMessage, false, timestamp, null, msgStatus, lastMsgId)
        {

        }

        public ConversationListObject()
        {

        }

        public ConversationListObject(string msisdn, string contactName, bool onHike, byte[] avatar)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._isOnhike = onHike;
            this._avatar = avatar;
        }

        public int CompareTo(ConversationListObject rhs)
        {
            if (this.Equals(rhs))
            {
                return 0;
            }
            //TODO check is Messages is empty
            return TimeStamp > rhs.TimeStamp ? -1 : 1;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            ConversationListObject o = obj as ConversationListObject;

            if ((System.Object)o == null)
            {
                return false;
            }
            return (_msisdn == o.Msisdn);
        }

        #endregion

        public void Write(BinaryWriter writer)
        {
            try
            {
                if (_msisdn == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_msisdn);

                if (_contactName == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_contactName);

                if (_lastMessage == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_lastMessage);
                writer.Write(_timeStamp);
                writer.Write(_isOnhike);
                writer.Write((int)_messageStatus);
                writer.Write(_isFirstMsg);
                writer.Write(_lastMsgId);
                writer.Write(_muteVal);
            }
            catch
            {
                throw new Exception("Unable to write to a file...");
            }
        }

        public void Read(BinaryReader reader)
        {
            string current_ver = "1.5.0.0";
            App.appSettings.TryGetValue<string>("File_System_Version", out current_ver);
            int val = Utils.compareVersion(current_ver, "1.4.5.5");
            if (val != 1) // current_ver <= 1.4.5.5
            {
                ReadVer_1_4_0_0(reader);
            }
            else  //current_ver > 1.4.5.5 
            {
                ReadVer_1_5_0_0(reader);
            }           
        }

        private void ReadVer_1_4_0_0(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_msisdn == "*@N@*")
                    _msisdn = null;
                count = reader.ReadInt32();
                _contactName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_contactName == "*@N@*") // this is done so that we can specifically set null if contact name is not there
                    _contactName = null;
                count = reader.ReadInt32();
                _lastMessage = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_lastMessage == "*@N@*")
                    _lastMessage = null;
                _timeStamp = reader.ReadInt64();
                _isOnhike = reader.ReadBoolean();
                _messageStatus = (ConvMessage.State)reader.ReadInt32();
                _isFirstMsg = reader.ReadBoolean();
                _lastMsgId = reader.ReadInt64();
            }
            catch
            {
                throw new Exception("Conversation Object corrupt");
            }
        }

        private void ReadVer_1_5_0_0(BinaryReader reader)
        {
            try
            {
                int count = reader.ReadInt32();
                _msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_msisdn == "*@N@*")
                    _msisdn = null;
                count = reader.ReadInt32();
                _contactName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_contactName == "*@N@*") // this is done so that we can specifically set null if contact name is not there
                    _contactName = null;
                count = reader.ReadInt32();
                _lastMessage = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                if (_lastMessage == "*@N@*")
                    _lastMessage = null;
                _timeStamp = reader.ReadInt64();
                _isOnhike = reader.ReadBoolean();
                _messageStatus = (ConvMessage.State)reader.ReadInt32();
                _isFirstMsg = reader.ReadBoolean();
                _lastMsgId = reader.ReadInt64();
                _muteVal = reader.ReadInt32();
            }
            catch
            {
                throw new Exception("Conversation Object corrupt");
            }
        }

        public void ReadVer_1_0_0_0(BinaryReader reader)
        {
            _msisdn = reader.ReadString();
            _contactName = reader.ReadString();
            if (_contactName == "*@N@*") // this is done so that we can specifically set null if contact name is not there
                _contactName = null;
            _lastMessage = reader.ReadString();
            _timeStamp = reader.ReadInt64();
            _isOnhike = reader.ReadBoolean();
            _messageStatus = (ConvMessage.State)reader.ReadInt32();
            _isFirstMsg = reader.ReadBoolean();
            _lastMsgId = reader.ReadInt64();
        }

        #region FAVOURITES AND PENDING SECTION

        public void WriteFavOrPending(BinaryWriter writer)
        {
            try
            {
                if (_msisdn == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_msisdn);

                if (_contactName == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_contactName);
                writer.Write(_isOnhike);
            }
            catch
            {
                throw new Exception("Unable to write to a file...");
            }
        }

        public void ReadFavOrPending(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            _msisdn = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (_msisdn == "*@N@*")
                _msisdn = null;
            count = reader.ReadInt32();
            _contactName = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
            if (_contactName == "*@N@*")
                _contactName = null;
            _isOnhike = reader.ReadBoolean();
        }
        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify Silverlight that a property has changed.
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception)
                    {
                    }
                });
            }
        }
        #endregion

        #region INotifyPropertyChanging Members

        public event PropertyChangingEventHandler PropertyChanging;
        private string ms;
        private string p1;
        private bool p2;
        private byte[] _av;

        // Used to notify that a property is about to change
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                try
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
                catch (Exception)
                { }
            }
        }
        #endregion
    }
}
