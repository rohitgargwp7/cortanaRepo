﻿using Microsoft.Phone.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class ConversationBox : UserControl
    {
        private BitmapImage _avatarImage;
        private string _userName;
        private string _lastMessage;
        private long _timestamp;
        private string _msisdn;
        private ConvMessage.State _messageState;
        private MenuItem favouriteMenuItem;
        public BitmapImage AvatarImage
        {
            get
            {
                return _avatarImage;
            }
            set
            {
                if (_avatarImage != value)
                {
                    _avatarImage = value;
                    this.profileImage.Source = _avatarImage;
                }
            }
        }

        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                if (_userName != value)
                {
                    _userName = value;
                    this.userNameTxtBlck.Text = !string.IsNullOrWhiteSpace(_userName) ? _userName : _msisdn;
                }
            }
        }

        public string LastMessage
        {
            get
            {
                return _lastMessage;
            }
            set
            {
                if (_lastMessage != value)
                {
                    _lastMessage = value;
                    Paragraph p = SmileyParser.Instance.LinkifyEmoticons(_lastMessage);
                    this.lastMessageTxtBlck.Blocks.Clear();
                    this.lastMessageTxtBlck.Blocks.Add(p);
                }
            }
        }

        public long Timestamp
        {
            get
            {
                return _timestamp;
            }
            set
            {
                if (_timestamp != value)
                {
                    _timestamp = value;
                    this.timestampTxtBlck.Text = TimeUtils.getTimeString(_timestamp);
                }
            }
        }

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (_msisdn != value.Trim())
                {
                    _msisdn = value.Trim();
                }
            }
        }
        public ConvMessage.State MessageState
        {
            get
            {
                return _messageState;
            }
            set
            {
                if (_messageState != value)
                {
                    _messageState = value;
                    switch (_messageState)
                    {
                        case ConvMessage.State.SENT_CONFIRMED:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.LastMsgForeground;
                            sdrImage.Source = UI_Utils.Instance.Sent;
                            sdrImage.Visibility = Visibility.Visible;
                            unreadCircle.Visibility = Visibility.Collapsed;
                            break;
                        case ConvMessage.State.SENT_DELIVERED:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.LastMsgForeground;
                            sdrImage.Source = UI_Utils.Instance.Delivered;
                            sdrImage.Visibility = Visibility.Visible;
                            unreadCircle.Visibility = Visibility.Collapsed;
                            break;
                        case ConvMessage.State.SENT_DELIVERED_READ:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.LastMsgForeground;
                            sdrImage.Source = UI_Utils.Instance.Read;
                            sdrImage.Visibility = Visibility.Visible;
                            unreadCircle.Visibility = Visibility.Collapsed;
                            break;
                        case ConvMessage.State.SENT_UNCONFIRMED:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.LastMsgForeground;
                            sdrImage.Source = UI_Utils.Instance.Trying;
                            sdrImage.Visibility = Visibility.Visible;
                            unreadCircle.Visibility = Visibility.Collapsed;
                            break;
                        case ConvMessage.State.RECEIVED_UNREAD:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.PhoneThemeColor;
                            sdrImage.Visibility = Visibility.Collapsed;
                            unreadCircle.Visibility = Visibility.Visible;
                            unreadCircle.Fill = UI_Utils.Instance.PhoneThemeColor;
                            break;
                        default:
                            lastMessageTxtBlck.Foreground = UI_Utils.Instance.LastMsgForeground;
                            sdrImage.Visibility = Visibility.Collapsed;
                            unreadCircle.Visibility = Visibility.Collapsed;
                            break;
                    }
                }
            }
        }

        public MenuItem FavouriteMenuItem
        {
            get
            {
                return favouriteMenuItem;
            }
            set
            {
                if (this.favouriteMenuItem != value)
                    this.favouriteMenuItem = value;
            }
        }

        public ConversationBox(BitmapImage avatarImage, string userName, string lastMessage, long timestamp, ConvMessage.State messageState, string msisdn)
        {
            InitializeComponent();
            this.AvatarImage = avatarImage;
            this.UserName = userName;
            this.LastMessage = lastMessage;
            this.Timestamp = timestamp;
            this.MessageState = messageState;
            this.Msisdn = msisdn;
        }

        public ConversationBox(ConversationListObject c)
        {
            InitializeComponent();
            this.AvatarImage = c.AvatarImage;
            this.UserName = c.NameToShow;
            this.LastMessage = c.LastMessage;
            this.Timestamp = c.TimeStamp;
            this.MessageState = c.MessageStatus;
            this.Msisdn = c.Msisdn;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            ConversationBox o = obj as ConversationBox;

            if (o == null)
            {
                return false;
            }
            return (_msisdn == o.Msisdn);
        }

        public void UpdateContextMenuFavourites(bool isFav)
        {
            if (favouriteMenuItem != null)
            {
                if (isFav) // if already favourite
                    favouriteMenuItem.Header = AppResources.RemFromFav_Txt;
                else
                    favouriteMenuItem.Header = AppResources.Add_To_Fav_Txt;
            }
        }
    }
}
