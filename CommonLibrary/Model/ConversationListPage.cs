using System.Windows;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using CommonLibrary.utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;
using System.Text;
using CommonLibrary.Languages;
using CommonLibrary.DbUtils;
using CommonLibrary.Lib;
using CommonLibrary.Misc;
using CommonLibrary.Utils;

namespace CommonLibrary.Model
{
    [DataContract]
    public class ConversationListObject : IComparable<ConversationListObject>, IBinarySerializable
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
        private bool _isFirstMsg = false; // not used anywhere
        private long _lastMsgId;
        private int _muteVal = -1; // this is used to track mute (added in version 1.5.0.0)
        private bool _isFav;
        private string _draftMessage;
        #endregion

        #region Properties

        public string ContactName
        {
            get
            {
                return _contactName;
            }
            set
            {
                if (_contactName != value)
                    _contactName = value;
            }
        }

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
                    _lastMessage = value;
                    _toastText = value;
                }
            }
        }

        string _toastText;
        /// <summary>
        /// use where we dont need to show typing notification
        /// </summary>
        public string ToastText
        {
            get
            {
                return _toastText;
            }
            set
            {
                if (value != _toastText)
                    _toastText = value;
            }
        }

        public long TimeStamp
        {
            get
            {
                return _timeStamp;
            }
            set
            {
                if (_timeStamp != value)
                    _timeStamp = value;
            }
        }

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (_msisdn != value)
                    _msisdn = value.Trim();
            }
        }

        public bool IsOnhike
        {
            get
            {
                return _isOnhike;
            }
            set
            {
                if (_isOnhike != value)
                    _isOnhike = value;
            }
        }

        public ConvMessage.State MessageStatus
        {
            get
            {
                return _messageStatus;
            }
            set
            {
                if (_messageStatus != value)
                    _messageStatus = value;

                if (_messageStatus == ConvMessage.State.RECEIVED_UNREAD)
                    UnreadCounter++;
                else
                    UnreadCounter = 0;
            }
        }

        public long LastMsgId
        {
            get
            {
                return _lastMsgId;
            }
            set
            {
                if (_lastMsgId != value)
                    _lastMsgId = value;
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

        public string NameToShow
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_contactName) || IsGroupChat)
                    return _contactName;
                return _msisdn;
            }
        }

        public bool IsGroupChat
        {
            get
            {
                return Utility.IsGroupConversation(_msisdn);
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
                    _unreadCounter = value;
            }
        }

        public string UnreadCounterString
        {
            get
            {
                return _unreadCounter <= 9 ? _unreadCounter.ToString() : "9+";
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
                    _isFirstMsg = value;
            }
        }

        public string _metadata { get; set; } //stores the latest pin info

        JObject metadata = null;
        public JObject MetaData
        {
            get
            {
                try
                {
                    if (metadata == null)
                        metadata = JObject.Parse(_metadata);

                    return metadata;
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                metadata = value;
                _metadata = (value == null) ? null : value.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        bool _isHidden = false;
        public bool IsHidden
        {
            get
            {
                return _isHidden;
            }
            set
            {
                if (_isHidden != value)
                    _isHidden = value;
            }
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, bool isOnhike, long timestamp, ConvMessage.State msgStatus, long lastMsgId)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this.LastMessage = lastMessage;
            this._timeStamp = timestamp;
            this._isOnhike = isOnhike;
            this._messageStatus = msgStatus;
            this._lastMsgId = lastMsgId;

            if (msgStatus == ConvMessage.State.RECEIVED_UNREAD)
                UnreadCounter++;
            else
                UnreadCounter = 0;
        }

        public ConversationListObject(string msisdn, string contactName, string lastMessage, long timestamp, ConvMessage.State msgStatus, long lastMsgId)
            : this(msisdn, contactName, lastMessage, false, timestamp, msgStatus, lastMsgId)
        {

        }

        public ConversationListObject()
        {

        }

        public ConversationListObject(string msisdn, string contactName, bool onHike)
        {
            this._msisdn = msisdn;
            this._contactName = contactName;
            this._isOnhike = onHike;
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

                if (_draftMessage == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_draftMessage);

                writer.Write(_isHidden);

                if (_metadata == null)
                    writer.WriteStringBytes("*@N@*");
                else
                    writer.WriteStringBytes(_metadata);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: Write : Unable To write, Exception : " + ex.StackTrace);
                throw new Exception("Unable to write to a file...");
            }
        }

        public void Read(BinaryReader reader)
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
                    _unreadCounter = _messageStatus == ConvMessage.State.RECEIVED_UNREAD ? 1 : 0;
                }

                try
                {
                    count = reader.ReadInt32();
                    _draftMessage = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (_draftMessage == "*@N@*")
                        _draftMessage = string.Empty;//so that on comparing with unsent empty text it returns true 
                }
                catch
                {
                    _draftMessage = string.Empty;
                }

                try
                {
                    _isHidden = reader.ReadBoolean();
                }
                catch
                {
                    _isHidden = false;
                }

                try
                {
                    count = reader.ReadInt32();
                    _metadata = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                    if (_metadata == "*@N@*")
                        _metadata = null;
                }
                catch
                {
                    _metadata = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConversationListPage :: ReadVer_Latest : Unable To write, Exception : " + ex.StackTrace);
                throw new Exception("Conversation Object corrupt");
            }
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
    }
}
