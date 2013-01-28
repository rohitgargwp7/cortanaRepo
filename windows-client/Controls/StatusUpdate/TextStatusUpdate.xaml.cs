using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class TextStatusUpdate : StatusUpdateBox
    {
        public TextStatusUpdate(string userName, BitmapImage userImage, string msisdn, string textOrLocationName, long timestamp)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            this.statusTextTxtBlk.Text = string.Format(AppResources.StatusUpdate_YouCanNowSeeUpdates_TxtBlk, textOrLocationName);
        }
    }
}
