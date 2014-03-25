﻿using System;
using System.Windows;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using Microsoft.Phone.Data.Linq.Mapping;
using System.Data.Linq;
using windows_client.utils;
using Newtonsoft.Json.Linq;
using System.Windows.Media.Imaging;
using System.Text;
using System.Linq;
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
        private Sticker _stickerObj;
        // private bool _hasFileAttachment = false;
        private bool _hasAttachment = false;
        private string _readByInfo;

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
            H2H_OFFLINE_IN_APP_TIP,
            CHAT_BACKGROUND_CHANGED,
            CHAT_BACKGROUND_CHANGE_NOT_SUPPORTED,
            MESSAGE_STATUS
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
                return ParticipantInfoState.STATUS_UPDATE;
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_END == type)
                return ParticipantInfoState.GROUP_END;
            else if (HikeConstants.MqttMessageTypes.GROUP_USER_JOINED_OR_WAITING == type)
                return ParticipantInfoState.GROUP_JOINED_OR_WAITING;
            else if (HikeConstants.MqttMessageTypes.USER_OPT_IN == type)
                return ParticipantInfoState.USER_OPT_IN;
            else if (HikeConstants.MqttMessageTypes.USER_JOIN == type)
            {
                bool isRejoin = false;
                JToken subtype;
                if (obj.TryGetValue(HikeConstants.SUB_TYPE, out subtype))
                {
                    isRejoin = HikeConstants.SUBTYPE_REJOIN == (string)subtype;
                }
                return isRejoin ? ParticipantInfoState.USER_REJOINED : ParticipantInfoState.USER_JOINED;
            }
            else if (HikeConstants.MqttMessageTypes.HIKE_USER == type)
                return ParticipantInfoState.HIKE_USER;
            else if (HikeConstants.MqttMessageTypes.SMS_USER == type)
                return ParticipantInfoState.SMS_USER;
            else if ("credits_gained" == type)
                return ParticipantInfoState.CREDITS_GAINED;
            else if (HikeConstants.MqttMessageTypes.BLOCK_INTERNATIONAL_USER == type)
                return ParticipantInfoState.INTERNATIONAL_USER;
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_NAME == type)
                return ParticipantInfoState.GROUP_NAME_CHANGE;
            else if (HikeConstants.MqttMessageTypes.DND_USER_IN_GROUP == type)
                return ParticipantInfoState.DND_USER;
            else if (HikeConstants.MqttMessageTypes.GROUP_DISPLAY_PIC == type)
                return ParticipantInfoState.GROUP_PIC_CHANGED;
            else if (HikeConstants.MqttMessageTypes.CHAT_BACKGROUNDS == type)
                return ParticipantInfoState.CHAT_BACKGROUND_CHANGED;
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
                    NotifyPropertyChanged("BubbleBackGroundColor");
                    NotifyPropertyChanged("MessageTextForeGround");
                    NotifyPropertyChanged("FileFailedImageVisibility");
                    if (_messageStatus == State.SENT_CONFIRMED)
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

        [Column(CanBeNull = true)]
        public string ReadByInfo
        {
            get
            {
                return _readByInfo;
            }
            set
            {
                if (_readByInfo != value)
                {
                    NotifyPropertyChanging("ReadByInfo");
                    _readByInfo = value;
                    NotifyPropertyChanged("ReadByInfo");
                }
            }
        }

        JArray _readByArray;
        public JArray ReadByArray
        {
            get
            {
                if (_readByArray == null)
                {
                    if (String.IsNullOrEmpty(_readByInfo))
                        return null;
                    else
                        _readByArray = JArray.Parse(_readByInfo);
                }

                return _readByArray;
            }
            set
            {
                if (value != _readByArray)
                    _readByArray = value;
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
                {
                    _fileAttachment = value;
                    NotifyPropertyChanged("SdrImage");
                }
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
                return _isSms || MessageStatus >= State.FORCE_SMS_SENT_CONFIRMED;
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

        public Sticker StickerObj
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

        public string TimeStampStr
        {
            get
            {
                if (participantInfoState == ParticipantInfoState.STATUS_UPDATE)
                    return TimeUtils.getRelativeTime(_timestamp);
                else
                {
                    if (MessageStatus >= State.FORCE_SMS_SENT_CONFIRMED)
                        return String.Format(AppResources.Sent_As_SMS, TimeUtils.getTimeStringForChatThread(_timestamp));
                    else
                        return TimeUtils.getTimeStringForChatThread(_timestamp);
                }
            }
        }

        public BitmapImage CloseImage
        {
            get
            {
                return UI_Utils.Instance.CloseButtonWhiteImage;
            }
        }

        public string DispMessage
        {
            get
            {
                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                    return string.IsNullOrEmpty(_fileAttachment.FileName) ? "contact" : _fileAttachment.FileName;
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
                        return UI_Utils.Instance.DownloadIconBigger;
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
            get { return App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.ID == "20" ? Visibility.Collapsed : Visibility.Visible; }
        }

        public Visibility SpecialNudgeVisibility
        {
            get { return App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.ID == "20" ? Visibility.Visible : Visibility.Collapsed; }
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
                return ChangingState ? 0.4 : 1;
            }
        }

        public BitmapImage PauseResumeImage
        {
            get
            {
                if (FileAttachment.FileState == Attachment.AttachmentState.PAUSED || FileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
                {
                    if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        return UI_Utils.Instance.ResumeFTRBlack;
                    else
                        return UI_Utils.Instance.ResumeFTRWhite;
                }
                else
                {
                    if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
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
                    if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        return UI_Utils.Instance.BlueSentNudgeImage;
                    else
                        return UI_Utils.Instance.WhiteSentNudgeImage;
                }
                else
                {
                    if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
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
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Sent_ChatTheme;
                            else
                                return UI_Utils.Instance.Sent;
                        }
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Sent;
                            else
                                return UI_Utils.Instance.Sent_ChatTheme;
                        }
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED:
                    case ConvMessage.State.SENT_DELIVERED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Delivered_ChatTheme;
                            else
                                return UI_Utils.Instance.Delivered;
                        }
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Delivered;
                            else
                                return UI_Utils.Instance.Delivered_ChatTheme;
                        }
                    case ConvMessage.State.FORCE_SMS_SENT_DELIVERED_READ:
                    case ConvMessage.State.SENT_DELIVERED_READ:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Read_ChatTheme;
                            else
                                return UI_Utils.Instance.Read;
                        }
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Read;
                            else
                                return UI_Utils.Instance.Read_ChatTheme;
                        }
                    case ConvMessage.State.SENT_UNCONFIRMED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Trying_ChatTheme;
                            else
                                return UI_Utils.Instance.Trying;
                        }
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Trying;
                            else
                                return UI_Utils.Instance.Trying_ChatTheme;
                        }
                    default:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                                return UI_Utils.Instance.Trying_ChatTheme;
                            else
                                return UI_Utils.Instance.Trying;
                        }
                        else
                        {
                            if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
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
                if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                    return UI_Utils.Instance.HttpFailed;
                else
                    return UI_Utils.Instance.HttpFailed_ChatTheme;
            }
        }

        public Visibility FileFailedImageVisibility
        {
            get
            {
                return FileAttachment != null && FileAttachment.FileState != Attachment.AttachmentState.STARTED
                && FileAttachment.FileState != Attachment.AttachmentState.PAUSED
                && FileAttachment.FileState != Attachment.AttachmentState.MANUAL_PAUSED
                && MessageStatus == State.SENT_FAILED ? Visibility.Visible : Visibility.Collapsed;
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
            }
        }

        private bool imageDownloadFailed = false;
        public BitmapImage MessageImage
        {
            get
            {
                if (_stickerObj != null)
                {
                    return _stickerObj.StickerImage;
                }

                if (_fileAttachment != null && _fileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                {
                    return UI_Utils.Instance.BlackContactIcon;
                }
                else if (_fileAttachment != null && _fileAttachment.Thumbnail != null)
                {
                    return UI_Utils.Instance.createImageFromBytes(_fileAttachment.Thumbnail);
                }
                return null;
            }
        }

        public bool ImageDownloadFailed
        {
            get
            {
                return imageDownloadFailed;
            }
            set
            {
                imageDownloadFailed = value;
                NotifyPropertyChanged("FileSize");
                NotifyPropertyChanged("MessageImage");
                NotifyPropertyChanged("ShowForwardMenu");
                NotifyPropertyChanged("IsStickerVisible");
                NotifyPropertyChanged("IsStickerLoading");
                NotifyPropertyChanged("IsHttpFailed");
            }
        }

        public Visibility PlayIconVisibility
        {
            get
            {
                if (_fileAttachment != null && (_fileAttachment.FileState != Attachment.AttachmentState.COMPLETED || _fileAttachment.ContentType.Contains(HikeConstants.VIDEO) || _fileAttachment.ContentType.Contains(HikeConstants.AUDIO)))
                {
                    if ((IsSent && _fileAttachment.FileState != Attachment.AttachmentState.COMPLETED) || _fileAttachment.FileState == Attachment.AttachmentState.STARTED || _fileAttachment.FileState == Attachment.AttachmentState.PAUSED || _fileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED)
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
                    if (!IsSent && (_fileAttachment.FileState == Attachment.AttachmentState.FAILED || _fileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED || _fileAttachment.FileState == Attachment.AttachmentState.CANCELED))
                        return UI_Utils.Instance.DownloadIcon;
                    else if (_fileAttachment.FileState == Attachment.AttachmentState.STARTED || _fileAttachment.FileState == Attachment.AttachmentState.PAUSED || _fileAttachment.FileState == Attachment.AttachmentState.MANUAL_PAUSED || _fileAttachment.FileState == Attachment.AttachmentState.NOT_STARTED)
                        return UI_Utils.Instance.BlankBitmapImage;
                    else if (_fileAttachment.ContentType.Contains(HikeConstants.AUDIO) && IsPlaying)
                        return UI_Utils.Instance.PauseIcon;
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
                    if (FileAttachment.ContentType.Contains(HikeConstants.LOCATION) || FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
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
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.SMS_PARTICIPANT_INVITED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.NotOnHikeImage;
                        else
                            return UI_Utils.Instance.NotOnHikeImage_ChatTheme;

                    case MessageType.SMS_PARTICIPANT_OPTED_IN:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.ChatAcceptedImage;
                        else
                            return UI_Utils.Instance.ChatAcceptedImage_ChatTheme;

                    case MessageType.USER_JOINED_HIKE:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.PARTICIPANT_LEFT:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.ParticipantLeft;
                        else
                            return UI_Utils.Instance.ParticipantLeft_ChatTheme;

                    case MessageType.GROUP_END:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.ParticipantLeft;
                        else
                            return UI_Utils.Instance.ParticipantLeft_ChatTheme;

                    case MessageType.WAITING:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.Waiting;
                        else
                            return UI_Utils.Instance.Waiting_ChatTheme;

                    case MessageType.REWARD:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.Reward;
                        else
                            return UI_Utils.Instance.Reward_ChatTheme;

                    case MessageType.INTERNATIONAL_USER_BLOCKED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.IntUserBlocked;
                        else
                            return UI_Utils.Instance.IntUserBlocked_ChatTheme;

                    case MessageType.PIC_UPDATE:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;

                    case MessageType.GROUP_NAME_CHANGED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.GrpNameChanged;
                        else
                            return UI_Utils.Instance.GrpNameChanged_ChatTheme;

                    case MessageType.GROUP_PIC_CHANGED:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.GrpPicChanged;
                        else
                            return UI_Utils.Instance.GrpPicChanged_ChatTheme;

                    case MessageType.CHAT_BACKGROUND:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.ChatBackgroundChanged;
                        else
                            return UI_Utils.Instance.ChatBackgroundChanged_ChatTheme;

                    case MessageType.TEXT_UPDATE:
                    default:
                        if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                            return UI_Utils.Instance.OnHikeImage;
                        else
                            return UI_Utils.Instance.OnHikeImage_ChatTheme;
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
                    JArray files = metadataObject[HikeConstants.FILES_DATA].ToObject<JArray>();
                    JObject fileObject = files[0].ToObject<JObject>();
                    return (String)fileObject[HikeConstants.LOCATION_TITLE];
                }
                else
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    return (String)metadataObject[HikeConstants.LOCATION_TITLE];
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
                    JArray files = metadataObject[HikeConstants.FILES_DATA].ToObject<JArray>();
                    JObject fileObject = files[0].ToObject<JObject>();
                    return (String)fileObject[HikeConstants.LOCATION_ADDRESS];
                }
                else
                {
                    var metadataObject = JObject.Parse(MetaDataString);
                    return (String)metadataObject[HikeConstants.LOCATION_ADDRESS];
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
                return "";

            try
            {
                var timeObj = JObject.Parse(this.MetaDataString);
                var seconds = Convert.ToInt64(timeObj[HikeConstants.FILE_PLAY_TIME].ToString());
                var durationTimeSpan = TimeSpan.FromSeconds(seconds);
                return durationTimeSpan.ToString("mm\\:ss");
            }
            catch
            {
                return "";
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

        public SolidColorBrush BorderBackgroundColor
        {
            get
            {
                if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                    return UI_Utils.Instance.Transparent;
                else
                    return UI_Utils.Instance.Black;
            }
        }

        public SolidColorBrush BubbleBackGroundColor
        {
            get
            {
                if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                {
                    if (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE) || StickerObj != null)
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
                    if (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE) || StickerObj != null)
                        return UI_Utils.Instance.Black40Opacity;
                    else if (IsSent)
                        return App.ViewModel.SelectedBackground != null ? App.ViewModel.SelectedBackground.SentBubbleBgColor : UI_Utils.Instance.White;
                    else
                        return App.ViewModel.SelectedBackground != null ? App.ViewModel.SelectedBackground.ReceivedBubbleBgColor : UI_Utils.Instance.White;
                }
            }
        }

        public SolidColorBrush ChatForegroundColor
        {
            get
            {
                return App.ViewModel.SelectedBackground != null ? App.ViewModel.SelectedBackground.ForegroundColor : UI_Utils.Instance.White;
            }
        }

        public SolidColorBrush BubbleForegroundColor
        {
            get
            {
                return App.ViewModel.SelectedBackground != null ? App.ViewModel.SelectedBackground.BubbleForegroundColor : UI_Utils.Instance.White;
            }
        }

        public SolidColorBrush MessageTextForeGround
        {
            get
            {
                if (GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION
                    || GrpParticipantState == ConvMessage.ParticipantInfoState.MESSAGE_STATUS
                    || GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                    return ChatForegroundColor;
                else
                {
                    if (App.ViewModel.SelectedBackground != null && App.ViewModel.SelectedBackground.IsDefault)
                    {
                        if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
                            return UI_Utils.Instance.Black;
                        else if (IsSent)
                            return UI_Utils.Instance.White;
                        else
                            return UI_Utils.Instance.ReceiveMessageForeground;
                    }
                    else
                    {
                        if (StickerObj != null || (this.MetaDataString != null && this.MetaDataString.Contains(HikeConstants.POKE)))
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
        }

        public Visibility SendAsSMSVisibility
        {
            get
            {
                if (IsSent && !IsSms && MessageStatus == State.SENT_CONFIRMED && App.newChatThreadPage != null && App.newChatThreadPage.IsSMSOptionValid)
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
            _isSent = (msgState == State.SENT_UNCONFIRMED ||
                        msgState == State.SENT_CONFIRMED ||
                        msgState == State.SENT_DELIVERED ||
                        msgState == State.SENT_DELIVERED_READ ||
                        msgState == State.SENT_FAILED ||
                        msgState == State.FORCE_SMS_SENT_CONFIRMED ||
                        msgState == State.FORCE_SMS_SENT_DELIVERED ||
                        msgState == State.FORCE_SMS_SENT_DELIVERED_READ);
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
                        singleFileInfo[HikeConstants.FILE_SIZE] = FileAttachment.FileSize;
                        singleFileInfo[HikeConstants.FILE_KEY] = FileAttachment.FileKey;
                        singleFileInfo[HikeConstants.FILE_CONTENT_TYPE] = FileAttachment.ContentType;

                        if (FileAttachment.ContentType.Contains(HikeConstants.AUDIO) && !String.IsNullOrEmpty(this.MetaDataString))
                        {
                            var timeObj = JObject.Parse(this.MetaDataString);
                            singleFileInfo[HikeConstants.FILE_PLAY_TIME] = timeObj[HikeConstants.FILE_PLAY_TIME];
                        }

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
                        singleFileInfo[HikeConstants.FILE_SIZE] = FileAttachment.FileSize;
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
            else if (metadataJsonString != null && metadataJsonString.Contains(HikeConstants.STICKER_ID))
            {
                data[HikeConstants.METADATA] = JObject.Parse(metadataJsonString);
                obj[HikeConstants.SUB_TYPE] = NetworkManager.STICKER;
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
                return String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Sticker_Txt) + HikeConstants.STICKER_URL + StickerObj.Category + "/" + StickerObj.Id.Substring(0, StickerObj.Id.IndexOf("_"));

            string message = Message;

            if (FileAttachment == null)
                return message;

            if (FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Location_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else if (FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
            {
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.ContactTransfer_Text) + HikeConstants.FILE_TRANSFER_BASE_URL +
                    "/" + FileAttachment.FileKey;
            }
            else
                message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.UnknownFile_txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
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
                string messageText = "";

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
                    isFileTransfer = metadataObject.TryGetValue(HikeConstants.FILES_DATA, out filesToken);
                    if (isFileTransfer)
                    {
                        JArray files = metadataObject[HikeConstants.FILES_DATA].ToObject<JArray>();
                        JObject fileObject = files[0].ToObject<JObject>();

                        JToken fileName;
                        JToken fileKey;
                        JToken thumbnail;
                        JToken contentType;
                        JToken fileSize;

                        int fs = 0;

                        fileObject.TryGetValue(HikeConstants.FILE_CONTENT_TYPE, out contentType);
                        fileObject.TryGetValue(HikeConstants.FILE_NAME, out fileName);
                        fileObject.TryGetValue(HikeConstants.FILE_KEY, out fileKey);
                        fileObject.TryGetValue(HikeConstants.FILE_THUMBNAIL, out thumbnail);

                        if (fileObject.TryGetValue(HikeConstants.FILE_SIZE, out fileSize))
                            fs = Convert.ToInt32(fileSize.ToString());

                        this.HasAttachment = true;

                        byte[] base64Decoded = null;
                        if (thumbnail != null)
                            base64Decoded = System.Convert.FromBase64String(thumbnail.ToString());

                        if (contentType.ToString().Contains(HikeConstants.LOCATION))
                        {
                            this.FileAttachment = new Attachment(fileName == null ? AppResources.Location_Txt : fileName.ToString(), fileKey == null ? "" : fileKey.ToString(), base64Decoded,
                        contentType.ToString(), Attachment.AttachmentState.NOT_STARTED, fs);

                            JObject locationFile = new JObject();
                            locationFile[HikeConstants.LATITUDE] = fileObject[HikeConstants.LATITUDE];
                            locationFile[HikeConstants.LONGITUDE] = fileObject[HikeConstants.LONGITUDE];
                            locationFile[HikeConstants.ZOOM_LEVEL] = fileObject[HikeConstants.ZOOM_LEVEL];
                            locationFile[HikeConstants.LOCATION_ADDRESS] = fileObject[HikeConstants.LOCATION_ADDRESS];
                            locationFile[HikeConstants.LOCATION_TITLE] = fileObject[HikeConstants.LOCATION_TITLE];

                            this.MetaDataString = locationFile.ToString(Newtonsoft.Json.Formatting.None);
                        }
                        else
                        {
                            this.FileAttachment = new Attachment(fileName == null ? "" : fileName.ToString(), fileKey == null ? "" : fileKey.ToString(), base64Decoded,
                           contentType.ToString(), Attachment.AttachmentState.NOT_STARTED, fs);
                        }

                        if (contentType.ToString().Contains(HikeConstants.CONTACT) || contentType.ToString().Contains(HikeConstants.AUDIO))
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
                        else
                            messageText = AppResources.UnknownFile_txt;
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
                JToken isSticker;
                JToken stickerJson;
                if (obj.TryGetValue(HikeConstants.SUB_TYPE, out isSticker) && data.TryGetValue(HikeConstants.METADATA, out stickerJson))
                {
                    metadataJsonString = stickerJson.ToString(Newtonsoft.Json.Formatting.None);
                    _message = AppResources.Sticker_Txt;
                }

                long serverTimeStamp = (long)data[HikeConstants.TIMESTAMP];

                long timedifference;
                if (App.appSettings.TryGetValue(HikeConstants.AppSettings.TIME_DIFF_EPOCH, out timedifference))
                {
                    _timestamp = serverTimeStamp - timedifference;
                }
                else
                    _timestamp = serverTimeStamp;

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

                this._message = this._message.Replace(";", "");// as while displaying MEMBERS_JOINED in CT we split on ; for dnd message
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

            SdrImageVisibility = attachmentState != Attachment.AttachmentState.STARTED
                && attachmentState != Attachment.AttachmentState.PAUSED
                && attachmentState != Attachment.AttachmentState.MANUAL_PAUSED
                && MessageStatus != State.SENT_FAILED
                ? Visibility.Visible : Visibility.Collapsed;

            NotifyPropertyChanged("SdrImageVisibility");
            NotifyPropertyChanged("FileFailedImageVisibility");

            ChangingState = false;
        }

        public void UpdateVisibilitySdrImage()
        {
            if (_fileAttachment == null)
            {
                SdrImageVisibility = Visibility.Visible;
                NotifyPropertyChanged("SdrImageVisibility");
            }
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
                case ParticipantInfoState.CHAT_BACKGROUND_CHANGED:
                    grpId = (string)jsonObj[HikeConstants.TO];
                    from = (string)jsonObj[HikeConstants.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == App.MSISDN)
                        this.Message = string.Format(AppResources.ChatBg_Changed_Text, AppResources.You_Txt);
                    else
                    {
                        gp = GroupManager.Instance.getGroupParticipant(null, from, grpId);
                        this.Message = string.Format(AppResources.ChatBg_Changed_Text, gp.Name);
                    }
                    this.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                    break;
                case ParticipantInfoState.CHAT_BACKGROUND_CHANGE_NOT_SUPPORTED:
                    grpId = (string)jsonObj[HikeConstants.TO];
                    from = (string)jsonObj[HikeConstants.FROM];
                    this._groupParticipant = from;
                    this._msisdn = grpId;
                    if (from == App.MSISDN)
                        this.Message = string.Format(AppResources.ChatBg_NotChanged_Text, AppResources.You_Txt);
                    else
                    {
                        gp = GroupManager.Instance.getGroupParticipant(null, from, grpId);
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
