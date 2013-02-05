using windows_client.utils;
using System.Windows.Media.Imaging;
using windows_client.Languages;

namespace windows_client.Controls.StatusUpdate
{
    public partial class ImageStatusUpdate : StatusUpdateBox
    {
        private BitmapImage _statusImageSource;

        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, BitmapImage statusImageBitmap, long timestamp)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = AppResources.StatusUpdate_Photo;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            if (statusImageBitmap != null)
                this.StatusImage = statusImageBitmap;
        }

        public ImageStatusUpdate(string userName, BitmapImage userImage, string msisdn, BitmapImage statusImageBitmap, string updateText, long timestamp)
            : base(userName, userImage, msisdn)
        {
            InitializeComponent();
            this.statusTextTxtBlk.Text = updateText;
            this.timestampTxtBlk.Text = TimeUtils.getRelativeTime(timestamp);
            this.StatusImage = statusImageBitmap;
        }

        public BitmapImage StatusImage
        {
            get
            {
                return _statusImageSource;
            }
            set
            {
                if (value != _statusImageSource)
                {
                    this.statusImage.Source = _statusImageSource = value;
                }   
            }
        }

    }
}
