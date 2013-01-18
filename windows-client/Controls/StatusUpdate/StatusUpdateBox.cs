using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace windows_client.Controls.StatusUpdate
{
    public class StatusUpdateBox : UserControl
    {
        private string _userName;
        private BitmapImage _userImage;

        public string UserName
        {
            get 
            {
                return _userName;
            }
            set 
            {
                if (value != _userName)
                {
                    _userName = value;
                }
            }
        }

        public BitmapImage UserImage
        {
            get
            {
                return _userImage;
            }
            set
            {
                if (value != _userImage)
                {
                    _userImage = value;
                }
            }
        }

        public StatusUpdateBox(string userName, BitmapImage userImage)
        {
            this.UserName = userName;
            this.UserImage = userImage;
        }

        public StatusUpdateBox()
        {
        }

    }
}
