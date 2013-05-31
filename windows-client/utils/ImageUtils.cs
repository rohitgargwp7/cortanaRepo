using System;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using System.IO;
using windows_client.DbUtils;
using System.Collections.Generic;
using System.Diagnostics;

namespace windows_client.utils
{
    public class UI_Utils
    {
        #region PRIVATE UI VARIABLES

        private SolidColorBrush textBoxBackground;
        private SolidColorBrush lastMsgForeground;
        private SolidColorBrush smsBackground;
        private SolidColorBrush hikeMsgBackground;
        private SolidColorBrush walkThroughSelectedColumn;
        private SolidColorBrush walkThroughUnselectedColumn;
        private SolidColorBrush black;
        private SolidColorBrush white;
        private SolidColorBrush btnGrayBackground;
        private SolidColorBrush btnGrayForeground;
        private SolidColorBrush groupChatHeaderColor;
        private SolidColorBrush signUpForeground;
        private SolidColorBrush receivedChatBubbleColor;
        private SolidColorBrush statusBubbleColor;
        private SolidColorBrush editProfileForeground;
        private SolidColorBrush receivedChatBubbleTimestamp;
        private SolidColorBrush hikeSentChatBubbleTimestamp;
        private SolidColorBrush smsSentChatBubbleTimestamp;
        private SolidColorBrush receivedChatBubbleProgress;
        private SolidColorBrush phoneThemeColor;
        private SolidColorBrush statusTextForeground;
        private SolidColorBrush deleteGreyBackground;
        private SolidColorBrush deleteBlackBackground;
        private BitmapImage onHikeImage;
        private BitmapImage notOnHikeImage;
        private BitmapImage chatAcceptedImage;
        private BitmapImage playIcon;
        private BitmapImage pauseIcon;
        private BitmapImage audioMicIcon;
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
        private BitmapImage waiting;
        private BitmapImage reward;
        private BitmapImage chatSmsError;
        private BitmapImage grpNameOrPicChanged;
        private BitmapImage participantLeft;
        private BitmapImage nudgeSend;
        private BitmapImage nudgeReceived;
        private BitmapImage textStatusImage;
        private BitmapImage friendRequestImage;
        private BitmapImage profilePicStatusImage;
        private BitmapImage noNewNotificationImage;
        private BitmapImage newNotificationImage;
        private BitmapImage contactIcon;
        private BitmapImage whiteContactIcon;
        private BitmapImage facebookDisabledIcon;
        private BitmapImage facebookEnabledIcon;
        private BitmapImage twitterDisabledIcon;
        private BitmapImage twitterEnabledIcon;
        private BitmapImage moodDisabledIcon;
        private BitmapImage moodEnabledIcon;
        private BitmapImage userProfileLockImage;
        private BitmapImage userProfileInviteImage;
        private BitmapImage userProfileStockImage;

        private BitmapImage[] defaultUserAvatars = new BitmapImage[7];
        private BitmapImage[] defaultGroupAvatars = new BitmapImage[7];
        private string[] defaultAvatarFileNames;

        private SolidColorBrush receiveMessageForeground;
        private Thickness convListEmoticonMargin = new Thickness(0, 5, 0, 0);
        private Thickness chatThreadKeyPadUpMargin = new Thickness(0, 315, 15, 0);
        private Thickness chatThreadKeyPadDownMargin = new Thickness(0, 0, 15, 0);
        public Thickness RecMessageBubbleTextMarginPortrait = new Thickness(0, 0, 90, 14);
        public Thickness ReceivedBubbleFileMarginPortrait = new Thickness(0, 0, 185, 14);
        public Thickness SentBubbleTextMarginPortrait = new Thickness(55, 12, 0, 10);
        public Thickness SentBubbleFileMarginPortrait = new Thickness(185, 12, 0, 10);
        public Thickness SentBubbleAudioFileMarginPortrait = new Thickness(155, 12, 0, 10);
        public Thickness RecievedBubbleTextMarginLS = new Thickness(0, 0, 285, 14);
        public Thickness ReceivedBubbleFileMarginLS = new Thickness(0, 0, 380, 14);
        public Thickness SentBubbleTextMarginLS = new Thickness(250, 12, 0, 10);
        public Thickness SentBubbleFileMarginLS = new Thickness(380, 12, 0, 10);
        public Thickness SentBubbleAudioFileMarginLS = new Thickness(345, 12, 0, 10);
        BitmapImage walkieTalkieGreyImage;
        BitmapImage walkieTalkieWhiteImage;
        BitmapImage walkieTalkieGreyImageBig;
        BitmapImage walkieTalkieWhiteImageBig;
        BitmapImage dustbinGreyImage;
        BitmapImage dustbinWhiteImage;
        SolidColorBrush whiteTextForeGround;
        SolidColorBrush greyTextForeGround;
        private FontFamily groupChatMessageHeader;
        private FontFamily messageText;

