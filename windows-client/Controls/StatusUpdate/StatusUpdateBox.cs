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
        private string _msisdn;

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

        public string Msisdn
        {
            get
            {
                return _msisdn;
            }
            set
            {
                if (value != _msisdn)
                {
                    _msisdn = value;
                }
            }
        }
        
        public StatusUpdateBox(string userName, BitmapImage userImage, string msisdn)
        {
            this.UserName = userName;
            this.UserImage = userImage;
            this.Msisdn = msisdn;
        }

        public StatusUpdateBox()
        {
        }
    }
}
