using System.Windows;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.utils;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Data;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {

        public ReceivedChatBubble(ConvMessage cm)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            string contentType = cm.FileAttachment == null?"": cm.FileAttachment.ContentType;
            bool showDownload = cm.FileAttachment != null && (cm.FileAttachment.FileState == Attachment.AttachmentState.CANCELED ||
                cm.FileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
            initializeBasedOnState(cm.HasAttachment, contentType, showDownload, cm.Message);

            if (cm.FileAttachment != null && cm.FileAttachment.Thumbnail != null && cm.FileAttachment.Thumbnail.Length != 0)
            {
                using (var memStream = new MemoryStream(cm.FileAttachment.Thumbnail))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage fileThumbnail = new BitmapImage();
                    fileThumbnail.SetSource(memStream);
                    this.MessageImage.Source = fileThumbnail;
                }
            }
        }

        public void updateProgress(double progressValue)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                this.downloadProgress.Value = progressValue;
                if (progressValue == this.downloadProgress.Maximum)
                {
                    this.downloadProgress.Opacity = 0;
                }
            });
        }

        protected override void uploadOrDownloadCanceled()
        {
            this.downloadProgress.Value = 0;
        }

        protected override void uploadOrDownloadCompleted()
        {
            if(this.PlayIcon!=null)
                this.PlayIcon.Visibility = Visibility.Collapsed;
        }


        private Grid attachment;
        public Image MessageImage;
        private Image PlayIcon;
        private ProgressBar downloadProgress;
        private LinkifiedTextBox MessageText;
        private TextBlock TimeStampBlock;
        
        private static Thickness imgMargin = new Thickness(12, 12, 12, 0);
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 6, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);

        private readonly SolidColorBrush progressColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));

        private void initializeBasedOnState(bool hasAttachment, string contentType, bool showDownload, string messageString)
        {
            Rectangle BubbleBg = new Rectangle();
            BubbleBg.Fill = UI_Utils.Instance.TextBoxBackground;
            Grid.SetRowSpan(BubbleBg, 2);
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
                MessageImage.HorizontalAlignment = HorizontalAlignment.Left;
                MessageImage.Margin = imgMargin;
                if (contentType.Contains("audio"))
                    this.MessageImage.Source = UI_Utils.Instance.AudioAttachment;

                Grid.SetRow(MessageImage, 0);
                attachment.Children.Add(MessageImage);

                if (contentType.Contains("video") || contentType.Contains("audio") || showDownload)
                {

                    PlayIcon = new Image();
                    PlayIcon.MaxWidth = 43;
                    PlayIcon.MaxHeight = 42;
                    if (contentType.Contains("image"))
                        PlayIcon.Source = UI_Utils.Instance.DownloadIcon;
                    else
                        PlayIcon.Source = UI_Utils.Instance.PlayIcon;
                    PlayIcon.HorizontalAlignment = HorizontalAlignment.Center;
                    PlayIcon.VerticalAlignment = VerticalAlignment.Center;

                    PlayIcon.Margin = imgMargin;
                    Grid.SetRow(PlayIcon, 0);
                    attachment.Children.Add(PlayIcon);
                }
                

                downloadProgress = new ProgressBar();
                downloadProgress.Height = 10;
                downloadProgress.Background = UI_Utils.Instance.TextBoxBackground;
                downloadProgress.Foreground = progressColor;
                downloadProgress.Minimum = 0;
                downloadProgress.MaxHeight = 100;
                Grid.SetRow(downloadProgress, 1);
                attachment.Children.Add(downloadProgress);
            }
            else
            {
                MessageText = new LinkifiedTextBox(UI_Utils.Instance.ReceiveMessageForeground, 24, messageString);
                MessageText.Width = 340;
                MessageText.Margin = messageTextMargin;
                //Binding messageTextBinding = new Binding("Text");
                //MessageText.SetBinding(LinkifiedTextBox.TextProperty, messageTextBinding);
                Grid.SetRow(MessageText, 0);
                wrapperGrid.Children.Add(MessageText);

            }

            TimeStampBlock = new TextBlock();
            TimeStampBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            TimeStampBlock.FontSize = 18;
            TimeStampBlock.Foreground = progressColor;
            TimeStampBlock.Text = TimeStamp;
            TimeStampBlock.Margin = timeStampBlockMargin;
            Grid.SetRow(TimeStampBlock, 1);
            wrapperGrid.Children.Add(TimeStampBlock);
        }
    }
}