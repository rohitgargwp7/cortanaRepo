﻿using System;
using System.Windows;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;
using CommonLibrary.utils;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using CommonLibrary.Misc;
using CommonLibrary.Languages;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using CommonLibrary.Model.Sticker;
using CommonLibrary.Constants;

namespace CommonLibrary.Model
{
    [Table(Name = "messages")]
    [Index(Columns = "Msisdn,Timestamp ASC", IsUnique = false, Name = "Msg_Idx")]
    public class ConvMessage : INotifyPropertyChanging
    {
        private long _messageId; // this corresponds to msgID stored in sender's DB
        private string _msisdn;
        private string _message;
        private State _messageStatus;
        private long _timestamp;
        private long _mappedMessageId; // this corresponds to msgID stored in receiver's DB
        private bool _isInvite;
        private bool _isSms;
        private string _groupParticipant;
        private string metadataJsonString;
        private ParticipantInfoState participantInfoState;
        private Attachment _fileAttachment = null;
        private StickerObj _stickerObj;
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
            UNKNOWN,
            FORCE_SMS_SENT_CONFIRMED,
            FORCE_SMS_SENT_DELIVERED, /* message delivered to client device */
            FORCE_SMS_SENT_DELIVERED_READ, /* message viewed by recipient */
            SENT_SOCKET_WRITE // mesage written on socket layer but not acked back
        }

        public enum ParticipantInfoState
        {
            NO_INFO, // This is a normal message
            PARTICIPANT_LEFT, // The participant has left
            PARTICIPANT_JOINED, // The participant has joined
            MEMBERS_JOINED, // this is used in new scenario
            GROUP_END, // Group chat has ended
            GROUP_NAME_CHANGE,
            GROUP_PIC_CHANGED,
            USER_OPT_IN,
            USER_JOINED,
            USER_REJOINED,
            HIKE_USER,
            SMS_USER,
            DND_USER,
            GROUP_JOINED_OR_WAITING,
            CREDITS_GAINED,
            INTERNATIONAL_USER,
            INTERNATIONAL_GROUP_USER,
            TYPING_NOTIFICATION,
            STATUS_UPDATE,
            IN_APP_TIP,
            FORCE_SMS_NOTIFICATION,
            CHAT_BACKGROUND_CHANGED,
            CHAT_BACKGROUND_CHANGE_NOT_SUPPORTED,
            MESSAGE_STATUS,
            UNREAD_NOTIFICATION,
            PIN_MESSAGE
        }

        public enum MessageType
        {
            HIKE_PARTICIPANT_JOINED, // hike participant has left
            SMS_PARTICIPANT_OPTED_IN, // sms participant has joined Group Chat
            SMS_PARTICIPANT_INVITED, // sms participant has invited
            PARTICIPANT_LEFT, // The participant has joined
            GROUP_END, // Group chat has ended
            USER_JOINED_HIKE, // Sms user joined hike
            WAITING,
            REWARD,
            INTERNATIONAL_USER_BLOCKED,
            TEXT_UPDATE,
            PIC_UPDATE,
            GROUP_NAME_CHANGED,
            GROUP_PIC_CHANGED,
            DEFAULT,
            UNKNOWN,
            FORCE_SMS,
            CHAT_BACKGROUND
        }

