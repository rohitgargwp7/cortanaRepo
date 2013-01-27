using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using windows_client.Controls.StatusUpdate;
using windows_client.DbUtils;
using windows_client.Model;

namespace windows_client.utils
{
    class StatusUpdateHelper
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static volatile StatusUpdateHelper instance = null;

        public static StatusUpdateHelper Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new StatusUpdateHelper();
                        }
                    }
                }
                return instance;
            }
        }

        public StatusUpdateBox createStatusUIObject(StatusMessage status, EventHandler<GestureEventArgs> yesTap,
            EventHandler<GestureEventArgs> noTap)
        {
            BitmapImage userProfileThumbnail = UI_Utils.Instance.getUserProfileThumbnail(status.Msisdn);
            string userName = "Madhur";//need to extract name for msisdn, try to use some cache instead querying db
            StatusUpdateBox statusUpdateBox = null;
            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.ADD_FRIEND:
                    statusUpdateBox = new FriendRequestStatus(userName, userProfileThumbnail, yesTap, noTap);
                    break;
                case StatusMessage.StatusType.PHOTO_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.MessageId, out statusImageBytes, out isThumbnail);
                    statusUpdateBox = new ImageStatusUpdate(userName, userProfileThumbnail, 
                        UI_Utils.Instance.createImageFromBytes(statusImageBytes), status.Timestamp);

                    break;
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new LocationOrText(userName, userProfileThumbnail, status.Message, status.Timestamp);
                    break;
            }
            return statusUpdateBox;
        }

        
    }
}
