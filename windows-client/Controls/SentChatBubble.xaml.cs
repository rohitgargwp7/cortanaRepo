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

        public SentChatBubble(ConvMessage cm)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(cm.HasAttachment);
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
            if (cm.FileAttachment!=null && cm.FileAttachment.Thumbnail!=null)
            {
                using (var memStream = new MemoryStream(cm.FileAttachment.Thumbnail))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage fileThumbnail = new BitmapImage();
                    fileThumbnail.SetSource(memStream);
                    this.MessageImage.Source = fileThumbnail;
                }
            }
            
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
        }

        public SentChatBubble(ConvMessage cm, BitmapImage image)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(true);
            if (!cm.IsSms)
            {
                bubbleColor = UI_Utils.Instance.HikeMsgBackground;
            }
            else
            {
                bubbleColor = UI_Utils.Instance.SmsBackground;
            }
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
            this.MessageImage.Source = image;
        }

        public SentChatBubble(MyChatBubble chatBubble, long messageId, bool onHike)
            :base(chatBubble, messageId)
        {
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
            if (chatBubble.FileAttachment != null && (chatBubble.FileAttachment.ContentType.Contains("video") ||
                (chatBubble.FileAttachment.ContentType.Contains("image"))))
            {
                if (chatBubble is SentChatBubble)
                    this.MessageImage.Source = (chatBubble as SentChatBubble).MessageImage.Source;
                else if (chatBubble is ReceivedChatBubble)
                    this.MessageImage.Source = (chatBubble as ReceivedChatBubble).MessageImage.Source;
            }
            

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
                            if (this.FileAttachment != null && this.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                            {
                                this.setAttachmentState(Attachment.AttachmentState.COMPLETED);
                            }
                            break;
                        case ConvMessage.State.SENT_DELIVERED:
                            this.SDRImage.Source = UI_Utils.Instance.MessageDeliveredBitmapImage;
                            break;
                        case ConvMessage.State.SENT_DELIVERED_READ:
                            this.SDRImage.Source = UI_Utils.Instance.MessageReadBitmapImage;
                            break;
                        case ConvMessage.State.SENT_FAILED:
                            this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                            break;
                    }
                });
            }
        }

        public void updateProgress(double progressValue)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.uploadProgress.Value = progressValue;
                if (progressValue == this.uploadProgress.Maximum)
                {
                    this.uploadProgress.Opacity = 0;
                }
            });
        }

        private void initializeBasedOnState(bool hasAttachment)
        {
            if (hasAttachment)
                this.MessageText.Visibility = Visibility.Collapsed;
            else
                this.attachment.Visibility = Visibility.Collapsed;
        }
    }
}