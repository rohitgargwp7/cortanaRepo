using System;
using System.Windows;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Text;
using windows_client.DbUtils;
using System.Collections.Generic;
using Microsoft.Phone.Shell;
using windows_client.Misc;
using windows_client.Languages;

namespace windows_client.Model
{
    [Table(Name = "messages")]
    [Index(Columns = "Msisdn,Timestamp ASC", IsUnique = false, Name = "Msg_Idx")]
    public class ConvMessage : INotifyPropertyChanged, INotifyPropertyChanging
    {

        private long _messageId; // this corresponds to msgID stored in sender's DB
        private string _msisdn;
        private string _message;
        private State _messageStatus;
        private long _timestamp;
        private long _mappedMessageId; // this corresponds to msgID stored in receiver's DB
        private bool _isInvite;
        private bool _isSent;
        private bool _isSms;
        private string _groupParticipant;
        private string metadataJsonString;
        private ParticipantInfoState participantInfoState;
        private Attachment _fileAttachment = null;

        // private bool _hasFileAttachment = false;
        private bool _hasAttachment = false;

        /* Adding entries to the beginning of this list is not backwards compatible */
        public enum State
        {
            SENT_UNCONFIRMED = 0,  /* message sent to server */
            SENT_FAILED, /* message could not be sent, manually retry */
            SENT_CONFIRMED, /* message received by server */
            SENT_DELIVERED, /* message delivered to client device */
            SENT_DELIVERED_READ, /* message viewed by recipient */
            RECEIVED_UNREAD, /* message received, but currently unread */
            RECEIVED_READ, /* message received an read */
            UNKNOWN
        }

        public enum ChatBubbleType
        {
            RECEIVED = 0,
            HIKE_SENT,
            SMS_SENT
        }

        public enum ParticipantInfoState
        {
            NO_INFO, // This is a normal message
            PARTICIPANT_LEFT, // The participant has left
            PARTICIPANT_JOINED, // The participant has joined
            MEMBERS_JOINED, // this is used in new scenario
            GROUP_END, // Group chat has ended
            GROUP_NAME_CHANGE,
            USER_OPT_IN,
            USER_JOINED,
            HIKE_USER,
            SMS_USER,
            DND_USER,
            GROUP_JOINED_OR_WAITING,
            CREDITS_GAINED,
            INTERNATIONAL_USER,
            INTERNATIONAL_GROUP_USER,
            STATUS_UPDATE
        }

