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
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

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
            HIKE_USER,
            SMS_USER,
            DND_USER,
            GROUP_JOINED_OR_WAITING,
            CREDITS_GAINED,
            INTERNATIONAL_USER,
            INTERNATIONAL_GROUP_USER,
            TYPING_NOTIFICATION,
            STATUS_UPDATE
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
            UNKNOWN
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
            else if (HikeConstants.MqttMessageTypes.GROUP_DISPLAY_PIC == type)
            {
                return ParticipantInfoState.GROUP_PIC_CHANGED;
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
                    _messageStatus = value;
                    NotifyPropertyChanged("SdrImage");
                    NotifyPropertyChanged("MessageStatus");
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
                    _timestamp = value;
                    NotifyPropertyChanged("Timestamp");
                    NotifyPropertyChanged("TimeStampStr");
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
                }
            }
        }

        private MessageType _notificationType;
        private BitmapImage _statusUpdateImage;
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
        public string TimeStampStr
        {
            get
            {
                if (participantInfoState == ParticipantInfoState.STATUS_UPDATE)
                    return TimeUtils.getRelativeTime(_timestamp);
                else
                    return TimeUtils.getTimeStringForChatThread(_timestamp);
            }
        }

        public string DispMessage
        {
            get
            {
                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                {
                    return string.IsNullOrEmpty(_fileAttachment.FileName) ? "contact" : _fileAttachment.FileName;
                }
                else
                    return _message;
            }
        }

        public BitmapImage SdrImage
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
                    case ConvMessage.State.SENT_FAILED:
                        return UI_Utils.Instance.HttpFailed;
                    case ConvMessage.State.SENT_UNCONFIRMED:
                        return UI_Utils.Instance.Trying;
                    default:
                        return UI_Utils.Instance.Trying;

                }
            }
        }
        private PageOrientation _currentOrientation;
        public PageOrientation CurrentOrientation
        {
            get
            {
                return _currentOrientation;
            }
            set
            {
                _currentOrientation = value;
                NotifyPropertyChanged("LayoutGridWidth");
                NotifyPropertyChanged("DataTemplateMargin");
            }
        }
        public BitmapImage MessageImage
        {
            get
            {

                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                {
                    if (_isSent)
                        return UI_Utils.Instance.WhiteContactIcon;
                    else
                        return UI_Utils.Instance.ContactIcon;
                }
                else if (_fileAttachment != null && _fileAttachment.Thumbnail != null)
                {
                    return UI_Utils.Instance.createImageFromBytes(_fileAttachment.Thumbnail);
                }
                else
                {
                    return UI_Utils.Instance.AudioAttachmentSend;
                }

            }
        }

        public Visibility PlayIconVisibility
        {
            get
            {
                if (_fileAttachment != null && ((_fileAttachment.FileState == Attachment.AttachmentState.CANCELED || _fileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED)
                    || _fileAttachment.ContentType.Contains(HikeConstants.VIDEO) || _fileAttachment.ContentType.Contains(HikeConstants.AUDIO)))
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }

        public BitmapImage PlayIconImage
        {
            get
            {
                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                    return UI_Utils.Instance.DownloadIcon;
                else
                    return UI_Utils.Instance.PlayIcon;
            }
        }

        double _progressBarValue = 0;
        public double ProgressBarValue
        {
            set
            {
                _progressBarValue = value;
                if (_progressBarValue >= 100)
                    NotifyPropertyChanging("PlayIconVisibility");
                NotifyPropertyChanged("ProgressBarVisibility");
                NotifyPropertyChanged("ProgressBarValue");
            }
            get
            {
                return _progressBarValue;
            }
        }

        public Visibility ProgressBarVisibility
        {
            get
            {
                if (_progressBarValue <= 0 || _progressBarValue >= 100)
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
            }
        }

        public BitmapImage NotificationImage
        {
            get
            {
                switch (_notificationType)
                {
                    case MessageType.HIKE_PARTICIPANT_JOINED:
                        return UI_Utils.Instance.OnHikeImage;

                    case MessageType.SMS_PARTICIPANT_INVITED:
                        return UI_Utils.Instance.NotOnHikeImage;

                    case MessageType.SMS_PARTICIPANT_OPTED_IN:
                        return UI_Utils.Instance.ChatAcceptedImage;

                    case MessageType.USER_JOINED_HIKE:
                        return UI_Utils.Instance.OnHikeImage;

                    case MessageType.PARTICIPANT_LEFT:
                        return UI_Utils.Instance.ParticipantLeft;

                    case MessageType.GROUP_END:
                        return UI_Utils.Instance.ParticipantLeft;

                    case MessageType.WAITING:
                        return UI_Utils.Instance.Waiting;

                    case MessageType.REWARD:
                        return UI_Utils.Instance.Reward;

                    case MessageType.INTERNATIONAL_USER_BLOCKED:
                        return UI_Utils.Instance.IntUserBlocked;

                    case MessageType.PIC_UPDATE:
                        return UI_Utils.Instance.OnHikeImage;

                    case MessageType.GROUP_NAME_CHANGED:
                        return UI_Utils.Instance.GrpNameOrPicChanged;

                    case MessageType.GROUP_PIC_CHANGED:
                        return UI_Utils.Instance.GrpNameOrPicChanged;

                    case MessageType.TEXT_UPDATE:
                    default:
                        return UI_Utils.Instance.OnHikeImage;

                }
            }
        }

        public BitmapImage StatusUpdateImage
        {
            set
            {
                _statusUpdateImage = value;
            }
            get
            {
                if (_statusUpdateImage != null)
                    return _statusUpdateImage;
                else
                    return MoodsInitialiser.Instance.GetMoodImageForMoodId(MoodsInitialiser.GetMoodId(metadataJsonString));
            }
        }

        public Visibility ShowCancelMenu
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility ShowForwardMenu
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.COMPLETED)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility ShowDeleteMenu
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        public Visibility GroupMemberVisibility
        {
            get
            {
                if (string.IsNullOrEmpty(_groupMemeberName))
                {
                    return Visibility.Collapsed;
                }
                return Visibility.Visible;
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
            }
        }

        public double LayoutGridWidth
        {
            get
            {
                if (_currentOrientation == PageOrientation.LandscapeLeft || _currentOrientation == PageOrientation.LandscapeRight)
                    return 768;
                return 480;
            }
        }

        public SolidColorBrush BubbleBackGroundColor
        {
            get
            {
                if (participantInfoState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    return UI_Utils.Instance.StatusBubbleColor;
                }
                else if (IsSent)
                {
                    if (IsSms)
                    {
                        return UI_Utils.Instance.SmsBackground;
                    }
                    else
                    {
                        return UI_Utils.Instance.HikeMsgBackground;
                    }
                }
                else
                {
                    return UI_Utils.Instance.ReceivedChatBubbleColor;
                }
            }
        }

        public SolidColorBrush TimeStampForeGround
        {
            get
            {
                if (participantInfoState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    return UI_Utils.Instance.ReceivedChatBubbleTimestamp;
                }
                else if (IsSent)
                {
                    if (IsSms)
                    {
                        return UI_Utils.Instance.SMSSentChatBubbleTimestamp;
                    }
                    else
                    {
                        return UI_Utils.Instance.HikeSentChatBubbleTimestamp;
                    }
                }
                else
                {
                    return UI_Utils.Instance.ReceivedChatBubbleTimestamp;
                }
            }
        }

        public SolidColorBrush MessageTextForeGround
        {
            get
            {
                if (participantInfoState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                {
                    return UI_Utils.Instance.ReceiveMessageForeground;
                }
                else if (IsSent)
                {
                    return UI_Utils.Instance.White;
                }
                else
                {
                    return UI_Utils.Instance.ReceiveMessageForeground;
                }
            }
        }

        public Thickness DataTemplateMargin
        {
            get
            {
                if (_currentOrientation == PageOrientation.LandscapeLeft || _currentOrientation == PageOrientation.LandscapeRight)
                {
                    if (IsSent)
                    {
                        if (FileAttachment == null || FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                            return UI_Utils.Instance.SentBubbleTextMarginLS;
                        else
                            return UI_Utils.Instance.SentBubbleFileMarginLS;
                    }
                    else
                    {
                        if (FileAttachment == null || FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                            return UI_Utils.Instance.RecievedBubbleTextMarginLS;
                        else
                            return UI_Utils.Instance.ReceivedBubbleFileMarginLS;
                    }
                }
                else
                {
                    if (IsSent)
                    {
                        if (FileAttachment == null || FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                            return UI_Utils.Instance.SentBubbleTextMarginPortrait;
                        else
                            return UI_Utils.Instance.SentBubbleFileMarginPortrait;
                    }
                    else
                    {
                        if (FileAttachment == null || FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                            return UI_Utils.Instance.RecMessageBubbleTextMarginPortrait;
                        else
                            return UI_Utils.Instance.ReceivedBubbleFileMarginPortrait;
                    }
                }
            }
        }
        public ConvMessage(string message, string msisdn, long timestamp, State msgState, PageOrientation currentOrientation)
            : this(message, msisdn, timestamp, msgState, -1, -1, currentOrientation)
        {
        }
        public ConvMessage(string message, string msisdn, long timestamp, State msgState)
            : this(message, msisdn, timestamp, msgState, -1, -1, PageOrientation.Portrait)
        {
        }
        public ConvMessage(string message, string msisdn, long timestamp, State msgState, long msgid, long mappedMsgId, PageOrientation currentOrientation)
        {
            this._msisdn = msisdn;
            this._message = message;
            this._timestamp = timestamp;
            this._messageId = msgid;
            this._mappedMessageId = mappedMsgId;
            this._currentOrientation = currentOrientation;
            _isSent = (msgState == State.SENT_UNCONFIRMED ||
                        msgState == State.SENT_CONFIRMED ||
                        msgState == State.SENT_DELIVERED ||
                        msgState == State.SENT_DELIVERED_READ ||
                        msgState == State.SENT_FAILED);
            MessageStatus = msgState;
        }

        public ConvMessage(string message, PageOrientation currentOrientation, ConvMessage convMessage)
        {
            this._message = message;
            this._currentOrientation = currentOrientation;
            _messageId = convMessage.MessageId;
            _msisdn = convMessage.Msisdn;
            _messageStatus = convMessage.MessageStatus;
            _timestamp = convMessage.Timestamp;
            _mappedMessageId = convMessage.MappedMessageId;
            _isInvite = convMessage.IsInvite;
            _isSent = convMessage.IsSent;
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
            if (isHikeMsg)
                data[HikeConstants.HIKE_MESSAGE] = _message;
            else
                data[HikeConstants.SMS_MESSAGE] = _message;
            data[HikeConstants.TIMESTAMP] = _timestamp;
            data[HikeConstants.MESSAGE_ID] = _messageId;

            if (HasAttachment)
            {
                try
                {
                    metadata = new JObject();
                    filesData = new JArray();
                    if (!FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                    {
                        if (FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT) && !string.IsNullOrEmpty(this.MetaDataString))
                            singleFileInfo = JObject.Parse(this.MetaDataString);
                        else
                            singleFileInfo = new JObject();
                        singleFileInfo[HikeConstants.FILE_NAME] = FileAttachment.FileName;
                        singleFileInfo[HikeConstants.FILE_KEY] = FileAttachment.FileKey;
                        singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = FileAttachment.ContentType;
                        if (FileAttachment.Thumbnail != null)
                            singleFileInfo[HikeConstants.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
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
                        singleFileInfo[HikeConstants.FILE_KEY] = FileAttachment.FileKey;
                        singleFileInfo[HikeConstants.FILE_NAME] = FileAttachment.FileName;
                        singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = FileAttachment.ContentType;
                        if (FileAttachment.Thumbnail != null)
                            singleFileInfo[HikeConstants.FILE_THUMBNAIL] = System.Convert.ToBase64String(FileAttachment.Thumbnail);
                    }
                    filesData.Add(singleFileInfo.ToObject<JToken>());
                    metadata[HikeConstants.FILES_DATA] = filesData;
                    data[HikeConstants.METADATA] = metadata;
                }
                catch (Exception e) //Incase  of error receiver will see it as a normal text message with a link (same as sms user)
                {                   //ideally code should never reach here.
                    Debug.WriteLine("ConvMessage :: serialize :: Exception while parsing metadat " + e.StackTrace);
                }
            }
            else if (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE))
            {
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine("ConvMessage :: NotifyPropertyChanged : NotifyPropertyChanged , Exception : " + ex.StackTrace);
                        }
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
                catch (Exception ex)
                {
                    Debug.WriteLine("ConvMessage :: NotifyPropertyChanging : NotifyPropertyChanging , Exception : " + ex.StackTrace);
                }
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
            catch (Exception ex)
            {
                Debug.WriteLine("ConvMessage :: serializeDeliveryReportRead : serializeDeliveryReportRead , Exception : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("ConvMessage ::  ConvMessage constructor : metadata parse , Exception : " + ex.StackTrace);
                }

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
                        this.FileAttachment = new Attachment(fileName == null ? "" : fileName.ToString(), fileKey == null ? "" : fileKey.ToString(), base64Decoded,
                           contentType.ToString(), Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                        if (contentType.ToString().Contains(HikeConstants.LOCATION))
                        {
                            JObject locationFile = new JObject();
                            locationFile[HikeConstants.LATITUDE] = fileObject[HikeConstants.LATITUDE];
                            locationFile[HikeConstants.LONGITUDE] = fileObject[HikeConstants.LONGITUDE];
                            locationFile[HikeConstants.ZOOM_LEVEL] = fileObject[HikeConstants.ZOOM_LEVEL];
                            locationFile[HikeConstants.LOCATION_ADDRESS] = fileObject[HikeConstants.LOCATION_ADDRESS];
                            this.MetaDataString = locationFile.ToString(Newtonsoft.Json.Formatting.None);
                        }

                        if (contentType.ToString().Contains(HikeConstants.CONTACT))
                        {
                            this.MetaDataString = fileObject.ToString(Newtonsoft.Json.Formatting.None);
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
                        else if (this.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
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
            catch (Exception ex)
            {
                Debug.WriteLine("ConvMessage ::  ConvMessage constructor :  parse json , Exception : " + ex.StackTrace);
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
                    return string.Format(msg, string.Format(AppResources.NamingConvention_Txt, groupList[0].FirstName, groupList.Count - 1));
            }
        }

        public void SetAttachmentState(Attachment.AttachmentState attachmentState)
        {
            this.FileAttachment.FileState = attachmentState;
            if (FileAttachment.FileState == Attachment.AttachmentState.CANCELED || FileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED)
                ProgressBarValue = 0;
            NotifyPropertyChanged("ShowCancelMenu");
            NotifyPropertyChanged("ShowForwardMenu");
            NotifyPropertyChanged("ShowDeleteMenu");
            NotifyPropertyChanged("SdrImage");
        }

        public ConvMessage(ParticipantInfoState participantInfoState, JObject jsonObj)
        {
            string grpId;
            string from;
            GroupParticipant gp;
            this.MessageId = -1;
            this.participantInfoState = participantInfoState;
            this.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
            this.Timestamp = TimeUtils.getCurrentTimeStamp();
            switch (this.participantInfoState)
            {
                case ParticipantInfoState.INTERNATIONAL_USER:
                    this.Message = AppResources.SMS_INDIA;
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.STATUS_UPDATE:
                    JObject data = (JObject)jsonObj[HikeConstants.DATA];
                    JToken val;

                    // this is to handle profile pic update
                    if (data.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                        this.Message = AppResources.Update_Profile_Pic_txt;
                    else  // status , moods update
                    {
                        if (data.TryGetValue(HikeConstants.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                            this.Message = val.ToString();
                    }

                    data.Remove(HikeConstants.THUMBNAIL);
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.GROUP_NAME_CHANGE:
                    grpId = (string)jsonObj[HikeConstants.TO];
                    from = (string)jsonObj[HikeConstants.FROM];
                    string grpName = (string)jsonObj[HikeConstants.DATA];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == App.MSISDN)
                    {
                        this.Message = string.Format(AppResources.GroupNameChangedByGrpMember_Txt, AppResources.You_Txt, grpName);
                    }
                    else
                    {
                        gp = GroupManager.Instance.getGroupParticipant(null, from, grpId);
                        this.Message = string.Format(AppResources.GroupNameChangedByGrpMember_Txt, gp.Name, grpName);
                    }
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.GROUP_PIC_CHANGED:
                    grpId = (string)jsonObj[HikeConstants.TO];
                    from = (string)jsonObj[HikeConstants.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    gp = GroupManager.Instance.getGroupParticipant(null, from, grpId);
                    this.Message = string.Format(AppResources.GroupImgChangedByGrpMember_Txt, gp.Name);
                    jsonObj.Remove(HikeConstants.DATA);
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                default:
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
            }
        }

    }
}
