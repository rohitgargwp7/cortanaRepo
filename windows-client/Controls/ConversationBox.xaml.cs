using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Model;

namespace windows_client.Controls
{
    public partial class ConversationBox : UserControl
    {
        private BitmapImage _avatarImage;
        private string _userName;
        private string _lastMessage;
        private long _timestamp;
        private ConvMessage.State _messageState;

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
                    this.userNameTxtBlck.Text = _userName;
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
                    this.lastMessageTxtBlck.Text = _lastMessage;
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
                }
            }
        }

        public ConversationBox(BitmapImage avatarImage, string userName, string lastMessage, long timestamp, ConvMessage.State messageState)
        {
            InitializeComponent();
            this.AvatarImage = avatarImage;
            this.UserName = userName;
            this.LastMessage = lastMessage;
            this.Timestamp = timestamp;
            this.MessageState = messageState;
        }

        public ConversationBox(ConversationListObject c)
        {
            InitializeComponent();
            update(c);
        }

        public void update(ConversationListObject c)
        {
            this.AvatarImage = c.AvatarImage;
            this.UserName = c.ContactName;
            this.LastMessage = c.LastMessage;
            this.Timestamp = c.TimeStamp;
            this.MessageState = c.MessageStatus;
        }

    }
}
