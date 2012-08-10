using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO;

namespace windows_client.utils
{
    public class UI_Utils
    {
        private BitmapImage onHikeImage = null;
        private BitmapImage notOnHikeImage = null;
        private BitmapImage defaultAvatarBitmapImage = null;
        private Dictionary<string, BitmapImage> imageCache = null;
        private Dictionary<string, bool> numbersWithDefaultImage = null;

        private static volatile UI_Utils instance = null;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private UI_Utils()
        {
            onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
            notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));
            defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
            imageCache = new Dictionary<string, BitmapImage>();
            numbersWithDefaultImage = new Dictionary<string, bool>();
        }

        public static UI_Utils Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new UI_Utils();
                    }
                }

                return instance;
            }
        }

        public BitmapImage NotOnHikeImage
        {
            get
            {
                return notOnHikeImage;
            }
        }

        public BitmapImage OnHikeImage
        {
            get
            {
                return onHikeImage;
            }
        }

        public BitmapImage DefaultAvatarBitmapImage
        {
            get
            {
                return defaultAvatarBitmapImage;
            }
        }

        public Dictionary<string, BitmapImage> ImageCache
        {
            get
            {
                return imageCache;
            }
        }

        public void updateImageInCache(string msisdn, byte[] imageBytes)
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

        public BitmapImage getBitMapImage(string msisdn)
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
                numbersWithDefaultImage.Add(msisdn, false);
                return defaultAvatarBitmapImage;
            }
            MemoryStream memStream = new MemoryStream((byte[])thumbnail.Avatar);
            memStream.Seek(0, SeekOrigin.Begin);
            BitmapImage empImage = new BitmapImage();
            empImage.CreateOptions = BitmapCreateOptions.BackgroundCreation;
            empImage.SetSource(memStream);
            imageCache[msisdn] = empImage;
            return empImage;
        }

    }
}
