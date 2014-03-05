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
        private SolidColorBrush receivedChatBubbleColor;
        private SolidColorBrush walkThroughSelectedColumn;
        private SolidColorBrush walkThroughUnselectedColumn;
        private SolidColorBrush transparent;
        private SolidColorBrush hikeBlue;
        private SolidColorBrush black;
        private SolidColorBrush white;
        private SolidColorBrush black40Opacity;
        private SolidColorBrush btnGrayBackground;
        private SolidColorBrush btnGrayForeground;
        private SolidColorBrush groupChatHeaderColor;
        private SolidColorBrush signUpForeground;
        private SolidColorBrush editProfileForeground;
        private SolidColorBrush receivedChatBubbleProgress;
        private SolidColorBrush phoneThemeColor;
        private SolidColorBrush statusTextForeground;
        private SolidColorBrush tappedCategoryColor;
        private SolidColorBrush untappedCategoryColor;
        private SolidColorBrush deleteGreyBackground;
        private SolidColorBrush deleteBlackBackground;
        private BitmapImage chatBackgroundImageWhite;
        private BitmapImage chatBackgroundImageBlack;
        private BitmapImage onHikeImage;
        private BitmapImage onHikeImage_ct;
        private BitmapImage notOnHikeImage;
        private BitmapImage notOnHikeImage_ct;
        private BitmapImage chatAcceptedImage;
        private BitmapImage chatAcceptedImage_ct;
        private BitmapImage playIcon;
        private BitmapImage pauseIcon;
        private BitmapImage downloadIcon;
        private BitmapImage httpFailed;
        private BitmapImage httpFailed_CT;
        private BitmapImage sent;
        private BitmapImage sent_ct;
        private BitmapImage delivered;
        private BitmapImage delivered_ct;
        private BitmapImage read;
        private BitmapImage read_ct;
        private BitmapImage trying;
        private BitmapImage trying_ct;
        private BitmapImage waiting;
        private BitmapImage waiting_ct;
        private BitmapImage reward;
        private BitmapImage reward_ct;
        private BitmapImage chatSmsError;
        private BitmapImage chatSmsError_ct;
        private BitmapImage grpNameChanged;
        private BitmapImage grpNameChanged_ct;
        private BitmapImage grpPicChanged;
        private BitmapImage grpPicChanged_ct;
        private BitmapImage chatBackgroundChanged;
        private BitmapImage chatBackgroundChanged_ct;
        private BitmapImage participantLeft;
        private BitmapImage participantLeft_ct;
        private BitmapImage nudgeSend;
        private BitmapImage nudgeReceived;
        private BitmapImage textStatusImage;
        private BitmapImage friendRequestImage;
        private BitmapImage profilePicStatusImage;
        private BitmapImage noNewNotificationImage;
        private BitmapImage newNotificationImage;
        private BitmapImage blackContactIcon;
        private BitmapImage whiteContactIcon;
        private BitmapImage facebookDisabledIcon;
        private BitmapImage facebookEnabledIcon;
        private BitmapImage twitterDisabledIcon;
        private BitmapImage twitterEnabledIcon;
        private BitmapImage userProfileLockImage;
        private BitmapImage userProfileInviteImage;
        private BitmapImage userProfileStockImage;
        private BitmapImage loadingImage;
        private BitmapImage lastSeenClockImageWhite;
        private BitmapImage lastSeenClockImageBlack;
        private BitmapImage humanoidOverlay;
        private BitmapImage humanoid2Overlay;
        private BitmapImage doggyOverlay;
        private BitmapImage kittyOverlay;
        private BitmapImage bollywoodOverlay;
        private BitmapImage trollOverlay;
        private BitmapImage expressionsOverlay;
        private BitmapImage smileyExpressionsOverlay;
        private BitmapImage avatarsOverlay;
        private BitmapImage indiansOverlay;
        private BitmapImage loveOverlay;
        private BitmapImage angryOverlay;
        private BitmapImage recentIcon;
        private BitmapImage humanoidInactive;
        private BitmapImage humanoid2Inactive;
        private BitmapImage doggyInactive;
        private BitmapImage kittyInactive;
        private BitmapImage bollywoodInactive;
        private BitmapImage trollInactive;
        private BitmapImage expressionsInactive;
        private BitmapImage smileyExpressionsInactive;
        private BitmapImage avatarsInactive;
        private BitmapImage indianInactive;
        private BitmapImage loveInactive;
        private BitmapImage angryInactive;
        private BitmapImage humanoidActive;
        private BitmapImage humanoid2Active;
        private BitmapImage doggyActive;
        private BitmapImage kittyActive;
        private BitmapImage bollywoodActive;
        private BitmapImage trollActive;
        private BitmapImage expressionsActive;
        private BitmapImage smileyExpressionsActive;
        private BitmapImage avatarsActive;
        private BitmapImage indianActive;
        private BitmapImage loveActive;
        private BitmapImage angryActive;
        private BitmapImage muteIcon;
        private BitmapImage muteIconForConversationView;
        private BitmapImage unmuteIcon;
        private BitmapImage circles;
        private BitmapImage hikeToastImage;
        private BitmapImage[] defaultUserAvatars = new BitmapImage[7];
        private BitmapImage[] defaultGroupAvatars = new BitmapImage[7];
        private string[] defaultAvatarFileNames;

        private SolidColorBrush receiveMessageForeground;
        private Thickness convListEmoticonMargin = new Thickness(0, 3, 0, -5);
        private Thickness newChatThreadEmoticonMargin = new Thickness(0, 10, 0, -10);
        BitmapImage walkieTalkieGreyImage;
        BitmapImage walkieTalkieWhiteImage;
        BitmapImage walkieTalkieBigImage;
        BitmapImage walkieTalkieDeleteSucImage;
        BitmapImage closeButtonWhiteImage;
        BitmapImage closeButtonBlackImage;
        BitmapImage cancelButtonWhiteImage;
        BitmapImage cancelButtonBlackImage;
        BitmapImage dustbinGreyImage;
        BitmapImage dustbinWhiteImage;
        SolidColorBrush whiteTextForeGround;
        SolidColorBrush greyTextForeGround;
        private FontFamily groupChatMessageHeader;
        private FontFamily messageText;
        private BitmapImage myLocationPin;

        private BitmapImage resumeFTRWhite;
        private BitmapImage resumeFTRBlack;
        private BitmapImage pausedFTRWhite;
        private BitmapImage pausedFTRBlack;

        private BitmapImage overlayRupeeImage;
        private BitmapImage overlaySmsImage;

        BitmapImage profileTickImage;

        #endregion

        #region STATUS UPDATE CUSTOM CONTROLS
        public Thickness TimelineStatusLayoutMargin = new Thickness(0, 0, 0, 24);
        public Thickness UserProfileStatusLayoutMargin = new Thickness(0, 0, 0, 0);
        public Thickness TimelineStatusTypeMargin = new Thickness(0, 8, 0, 0);
        public Thickness UserProfileStatusTypeMargin = new Thickness(12, 34, 0, 0);
        public Thickness TimelineStatusTextMargin = new Thickness(20, 0, 5, 0);
        public Thickness UserProfileStatusTextMargin = new Thickness(18, 0, 5, 0);
        public Thickness StatusImageMargin = new Thickness(12, 28, 0, 12);
        public SolidColorBrush RedBorderBrush = new SolidColorBrush(Colors.Red);
        public BitmapImage BlankBitmapImage = new BitmapImage();
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

        public SolidColorBrush ReceivedChatBubbleColor
        {
            get
            {
                if (receivedChatBubbleColor == null)
                    receivedChatBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));

                return receivedChatBubbleColor;
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

        public SolidColorBrush Transparent
        {
            get
            {
                if (transparent == null)
                    transparent = new SolidColorBrush(Colors.Transparent);
                return transparent;
            }
        }

        public SolidColorBrush HikeBlue
        {
            get
            {
                if (hikeBlue == null)
                    hikeBlue = new SolidColorBrush(Color.FromArgb(255, 46, 145, 211));
                return hikeBlue;
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

        public SolidColorBrush Black40Opacity
        {
            get
            {
                if (black40Opacity == null)
                    black40Opacity = new SolidColorBrush(Color.FromArgb(102, 0x0, 0x0, 0x0));
                return black40Opacity;
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

        public SolidColorBrush TappedCategoryColor
        {
            get
            {
                if (tappedCategoryColor == null)
                    tappedCategoryColor = new SolidColorBrush(Color.FromArgb(255, 0x1b, 0xa1, 0xe2));
                return tappedCategoryColor;
            }
        }
        public SolidColorBrush UntappedCategoryColor
        {
            get
            {
                if (untappedCategoryColor == null)
                    untappedCategoryColor = new SolidColorBrush(Color.FromArgb(255, 0x4d, 0x4d, 0x4d));
                return untappedCategoryColor;
            }
        }

        public BitmapImage MyLocationPin
        {
            get
            {
                if (myLocationPin == null)
                    myLocationPin = new BitmapImage(new Uri("/view/images/MyLocation.png", UriKind.Relative));

                return myLocationPin;
            }
        }

        public BitmapImage ChatBackgroundImageWhite
        {
            get
            {
                if (chatBackgroundImageWhite == null)
                    chatBackgroundImageWhite = new BitmapImage(new Uri("/view/images/paint.png", UriKind.Relative));
                return chatBackgroundImageWhite;
            }
        }

        public BitmapImage ChatBackgroundImageBlack
        {
            get
            {
                if (chatBackgroundImageBlack == null)
                    chatBackgroundImageBlack = new BitmapImage(new Uri("/view/images/paint_black.png", UriKind.Relative));
                return chatBackgroundImageBlack;
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

        public BitmapImage OnHikeImage_ChatTheme
        {
            get
            {
                if (onHikeImage_ct == null)
                    onHikeImage_ct = new BitmapImage(new Uri("/View/images/chat_joined_blue_CT.png", UriKind.Relative));
                return onHikeImage_ct;
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

        public BitmapImage NotOnHikeImage_ChatTheme
        {
            get
            {
                if (notOnHikeImage_ct == null)
                    notOnHikeImage_ct = new BitmapImage(new Uri("/View/images/chat_invited_green_CT.png", UriKind.Relative));
                return notOnHikeImage_ct;
            }
        }

        public BitmapImage OverlayRupeeImage
        {
            get
            {
                if (overlayRupeeImage == null)
                    overlayRupeeImage = new BitmapImage(new Uri("/View/images/rupee.png", UriKind.Relative));
                return overlayRupeeImage;
            }
        }

        public BitmapImage OverlaySmsImage
        {
            get
            {
                if (overlaySmsImage == null)
                    overlaySmsImage = new BitmapImage(new Uri("/View/images/icon_sms.png", UriKind.Relative));
                return overlaySmsImage;
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

        public BitmapImage ChatAcceptedImage_ChatTheme
        {
            get
            {
                if (chatAcceptedImage_ct == null)
                    chatAcceptedImage_ct = new BitmapImage(new Uri("/View/images/chat_invited_green_CT.png", UriKind.Relative));
                return chatAcceptedImage_ct;
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

        public BitmapImage HikeToastImage
        {
            get
            {
                if (hikeToastImage == null)
                {
                    hikeToastImage = new BitmapImage(new Uri("/View/images/hike_toast_image.png", UriKind.RelativeOrAbsolute));
                }

                return hikeToastImage;
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

        public BitmapImage HttpFailed_ChatTheme
        {
            get
            {
                if (httpFailed_CT == null)
                    httpFailed_CT = new BitmapImage(new Uri("/View/images/error_icon_CT.png", UriKind.Relative));

                return httpFailed_CT;
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

        public BitmapImage Sent_ChatTheme
        {
            get
            {
                if (sent_ct == null)
                    sent_ct = new BitmapImage(new Uri("/View/images/ic_sent_CT.png", UriKind.Relative));
                return sent_ct;
            }
        }

        public BitmapImage PausedFTRWhite
        {
            get
            {
                if (pausedFTRWhite == null)
                    pausedFTRWhite = new BitmapImage(new Uri("/View/images/pause_white_ftr.png", UriKind.Relative));

                return pausedFTRWhite;
            }
        }

        public BitmapImage ResumeFTRWhite
        {
            get
            {
                if (resumeFTRWhite == null)
                    resumeFTRWhite = new BitmapImage(new Uri("/View/images/resume_white_ftr.png", UriKind.Relative));

                return resumeFTRWhite;
            }
        }

        public BitmapImage PausedFTRBlack
        {
            get
            {
                if (pausedFTRBlack == null)
                    pausedFTRBlack = new BitmapImage(new Uri("/View/images/pause_black_ftr.png", UriKind.Relative));

                return pausedFTRBlack;
            }
        }

        public BitmapImage ResumeFTRBlack
        {
            get
            {
                if (resumeFTRBlack == null)
                    resumeFTRBlack = new BitmapImage(new Uri("/View/images/resume_black_ftr.png", UriKind.Relative));

                return resumeFTRBlack;
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

        public BitmapImage Delivered_ChatTheme
        {
            get
            {
                if (delivered_ct == null)
                    delivered_ct = new BitmapImage(new Uri("/View/images/ic_delivered_CT.png", UriKind.Relative));

                return delivered_ct;
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

        public BitmapImage Read_ChatTheme
        {
            get
            {
                if (read_ct == null)
                    read_ct = new BitmapImage(new Uri("/View/images/ic_read_CT.png", UriKind.Relative));

                return read_ct;
            }
        }

        public BitmapImage Trying_ChatTheme
        {
            get
            {
                if (trying_ct == null)
                    trying_ct = new BitmapImage(new Uri("/View/images/icon_sending_CT.png", UriKind.Relative));

                return trying_ct;
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

        public BitmapImage Waiting_ChatTheme
        {
            get
            {
                if (waiting_ct == null)
                    waiting_ct = new BitmapImage(new Uri("/View/images/chat_waiting_CT.png", UriKind.Relative));

                return waiting_ct;
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

        public BitmapImage Reward_ChatTheme
        {
            get
            {
                if (reward_ct == null)
                    reward_ct = new BitmapImage(new Uri("/View/images/chat_reward_ct.png", UriKind.Relative));
                return reward_ct;
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

        public BitmapImage IntUserBlocked_ChatTheme
        {
            get
            {
                if (chatSmsError_ct == null)
                    chatSmsError_ct = new BitmapImage(new Uri("/View/images/chat_sms_error_CT.png", UriKind.Relative));
                return chatSmsError_ct;
            }
        }

        public BitmapImage GrpNameChanged
        {
            get
            {
                if (grpNameChanged == null)
                    grpNameChanged = new BitmapImage(new Uri("/View/images/group_name_changed.png", UriKind.Relative));
                return grpNameChanged;
            }
        }

        public BitmapImage GrpNameChanged_ChatTheme
        {
            get
            {
                if (grpNameChanged_ct == null)
                    grpNameChanged_ct = new BitmapImage(new Uri("/View/images/group_name_changed_CT.png", UriKind.Relative));

                return grpNameChanged_ct;
            }
        }

        public BitmapImage GrpPicChanged
        {
            get
            {
                if (grpPicChanged == null)
                    grpPicChanged = new BitmapImage(new Uri("/View/images/group_pic_changed.png", UriKind.Relative));
                return grpPicChanged;
            }
        }

        public BitmapImage GrpPicChanged_ChatTheme
        {
            get
            {
                if (grpPicChanged_ct == null)
                    grpPicChanged_ct = new BitmapImage(new Uri("/View/images/group_pic_changed_CT.png", UriKind.Relative));

                return grpPicChanged_ct;
            }
        }

        public BitmapImage ChatBackgroundChanged
        {
            get
            {
                if (chatBackgroundChanged == null)
                    chatBackgroundChanged = new BitmapImage(new Uri("/View/images/chatBackground_changed.png", UriKind.Relative));
                return chatBackgroundChanged;
            }
        }

        public BitmapImage ChatBackgroundChanged_ChatTheme
        {
            get
            {
                if (chatBackgroundChanged_ct == null)
                    chatBackgroundChanged_ct = new BitmapImage(new Uri("/View/images/chatBackground_changed_CT.png", UriKind.Relative));

                return chatBackgroundChanged_ct;
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

        public BitmapImage ParticipantLeft_ChatTheme
        {
            get
            {
                if (participantLeft_ct == null)
                    participantLeft_ct = new BitmapImage(new Uri("/View/images/chat_left_CT.png", UriKind.Relative));

                return participantLeft_ct;
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

        public BitmapImage BlackContactIcon
        {
            get
            {
                if (blackContactIcon == null)
                    blackContactIcon = new BitmapImage(new Uri("/View/images/menu_contact_icon_black.png", UriKind.Relative));

                return blackContactIcon;
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

        public BitmapImage UserProfileLockImage
        {
            get
            {
                if (userProfileLockImage == null)
                {
                    if (Utils.isDarkTheme())
                        userProfileLockImage = new BitmapImage(new Uri("/View/images/user_lock_white.png", UriKind.Relative));
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

        public BitmapImage StickerLoadingImage
        {
            get
            {
                if (loadingImage == null)
                    loadingImage = new BitmapImage(new Uri("/View/images/loading.png", UriKind.Relative));
                return loadingImage;
            }
        }

        public BitmapImage LastSeenClockImageWhite
        {
            get
            {
                if (lastSeenClockImageWhite == null)
                    lastSeenClockImageWhite = new BitmapImage(new Uri("/View/images/last_seen_clock_white.png", UriKind.Relative));

                return lastSeenClockImageWhite;
            }
        }

        public BitmapImage LastSeenClockImageBlack
        {
            get
            {
                if (lastSeenClockImageBlack == null)
                    lastSeenClockImageBlack = new BitmapImage(new Uri("/View/images/last_seen_clock_black.png", UriKind.Relative));

                return lastSeenClockImageBlack;
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

        public Thickness NewChatThreadEmoticonMargin
        {
            get
            {
                return newChatThreadEmoticonMargin;
            }
        }

        public Thickness ConvListEmoticonMargin
        {
            get
            {
                return convListEmoticonMargin;
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
                if (walkieTalkieBigImage == null)
                    walkieTalkieBigImage = new BitmapImage(new Uri("/View/images/Walkie_Talkie_White_big.png", UriKind.Relative));

                return walkieTalkieBigImage;
            }
        }

        public BitmapImage WalkieTalkieDeleteSucImage
        {
            get
            {
                if (walkieTalkieDeleteSucImage == null)
                    walkieTalkieDeleteSucImage = new BitmapImage(new Uri("/View/images/deleted_white_icon.png", UriKind.Relative));

                return walkieTalkieDeleteSucImage;
            }
        }

        public BitmapImage CloseButtonBlackImage
        {
            get
            {
                if (closeButtonBlackImage == null)
                    closeButtonBlackImage = new BitmapImage(new Uri("/View/images/close_black.png", UriKind.Relative));

                return closeButtonBlackImage;
            }
        }

        public BitmapImage CloseButtonWhiteImage
        {
            get
            {
                if (closeButtonWhiteImage == null)
                    closeButtonWhiteImage = new BitmapImage(new Uri("/View/images/close_white.png", UriKind.Relative));

                return closeButtonWhiteImage;
            }
        }
        public BitmapImage Circles
        {
            get
            {
                if (circles == null)
                {
                    circles = new BitmapImage(new Uri("/View/images/circles.png", UriKind.Relative));
                }
                return circles;
            }
        }

        public BitmapImage CancelButtonBlackImage
        {
            get
            {
                if (cancelButtonBlackImage == null)
                    cancelButtonBlackImage = new BitmapImage(new Uri("/View/images/cancel_black.png", UriKind.Relative));

                return cancelButtonBlackImage;
            }
        }

        public BitmapImage CancelButtonWhiteImage
        {
            get
            {
                if (cancelButtonWhiteImage == null)
                    cancelButtonWhiteImage = new BitmapImage(new Uri("/View/images/cancel_white.png", UriKind.Relative));

                return cancelButtonWhiteImage;
            }
        }

        #region Sticker Category Overlays

        public BitmapImage HumanoidOverlay
        {
            get
            {
                if (humanoidOverlay == null)
                {
                    humanoidOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_overlay.png", UriKind.Relative));
                }
                return humanoidOverlay;
            }
        }

        public BitmapImage Humanoid2Overlay
        {
            get
            {
                if (humanoid2Overlay == null)
                {
                    humanoid2Overlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_overlay.png", UriKind.Relative));
                }
                return humanoid2Overlay;
            }
        }

        public BitmapImage DoggyOverlay
        {
            get
            {
                if (doggyOverlay == null)
                {
                    doggyOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_overlay.png", UriKind.Relative));
                }
                return doggyOverlay;
            }
        }

        public BitmapImage KittyOverlay
        {
            get
            {
                if (kittyOverlay == null)
                {
                    kittyOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/kitty_overlay.png", UriKind.Relative));
                }
                return kittyOverlay;
            }
        }

        public BitmapImage BollywoodOverlay
        {
            get
            {
                if (bollywoodOverlay == null)
                {
                    bollywoodOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/bollywood_overlay.png", UriKind.Relative));
                }
                return bollywoodOverlay;
            }
        }

        public BitmapImage TrollOverlay
        {
            get
            {
                if (trollOverlay == null)
                {
                    trollOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/troll_overlay.png", UriKind.Relative));
                }
                return trollOverlay;
            }
        }

        public BitmapImage AvatarsOverlay
        {
            get
            {
                if (avatarsOverlay == null)
                {
                    avatarsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars_overlay.png", UriKind.Relative));
                }
                return avatarsOverlay;
            }
        }

        public BitmapImage IndiansOverlay
        {
            get
            {
                if (indiansOverlay == null)
                    indiansOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_overlay.png", UriKind.Relative));

                return indiansOverlay;
            }
        }

        public BitmapImage AngryOverlay
        {
            get
            {
                if (angryOverlay == null)
                    angryOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/hotheads_overlay.png", UriKind.Relative));

                return angryOverlay;
            }
        }
        public BitmapImage LoveOverlay
        {
            get
            {
                if (loveOverlay == null)
                    loveOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/iloveyou_overlay.png", UriKind.Relative));

                return loveOverlay;
            }
        }
        public BitmapImage ExpressionsOverlay
        {
            get
            {
                if (expressionsOverlay == null)
                {
                    expressionsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions_overlay.png", UriKind.Relative));
                }
                return expressionsOverlay;
            }
        }

        public BitmapImage SmileyExpressionsOverlay
        {
            get
            {
                if (smileyExpressionsOverlay == null)
                {
                    smileyExpressionsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/smileyExpressions_overlay.png", UriKind.Relative));
                }
                return smileyExpressionsOverlay;
            }
        }

        #endregion

        #region Sticker categroy icons

        public BitmapImage HumanoidInactive
        {
            get
            {
                if (humanoidInactive == null)
                {
                    humanoidInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_icon_inactive.png", UriKind.Relative));
                }
                return humanoidInactive;
            }
        }

        public BitmapImage Humanoid2Inactive
        {
            get
            {
                if (humanoid2Inactive == null)
                {
                    humanoid2Inactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_icon_inactive.png", UriKind.Relative));
                }
                return humanoid2Inactive;
            }
        }

        public BitmapImage DoggyInactive
        {
            get
            {
                if (doggyInactive == null)
                {
                    doggyInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_i_icon.png", UriKind.Relative));
                }
                return doggyInactive;
            }
        }

        public BitmapImage KittyInactive
        {
            get
            {
                if (kittyInactive == null)
                {
                    kittyInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/kitty_i_icon.png", UriKind.Relative));
                }
                return kittyInactive;
            }
        }

        public BitmapImage BollywoodInactive
        {
            get
            {
                if (bollywoodInactive == null)
                {
                    bollywoodInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/Inactive_bolly.png", UriKind.Relative));
                }
                return bollywoodInactive;
            }
        }

        public BitmapImage TrollInactive
        {
            get
            {
                if (trollInactive == null)
                {
                    trollInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/rf_icon.png", UriKind.Relative));
                }
                return trollInactive;
            }
        }

        public BitmapImage ExpressionsInactive
        {
            get
            {
                if (expressionsInactive == null)
                {
                    expressionsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions_i.png", UriKind.Relative));
                }
                return expressionsInactive;
            }
        }

        public BitmapImage SmileyExpressionsInactive
        {
            get
            {
                if (smileyExpressionsInactive == null)
                {
                    smileyExpressionsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/smileyExpressions_i.png", UriKind.Relative));
                }
                return smileyExpressionsInactive;
            }
        }

        public BitmapImage AvatarsInactive
        {
            get
            {
                if (avatarsInactive == null)
                {
                    avatarsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars_i.png", UriKind.Relative));
                }
                return avatarsInactive;
            }
        }

        public BitmapImage IndianInactive
        {
            get
            {
                if (indianInactive == null)
                {
                    indianInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_inactive.png", UriKind.Relative));
                }
                return indianInactive;
            }
        }

        public BitmapImage LoveInactive
        {
            get
            {
                if (loveInactive == null)
                {
                    loveInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/love_inactive.png", UriKind.Relative));
                }
                return loveInactive;
            }
        }
        public BitmapImage AngryInactive
        {
            get
            {
                if (angryInactive == null)
                {
                    angryInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/angry_inactive.png", UriKind.Relative));
                }
                return angryInactive;
            }
        }
        public BitmapImage HumanoidActive
        {
            get
            {
                if (humanoidActive == null)
                {
                    humanoidActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_icon.png", UriKind.Relative));
                }
                return humanoidActive;
            }
        }

        public BitmapImage Humanoid2Active
        {
            get
            {
                if (humanoid2Active == null)
                {
                    humanoid2Active = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_icon.png", UriKind.Relative));
                }
                return humanoid2Active;
            }
        }

        public BitmapImage DoggyActive
        {
            get
            {
                if (doggyActive == null)
                {
                    doggyActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_icon.png", UriKind.Relative));
                }
                return doggyActive;
            }
        }

        public BitmapImage KittyActive
        {
            get
            {
                if (kittyActive == null)
                {
                    kittyActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/kitty_icon.png", UriKind.Relative));
                }
                return kittyActive;
            }
        }

        public BitmapImage BollywoodActive
        {
            get
            {
                if (bollywoodActive == null)
                {
                    bollywoodActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/active_bolly.png", UriKind.Relative));
                }
                return bollywoodActive;
            }
        }

        public BitmapImage TrollActive
        {
            get
            {
                if (trollActive == null)
                {
                    trollActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/rf_i_icon.png", UriKind.Relative));
                }
                return trollActive;
            }
        }

        public BitmapImage ExpressionsActive
        {
            get
            {
                if (expressionsActive == null)
                {
                    expressionsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions.png", UriKind.Relative));
                }
                return expressionsActive;
            }
        }

        public BitmapImage SmileyExpressionsActive
        {
            get
            {
                if (smileyExpressionsActive == null)
                {
                    smileyExpressionsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/smileyExpressions.png", UriKind.Relative));
                }
                return smileyExpressionsActive;
            }
        }

        public BitmapImage AvatarsActive
        {
            get
            {
                if (avatarsActive == null)
                {
                    avatarsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars.png", UriKind.Relative));
                }
                return avatarsActive;
            }
        }

        public BitmapImage IndianActive
        {
            get
            {
                if (indianActive == null)
                {
                    indianActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_active.png", UriKind.Relative));
                }
                return indianActive;
            }
        }

        public BitmapImage AngryActive
        {
            get
            {
                if (angryActive == null)
                {
                    angryActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/angry_active.png", UriKind.Relative));
                }
                return angryActive;
            }
        }
        public BitmapImage LoveActive
        {
            get
            {
                if (loveActive == null)
                {
                    loveActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/love_active.png", UriKind.Relative));
                }
                return loveActive;
            }
        }
        #endregion

        public BitmapImage RecentIcon
        {
            get
            {
                if (recentIcon == null)
                {
                    recentIcon = new BitmapImage(new Uri("/View/images/recent_icon.png", UriKind.Relative));
                }
                return recentIcon;
            }
        }

        public BitmapImage MuteIcon
        {
            get
            {
                if (muteIcon == null)
                {
                    if (Utils.isDarkTheme())
                        muteIcon = new BitmapImage(new Uri("/View/images/mute_icon_main_l.png", UriKind.Relative));
                    else
                        muteIcon = new BitmapImage(new Uri("/View/images/mute_icon_main_g.png", UriKind.Relative));
                }
                return muteIcon;
            }
        }

        public BitmapImage MuteIconForConversationView
        {
            get
            {
                if (muteIconForConversationView == null)
                {
                    if (Utils.isDarkTheme())
                        muteIconForConversationView = new BitmapImage(new Uri("/View/images/mute_icon_main_white.png", UriKind.Relative));
                    else
                        muteIconForConversationView = new BitmapImage(new Uri("/View/images/mute_icon_main_b.png", UriKind.Relative));
                }

                return muteIconForConversationView;
            }
        }

        public BitmapImage UnmuteIcon
        {
            get
            {
                if (unmuteIcon == null)
                {
                    if (Utils.isDarkTheme())
                        unmuteIcon = new BitmapImage(new Uri("/View/images/unmute_icon_main_l.png", UriKind.Relative));
                    else
                        unmuteIcon = new BitmapImage(new Uri("/View/images/unmute_icon_main_g.png", UriKind.Relative));
                }
                return unmuteIcon;
            }
        }

        public Thickness ZeroThickness = new Thickness(0, 0, 0, 0);
        public Thickness NewCategoryThickness = new Thickness(0, 5, 0, 0);

        public BitmapImage ProfileTickImage
        {
            get
            {
                if (profileTickImage == null)
                    profileTickImage = new BitmapImage(new Uri("/View/Images/Item_Selected.png", UriKind.Relative));

                return profileTickImage;
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

        public byte[] PngImgToJpegByteArray(BitmapImage image)
        {
            try
            {
                WriteableBitmap writeableBitmap = new WriteableBitmap(image);
                WriteableBitmap mergedBItmpapImage = new WriteableBitmap(writeableBitmap.PixelWidth, writeableBitmap.PixelHeight); //size of mergedBItmpapImage canvas
                double aspectratio = writeableBitmap.PixelHeight * 1.0 / writeableBitmap.PixelWidth;
                mergedBItmpapImage.Clear(Color.FromArgb(255, 0x32, 0x32, 0x32));
                Rect rec = new Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight);
                mergedBItmpapImage.Blit(rec, writeableBitmap, rec);

                int maxSize = 0;
                if (Utils.PalleteResolution == Utils.Resolutions.WVGA)
                    maxSize = 135;
                else if (Utils.PalleteResolution == Utils.Resolutions.WXGA)
                    maxSize = 200;
                else
                    maxSize = 190;

                int toWidth = 0;
                int toHeight = 0;

                if (writeableBitmap.PixelWidth > maxSize && writeableBitmap.PixelHeight > maxSize)
                {
                    if (writeableBitmap.PixelWidth > writeableBitmap.PixelHeight)
                    {
                        toWidth = maxSize;
                        toHeight = Convert.ToInt32(maxSize * aspectratio);
                    }
                    else
                    {
                        toHeight = maxSize;
                        toWidth = Convert.ToInt32(toHeight / aspectratio);
                    }
                }
                else if (writeableBitmap.PixelWidth > maxSize)
                {
                    toWidth = maxSize;
                    toHeight = Convert.ToInt32(maxSize * aspectratio);
                }
                else if (writeableBitmap.PixelHeight > maxSize)
                {
                    toHeight = maxSize;
                    toWidth = Convert.ToInt32(toHeight / aspectratio);
                }
                else
                {
                    toWidth = Convert.ToInt32(writeableBitmap.PixelWidth);
                    toHeight = Convert.ToInt32(writeableBitmap.PixelHeight);
                }
                using (var msLargeImage = new MemoryStream())
                {
                    mergedBItmpapImage.SaveJpeg(msLargeImage, toWidth, toHeight, 0, 100);
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

            try
            {
                int toWidth = GetMaxToWidthForImage(bitmapImage.PixelHeight, bitmapImage.PixelWidth);
                if (toWidth != 0)
                    return getCompressedImage(imagebytes, toWidth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("IMAGE UTILS :: Exception while calculating aspect ratio  for compressed image: " + ex.StackTrace);
            }

            return bitmapImage;
        }

        public int GetMaxToWidthForImage(double height, double width)
        {
            var aspectratio = height / width;
            int toWidth = 0;

            if (width > 480 && height > 800)
                toWidth = width > height ? 480 : Convert.ToInt32(800 / aspectratio);
            else if (width > 480)
                toWidth = 480;
            else if (height > 800)
                toWidth = Convert.ToInt32(800 / aspectratio);
            return toWidth;
        }

        BitmapImage getCompressedImage(byte[] imagebytes, int toWidth)
        {
            if (imagebytes == null || imagebytes.Length == 0)
                return null;
            BitmapImage bitmapImage = null;
            try
            {
                using (var memStream = new MemoryStream(imagebytes))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    bitmapImage = new BitmapImage() { DecodePixelWidth = toWidth };
                    //If you specify BackgroundCreation in the CreateOptions of a BitmapImage, you have to specify only DecodePixelHeight.
                    //The DecodePixelWidth property is ignored when you specify BackgroundCreation.
                    bitmapImage.SetSource(memStream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("IMAGE UTILS :: Exception while creating compressed bitmap image from memstream : " + e.StackTrace);
            }

            return bitmapImage;
        }

        public void createImageFromBytes(byte[] imagebytes, BitmapImage bitmapImage)
        {
            if (imagebytes == null || imagebytes.Length == 0)
                return;
            try
            {
                using (var memStream = new MemoryStream(imagebytes))
                {
                    memStream.Seek(0, SeekOrigin.Begin);
                    if (bitmapImage == null)
                        bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(memStream);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("IMAGE UTILS :: Exception while creating bitmap image from memstream : " + e.StackTrace);
            }
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

        public SolidColorBrush ConvertStringToColor(string hexString)
        {
            byte a = 0;
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (hexString.StartsWith("#"))
                hexString = hexString.Substring(1);

            if (hexString.Length == 8)
            {
                a = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                r = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                g = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                b = Convert.ToByte(Int32.Parse(hexString.Substring(6, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            }
            else
            {
                a = Convert.ToByte(255);
                r = Convert.ToByte(Int32.Parse(hexString.Substring(0, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                g = Convert.ToByte(Int32.Parse(hexString.Substring(2, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
                b = Convert.ToByte(Int32.Parse(hexString.Substring(4, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            }

            return new SolidColorBrush(Color.FromArgb(a, r, g, b));
        }

        public byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            WriteableBitmap wb = new WriteableBitmap(bitmapImage);

            using (MemoryStream ms = new MemoryStream())
            {
                wb.SaveJpeg(ms, bitmapImage.PixelWidth, bitmapImage.PixelHeight, 0, 100);

                return ms.ToArray();
            }
        }
    }
}
