using Microsoft.Phone.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using windows_client.DbUtils;
using windows_client.Languages;
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

        // private constructor to avoid instantiation
        private StatusUpdateHelper()
        {
        }

        public BaseStatusUpdate CreateStatusUpdate(StatusMessage status, bool isShowOnTimeline)
        {
            if (status == null) // TODO : Madhur garg : To handle null where this function is called
                return null;

            string userName;
            BitmapImage userProfileThumbnail = null;

            if (App.MSISDN == status.Msisdn)
            {
                userName = AppResources.Me_Txt;
                userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(HikeConstants.MY_PROFILE_PIC);
            }
            else
            {
                ConversationListObject co = Utils.GetConvlistObj(status.Msisdn);

                if (co != null)
                {
                    userName = co.NameToShow;

                    if (App.ViewModel.ConvMap.ContainsKey(status.Msisdn) && co.Avatar != null)
                        userProfileThumbnail = co.AvatarImage;
                    else
                        userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(status.Msisdn);
                }
                else
                {
                    bool isInAddressBook = false;
                    ContactInfo cn = ContactUtils.GetContactInfo(status.Msisdn, out isInAddressBook);
                    
                    if (cn == null)
                    {
                        cn = new ContactInfo(status.Msisdn, null, true);
                        App.ViewModel.ContactsCache[status.Msisdn] = cn;
                    }

                    userName = (cn != null && !string.IsNullOrWhiteSpace(cn.Name)) ? cn.Name : status.Msisdn;
                    userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(status.Msisdn);
                }
            }

            BaseStatusUpdate statusUpdate = null;

            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.PROFILE_PIC_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.ServerId, out statusImageBytes, out isThumbnail);
                    
                    var img = UI_Utils.Instance.createImageFromBytes(statusImageBytes);
                    if (isThumbnail)
                        userProfileThumbnail = img;

                    statusUpdate = new ImageStatus(userName, userProfileThumbnail, status, isShowOnTimeline, img);
                    break;

                case StatusMessage.StatusType.IS_NOW_FRIEND:
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdate = new TextStatus(userName, userProfileThumbnail, status, isShowOnTimeline);
                    break;
            }

            return statusUpdate;
        }

        public void DeleteMyStatus(BaseStatusUpdate sb)
        {
            AccountUtils.deleteStatus(new AccountUtils.parametrisedPostResponseFunction(deleteStatus_Callback),
                AccountUtils.BASE + "/user/status/" + sb.ServerId, sb);
        }

        private void deleteStatus_Callback(JObject jObj, Object obj)
        {
            if (jObj != null && HikeConstants.OK == (string)jObj[HikeConstants.STAT] && obj != null && obj is BaseStatusUpdate)
            {
                BaseStatusUpdate sb = obj as BaseStatusUpdate;
                StatusMsgsTable.DeleteStatusMsg(sb.ServerId);

                var status = StatusMsgsTable.GetUserLastStatusMsg(sb.Msisdn);
                
                if (status == null)
                    StatusMsgsTable.DeleteLastStatusFile();
                else
                    StatusMsgsTable.SaveLastStatusMessage(status.Message, status.MoodId);
                
                App.HikePubSubInstance.publish(HikePubSub.STATUS_DELETED, sb);
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.StatusDetionFail_Txt, AppResources.Please_Try_Again_Txt, MessageBoxButton.OK);
                });
            }
        }

        public bool IsTwoWayFriend(string msisdn)
        {
            return FriendsTableUtils.GetFriendStatus(msisdn) == FriendsTableUtils.FriendStatusEnum.FRIENDS;
        }
    }
}
