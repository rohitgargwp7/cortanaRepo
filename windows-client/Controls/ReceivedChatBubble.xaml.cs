using System.Windows;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.utils;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Data;
using Microsoft.Phone.Controls;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {

        public ReceivedChatBubble(ConvMessage cm, bool isGroupChat, string userName)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            string contentType = cm.FileAttachment == null?"": cm.FileAttachment.ContentType;
            bool showDownload = cm.FileAttachment != null && (cm.FileAttachment.FileState == Attachment.AttachmentState.CANCELED ||
                cm.FileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
            initializeBasedOnState(cm.HasAttachment, contentType, showDownload, cm.Message, isGroupChat, userName);

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
                if (this.temporaryProgressBar!=null && this.temporaryProgressBar.Visibility == Visibility.Visible)
                {
                    this.temporaryProgressBar.Visibility = Visibility.Collapsed;
                    this.downloadProgress.Visibility = Visibility.Visible;
                }

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
            if(this.PlayIcon!=null && this.FileAttachment.ContentType.Contains("image"))
                this.PlayIcon.Visibility = Visibility.Collapsed;
        }

        protected override void uploadOrDownloadStarted()
        {
            if (this.downloadProgress.Visibility == Visibility.Visible)
                this.downloadProgress.Visibility = Visibility.Collapsed;
            this.downloadProgress.Value = 0;
            if (this.temporaryProgressBar != null)
            {
                this.temporaryProgressBar.IsEnabled = true;
                this.temporaryProgressBar.Opacity = 1;
                this.temporaryProgressBar.Visibility = Visibility.Visible;
                temporaryProgressBar.IsIndeterminate = true;
            }
        }



        private Grid attachment;
        public Image MessageImage;
        private Image PlayIcon;
        private ProgressBar downloadProgress;
        private LinkifiedTextBox MessageText;
        private TextBlock TimeStampBlock;
        private PerformanceProgressBar temporaryProgressBar;
        
        private static Thickness imgMargin = new Thickness(12, 12, 12, 0);
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 12, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);
        private static Thickness userNameMargin = new Thickness(12, 12, 0, 0);


        private void initializeBasedOnState(bool hasAttachment, string contentType, bool showDownload, string messageString,
            bool isGroupChat, string userName)
        {
            Rectangle BubbleBg = new Rectangle();
            BubbleBg.Fill = UI_Utils.Instance.ReceivedChatBubbleColor;
            bubblePointer.Fill = UI_Utils.Instance.ReceivedChatBubbleColor;
            Grid.SetRowSpan(BubbleBg, 2 + (isGroupChat?1:0));
            wrapperGrid.Children.Add(BubbleBg);

            int rowNumber = 0;

            if (isGroupChat)
            {
                TextBlock textBlck = new TextBlock();
                textBlck.Text = userName + " -";
                textBlck.FontSize = 22;
                textBlck.FontFamily = UI_Utils.Instance.GroupChatMessageHeader;
                textBlck.Foreground = UI_Utils.Instance.GroupChatHeaderColor;
                textBlck.Margin = userNameMargin;
                Grid.SetRow(textBlck, 0);
                wrapperGrid.Children.Add(textBlck);
                rowNumber = 1;
            }

            if (hasAttachment)
            {
                attachment = new Grid();
                RowDefinition r1 = new RowDefinition();
                r1.Height = GridLength.Auto;
                RowDefinition r2 = new RowDefinition();
                r2.Height = GridLength.Auto;
                attachment.RowDefinitions.Add(r1);
                attachment.RowDefinitions.Add(r2);
                Grid.SetRow(attachment, rowNumber);
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
                downloadProgress.Background = new SolidColorBrush(Color.FromArgb(255, 0x99, 0x99, 0x99));
                downloadProgress.Foreground = UI_Utils.Instance.ReceivedChatBubbleProgress;
                downloadProgress.Minimum = 0;
                downloadProgress.MaxHeight = 100;
                downloadProgress.Opacity = 0;
                if (showDownload)
                {
                    temporaryProgressBar = new PerformanceProgressBar();
                    temporaryProgressBar.Height = 10;
//                    temporaryProgressBar.Background = UI_Utils.Instance.TextBoxBackground;
                    temporaryProgressBar.Foreground = UI_Utils.Instance.ReceivedChatBubbleProgress;
                    temporaryProgressBar.IsEnabled = false;
                    temporaryProgressBar.Opacity = 1;
                    downloadProgress.Visibility = Visibility.Collapsed;
                    downloadProgress.Opacity = 1;
                    Grid.SetRow(temporaryProgressBar, 1);
                    attachment.Children.Add(temporaryProgressBar);
                }
                Grid.SetRow(downloadProgress, 1);
                attachment.Children.Add(downloadProgress);
            }
            else
            {
                MessageText = new LinkifiedTextBox(UI_Utils.Instance.ReceiveMessageForeground, 22, messageString);
                MessageText.Width = 340;
                if(!isGroupChat)
                    MessageText.Margin = messageTextMargin;
                MessageText.FontFamily = UI_Utils.Instance.MessageText;
                Grid.SetRow(MessageText, rowNumber);
                wrapperGrid.Children.Add(MessageText);
            }
            TimeStampBlock = new TextBlock();
            TimeStampBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            TimeStampBlock.FontSize = 18;
            TimeStampBlock.Foreground = UI_Utils.Instance.ReceivedChatBubbleTimestamp;
            TimeStampBlock.Text = TimeStamp;
            TimeStampBlock.Margin = timeStampBlockMargin;
            Grid.SetRow(TimeStampBlock, rowNumber + 1);
            wrapperGrid.Children.Add(TimeStampBlock);
        }
    }
}