using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace windows_client.utils
{
    public class UI_Utils
    {
        #region private variables
        private static SolidColorBrush textBoxBackground;
        private static SolidColorBrush smsBackground;
        private static SolidColorBrush hikeMsgBackground;
        private static BitmapImage onHikeImage;
        private static BitmapImage notOnHikeImage;
        private static BitmapImage playIcon;
        private static BitmapImage audioAttachment;
        private static BitmapImage httpFailed;
        private static BitmapImage typingNotificationBitmap;
        private static BitmapImage sent;
        private static BitmapImage delivered;
        private static BitmapImage read;
        private static BitmapImage defaultAvatarBitmapImage;
        #endregion

        #region public static properties
        public static SolidColorBrush TextBoxBackground
        {
            get
            {
                if (textBoxBackground == null)
                    textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
                return textBoxBackground;
            }
        }

        public static SolidColorBrush SmsBackground
        {
            get
            {
                if (smsBackground == null)
                    smsBackground = new SolidColorBrush(Color.FromArgb(255, 219, 242, 207));
                return smsBackground;
            }
        }

        public static SolidColorBrush HikeMsgBackground
        {
            get
            {
                if (hikeMsgBackground == null)
                    hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 47, 152, 218));
                return hikeMsgBackground;
            }
        }

        public static BitmapImage OnHikeImage
        {
            get
            {
                if (onHikeImage == null)
                    onHikeImage = new BitmapImage(new Uri("/View/images/ic_hike_user.png", UriKind.Relative));
                return onHikeImage;
            }
        }

        public static BitmapImage NotOnHikeImage
        {
            get
            {
                if (notOnHikeImage == null)
                    notOnHikeImage = new BitmapImage(new Uri("/View/images/ic_sms_user.png", UriKind.Relative));
                return notOnHikeImage;
            }
        }

        public static BitmapImage PlayIcon
        {
            get
            {
                if (playIcon == null)
                    playIcon = new BitmapImage(new Uri("/View/images/play_icon.png", UriKind.Relative));
                return playIcon;
            }
        }

        public static BitmapImage AudioAttachment
        {
            get
            {
                if (audioAttachment == null)
                    audioAttachment = new BitmapImage(new Uri("/View/images/audio_file_icon.png", UriKind.Relative));
                return audioAttachment;
            }
        }

        public static BitmapImage HttpFailed
        {
            get
            {
                if (httpFailed == null)
                    httpFailed = new BitmapImage(new Uri("/View/images/error_icon.png", UriKind.Relative));
                return httpFailed;
            }
        }

        public static BitmapImage TypingNotificationBitmap
        {
            get
            {
                if (typingNotificationBitmap == null)
                    typingNotificationBitmap = new BitmapImage(new Uri("/View/images/typing.png", UriKind.Relative));
                return typingNotificationBitmap;
            }
        }

        public static BitmapImage Sent
        {
            get
            {
                if (sent == null)
                    sent = new BitmapImage(new Uri("/View/images/ic_sent.png", UriKind.Relative));
                return sent;
            }
        }

        public static BitmapImage Delivered
        {
            get
            {
                if (delivered == null)
                    delivered = new BitmapImage(new Uri("/View/images/ic_delivered.png", UriKind.Relative));
                return delivered;
            }
        }

        public static BitmapImage Read
        {
            get
            {
                if (read == null)
                    read = new BitmapImage(new Uri("/View/images/ic_read.png", UriKind.Relative));
                return read;
            }
        }

        public static BitmapImage DefaultAvatarBitmapImage
        {
            get
            {
                if (defaultAvatarBitmapImage == null)
                    defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/ic_avatar0.png", UriKind.Relative));
                return defaultAvatarBitmapImage;
            }
        }
        #endregion

        public static Paragraph Linkify(string message)
        {
            MatchCollection matchCollection = SmileyParser.SmileyPattern.Matches(message);
            var p = new Paragraph();
            int startIndex = 0;
            int endIndex = -1;
            int maxCount = matchCollection.Count < HikeConstants.MAX_EMOTICON_SUPPORTED ? matchCollection.Count : HikeConstants.MAX_EMOTICON_SUPPORTED;

            for (int i = 0; i < maxCount; i++)
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