        #endregion

        #region STATUS UPDATE CUSTOM CONTROLS
        public Thickness TimelineStatusLayoutMargin = new Thickness(0, 0, 0, 24);
        public Thickness UserProfileStatusLayoutMargin = new Thickness(0, 0, 0, 0);
        public Thickness TimelineStatusTypeMargin = new Thickness(0, 8, 0, 0);
        public Thickness UserProfileStatusTypeMargin = new Thickness(12, 34, 0, 0);
        public Thickness TimelineStatusTextMargin = new Thickness(20, 0, 5, 0);
        public Thickness UserProfileStatusTextMargin = new Thickness(18, 0, 5, 0);
        public Thickness StatusImageMargin = new Thickness(12, 28, 0, 12);
        public SolidColorBrush ShowBorderBrush = new SolidColorBrush(Colors.Red);
        public SolidColorBrush HideBorderBrush = new SolidColorBrush(Colors.Black);
        #endregion

        private Dictionary<string, BitmapImage> _bitMapImageCache = null;

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

            _bitMapImageCache = new Dictionary<string, BitmapImage>();
        }

        #region public  properties

        public Dictionary<string, BitmapImage> BitmapImageCache
        {
            get
            {
                return _bitMapImageCache;
            }
            set
            {
                if (value != _bitMapImageCache)
                    _bitMapImageCache = value;
            }
        }

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

        public SolidColorBrush ButtonGrayBackground
        {
            get
            {
                if (btnGrayBackground == null)
                    btnGrayBackground = new SolidColorBrush(Color.FromArgb(255, 89, 89, 89));
                return btnGrayBackground;
            }
        }

        public SolidColorBrush ButtonGrayForeground
        {
            get
            {
                if (btnGrayForeground == null)
                    btnGrayForeground = new SolidColorBrush(Color.FromArgb(255, 233, 236, 238));
                return btnGrayForeground;
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
        public SolidColorBrush StatusBubbleColor
        {
            get
            {
                if (statusBubbleColor == null)
                {
                    if (Utils.isDarkTheme())
                        statusBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0x26, 0x26, 0x26));
                    else
                        statusBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));
                }
                return statusBubbleColor;
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

