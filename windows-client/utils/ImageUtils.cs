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
        public static BitmapImage defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
        public static Dictionary<string, BitmapImage> imageCache = new Dictionary<string, BitmapImage>();
        private static List<string> numbersWithDefaultImage = new List<string>();

        public static void updateImageInCache(string msisdn, byte[] imageBytes)
        {
           
                if (imageBytes == null)
                    return;

                MemoryStream memStream = new MemoryStream(imageBytes);
                memStream.Seek(0, SeekOrigin.Begin);
                BitmapImage empImage = new BitmapImage();
                empImage.SetSource(memStream);
                imageCache[msisdn] = empImage;
        }

        public static BitmapImage getBitMapImage(string msisdn)
        {
            if (imageCache.ContainsKey(msisdn))
            {
                BitmapImage cachedImage;
                imageCache.TryGetValue(msisdn, out cachedImage);
                return cachedImage;
            }
            return defaultAvatarBitmapImage;
        }

    }
}
