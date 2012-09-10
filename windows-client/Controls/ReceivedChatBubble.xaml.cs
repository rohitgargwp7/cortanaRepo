using System;
using System.Windows;
using windows_client.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;
using Microsoft.Phone.Controls;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {

        public ReceivedChatBubble(ConvMessage cm)
            : base(cm)
        {
            // Required to initialize variables
            InitializeComponent();
//            initializeBasedOnState(cm.HasAttachment);
            if (cm.FileAttachment != null && cm.FileAttachment.Thumbnail != null && cm.FileAttachment.Thumbnail.Length != 0)
            {
                using (var memStream = new MemoryStream(cm.FileAttachment.Thumbnail))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    BitmapImage fileThumbnail = new BitmapImage();
                    fileThumbnail.SetSource(memStream);
                    this.MessageImage.Source = fileThumbnail;
                }
                this.MessageText.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.MessageImage.Visibility = Visibility.Collapsed;
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

        //private void initializeBasedOnState(bool hasAttachment)
        //{
        //    if (hasAttachment)
        //        this.MessageText.Visibility = Visibility.Collapsed;
        //    else
        //        this.attachment.Visibility = Visibility.Collapsed;
        //}
    }
}