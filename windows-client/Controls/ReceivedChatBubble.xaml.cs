using System;
using System.Windows;
using windows_client.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using Microsoft.Phone.Controls;
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

            initializeUrself(cm.HasAttachment, contentType);
            initializeBasedOnState(cm.HasAttachment, contentType);

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

        private void initializeBasedOnState(bool hasAttachment, string contentType)
        {
            if (hasAttachment)
            {
                if (contentType.Contains("video") || contentType.Contains("audio"))
                {
                    if (contentType.Contains("audio"))
                        this.MessageImage.Source = UI_Utils.Instance.AudioAttachment;
                    PlayIcon.Visibility = Visibility.Visible;
                }
            }
        }

        private Grid attachment;
        public Image MessageImage;
        private Image PlayIcon;
        private ProgressBar downloadProgress;
        private LinkifiedTextBoxReceive MessageText;
        private TextBlock TimeStampBlock;
        
        private static Thickness imgMargin = new Thickness(12, 12, 12, 0);
        private static Thickness progressMargin = new Thickness(0, 5, 0, 0);
        private static Thickness messageTextMargin = new Thickness(0, 6, 0, 0);
        private static Thickness timeStampBlockMargin = new Thickness(12, 0, 12, 6);

        private readonly SolidColorBrush progressColor = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));

        private void initializeUrself(bool hasAttachment, string contentType)
        {
            RowDefinition w1 = new RowDefinition();
            w1.Height = GridLength.Auto;
            RowDefinition w2 = new RowDefinition();
            w2.Height = GridLength.Auto;
            wrapperGrid.RowDefinitions.Add(w1);
            wrapperGrid.RowDefinitions.Add(w2);


            Rectangle bubbleOutline = new Rectangle();
            bubbleOutline.Fill = UI_Utils.Instance.TextBoxBackground;
            Grid.SetRowSpan(bubbleOutline, 2);
            wrapperGrid.Children.Add(bubbleOutline);

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

                downloadProgress = new ProgressBar();
                downloadProgress.Height = 10;
                downloadProgress.Foreground = progressColor;
                downloadProgress.Minimum = 0;
                downloadProgress.MaxHeight = 100;
                Grid.SetRow(downloadProgress, 1);
                attachment.Children.Add(downloadProgress);
            }
            else
            {
                MessageText = new LinkifiedTextBoxReceive();
                MessageText.Width = 340;
                MessageText.Foreground = progressColor;
                Binding messageTextBinding = new Binding("Text");
                MessageText.SetBinding(LinkifiedTextBoxReceive.TextProperty, messageTextBinding);
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