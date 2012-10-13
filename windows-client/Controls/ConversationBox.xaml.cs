using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.Controls
{
    public partial class ConversationBox : UserControl
    {
        public ConversationBox(BitmapImage profileImage, string userName, string lastMessage, long timeStamp, 
            bool isSentMessage) //set it true is we have to show SDR, set false for event notification messages
        {
            this.profileImage.Source = profileImage;
            this.userName.Text = userName;
            this.lastMessage.Text = lastMessage;
            this.timestamp.Text = TimeUtils.getTimeString(timeStamp);
        }
    }
}
