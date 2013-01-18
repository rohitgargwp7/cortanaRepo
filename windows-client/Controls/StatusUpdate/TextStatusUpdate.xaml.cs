using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using windows_client.utils;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        public TextStatusUpdate(string userName, BitmapImage userImage, string locationName, long timestamp)
            : base(userName, userImage)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, locationName);
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
