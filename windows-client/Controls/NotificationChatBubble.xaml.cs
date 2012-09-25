using System;
using System.Windows.Media;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class NotificationChatBubble : MyChatBubble {

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
        }


        public NotificationChatBubble(MessageType messageType, string message)
        {
            InitializeComponent();
            setNotificationMessage(messageType, message);
        }

        public void setNotificationMessage(MessageType messageType, string message)
        {
            switch (messageType)
            {
                case MessageType.HIKE_PARTICIPANT_JOINED:
                    NotificationImage.Source = UI_Utils.Instance.OnHikeImage;
                    break;
                case MessageType.SMS_PARTICIPANT_INVITED:
                    NotificationImage.Source = UI_Utils.Instance.NotOnHikeImage;
                    break;
                case MessageType.SMS_PARTICIPANT_OPTED_IN:
                    NotificationImage.Source = UI_Utils.Instance.ChatAcceptedImage;
                    break;
                case MessageType.USER_JOINED_HIKE:
                    NotificationImage.Source = UI_Utils.Instance.ChatAcceptedImage;
                    break;
                case MessageType.PARTICIPANT_LEFT:
                    NotificationImage.Source = UI_Utils.Instance.ParticipantLeft;
                    break;
                case MessageType.GROUP_END:
                    NotificationImage.Source = UI_Utils.Instance.ParticipantLeft;
                    break;
                case MessageType.WAITING:
                    NotificationImage.Source = UI_Utils.Instance.Waiting;
                    break;
                case MessageType.REWARD:
                    NotificationImage.Source = UI_Utils.Instance.Reward;
                    break;
            }
            Text = message;
        }
    }
}