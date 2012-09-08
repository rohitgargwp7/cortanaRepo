using System;
using System.Windows;
using System.Windows.Media;
using windows_client.utils;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.IO;
using windows_client.DbUtils;
using Microsoft.Phone.Controls;

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble
    {
        private SolidColorBrush bubbleColor;
        private ConvMessage.State messageState;
        public SentChatBubble(ConvMessage cm, ContextMenu menu)
            : base(cm, menu)
        {
            // Required to initialize variables
            InitializeComponent();
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
            if (cm.FileAttachment!=null && (cm.FileAttachment.ContentType.Contains("video") || (cm.FileAttachment.ContentType.Contains("image"))))
            {
                byte[] imageBytes;
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_THUMBNAILS + "/" + 
                    cm.Msisdn + "/" + Convert.ToString(cm.MessageId), out imageBytes);

                MemoryStream memStream = new MemoryStream(imageBytes);
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage fileThumbnail = new BitmapImage();
                fileThumbnail.SetSource(memStream);
                this.MessageImage.Source = fileThumbnail;
            }
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
        }

        public SentChatBubble(bool onHike, BitmapImage sentImage, long messageId)
            : base(messageId)
        {
            // Required to initialize variables
            InitializeComponent();

            if (onHike)
            {
                bubbleColor = UI_Utils.Instance.HikeMsgBackground;
            }
            else
            {
                bubbleColor = UI_Utils.Instance.SmsBackground;
            }
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
            this.MessageImage.Source = sentImage;
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