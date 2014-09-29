using CommonLibrary.Constants;
using System.Windows;
using windows_client.Controls;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.TemplateSelectors
{
    public class ChatThreadTemplateSelector : TemplateSelector
    {
        #region Properties

        public DataTemplate DtMessageStatus
        {
            get;
            set;
        }

        public DataTemplate DtForceSMSNotification
        {
            get;
            set;
        }

        public DataTemplate DtNotificationBubble
        {
            get;
            set;
        }

        public DataTemplate DtTypingNotificationBubble
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleLocation
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleText
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleFile
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleAudioFile
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleNudge
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleContact
        {
            get;
            set;
        }

        public DataTemplate DtRecievedSticker
        {
            get;
            set;
        }

        public DataTemplate DtRecievedBubbleUnknownFile
        {
            get;
            set;
        }
        public DataTemplate DtSentBubbleText
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleFile
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleLocation
        {
            get;
            set;
        }

        public DataTemplate DtSentBubbleAudioFile
        {
            get;
            set;
        }
        public DataTemplate DtSentBubbleNudge
        {
            get;
            set;
        }
        public DataTemplate DtSentBubbleUnknownFile
        {
            get;
            set;
        }
        public DataTemplate DtSentBubbleContact
        {
            get;
            set;
        }

        public DataTemplate DtStatusUpdateBubble
        {
            get;
            set;
        }

        public DataTemplate DtGCPin
        {
            get;
            set;
        }

        public DataTemplate DtSentSticker
        {
            get;
            set;
        }

        #endregion

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Determine which template to return;
            ConvMessage convMesssage = (ConvMessage)item;
            if (HikeInstantiation.NewChatThreadPageObj != null)
            {
                if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    if (convMesssage.IsSent)
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(FTBasedConstants.POKE))
                            return DtSentBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtSentSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
                            return DtSentBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                            return DtSentBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
                            return DtSentBubbleLocation;
                        else if (convMesssage.FileAttachment != null && (convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.VIDEO) || convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.IMAGE)))
                            return DtSentBubbleFile;
                        else if (convMesssage.FileAttachment != null)
                            return DtSentBubbleUnknownFile;
                        else
                            return DtSentBubbleText;
                    }
                    else
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(FTBasedConstants.POKE))
                            return DtRecievedBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtRecievedSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
                            return DtRecievedBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                            return DtRecievedBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
                            return DtRecievedBubbleLocation;
                        else if (convMesssage.FileAttachment != null && (convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.VIDEO) || convMesssage.FileAttachment.ContentType.Contains(FTBasedConstants.IMAGE)))
                            return DtRecievedBubbleFile;
                        else if (convMesssage.FileAttachment != null)
                            return DtRecievedBubbleUnknownFile;
                        else
                            return DtRecievedBubbleText;
                    }
                }
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.MESSAGE_STATUS)
                    return DtMessageStatus;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.FORCE_SMS_NOTIFICATION)
                    return DtForceSMSNotification;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.STATUS_UPDATE)
                    return DtStatusUpdateBubble;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.TYPING_NOTIFICATION)
                    return DtTypingNotificationBubble;
                else if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                    return DtGCPin;
                else
                    return DtNotificationBubble;
            }
            else
                return (new DataTemplate());
        }
    }
}
