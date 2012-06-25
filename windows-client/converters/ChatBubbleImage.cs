using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using windows_client.Model;
using windows_client.DbUtils;
using System.IO;

namespace windows_client.converters
{
    public class ChatBubbleImage : IValueConverter
    {
        private static BitmapImage defaultBitmapImage;
        private static string msisdn;
        private static BitmapImage contactImage;


        public ChatBubbleImage()
        {
            if (defaultBitmapImage == null)
            {
                Uri uri = new Uri("/View/images/ic_avatar0.png", UriKind.Relative);
                defaultBitmapImage = new BitmapImage(uri);
            }
            if (contactImage == null)
            {
                contactImage = new BitmapImage();
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string inputMsisdn = (string)value;
            if (inputMsisdn == msisdn)
                return contactImage;
            msisdn = inputMsisdn;
            Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(msisdn);
            if (thumbnail != null)
            {
                MemoryStream memStream = new MemoryStream((byte[])thumbnail.Avatar);
                memStream.Seek(0, SeekOrigin.Begin);
                contactImage.SetSource(memStream);
                return contactImage;
            }
            else 
            {
                return defaultBitmapImage;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
