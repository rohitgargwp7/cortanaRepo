using System;
using System.Windows.Media;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class StatusChatBubble : MyChatBubble 
    {
        public StatusChatBubble(ConvMessage cm)
        {
            this.statusMessageTxtBlk.Text = this.Text = cm.Message;
            this.statusTimestampTxtBlk.Text = this.TimeStamp = TimeUtils.getRelativeTime(cm.Timestamp);
        }
    }
}