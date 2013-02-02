using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using windows_client.DbUtils;

namespace windows_client.utils
{
    public class UI_Utils
    {
        #region private variables
        private SolidColorBrush textBoxBackground;
        private SolidColorBrush lastMsgForeground;
        private SolidColorBrush smsBackground;
        private SolidColorBrush hikeMsgBackground;
        private SolidColorBrush walkThroughSelectedColumn;
        private SolidColorBrush walkThroughUnselectedColumn;
        private SolidColorBrush black;
        private SolidColorBrush white;
        private SolidColorBrush groupChatHeaderColor;
        private SolidColorBrush signUpForeground;
        private SolidColorBrush receivedChatBubbleColor;
        private SolidColorBrush editProfileForeground;
        private SolidColorBrush receivedChatBubbleTimestamp;
        private SolidColorBrush hikeSentChatBubbleTimestamp;
        private SolidColorBrush smsSentChatBubbleTimestamp;
        private SolidColorBrush receivedChatBubbleProgress;
        private SolidColorBrush phoneThemeColor;
        private BitmapImage onHikeImage;
        private BitmapImage notOnHikeImage;
        private BitmapImage chatAcceptedImage;
        private BitmapImage playIcon;
        private BitmapImage downloadIcon;
        private BitmapImage audioAttachmentReceive;
        private BitmapImage audioAttachmentSend;
        private BitmapImage httpFailed;
        private BitmapImage typingNotificationBitmap;
        private BitmapImage emptyImage;
        private BitmapImage sent;
        private BitmapImage delivered;
        private BitmapImage read;
        private BitmapImage trying;
        private BitmapImage unread;
        private BitmapImage waiting;
        private BitmapImage reward;
        private BitmapImage participantLeft;
        private BitmapImage nudgeSend;
        private BitmapImage nudgeReceived;
        private BitmapImage textStatusImage;
        private BitmapImage friendRequestImage;
        private BitmapImage[] defaultUserAvatars = new BitmapImage[7];
        private BitmapImage[] defaultGroupAvatars = new BitmapImage[7];
        private string[] defaultAvatarFileNames;

