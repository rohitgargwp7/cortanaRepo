using System;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using windows_client.DbUtils;
using windows_client.Model;
using System.IO;
using System.Windows.Media;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace windows_client.utils
{
    public class UI_Utils
    {
        public static readonly SolidColorBrush whiteBackground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        public static readonly SolidColorBrush blackBackground = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
        public static readonly SolidColorBrush textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
        public static readonly SolidColorBrush smsBackground = new SolidColorBrush(Color.FromArgb(255, 219, 242, 207));
        public static readonly SolidColorBrush hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 47, 152, 218));

        private BitmapImage onHikeImage = null;
        private BitmapImage notOnHikeImage = null;
        private BitmapImage defaultAvatarBitmapImage = null;
        private BitmapImage sent = null;
        private BitmapImage delivered = null;
        private BitmapImage read = null;

        private static volatile UI_Utils instance = null;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private UI_Utils()
        {
            onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
            notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));
            defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
            sent = new BitmapImage(new Uri("/View/images/ic_sent.png", UriKind.Relative));
            delivered = new BitmapImage(new Uri("/View/images/ic_delivered.png", UriKind.Relative));
            read = new BitmapImage(new Uri("/View/images/ic_read.png", UriKind.Relative));
            onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
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
        public BitmapImage MessageReadBitmapImage
        {
            get
            {
                return read;
            }
        }

        public BitmapImage MessageDeliveredBitmapImage
        {
            get
            {
                return delivered;
            }
        }

        public BitmapImage MessageSentBitmapImage
        {
            get
            {
                return sent;
            }
        }

        public static Paragraph Linkify(string message)
        {
            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(message);
            var p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;

            for (int i = 0; i < matchCollection.Count; i++)
            {
                String emoticon = matchCollection[i].ToString();

                //Regex never returns an empty string. Still have added an extra check
                if (String.IsNullOrEmpty(emoticon))
                    continue;

                int index = matchCollection[i].Index;
                endIndex = index - 1;

                if (index > 0)
                {
                    Run r = new Run();
                    r.Text = message.Substring(startIndex, endIndex - startIndex + 1);
                    p.Inlines.Add(r);
                }

                startIndex = index + emoticon.Length;

                //TODO check if imgPath is null or not
                Image img = new Image();
                img.Source = SmileyParser.lookUpFromCache(emoticon);
                img.Height = 40;
                img.Width = 40;

                InlineUIContainer ui = new InlineUIContainer();
                ui.Child = img;
                p.Inlines.Add(ui);
            }
            if (startIndex < message.Length)
            {
                Run r2 = new Run();
                r2.Text = message.Substring(startIndex, message.Length - startIndex);
                p.Inlines.Add(r2);
            }
            return p;

        }

    }
}
