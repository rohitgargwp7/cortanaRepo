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
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO;

namespace windows_client.utils
{
    public class UserInterfaceUtils
    {
        public readonly static BitmapImage onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
        public readonly static BitmapImage notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));       
        private static BitmapImage defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
        private static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();
        private static Dictionary<string, bool> numbersWithDefaultImage = new Dictionary<string, bool>();

        public static BitmapImage DefaultAvatarBitmapImage
        {
            get
            {
                return defaultAvatarBitmapImage;
            }
        }

        public static Dictionary<string, BitmapImage> ImageCache
        {
            get
            {
                return imageCache;
            }
        }

        public static void updateImageInCache(string msisdn, byte[] imageBytes)
        {
           
                if (!numbersWithDefaultImage.ContainsKey(msisdn) && !imageCache.ContainsKey(msisdn))
                    return;

                MemoryStream memStream = new MemoryStream(imageBytes);
                memStream.Seek(0, SeekOrigin.Begin);

                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                if (numbersWithDefaultImage.ContainsKey(msisdn))
                {
                    numbersWithDefaultImage.Remove(msisdn);
                }
                else if (imageCache.ContainsKey(msisdn))
                {
                    imageCache.Remove(msisdn);
                }
                imageCache.Add(msisdn, empImage);

        }

        public static BitmapImage getBitMapImage(string msisdn)
        {
            if (imageCache.ContainsKey(msisdn))
            {
                BitmapImage cachedImage;
                imageCache.TryGetValue(msisdn, out cachedImage);
                return cachedImage;
            }
            if (numbersWithDefaultImage.ContainsKey(msisdn))
                return defaultAvatarBitmapImage;

            Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(msisdn);
            if (thumbnail == null)
            {
                numbersWithDefaultImage.Add(msisdn,false);
                return defaultAvatarBitmapImage;
            }
            MemoryStream memStream = new MemoryStream((byte[])thumbnail.Avatar);
            memStream.Seek(0, SeekOrigin.Begin);
            BitmapImage empImage = new BitmapImage();
            empImage.SetSource(memStream);
            imageCache[msisdn] = empImage;
            return empImage;
        }

    }
}
