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

namespace windows_client.Controls {
    public partial class ReceivedChatBubble : MyChatBubble {
        public ReceivedChatBubble(ConvMessage cm) {
            // Required to initialize variables
            InitializeComponent();
            this.Text = cm.Message;
            this.TimeStamp = DateTime.Now;

        }
    }
}