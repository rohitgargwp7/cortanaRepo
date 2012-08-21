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
using windows_client.Model;
using System.Windows.Media.Imaging;

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble {
        private SolidColorBrush bubbleColor;
        public SentChatBubble(ConvMessage cm) 
        {
            // Required to initialize variables
            InitializeComponent();

            this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;
            this.Text = cm.Message;
            this.TimeStamp = DateTime.Now;
            //IsSms is false for group chat
            if (cm.IsSms)
            {
                bubbleColor = UI_Utils.smsBackground;
            }
            else
            {
                bubbleColor = UI_Utils.hikeMsgBackground;
            }
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
        }
        public void SetSDRImage(BitmapImage bm)
        {
            this.SDRImage.Source = bm;
        }
    }
}