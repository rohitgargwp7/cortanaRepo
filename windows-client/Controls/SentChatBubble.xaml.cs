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
using windows_client.View;
using Microsoft.Phone.Controls;

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble {
        private SolidColorBrush bubbleColor;
        private ConvMessage.State messageState;
        public SentChatBubble(ConvMessage cm,RoutedEventHandler copyClick, RoutedEventHandler forwardClick)
            : base(copyClick, forwardClick)
        {
            // Required to initialize variables
            InitializeComponent();

            //            this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;

            this.Text = cm.Message;
            this.TimeStamp = TimeUtils.getTimeStringForChatThread(cm.Timestamp);
            //IsSms is false for group chat
            if (cm.IsSms)
            {
                bubbleColor = UI_Utils.Instance.SmsBackground;
            }
            else
            {
                bubbleColor = UI_Utils.Instance.HikeMsgBackground;
            }
            switch (cm.MessageStatus)
            { 
                case ConvMessage.State.SENT_CONFIRMED:
                    this.SDRImage.Source = UI_Utils.Instance.MessageSentBitmapImage;
                    break;
                case ConvMessage.State.SENT_DELIVERED:
                    this.SDRImage.Source = UI_Utils.Instance.MessageDeliveredBitmapImage;
                    break;
                case ConvMessage.State.SENT_DELIVERED_READ:
                    this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;
                    break;
                default:
                    break;

            }
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
        }
        public void SetSentMessageStatus(ConvMessage.State msgState)
        {

            if ((int)messageState < (int)msgState)
            {
                messageState = msgState;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    switch (messageState)
                    { 
                        case ConvMessage.State.SENT_CONFIRMED:
                            this.SDRImage.Source = UI_Utils.Instance.MessageSentBitmapImage;
                            break;
                        case ConvMessage.State.SENT_DELIVERED:
                            this.SDRImage.Source = UI_Utils.Instance.MessageDeliveredBitmapImage;
                            break;
                        case ConvMessage.State.SENT_DELIVERED_READ:
                            this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;
                            break;
                    }
                });
            }
        }
    }
}