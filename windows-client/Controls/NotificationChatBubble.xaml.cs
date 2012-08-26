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
        public NotificationChatBubble(string message, bool onHike) {
            // Required to initialize variables
            InitializeComponent();
            if (!String.IsNullOrEmpty(message))
            {
                this.UserName.Text = message;
            }
            this.OnHikeImage.Source = UI_Utils.Instance.OnHikeImage;
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