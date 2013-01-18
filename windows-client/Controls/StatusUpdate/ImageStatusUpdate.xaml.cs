using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        public ImageStatusUpdate(string userName, BitmapImage userImage, BitmapImage statusImageBitmap, string locationName, long timestamp)
            : base(userName, userImage, StatusType.IMAGE_UPDATE)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, locationName);
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            this.statusImage.Source = statusImageBitmap;
        }
    }
}