        public SolidColorBrush StatusTextForeground
        {
            get
            {
                if (statusTextForeground == null)
                {
                    if (Utils.isDarkTheme())
                        statusTextForeground = new SolidColorBrush(Color.FromArgb(255, 0xd9, 0xd9, 0xd9));
                    else
                        statusTextForeground = new SolidColorBrush(Color.FromArgb(255, 0x4f, 0x4f, 0x4f));
                }
                return statusTextForeground;
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

        public BitmapImage PauseIcon
        {
            get
            {
                if (pauseIcon == null)
                    pauseIcon = new BitmapImage(new Uri("/View/images/pause_icon.png", UriKind.Relative));

                return pauseIcon;
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
                {
                    trying = new BitmapImage(new Uri("/View/images/icon_sending.png", UriKind.Relative));
                }
                return trying;
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

        public BitmapImage IntUserBlocked
        {
            get
            {
                if (chatSmsError == null)
                    chatSmsError = new BitmapImage(new Uri("/View/images/chat_sms_error.png", UriKind.Relative));
                return chatSmsError;
            }
        }

        public BitmapImage GrpNameOrPicChanged
        {
            get
            {
                if (grpNameOrPicChanged == null)
                    grpNameOrPicChanged = new BitmapImage(new Uri("/View/images/group_name_changed.png", UriKind.Relative));
                return grpNameOrPicChanged;
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

        public BitmapImage ProfilePicStatusImage
        {
            get
            {
                if (profilePicStatusImage == null)
                    profilePicStatusImage = new BitmapImage(new Uri("/View/images/timeline_photo.png", UriKind.Relative));
                return profilePicStatusImage;
            }
        }

        public BitmapImage NoNewNotificationImage
        {
            get
            {
                if (noNewNotificationImage == null)
                    noNewNotificationImage = new BitmapImage(new Uri("/View/images/notification_read.png", UriKind.Relative));
                return noNewNotificationImage;
            }
        }

        public BitmapImage NewNotificationImage
        {
            get
            {
                if (newNotificationImage == null)
                    newNotificationImage = new BitmapImage(new Uri("/View/images/notification_unread.png", UriKind.Relative));
                return newNotificationImage;
            }
        }

        public BitmapImage ContactIcon
        {
            get
            {
                if (contactIcon == null)
                {
                    if (Utils.isDarkTheme())
                        contactIcon = WhiteContactIcon;
                    else
                        contactIcon = new BitmapImage(new Uri("/View/images/menu_contact_icon_black.png", UriKind.Relative));
                }
                return contactIcon;
            }
        }

        public BitmapImage WhiteContactIcon
        {
            get
            {
                if (whiteContactIcon == null)
                {
                    whiteContactIcon = new BitmapImage(new Uri("/View/images/menu_contact_icon.png", UriKind.Relative));
                }
                return whiteContactIcon;
            }
        }
        public BitmapImage FacebookDisabledIcon
        {
            get
            {
                if (facebookDisabledIcon == null)
                    facebookDisabledIcon = new BitmapImage(new Uri("/View/images/fb_status_disabled.png", UriKind.Relative));
                return facebookDisabledIcon;
            }
        }

        public BitmapImage FacebookEnabledIcon
        {
            get
            {
                if (facebookEnabledIcon == null)
                    facebookEnabledIcon = new BitmapImage(new Uri("/View/images/fb_status.png", UriKind.Relative));
                return facebookEnabledIcon;
            }
        }

        public BitmapImage TwitterDisabledIcon
        {
            get
            {
                if (twitterDisabledIcon == null)
                    twitterDisabledIcon = new BitmapImage(new Uri("/View/images/twitter_status_disabled.png", UriKind.Relative));
                return twitterDisabledIcon;
            }
        }

        public BitmapImage TwitterEnabledIcon
        {
            get
            {
                if (twitterEnabledIcon == null)
                    twitterEnabledIcon = new BitmapImage(new Uri("/View/images/twitter_status.png", UriKind.Relative));
                return twitterEnabledIcon;
            }
        }

        public BitmapImage MoodDisabledIcon
        {
            get
            {
                if (moodDisabledIcon == null)
                    moodDisabledIcon = new BitmapImage(new Uri("/View/images/moods_status_icon_disabled.png", UriKind.Relative));
                return moodDisabledIcon;
            }
        }

        public BitmapImage MoodEnabledIcon
        {
            get
            {
                if (moodEnabledIcon == null)
                    moodEnabledIcon = new BitmapImage(new Uri("/View/images/moods_status_icon.png", UriKind.Relative));
                return moodEnabledIcon;
            }
        }

        public BitmapImage UserProfileLockImage
        {
            get
            {
                if (userProfileLockImage == null)
                {
                    if (Utils.isDarkTheme())
                        userProfileLockImage = new BitmapImage(new Uri("/View/images/user_lock_white.png", UriKind.Relative));//todo:add white image
                    else
                        userProfileLockImage = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));
                }
                return userProfileLockImage;
            }
        }

        public BitmapImage UserProfileInviteImage
        {
            get
            {
                if (userProfileInviteImage == null)
                {
                    if (Utils.isDarkTheme())
                        userProfileInviteImage = new BitmapImage(new Uri("/View/images/user_invite_white.png", UriKind.Relative));
                    else
                        userProfileInviteImage = new BitmapImage(new Uri("/View/images/user_invite.png", UriKind.Relative));
                }
                return userProfileInviteImage;
            }
        }

        public BitmapImage UserProfileStockImage
        {
            get
            {
                if (userProfileStockImage == null)
                {
                    if (Utils.isDarkTheme())
                        userProfileStockImage = new BitmapImage(new Uri("/View/images/profile_header_stock_dark.png", UriKind.Relative));
                    else
                        userProfileStockImage = new BitmapImage(new Uri("/View/images/profile_header_stock.png", UriKind.Relative));
                }
                return userProfileStockImage;
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

        public SolidColorBrush GreyTextForeGround
        {
            get
            {
                if (greyTextForeGround == null)
                        greyTextForeGround = new SolidColorBrush(Color.FromArgb(255, 104, 104, 104));
                
                return greyTextForeGround;
            }
        }

        public SolidColorBrush WhiteTextForeGround
        {
            get
            {
                if (whiteTextForeGround == null)
                    whiteTextForeGround = new SolidColorBrush(Colors.White);

                return whiteTextForeGround;
            }
        }

        public FontFamily SemiBoldFont
        {
            get
            {
                if (groupChatMessageHeader == null)
                    groupChatMessageHeader = new FontFamily("Segoe WP Semibold");
                return groupChatMessageHeader;
            }
        }

        public FontFamily SemiLightFont
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

        public SolidColorBrush DeleteBlackBackground
        {
            get
            {
                if (deleteBlackBackground == null)
                    deleteBlackBackground = new SolidColorBrush(Colors.Black);
                return deleteBlackBackground;
        }
        }

        public SolidColorBrush DeleteGreyBackground
        {
            get
            {
                if (deleteGreyBackground == null)
                    deleteGreyBackground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 105, 105, 105));
                return deleteGreyBackground;
            }
        }

        public BitmapImage DustbinGreyImage
        {
            get
            {
                if (dustbinGreyImage == null)
                    dustbinGreyImage = new BitmapImage(new Uri("/View/images/deleted_grey_icon.png", UriKind.Relative));

                return dustbinGreyImage;
            }
        }

        public BitmapImage DustbinWhiteImage
        {
            get
            {
                if (dustbinWhiteImage == null)
                    dustbinWhiteImage = new BitmapImage(new Uri("/View/images/deleted_white_icon.png", UriKind.Relative));

                return dustbinWhiteImage;
            }
        }

        public BitmapImage WalkieTalkieGreyImage
        {
            get
            {
                if (walkieTalkieGreyImage == null)
                    walkieTalkieGreyImage = new BitmapImage(new Uri("/View/images/Walkie_Talkie_Grey_small.png", UriKind.Relative));

                return walkieTalkieGreyImage;
            }
        }
        
        public BitmapImage WalkieTalkieWhiteImage
        {
            get
            {
                if (walkieTalkieWhiteImage == null)
                    walkieTalkieWhiteImage = new BitmapImage(new Uri("/View/images/Walkie_Talkie_White_small.png", UriKind.Relative));

                return walkieTalkieWhiteImage;
            }
        }

        public BitmapImage WalkieTalkieBigImage
        {
            get
            {
                if (Utils.isDarkTheme())
                {
                    if (walkieTalkieGreyImageBig == null)
                        walkieTalkieGreyImageBig = new BitmapImage(new Uri("/View/images/Walkie_Talkie_Black_big.png", UriKind.Relative));

                    return walkieTalkieGreyImageBig;
                }
                else
                {
                    if (walkieTalkieWhiteImageBig == null)
                        walkieTalkieWhiteImageBig = new BitmapImage(new Uri("/View/images/Walkie_Talkie_White_big.png", UriKind.Relative));

                    return walkieTalkieWhiteImageBig;
                }
            }
        }

        public BitmapImage WalkieTalkieDeleteSucImage
        {
            get
            {
                if (Utils.isDarkTheme())
                {
                    if (walkieTalkieGreyImageBig == null)
                        walkieTalkieGreyImageBig = new BitmapImage(new Uri("/View/images/deleted_grey_icon.png", UriKind.Relative));

                    return walkieTalkieGreyImageBig;
                }
                else
                {
                    if (walkieTalkieWhiteImageBig == null)
                        walkieTalkieWhiteImageBig = new BitmapImage(new Uri("/View/images/deleted_white_icon.png", UriKind.Relative));

                    return walkieTalkieWhiteImageBig;
                }
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

        public byte[] BitmapImgToByteArray(BitmapImage image)
        {
            try
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(image);
                using (var msLargeImage = new MemoryStream())
                {
                    writeableBitmap.SaveJpeg(msLargeImage, 90, 90, 0, 90);
                    return msLargeImage.ToArray();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ImageUtils ::  BitmapImgToByteArray :  BitmapImgToByteArray , Exception : " + ex.StackTrace);
                return null;
            }
        }

        public BitmapImage createImageFromBytes(byte[] imagebytes)
        {
            if (imagebytes == null || imagebytes.Length == 0)
                return null;
            BitmapImage bitmapImage = null;
            try
            {
                using (var memStream = new MemoryStream(imagebytes))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(memStream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("IMAGE UTILS :: Exception while creating bitmap image from memstream : " + e.StackTrace);
            }
            return bitmapImage;
        }

        /// <summary>
        /// Call this function only if you want to cache the Bitmap Image
        /// </summary>
        /// <param name="msisdn"></param>
        /// <returns></returns>
        public BitmapImage GetBitmapImage(string msisdn)
        {
            return GetBitmap(msisdn, true);
        }

        /// <summary>
        /// Call this function if donot want to save in cache
        /// </summary>
        /// <param name="msisdn"></param>
        /// <param name="shouldSave"></param>
        /// <returns></returns>
        public BitmapImage GetBitmapImage(string msisdn, bool shouldSave)
        {
            return GetBitmap(msisdn, false);
        }

        private BitmapImage GetBitmap(string msisdn, bool saveInCache)
        {
            if (_bitMapImageCache.ContainsKey(msisdn))
                return _bitMapImageCache[msisdn];
            // if no image for this user exists

            byte[] profileImageBytes = MiscDBUtil.getThumbNailForMsisdn(msisdn);
            if (profileImageBytes != null && profileImageBytes.Length > 0)
            {
                BitmapImage img = createImageFromBytes(profileImageBytes);
                if (img != null)
                {
                    if (saveInCache)
                        _bitMapImageCache[msisdn] = img;
                    return img;
                }
            }
            // do not add default avatar images to cache as they are already chached.
            if (Utils.isGroupConversation(msisdn))
                return getDefaultGroupAvatar(msisdn);
            return getDefaultAvatar(msisdn);
        }
    }
}
