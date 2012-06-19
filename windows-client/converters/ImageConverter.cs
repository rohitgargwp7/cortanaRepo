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
using System.IO;
using System.Windows.Media.Imaging;
using System.Resources;
using System.Windows.Resources;
using System.Reflection;

namespace windows_client.converters
{
    public class ImageConverter : IValueConverter
    {
        private static BitmapImage defaultBitmapImage;
        public ImageConverter()
        {
            if (defaultBitmapImage == null)
            {
                Uri uri = new Uri("/View/images/ic_avatar0.png", UriKind.Relative);
                defaultBitmapImage = new BitmapImage(uri);
            }
        }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return defaultBitmapImage;
            MemoryStream memStream = new MemoryStream((byte[])value);
            memStream.Seek(0, SeekOrigin.Begin);
            BitmapImage empImage = new BitmapImage();
            empImage.SetSource(memStream);
            return empImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
