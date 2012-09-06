using System;
using System.Windows;
using windows_client.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;
using windows_client.DbUtils;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {

        public ReceivedChatBubble(ConvMessage cm, Dictionary<string, RoutedEventHandler> contextMenuDictionary)
            : base(cm, contextMenuDictionary)
        {
            // Required to initialize variables
            InitializeComponent();
            if (cm.FileAttachment!=null && (cm.FileAttachment.ContentType.Contains("video") || (cm.FileAttachment.ContentType.Contains("image"))))
            {
                byte[] imageBytes;
                MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_THUMBNAILS + "/" + 
                    cm.Msisdn + "/" + Convert.ToString(cm.MessageId),
                    out imageBytes);

                MemoryStream memStream = new MemoryStream(imageBytes);
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage fileThumbnail = new BitmapImage();
                fileThumbnail.SetSource(memStream);
                this.MessageImage.Source = fileThumbnail;
            }
        }



    }
}