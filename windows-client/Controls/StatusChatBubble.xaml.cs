using Newtonsoft.Json.Linq;
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
            this.statusTypeImage.Source = MoodsInitialiser.Instance.GetMoodImageForMoodId(GetMoodId(cm.MetaDataString));
            this.MetaDataString = cm.MetaDataString;
            InitialiseColor();
        }

        public StatusChatBubble(ConvMessage cm,BitmapImage img)
        {
            InitializeComponent();
            this.statusMessageTxtBlk.Text = this.Text = AppResources.PicUpdate_StatusTxt;
            this.statusTimestampTxtBlk.Text = this.TimeStamp = TimeUtils.getRelativeTime(cm.Timestamp);
            this.statusTypeImage.Source = img;
            this.MetaDataString = cm.MetaDataString;
            InitialiseColor();
        }
        public void InitialiseColor()
        {
            this.LayoutRoot.Background = UI_Utils.Instance.StatusBubbleColor;
            this.statusMessageTxtBlk.Foreground = UI_Utils.Instance.ReceiveMessageForeground;
            this.statusTimestampTxtBlk.Foreground = UI_Utils.Instance.ReceivedChatBubbleTimestamp;
        }

        private static int GetMoodId(string metadataJson)
        {
            int moodId = -1;
            if(string.IsNullOrWhiteSpace(metadataJson))
                return -1;
            try
            {
                JObject metaData = JObject.Parse(metadataJson);
                JObject data = (JObject)metaData[HikeConstants.DATA];
                if (data[HikeConstants.MOOD] != null)
                {
                    string moodId_String = data[HikeConstants.MOOD].ToString();
                    if (!string.IsNullOrEmpty(moodId_String))
                    {
                        int.TryParse(moodId_String, out moodId);
                    }
                }
            }
            catch (Exception)
            { }
            return moodId;
        }
    }
}