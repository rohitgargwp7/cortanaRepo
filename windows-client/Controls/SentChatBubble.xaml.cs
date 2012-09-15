using System.Windows;
using System.Windows.Media;
using windows_client.utils;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Data;

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
            string contentType = cm.FileAttachment == null?"": cm.FileAttachment.ContentType;
            initializeBasedOnState(cm.HasAttachment, contentType);
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
                case ConvMessage.State.SENT_UNCONFIRMED:
                case ConvMessage.State.UNKNOWN:
                    if (cm.HasAttachment)
                    {
                        this.SDRImage.Source = UI_Utils.Instance.HttpFailed;
                    }
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

        public SentChatBubble(ConvMessage cm, byte[] thumbnailsBytes)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(true, "image");
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
            using (var memStream = new MemoryStream(thumbnailsBytes))
            {
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage fileThumbnail = new BitmapImage();
                fileThumbnail.SetSource(memStream);
                this.MessageImage.Source = fileThumbnail;
            }
        }

        public SentChatBubble(MyChatBubble chatBubble, long messageId, bool onHike)
            :base(chatBubble, messageId)
        {
            InitializeComponent();
            string contentType = chatBubble.FileAttachment == null ? "" : chatBubble.FileAttachment.ContentType;
            initializeBasedOnState(chatBubble.FileAttachment != null, contentType);

            if (onHike)
            {
                bubbleColor = UI_Utils.Instance.HikeMsgBackground;
                uploadProgress.Background = UI_Utils.Instance.HikeMsgBackground;
            }
            else
            {
                bubbleColor = UI_Utils.Instance.SmsBackground;
                uploadProgress.Background = UI_Utils.Instance.SmsBackground;
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

        protected override void uploadOrDownloadCanceled()
        {
            this.uploadProgress.Value = 0;
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

        private void initializeBasedOnState(bool hasAttachment, string contentType)
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
                    this.MessageImage.Source = UI_Utils.Instance.AudioAttachment;

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

                //    <ProgressBar Grid.Row="1" x:Name="uploadProgress" Margin="0,5,0,0" Grid.Column="1" Height="8" Background="Transparent" Foreground="#333333" Minimum="0" Maximum="100"></ProgressBar>
                uploadProgress = new ProgressBar();
                uploadProgress.Height = 10;
                uploadProgress.Foreground = progressColor;
                uploadProgress.Minimum = 0;
                uploadProgress.MaxHeight = 100;
                Grid.SetRow(uploadProgress, 1);
                attachment.Children.Add(uploadProgress);
            }
            else
            {
                MessageText = new LinkifiedTextBox();
                MessageText.Width = 340;
                MessageText.Foreground = progressColor;
                MessageText.Margin = messageTextMargin;
                Binding messageTextBinding = new Binding("Text");
                MessageText.SetBinding(LinkifiedTextBox.TextProperty, messageTextBinding);
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
            TimeStampBlock.Foreground = progressColor;
            TimeStampBlock.Text = TimeStamp;
            TimeStampBlock.Margin = timeStampBlockMargin;
            Grid.SetRow(TimeStampBlock, 1);
            Grid.SetColumn(TimeStampBlock, 1);
            wrapperGrid.Children.Add(TimeStampBlock);

        }
    }
}