        /// <summary>
        /// Get the participant info from Json Object
        /// </summary>
        /// <param name="obj">json object</param>
        /// <returns>Participant info type for the conv message</returns>
        public static ParticipantInfoState fromJSON(JObject obj)
        {
            if (obj == null)
                return ParticipantInfoState.NO_INFO;

            JToken typeToken = null;
            string type = null;

            if (obj.TryGetValue(ServerJsonKeys.GC_PIN,out typeToken))
                return ParticipantInfoState.PIN_MESSAGE;

            if (obj.TryGetValue(ServerJsonKeys.TYPE, out typeToken))
                type = typeToken.ToString();
            else
                type = null;

            if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_JOIN == type)
                return ParticipantInfoState.PARTICIPANT_JOINED;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_JOIN_NEW == type)
                return ParticipantInfoState.MEMBERS_JOINED;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_LEAVE == type)
            {
                JToken jt = null;
                if (obj.TryGetValue(ServerJsonKeys.SUB_TYPE, out jt))
                    return ParticipantInfoState.INTERNATIONAL_GROUP_USER;
                return ParticipantInfoState.PARTICIPANT_LEFT;
            }
            else if (ServerJsonKeys.MqttMessageTypes.STATUS_UPDATE == type)
                return ParticipantInfoState.STATUS_UPDATE;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_END == type)
                return ParticipantInfoState.GROUP_END;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_USER_JOINED_OR_WAITING == type)
                return ParticipantInfoState.GROUP_JOINED_OR_WAITING;
            else if (ServerJsonKeys.MqttMessageTypes.USER_OPT_IN == type)
                return ParticipantInfoState.USER_OPT_IN;
            else if (ServerJsonKeys.MqttMessageTypes.USER_JOIN == type)
            {
                bool isRejoin = false;
                JToken subtype;
                if (obj.TryGetValue(ServerJsonKeys.SUB_TYPE, out subtype))
                {
                    isRejoin = ServerJsonKeys.SUBTYPE_REJOIN == (string)subtype;
                }
                return isRejoin ? ParticipantInfoState.USER_REJOINED : ParticipantInfoState.USER_JOINED;
            }
            else if (ServerJsonKeys.MqttMessageTypes.HIKE_USER == type)
                return ParticipantInfoState.HIKE_USER;
            else if (ServerJsonKeys.MqttMessageTypes.SMS_USER == type)
                return ParticipantInfoState.SMS_USER;
            else if ("credits_gained" == type)
                return ParticipantInfoState.CREDITS_GAINED;
            else if (ServerJsonKeys.MqttMessageTypes.BLOCK_INTERNATIONAL_USER == type)
                return ParticipantInfoState.INTERNATIONAL_USER;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_NAME == type)
                return ParticipantInfoState.GROUP_NAME_CHANGE;
            else if (ServerJsonKeys.MqttMessageTypes.DND_USER_IN_GROUP == type)
                return ParticipantInfoState.DND_USER;
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_DISPLAY_PIC == type)
                return ParticipantInfoState.GROUP_PIC_CHANGED;
            else if (ServerJsonKeys.MqttMessageTypes.CHAT_BACKGROUNDS == type)
                return ParticipantInfoState.CHAT_BACKGROUND_CHANGED;
            else  // shows type == null
            {
                // maybe dead code - handling done for dndnumbers in MessageMetaData.cs
                JToken jarray;
                if (obj.TryGetValue(ServerJsonKeys.DND_NUMBERS, out jarray))
                {
                    JArray dndNumbers = (JArray)jarray;
                    if (dndNumbers != null)
                    {
                        return ParticipantInfoState.DND_USER;
                    }
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
                NotifyPropertyChanging("MetaDataString");
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
                    NotifyPropertyChanging("HasAttachment");
                    _hasAttachment = value;
                }
            }
        }

        //to be used for upgrading users
        [Column(CanBeNull = true)]
        public string ReadByInfo
        {
            get;
            set;
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

        public StatusMessage StatusUpdateObj
        {
            get;
            set;
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
                    _isInvite = value;
            }
        }

        public bool IsSent
        {
            get
            {
                return (_messageStatus == State.SENT_UNCONFIRMED ||
                        _messageStatus == State.SENT_CONFIRMED ||
                        _messageStatus == State.SENT_SOCKET_WRITE ||
                        _messageStatus == State.SENT_DELIVERED ||
                        _messageStatus == State.SENT_DELIVERED_READ ||
                        _messageStatus == State.SENT_FAILED ||
                        _messageStatus == State.FORCE_SMS_SENT_CONFIRMED ||
                        _messageStatus == State.FORCE_SMS_SENT_DELIVERED ||
                        _messageStatus == State.FORCE_SMS_SENT_DELIVERED_READ);
            }
        }

        public bool IsSms
        {
            get
            {
                return _isSms || MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED || MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ;
            }
            set
            {
                if (value != _isSms)
                    _isSms = value;
            }
        }

        public StickerObj StickerObj
        {
            set
            {
                if (value != null)
                    _stickerObj = value;
            }
            get
            {
                return _stickerObj;
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
                    participantInfoState = value;
            }
        }

        private MessageType _notificationType;
        public MessageType NotificationType
        {
            get
            {
                return _notificationType;
            }
            set
            {
                _notificationType = value;
            }
        }

        bool _isGroup;
        bool IsGroup
        {
            get
            {
                return _isGroup;
            }
            set
            {
                if (value != _isGroup)
                    _isGroup = value;
            }
        }

        private bool imageDownloadFailed = false;
        public bool ImageDownloadFailed
        {
            get
            {
                return imageDownloadFailed;
            }
            set
            {
                imageDownloadFailed = value;
            }
        }

        bool _isInAddressBook;
        public bool IsInAddressBook
        {
            get
            {
                return _isInAddressBook;
            }
            set
            {
                if (value != _isInAddressBook)
                    _isInAddressBook = value;
            }
        }

        private string _groupMemeberName;
        public string GroupMemberName
        {
            get
            {
                return _groupMemeberName;
            }
            set
            {
                _groupMemeberName = value;
                IsGroup = true;
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
            MessageStatus = msgState;
        }

        public ConvMessage(string message, ConvMessage convMessage)
        {
            this._message = message;
            _messageId = convMessage.MessageId;
            _msisdn = convMessage.Msisdn;
            _messageStatus = convMessage.MessageStatus;
            _timestamp = convMessage.Timestamp;
            _mappedMessageId = convMessage.MappedMessageId;
            _isInvite = convMessage.IsInvite;
            _isSms = convMessage.IsSms;
            _groupParticipant = convMessage.GroupParticipant;
            metadataJsonString = convMessage.metadataJsonString;
            participantInfoState = convMessage.participantInfoState;
            _fileAttachment = convMessage._fileAttachment;
            _hasAttachment = convMessage._fileAttachment != null;
        }

        public JObject serialize(bool isHikeMsg)
        {
            JObject obj = new JObject();
            JObject data = new JObject();
            JObject metadata;
            JArray filesData;
            JObject singleFileInfo;

            data[isHikeMsg ? ServerJsonKeys.HIKE_MESSAGE : ServerJsonKeys.SMS_MESSAGE] = _message;
            data[ServerJsonKeys.TIMESTAMP] = _timestamp;
            data[ServerJsonKeys.MESSAGE_ID] = _messageId;

            // Added stealth = true at data layer for convmessage for stealth chat
            if (HikeInstantiation.ViewModel != null && HikeInstantiation.ViewModel.ConvMap != null && HikeInstantiation.ViewModel.ConvMap.ContainsKey(Msisdn) && HikeInstantiation.ViewModel.ConvMap[Msisdn].IsHidden)
                data[ServerJsonKeys.STEALTH] = true;

            if (HasAttachment)
            {
                try
                {
                    metadata = new JObject();
                    filesData = new JArray();

                    if (!FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
                    {
                        if (FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT) && !string.IsNullOrEmpty(this.MetaDataString))
                            singleFileInfo = JObject.Parse(this.MetaDataString);
                        else
                            singleFileInfo = new JObject();

                        singleFileInfo[ServerJsonKeys.FILE_NAME] = FileAttachment.FileName;
                        singleFileInfo[ServerJsonKeys.FILE_SIZE] = FileAttachment.FileSize;
                        singleFileInfo[ServerJsonKeys.FILE_KEY] = FileAttachment.FileKey;
                        singleFileInfo[ServerJsonKeys.FILE_CONTENT_TYPE] = FileAttachment.ContentType;
                        singleFileInfo[ServerJsonKeys.SOURCE] = Attachment.GetAttachmentSource(FileAttachment.FileSource);

                        if (FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO) && !String.IsNullOrEmpty(this.MetaDataString))
                        {
                            var timeObj = JObject.Parse(this.MetaDataString);
                            singleFileInfo[ServerJsonKeys.FILE_PLAY_TIME] = timeObj[ServerJsonKeys.FILE_PLAY_TIME];
                        }

                        if (FileAttachment.Thumbnail != null)
                            singleFileInfo[ServerJsonKeys.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
                    }
                    else
                    {
                        //add thumbnail here
                        JObject metadataFromConvMessage = JObject.Parse(this.MetaDataString);
                        JToken tempFileArrayToken;

                        //TODO - Madhur Garg - Metadata of sent & received location are different that's why this if statement is used.
                        //Make it same for type of messages
                        if (metadataFromConvMessage.TryGetValue("files", out tempFileArrayToken) && tempFileArrayToken != null)
                        {
                            JArray tempFilesArray = tempFileArrayToken.ToObject<JArray>();
                            singleFileInfo = tempFilesArray[0].ToObject<JObject>();
                        }
                        else
                        {
                            singleFileInfo = JObject.Parse(this.MetaDataString);
                        }

                        singleFileInfo[ServerJsonKeys.FILE_SIZE] = FileAttachment.FileSize;
                        singleFileInfo[ServerJsonKeys.FILE_KEY] = FileAttachment.FileKey;
                        singleFileInfo[ServerJsonKeys.FILE_NAME] = FileAttachment.FileName;
                        singleFileInfo[ServerJsonKeys.FILE_CONTENT_TYPE] = FileAttachment.ContentType;
                        singleFileInfo[ServerJsonKeys.SOURCE] = Attachment.GetAttachmentSource(FileAttachment.FileSource);

                        if (FileAttachment.Thumbnail != null)
                            singleFileInfo[ServerJsonKeys.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
                    }

                    filesData.Add(singleFileInfo.ToObject<JToken>());
                    metadata[ServerJsonKeys.FILES_DATA] = filesData;
                    data[ServerJsonKeys.METADATA] = metadata;
                }
                catch (Exception e) //Incase  of error receiver will see it as a normal text message with a link (same as sms user)
                {                   //ideally code should never reach here.
                    Debug.WriteLine("ConvMessage :: serialize :: Exception while parsing metadat " + e.StackTrace);
                }
            }
            else if (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE))
            {
                data["poke"] = true;
            }
            else if (metadataJsonString != null && metadataJsonString.Contains(HikeConstants.STICKER_ID))
            {
                data[ServerJsonKeys.METADATA] = JObject.Parse(metadataJsonString);
                obj[ServerJsonKeys.SUB_TYPE] = NetworkManager.STICKER;
            }
            else if (this.MetaDataString != null && this.MetaDataString.Contains(ServerJsonKeys.GC_PIN))
            {
                data[ServerJsonKeys.METADATA] = JObject.Parse(metadataJsonString);
            }

            obj[ServerJsonKeys.TO] = _msisdn;
            obj[ServerJsonKeys.DATA] = data;

            obj[ServerJsonKeys.TYPE] = _isInvite ? NetworkManager.INVITE : NetworkManager.MESSAGE;

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

            //to handle duplicate message check and some other edge cases we cant compare message ids directly
            //For duplicate messsage we check whether that message exists in db or not, so db object has id greater than 0 and and for other object this is -1
            //so if both have greater than zero than this check is sufficient
            if (_messageId > 0 && other.MessageId > 0)
            {
                return _messageId == other.MessageId;
            }

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
            if (_fileAttachment != null && other.FileAttachment != null
                && _fileAttachment.FileKey != null && other.FileAttachment.FileKey != null
                && !_fileAttachment.FileKey.Equals(other.FileAttachment.FileKey))
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
                catch (Exception ex)
                {
                    Debug.WriteLine("ConvMessage :: NotifyPropertyChanging : NotifyPropertyChanging , Exception : " + ex.StackTrace);
                }
            }
        }
        #endregion

        public String GetMessageForServer()
        {
            if (StickerObj != null)
                return String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Sticker_Txt) + ServerUrls.GetStickerUrl + StickerObj.Category + "/" + StickerObj.Id.Substring(0, StickerObj.Id.IndexOf("_"));

            string message = Message;

            if (FileAttachment == null)
                return message;

            if (FileAttachment.ContentType.Contains(FTBasedConstants.IMAGE))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + ServerUrls.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + ServerUrls.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(FTBasedConstants.VIDEO))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + ServerUrls.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Location_Txt) + ServerUrls.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.ContactTransfer_Text) + ServerUrls.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.UnknownFile_txt) + ServerUrls.FILE_TRANSFER_BASE_URL +
                        "/" + FileAttachment.FileKey;

            return message;
        }

        public JObject serializeDeliveryReportRead()
        {
            JObject obj = new JObject();
            JArray ids = new JArray();
            try
            {
                ids.Add(Convert.ToString(_mappedMessageId));
                obj.Add(ServerJsonKeys.DATA, ids);
                obj.Add(ServerJsonKeys.TYPE, NetworkManager.MESSAGE_READ);
                obj.Add(ServerJsonKeys.TO, _msisdn);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConvMessage :: serializeDeliveryReportRead : serializeDeliveryReportRead , Exception : " + ex.StackTrace);
            }
            return obj;
        }

        /// <summary>
        /// Create convmessage from json object. Called in case of message type
        /// </summary>
        /// <param name="obj">json obj which is to be parsed</param>
        public ConvMessage(JObject obj)
        {
            try
            {
                bool isFileTransfer;
                JObject metadataObject = null;
                JToken val = null;
                obj.TryGetValue(ServerJsonKeys.TO, out val);
                string messageText = String.Empty;

                JToken metadataToken = null;
                try
                {
                    obj[ServerJsonKeys.DATA].ToObject<JObject>().TryGetValue(ServerJsonKeys.METADATA, out metadataToken);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ConvMessage ::  ConvMessage constructor : metadata parse , Exception : " + ex.StackTrace);
                }

                if (metadataToken != null)
                {
                    metadataObject = JObject.FromObject(metadataToken);
                    JToken filesToken = null;
                    isFileTransfer = metadataObject.TryGetValue(ServerJsonKeys.FILES_DATA, out filesToken);
                    if (isFileTransfer)
                    {
                        JArray files = metadataObject[ServerJsonKeys.FILES_DATA].ToObject<JArray>();
                        JObject fileObject = files[0].ToObject<JObject>();

                        JToken fileName;
                        JToken fileKey;
                        JToken thumbnail;
                        JToken contentType;
                        JToken fileSize;

                        int fs = 0;

                        fileObject.TryGetValue(ServerJsonKeys.FILE_CONTENT_TYPE, out contentType);
                        fileObject.TryGetValue(ServerJsonKeys.FILE_NAME, out fileName);

                        // These two conditions check for empty filename received on json packet. This bug was mainly occured on packets received from Android devices
                        if (contentType.ToString().Contains(FTBasedConstants.LOCATION))
                        {
                            if (fileName == null || String.IsNullOrWhiteSpace(fileName.ToString()))
                                fileName = AppResources.Location_Txt;
                        }
                        else if (contentType.ToString().Contains(FTBasedConstants.CONTACT))
                        {
                            if (fileName == null || String.IsNullOrWhiteSpace(fileName.ToString()))
                            {
                                fileObject.TryGetValue(ServerJsonKeys.CS_NAME, out fileName);

                                if (fileName == null || String.IsNullOrWhiteSpace(fileName.ToString()))
                                    fileName = AppResources.ContactTransfer_Text;
                            }
                        }

                        fileObject.TryGetValue(ServerJsonKeys.FILE_KEY, out fileKey);
                        fileObject.TryGetValue(ServerJsonKeys.FILE_THUMBNAIL, out thumbnail);

                        if (fileObject.TryGetValue(ServerJsonKeys.FILE_SIZE, out fileSize))
                            fs = Convert.ToInt32(fileSize.ToString());

                        this.HasAttachment = true;

                        byte[] base64Decoded = null;
                        if (thumbnail != null)
                            base64Decoded = System.Convert.FromBase64String(thumbnail.ToString());

                        if (contentType.ToString().Contains(FTBasedConstants.LOCATION))
                        {
                            this.FileAttachment = new Attachment(fileName.ToString(), fileKey == null ? String.Empty : fileKey.ToString(), base64Decoded,
                        contentType.ToString(), Attachment.AttachmentState.NOT_STARTED, Attachment.AttachemntSource.CAMERA, fs);

                            JObject locationFile = new JObject();
                            locationFile[ServerJsonKeys.LATITUDE] = fileObject[ServerJsonKeys.LATITUDE];
                            locationFile[ServerJsonKeys.LONGITUDE] = fileObject[ServerJsonKeys.LONGITUDE];
                            locationFile[ServerJsonKeys.ZOOM_LEVEL] = fileObject[ServerJsonKeys.ZOOM_LEVEL];
                            locationFile[ServerJsonKeys.LOCATION_ADDRESS] = fileObject[ServerJsonKeys.LOCATION_ADDRESS];
                            locationFile[ServerJsonKeys.LOCATION_TITLE] = fileObject[ServerJsonKeys.LOCATION_TITLE];

                            this.MetaDataString = locationFile.ToString(Newtonsoft.Json.Formatting.None); // store location data in metadata
                        }
                        else
                        {
                            this.FileAttachment = new Attachment(fileName.ToString(), fileKey == null ? String.Empty : fileKey.ToString(), base64Decoded,
                           contentType.ToString(), Attachment.AttachmentState.NOT_STARTED, Attachment.AttachemntSource.CAMERA, fs);
                        }

                        if (contentType.ToString().Contains(FTBasedConstants.CONTACT) || contentType.ToString().Contains(FTBasedConstants.AUDIO))
                        {
                            this.MetaDataString = fileObject.ToString(Newtonsoft.Json.Formatting.None); // store file object for contact and audio
                        }
                    }
                    else
                    {
                        metadataJsonString = metadataObject.ToString(Newtonsoft.Json.Formatting.None); // store only metadata
                    }
                }

                participantInfoState = fromJSON(metadataObject);
                if (val != null) // represents group message
                {
                    _msisdn = val.ToString();
                    _groupParticipant = (string)obj[ServerJsonKeys.FROM];
                }
                else
                {
                    _msisdn = (string)obj[ServerJsonKeys.FROM]; /*represents msg is coming from another client or system msg*/
                    _groupParticipant = null;
                }

                JObject data = (JObject)obj[ServerJsonKeys.DATA];
                JToken msg;

                if (data.TryGetValue(ServerJsonKeys.SMS_MESSAGE, out msg)) // if sms 
                {
                    _message = msg.ToString();
                    _isSms = true;
                }
                else       // if not sms
                {
                    _isSms = false;
                    if (this.HasAttachment)
                    {
                        if (this.FileAttachment.ContentType.Contains(FTBasedConstants.IMAGE))
                            messageText = AppResources.Image_Txt;
                        else if (this.FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                            messageText = AppResources.Audio_Txt;
                        else if (this.FileAttachment.ContentType.Contains(FTBasedConstants.VIDEO))
                            messageText = AppResources.Video_Txt;
                        else if (this.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
                            messageText = AppResources.Location_Txt;
                        else if (this.FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
                            messageText = AppResources.ContactTransfer_Text;
                        else
                            messageText = AppResources.UnknownFile_txt;
                        this._message = messageText;
                    }
                    else
                    {
                        if (participantInfoState == ParticipantInfoState.INTERNATIONAL_USER)
                            _message = AppResources.SMS_Works_Only_In_India_Txt;
                        else
                            _message = (string)data[ServerJsonKeys.HIKE_MESSAGE];
                    }

                }
                if (data.TryGetValue("poke", out msg)) // if sms 
                {
                    metadataJsonString = "{poke: true}"; //metadata in case of nudge
                }
                JToken isSticker;
                JToken stickerJson;
                if (obj.TryGetValue(ServerJsonKeys.SUB_TYPE, out isSticker) && data.TryGetValue(ServerJsonKeys.METADATA, out stickerJson))
                {
                    metadataJsonString = stickerJson.ToString(Newtonsoft.Json.Formatting.None); // metadata for stickers
                    _message = AppResources.Sticker_Txt;
                }

                long serverTimeStamp = (long)data[ServerJsonKeys.TIMESTAMP];

                long timedifference;
                if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.TIME_DIFF_EPOCH, out timedifference))
                    _timestamp = serverTimeStamp - timedifference;
                else
                    _timestamp = serverTimeStamp;

                /* prevent us from receiving a message from the future */

                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / 1000;
                this.Timestamp = (this.Timestamp > now) ? now : this.Timestamp;

                /* if we're deserialized an object from json, it's always unread */
                this.MessageStatus = State.RECEIVED_UNREAD;
                this._messageId = -1;
                string mappedMsgID = (string)data[ServerJsonKeys.MESSAGE_ID];
                this.MappedMessageId = System.Int64.Parse(mappedMsgID);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConvMessage ::  ConvMessage constructor :  parse json , Exception : " + ex.StackTrace);
                throw new Exception("Error in parsing json");
            }
        }

        public ConvMessage()
        {
        }

        /// <summary>
        /// Create convMessage from json Object. Called for group chats
        /// </summary>
        /// <param name="obj">json object to be parsed</param>
        /// <param name="isSelfGenerated">indicates that the object generated by client</param>
        /// <param name="addedLater">indicates that the members will be added later on runtime</param>
        public ConvMessage(JObject obj, bool isSelfGenerated, bool addedLater)
        {
            // If the message is a group message we get a TO field consisting of the Group ID
            string toVal = obj[ServerJsonKeys.TO].ToString();
            this._msisdn = (toVal != null) ? (string)obj[ServerJsonKeys.TO] : (string)obj[ServerJsonKeys.FROM]; /*represents msg is coming from another client*/
            this._groupParticipant = (toVal != null) ? (string)obj[ServerJsonKeys.FROM] : null;

            JObject metaDataObj = new JObject();

            JToken type;
            if (obj.TryGetValue(ServerJsonKeys.TYPE, out type))
                metaDataObj.Add(ServerJsonKeys.TYPE, type);

            JToken subType;
            if (obj.TryGetValue(ServerJsonKeys.SUB_TYPE, out subType))
                metaDataObj.Add(ServerJsonKeys.SUB_TYPE, subType);

            this.participantInfoState = fromJSON(metaDataObj);
            this.metadataJsonString = metaDataObj.ToString(Newtonsoft.Json.Formatting.None);

            if (this.participantInfoState == ParticipantInfoState.MEMBERS_JOINED || this.participantInfoState == ParticipantInfoState.PARTICIPANT_JOINED)
            {
                JArray arr = (JArray)obj[ServerJsonKeys.DATA];
                List<GroupParticipant> addedMembers = null;
                for (int i = 0; i < arr.Count; i++)
                {
                    JObject nameMsisdn = (JObject)arr[i];
                    string msisdn = (string)nameMsisdn[ServerJsonKeys.MSISDN];
                    if (msisdn == HikeInstantiation.MSISDN)
                        continue;
                    bool onhike = true;
                    bool dnd = true;
                    try
                    {
                        onhike = (bool)nameMsisdn["onhike"];
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConvMessage ::  ConvMessage(JObject obj, bool isSelfGenerated, bool addedLater) :  parse json onhike, Exception : " + ex.StackTrace);
                    }
                    try
                    {
                        dnd = (bool)nameMsisdn["dnd"];
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ConvMessage ::  ConvMessage(JObject obj, bool isSelfGenerated, bool addedLater) :  parse json dnd, Exception : " + ex.StackTrace);
                    }

                    GroupParticipant gp = GroupManager.Instance.GetGroupParticipant((string)nameMsisdn[ServerJsonKeys.NAME], msisdn, _msisdn);
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
                    GroupManager.Instance.GroupParticpantsCache[toVal].Sort();
                if (addedLater)
                {
                    addedMembers.Sort();
                    this._message = GetMsgText(addedMembers, false);
                }
                else
                    this._message = GetMsgText(GroupManager.Instance.GroupParticpantsCache[toVal], true);

                this._message = this._message.Replace(";", String.Empty);// as while displaying MEMBERS_JOINED in CT we split on ; for dnd message
            }

            else if (this.participantInfoState == ParticipantInfoState.GROUP_END)
            {
                this._message = AppResources.GROUP_CHAT_END;
            }
            else if (this.participantInfoState == ParticipantInfoState.PARTICIPANT_LEFT || this.participantInfoState == ParticipantInfoState.INTERNATIONAL_GROUP_USER)// Group member left
            {
                this._groupParticipant = (toVal != null) ? (string)obj[ServerJsonKeys.DATA] : null;
                GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(_groupParticipant, _groupParticipant, _msisdn);
                this._message = String.Format(AppResources.USER_LEFT, gp.FirstName);
                gp.HasLeft = true;
                gp.IsUsed = false;
            }

            this._timestamp = TimeUtils.GetCurrentTimeStamp();
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
                    return string.Format(msg, string.Format(AppResources.NamingConvention_Txt, groupList[0].FirstName, groupList.Count - 1));
            }
        }

        public void SetAttachmentState(Attachment.AttachmentState attachmentState)
        {
            this.FileAttachment.FileState = attachmentState;
        }

        public ConvMessage(ParticipantInfoState participantInfoState, JObject jsonObj, long timeStamp = 0)
        {
            string grpId;
            string from;
            GroupParticipant gp;
            this.MessageId = -1;
            this.participantInfoState = participantInfoState;
            this.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
            this.Timestamp = timeStamp == 0 ? TimeUtils.GetCurrentTimeStamp() : timeStamp;

            switch (this.participantInfoState)
            {
                case ParticipantInfoState.INTERNATIONAL_USER:
                    this.Message = AppResources.SMS_INDIA;
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.STATUS_UPDATE:
                    JObject data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    JToken val;

                    // this is to handle profile pic update
                    if (data.TryGetValue(ServerJsonKeys.PROFILE_UPDATE, out val) && true == (bool)val)
                        this.Message = AppResources.Update_Profile_Pic_txt;
                    else  // status , moods update
                    {
                        if (data.TryGetValue(ServerJsonKeys.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                            this.Message = val.ToString();
                    }

                    data.Remove(ServerJsonKeys.THUMBNAIL);
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.GROUP_NAME_CHANGE:
                    grpId = (string)jsonObj[ServerJsonKeys.TO];
                    from = (string)jsonObj[ServerJsonKeys.FROM];
                    string grpName = (string)jsonObj[ServerJsonKeys.DATA];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == HikeInstantiation.MSISDN)
                    {
                        this.Message = string.Format(AppResources.GroupNameChangedByGrpMember_Txt, AppResources.You_Txt, grpName);
                    }
                    else
                    {
                        gp = GroupManager.Instance.GetGroupParticipant(null, from, grpId);
                        this.Message = string.Format(AppResources.GroupNameChangedByGrpMember_Txt, gp.Name, grpName);
                    }
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.GROUP_PIC_CHANGED:
                    grpId = (string)jsonObj[ServerJsonKeys.TO];
                    from = (string)jsonObj[ServerJsonKeys.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    gp = GroupManager.Instance.GetGroupParticipant(null, from, grpId);
                    this.Message = string.Format(AppResources.GroupImgChangedByGrpMember_Txt, gp.Name);
                    jsonObj.Remove(ServerJsonKeys.DATA);
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.CHAT_BACKGROUND_CHANGED:
                    grpId = (string)jsonObj[ServerJsonKeys.TO];
                    from = (string)jsonObj[ServerJsonKeys.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == HikeInstantiation.MSISDN)
                        this.Message = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
                    else
                    {
                        gp = GroupManager.Instance.GetGroupParticipant(null, from, grpId);
                        this.Message = string.Format(AppResources.ChatBg_Changed_Text, gp.Name);
                    }
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.CHAT_BACKGROUND_CHANGE_NOT_SUPPORTED:
                    grpId = (string)jsonObj[ServerJsonKeys.TO];
                    from = (string)jsonObj[ServerJsonKeys.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == HikeInstantiation.MSISDN)
                        this.Message = string.Format(AppResources.ChatBg_NotChanged_Text, AppResources.You_Txt);
                    else
                    {
                        gp = GroupManager.Instance.GetGroupParticipant(null, from, grpId);
                        this.Message = string.Format(AppResources.ChatBg_NotChanged_Text, gp.Name);
                    }
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                default:
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
            }
        }
    }
}
