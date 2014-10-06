﻿﻿using System;
using System.Windows;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.Misc;
using windows_client.Languages;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.Phone.Controls;
using windows_client.Model.Sticker;
using CommonLibrary.Constants;

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

            if (obj.TryGetValue(HikeConstants.NavigationKeys.GC_PIN,out typeToken))
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
                    NotifyPropertyChanged("MessageId");
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
                    NotifyPropertyChanged("DispMessage");
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
                    NotifyPropertyChanged("SdrImage");
                    NotifyPropertyChanged("MessageStatus");
                    NotifyPropertyChanged("SendAsSMSVisibility");
                    NotifyPropertyChanged("MediaActionVisibility");
                    NotifyPropertyChanged("BubbleBackGroundColor");
                    NotifyPropertyChanged("MessageTextForeGround");
                    NotifyPropertyChanged("FileFailedImageVisibility");
                    if (_messageStatus == State.SENT_CONFIRMED || _messageStatus == State.SENT_SOCKET_WRITE)
                    {
                        SdrImageVisibility = Visibility.Visible;
                        NotifyPropertyChanged("SdrImageVisibility");
                    }
                    if (_messageStatus >= State.FORCE_SMS_SENT_CONFIRMED)
                        NotifyPropertyChanged("TimeStampStr");
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
                    NotifyPropertyChanging("GroupParticipant");
                    _groupParticipant = value;
                    NotifyPropertyChanged("GroupParticipant");
                    NotifyPropertyChanged("GroupMemberMsisdn");
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
                {
                    _fileAttachment = value;
                    NotifyPropertyChanged("SdrImage");
                }
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
                {
                    _isSms = value;
                    NotifyPropertyChanged("SendAsSMSVisibility");
                    NotifyPropertyChanged("BubbleBackGroundColor");
                    NotifyPropertyChanged("MessageTextForeGround");
                }
            }
        }

        public StickerObj StickerObj
        {
            set
            {
                if (value != null)
                {
                    _stickerObj = value;
                }
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

        public string GCPinMessageSenderName
        {
            get
            {
                if (this.IsSent)
                    return AppResources.You_Txt;

                if (this.GroupMemberName == null)
                    return this.GroupParticipant;
                else
                    return this.GroupMemberName;
            }
        }

        public string DirectTimeStampStr
        {
            get
            {
                return TimeUtils.getTimeStringForChatThread(_timestamp);
            }
        }

        public string TimeStampStr
        {
            get
            {
                if (participantInfoState == ParticipantInfoState.STATUS_UPDATE || participantInfoState == ParticipantInfoState.PIN_MESSAGE)
                    return TimeUtils.getRelativeTime(_timestamp);
                else
                {
                    if (MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED || MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ)
                        return String.Format(AppResources.Sent_As_SMS, TimeUtils.getTimeStringForChatThread(_timestamp));
                    else
                        return TimeUtils.getTimeStringForChatThread(_timestamp);
                }
            }
        }

        public BitmapImage TypingNotificationImage
        {
            get
            {
                return HikeInstantiation.ViewModel.SelectedBackground != null && !HikeInstantiation.ViewModel.SelectedBackground.IsLightTheme ? UI_Utils.Instance.TypingNotificationWhite : UI_Utils.Instance.TypingNotificationBlack;
            }
        }

        public string DispMessage
        {
            get
            {
                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
                    return GetContactName();
                else
                    return _message;
            }
        }

        public string FileName
        {
            get
            {
                return _fileAttachment != null ? _fileAttachment.FileName : string.Empty;
            }
        }

        public string FileType
        {
            get
            {
                return GetFileExtension();
            }
        }

        public BitmapImage UnknownFileTypeIconImage
        {
            get
            {
                if (_fileAttachment != null)
                {
                    if (!IsSent && _fileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                        return UI_Utils.Instance.DownloadIconUnknownFileType;
                    else
                        return UI_Utils.Instance.AttachmentIcon;
                }

                return null;
            }
        }

        public Visibility DispMessageVisibility
        {
            get { return String.IsNullOrEmpty(DispMessage) ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility NormalNudgeVisibility
        {
            get { return HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.ID == "20" ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility SpecialNudgeVisibility
        {
            get { return HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.ID == "20" ? Visibility.Visible : Visibility.Collapsed; }
        }

        public BitmapImage SpecialNudgeImage
        {
            get
            {
                return IsSent ? UI_Utils.Instance.HeartNudgeSent : UI_Utils.Instance.HeartNudgeReceived;
            }
        }

        bool _changingState;
        public bool ChangingState
        {
            get
            {
                return _changingState;
            }
            set
            {
                if (value != _changingState)
                {
                    _changingState = value;
                    NotifyPropertyChanged("PauseResumeImageOpacity");
                }
            }
        }

        public double PauseResumeImageOpacity
        {
            get
            {
                return ChangingState ? 0.6 : 1;
            }
        }

        public BitmapImage PauseResumeImage
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.PAUSED || FileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.ResumeFTRBlack;
                    else
                        return UI_Utils.Instance.ResumeFTRWhite;
                }
                else
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.PausedFTRBlack;
                    else
                        return UI_Utils.Instance.PausedFTRWhite;
                }
            }
        }

        public Visibility PauseResumeImageVisibility
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.PAUSED || FileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED || FileAttachment.FileState == Attachment.AttachmentState.STARTED)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public BitmapImage NudgeImage
        {
            get
            {
                if (IsSent)
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.BlueSentNudgeImage;
                    else
                        return UI_Utils.Instance.WhiteSentNudgeImage;
                }
                else
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.BlueReceivedNudgeImage;
                    else
                        return UI_Utils.Instance.WhiteReceivedNudgeImage;
                }
            }
        }

        public BitmapImage SdrImage
        {
            get
            {
                switch (_messageStatus)
                {
                    case ConvMessage.State.FORCE_SMS_SENT_CONFIRMED:
                    case ConvMessage.State.SENT_CONFIRMED:
                    case ConvMessage.State.SENT_SOCKET_WRITE:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Sent_ChatTheme;
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                                return UI_Utils.Instance.Sent;
                            else
                                return UI_Utils.Instance.Sent_ChatTheme;
                        }
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED:
                    case ConvMessage.State.SENT_DELIVERED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Delivered_ChatTheme;
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                                return UI_Utils.Instance.Delivered;
                            else
                                return UI_Utils.Instance.Delivered_ChatTheme;
                        }
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ:
                    case ConvMessage.State.SENT_DELIVERED_READ:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Read_ChatTheme;
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                                return UI_Utils.Instance.Read;
                            else
                                return UI_Utils.Instance.Read_ChatTheme;
                        }
                    case ConvMessage.State.SENT_UNCONFIRMED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Trying_ChatTheme;
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                                return UI_Utils.Instance.Trying;
                            else
                                return UI_Utils.Instance.Trying_ChatTheme;
                        }
                    default:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Trying_ChatTheme;
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                                return UI_Utils.Instance.Trying;
                            else
                                return UI_Utils.Instance.Trying_ChatTheme;
                        }
                }
            }
        }

        Visibility _sdrImageVisibility = Visibility.Visible;
        public Visibility SdrImageVisibility
        {
            get
            {
                return FileFailedImageVisibility == Visibility.Visible ? Visibility.Collapsed : _sdrImageVisibility;
            }
            set
            {
                if (value != _sdrImageVisibility)
                {
                    _sdrImageVisibility = value;
                    NotifyPropertyChanged("SdrImageVisibility");
                }
            }
        }

        public BitmapImage FileFailedImage
        {
            get
            {
                if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                    return UI_Utils.Instance.HttpFailed;
                else
                    return UI_Utils.Instance.HttpFailed_ChatTheme;
            }
        }

        public Visibility FileFailedImageVisibility
        {
            get
            {
                return FileAttachment != null && MessageStatus == State.SENT_FAILED &&
                    (FileAttachment.FileState == Attachment.AttachmentState.FAILED
                    || FileAttachment.FileState == Attachment.AttachmentState.CANCELED) ?
                    Visibility.Visible : Visibility.Collapsed;
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
                {
                    _isGroup = value;
                    NotifyPropertyChanged("DirectMessageVisibility");
                }
            }
        }

        public Visibility DirectMessageVisibility
        {
            get
            {
                return IsGroup ? Visibility.Visible : Visibility.Collapsed;
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
                NotifyPropertyChanged("MessageBubbleWidth");
                NotifyPropertyChanged("MessageBubbleMinWidth");
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
                NotifyPropertyChanged("ShowForwardMenu");
                NotifyPropertyChanged("IsStickerVisible");
                NotifyPropertyChanged("IsStickerLoading");
                NotifyPropertyChanged("IsHttpFailed");
                if (StickerObj != null)
                {
                    StickerObj.NotifyPropertyChanged("StickerImage");
                }
            }
        }

        public Visibility PlayIconVisibility
        {
            get
            {
                if (_fileAttachment != null && (_fileAttachment.FileState != Attachment.AttachmentState.COMPLETED || _fileAttachment.ContentType.Contains(FTBasedConstants.VIDEO) || _fileAttachment.ContentType.Contains(FTBasedConstants.AUDIO)))
                {
                    if ((IsSent && _fileAttachment.FileState != Attachment.AttachmentState.COMPLETED && _fileAttachment.FileState != Attachment.AttachmentState.FAILED)
                        || _fileAttachment.FileState == Attachment.AttachmentState.STARTED || _fileAttachment.FileState == Attachment.AttachmentState.PAUSED
                        || _fileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
                        return Visibility.Collapsed;
                    return Visibility.Visible;
                }
                else
                    return Visibility.Collapsed;
            }
        }

        public BitmapImage PlayIconImage
        {
            get
            {
                if (_fileAttachment != null)
                {
                    if (_fileAttachment.FileState == Attachment.AttachmentState.FAILED)
                    {
                        if (_fileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                            return UI_Utils.Instance.RetryAudioIcon;
                        else
                            return UI_Utils.Instance.RetryIcon;
                    }
                    if (!IsSent && (_fileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED || _fileAttachment.FileState == Attachment.AttachmentState.CANCELED))
                    {
                        if (_fileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                            return UI_Utils.Instance.DownloadAudioIcon;
                        else
                            return UI_Utils.Instance.DownloadIcon;
                    }
                    else if (_fileAttachment.FileState == Attachment.AttachmentState.STARTED || _fileAttachment.FileState == Attachment.AttachmentState.PAUSED || _fileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED || _fileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED)
                        return UI_Utils.Instance.BlankBitmapImage;
                    else if (_fileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                    {
                        if (IsPlaying)
                            return UI_Utils.Instance.PauseIcon;
                        else
                            return UI_Utils.Instance.PlayAudioIcon;
                    }
                    else
                        return UI_Utils.Instance.PlayIcon;
                }

                return null;
            }
        }

        Boolean _isPlaying = false;
        public Boolean IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set
            {
                _isPlaying = value;
                NotifyPropertyChanged("PlayIconImage");
            }
        }

        Boolean _isStopped = true;
        public Boolean IsStopped
        {
            get
            {
                return _isStopped;
            }
            set
            {
                if (_isStopped != value)
                {
                    _isStopped = value;
                    NotifyPropertyChanged("PlayTimeText");
                }
            }
        }

        public Visibility LocationAddressVisibility
        {
            get
            {
                return String.IsNullOrEmpty(Address) && String.IsNullOrEmpty(PlaceName) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility AddressVisibility
        {
            get
            {
                return String.IsNullOrEmpty(Address) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public Visibility PlaceNameVisibility
        {
            get
            {
                return String.IsNullOrEmpty(PlaceName) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        string _address;
        public String Address
        {
            get
            {
                if (_address == null)
                    _address = getAddressFromMetaData();

                return _address;
            }
        }

        string _placeName;
        public String PlaceName
        {
            get
            {
                if (_placeName == null)
                    _placeName = getPlaceNameFromMetaData();

                return _placeName;
            }
        }

        public String DurationText
        {
            get
            {
                return getTimeTextFromMetaData();
            }
        }

        string _playTimeText;
        public String PlayTimeText
        {
            get
            {
                if (IsStopped)
                    return DurationText;
                else
                    return _playTimeText;
            }
            set
            {
                if (_playTimeText != value)
                {
                    _playTimeText = value;
                    NotifyPropertyChanged("PlayTimeText");
                }
            }
        }

        double _playProgressBarValue = 0;
        public double PlayProgressBarValue
        {
            set
            {
                _playProgressBarValue = value;
                if (_playProgressBarValue >= 100)
                {
                    IsPlaying = false;
                    _playProgressBarValue = 0;
                }

                Debug.WriteLine(_playProgressBarValue);

                NotifyPropertyChanged("PlayProgressBarValue");
            }
            get
            {
                return _playProgressBarValue;
            }
        }

        double _progressBarValue = 0;
        public double ProgressBarValue
        {
            set
            {
                _progressBarValue = value;
                if (_progressBarValue >= 100)
                {
                    SdrImageVisibility = Visibility.Visible;
                    NotifyPropertyChanged("SdrImageVisibility");
                    NotifyPropertyChanged("FileFailedImageVisibility");
                }
                NotifyPropertyChanging("PlayIconVisibility");
                NotifyPropertyChanging("PlayIconImage");
                NotifyPropertyChanged("ProgressBarVisibility");
                NotifyPropertyChanged("FileSizeVisibility");
                NotifyPropertyChanged("ProgressBarValue");
                NotifyPropertyChanged("ProgressText");
                NotifyPropertyChanged("UnknownFileTypeIconImage");
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
                if (FileAttachment != null)
                {
                    if (FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION) || FileAttachment.ContentType.Contains(FTBasedConstants.CONTACT))
                    {
                        if (_progressBarValue <= 0 || _progressBarValue >= 100)
                            return Visibility.Collapsed;
                        else
                            return Visibility.Visible;
                    }
                    else
                    {
                        if ((_progressBarValue <= 0 || _progressBarValue >= 100) && FileAttachment.FileSize <= 0)
                        {
                            return Visibility.Collapsed;
                        }
                        else
                            return Visibility.Visible;
                    }
                }
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility FileSizeVisibility
        {
            get
            {
                if (FileAttachment != null)
                {
                    if (FileAttachment.FileSize <= 0 || FileAttachment.FileState == Attachment.AttachmentState.COMPLETED)
                        return Visibility.Collapsed;
                    else
                        return Visibility.Visible;
                }
                else
                    return Visibility.Collapsed;
            }
        }

        public BitmapImage NotificationImage
        {
            get
            {
                switch (_notificationType)
                {
                    case MessageType.HIKE_PARTICIPANT_JOINED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.SMS_PARTICIPANT_INVITED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.NotOnHikeImage;
                        else
                            return UI_Utils.Instance.NotOnHikeImage_ChatTheme;

                    case MessageType.SMS_PARTICIPANT_OPTED_IN:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.ChatAcceptedImage;
                        else
                            return UI_Utils.Instance.ChatAcceptedImage_ChatTheme;

                    case MessageType.USER_JOINED_HIKE:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.PARTICIPANT_LEFT:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.ParticipantLeft;
                        else
                            return UI_Utils.Instance.ParticipantLeft_ChatTheme;

                    case MessageType.GROUP_END:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.ParticipantLeft;
                        else
                            return UI_Utils.Instance.ParticipantLeft_ChatTheme;

                    case MessageType.WAITING:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Waiting;
                        else
                            return UI_Utils.Instance.Waiting_ChatTheme;

                    case MessageType.REWARD:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.Reward;
                        else
                            return UI_Utils.Instance.Reward_ChatTheme;

                    case MessageType.INTERNATIONAL_USER_BLOCKED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.IntUserBlocked;
                        else
                            return UI_Utils.Instance.IntUserBlocked_ChatTheme;

                    case MessageType.PIC_UPDATE:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.GROUP_NAME_CHANGED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.GrpNameChanged;
                        else
                            return UI_Utils.Instance.GrpNameChanged_ChatTheme;

                    case MessageType.GROUP_PIC_CHANGED:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.GrpPicChanged;
                        else
                            return UI_Utils.Instance.GrpPicChanged_ChatTheme;

                    case MessageType.CHAT_BACKGROUND:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.ChatBackgroundChanged;
                        else
                            return UI_Utils.Instance.ChatBackgroundChanged_ChatTheme;

                    case MessageType.TEXT_UPDATE:
                    default:
                        if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;
                }
            }
        }

        public BitmapImage StatusUpdateImage
        {
            get
            {
                if (_statusUpdateImage != null)
                    return _statusUpdateImage;
                else
                    return MoodsInitialiser.Instance.GetMoodImageForMoodId(MoodsInitialiser.GetMoodId(metadataJsonString));
            }
            set
            {
                _statusUpdateImage = value;
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
                if (!string.IsNullOrEmpty(metadataJsonString) && metadataJsonString.Contains(HikeConstants.STICKER_ID))
                {
                    if (_stickerObj != null && _stickerObj.IsStickerDownloaded && !imageDownloadFailed)
                        return Visibility.Visible;
                    else
                        return Visibility.Collapsed;
                }
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

        public Visibility IsStickerVisible
        {
            get
            {
                if (StickerObj != null && StickerObj.IsStickerDownloaded)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility IsStickerLoading
        {
            get
            {
                if (StickerObj != null && !StickerObj.IsStickerDownloaded && !imageDownloadFailed)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility IsHttpFailed
        {
            get
            {
                if (StickerObj != null && !StickerObj.IsStickerDownloaded && imageDownloadFailed)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
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
                {
                    _isInAddressBook = value;
                    NotifyPropertyChanged("GroupMemberMsisdn");
                }
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
                NotifyPropertyChanged("GroupMemberName");
                NotifyPropertyChanged("GCPinMessageSenderName");
                IsGroup = true;
            }
        }

        public string GroupMemberMsisdn
        {
            get
            {
                return !IsGroup || IsInAddressBook || _groupParticipant.Contains(GroupMemberName) ? String.Empty : "(" + _groupParticipant + ") ";
            }
        }

        public String FileSize
        {
            get
            {
                if (FileAttachment != null && FileAttachment.FileSize != 0)
                    return Utils.ConvertToStorageSizeString(FileAttachment.FileSize);
                else
                    return String.Empty;
            }
        }

        string getPlaceNameFromMetaData()
        {
            if (String.IsNullOrEmpty(this.MetaDataString))
                return String.Empty;

            try
            {
                if (IsSent)
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    JArray files = metadataObject[ServerJsonKeys.FILES_DATA].ToObject<JArray>();
                    JObject fileObject = files[0].ToObject<JObject>();
                    return (String)fileObject[ServerJsonKeys.LOCATION_TITLE];
                }
                else
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    return (String)metadataObject[ServerJsonKeys.LOCATION_TITLE];
                }
            }
            catch
            {
                return String.Empty;
            }
        }

        string getAddressFromMetaData()
        {
            if (String.IsNullOrEmpty(this.MetaDataString))
                return String.Empty;

            try
            {
                if (IsSent)
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    JArray files = metadataObject[ServerJsonKeys.FILES_DATA].ToObject<JArray>();
                    JObject fileObject = files[0].ToObject<JObject>();
                    return (String)fileObject[ServerJsonKeys.LOCATION_ADDRESS];
                }
                else
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    return (String)metadataObject[ServerJsonKeys.LOCATION_ADDRESS];
                }
            }
            catch
            {
                return String.Empty;
            }
        }

        string getTimeTextFromMetaData()
        {
            if (String.IsNullOrEmpty(this.MetaDataString))
                return String.Empty;

            try
            {
                var timeObj = JObject.Parse(this.MetaDataString);
                var seconds = Convert.ToInt64(timeObj[ServerJsonKeys.FILE_PLAY_TIME].ToString());
                var durationTimeSpan = TimeSpan.FromSeconds(seconds);
                return durationTimeSpan.ToString("mm\\:ss");
            }
            catch
            {
                return String.Empty;
            }
        }

        public int MessageBubbleWidth
        {
            get
            {
                if ((_currentOrientation & PageOrientation.Landscape) == PageOrientation.Landscape)
                {
                    return HikeConstants.CHATBUBBLE_LANDSCAPE_WIDTH;
                }
                else if ((_currentOrientation & PageOrientation.Portrait) == PageOrientation.Portrait)
                {
                    return HikeConstants.CHATBUBBLE_PORTRAIT_WIDTH;
                }
                return HikeConstants.CHATBUBBLE_PORTRAIT_WIDTH;
            }
        }

        public int MessageBubbleMinWidth
        {
            get
            {
                if ((_currentOrientation & PageOrientation.Landscape) == PageOrientation.Landscape)
                {
                    return HikeConstants.CHATBUBBLE_LANDSCAPE_MINWIDTH;
                }
                else if ((_currentOrientation & PageOrientation.Portrait) == PageOrientation.Portrait)
                {
                    return HikeConstants.CHATBUBBLE_PORTRAIT_MINWIDTH;
                }
                return HikeConstants.CHATBUBBLE_PORTRAIT_MINWIDTH;
            }
        }

        public SolidColorBrush BorderBackgroundColor
        {
            get
            {
                if (HikeInstantiation.ViewModel.SelectedBackground != null)
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.Transparent;
                    else
                        return HikeInstantiation.ViewModel.SelectedBackground.HeaderBackground;
                }
                else
                    return UI_Utils.Instance.Black;
            }
        }

        public SolidColorBrush NotificationBorderBrush
        {
            get
            {
                if (HikeInstantiation.ViewModel.SelectedBackground != null)
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                        return UI_Utils.Instance.Black;
                    else
                        return UI_Utils.Instance.White;
                }
                else
                    return UI_Utils.Instance.Black;
            }
        }

        public SolidColorBrush BubbleBackGroundColor
        {
            get
            {
                if (HikeInstantiation.ViewModel.SelectedBackground != null)
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                    {
                        if (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE) || StickerObj != null)
                            return UI_Utils.Instance.LightGray;
                        else if (IsSent)
                        {
                            if (IsSms)
                                return UI_Utils.Instance.SmsBackground;
                            else
                                return UI_Utils.Instance.HikeMsgBackground;
                        }
                        else
                            return UI_Utils.Instance.ReceivedChatBubbleColor;
                    }
                    else
                    {
                        if (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE) || StickerObj != null)
                            return HikeInstantiation.ViewModel.SelectedBackground.HeaderBackground;
                        else if (IsSent)
                            return HikeInstantiation.ViewModel.SelectedBackground.SentBubbleBgColor;
                        else
                            return HikeInstantiation.ViewModel.SelectedBackground.ReceivedBubbleBgColor;
                    }
                }
                else
                    return UI_Utils.Instance.White;
            }
        }

        public SolidColorBrush ChatForegroundColor
        {
            get
            {
                return HikeInstantiation.ViewModel.SelectedBackground != null ? HikeInstantiation.ViewModel.SelectedBackground.ForegroundColor : UI_Utils.Instance.White;
            }
        }

        public SolidColorBrush BubbleForegroundColor
        {
            get
            {
                return HikeInstantiation.ViewModel.SelectedBackground != null ? HikeInstantiation.ViewModel.SelectedBackground.BubbleForegroundColor : UI_Utils.Instance.White;
            }
        }

        public SolidColorBrush MessageTextForeGround
        {
            get
            {
                if (GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION
                    || GrpParticipantState == ConvMessage.ParticipantInfoState.MESSAGE_STATUS
                    || GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE
                    || GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                    return ChatForegroundColor;
                else
                {
                    if (HikeInstantiation.ViewModel.SelectedBackground != null && HikeInstantiation.ViewModel.SelectedBackground.IsDefault && !HikeInstantiation.ViewModel.IsDarkMode)
                    {
                        if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                            return UI_Utils.Instance.Black;
                        else
                            return BubbleForegroundColor;
                    }
                    else
                    {
                        if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(FTBasedConstants.POKE)))
                            return UI_Utils.Instance.White;
                        else
                            return BubbleForegroundColor;
                    }
                }
            }
        }

        public void UpdateChatBubbles()
        {
            NotifyPropertyChanged("MessageTextForeGround");
            NotifyPropertyChanged("PauseResumeImage");
            NotifyPropertyChanged("ChatForegroundColor");
            NotifyPropertyChanged("BorderBackgroundColor");
            NotifyPropertyChanged("BubbleBackGroundColor");
            NotifyPropertyChanged("SdrImage");
            NotifyPropertyChanged("NotificationImage");
            NotifyPropertyChanged("NudgeImage");
            NotifyPropertyChanged("SpecialNudgeVisibility");
            NotifyPropertyChanged("NormalNudgeVisibility");
            NotifyPropertyChanged("FileFailedImage");
            NotifyPropertyChanged("TypingNotificationImage");
            NotifyPropertyChanged("NotificationBorderBrush");
        }

        public Visibility SendAsSMSVisibility
        {
            get
            {
                //not enabling send as sms on socket write
                if (IsSent && !IsSms && MessageStatus == State.SENT_CONFIRMED && HikeInstantiation.NewChatThreadPageObj != null && HikeInstantiation.NewChatThreadPageObj.IsSMSOptionValid)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public Visibility MediaActionVisibility
        {
            get
            {
                if (MessageStatus == State.SENT_UNCONFIRMED || MessageStatus == State.SENT_FAILED || MessageStatus == State.SENT_SOCKET_WRITE || MessageStatus == State.SENT_CONFIRMED)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
        }

        public bool UserTappedDownload
        {
            get;
            set;
        }

        public string ProgressText
        {
            get;
            set;
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
            else if (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.NavigationKeys.GC_PIN))
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

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        // Used to notify that a property changed
        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null && !string.IsNullOrEmpty(propertyName))
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


        string GetContactName()
        {
            string name = null;
            if (!string.IsNullOrEmpty(metadataJsonString))
            {
                JObject contactInfo = JObject.Parse(metadataJsonString);
                JToken jt;
                if (contactInfo.TryGetValue(ServerJsonKeys.CS_NAME, out jt) && jt != null)
                    name = jt.ToString();
            }

            return name ?? (string.IsNullOrEmpty(_fileAttachment.FileName) ? AppResources.ContactTransfer_Text : _fileAttachment.FileName);
        }
        string GetFileExtension()
        {
            if (_fileAttachment != null && !string.IsNullOrEmpty(_fileAttachment.FileName))
            {
                int index = _fileAttachment.FileName.LastIndexOf('.');
                if (index > -1 && index < _fileAttachment.FileName.Length - 1)//so last char is not dot
                {
                    return _fileAttachment.FileName.Substring(index + 1).ToUpper();
                }
            }
            return "FILE";//in case no extension type and no need to translate this
        }

        public void SetAttachmentState(Attachment.AttachmentState attachmentState)
        {
            this.FileAttachment.FileState = attachmentState;
            if (FileAttachment.FileState == Attachment.AttachmentState.CANCELED || FileAttachment.FileState == Attachment.AttachmentState.FAILED || FileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED)
                ProgressBarValue = 0;
            NotifyPropertyChanged("ShowCancelMenu");
            NotifyPropertyChanged("ShowForwardMenu");
            NotifyPropertyChanged("ShowDeleteMenu");
            NotifyPropertyChanged("PauseResumeImage");
            NotifyPropertyChanged("PauseResumeImageVisibility");
            NotifyPropertyChanged("SdrImage");
            NotifyPropertyChanged("FileFailedImage");
            NotifyPropertyChanged("PlayIconVisibility");
            NotifyPropertyChanged("PlayIconImage");
            NotifyPropertyChanged("FileSizeVisibility");
            NotifyPropertyChanged("UnknownFileTypeIconImage");

            SdrImageVisibility = attachmentState != Attachment.AttachmentState.NOT_STARTED
                && attachmentState != Attachment.AttachmentState.STARTED
                && attachmentState != Attachment.AttachmentState.PAUSED
                && attachmentState != Attachment.AttachmentState.MANUAL_PAUSED
                && MessageStatus != State.SENT_FAILED
                ? Visibility.Visible : Visibility.Collapsed;

            NotifyPropertyChanged("SdrImageVisibility");
            NotifyPropertyChanged("FileFailedImageVisibility");

            ChangingState = false;
        }

        public ConvMessage(ParticipantInfoState participantInfoState, JObject jsonObj, long timeStamp = 0)
        {
            string grpId;
            string from;
            GroupParticipant gp;
            this.MessageId = -1;
            this.participantInfoState = participantInfoState;
            this.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
            this.Timestamp = timeStamp == 0 ? TimeUtils.getCurrentTimeStamp() : timeStamp;

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
