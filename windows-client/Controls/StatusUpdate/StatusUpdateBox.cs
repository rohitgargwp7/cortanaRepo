using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace windows_client.Controls.StatusUpdate
{
    public class StatusUpdateBox : UserControl
    {
        private string _userName;
        private BitmapImage _userImage;
        public StatusType _updateType;


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

        public StatusType UpdateType
        {
            get
            {
                return _updateType;
            }
            set
            {
                if (value != _updateType)
                {
                    _updateType = value;
                }
            }
        }


        public enum StatusType
        { 
            TEXT_UPDATE,
            IMAGE_UPDATE,
            LOCATION_UPDATE,
            FRIEND_REQUEST
        }


        public StatusUpdateBox(string userName, BitmapImage userImage, StatusType updateType)
        {
            this.UserName = userName;
            this.UserImage = userImage;
            this.UpdateType = UpdateType;
        }

        public StatusUpdateBox()
        {
        }

    }
}
