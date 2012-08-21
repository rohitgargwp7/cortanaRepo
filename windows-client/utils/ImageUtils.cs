using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO;
using System.Windows.Media;

namespace windows_client.utils
{
    public class UI_Utils
    {
        private BitmapImage onHikeImage = null;
        private BitmapImage notOnHikeImage = null;
        private BitmapImage defaultAvatarBitmapImage = null;

        public readonly SolidColorBrush smsBackground = new SolidColorBrush(Color.FromArgb(255, 163, 210, 80));
        public readonly SolidColorBrush hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 27, 161, 226));

        private static volatile UI_Utils instance = null;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private UI_Utils()
        {
            onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
            notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));
            defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
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

    }
}
