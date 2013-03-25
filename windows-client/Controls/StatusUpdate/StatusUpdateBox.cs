using Microsoft.Phone.Controls;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using windows_client.DbUtils;
using windows_client.Languages;
using windows_client.Model;
using windows_client.utils;

namespace windows_client.Controls.StatusUpdate
{
    public class StatusUpdateBox : UserControl
    {
        private string _userName;
        private BitmapImage _userImage;
        private string _msisdn;
        private bool _isUnread;
        private string _serverId;

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

        public string serverId
        {
            get
            {
                return _serverId;
            }
        }

        public virtual bool IsUnread
        {
            get
            {
                return _isUnread;
            }
            set
            {
                if (value != _isUnread)
                {
                    _isUnread = value;
                }
            }
        }

        public StatusUpdateBox(string userName, BitmapImage userImage, string msisdn, string serverId)
        {
            this.UserName = userName;
            this.UserImage = userImage;
            this.Msisdn = msisdn;
            this._serverId = serverId;
            if (App.MSISDN == msisdn)
            {
                ContextMenu menu = new ContextMenu();
                menu.IsZoomEnabled = true;
                MenuItem menuItemDelete = new MenuItem();
                menuItemDelete.Header = AppResources.Delete_Txt;
                menuItemDelete.Tap += delete_Tap;
                menu.Items.Add(menuItemDelete);
                ContextMenuService.SetContextMenu(this, menu);
            }
        }

        private void delete_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StatusUpdateHelper.Instance.deleteMyStatus(this);
        }

        public StatusUpdateBox(ConversationListObject c, string serverId)
            : this(c.NameToShow, c.AvatarImage, c.Msisdn, serverId)
        {
        }

        public StatusUpdateBox()
        {
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            StatusUpdateBox otherSb = (StatusUpdateBox)obj;
            if (this._serverId == null || otherSb._serverId == null)
                return false;
            return this._serverId.Equals(otherSb._serverId);
        }
    }
}
