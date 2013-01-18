using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class LocationStatusUpdate : StatusUpdateBox
    {
        public LocationStatusUpdate(string userName, BitmapImage userImage, string locationName, long timestamp)
            : base(userName, userImage)
        {
            InitializeComponent();
            this.locationTxtBlk .Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, locationName);
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
        }
    }
}
