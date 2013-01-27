using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        public ImageStatusUpdate(string userName, BitmapImage userImage, BitmapImage statusImageBitmap, long timestamp)
            : base(userName, userImage)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (statusImageBitmap != null)
                this.statusImage.Source = statusImageBitmap;
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, BitmapImage statusImageBitmap, string updateText, long timestamp)
            : base(userName, userImage)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = updateText;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            this.statusImage.Source = statusImageBitmap;
        }

        public BitmapImage StatusImage
        {
            set
            {
                this.statusImage.Source = value;
            }
        }

    }
}
