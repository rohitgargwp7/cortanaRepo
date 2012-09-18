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
        private SolidColorBrush textBoxBackground;
        private SolidColorBrush smsBackground;
        private SolidColorBrush hikeMsgBackground;
        private BitmapImage onHikeImage;
        private BitmapImage notOnHikeImage;
        private BitmapImage playIcon;
        private BitmapImage audioAttachment;
        private BitmapImage httpFailed;
        private BitmapImage typingNotificationBitmap;
        private BitmapImage sent;
        private BitmapImage delivered;
        private BitmapImage read;
        private BitmapImage defaultAvatarBitmapImage;
        private BitmapImage defaultGroupImage;
        private BitmapImage waiting;
        private BitmapImage reward;
        private BitmapImage participantLeft;

        private static volatile UI_Utils instance = null;

        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

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
        #endregion

        #region public  properties
        public SolidColorBrush TextBoxBackground
        {
            get
            {
                if (textBoxBackground == null)
                    textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));
                return textBoxBackground;
            }
        }

        public SolidColorBrush SmsBackground
        {
            get
            {
                if (smsBackground == null)
                    smsBackground = new SolidColorBrush(Color.FromArgb(255, 219, 242, 207));
                return smsBackground;
            }
        }

        public SolidColorBrush HikeMsgBackground
        {
            get
            {
                if (hikeMsgBackground == null)
                    hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 47, 152, 218));
                return hikeMsgBackground;
            }
        }

        public BitmapImage OnHikeImage
        {
            get
            {
                if (onHikeImage == null)
                    onHikeImage = new BitmapImage(new Uri("/View/images/chat_joined_blue.png", UriKind.Relative));
                return onHikeImage;
            }
        }

        public BitmapImage NotOnHikeImage
        {
            get
            {
                if (notOnHikeImage == null)
                    notOnHikeImage = new BitmapImage(new Uri("/View/images/chat_joined_green.png", UriKind.Relative));
                return notOnHikeImage;
            }
        }

        public BitmapImage PlayIcon
        {
            get
            {
                if (playIcon == null)
                    playIcon = new BitmapImage(new Uri("/View/images/play_icon.png", UriKind.Relative));
                return playIcon;
            }
        }

        public BitmapImage AudioAttachment
        {
            get
            {
                if (audioAttachment == null)
                    audioAttachment = new BitmapImage(new Uri("/View/images/audio_file_icon.png", UriKind.Relative));
                return audioAttachment;
            }
        }

        public BitmapImage HttpFailed
        {
            get
            {
                if (httpFailed == null)
                    httpFailed = new BitmapImage(new Uri("/View/images/error_icon.png", UriKind.Relative));
                return httpFailed;
            }
        }

        public BitmapImage TypingNotificationBitmap
        {
            get
            {
                if (typingNotificationBitmap == null)
                    typingNotificationBitmap = new BitmapImage(new Uri("/View/images/typing.png", UriKind.Relative));
                return typingNotificationBitmap;
            }
        }

        public BitmapImage Sent
        {
            get
            {
                if (sent == null)
                    sent = new BitmapImage(new Uri("/View/images/ic_sent.png", UriKind.Relative));
                return sent;
            }
        }

        public BitmapImage Delivered
        {
            get
            {
                if (delivered == null)
                    delivered = new BitmapImage(new Uri("/View/images/ic_delivered.png", UriKind.Relative));
                return delivered;
            }
        }

        public BitmapImage Read
        {
            get
            {
                if (read == null)
                    read = new BitmapImage(new Uri("/View/images/ic_read.png", UriKind.Relative));
                return read;
            }
        }

        public BitmapImage DefaultAvatarBitmapImage
        {
            get
            {
                if (defaultAvatarBitmapImage == null)
                    defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/default_user.png", UriKind.Relative));
                return defaultAvatarBitmapImage;
            }
        }

        public BitmapImage DefaultGroupImage
        {
            get
            {
                if (defaultGroupImage == null)
                    defaultGroupImage = new BitmapImage(new Uri("/View/images/default_group.png", UriKind.Relative));
                return defaultGroupImage;
            }
        }

        public BitmapImage Waiting
        {
            get
            {
                if (waiting == null)
                    waiting = new BitmapImage(new Uri("/View/images/chat_waiting.png", UriKind.Relative));
                return waiting;
            }
        }

        public BitmapImage Reward
        {
            get
            {
                if (reward == null)
                    reward = new BitmapImage(new Uri("/View/images/chat_reward.png", UriKind.Relative));
                return reward;
            }
        }

        public BitmapImage ParticipantLeft
        {
            get
            {
                if (participantLeft == null)
                    participantLeft = new BitmapImage(new Uri("/View/images/chat_left.png", UriKind.Relative));
                return participantLeft;
            }
        }

        #endregion

        public Paragraph Linkify(string message)
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
