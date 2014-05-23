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

        private BitmapImage[] defaultUserAvatars = new BitmapImage[5];
        private BitmapImage[] defaultUserAvatarsHighRes = new BitmapImage[5];
        private BitmapImage[] defaultGroupAvatars = new BitmapImage[5];
        private BitmapImage[] defaultGroupAvatarsHighRes = new BitmapImage[5];

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

        #region Chat Thread Colors

        private SolidColorBrush textBoxBackground;
        public SolidColorBrush TextBoxBackground
        {
            get
            {
                if (textBoxBackground == null)
                    textBoxBackground = new SolidColorBrush(Color.FromArgb(255, 238, 238, 236));

                return textBoxBackground;
            }
        }

        private SolidColorBrush smsBackground;
        public SolidColorBrush SmsBackground
        {
            get
            {
                if (smsBackground == null)
                    smsBackground = new SolidColorBrush(Color.FromArgb(255, 163, 210, 80));

                return smsBackground;
            }
        }

        SolidColorBrush _hikeMsgBackground;
        public SolidColorBrush HikeMsgBackground
        {
            get
            {
                if (_hikeMsgBackground == null)
                    _hikeMsgBackground = new SolidColorBrush(Color.FromArgb(255, 178, 229, 255));

                return _hikeMsgBackground;
            }
        }

        private SolidColorBrush receivedChatBubbleColor;
        public SolidColorBrush ReceivedChatBubbleColor
        {
            get
            {
                if (receivedChatBubbleColor == null)
                    receivedChatBubbleColor = new SolidColorBrush(Color.FromArgb(255, 0xef, 0xef, 0xef));

                return receivedChatBubbleColor;
            }
        }

        private SolidColorBrush receiveMessageForeground;
        public SolidColorBrush ReceiveMessageForeground
        {
            get
            {
                if (receiveMessageForeground == null)
                    receiveMessageForeground = new SolidColorBrush(Color.FromArgb(255, 83, 83, 83));

                return receiveMessageForeground;
            }
        }

        #endregion

        #region Standard Colors

        private SolidColorBrush transparent;
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
                return (SolidColorBrush)Application.Current.Resources["HikeBlueHeader"];
            }
        }

        private SolidColorBrush red;
        public SolidColorBrush Red
        {
            get
            {
                if (red == null)
                    red = new SolidColorBrush(Color.FromArgb(255, 0xe9, 0x54, 0x54));

                return red;
            }
        }

        private SolidColorBrush pink;
        public SolidColorBrush Pink
        {
            get
            {
                if (pink == null)
                    pink = new SolidColorBrush(Color.FromArgb(255, 0xf7, 0x52, 0x5a));

                return pink;
            }
        }

        private SolidColorBrush black;
        public SolidColorBrush Black
        {
            get
            {
                if (black == null)
                    black = new SolidColorBrush(Colors.Black);

                return black;
            }
        }

        private SolidColorBrush white;
        public SolidColorBrush White
        {
            get
            {
                if (white == null)
                    white = new SolidColorBrush(Colors.White);

                return white;
            }
        }

        private SolidColorBrush lightGray;
        public SolidColorBrush LightGray
        {
            get
            {
                if (lightGray == null)
                    lightGray = new SolidColorBrush(Color.FromArgb(255, 0xe5, 0xe5, 0xe2));

                return lightGray;
            }
        }

        SolidColorBrush gray;
        public SolidColorBrush Gray
        {
            get
            {
                if (gray == null)
                    gray = new SolidColorBrush(Color.FromArgb(255, 0x99, 0x99, 0x99));

                return gray;
            }
        }

        SolidColorBrush darkGray;
        public SolidColorBrush DarkGray
        {
            get
            {
                if (darkGray == null)
                    darkGray = new SolidColorBrush(Color.FromArgb(255, 0xdd, 0xdd, 0xdd));

                return darkGray;
            }
        }

        #endregion

        #region App Specific Colors

        public SolidColorBrush PhoneThemeColor
        {
            get
            {
                return (SolidColorBrush)Application.Current.Resources["HikeBlueHeader"];
            }
        }

        private SolidColorBrush statusTextForeground;
        public SolidColorBrush StatusTextForeground
        {
            get
            {
                if (statusTextForeground == null)
                    statusTextForeground = new SolidColorBrush(Color.FromArgb(255, 0x88, 0x88, 0x88));

                return statusTextForeground;
            }
        }

        private SolidColorBrush untappedCategoryColor;
        public SolidColorBrush UntappedCategoryColor
        {
            get
            {
                if (untappedCategoryColor == null)
                    untappedCategoryColor = new SolidColorBrush(Color.FromArgb(255, 0x4d, 0x4d, 0x4d));

                return untappedCategoryColor;
            }
        }

        #endregion

        private BitmapImage myLocationPin;
        public BitmapImage MyLocationPin
        {
            get
            {
                if (myLocationPin == null)
                    myLocationPin = new BitmapImage(new Uri("/view/images/MyLocation.png", UriKind.Relative));

                return myLocationPin;
            }
        }

        #region System Notifications

        private BitmapImage onHikeImage;
        public BitmapImage OnHikeImage
        {
            get
            {
                if (onHikeImage == null)
                    onHikeImage = new BitmapImage(new Uri("/View/images/chat_joined_blue.png", UriKind.Relative));

                return onHikeImage;
            }
        }

        private BitmapImage onHikeImage_ChatTheme;
        public BitmapImage OnHikeImage_ChatTheme
        {
            get
            {
                if (onHikeImage_ChatTheme == null)
                    onHikeImage_ChatTheme = new BitmapImage(new Uri("/View/images/chat_joined_blue_CT.png", UriKind.Relative));

                return onHikeImage_ChatTheme;
            }
        }

        BitmapImage notOnHikeImage;
        public BitmapImage NotOnHikeImage
        {
            get
            {
                if (notOnHikeImage == null)
                    notOnHikeImage = new BitmapImage(new Uri("/View/images/chat_invited_green.png", UriKind.Relative));

                return notOnHikeImage;
            }
        }

        BitmapImage notOnHikeImage_ChatTheme;
        public BitmapImage NotOnHikeImage_ChatTheme
        {
            get
            {
                if (notOnHikeImage_ChatTheme == null)
                    notOnHikeImage_ChatTheme = new BitmapImage(new Uri("/View/images/chat_invited_green_CT.png", UriKind.Relative));

                return notOnHikeImage_ChatTheme;
            }
        }

        private BitmapImage chatAcceptedImage;
        public BitmapImage ChatAcceptedImage
        {
            get
            {
                if (chatAcceptedImage == null)
                    chatAcceptedImage = new BitmapImage(new Uri("/View/images/chat_invited_green.png", UriKind.Relative));

                return chatAcceptedImage;
            }
        }

        private BitmapImage chatAcceptedImage_ChatTheme;
        public BitmapImage ChatAcceptedImage_ChatTheme
        {
            get
            {
                if (chatAcceptedImage_ChatTheme == null)
                    chatAcceptedImage_ChatTheme = new BitmapImage(new Uri("/View/images/chat_invited_green_CT.png", UriKind.Relative));

                return chatAcceptedImage_ChatTheme;
            }
        }

        private BitmapImage hikeToastImage;
        public BitmapImage HikeToastImage
        {
            get
            {
                if (hikeToastImage == null)
                    hikeToastImage = new BitmapImage(new Uri("/View/images/hike_toast_image.png", UriKind.RelativeOrAbsolute));

                return hikeToastImage;
            }
        }

        private BitmapImage httpFailed;
        public BitmapImage HttpFailed
        {
            get
            {
                if (httpFailed == null)
                    httpFailed = new BitmapImage(new Uri("/View/images/error_icon.png", UriKind.Relative));

                return httpFailed;
            }
        }

        private BitmapImage httpFailed_ChatTheme;
        public BitmapImage HttpFailed_ChatTheme
        {
            get
            {
                if (httpFailed_ChatTheme == null)
                    httpFailed_ChatTheme = new BitmapImage(new Uri("/View/images/error_icon_CT.png", UriKind.Relative));

                return httpFailed_ChatTheme;
            }
        }

        private BitmapImage reward;
        public BitmapImage Reward
        {
            get
            {
                if (reward == null)
                    reward = new BitmapImage(new Uri("/View/images/chat_reward.png", UriKind.Relative));

                return reward;
            }
        }

        private BitmapImage reward_ct;
        public BitmapImage Reward_ChatTheme
        {
            get
            {
                if (reward_ct == null)
                    reward_ct = new BitmapImage(new Uri("/View/images/chat_reward_ct.png", UriKind.Relative));
                return reward_ct;
            }
        }

        private BitmapImage chatSmsError;
        public BitmapImage IntUserBlocked
        {
            get
            {
                if (chatSmsError == null)
                    chatSmsError = new BitmapImage(new Uri("/View/images/chat_sms_error.png", UriKind.Relative));
                return chatSmsError;
            }
        }

        private BitmapImage chatSmsError_ct;
        public BitmapImage IntUserBlocked_ChatTheme
        {
            get
            {
                if (chatSmsError_ct == null)
                    chatSmsError_ct = new BitmapImage(new Uri("/View/images/chat_sms_error_CT.png", UriKind.Relative));
                return chatSmsError_ct;
            }
        }

        private BitmapImage grpNameChanged;
        public BitmapImage GrpNameChanged
        {
            get
            {
                if (grpNameChanged == null)
                    grpNameChanged = new BitmapImage(new Uri("/View/images/group_name_changed.png", UriKind.Relative));
                return grpNameChanged;
            }
        }

        private BitmapImage grpNameChanged_ct;
        public BitmapImage GrpNameChanged_ChatTheme
        {
            get
            {
                if (grpNameChanged_ct == null)
                    grpNameChanged_ct = new BitmapImage(new Uri("/View/images/group_name_changed_CT.png", UriKind.Relative));

                return grpNameChanged_ct;
            }
        }

        private BitmapImage grpPicChanged;
        public BitmapImage GrpPicChanged
        {
            get
            {
                if (grpPicChanged == null)
                    grpPicChanged = new BitmapImage(new Uri("/View/images/group_pic_changed.png", UriKind.Relative));
                return grpPicChanged;
            }
        }

        private BitmapImage grpPicChanged_ct;
        public BitmapImage GrpPicChanged_ChatTheme
        {
            get
            {
                if (grpPicChanged_ct == null)
                    grpPicChanged_ct = new BitmapImage(new Uri("/View/images/group_pic_changed_CT.png", UriKind.Relative));

                return grpPicChanged_ct;
            }
        }

        private BitmapImage chatBackgroundChanged;
        public BitmapImage ChatBackgroundChanged
        {
            get
            {
                if (chatBackgroundChanged == null)
                    chatBackgroundChanged = new BitmapImage(new Uri("/View/images/chatBackground_changed.png", UriKind.Relative));
                return chatBackgroundChanged;
            }
        }

        private BitmapImage chatBackgroundChanged_ct;
        public BitmapImage ChatBackgroundChanged_ChatTheme
        {
            get
            {
                if (chatBackgroundChanged_ct == null)
                    chatBackgroundChanged_ct = new BitmapImage(new Uri("/View/images/chatBackground_changed_CT.png", UriKind.Relative));

                return chatBackgroundChanged_ct;
            }
        }

        private BitmapImage participantLeft;
        public BitmapImage ParticipantLeft
        {
            get
            {
                if (participantLeft == null)
                    participantLeft = new BitmapImage(new Uri("/View/images/chat_left.png", UriKind.Relative));

                return participantLeft;
            }
        }

        private BitmapImage participantLeft_ct;
        public BitmapImage ParticipantLeft_ChatTheme
        {
            get
            {
                if (participantLeft_ct == null)
                    participantLeft_ct = new BitmapImage(new Uri("/View/images/chat_left_CT.png", UriKind.Relative));

                return participantLeft_ct;
            }
        }

        #endregion

        #region FTUE

        private BitmapImage overlayRupeeImage;
        public BitmapImage OverlayRupeeImage
        {
            get
            {
                if (overlayRupeeImage == null)
                    overlayRupeeImage = new BitmapImage(new Uri("/View/images/rupee.png", UriKind.Relative));

                return overlayRupeeImage;
            }
        }

        private BitmapImage overlaySmsImage;
        public BitmapImage OverlaySmsImage
        {
            get
            {
                if (overlaySmsImage == null)
                    overlaySmsImage = new BitmapImage(new Uri("/View/images/icon_sms.png", UriKind.Relative));

                return overlaySmsImage;
            }
        }

        private BitmapImage girlSelectedImage;
        public BitmapImage GirlSelectedImage
        {
            get
            {
                if (girlSelectedImage == null)
                    girlSelectedImage = new BitmapImage(new Uri("/View/images/FTUE/girl_selected.png", UriKind.Relative));

                return girlSelectedImage;
            }
        }

        private BitmapImage girlUnSelectedImage;
        public BitmapImage GirlUnSelectedImage
        {
            get
            {
                if (girlUnSelectedImage == null)
                    girlUnSelectedImage = new BitmapImage(new Uri("/View/images/FTUE/girl_unselected.png", UriKind.Relative));

                return girlUnSelectedImage;
            }
        }

        private BitmapImage boyUnSelectedImage;
        public BitmapImage BoyUnSelectedImage
        {
            get
            {
                if (boyUnSelectedImage == null)
                    boyUnSelectedImage = new BitmapImage(new Uri("/View/images/FTUE/boy_unselected.png", UriKind.Relative));

                return boyUnSelectedImage;
            }
        }

        private BitmapImage boySelectedImage;
        public BitmapImage BoySelectedImage
        {
            get
            {
                if (boySelectedImage == null)
                    boySelectedImage = new BitmapImage(new Uri("/View/images/FTUE/boy_selected.png", UriKind.Relative));

                return boySelectedImage;
            }
        }

        #endregion

        #region File Transfer

        private BitmapImage playIcon;
        public BitmapImage PlayIcon
        {
            get
            {
                if (playIcon == null)
                    playIcon = new BitmapImage(new Uri("/View/images/play_icon.png", UriKind.Relative));

                return playIcon;
            }
        }

        private BitmapImage pauseIcon;
        public BitmapImage PauseIcon
        {
            get
            {
                if (pauseIcon == null)
                    pauseIcon = new BitmapImage(new Uri("/View/images/pause_icon.png", UriKind.Relative));

                return pauseIcon;
            }
        }

        private BitmapImage downloadIcon;
        public BitmapImage DownloadIcon
        {
            get
            {
                if (downloadIcon == null)
                    downloadIcon = new BitmapImage(new Uri("/View/images/download_icon.png", UriKind.Relative));

                return downloadIcon;
            }
        }

        private BitmapImage downloadIconBigger;
        public BitmapImage DownloadIconBigger
        {
            get
            {
                if (downloadIconBigger == null)
                    downloadIconBigger = new BitmapImage(new Uri("/View/images/download_icon_big.png", UriKind.Relative));

                return downloadIconBigger;
            }
        }

        private BitmapImage fileAttachmnetIcon;
        public BitmapImage AttachmentIcon
        {
            get
            {
                if (fileAttachmnetIcon == null)
                    fileAttachmnetIcon = new BitmapImage(new Uri("/View/images/icon_file_attachment.png", UriKind.Relative));

                return fileAttachmnetIcon;
            }
        }

        private BitmapImage pausedFTRWhite;
        public BitmapImage PausedFTRWhite
        {
            get
            {
                if (pausedFTRWhite == null)
                    pausedFTRWhite = new BitmapImage(new Uri("/View/images/pause_white_ftr.png", UriKind.Relative));

                return pausedFTRWhite;
            }
        }

        private BitmapImage resumeFTRWhite;
        public BitmapImage ResumeFTRWhite
        {
            get
            {
                if (resumeFTRWhite == null)
                    resumeFTRWhite = new BitmapImage(new Uri("/View/images/resume_white_ftr.png", UriKind.Relative));

                return resumeFTRWhite;
            }
        }

        private BitmapImage pausedFTRBlack;
        public BitmapImage PausedFTRBlack
        {
            get
            {
                if (pausedFTRBlack == null)
                    pausedFTRBlack = new BitmapImage(new Uri("/View/images/pause_black_ftr.png", UriKind.Relative));

                return pausedFTRBlack;
            }
        }

        private BitmapImage resumeFTRBlack;
        public BitmapImage ResumeFTRBlack
        {
            get
            {
                if (resumeFTRBlack == null)
                    resumeFTRBlack = new BitmapImage(new Uri("/View/images/resume_black_ftr.png", UriKind.Relative));

                return resumeFTRBlack;
            }
        }

        #endregion

        #region SDR

        private BitmapImage sent;
        public BitmapImage Sent
        {
            get
            {
                if (sent == null)
                    sent = new BitmapImage(new Uri("/View/images/ic_sent.png", UriKind.Relative));

                return sent;
            }
        }

        private BitmapImage sent_Grey;
        public BitmapImage Sent_Grey
        {
            get
            {
                if (sent_Grey == null)
                    sent_Grey = new BitmapImage(new Uri("/View/images/ic_sent_grey.png", UriKind.Relative));

                return sent_Grey;
            }
        }

        private BitmapImage sent_ChatTheme;
        public BitmapImage Sent_ChatTheme
        {
            get
            {
                if (sent_ChatTheme == null)
                    sent_ChatTheme = new BitmapImage(new Uri("/View/images/ic_sent_CT.png", UriKind.Relative));
                return sent_ChatTheme;
            }
        }

        private BitmapImage delivered;
        public BitmapImage Delivered
        {
            get
            {
                if (delivered == null)
                    delivered = new BitmapImage(new Uri("/View/images/ic_delivered.png", UriKind.Relative));

                return delivered;
            }
        }

        private BitmapImage delivered_Grey;
        public BitmapImage Delivered_Grey
        {
            get
            {
                if (delivered_Grey == null)
                    delivered_Grey = new BitmapImage(new Uri("/View/images/ic_delivered_grey.png", UriKind.Relative));

                return delivered_Grey;
            }
        }

        private BitmapImage delivered_ChatTheme;
        public BitmapImage Delivered_ChatTheme
        {
            get
            {
                if (delivered_ChatTheme == null)
                    delivered_ChatTheme = new BitmapImage(new Uri("/View/images/ic_delivered_CT.png", UriKind.Relative));

                return delivered_ChatTheme;
            }
        }

        private BitmapImage read;
        public BitmapImage Read
        {
            get
            {
                if (read == null)
                    read = new BitmapImage(new Uri("/View/images/ic_read.png", UriKind.Relative));

                return read;
            }
        }

        private BitmapImage read_Grey;
        public BitmapImage Read_Grey
        {
            get
            {
                if (read_Grey == null)
                    read_Grey = new BitmapImage(new Uri("/View/images/ic_read_grey.png", UriKind.Relative));

                return read_Grey;
            }
        }

        private BitmapImage read_ChatTheme;
        public BitmapImage Read_ChatTheme
        {
            get
            {
                if (read_ChatTheme == null)
                    read_ChatTheme = new BitmapImage(new Uri("/View/images/ic_read_CT.png", UriKind.Relative));

                return read_ChatTheme;
            }
        }

        private BitmapImage trying_ChatTheme;
        public BitmapImage Trying_ChatTheme
        {
            get
            {
                if (trying_ChatTheme == null)
                    trying_ChatTheme = new BitmapImage(new Uri("/View/images/icon_sending_CT.png", UriKind.Relative));

                return trying_ChatTheme;
            }
        }

        private BitmapImage trying;
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

        private BitmapImage trying_Grey;
        public BitmapImage Trying_Grey
        {
            get
            {
                if (trying_Grey == null)
                {
                    trying_Grey = new BitmapImage(new Uri("/View/images/icon_sending_grey.png", UriKind.Relative));
                }
                return trying_Grey;
            }
        }

        private BitmapImage waiting;
        public BitmapImage Waiting
        {
            get
            {
                if (waiting == null)
                    waiting = new BitmapImage(new Uri("/View/images/chat_waiting.png", UriKind.Relative));
                return waiting;
            }
        }

        private BitmapImage waiting_ct;
        public BitmapImage Waiting_ChatTheme
        {
            get
            {
                if (waiting_ct == null)
                    waiting_ct = new BitmapImage(new Uri("/View/images/chat_waiting_CT.png", UriKind.Relative));

                return waiting_ct;
            }
        }

        #endregion

        #region Chat Thread

        private BitmapImage typingNotificationWhite;
        public BitmapImage TypingNotificationWhite
        {
            get
            {
                if (typingNotificationWhite == null)
                    typingNotificationWhite = new BitmapImage(new Uri("/view/images/typing_white.png", UriKind.Relative));

                return typingNotificationWhite;
            }
        }

        private BitmapImage typingNotificationBlack;
        public BitmapImage TypingNotificationBlack
        {
            get
            {
                if (typingNotificationBlack == null)
                    typingNotificationBlack = new BitmapImage(new Uri("/view/images/typing.png", UriKind.Relative));

                return typingNotificationBlack;
            }
        }

        private BitmapImage blackContactIcon;
        public BitmapImage BlackContactIcon
        {
            get
            {
                if (blackContactIcon == null)
                    blackContactIcon = new BitmapImage(new Uri("/View/images/menu_contact_icon_black.png", UriKind.Relative));

                return blackContactIcon;
            }
        }

        private BitmapImage walkieTalkieImage;
        public BitmapImage WalkieTalkieImage
        {
            get
            {
                if (walkieTalkieImage == null)
                    walkieTalkieImage = new BitmapImage(new Uri("/View/images/Mic_icon_black_small.png", UriKind.Relative));

                return walkieTalkieImage;
            }
        }

        private BitmapImage sendMessageImage;
        public BitmapImage SendMessageImage
        {
            get
            {
                if (sendMessageImage == null)
                    sendMessageImage = new BitmapImage(new Uri("/View/images/sendMessage.png", UriKind.Relative));

                return sendMessageImage;
            }
        }

        #region Nudge

        private BitmapImage nudgeSend;
        public BitmapImage NudgeSent
        {
            get
            {
                if (nudgeSend == null)
                    nudgeSend = new BitmapImage(new Uri("/View/images/nudge_sent.png", UriKind.Relative));

                return nudgeSend;
            }
        }

        private BitmapImage nudgeReceived;
        public BitmapImage NudgeReceived
        {
            get
            {
                if (nudgeReceived == null)
                    nudgeReceived = new BitmapImage(new Uri("/View/images/nudge_received.png", UriKind.Relative));

                return nudgeReceived;
            }
        }

        private BitmapImage heartNudgeSend;
        public BitmapImage HeartNudgeSent
        {
            get
            {
                if (heartNudgeSend == null)
                    heartNudgeSend = new BitmapImage(new Uri("/View/images/heartsNudgeSent.png", UriKind.Relative));

                return heartNudgeSend;
            }
        }

        private BitmapImage heartNudgeReceived;
        public BitmapImage HeartNudgeReceived
        {
            get
            {
                if (heartNudgeReceived == null)
                    heartNudgeReceived = new BitmapImage(new Uri("/View/images/heartsNudgeReceived.png", UriKind.Relative));

                return heartNudgeReceived;
            }
        }

        private BitmapImage whiteSentNudgeImage;
        public BitmapImage WhiteSentNudgeImage
        {
            get
            {
                if (whiteSentNudgeImage == null)
                    whiteSentNudgeImage = new BitmapImage(new Uri("/View/images/nudge_sent.png", UriKind.Relative));

                return whiteSentNudgeImage;
            }
        }

        private BitmapImage whiteReceivedNudgeImage;
        public BitmapImage WhiteReceivedNudgeImage
        {
            get
            {
                if (whiteReceivedNudgeImage == null)
                    whiteReceivedNudgeImage = new BitmapImage(new Uri("/View/images/nudge_received.png", UriKind.Relative));

                return whiteReceivedNudgeImage;
            }
        }

        private BitmapImage blueReceivedNudgeImage;
        public BitmapImage BlueReceivedNudgeImage
        {
            get
            {
                if (blueReceivedNudgeImage == null)
                    blueReceivedNudgeImage = new BitmapImage(new Uri("/View/images/nudge_received_blue.png", UriKind.Relative));

                return blueReceivedNudgeImage;
            }
        }

        private BitmapImage blueSentNudgeImage;
        public BitmapImage BlueSentNudgeImage
        {
            get
            {
                if (blueSentNudgeImage == null)
                    blueSentNudgeImage = new BitmapImage(new Uri("/View/images/nudge_sent_blue.png", UriKind.Relative));

                return blueSentNudgeImage;
            }
        }

        #endregion

        #region Walkie Talkie

        BitmapImage blankBitmapImage;
        public BitmapImage BlankBitmapImage
        {
            get
            {
                if (blankBitmapImage == null)
                    blankBitmapImage = new BitmapImage();

                return blankBitmapImage;
            }
        }

        BitmapImage dustbinWhiteImage;
        public BitmapImage DustbinWhiteImage
        {
            get
            {
                if (dustbinWhiteImage == null)
                    dustbinWhiteImage = new BitmapImage(new Uri("/View/images/deleted_white_icon.png", UriKind.Relative));

                return dustbinWhiteImage;
            }
        }

        BitmapImage walkieTalkieWhiteImage;
        public BitmapImage WalkieTalkieWhiteImage
        {
            get
            {
                if (walkieTalkieWhiteImage == null)
                    walkieTalkieWhiteImage = new BitmapImage(new Uri("/View/images/Walkie_Talkie_White_small.png", UriKind.Relative));

                return walkieTalkieWhiteImage;
            }
        }

        BitmapImage closeButtonWhiteImage;
        public BitmapImage CloseButtonWhiteImage
        {
            get
            {
                if (closeButtonWhiteImage == null)
                    closeButtonWhiteImage = new BitmapImage(new Uri("/View/images/close_white.png", UriKind.Relative));

                return closeButtonWhiteImage;
            }
        }

        #endregion

        #region Chat Theme

        private BitmapImage circles;
        public BitmapImage Circles
        {
            get
            {
                if (circles == null)
                    circles = new BitmapImage(new Uri("/View/images/circles.png", UriKind.Relative));

                return circles;
            }
        }

        #endregion

        #region Emoticons

        private BitmapImage emotCat1Inactive;
        public BitmapImage EmotCat1Inactive
        {
            get
            {
                if (emotCat1Inactive == null)
                    emotCat1Inactive = new BitmapImage(new Uri("/View/images/emoticons/emote_cat1_inactive.png", UriKind.Relative));

                return emotCat1Inactive;
            }
        }

        private BitmapImage emotCat2Inactive;
        public BitmapImage EmotCat2Inactive
        {
            get
            {
                if (emotCat2Inactive == null)
                    emotCat2Inactive = new BitmapImage(new Uri("/View/images/emoticons/emote_cat2_inactive.png", UriKind.Relative));

                return emotCat2Inactive;
            }
        }

        private BitmapImage emotCat3Inactive;
        public BitmapImage EmotCat3Inactive
        {
            get
            {
                if (emotCat3Inactive == null)
                    emotCat3Inactive = new BitmapImage(new Uri("/View/images/emoticons/emote_cat3_inactive.png", UriKind.Relative));

                return emotCat3Inactive;
            }
        }

        private BitmapImage emotCat4Inactive;
        public BitmapImage EmotCat4Inactive
        {
            get
            {
                if (emotCat4Inactive == null)
                    emotCat4Inactive = new BitmapImage(new Uri("/View/images/emoticons/emote_cat4_inactive.png", UriKind.Relative));

                return emotCat4Inactive;
            }
        }

        private BitmapImage emotCat1Active;
        public BitmapImage EmotCat1Active
        {
            get
            {
                if (emotCat1Active == null)
                    emotCat1Active = new BitmapImage(new Uri("/View/images/emoticons/emote_cat1_active.png", UriKind.Relative));

                return emotCat1Active;
            }
        }

        private BitmapImage emotCat2Active;
        public BitmapImage EmotCat2Active
        {
            get
            {
                if (emotCat2Active == null)
                    emotCat2Active = new BitmapImage(new Uri("/View/images/emoticons/emote_cat2_active.png", UriKind.Relative));

                return emotCat2Active;
            }
        }

        private BitmapImage emotCat3Active;
        public BitmapImage EmotCat3Active
        {
            get
            {
                if (emotCat3Active == null)
                    emotCat3Active = new BitmapImage(new Uri("/View/images/emoticons/emote_cat3_active.png", UriKind.Relative));

                return emotCat3Active;
            }
        }

        private BitmapImage emotCat4Active;
        public BitmapImage EmotCat4Active
        {
            get
            {
                if (emotCat4Active == null)
                    emotCat4Active = new BitmapImage(new Uri("/View/images/emoticons/emote_cat4_active.png", UriKind.Relative));

                return emotCat4Active;
            }
        }

        #endregion

        #region Stickers

        #region Sticker Category Overlays

        private BitmapImage humanoidOverlay;
        public BitmapImage HumanoidOverlay
        {
            get
            {
                if (humanoidOverlay == null)
                    humanoidOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_overlay.png", UriKind.Relative));

                return humanoidOverlay;
            }
        }

        private BitmapImage humanoid2Overlay;
        public BitmapImage Humanoid2Overlay
        {
            get
            {
                if (humanoid2Overlay == null)
                    humanoid2Overlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_overlay.png", UriKind.Relative));

                return humanoid2Overlay;
            }
        }

        private BitmapImage doggyOverlay;
        public BitmapImage DoggyOverlay
        {
            get
            {
                if (doggyOverlay == null)
                    doggyOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_overlay.png", UriKind.Relative));

                return doggyOverlay;
            }
        }

        private BitmapImage kittyOverlay;
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

        private BitmapImage bollywoodOverlay;
        public BitmapImage BollywoodOverlay
        {
            get
            {
                if (bollywoodOverlay == null)
                    bollywoodOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/bolly_overlay.png", UriKind.Relative));

                return bollywoodOverlay;
            }
        }

        private BitmapImage trollOverlay;
        public BitmapImage TrollOverlay
        {
            get
            {
                if (trollOverlay == null)
                    trollOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/rage_overlay.png", UriKind.Relative));

                return trollOverlay;
            }
        }

        private BitmapImage avatarsOverlay;
        public BitmapImage AvatarsOverlay
        {
            get
            {
                if (avatarsOverlay == null)
                    avatarsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars_overlay.png", UriKind.Relative));

                return avatarsOverlay;
            }
        }

        private BitmapImage indiansOverlay;
        public BitmapImage IndiansOverlay
        {
            get
            {
                if (indiansOverlay == null)
                    indiansOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_overlay.png", UriKind.Relative));

                return indiansOverlay;
            }
        }
        private BitmapImage sportsOverlay;
        public BitmapImage SportsOverlay
        {
            get
            {
                if (sportsOverlay == null)
                    sportsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/sports_overlay.png", UriKind.Relative));

                return sportsOverlay;
            }
        }
        private BitmapImage angryOverlay;
        public BitmapImage AngryOverlay
        {
            get
            {
                if (angryOverlay == null)
                    angryOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/angry_overlay.png", UriKind.Relative));

                return angryOverlay;
            }
        }

        private BitmapImage loveOverlay;
        public BitmapImage LoveOverlay
        {
            get
            {
                if (loveOverlay == null)
                    loveOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/love_overlay.png", UriKind.Relative));

                return loveOverlay;
            }
        }

        private BitmapImage expressionsOverlay;
        public BitmapImage ExpressionsOverlay
        {
            get
            {
                if (expressionsOverlay == null)
                    expressionsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions_overlay.png", UriKind.Relative));

                return expressionsOverlay;
            }
        }

        private BitmapImage smileyExpressionsOverlay;
        public BitmapImage SmileyExpressionsOverlay
        {
            get
            {
                if (smileyExpressionsOverlay == null)
                {
                    smileyExpressionsOverlay = new BitmapImage(new Uri("/View/images/stickers/categorySets/goofy_overlay.png", UriKind.Relative));
                }
                return smileyExpressionsOverlay;
            }
        }

        #endregion

        #region Sticker categroy icons

        private BitmapImage humanoidInactive;
        public BitmapImage HumanoidInactive
        {
            get
            {
                if (humanoidInactive == null)
                    humanoidInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_inactive.png", UriKind.Relative));

                return humanoidInactive;
            }
        }

        private BitmapImage humanoid2Inactive;
        public BitmapImage Humanoid2Inactive
        {
            get
            {
                if (humanoid2Inactive == null)
                    humanoid2Inactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_inactive.png", UriKind.Relative));

                return humanoid2Inactive;
            }
        }

        private BitmapImage doggyInactive;
        public BitmapImage DoggyInactive
        {
            get
            {
                if (doggyInactive == null)
                    doggyInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_inactive.png", UriKind.Relative));

                return doggyInactive;
            }
        }

        private BitmapImage kittyInactive;
        public BitmapImage KittyInactive
        {
            get
            {
                if (kittyInactive == null)
                    kittyInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/kitty_inactive.png", UriKind.Relative));

                return kittyInactive;
            }
        }

        private BitmapImage bollywoodInactive;
        public BitmapImage BollywoodInactive
        {
            get
            {
                if (bollywoodInactive == null)
                    bollywoodInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/bolly_inactive.png", UriKind.Relative));

                return bollywoodInactive;
            }
        }

        private BitmapImage trollInactive;
        public BitmapImage TrollInactive
        {
            get
            {
                if (trollInactive == null)
                    trollInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/rage_inactive.png", UriKind.Relative));

                return trollInactive;
            }
        }

        private BitmapImage expressionsInactive;
        public BitmapImage ExpressionsInactive
        {
            get
            {
                if (expressionsInactive == null)
                    expressionsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions_inactive.png", UriKind.Relative));

                return expressionsInactive;
            }
        }

        private BitmapImage smileyExpressionsInactive;
        public BitmapImage SmileyExpressionsInactive
        {
            get
            {
                if (smileyExpressionsInactive == null)
                    smileyExpressionsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/goofy_inactive.png", UriKind.Relative));

                return smileyExpressionsInactive;
            }
        }

        private BitmapImage avatarsInactive;
        public BitmapImage AvatarsInactive
        {
            get
            {
                if (avatarsInactive == null)
                    avatarsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars_inactive.png", UriKind.Relative));

                return avatarsInactive;
            }
        }

        private BitmapImage indianInactive;
        public BitmapImage IndianInactive
        {
            get
            {
                if (indianInactive == null)
                    indianInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_inactive.png", UriKind.Relative));

                return indianInactive;
            }
        }
        private BitmapImage sportsInactive;
        public BitmapImage SportsInactive
        {
            get
            {
                if (sportsInactive == null)
                    sportsInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/sports_inactive.png", UriKind.Relative));

                return sportsInactive;
            }
        }
        BitmapImage loveInactive;
        public BitmapImage LoveInactive
        {
            get
            {
                if (loveInactive == null)
                    loveInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/love_inactive.png", UriKind.Relative));

                return loveInactive;
            }
        }

        private BitmapImage angryInactive;
        public BitmapImage AngryInactive
        {
            get
            {
                if (angryInactive == null)
                    angryInactive = new BitmapImage(new Uri("/View/images/stickers/categorySets/angry_inactive.png", UriKind.Relative));

                return angryInactive;
            }
        }

        private BitmapImage recentIconInActive;
        public BitmapImage RecentIconInActive
        {
            get
            {
                if (recentIconInActive == null)
                    recentIconInActive = new BitmapImage(new Uri("/View/images/recent_inactive.png", UriKind.Relative));

                return recentIconInActive;
            }
        }

        private BitmapImage humanoidActive;
        public BitmapImage HumanoidActive
        {
            get
            {
                if (humanoidActive == null)
                    humanoidActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid_active.png", UriKind.Relative));

                return humanoidActive;
            }
        }

        private BitmapImage humanoid2Active;
        public BitmapImage Humanoid2Active
        {
            get
            {
                if (humanoid2Active == null)
                    humanoid2Active = new BitmapImage(new Uri("/View/images/stickers/categorySets/humanoid2_active.png", UriKind.Relative));

                return humanoid2Active;
            }
        }

        private BitmapImage doggyActive;
        public BitmapImage DoggyActive
        {
            get
            {
                if (doggyActive == null)
                    doggyActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/doggy_active.png", UriKind.Relative));

                return doggyActive;
            }
        }

        private BitmapImage kittyActive;
        public BitmapImage KittyActive
        {
            get
            {
                if (kittyActive == null)
                    kittyActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/kitty_active.png", UriKind.Relative));

                return kittyActive;
            }
        }

        private BitmapImage bollywoodActive;
        public BitmapImage BollywoodActive
        {
            get
            {
                if (bollywoodActive == null)
                    bollywoodActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/bolly_active.png", UriKind.Relative));

                return bollywoodActive;
            }
        }

        private BitmapImage trollActive;
        public BitmapImage TrollActive
        {
            get
            {
                if (trollActive == null)
                    trollActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/rage_active.png", UriKind.Relative));

                return trollActive;
            }
        }

        private BitmapImage expressionsActive;
        public BitmapImage ExpressionsActive
        {
            get
            {
                if (expressionsActive == null)
                    expressionsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/expressions_active.png", UriKind.Relative));

                return expressionsActive;
            }
        }

        private BitmapImage smileyExpressionsActive;
        public BitmapImage SmileyExpressionsActive
        {
            get
            {
                if (smileyExpressionsActive == null)
                    smileyExpressionsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/goofy_active.png", UriKind.Relative));

                return smileyExpressionsActive;
            }
        }

        private BitmapImage avatarsActive;
        public BitmapImage AvatarsActive
        {
            get
            {
                if (avatarsActive == null)
                    avatarsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/avatars_active.png", UriKind.Relative));

                return avatarsActive;
            }
        }

        private BitmapImage indianActive;
        public BitmapImage IndianActive
        {
            get
            {
                if (indianActive == null)
                    indianActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/indian_active.png", UriKind.Relative));

                return indianActive;
            }
        }
        private BitmapImage sportsActive;
        public BitmapImage SportsActive
        {
            get
            {
                if (sportsActive == null)
                    sportsActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/sports_active.png", UriKind.Relative));

                return sportsActive;
            }
        }
        private BitmapImage angryActive;
        public BitmapImage AngryActive
        {
            get
            {
                if (angryActive == null)
                    angryActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/angry_active.png", UriKind.Relative));

                return angryActive;
            }
        }

        private BitmapImage loveActive;
        public BitmapImage LoveActive
        {
            get
            {
                if (loveActive == null)
                    loveActive = new BitmapImage(new Uri("/View/images/stickers/categorySets/love_active.png", UriKind.Relative));

                return loveActive;
            }
        }

        private BitmapImage recentIconActive;
        public BitmapImage RecentIconActive
        {
            get
            {
                if (recentIconActive == null)
                    recentIconActive = new BitmapImage(new Uri("/View/images/recent_active.png", UriKind.Relative));

                return recentIconActive;
            }
        }

        #endregion

        private BitmapImage loadingImage;
        public BitmapImage StickerLoadingImage
        {
            get
            {
                if (loadingImage == null)
                    loadingImage = new BitmapImage(new Uri("/View/images/loading.png", UriKind.Relative));

                return loadingImage;
            }
        }

        #endregion

        #endregion

        #region Profile

        private BitmapImage textStatusImage;
        public BitmapImage TextStatusImage
        {
            get
            {
                if (textStatusImage == null)
                    textStatusImage = new BitmapImage(new Uri("/View/images/timeline_status.png", UriKind.Relative));

                return textStatusImage;
            }
        }

        private BitmapImage profilePicStatusImage;
        public BitmapImage ProfilePicStatusImage
        {
            get
            {
                if (profilePicStatusImage == null)
                    profilePicStatusImage = new BitmapImage(new Uri("/View/images/timeline_photo.png", UriKind.Relative));

                return profilePicStatusImage;
            }
        }

        private BitmapImage userProfileLockImage;
        public BitmapImage UserProfileLockImage
        {
            get
            {
                if (userProfileLockImage == null)
                    userProfileLockImage = new BitmapImage(new Uri("/View/images/user_lock.png", UriKind.Relative));

                return userProfileLockImage;
            }
        }

        private BitmapImage userProfileInviteImage;
        public BitmapImage UserProfileInviteImage
        {
            get
            {
                if (userProfileInviteImage == null)
                    userProfileInviteImage = new BitmapImage(new Uri("/View/images/user_invite.png", UriKind.Relative));

                return userProfileInviteImage;
            }
        }

        #endregion

        #region Conversation Page Tabs

        BitmapImage statusTabImageSelected;
        public BitmapImage StatusTabImageSelected
        {
            get
            {
                if (statusTabImageSelected == null)
                    statusTabImageSelected = new BitmapImage(new Uri("/View/images/ConversationPage/status_Selected.png", UriKind.Relative));

                return statusTabImageSelected;
            }
        }

        BitmapImage statusTabImageNotSelected;
        public BitmapImage StatusTabImageNotSelected
        {
            get
            {
                if (statusTabImageNotSelected == null)
                    statusTabImageNotSelected = new BitmapImage(new Uri("/View/images/ConversationPage/status_NotSelected.png", UriKind.Relative));

                return statusTabImageNotSelected;
            }
        }

        BitmapImage chatsTabImageSelected;
        public BitmapImage ChatsTabImageSelected
        {
            get
            {
                if (chatsTabImageSelected == null)
                    chatsTabImageSelected = new BitmapImage(new Uri("/View/images/ConversationPage/chat_Selected.png", UriKind.Relative));

                return chatsTabImageSelected;
            }
        }

        BitmapImage chatsTabImageNotSelected;
        public BitmapImage ChatsTabImageNotSelected
        {
            get
            {
                if (chatsTabImageNotSelected == null)
                    chatsTabImageNotSelected = new BitmapImage(new Uri("/View/images/ConversationPage/chat_NotSelected.png", UriKind.Relative));

                return chatsTabImageNotSelected;
            }
        }

        BitmapImage profileTabImageSelected;
        public BitmapImage FriendsTabImageSelected
        {
            get
            {
                if (friendsTabImageSelected == null)
                    friendsTabImageSelected = new BitmapImage(new Uri("/View/images/ConversationPage/friend_Selected.png", UriKind.Relative));

                return friendsTabImageSelected;
            }
        }

        BitmapImage profileTabImageNotSelected;
        public BitmapImage FriendsTabImageNotSelected
        {
            get
            {
                if (friendsTabImageNotSelected == null)
                    friendsTabImageNotSelected = new BitmapImage(new Uri("/View/images/ConversationPage/friend_NotSelected.png", UriKind.Relative));

                return friendsTabImageNotSelected;
            }
        }

        BitmapImage friendsTabImageSelected;
        public BitmapImage ProfileTabImageSelected
        {
            get
            {
                if (profileTabImageSelected == null)
                    profileTabImageSelected = new BitmapImage(new Uri("/View/images/profile_Selected.png", UriKind.Relative));

                return profileTabImageSelected;
            }
        }

        BitmapImage friendsTabImageNotSelected;
        public BitmapImage ProfileTabImageNotSelected
        {
            get
            {
                if (profileTabImageNotSelected == null)
                    profileTabImageNotSelected = new BitmapImage(new Uri("/View/images/profile_NotSelected.png", UriKind.Relative));

                return profileTabImageNotSelected;
            }
        }

        #endregion

        #region Thickness

        private Thickness newChatThreadEmoticonMargin = new Thickness(0, 10, 0, -10);
        public Thickness NewChatThreadEmoticonMargin
        {
            get
            {
                return newChatThreadEmoticonMargin;
            }
        }

        private Thickness convListEmoticonMargin = new Thickness(0, 3, 0, -5);
        public Thickness ConvListEmoticonMargin
        {
            get
            {
                return convListEmoticonMargin;
            }
        }

        Thickness _zeroThickness = new Thickness(0);
        public Thickness ZeroThickness
        {
            get
            {
                return _zeroThickness;
            }
        }

        Thickness _newCategoryThickness= new Thickness(0, 2, 0, 0);
        public Thickness NewCategoryThickness
        {
            get
            {
                return _newCategoryThickness;
            }
        }

        #endregion

        BitmapImage profileTickImage;
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
            return sumOfCodes % 5;
        }

        public BitmapImage getDefaultAvatar(string msisdn, bool isHighRes)
        {
            if (msisdn == App.MSISDN)
                msisdn = HikeConstants.MY_PROFILE_PIC;

            int index = computeHash(msisdn);
            if (isHighRes)
            {
                if (defaultUserAvatarsHighRes[index] == null)
                {
                    switch (index)
                    {
                        case 0:
                            defaultUserAvatarsHighRes[0] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_hires_blue.png", UriKind.Relative));
                            break;
                        case 1:
                            defaultUserAvatarsHighRes[1] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_hires_green.png", UriKind.Relative));
                            break;
                        case 2:
                            defaultUserAvatarsHighRes[2] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_hires_orange.png", UriKind.Relative));
                            break;
                        case 3:
                            defaultUserAvatarsHighRes[3] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_hires_pink.png", UriKind.Relative));
                            break;
                        case 4:
                            defaultUserAvatarsHighRes[4] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_hires_purple.png", UriKind.Relative));
                            break;
                    }
                }

                return defaultUserAvatarsHighRes[index];
            }
            else
            {
                if (defaultUserAvatars[index] == null)
                {
                    switch (index)
                    {
                        case 0:
                            defaultUserAvatars[0] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_blue.png", UriKind.Relative));
                            break;
                        case 1:
                            defaultUserAvatars[1] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_green.png", UriKind.Relative));
                            break;
                        case 2:
                            defaultUserAvatars[2] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_orange.png", UriKind.Relative));
                            break;
                        case 3:
                            defaultUserAvatars[3] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_pink.png", UriKind.Relative));
                            break;
                        case 4:
                            defaultUserAvatars[4] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_purple.png", UriKind.Relative));
                            break;
                    }
                }

                return defaultUserAvatars[index];
            }
        }

        public BitmapImage getDefaultGroupAvatar(string groupId, bool isHighRes)
        {
            int index = computeHash(groupId);

            if (isHighRes)
            {
                if (defaultGroupAvatarsHighRes[index] == null)
                {
                    switch (index)
                    {
                        case 0:
                            defaultGroupAvatarsHighRes[0] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_hires_blue.png", UriKind.Relative));
                            break;
                        case 1:
                            defaultGroupAvatarsHighRes[1] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_hires_green.png", UriKind.Relative));
                            break;
                        case 2:
                            defaultGroupAvatarsHighRes[2] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_hires_orange.png", UriKind.Relative));
                            break;
                        case 3:
                            defaultGroupAvatarsHighRes[3] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_hires_pink.png", UriKind.Relative));
                            break;
                        case 4:
                            defaultGroupAvatarsHighRes[4] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_hires_purple.png", UriKind.Relative));
                            break;
                    }
                }

                return defaultGroupAvatarsHighRes[index];
            }
            else
            {
                if (defaultGroupAvatars[index] == null)
                {
                    switch (index)
                    {
                        case 0:
                            defaultGroupAvatars[0] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_blue.png", UriKind.Relative));
                            break;
                        case 1:
                            defaultGroupAvatars[1] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_green.png", UriKind.Relative));
                            break;
                        case 2:
                            defaultGroupAvatars[2] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_orange.png", UriKind.Relative));
                            break;
                        case 3:
                            defaultGroupAvatars[3] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_pink.png", UriKind.Relative));
                            break;
                        case 4:
                            defaultGroupAvatars[4] = new BitmapImage(new Uri("/View/images/avatars/default_avatar_group_purple.png", UriKind.Relative));
                            break;
                    }
                }
                return defaultGroupAvatars[index];
            }
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
            if (msisdn == App.MSISDN)
                msisdn = HikeConstants.MY_PROFILE_PIC;
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
                return getDefaultGroupAvatar(msisdn, false);
            return getDefaultAvatar(msisdn, false);
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
