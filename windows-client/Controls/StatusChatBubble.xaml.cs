using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class StatusChatBubble : MyChatBubble 
    {
        public StatusChatBubble(ConvMessage cm)
        {
            InitializeComponent();
            this.statusMessageTxtBlk.Text = this.Text = cm.Message;
            this.statusTimestampTxtBlk.Text = this.TimeStamp = TimeUtils.getRelativeTime(cm.Timestamp);
            this.statusTypeImage.Source = UI_Utils.Instance.TextStatusImage;
        }

        public StatusChatBubble(ConvMessage cm,BitmapImage img)
        {
            InitializeComponent();
            this.statusMessageTxtBlk.Text = this.Text = AppResources.PicUpdate_StatusTxt;
            this.statusTimestampTxtBlk.Text = this.TimeStamp = TimeUtils.getRelativeTime(cm.Timestamp);
            this.statusTypeImage.Source = img;
        }
    }
}