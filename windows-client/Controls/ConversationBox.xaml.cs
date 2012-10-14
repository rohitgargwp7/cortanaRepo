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
        private ConvMessage.State _messageStatus;
        private byte[] _avatar;

        //GK add message id as private member if required

        public ConversationBox(byte[] avatar, string userName, string lastMessage, long timeStamp,
            bool isNotification,  //set it true for event notification messages
            ConvMessage.State messageState)
        {
            this._avatar = avatar;
            this.userName.Text = userName;
            this.lastMessage.Text = lastMessage;
            this.timestamp.Text = TimeUtils.getTimeString(timeStamp);
            this._messageStatus = messageState;
        }

        public ConversationBox(string userName, string lastMessage, long timeStamp,
            ConvMessage.State messageState)
            :this(null, userName, lastMessage, timeStamp, false, messageState)
        { 
        
        }

        public ConversationBox(byte[] avatar, string userName, string lastMessage, long timeStamp,
            ConvMessage.State messageState)
            : this(avatar, userName, lastMessage, timeStamp, false, messageState)
        {

        }


        //this property is used for showing ~,S,D,R & dor for unread messages
        public ConvMessage.State MessageStatus
        {
            get
            {
                return _messageStatus;
            }
            set
            {
                if (_messageStatus != value)
                {
                    _messageStatus = value;
                }
            }
        }

        //set this on updation of profile image
        public byte[] Avatar
        {
            get
            {
                return _avatar;
            }
            set
            {
                if (_avatar != value)
                {
                    _avatar = value;
                }
            }
        }

    }
}
