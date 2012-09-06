using System;
using System.Windows;
using windows_client.Model;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.IO;

namespace windows_client.Controls
{
    public partial class ReceivedChatBubble : MyChatBubble
    {

        public ReceivedChatBubble(ConvMessage cm, Dictionary<string, RoutedEventHandler> contextMenuDictionary)
            : base(cm, contextMenuDictionary)
        {
            // Required to initialize variables
            InitializeComponent();
            if (cm.HasAttachment)
            {
                byte[] imageBytes;
                Attachment.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + 
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