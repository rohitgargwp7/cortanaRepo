using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using windows_client.DbUtils;
using windows_client.Model;

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

        public static StatusUpdateBox statusUpdateBoxFactory(StatusMessage status, BitmapImage userImage, string userName, 
            long timestamp)
        {
            StatusUpdateBox statusUpdateBox = null;
            switch (status.Status_Type)
            { 
                case StatusMessage.StatusType.ADD_FRIEND:
                    statusUpdateBox = new FriendRequestStatus(userName, userImage, null, null);
                    break;
                case StatusMessage.StatusType.PHOTO_UPDATE:
                    statusUpdateBox = new ImageStatusUpdate(userName, userImage, null, timestamp);
                    break;
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new TextStatusUpdate(userName, userImage, status.Message, timestamp);

                    break;
            
            }
            return statusUpdateBox;
        }
             
    }
}
