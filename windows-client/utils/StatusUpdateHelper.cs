using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

        public StatusUpdateBox createStatusUIObject(StatusMessage status, EventHandler<System.Windows.Input.GestureEventArgs> statusBoxTap,
            EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap,
            EventHandler<System.Windows.Input.GestureEventArgs> enlargePic_Tap)
        {
            string userName;
            BitmapImage userProfileThumbnail;
            if (App.MSISDN == status.Msisdn)
            {
                if (!App.appSettings.TryGetValue(App.ACCOUNT_NAME, out userName))
                    userName = App.MSISDN;
                userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
            }
            else
            {
                ConversationListObject co = Utils.GetConvlistObj(status.Msisdn);
                if (co != null)
                {
                    userName = co.NameToShow;
                    userProfileThumbnail = co.AvatarImage;
                }
                else
                {
                    ContactInfo cn = null;
                    if (App.ViewModel.ContactsCache.ContainsKey(status.Msisdn))
                        cn = App.ViewModel.ContactsCache[status.Msisdn];
                    else
                    {
                        cn = UsersTableUtils.getContactInfoFromMSISDN(status.Msisdn);
                        App.ViewModel.ContactsCache[status.Msisdn] = cn;
                    }
                    userName = cn != null ? cn.Name : status.Msisdn;
                    userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(status.Msisdn);
                }
            }
            StatusUpdateBox statusUpdateBox = null;
            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.PROFILE_PIC_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.StatusId, out statusImageBytes, out isThumbnail);
                    statusUpdateBox = new ImageStatusUpdate(userName, userProfileThumbnail, status.Msisdn, status.StatusId,
                        UI_Utils.Instance.createImageFromBytes(statusImageBytes), status.Timestamp, status.IsRead, statusBubbleImageTap);
                    if (isThumbnail)
                    {
                        object[] statusObjects = new object[2];
                        statusObjects[0] = status;
                        statusObjects[1] = statusUpdateBox;
                        AccountUtils.createGetRequest(AccountUtils.BASE + "/user/status/" + status.Message + "?only_image=true",
                            onStatusImageDownloaded, true, statusObjects);
                    }
                    if (enlargePic_Tap != null)
                        (statusUpdateBox as ImageStatusUpdate).statusImage.Tap += enlargePic_Tap;
                    break;
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new TextStatusUpdate(userName, userProfileThumbnail, status.Msisdn, status.StatusId, status.Message,
                        status.Timestamp, status.IsRead, status.Status_Type, statusBubbleImageTap);
                    break;
            }
            if (statusBoxTap != null)
            {
                statusUpdateBox.Tap += statusBoxTap;
            }
            return statusUpdateBox;
        }

        private void onStatusImageDownloaded(byte[] fileBytes, object status)
        {
            object[] vars = status as object[];
            StatusMessage statusMessage = vars[0] as StatusMessage;
            ImageStatusUpdate statusMessageUI = vars[1] as ImageStatusUpdate;
            if (fileBytes != null && fileBytes.Length > 0)
            {
                //TODO move to background thread
                //                MiscDBUtil.saveStatusImage(statusMessage.Msisdn, statusMessage.StatusId, fileBytes);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    statusMessageUI.StatusImage = UI_Utils.Instance.createImageFromBytes(fileBytes);
                });
            }
        }

        public void deleteMyStatus(long statusId)
        { 
        }

        public void deleteFriendStatus(long statusId)
        { 
        
        }
    }
}
