using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class LocationOrText : StatusUpdateBox
    {
        public LocationOrText(string userName, BitmapImage userImage, StatusType updateType, string textOrLocationName, long timestamp)
            : base(userName, userImage, updateType)
        {
            InitializeComponent();
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (updateType == StatusType.TEXT_UPDATE)
            {
                this.statusTextTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, textOrLocationName);
            }
            else // for location
            {
                this.statusTextTxtBlk.Text = string.Format(AppResources.StatuUpdate_Location, textOrLocationName);
            }
        }
    }
}
