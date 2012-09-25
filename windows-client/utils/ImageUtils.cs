using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows;

namespace windows_client.utils
{
    public class UI_Utils
    {
        #region private variables
        private SolidColorBrush textBoxBackground;
        private SolidColorBrush smsBackground;
        private SolidColorBrush hikeMsgBackground;
        private SolidColorBrush walkThroughSelectedColumn;
        private SolidColorBrush walkThroughUnselectedColumn;
        private BitmapImage onHikeImage;
        private BitmapImage notOnHikeImage;
        private BitmapImage chatAcceptedImage;
        private BitmapImage playIcon;
        private BitmapImage downloadIcon;
        private BitmapImage audioAttachment;
        private BitmapImage httpFailed;
        private BitmapImage typingNotificationBitmap;
        private BitmapImage sent;
        private BitmapImage delivered;
        private BitmapImage read;
        private BitmapImage trying;
        private BitmapImage defaultAvatarBitmapImage;
        private BitmapImage defaultGroupImage;
        private BitmapImage waiting;
        private BitmapImage reward;
        private BitmapImage participantLeft;
        private SolidColorBrush receiveMessageForeground;
        private Thickness convListEmoticonMargin;


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
                    smsBackground = new SolidColorBrush(Color.FromArgb(255, 163, 210, 80));
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

        public SolidColorBrush WalkThroughSelectedColumn
        {
            get
            {
                if (walkThroughSelectedColumn == null)
                    walkThroughSelectedColumn = new SolidColorBrush(Color.FromArgb(255, 0x76, 0x76, 0x76));
                return walkThroughSelectedColumn;
            }
        }

        public SolidColorBrush WalkThroughUnselectedColumn
        {
            get
            {
                if (walkThroughUnselectedColumn == null)
                    walkThroughUnselectedColumn = new SolidColorBrush(Color.FromArgb(255, 0xe8, 0xe9, 0xe9));
                return walkThroughUnselectedColumn;
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
                    notOnHikeImage = new BitmapImage(new Uri("/View/images/chat_invited_green.png", UriKind.Relative));
                return notOnHikeImage;
            }
        }

        public BitmapImage ChatAcceptedImage
        {
            get
            {
                if (chatAcceptedImage == null)
                    chatAcceptedImage = new BitmapImage(new Uri("/View/images/chat_invited_green.png", UriKind.Relative));
                return chatAcceptedImage;
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

        public BitmapImage DownloadIcon
        {
            get
            {
                if (downloadIcon == null)
                    downloadIcon = new BitmapImage(new Uri("/View/images/download_icon.png", UriKind.Relative));
                return downloadIcon;
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

        public BitmapImage Trying
        {
            get
            {
                if (trying == null)
                    trying = new BitmapImage(new Uri("/View/images/trying_icon.png", UriKind.Relative));
                return trying;
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

        public SolidColorBrush ReceiveMessageForeground
        {
            get
            {
                if (receiveMessageForeground == null)
                    receiveMessageForeground = new SolidColorBrush(Color.FromArgb(255, 83, 83, 83));
                return receiveMessageForeground;
            }
        }

        public Thickness ConvListEmoticonMargin
        {
            get
            { 
                if(convListEmoticonMargin == null)
                    convListEmoticonMargin = new Thickness(0, 5, 0, 0);
                return convListEmoticonMargin;
            }
        }

        #endregion
    }
}
