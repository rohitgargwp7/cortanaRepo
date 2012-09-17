using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class NotificationChatBubble : MyChatBubble {

        private string parameter;

        public enum MessageType
        {
            HIKE_PARTICIPANT_JOINED, // hike participant has left
            SMS_PARTICIPANT_JOINED, // sms participant has joined
            PARTICIPANT_LEFT, // The participant has joined
            GROUP_END, // Group chat has ended
            WAITING,
            REWARD,
        }


        public NotificationChatBubble(MessageType messageType, string parameter)
        {
            InitializeComponent();
            this.parameter = parameter;
            setNotificationMessage(messageType);
        }

        public void setNotificationMessage(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.HIKE_PARTICIPANT_JOINED:
                    UserName.Text = String.Format(HikeConstants.PARTICIPANT_JOINED, parameter);
                    NotificationImage.Source = UI_Utils.OnHikeImage;
                    break;
                case MessageType.SMS_PARTICIPANT_JOINED:
                    UserName.Text = String.Format(HikeConstants.PARTICIPANT_JOINED, parameter);
                    NotificationImage.Source = UI_Utils.NotOnHikeImage;
                    break;
                case MessageType.PARTICIPANT_LEFT:
                    UserName.Text = String.Format(HikeConstants.PARTICIPANT_LEFT, parameter);
                    NotificationImage.Source = UI_Utils.ParticipantLeft;
                    break;
                case MessageType.GROUP_END:
                    UserName.Text = String.Format(HikeConstants.GROUP_CHAT_ENDED, parameter);
                    break;
                case MessageType.WAITING:
                    UserName.Text = String.Format(HikeConstants.WAITING_TO_JOIN, parameter);
                    NotificationImage.Source = UI_Utils.Waiting;
                    break;
                case MessageType.REWARD:
                    UserName.Text = String.Format(HikeConstants.REWARDS, parameter);
                    NotificationImage.Source = UI_Utils.Reward;
                    break;
            }
        }


        public NotificationChatBubble(string message, bool onHike) {
            // Required to initialize variables
            InitializeComponent();
            if (Utils.isDarkTheme())
                notStackPanel.Background = new SolidColorBrush(Color.FromArgb(51,51,51,1));
            if (!String.IsNullOrEmpty(message))
            {
                this.UserName.Text = message;
            }
            this.NotificationImage.Source = UI_Utils.OnHikeImage;
            if (onHike)
            {
                //this.HikeBubble.Source = UI_Utils.MessageReadBitmapImage;
            }
            else
            {
                //this.HikeBubble.Source = UI_Utils.DefaultAvatarBitmapImage;

            }
        }
    }
}