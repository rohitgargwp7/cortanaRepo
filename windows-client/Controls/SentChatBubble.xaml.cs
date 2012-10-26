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

namespace windows_client.Controls
{
    public partial class SentChatBubble : MyChatBubble
    {
        private SolidColorBrush bubbleColor;
        private ConvMessage.State messageState;
        private IScheduler scheduler = Scheduler.NewThread;

        public SentChatBubble(ConvMessage cm, bool readFromDB)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            string contentType = cm.FileAttachment == null ? "" : cm.FileAttachment.ContentType;
            initializeBasedOnState(cm.HasAttachment, contentType, cm.Message, cm.IsSms);
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
                        this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
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

        public SentChatBubble(ConvMessage cm, byte[] thumbnailsBytes)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(true, cm.FileAttachment.ContentType, cm.Message, cm.IsSms);
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
                            this.SDRImage.Source = UI_Utils.Instance.Sent;
                            if (this.FileAttachment != null && this.FileAttachment.FileState != Attachment.AttachmentState.COMPLETED)
                            {
                                this.setAttachmentState(Attachment.AttachmentState.COMPLETED);
                            }
                            break;
                        case ConvMessage.State.SENT_DELIVERED:
                            this.SDRImage.Source = UI_Utils.Instance.Delivered;
                            break;
                        case ConvMessage.State.SENT_DELIVERED_READ:
                            this.SDRImage.Source = UI_Utils.Instance.Read;
                            break;
                        case ConvMessage.State.SENT_FAILED:
                            this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                            break;
                        case ConvMessage.State.SENT_UNCONFIRMED:
                            if (this.FileAttachment != null)
                            {
                                this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
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
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 6, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);
        private readonly SolidColorBrush progressColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
        private static Thickness sdrImageMargin = new Thickness(0, 0, 10, 0);

        private void initializeBasedOnState(bool hasAttachment, string contentType, string messageString, bool isSMS)
        {
            BubbleBg = new Rectangle();
            Grid.SetRowSpan(BubbleBg, 2);
            Grid.SetColumn(BubbleBg, 1);
            wrapperGrid.Children.Add(BubbleBg);

            if (hasAttachment)
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
            }

            SDRImage = new Image();
            SDRImage.Margin = sdrImageMargin;
            SDRImage.Height = 20;
            Grid.SetRowSpan(SDRImage, 2);
            Grid.SetColumn(SDRImage, 0);
            wrapperGrid.Children.Add(SDRImage);


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
    }
}