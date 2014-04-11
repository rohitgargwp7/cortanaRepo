﻿using System.Windows;
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
using windows_client.Languages;
using windows_client.Controls;
using windows_client.DbUtils;

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
        private int _unreadCounter = 0;
        private long _timeStamp;
        private bool _isOnhike;
        private ConvMessage.State _messageStatus;
        private byte[] _avatar;
        private bool _isFirstMsg = false; // not used anywhere
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
                return string.IsNullOrEmpty(_typingNotificationText) ? _lastMessage : _typingNotificationText;
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

        /// <summary>
        /// use where we dont need to show typing notification
        /// </summary>
        public string ToastText
        {
            get
            {
                return _lastMessage;
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
                    NotifyPropertyChanged("TimeStampVisibility");
                    NotifyPropertyChanged("MuteIconTimeStampVisibility");
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
                    NotifyPropertyChanged("ShowOnHikeImage");
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
                    NotifyPropertyChanged("UnreadCircleVisibility");
                }

                if (_messageStatus == ConvMessage.State.RECEIVED_UNREAD)
                    UnreadCounter++;
                else
                    UnreadCounter = 0;
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

        public Visibility ShowOnHikeImage
        {
            get
            {
                if (_isOnhike)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        [DataMember]
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

                NotifyPropertyChanged("MuteIconVisibility");
                NotifyPropertyChanged("MuteIconTimeStampVisibility");
            }
        }

        public Visibility MuteIconVisibility
        {
            get
            {
                if (_muteVal > -1)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public BitmapImage MuteIcon
        {
            get
            {
                return UI_Utils.Instance.MuteIconForConversationView;
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

        public Visibility AddToFriendVisibility
        {
            get
            {
                if (App.ViewModel.BlockedHashset.Contains(Msisdn) || Utils.isGroupConversation(Msisdn) || Utils.IsHikeBotMsg(Msisdn))
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public BitmapImage SDRStatusImage
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.FORCE_SMS_SENT_CONFIRMED:
                    case ConvMessage.State.SENT_CONFIRMED:
                        return UI_Utils.Instance.Sent_Grey;
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED:
                    case ConvMessage.State.SENT_DELIVERED:
                        return UI_Utils.Instance.Delivered_Grey;
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ:
                    case ConvMessage.State.SENT_DELIVERED_READ:
                        return UI_Utils.Instance.Read_Grey;
                    case ConvMessage.State.SENT_UNCONFIRMED:
                        return UI_Utils.Instance.Trying_Grey;
                    case ConvMessage.State.SENT_FAILED:
                        return UI_Utils.Instance.Trying_Grey;
                    default:
                        return null;
                }
            }
        }

        public Visibility SDRStatusImageVisible
        {
            get
            {
                if (string.IsNullOrEmpty(_typingNotificationText))
                    switch (_messageStatus)
                    {
                        case ConvMessage.State.FORCE_SMS_SENT_CONFIRMED:
                        case ConvMessage.State.FORCE_SMS_SENT_DELIVERED:
                        case ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ:
                        case ConvMessage.State.SENT_CONFIRMED:
                        case ConvMessage.State.SENT_DELIVERED:
                        case ConvMessage.State.SENT_DELIVERED_READ:
                        case ConvMessage.State.SENT_UNCONFIRMED:
                        case ConvMessage.State.SENT_FAILED:
                            return Visibility.Visible;
                        default:
                            return Visibility.Collapsed;
                    }
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility UnreadCircleVisibility
        {
            get
            {
                if (_messageStatus == ConvMessage.State.RECEIVED_UNREAD && string.IsNullOrEmpty(_typingNotificationText) && !IsLastMsgStatusUpdate)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public double UnreadCounterWidth
        {
            get
            {
                return getUnreadCounterWidth();
            }
        }

        public int UnreadCounter
        {
            get
            {
                return _unreadCounter;
            }
            set
            {
                if (_unreadCounter != value)
                {
                    _unreadCounter = value;

                    NotifyPropertyChanged("UnreadCounter");
                    NotifyPropertyChanged("UnreadCounterWidth");
                }
            }
        }

        public bool IsLastMsgStatusUpdate
        {
            get
            {
                return _isFirstMsg;
            }
            set
            {
                if (value != _isFirstMsg)
                {
                    _isFirstMsg = value;
                    NotifyPropertyChanged("UnreadCircleVisibility");
                }
            }
        }

        public string NameToShow
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_contactName))
                    return _contactName;
                else if (IsGroupChat)
                {
                    //occurence of this is negligible so doing this here
                    GroupManager.Instance.LoadGroupParticipants(_msisdn);
                    _contactName = GroupManager.Instance.defaultGroupName(_msisdn);
                    ConversationTableUtils.updateGroupName(_msisdn, _contactName);
                    return _contactName;
                }
                else
                    return _msisdn;
            }
        }

        public Visibility TimeStampVisibility
        {
            get
            {
                return String.IsNullOrEmpty(LastMessage) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility MuteIconTimeStampVisibility
        {
            get
            {
                return TimeStampVisibility == Visibility.Visible || MuteIconVisibility == Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public string FormattedTimeStamp
        {
            get
            {
                return TimeUtils.getTimeString(TimeStamp);
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
                    NotifyPropertyChanged("ConvImage");
                }
            }
        }

        bool _isSelected = false;
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    NotifyPropertyChanged("ConvImage");
                }
            }
        }

        public BitmapImage ConvImage
        {
            get
            {
                if (_isSelected)
                    return UI_Utils.Instance.ProfileTickImage;
                else
                    return AvatarImage;
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
                            return UI_Utils.Instance.getDefaultGroupAvatar(Msisdn);
                        return UI_Utils.Instance.getDefaultAvatar(Msisdn);
                    }
                    else
                    {
                        empImage = UI_Utils.Instance.createImageFromBytes(_avatar);
                        UI_Utils.Instance.BitmapImageCache[_msisdn] = empImage; // update cache
                        return empImage;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConversationListPage :: AvatarImage : AvatarImage, Exception : " + ex.StackTrace);
                    return null;
                }
            }
        }

        public string LastMessageColor
        {
            get
            {
                if (!string.IsNullOrEmpty(_typingNotificationText) || _messageStatus == ConvMessage.State.RECEIVED_UNREAD)
                {
                    Color currentAccentColorHex =
                       (Color)Application.Current.Resources["PhoneAccentColor"];
                    return currentAccentColorHex.ToString();
                }
                else
                    return "gray";
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
                    return AppResources.RemFromFav_Txt;
                else
                    return AppResources.Add_To_Fav_Txt;
            }
        }

        public string ProfileMsg
        {
            get
            {
                if (IsGroupChat)
                    return AppResources.GroupInfo_Txt;
                else
                    return AppResources.User_Info_Txt;
            }
        }

        public Visibility ViewProfileVisibility
        {
            get
            {
                if (Utils.IsHikeBotMsg(_msisdn) || (IsGroupChat && !IsGroupAlive))
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public bool IsGroupChat
        {
            get
            {
                return Utils.isGroupConversation(_msisdn);
            }
        }
        public bool? _isGroupAlive;
        public bool IsGroupAlive
        {
            get
            {
                if (IsGroupChat && _isGroupAlive == null)
                    _isGroupAlive = GroupTableUtils.IsGroupAlive(_msisdn);

                return (bool)_isGroupAlive;
            }
            set
            {
                if (value != _isGroupAlive)
                    _isGroupAlive = value;
            }
        }

        private long lastTypingNotificationShownTime;
        private string _typingNotificationText;

        public string TypingNotificationText
        {
            get
            {
                return _typingNotificationText;
            }
            set
            {
                if (value != _typingNotificationText)
                {
                    _typingNotificationText = value;
                    if (!string.IsNullOrEmpty(_typingNotificationText))
                        lastTypingNotificationShownTime = TimeUtils.getCurrentTimeStamp();
                    NotifyPropertyChanged("LastMessage");
                    NotifyPropertyChanged("UnreadCircleVisibility");
                    NotifyPropertyChanged("SDRStatusImageVisible");
                    NotifyPropertyChanged("LastMessageColor");
                }
            }
        }

        public void AutoHidetypingNotification()
        {
            long timeElapsed = TimeUtils.getCurrentTimeStamp() - lastTypingNotificationShownTime;
            if (timeElapsed >= HikeConstants.TYPING_NOTIFICATION_AUTOHIDE)
            {
                TypingNotificationText = null;
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

            if (msgStatus == ConvMessage.State.RECEIVED_UNREAD)
                UnreadCounter++;
            else
                UnreadCounter = 0;
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

        double getUnreadCounterWidth()
        {
            double defaultWidth = 17;
            var num = UnreadCounter;

            while (num != 0)
            {
                num /= 10;
                defaultWidth += 8;
            }

            return defaultWidth;
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
                writer.Write(_unreadCounter);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: Write : Unable To write, Exception : " + ex.StackTrace);
                throw new Exception("Unable to write to a file...");
            }
        }

        public void Read(BinaryReader reader)
        {
            if (Utils.compareVersion(App.CURRENT_VERSION, "1.5.0.0") != 1) // current_ver <= 1.5.0.0
            {
                ReadVer_1_4_0_0(reader);
            }
            else  //current_ver >= 1.5.0.0
            {
                ReadVer_Latest(reader);
            }
        }

        public void ReadVer_1_4_0_0(BinaryReader reader)
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
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: ReadVer_1_4_0_0 : Unable To write, Exception : " + ex.StackTrace);
                throw new Exception("Conversation Object corrupt");
            }
        }

        public void ReadVer_Latest(BinaryReader reader)
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

                try
                {
                    _unreadCounter = reader.ReadInt32();
                }
                catch
                {
                    if (_messageStatus == ConvMessage.State.RECEIVED_UNREAD)
                        _unreadCounter = 1;
                    else
                        _unreadCounter = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: ReadVer_Latest : Unable To write, Exception : " + ex.StackTrace);
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
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: WriteFavOrPending : WriteFavOrPending, Exception : " + ex.StackTrace);
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
                        if (propertyName != null)
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConversationListPage :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
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
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (propertyName != null)
                            PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                    }
                    catch (Exception)
                    { }
                });
            }
        }
        #endregion


    }
}
