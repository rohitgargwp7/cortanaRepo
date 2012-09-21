using System;
using System.Windows.Media;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class NotificationChatBubble : MyChatBubble {

        public enum MessageType
        {
            HIKE_PARTICIPANT_JOINED, // hike participant has left
            SMS_PARTICIPANT_OPTED_IN, // sms participant has joined
            SMS_PARTICIPANT_INVITED, // sms participant has invited
            PARTICIPANT_LEFT, // The participant has joined
            GROUP_END, // Group chat has ended
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


        public NotificationChatBubble(string message, bool onHike) {
            // Required to initialize variables
            InitializeComponent();
            if (Utils.isDarkTheme())
                notStackPanel.Background = new SolidColorBrush(Color.FromArgb(51,51,51,1));
            if (!String.IsNullOrEmpty(message))
            {
                this.Text = message;
            }
            this.NotificationImage.Source = UI_Utils.Instance.OnHikeImage;
            if (onHike)
            {
                //this.HikeBubble.Source = UI_Utils.Instance.MessageReadBitmapImage;
            }
            else
            {
                //this.HikeBubble.Source = UI_Utils.Instance.DefaultAvatarBitmapImage;

            }
        }
    }
}