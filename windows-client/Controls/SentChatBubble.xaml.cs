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
        private Attachment fileAttachment;

        LinkifiedTextBox MessageText2;

        public bool isCanceled = false;

        public SentChatBubble(ConvMessage cm, Dictionary<string, RoutedEventHandler> contextMenuDictionary)
            : base(cm, contextMenuDictionary)
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
            if (cm.FileAttachment!=null && (cm.FileAttachment.ContentType.Contains("video") || (cm.FileAttachment.ContentType.Contains("image"))))
            {
                byte[] imageBytes;
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_THUMBNAILS + "/" + 
                    cm.Msisdn + "/" + Convert.ToString(cm.MessageId), out imageBytes);

                using (var memStream = new MemoryStream(imageBytes))
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

        public SentChatBubble(bool onHike, BitmapImage sentImage, long messageId, Dictionary<string, RoutedEventHandler> uploadingMenu)
            : base(messageId, uploadingMenu)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(true);
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

        public SentChatBubble(MyChatBubble chatBubble, long messageId, bool onHike)
            :base(chatBubble, messageId)
        {
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
                    this.MessageImage = (chatBubble as SentChatBubble).MessageImage;
                else if (chatBubble is ReceivedChatBubble)
                    this.MessageImage = (chatBubble as ReceivedChatBubble).MessageImage;
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

        public void updateProgress(double progressValue)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.uploadProgress.Value = progressValue;
                if (progressValue == this.uploadProgress.Maximum)
                {
                    this.uploadProgress.Visibility = Visibility.Collapsed;
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