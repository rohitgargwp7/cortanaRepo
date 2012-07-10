using System;
using System.Windows;
using System.Windows.Data;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.DbUtils;
using windows_client.Model;

namespace windows_client.converters
{
    public class ImageConverter : IValueConverter
    {
        private static BitmapImage defaultBitmapImage;
        private static Dictionary<string, BitmapImage> imageCache;
        private static List<string> numbersWithDefaultImage;
        private static HikePubSub pubSub;

        public ImageConverter()
        {
            if (imageCache == null)
                imageCache = new Dictionary<string, BitmapImage>();
            if (numbersWithDefaultImage == null)
                numbersWithDefaultImage = new List<string>();
            if (defaultBitmapImage == null)
            {
                Uri uri = new Uri("/View/images/ic_avatar0.png", UriKind.Relative);
                defaultBitmapImage = new BitmapImage(uri);
            }
            pubSub = new HikePubSub();
        }

        public static void updateImageInCache(string msisdn, byte[] imageBytes)
        {

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (!numbersWithDefaultImage.Contains(msisdn) && !imageCache.ContainsKey(msisdn))
                    return;

                MemoryStream memStream = new MemoryStream(imageBytes);
                memStream.Seek(0, SeekOrigin.Begin);

                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                if (numbersWithDefaultImage.Contains(msisdn))
                {
                    numbersWithDefaultImage.Remove(msisdn);
                }
                else if (imageCache.ContainsKey(msisdn))
                {
                    imageCache.Remove(msisdn);
                }
                imageCache.Add(msisdn, empImage);
            });

        }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;

            string msisdn = (string)value;
            if (imageCache.ContainsKey(msisdn))
            {
                BitmapImage cachedImage;
                imageCache.TryGetValue(msisdn, out cachedImage);
                return cachedImage;
            }
            if (numbersWithDefaultImage.Contains(msisdn))
                return defaultBitmapImage;

            Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(msisdn);
            if (thumbnail == null)
            {
                numbersWithDefaultImage.Add(msisdn);
                return defaultBitmapImage;
            }
            MemoryStream memStream = new MemoryStream((byte[])thumbnail.Avatar);
            memStream.Seek(0, SeekOrigin.Begin);
            BitmapImage empImage = new BitmapImage();
            empImage.SetSource(memStream);
            imageCache[msisdn] = empImage;
            return empImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}
