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
using System.Collections.Generic;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {
        public ReceivedChatBubble(ConvMessage cm, Dictionary<string, RoutedEventHandler> contextMenuDictionary)
            : base(cm, contextMenuDictionary)
        {
            // Required to initialize variables
            InitializeComponent();
        }
    }
}