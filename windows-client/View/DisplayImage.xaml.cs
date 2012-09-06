using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using System.Windows.Media.Imaging;
using System.IO;

namespace windows_client.View
{
    public partial class DisplayImage : PhoneApplicationPage
    {
        private byte[] imageBytes;
        public DisplayImage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (PhoneApplicationService.Current.State.ContainsKey("objForFileTransferChatThread"))
            {
                //long messsageId = (long)PhoneApplicationService.Current.State["objForFileTransferChatThread"];
                //Attachment.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" +
                //    cm.Msisdn + "/" + Convert.ToString(messsageId) + "_large", 
                //    out imageBytes);
                //if (imageBytes != null)
                //{
                //    MemoryStream memStream = new MemoryStream(imageBytes);
                //    memStream.Seek(0, SeekOrigin.Begin);
                //    BitmapImage fileThumbnail = new BitmapImage();
                //    fileThumbnail.SetSource(memStream);
                //    this.FileImage.Source = fileThumbnail;
                //}
            }
        }
    }
}