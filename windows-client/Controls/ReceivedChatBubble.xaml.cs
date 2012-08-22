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
    public partial class ReceivedChatBubble : MyChatBubble
    {
        public ReceivedChatBubble(ConvMessage cm, RoutedEventHandler copyClick, RoutedEventHandler forwardClick)
            : base(copyClick, forwardClick)
        {
            // Required to initialize variables
            InitializeComponent();
            this.Text = cm.Message;
            this.TimeStamp = TimeUtils.getTimeStringForChatThread(cm.Timestamp);
        }
    }
}