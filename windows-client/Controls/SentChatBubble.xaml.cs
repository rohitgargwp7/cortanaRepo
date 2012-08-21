using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using windows_client;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble {
        public SentChatBubble() {
            // Required to initialize variables
            InitializeComponent();

            this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;
        }
    }
}