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
using windows_client.DbUtils;
using windows_client.View;
using System.Collections.Generic;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {
        public static ReceivedChatBubble getSplitChatBubbles(ConvMessage cm, bool isGroupChat, string userName)
        {
            ReceivedChatBubble receivedChatBubble;
            if (cm.Message.Length < HikeConstants.MAX_CHATBUBBLE_SIZE)
            {
                receivedChatBubble = new ReceivedChatBubble(cm, isGroupChat, userName, cm.Message);
                return receivedChatBubble;
            }
            receivedChatBubble = new ReceivedChatBubble(cm, isGroupChat, userName, cm.Message.Substring(0, HikeConstants.MAX_CHATBUBBLE_SIZE));
            receivedChatBubble.splitChatBubbles = new List<MyChatBubble>();
            int lengthOfNextBubble = 1400;
            for (int i = 1; i <= (cm.Message.Length / HikeConstants.MAX_CHATBUBBLE_SIZE); i++)
            {
                if ((cm.Message.Length - (i) * HikeConstants.MAX_CHATBUBBLE_SIZE) / HikeConstants.MAX_CHATBUBBLE_SIZE > 0)
                {
                    lengthOfNextBubble = HikeConstants.MAX_CHATBUBBLE_SIZE;
                }
                else
                {
                    lengthOfNextBubble = (cm.Message.Length - (i) * HikeConstants.MAX_CHATBUBBLE_SIZE) % HikeConstants.MAX_CHATBUBBLE_SIZE;
                }
                ReceivedChatBubble splitBubble = new ReceivedChatBubble(cm, isGroupChat, userName, cm.Message.Substring
                    (i * HikeConstants.MAX_CHATBUBBLE_SIZE, lengthOfNextBubble));
                receivedChatBubble.splitChatBubbles.Add(splitBubble);
            }
            return receivedChatBubble;
        }

        public ReceivedChatBubble(ConvMessage cm, bool isGroupChat, string userName, string messageString)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
            string contentType = cm.FileAttachment == null ? "" : cm.FileAttachment.ContentType;
            bool showDownload = cm.FileAttachment != null && (cm.FileAttachment.FileState == Attachment.AttachmentState.CANCELED ||
                cm.FileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
            initializeBasedOnState(cm, isGroupChat, userName, messageString);

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
                if (this.temporaryProgressBar != null && this.temporaryProgressBar.Visibility == Visibility.Visible)
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
            if (this.PlayIcon != null && this.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
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

        public override void setAttachmentState(Attachment.AttachmentState attachmentState)
        {
            this.FileAttachment.FileState = attachmentState;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                var currentPage = ((App)Application.Current).RootFrame.Content as NewChatThread;
                if (currentPage != null)
                {
                    ContextMenu contextMenu = currentPage.createAttachmentContextMenu(attachmentState, false);
                    ContextMenuService.SetContextMenu(this, contextMenu);
                    switch (attachmentState)
                    {
                        case Attachment.AttachmentState.CANCELED:
                            uploadOrDownloadCanceled();
                            break;
                        case Attachment.AttachmentState.FAILED_OR_NOT_STARTED:
                            MessagesTableUtils.removeUploadingOrDownloadingMessage(this.MessageId);
                            break;
                        case Attachment.AttachmentState.COMPLETED:
                            uploadOrDownloadCompleted();
                            MessagesTableUtils.removeUploadingOrDownloadingMessage(this.MessageId);
                            break;
                        case Attachment.AttachmentState.STARTED:
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
        private ProgressBar downloadProgress;
        private LinkifiedTextBox MessageText;
        private TextBlock TimeStampBlock;
        private PerformanceProgressBar temporaryProgressBar;

        private static Thickness nudgeMargin = new Thickness(12, 12, 12, 10);
        private static Thickness imgMargin = new Thickness(12, 12, 12, 0);
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 12, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);
        private static Thickness userNameMargin = new Thickness(12, 12, 0, 0);


        private void initializeBasedOnState(ConvMessage cm, bool isGroupChat, string userName, string messageString)
        {
            bool hasAttachment = cm.HasAttachment;
            string contentType = cm.FileAttachment == null ? "" : cm.FileAttachment.ContentType;
            bool isContact = hasAttachment && contentType == HikeConstants.CONTACT;

            bool showDownload = cm.FileAttachment != null && (cm.FileAttachment.FileState == Attachment.AttachmentState.CANCELED ||
                cm.FileAttachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED) && !isContact;
            bool isNudge = cm.MetaDataString != null && cm.MetaDataString.Contains("poke");

            Rectangle BubbleBg = new Rectangle();
            if (!isNudge)
            {
                BubbleBg.Fill = UI_Utils.Instance.ReceivedChatBubbleColor;
                bubblePointer.Fill = UI_Utils.Instance.ReceivedChatBubbleColor;
            }
            else
            {
                BubbleBg.Fill = UI_Utils.Instance.PhoneThemeColor;
                bubblePointer.Fill = UI_Utils.Instance.PhoneThemeColor;
            }
            Grid.SetRowSpan(BubbleBg, 2 + (isGroupChat ? 1 : 0));
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

            if (hasAttachment || isNudge)
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
                if (contentType.Contains(HikeConstants.AUDIO))
                    this.MessageImage.Source = UI_Utils.Instance.AudioAttachmentReceive;
                else if (isNudge)
                {
                    this.MessageImage.Source = UI_Utils.Instance.NudgeReceived;
                    this.MessageImage.Height = 35;
                    this.MessageImage.Width = 46;
                    this.MessageImage.Margin = nudgeMargin;
                }
                else if (isContact)
                {
                    this.MessageImage.Source = UI_Utils.Instance.NudgeSent;
                    this.MessageImage.Height = 35;
                    this.MessageImage.Width = 48;
                    this.MessageImage.Margin = nudgeMargin;
                    MessageText = new LinkifiedTextBox(UI_Utils.Instance.White, 22, messageString);
                    MessageText.Width = 330;
                    MessageText.Margin = messageTextMargin;
                    MessageText.FontFamily = UI_Utils.Instance.MessageText;
                    Grid.SetRow(MessageText, 0);
                    Grid.SetColumn(MessageText, 1);
                    attachment.Children.Add(MessageText);

                }
                Grid.SetRow(MessageImage, 0);
                attachment.Children.Add(MessageImage);

                if ((contentType.Contains(HikeConstants.VIDEO) || contentType.Contains(HikeConstants.AUDIO) || showDownload) && !contentType.Contains(HikeConstants.LOCATION))
                {

                    PlayIcon = new Image();
                    PlayIcon.MaxWidth = 43;
                    PlayIcon.MaxHeight = 42;
                    if (contentType.Contains(HikeConstants.IMAGE))
                        PlayIcon.Source = UI_Utils.Instance.DownloadIcon;
                    else
                        PlayIcon.Source = UI_Utils.Instance.PlayIcon;
                    PlayIcon.HorizontalAlignment = HorizontalAlignment.Center;
                    PlayIcon.VerticalAlignment = VerticalAlignment.Center;

                    PlayIcon.Margin = imgMargin;
                    Grid.SetRow(PlayIcon, 0);
                    attachment.Children.Add(PlayIcon);

                }
                if (!isNudge)
                {

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
            }
            else
            {
                MessageText = new LinkifiedTextBox(UI_Utils.Instance.ReceiveMessageForeground, 22, messageString);
                MessageText.Width = 330;
                if (!isGroupChat)
                    MessageText.Margin = messageTextMargin;
                MessageText.FontFamily = UI_Utils.Instance.MessageText;
                Grid.SetRow(MessageText, rowNumber);
                wrapperGrid.Children.Add(MessageText);
            }
            if (!isNudge)
            {
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
}