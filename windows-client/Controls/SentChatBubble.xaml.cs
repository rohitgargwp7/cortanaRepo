using System.Windows;
using System.Windows.Media;
using windows_client.utils;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Data;
using Microsoft.Phone.Reactive;
using System;
using windows_client.View;
using windows_client.DbUtils;
using System.Collections.Generic;
using System.Diagnostics;

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble
    {
        private SolidColorBrush bubbleColor;
        private ConvMessage.State messageState;
        private IScheduler scheduler = Scheduler.NewThread;

        public static SentChatBubble getSplitChatBubbles(ConvMessage cm, bool readFromDB)
        {
            try
            {
                SentChatBubble sentChatBubble;
                if (cm.Message.Length < HikeConstants.MAX_CHATBUBBLE_SIZE)
                {
                    sentChatBubble = new SentChatBubble(cm, readFromDB, cm.Message);
                    return sentChatBubble;
                }
                sentChatBubble = new SentChatBubble(cm, readFromDB, cm.Message.Substring(0, HikeConstants.MAX_CHATBUBBLE_SIZE));
                sentChatBubble.splitChatBubbles = new List<MyChatBubble>();
                int lengthOfNextBubble = 1800;
                for (int i = 1; i <= (cm.Message.Length / HikeConstants.MAX_CHATBUBBLE_SIZE); i++)
                {
                    if ((cm.Message.Length - (i ) * HikeConstants.MAX_CHATBUBBLE_SIZE) / HikeConstants.MAX_CHATBUBBLE_SIZE > 0)
                    {
                        lengthOfNextBubble = HikeConstants.MAX_CHATBUBBLE_SIZE;
                    }
                    else
                    {
                        lengthOfNextBubble = (cm.Message.Length - (i) * HikeConstants.MAX_CHATBUBBLE_SIZE) % HikeConstants.MAX_CHATBUBBLE_SIZE;
                    }
                    SentChatBubble splitBubble = new SentChatBubble(cm, readFromDB, cm.Message.Substring(i * HikeConstants.MAX_CHATBUBBLE_SIZE,
                        lengthOfNextBubble));
                    sentChatBubble.splitChatBubbles.Add(splitBubble);
                }
                return sentChatBubble;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Sent chat bubble :: " + e.StackTrace);
                return null;
            }
        }

        public SentChatBubble(ConvMessage cm, bool readFromDB, string messageText)
            : base(cm)
        {
            InitializeComponent();
            initializeBasedOnState(cm, messageText);
            //IsSms is false for group chat
            switch (cm.MessageStatus)
            {
                case ConvMessage.State.SENT_CONFIRMED:
                    this.SDRImage.Source = UI_Utils.Instance.Sent;
                    break;
                case ConvMessage.State.SENT_DELIVERED:
                    this.SDRImage.Source = UI_Utils.Instance.Delivered;
                    break;
                case ConvMessage.State.SENT_DELIVERED_READ:
                    this.SDRImage.Source = UI_Utils.Instance.Read;
                    break;
                case ConvMessage.State.UNKNOWN:
                    if (cm.HasAttachment)
                    {
                        this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                    }
                    break;
                case ConvMessage.State.SENT_UNCONFIRMED:
                    if (this.FileAttachment != null)
                    {
                        if (string.IsNullOrEmpty(this.FileAttachment.FileKey))
                            this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                        else
                            this.SDRImage.Source = UI_Utils.Instance.Trying;
                    }
                    else if (readFromDB)
                    {
                        this.SDRImage.Source = UI_Utils.Instance.Trying;
                    }
                    else
                    {
                        scheduleTryingImage();
                    }
                    break;
                default:
                    break;
            }
            if (cm.FileAttachment != null && cm.FileAttachment.Thumbnail != null)
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

        //public SentChatBubble(ConvMessage cm, bool readFromDB)
        //    : base(cm)
        //{
        //    // Required to initialize variables
        //    InitializeComponent();
        //    initializeBasedOnState(cm, cm.Message);
        //    //IsSms is false for group chat
        //    switch (cm.MessageStatus)
        //    {
        //        case ConvMessage.State.SENT_CONFIRMED:
        //            this.SDRImage.Source = UI_Utils.Instance.Sent;
        //            break;
        //        case ConvMessage.State.SENT_DELIVERED:
        //            this.SDRImage.Source = UI_Utils.Instance.Delivered;
        //            break;
        //        case ConvMessage.State.SENT_DELIVERED_READ:
        //            this.SDRImage.Source = UI_Utils.Instance.Read;
        //            break;
        //        case ConvMessage.State.UNKNOWN:
        //            if (cm.HasAttachment)
        //            {
        //                this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
        //            }
        //            break;
        //        case ConvMessage.State.SENT_UNCONFIRMED:
        //            if (this.FileAttachment != null)
        //            {
        //                this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
        //            }
        //            else if (readFromDB)
        //            {
        //                this.SDRImage.Source = UI_Utils.Instance.Trying;
        //            }
        //            else
        //            {
        //                scheduleTryingImage();
        //            }
        //            break;
        //        default:
        //            break;
        //    }
        //    if (cm.FileAttachment != null && cm.FileAttachment.Thumbnail != null)
        //    {
        //        using (var memStream = new MemoryStream(cm.FileAttachment.Thumbnail))
        //        {
        //            memStream.Seek(0, SeekOrigin.Begin);
        //            BitmapImage fileThumbnail = new BitmapImage();
        //            fileThumbnail.SetSource(memStream);
        //            this.MessageImage.Source = fileThumbnail;
        //        }
        //    }
        //    this.BubblePoint.Fill = bubbleColor;
        //    this.BubbleBg.Fill = bubbleColor;
        //}

        public SentChatBubble(ConvMessage cm, byte[] thumbnailsBytes)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(cm, cm.Message);
            this.BubblePoint.Fill = bubbleColor;
            this.BubbleBg.Fill = bubbleColor;
            if (thumbnailsBytes != null && thumbnailsBytes.Length > 0)
            {
                using (var memStream = new MemoryStream(thumbnailsBytes))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage fileThumbnail = new BitmapImage();
                    fileThumbnail.SetSource(memStream);
                    this.MessageImage.Source = fileThumbnail;
                }
            }
        }

        //for those messages where attachment is uploaded but mqtt failed
        public void SetSentMessageStatusForUploadedAttachments()
        {
            this.SDRImage.Source = null;
            scheduleTryingImage();
        }

        public void SetSentMessageStatus(ConvMessage.State msgState)
        {
            if ((int)messageState <= (int)msgState)
            {
                messageState = msgState;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    switch (messageState)
                    {
                        case ConvMessage.State.SENT_CONFIRMED:
                            setSDRImage(UI_Utils.Instance.Sent);
                            if (this.FileAttachment != null && this.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                            {
                                this.setAttachmentState(Attachment.AttachmentState.COMPLETED);
                            }
                            break;
                        case ConvMessage.State.SENT_DELIVERED:
                            setSDRImage(UI_Utils.Instance.Delivered);
                            break;
                        case ConvMessage.State.SENT_DELIVERED_READ:
                            setSDRImage(UI_Utils.Instance.Read);
                            break;
                        case ConvMessage.State.SENT_FAILED:
                            setSDRImage(UI_Utils.Instance.HttpFailed);
                            break;
                        case ConvMessage.State.SENT_UNCONFIRMED:
                            if (this.FileAttachment != null)
                            {
                                if (string.IsNullOrEmpty(this.FileAttachment.FileKey))
                                    this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                                else
                                    this.SDRImage.Source = UI_Utils.Instance.Trying;
                            }
                            else
                            {
                                scheduleTryingImage();
                            }
                            break;
                    }
                });
            }
        }

        private void setSDRImage(BitmapImage img)
        {
            this.SDRImage.Source = img;
            if (this.splitChatBubbles != null && this.splitChatBubbles.Count > 0)
            {
                for (int i = 0; i < this.splitChatBubbles.Count; i++)
                {
                    (this.splitChatBubbles[i] as SentChatBubble).SDRImage.Source = img;
                }
            }
        }

        public void scheduleTryingImage()
        {
            scheduler.Schedule(setTryingImage, TimeSpan.FromSeconds(3));
        }

        private void setTryingImage()
        {
            if (this.messageState == ConvMessage.State.SENT_UNCONFIRMED)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    this.SDRImage.Source = UI_Utils.Instance.Trying;
                });
            }
        }

        public bool updateProgress(double progressValue)
        {
            if (this.FileAttachment.FileState == Attachment.AttachmentState.CANCELED)
                return false;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                progressValue -= 10;
                progressValue = progressValue > 0 ? progressValue : 0;
                this.uploadProgress.Value = progressValue;
                if (progressValue == this.uploadProgress.Maximum)
                {
                    this.uploadProgress.Opacity = 0;
                }
            });
            return true;
        }

        protected override void uploadOrDownloadCanceled()
        {
            this.uploadProgress.Value = 0;
            this.uploadProgress.Opacity = 0;
            this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
        }

        protected override void uploadOrDownloadStarted()
        {
            this.uploadProgress.Value = 0;
            this.uploadProgress.Opacity = 1;
            this.SDRImage.Source = null;
        }

        public override void setAttachmentState(Attachment.AttachmentState attachmentState)
        {
            this.FileAttachment.FileState = attachmentState;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                if (currentPage != null)
                {
                    switch (attachmentState)
                    {
                        case Attachment.AttachmentState.CANCELED:
                            uploadOrDownloadCanceled();
                            setContextMenu(currentPage.AttachmentUploadCanceledOrFailed);
                            break;
                        case Attachment.AttachmentState.FAILED_OR_NOT_STARTED:
                            uploadOrDownloadCanceled();
                            setContextMenu(currentPage.AttachmentUploadCanceledOrFailed);
                            MessagesTableUtils.removeUploadingOrDownloadingMessage(this.MessageId);
                            break;
                        case Attachment.AttachmentState.COMPLETED:
                            setContextMenu(currentPage.AttachmentUploadedOrDownloaded);
                            uploadOrDownloadCompleted();
                            MessagesTableUtils.removeUploadingOrDownloadingMessage(this.MessageId);
                            break;
                        case Attachment.AttachmentState.STARTED:
                            setContextMenu(currentPage.AttachmentUploading);
                            uploadOrDownloadStarted();
                            MessagesTableUtils.addUploadingOrDownloadingMessage(this.MessageId);
                            break;
                    }
                }
            });
        }



        private Grid attachment;
        public Image MessageImage;
        private Image PlayIcon;
        private ProgressBar uploadProgress;
        private LinkifiedTextBox MessageText;
        private TextBlock TimeStampBlock;
        private Rectangle BubbleBg;
        private Image SDRImage;

        private static Thickness imgMargin = new Thickness(12, 12, 12, 0);
        private static Thickness nudgeMargin = new Thickness(12, 12, 12, 10);
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 6, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);
        private readonly SolidColorBrush progressColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private static Thickness sdrImageMargin = new Thickness(0, 0, 10, 0);

        private static Thickness textBubbleMargin = new Thickness(74, 12, 0, 10);
        private static Thickness nudgeBubbleMargin = new Thickness(348, 12, 0, 10);
        private static Thickness attachmentBubbleMargin = new Thickness(200, 12, 0, 10);

        private void initializeBasedOnState(ConvMessage cm, string messageString)
        {
            bool hasAttachment = cm.HasAttachment;
            string contentType = cm.FileAttachment == null ? "" : cm.FileAttachment.ContentType;
            //string messageString = cm.Message;
            bool isSMS = cm.IsSms;
            bool isNudge = cm.MetaDataString != null && cm.MetaDataString.Contains("poke");


            BubbleBg = new Rectangle();
            Grid.SetRowSpan(BubbleBg, 2);
            Grid.SetColumn(BubbleBg, 1);
            wrapperGrid.Children.Add(BubbleBg);

            if (hasAttachment || isNudge)
            {
                attachment = new Grid();
                RowDefinition r1 = new RowDefinition();
                r1.Height = GridLength.Auto;
                RowDefinition r2 = new RowDefinition();
                r2.Height = GridLength.Auto;
                attachment.RowDefinitions.Add(r1);
                attachment.RowDefinitions.Add(r2);
                Grid.SetRow(attachment, 0);
                Grid.SetColumn(attachment, 1);
                wrapperGrid.Children.Add(attachment);

                MessageImage = new Image();
                MessageImage.MaxWidth = 180;
                MessageImage.MaxHeight = 180;
                MessageImage.HorizontalAlignment = HorizontalAlignment.Right;
                MessageImage.Margin = imgMargin;
                if (contentType.Contains("audio"))
                    this.MessageImage.Source = UI_Utils.Instance.AudioAttachmentSend;
                else if (isNudge)
                {
                    this.MessageImage.Source = UI_Utils.Instance.NudgeSent;
                    this.MessageImage.Height = 24;
                    this.MessageImage.Width = 31;
                    this.MessageImage.Margin = nudgeMargin;
                    this.Margin = nudgeBubbleMargin;
                }
                Grid.SetRow(MessageImage, 0);
                attachment.Children.Add(MessageImage);

                if (contentType.Contains("video") || contentType.Contains("audio"))
                {
                    PlayIcon = new Image();
                    PlayIcon.MaxWidth = 43;
                    PlayIcon.MaxHeight = 42;
                    PlayIcon.Source = UI_Utils.Instance.PlayIcon;
                    PlayIcon.HorizontalAlignment = HorizontalAlignment.Center;
                    PlayIcon.VerticalAlignment = VerticalAlignment.Center;
                    PlayIcon.Margin = imgMargin;
                    Grid.SetRow(PlayIcon, 0);
                    attachment.Children.Add(PlayIcon);
                }
                if (!isNudge)
                {
                    uploadProgress = new ProgressBar();
                    uploadProgress.Height = 10;
                    if (isSMS)
                    {
                        uploadProgress.Background = UI_Utils.Instance.SmsBackground;
                    }
                    else
                    {
                        uploadProgress.Background = UI_Utils.Instance.HikeMsgBackground;
                    }
                    uploadProgress.Opacity = 0;
                    uploadProgress.Foreground = progressColor;
                    uploadProgress.Value = 0;
                    uploadProgress.Minimum = 0;
                    uploadProgress.MaxHeight = 100;
                    Grid.SetRow(uploadProgress, 1);
                    attachment.Children.Add(uploadProgress);
                    this.Margin = attachmentBubbleMargin;
                }
            }
            else
            {
                MessageText = new LinkifiedTextBox(UI_Utils.Instance.White, 22, messageString);
                MessageText.Width = 330;
                MessageText.Foreground = progressColor;
                MessageText.Margin = messageTextMargin;
                MessageText.FontFamily = UI_Utils.Instance.MessageText;
                Grid.SetRow(MessageText, 0);
                Grid.SetColumn(MessageText, 1);
                wrapperGrid.Children.Add(MessageText);
                this.Margin = textBubbleMargin;
            }

            SDRImage = new Image();
            SDRImage.Margin = sdrImageMargin;
            SDRImage.Height = 20;
            Grid.SetRowSpan(SDRImage, 2);
            Grid.SetColumn(SDRImage, 0);
            wrapperGrid.Children.Add(SDRImage);
            if (!isNudge)
            {
                TimeStampBlock = new TextBlock();
                TimeStampBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                TimeStampBlock.FontSize = 18;
                if (isSMS)
                {
                    TimeStampBlock.Foreground = UI_Utils.Instance.SMSSentChatBubbleTimestamp;
                }
                else
                {
                    TimeStampBlock.Foreground = UI_Utils.Instance.HikeSentChatBubbleTimestamp;
                }
                TimeStampBlock.Text = TimeStamp;
                TimeStampBlock.Margin = timeStampBlockMargin;
                Grid.SetRow(TimeStampBlock, 1);
                Grid.SetColumn(TimeStampBlock, 1);
                wrapperGrid.Children.Add(TimeStampBlock);
            }

            if (isNudge)
            {
                bubbleColor = UI_Utils.Instance.PhoneThemeColor;
            }
            else if (isSMS)
            {
                bubbleColor = UI_Utils.Instance.SmsBackground;
            }
            else
            {
                bubbleColor = UI_Utils.Instance.HikeMsgBackground;
            }


        }
    }
}