        public static ParticipantInfoState fromJSON(JObject obj)
        {
            if (obj == null)
                return ParticipantInfoState.NO_INFO;
            JToken typeToken = null;
            string type = null;
            if (obj.TryGetValue(HikeConstants.TYPE, out typeToken))
                type = typeToken.ToString();
            else
                type = null;

            if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type)
                return ParticipantInfoState.PARTICIPANT_JOINED;

            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN_NEW == type)
                return ParticipantInfoState.MEMBERS_JOINED;

            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE == type)
            {
                JToken jt = null;
                if (obj.TryGetValue("st", out jt))
                    return ParticipantInfoState.INTERNATIONAL_GROUP_USER;
                return ParticipantInfoState.PARTICIPANT_LEFT;
            }
            else if (HikeConstants.MqttMessageTypes.STATUS_UPDATE == type)
            {
                return ParticipantInfoState.STATUS_UPDATE;
            }
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_END == type)
            {
                return ParticipantInfoState.GROUP_END;
            }
            else if (HikeConstants.MqttMessageTypes.GROUP_USER_JOINED_OR_WAITING == type)
            {
                return ParticipantInfoState.GROUP_JOINED_OR_WAITING;
            }
            else if (HikeConstants.MqttMessageTypes.USER_OPT_IN == type)
            {
                return ParticipantInfoState.USER_OPT_IN;
            }
            else if (HikeConstants.MqttMessageTypes.USER_JOIN == type)
            {
                return ParticipantInfoState.USER_JOINED;
            }
            else if (HikeConstants.MqttMessageTypes.HIKE_USER == type)
            {
                return ParticipantInfoState.HIKE_USER;
            }
            else if (HikeConstants.MqttMessageTypes.SMS_USER == type)
            {
                return ParticipantInfoState.SMS_USER;
            }
            else if ("credits_gained" == type)
            {
                return ParticipantInfoState.CREDITS_GAINED;
            }
            else if (HikeConstants.MqttMessageTypes.BLOCK_INTERNATIONAL_USER == type)
            {
                return ParticipantInfoState.INTERNATIONAL_USER;
            }
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_NAME == type)
            {
                return ParticipantInfoState.GROUP_NAME_CHANGE;
            }
            else if (HikeConstants.MqttMessageTypes.DND_USER_IN_GROUP == type)
            {
                return ParticipantInfoState.DND_USER;
            }
            else  // shows type == null
            {
                JArray dndNumbers = (JArray)obj["dndnumbers"];
                if (dndNumbers != null)
                {
                    return ParticipantInfoState.DND_USER;
                }
            }
            return ParticipantInfoState.NO_INFO;
        }


        #region Messages Table member

        [Column(IsVersion = true)]
        private Binary version;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "int Not Null IDENTITY")]
        public long MessageId
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
                }
            }
        }

        [Column]
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
                    _msisdn = value;
                }
            }
        }

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
                }
            }
        }

        [Column(IsDbGenerated = false, UpdateCheck = UpdateCheck.Never)]
        public State MessageStatus
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
                    NotifyPropertyChanged("SdrImage");
                }
            }
        }

        [Column]
        public long Timestamp
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
                }
            }
        }

        [Column]
        public long MappedMessageId
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
        [Column]
        public string GroupParticipant
        {
            get
            {
                return _groupParticipant;
            }
            set
            {
                if (_groupParticipant != value)
                {
                    NotifyPropertyChanging("GroupParticipant");
                    _groupParticipant = value;
                    NotifyPropertyChanged("GroupParticipant");
                }
            }
        }

        [Column]
        public string MetaDataString
        {
            get
            {
                return metadataJsonString;
            }
            set
            {
                metadataJsonString = value;
                if (string.IsNullOrEmpty(metadataJsonString))
                    participantInfoState = ParticipantInfoState.NO_INFO;
                else
                    participantInfoState = fromJSON(JObject.Parse(metadataJsonString));
            }
        }

        [Column]
        public bool HasAttachment
        {
            get
            {
                return _hasAttachment;
            }
            set
            {
                if (_hasAttachment != value)
                {
                    _hasAttachment = value;
                }
            }
        }

        public Attachment FileAttachment
        {
            get
            {
                return _fileAttachment;
            }
            set
            {
                if (_fileAttachment != value)
                    _fileAttachment = value;
            }
        }

        public ChatBubbleType MsgType
        {
            get
            {
                if (!IsSent)
                    return ChatBubbleType.RECEIVED;
                if (IsSms)
                    return ChatBubbleType.SMS_SENT;
                return ChatBubbleType.HIKE_SENT;
            }

        }

        public bool IsInvite
        {
            get
            {
                return _isInvite;
            }
            set
            {
                if (_isInvite != value)
                {
                    NotifyPropertyChanging("IsInvite");
                    _isInvite = value;
                    NotifyPropertyChanged("IsInvite");
                }
            }
        }

        public bool IsSent
        {
            get
            {
                return (_messageStatus == State.SENT_UNCONFIRMED ||
                        _messageStatus == State.SENT_CONFIRMED ||
                        _messageStatus == State.SENT_DELIVERED ||
                        _messageStatus == State.SENT_DELIVERED_READ ||
                        _messageStatus == State.SENT_FAILED);
            }
        }

        public bool IsSms
        {
            get
            {
                return _isSms;
            }
            set
            {
                if (value != _isSms)
                    _isSms = value;
            }
        }

        public ParticipantInfoState GrpParticipantState
        {
            get
            {
                return participantInfoState;
            }
            set
            {
                if (value != participantInfoState)
                {
                    participantInfoState = value;
                    NotifyPropertyChanged("GrpParticipantState");
                    NotifyPropertyChanged("ChatBubbleVisiblity");
                    NotifyPropertyChanged("NotificationMessageVisiblity");

                }
            }
        }

        public Visibility ChatBubbleVisiblity
        {
            get
            {

                if (participantInfoState != ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
        }

        public Visibility NotificationMessageVisiblity
        {
            get
            {

                if (participantInfoState != ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState)
            : this(message, msisdn, timestamp, msgState, -1, -1)
        {
        }

        public ConvMessage(string message, string msisdn, long timestamp, State msgState, long msgid, long mappedMsgId)
        {
            this._msisdn = msisdn;
            this._message = message;
            this._timestamp = timestamp;
            this._messageId = msgid;
            this._mappedMessageId = mappedMsgId;
            _isSent = (msgState == State.SENT_UNCONFIRMED ||
                        msgState == State.SENT_CONFIRMED ||
                        msgState == State.SENT_DELIVERED ||
                        msgState == State.SENT_DELIVERED_READ ||
                        msgState == State.SENT_FAILED);
            MessageStatus = msgState;
        }

        public JObject serialize(bool isHikeMsg)
        {
            JObject obj = new JObject();
            JObject data = new JObject();
            JObject metadata;
            JArray filesData;
            JObject singleFileInfo;
            if (isHikeMsg)
                data[HikeConstants.HIKE_MESSAGE] = _message;
            else
                data[HikeConstants.SMS_MESSAGE] = _message;
            data[HikeConstants.TIMESTAMP] = _timestamp;
            data[HikeConstants.MESSAGE_ID] = _messageId;

            if (HasAttachment)
            {
                metadata = new JObject();
                filesData = new JArray();
                if (!FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                {
                    singleFileInfo = new JObject();
                    singleFileInfo[HikeConstants.FILE_NAME] = FileAttachment.FileName;
                    singleFileInfo[HikeConstants.FILE_KEY] = FileAttachment.FileKey;
                    singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = FileAttachment.ContentType;
                    if (FileAttachment.Thumbnail != null)
                        singleFileInfo[HikeConstants.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
                    //if (FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                    //{
                    //    JObject locationInfo = JObject.Parse(this.MetaDataString);
                    //    singleFileInfo[HikeConstants.LATITUDE] = locationInfo[HikeConstants.LATITUDE];
                    //    singleFileInfo[HikeConstants.LONGITUDE] = locationInfo[HikeConstants.LONGITUDE];
                    //    singleFileInfo[HikeConstants.ZOOM_LEVEL] = locationInfo[HikeConstants.ZOOM_LEVEL];
                    //    singleFileInfo[HikeConstants.LOCATION_ADDRESS] = locationInfo[HikeConstants.LOCATION_ADDRESS];
                    //}
                }
                else
                {
                    //add thumbnail here
                    JObject uploadedJSON = JObject.Parse(this.MetaDataString);
                    singleFileInfo = uploadedJSON[HikeConstants.FILES_DATA].ToObject<JArray>()[0].ToObject<JObject>();
                    singleFileInfo[HikeConstants.FILE_KEY] = FileAttachment.FileKey;
                    singleFileInfo[HikeConstants.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
                }
                filesData.Add(singleFileInfo.ToObject<JToken>());
                metadata[HikeConstants.FILES_DATA] = filesData;
                data[HikeConstants.METADATA] = metadata;
            }
            else if (this.MetaDataString !=null && this.MetaDataString.Contains("poke"))
            {
                //metadata = new JObject();
                //metadata["poke"] = true;
                data["poke"] = true;
            }

            obj[HikeConstants.TO] = _msisdn;
            obj[HikeConstants.DATA] = data;
            obj[HikeConstants.TYPE] = _isInvite ? NetworkManager.INVITE : NetworkManager.MESSAGE;

            return obj;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            ConvMessage other = (ConvMessage)obj;

            if (IsSent != other.IsSent)
                return false;
            if (Message == null)
            {
                if (other.Message != null)
                    return false;
            }
            else if (Message.CompareTo(other.Message) != 0)
                return false;
            if (Msisdn == null)
            {
                if (other.Msisdn != null)
                    return false;
            }
            else if (Msisdn.CompareTo(other.Msisdn) != 0)
                return false;
            if (MessageStatus.Equals(other.MessageStatus))
                return false;
            if (Timestamp != other.Timestamp)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + (IsSent ? 1231 : 1237);
            result = prime * result + ((Message == null) ? 0 : Message.GetHashCode());
            result = prime * result + ((Msisdn == null) ? 0 : Msisdn.GetHashCode());
            result = prime * result + MessageStatus.GetHashCode();
            result = prime * result + (int)(Timestamp ^ (Convert.ToUInt32(Timestamp) >> 32));

            return result;
        }

        public string getTimestampFormatted()
        {
            return TimeUtils.getRelativeTime(Timestamp);
        }

        #region ChatThread Page Bindings for Converters

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

        public string Alignment
        {
            get
            {
                if (IsSent)
                    return "right";
                else
                    return "left";
            }
        }

        public string ChatBubbleDirection
        {
            get
            {
                if (IsSent)
                    return "LowerRight";
                else
                    return "UpperLeft";
            }
        }

        public string BubbleBackground
        {
            get
            {
                if (ChatBubbleType.RECEIVED == MsgType)
                {
                    return "#eeeeec";
                }
                else if (ChatBubbleType.HIKE_SENT == MsgType)
                {
                    return "#1ba1e2";
                }
                else
                {
                    return "#a3d250";
                }
            }
        }

        public string ChatBubbleMargin
        {
            get
            {
                if (IsSent)
                    return "15,0,10,10";
                else
                    return "5,0,10,10";
            }
        }

        public string SdrImageVisibility
        {
            get
            {
                if (IsSent)
                    return "Visible";
                else
                    return "Collapsed";
            }
        }

        public string ChatTimeFormat
        {
            get
            {
                return TimeUtils.getTimeString(_timestamp);
            }
        }
        #endregion

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
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
                        catch (Exception e)
                        { }
                    });
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
                try
                {
                    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }
                catch (Exception)
                { }
            }
        }
        #endregion

        public JObject serializeDeliveryReportRead()
        {
            JObject obj = new JObject();
            JArray ids = new JArray();
            try
            {
                ids.Add(Convert.ToString(_mappedMessageId));
                obj.Add(HikeConstants.DATA, ids);
                obj.Add(HikeConstants.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(HikeConstants.TO, _msisdn);
            }
            catch (Exception e)
            {

            }
            return obj;
        }

        public ConvMessage(JObject obj)
        {
            try
            {
                bool isFileTransfer;
                JObject metadataObject = null;
                JToken val = null;
                obj.TryGetValue(HikeConstants.TO, out val);

                JToken metadataToken = null;
                try
                {
                    obj[HikeConstants.DATA].ToObject<JObject>().TryGetValue(HikeConstants.METADATA, out metadataToken);
                }
                catch { }

                if (metadataToken != null)
                {
                    metadataObject = JObject.FromObject(metadataToken);
                    JToken filesToken = null;
                    isFileTransfer = metadataObject.TryGetValue("files", out filesToken);
                    if (isFileTransfer)
                    {
                        JArray files = metadataObject["files"].ToObject<JArray>();
                        JObject fileObject = files[0].ToObject<JObject>();

                        JToken fileName;
                        JToken fileKey;
                        JToken thumbnail;
                        JToken contentType;

                        fileObject.TryGetValue(HikeConstants.FILE_CONTENT_TYPE, out contentType);
                        fileObject.TryGetValue(HikeConstants.FILE_NAME, out fileName);
                        fileObject.TryGetValue(HikeConstants.FILE_KEY, out fileKey);
                        fileObject.TryGetValue(HikeConstants.FILE_THUMBNAIL, out thumbnail);
                        this.HasAttachment = true;

                        byte[] base64Decoded = null;
                        if (thumbnail != null)
                            base64Decoded = System.Convert.FromBase64String(thumbnail.ToString());
                        this.FileAttachment = new Attachment(fileName==null?"":fileName.ToString(), fileKey.ToString(), base64Decoded,
                           contentType.ToString(), Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                        if (contentType.ToString().Contains(HikeConstants.LOCATION))
                        {
                            JObject locationFile = new JObject();
                            locationFile[HikeConstants.LATITUDE] = fileObject[HikeConstants.LATITUDE];
                            locationFile[HikeConstants.LONGITUDE] = fileObject[HikeConstants.LONGITUDE];
                            locationFile[HikeConstants.ZOOM_LEVEL] = fileObject[HikeConstants.ZOOM_LEVEL];
                            locationFile[HikeConstants.LOCATION_ADDRESS] = fileObject[HikeConstants.LOCATION_ADDRESS];
                            this.MetaDataString = locationFile.ToString();

                        }
                    }
                    else
                    {
                        metadataJsonString = metadataObject.ToString(Newtonsoft.Json.Formatting.None);
                    }

                }
                participantInfoState = fromJSON(metadataObject);
                if (val != null) // represents group message
                {
                    _msisdn = val.ToString();
                    _groupParticipant = (string)obj[HikeConstants.FROM];
                }
                else
                {
                    _msisdn = (string)obj[HikeConstants.FROM]; /*represents msg is coming from another client or system msg*/
                    _groupParticipant = null;
                }

                JObject data = (JObject)obj[HikeConstants.DATA];
                JToken msg;

                if (data.TryGetValue(HikeConstants.SMS_MESSAGE, out msg)) // if sms 
                {
                    _message = msg.ToString();
                    _isSms = true;
                }
                else       // if not sms
                {
                    _isSms = false;
                    if (this.HasAttachment)
                    {
                        string messageText = "";
                        if (this.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                            messageText = AppResources.Image_Txt;
                        else if (this.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                            messageText = AppResources.Audio_Txt;
                        else if (this.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                            messageText = AppResources.Video_Txt;
                        else if (this.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            messageText = AppResources.Location_Txt;
                        else if (this.FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                            messageText = AppResources.ContactTransfer_Text;
                        this._message = messageText;
                    }
                    else
                    {
                        if (participantInfoState == ParticipantInfoState.INTERNATIONAL_USER)
                            _message = AppResources.SMS_Works_Only_In_India_Txt;
                        else
                            _message = (string)data[HikeConstants.HIKE_MESSAGE];
                    }

                }
                if (data.TryGetValue("poke", out msg)) // if sms 
                {
                    metadataJsonString = "{poke: true}";
                }

                //JToken ts = null;
                //if (data.TryGetValue(HikeConstants.TIMESTAMP, out ts))
                _timestamp = TimeUtils.getCurrentTimeStamp();

                /* prevent us from receiving a message from the future */

                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                this.Timestamp = (this.Timestamp > now) ? now : this.Timestamp;

                /* if we're deserialized an object from json, it's always unread */
                this.MessageStatus = State.RECEIVED_UNREAD;
                this._messageId = -1;
                string mappedMsgID = (string)data[HikeConstants.MESSAGE_ID];
                this.MappedMessageId = System.Int64.Parse(mappedMsgID);
            }
            catch (Exception e)
            {
                throw new Exception("Error in parsing json");
            }
        }

        public ConvMessage()
        {
        }

        public ConvMessage(JObject obj, bool isSelfGenerated, bool addedLater)
        {
            // If the message is a group message we get a TO field consisting of the Group ID
            string toVal = obj[HikeConstants.TO].ToString();
            this._msisdn = (toVal != null) ? (string)obj[HikeConstants.TO] : (string)obj[HikeConstants.FROM]; /*represents msg is coming from another client*/
            this._groupParticipant = (toVal != null) ? (string)obj[HikeConstants.FROM] : null;
            this.participantInfoState = fromJSON(obj);
            this.metadataJsonString = obj.ToString(Newtonsoft.Json.Formatting.None);

            if (this.participantInfoState == ParticipantInfoState.MEMBERS_JOINED || this.participantInfoState == ParticipantInfoState.PARTICIPANT_JOINED)
            {
                JArray arr = (JArray)obj[HikeConstants.DATA];
                List<GroupParticipant> addedMembers = null;
                for (int i = 0; i < arr.Count; i++)
                {
                    JObject nameMsisdn = (JObject)arr[i];
                    string msisdn = (string)nameMsisdn[HikeConstants.MSISDN];
                    if (msisdn == App.MSISDN)
                        continue;
                    bool onhike = true;
                    bool dnd = true;
                    try
                    {
                        onhike = (bool)nameMsisdn["onhike"];
                    }
                    catch { }
                    try
                    {
                        dnd = (bool)nameMsisdn["dnd"];
                    }
                    catch { }

                    GroupParticipant gp = GroupManager.Instance.getGroupParticipant((string)nameMsisdn[HikeConstants.NAME], msisdn, _msisdn);
                    gp.HasLeft = false;
                    if (!isSelfGenerated) // if you yourself created JSON dont update these as GP is already updated while creating grp.
                    {
                        gp.IsOnHike = onhike;
                        gp.IsDND = dnd;
                    }
                    if (addedLater)
                    {
                        if (addedMembers == null)
                            addedMembers = new List<GroupParticipant>(arr.Count);
                        addedMembers.Add(gp);
                    }
                }
                if (!isSelfGenerated) // when I am group owner chache is already sorted
                    GroupManager.Instance.GroupCache[toVal].Sort();
                if (addedLater)
                {
                    addedMembers.Sort();
                    this._message = GetMsgText(addedMembers, false);
                }
                else
                    this._message = GetMsgText(GroupManager.Instance.GroupCache[toVal], true);
            }

            else if (this.participantInfoState == ParticipantInfoState.GROUP_END)
            {
                this._message = AppResources.GROUP_CHAT_END;
            }
            else if (this.participantInfoState == ParticipantInfoState.PARTICIPANT_LEFT || this.participantInfoState == ParticipantInfoState.INTERNATIONAL_GROUP_USER)// Group member left
            {
                this._groupParticipant = (toVal != null) ? (string)obj[HikeConstants.DATA] : null;
                GroupParticipant gp = GroupManager.Instance.getGroupParticipant(_groupParticipant, _groupParticipant, _msisdn);
                this._message = gp.FirstName + AppResources.USER_LEFT;
                gp.HasLeft = true;
                gp.IsUsed = false;
            }

            this._timestamp = TimeUtils.getCurrentTimeStamp();
            if (isSelfGenerated)
                this.MessageStatus = State.UNKNOWN;
            else
                this.MessageStatus = State.RECEIVED_UNREAD;
        }

        private string GetMsgText(List<GroupParticipant> groupList, bool isNewGroup)
        {
            string msg = AppResources.GroupChatWith_Txt;
            if (!isNewGroup)
                msg = AppResources.Added_X_To_GC;
            switch (groupList.Count)
            {
                case 1:
                    return string.Format(msg, groupList[0].FirstName);
                case 2:
                    return string.Format(msg, groupList[0].FirstName + AppResources.And_txt
                    + groupList[1].FirstName);
                default:
                    return string.Format(msg, string.Format(AppResources.NamingConvention_Txt,groupList[0].FirstName ,groupList.Count - 1));
            }
        }

        public ConvMessage(ParticipantInfoState participantInfoState, JObject jsonObj)
        {
            this.MessageId = -1;
            this.participantInfoState = participantInfoState;
            this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
            this.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
            this.Timestamp = TimeUtils.getCurrentTimeStamp();
            switch (this.participantInfoState)
            {
                case ParticipantInfoState.INTERNATIONAL_USER:
                    this.Message = AppResources.SMS_INDIA;
                    break;
                case ParticipantInfoState.STATUS_UPDATE:
                    JObject data = (JObject)jsonObj[HikeConstants.DATA];
                    JToken val;
                    if (data.TryGetValue(HikeConstants.TEXT_UPDATE_MSG, out val) && val != null)
                        this.Message = val.ToString();
                    else // this is to handle profile pic update
                        this.Message = "pu";
                    break;
                default: break;
            }
        }
    }
}