        private SolidColorBrush receiveMessageForeground;
        private Thickness convListEmoticonMargin = new Thickness(0, 5, 0, 0);
        private Thickness chatThreadKeyPadUpMargin = new Thickness(0, 315, 15, 0);
        private Thickness chatThreadKeyPadDownMargin = new Thickness(0, 0, 15, 0);
        private FontFamily groupChatMessageHeader;
        private FontFamily messageText;

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
                        {
                            instance = new UI_Utils();
                        }
                    }
                }
                return instance;
            }
        }
        #endregion

        private UI_Utils()
        {
            defaultAvatarFileNames = new string[14];
            defaultAvatarFileNames[0] = "Digital.jpg";
            defaultAvatarFileNames[1] = "Sneakers.jpg";
            defaultAvatarFileNames[2] = "Space.jpg";
            defaultAvatarFileNames[3] = "Beach.jpg";
            defaultAvatarFileNames[4] = "Candy.jpg";
            defaultAvatarFileNames[5] = "Cocktail.jpg";
            defaultAvatarFileNames[6] = "Coffee.jpg";
            defaultAvatarFileNames[7] = "RedPeople.jpg";
            defaultAvatarFileNames[8] = "TealPeople.jpg";
            defaultAvatarFileNames[9] = "BluePeople.jpg";
            defaultAvatarFileNames[10] = "CoffeePeople.jpg";
            defaultAvatarFileNames[11] = "EarthyPeople.jpg";
            defaultAvatarFileNames[12] = "GreenPeople.jpg";
            defaultAvatarFileNames[13] = "PinkPeople.jpg";
        }

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

        public SolidColorBrush LastMsgForeground
        {
            get
            {
                if (lastMsgForeground == null)
                    lastMsgForeground = new SolidColorBrush(Colors.Gray);
                return lastMsgForeground;
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

        public SolidColorBrush Black
        {
            get
            {
                if (black == null)
                    black = new SolidColorBrush(Color.FromArgb(255, 0x0, 0x0, 0x0));
                return black;
            }
        }

        public SolidColorBrush White
        {
            get
            {
                if (white == null)
                    white = new SolidColorBrush(Color.FromArgb(255, 0xff, 0xff, 0xff));
                return white;
            }
        }

        public SolidColorBrush GroupChatHeaderColor
        {
            get
            {
                if (groupChatHeaderColor == null)
                    if (Utils.isDarkTheme())
                        groupChatHeaderColor = UI_Utils.Instance.White;
                    else
                        groupChatHeaderColor = new SolidColorBrush(Color.FromArgb(255, 0x53, 0x53, 0x53));
                return groupChatHeaderColor;
            }
        }

        public SolidColorBrush SignUpForeground
        {
            get
            {
                if (signUpForeground == null)
                    signUpForeground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
                return signUpForeground;
            }
        }

        public SolidColorBrush ReceivedChatBubbleColor
        {
            get
            {
                if (receivedChatBubbleColor == null)
                {
                    if (Utils.isDarkTheme())
                        receivedChatBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0x50, 0x50, 0x50));
                    else
                        receivedChatBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));
                }
                return receivedChatBubbleColor;
            }
        }

        public SolidColorBrush EditProfileForeground
        {
            get
            {
                if (editProfileForeground == null)
                {
                    if (Utils.isDarkTheme())
                        editProfileForeground = new SolidColorBrush(Color.FromArgb(255, 0xa8, 0xa8, 0xa8));
                    else
                        editProfileForeground = new SolidColorBrush(Color.FromArgb(255, 0x8d, 0x8d, 0x8d));
                }
                return editProfileForeground;
            }
        }

        public SolidColorBrush ReceivedChatBubbleTimestamp
        {
            get
            {
                if (receivedChatBubbleTimestamp == null)
                {
                    if (Utils.isDarkTheme())
                        receivedChatBubbleTimestamp = new SolidColorBrush(Color.FromArgb(255, 0xbc, 0xbc, 0xbc));
                    else
                        receivedChatBubbleTimestamp = new SolidColorBrush(Color.FromArgb(255, 0x83, 0x83, 0x83));
                }
                return receivedChatBubbleTimestamp;
            }
        }

        public SolidColorBrush HikeSentChatBubbleTimestamp
        {
            get
            {
                if (hikeSentChatBubbleTimestamp == null)
                {
                    hikeSentChatBubbleTimestamp = new SolidColorBrush(Color.FromArgb(255, 0xb4, 0xd9, 0xf3));
                }
                return hikeSentChatBubbleTimestamp;
            }
        }

        public SolidColorBrush SMSSentChatBubbleTimestamp
        {
            get
            {
                if (smsSentChatBubbleTimestamp == null)
                {
                    smsSentChatBubbleTimestamp = new SolidColorBrush(Color.FromArgb(255, 0xd6, 0xea, 0xb9));
                }
                return smsSentChatBubbleTimestamp;
            }
        }

        public SolidColorBrush ReceivedChatBubbleProgress
        {
            get
            {
                if (receivedChatBubbleProgress == null)
                {
                    if (Utils.isDarkTheme())
                        receivedChatBubbleProgress = new SolidColorBrush(Color.FromArgb(255, 0xb8, 0xb8, 0xb8));
                    else
                        receivedChatBubbleProgress = new SolidColorBrush(Color.FromArgb(255, 0x33, 0x33, 0x33));
                }
                return receivedChatBubbleProgress;
            }
        }

        public SolidColorBrush PhoneThemeColor
        {
            get
            {
                if (phoneThemeColor == null)
                {
                    phoneThemeColor = new SolidColorBrush((Color)Application.Current.Resources["PhoneAccentColor"]); ;
                }
                return phoneThemeColor;
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

        public BitmapImage AudioAttachmentReceive
        {
            get
            {
                if (audioAttachmentReceive == null)
                    audioAttachmentReceive = new BitmapImage(new Uri("/View/images/audio_file_icon.png", UriKind.Relative));
                return audioAttachmentReceive;
            }
        }


        public BitmapImage AudioAttachmentSend
        {
            get
            {
                if (audioAttachmentSend == null)
                    audioAttachmentSend = new BitmapImage(new Uri("/View/images/audio_file_icon_white.png", UriKind.Relative));
                return audioAttachmentSend;
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

        public BitmapImage EmptyImage
        {
            get
            {
                if (emptyImage == null)
                    emptyImage = new BitmapImage(new Uri("/View/images/emptyImage.png", UriKind.Relative));
                return emptyImage;
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

        public BitmapImage Unread
        {
            get
            {
                if (unread == null)
                    unread = new BitmapImage(new Uri("/View/images/new_message.png", UriKind.Relative));
                return unread;
            }
        }


        //public BitmapImage DefaultAvatarBitmapImage
        //{
        //    get
        //    {
        //        if (defaultAvatarBitmapImage == null)
        //            defaultAvatarBitmapImage = new BitmapImage(new Uri("/View/images/default_user.png", UriKind.Relative));
        //        return defaultAvatarBitmapImage;
        //    }
        //}

        //public BitmapImage DefaultGroupImage
        //{
        //    get
        //    {
        //        if (defaultGroupImage == null)
        //            defaultGroupImage = new BitmapImage(new Uri("/View/images/default_group.png", UriKind.Relative));
        //        return defaultGroupImage;
        //    }
        //}

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

        public BitmapImage IntUserBlocked
        {
            get
            {
                if (reward == null)
                    reward = new BitmapImage(new Uri("/View/images/chat_sms_error.png", UriKind.Relative));
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

        public BitmapImage NudgeSent
        {
            get
            {
                if (nudgeSend == null)
                    nudgeSend = new BitmapImage(new Uri("/View/images/nudge_sent.png", UriKind.Relative));
                return nudgeSend;
            }
        }
        
        public BitmapImage NudgeReceived
        {
            get
            {
                if (nudgeReceived == null)
                    nudgeReceived = new BitmapImage(new Uri("/View/images/nudge_received.png", UriKind.Relative));
                return nudgeReceived;
            }
        }

        public BitmapImage TextStatusImage
        {
            get
            {
                if (textStatusImage == null)
                    textStatusImage = new BitmapImage(new Uri("/View/images/timeline_status.png", UriKind.Relative));
                return textStatusImage;
            }
        }
        public BitmapImage FriendRequestImage
        {
            get
            {
                if (friendRequestImage == null)
                    friendRequestImage = new BitmapImage(new Uri("/View/images/timeline_friend.png", UriKind.Relative));
                return friendRequestImage;
            }
        }

        public SolidColorBrush ReceiveMessageForeground
        {
            get
            {
                if (receiveMessageForeground == null)
                {
                    if (Utils.isDarkTheme())
                        receiveMessageForeground = this.White;
                    else
                        receiveMessageForeground = new SolidColorBrush(Color.FromArgb(255, 83, 83, 83));
                }
                return receiveMessageForeground;
            }
        }



        public FontFamily GroupChatMessageHeader
        {
            get
            {
                if (groupChatMessageHeader == null)
                    groupChatMessageHeader = new FontFamily("Segoe WP Semibold");
                return groupChatMessageHeader;
            }
        }

        public FontFamily MessageText
        {
            get
            {
                if (messageText == null)
                    messageText = new FontFamily("Segoe WP SemiLight");
                return messageText;
            }
        }


        public Thickness ConvListEmoticonMargin
        {
            get
            {
                return convListEmoticonMargin;
            }
        }

        public Thickness ChatThreadKeyPadUpMargin
        {
            get
            {
                return chatThreadKeyPadUpMargin;
            }
        }

        public Thickness ChatThreadKeyPadDownMargin
        {
            get
            {
                return chatThreadKeyPadDownMargin;
            }
        }

        #endregion

        #region DEFAULT AVATARS
        private int computeHash(string msisdn)
        {
            string last3Digits = msisdn.Substring(msisdn.Length - 3);
            int sumOfCodes = last3Digits[0] + last3Digits[1] + last3Digits[2];
            return sumOfCodes % 7;
        }

        public string getDefaultAvatarFileName(string msisdn, bool isGroup)
        {
            int index = computeHash(msisdn);
            index += (isGroup ? 7 : 0);
            return defaultAvatarFileNames[index];
        }

        public BitmapImage getDefaultAvatar(string msisdn)
        {
            int index = computeHash(msisdn);
            if (defaultUserAvatars[index] == null)
            {
                switch (index)
                {
                    case 0:
                        defaultUserAvatars[0] = new BitmapImage(new Uri("/View/images/avatars/Digital.png", UriKind.Relative));
                        break;
                    case 1:
                        defaultUserAvatars[1] = new BitmapImage(new Uri("/View/images/avatars/Sneakers.png", UriKind.Relative));
                        break;
                    case 2:
                        defaultUserAvatars[2] = new BitmapImage(new Uri("/View/images/avatars/Space.png", UriKind.Relative));
                        break;
                    case 3:
                        defaultUserAvatars[3] = new BitmapImage(new Uri("/View/images/avatars/Beach.png", UriKind.Relative));
                        break;
                    case 4:
                        defaultUserAvatars[4] = new BitmapImage(new Uri("/View/images/avatars/Candy.png", UriKind.Relative));
                        break;
                    case 5:
                        defaultUserAvatars[5] = new BitmapImage(new Uri("/View/images/avatars/Cocktail.png", UriKind.Relative));
                        break;
                    default:
                        defaultUserAvatars[6] = new BitmapImage(new Uri("/View/images/avatars/Coffee.png", UriKind.Relative));
                        break;
                }
            }
            return defaultUserAvatars[index];
        }

        public BitmapImage getDefaultGroupAvatar(string groupId)
        {
            int index = computeHash(groupId);
            if (defaultGroupAvatars[index] == null)
            {
                switch (index)
                {
                    case 0:
                        defaultGroupAvatars[0] = new BitmapImage(new Uri("/View/images/avatars/RedPeople.png", UriKind.Relative));
                        break;
                    case 1:
                        defaultGroupAvatars[1] = new BitmapImage(new Uri("/View/images/avatars/TealPeople.png", UriKind.Relative));
                        break;
                    case 2:
                        defaultGroupAvatars[2] = new BitmapImage(new Uri("/View/images/avatars/BluePeople.png", UriKind.Relative));
                        break;
                    case 3:
                        defaultGroupAvatars[3] = new BitmapImage(new Uri("/View/images/avatars/CoffeePeople.png", UriKind.Relative));
                        break;
                    case 4:
                        defaultGroupAvatars[4] = new BitmapImage(new Uri("/View/images/avatars/EarthyPeople.png", UriKind.Relative));
                        break;
                    case 5:
                        defaultGroupAvatars[5] = new BitmapImage(new Uri("/View/images/avatars/GreenPeople.png", UriKind.Relative));
                        break;
                    default:
                        defaultGroupAvatars[6] = new BitmapImage(new Uri("/View/images/avatars/PinkPeople.png", UriKind.Relative));
                        break;
                }
            }
            return defaultGroupAvatars[index];
        }
        #endregion

        public BitmapImage createImageFromBytes(byte[] imagebytes)
        {
            BitmapImage bitmapImage = null;
            using (var memStream = new MemoryStream(imagebytes))
            {
                memStream.Seek(0, SeekOrigin.Begin);
                bitmapImage = new BitmapImage();
                bitmapImage.SetSource(memStream);
            }
            return bitmapImage;
        }

        public BitmapImage getUserProfileThumbnail(string msisdn)
        {
            byte[] profileImageBytes = MiscDBUtil.getThumbNailForMsisdn(msisdn);
            if (profileImageBytes != null && profileImageBytes.Length > 0)
                return createImageFromBytes(profileImageBytes);
            if (Utils.isGroupConversation(msisdn))
                return getDefaultGroupAvatar(msisdn);
            return getDefaultAvatar(msisdn);
        }
    }
}
