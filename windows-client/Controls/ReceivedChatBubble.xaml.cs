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

        public ReceivedChatBubble(ConvMessage cm, ContextMenu menu)
            : base(cm, menu)
        {
            // Required to initialize variables
            InitializeComponent();
            initializeBasedOnState(cm.HasAttachment);
            if (cm.FileAttachment!=null && (cm.FileAttachment.ContentType.Contains("video") || (cm.FileAttachment.ContentType.Contains("image"))))
            {
                byte[] imageBytes;
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_THUMBNAILS + "/" + 
                    cm.Msisdn + "/" + Convert.ToString(cm.MessageId),
                    out imageBytes);

                using (var memStream = new MemoryStream(imageBytes))
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
                    this.downloadProgress.Visibility = Visibility.Collapsed;
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