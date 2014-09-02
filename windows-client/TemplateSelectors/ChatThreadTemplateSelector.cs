using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using windows_client.Controls;
using windows_client.Model;

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
            if (App.newChatThreadPage != null)
            {
                if (convMesssage.GrpParticipantState == ConvMessage.ParticipantInfoState.NO_INFO)
                {
                    if (convMesssage.IsSent)
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                            return DtSentBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtSentSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                            return DtSentBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                            return DtSentBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            return DtSentBubbleLocation;
                        else if (convMesssage.FileAttachment != null && (convMesssage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO) || convMesssage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE)))
                            return DtSentBubbleFile;
                        else if (convMesssage.FileAttachment != null)
                            return DtSentBubbleUnknownFile;
                        else
                            return DtSentBubbleText;
                    }
                    else
                    {
                        if (convMesssage.MetaDataString != null && convMesssage.MetaDataString.Contains(HikeConstants.POKE))
                            return DtRecievedBubbleNudge;
                        if (convMesssage.StickerObj != null)
                            return DtRecievedSticker;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                            return DtRecievedBubbleContact;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                            return DtRecievedBubbleAudioFile;
                        else if (convMesssage.FileAttachment != null && convMesssage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                            return DtRecievedBubbleLocation;
                        else if (convMesssage.FileAttachment != null && (convMesssage.FileAttachment.ContentType.Contains(HikeConstants.VIDEO) || convMesssage.FileAttachment.ContentType.Contains(HikeConstants.IMAGE)))
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
                else
                    return DtNotificationBubble;
            }
            else
                return (new DataTemplate());
        }
    }
}
