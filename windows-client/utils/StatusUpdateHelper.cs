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
using windows_client.Controls.StatusUpdate;
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

        public StatusUpdateBox createStatusUIObject(StatusMessage status, bool isShowOnTimeline,
            EventHandler<System.Windows.Input.GestureEventArgs> statusBoxTap,
            EventHandler<System.Windows.Input.GestureEventArgs> statusBubbleImageTap,
            EventHandler<System.Windows.Input.GestureEventArgs> enlargePic_Tap)
        {
            if (status == null) // TODO : Madhur garg : To handle null where this function is called
                return null;

            string userName;
            BitmapImage userProfileThumbnail;

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
                        
                        if (cn == null)
                            cn = new ContactInfo(status.Msisdn, null, true);
                        
                        cn.FriendStatus = FriendsTableUtils.FriendStatusEnum.FRIENDS;
                        App.ViewModel.ContactsCache[status.Msisdn] = cn;
                    }

                    userName = (cn != null && string.IsNullOrWhiteSpace(cn.Name)) ? cn.Name : status.Msisdn;
                    userProfileThumbnail = UI_Utils.Instance.GetBitmapImage(status.Msisdn);
                }
            }

            StatusUpdateBox statusUpdateBox = null;
            
            switch (status.Status_Type)
            {
                case StatusMessage.StatusType.PROFILE_PIC_UPDATE:
                    byte[] statusImageBytes = null;
                    bool isThumbnail;
                    MiscDBUtil.getStatusUpdateImage(status.Msisdn, status.ServerId, out statusImageBytes, out isThumbnail);
                    statusUpdateBox = new ImageStatusUpdate(userName, userProfileThumbnail, status, isShowOnTimeline,
                        UI_Utils.Instance.createImageFromBytes(statusImageBytes), statusBubbleImageTap);
                    if (enlargePic_Tap != null)
                        (statusUpdateBox as ImageStatusUpdate).statusImage.Tap += enlargePic_Tap;
                    break;
            
                case StatusMessage.StatusType.IS_NOW_FRIEND:
                case StatusMessage.StatusType.TEXT_UPDATE:
                    statusUpdateBox = new TextStatusUpdate(userName, userProfileThumbnail, status, isShowOnTimeline, statusBubbleImageTap);
                    break;
            }

            if (statusBoxTap != null)
                statusUpdateBox.Tap += statusBoxTap;
            
            return statusUpdateBox;
        }

        public void deleteMyStatus(StatusUpdateBox sb)
        {
            AccountUtils.deleteStatus(new AccountUtils.parametrisedPostResponseFunction(deleteStatus_Callback),
                AccountUtils.BASE + "/user/status/" + sb.serverId, sb);
        }

        private void deleteStatus_Callback(JObject jObj, Object obj)
        {
            if (jObj != null && HikeConstants.OK == (string)jObj[HikeConstants.STAT] && obj != null && obj is StatusUpdateBox)
            {
                StatusUpdateBox sb = obj as StatusUpdateBox;
                StatusMsgsTable.DeleteStatusMsg(sb.serverId);
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

        public void postStatus_Callback(JObject obj)
        {
            string stat = "";
            if (obj != null)
            {
                JToken statusToken;
                obj.TryGetValue(HikeConstants.STAT, out statusToken);
                stat = statusToken.ToString();
            }
            if (stat == HikeConstants.OK)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    JToken statusData;
                    JObject moodData;
                    obj.TryGetValue(HikeConstants.Extras.DATA, out statusData);
                    try
                    {
                        moodData = statusData.ToObject<JObject>();
                        string statusId = statusData["statusid"].ToString();
                        string message = statusData["msg"].ToString();

                        int moodId = -1;
                        int tod = 0;
                        if (statusData[HikeConstants.MOOD] != null)
                        {
                            string moodId_String = statusData[HikeConstants.MOOD].ToString();
                            if (!string.IsNullOrEmpty(moodId_String))
                            {
                                int.TryParse(moodId_String, out moodId);
                                if (moodId > 0)
                                    tod = statusData[HikeConstants.TIME_OF_DAY].ToObject<int>();
                            }
                        }
                        // status should be in read state when posted yourself
                        StatusMessage sm = new StatusMessage(App.MSISDN, message, StatusMessage.StatusType.TEXT_UPDATE, statusId,
                            TimeUtils.getCurrentTimeStamp(), true, -1, moodId, tod, true);
                        StatusMsgsTable.InsertStatusMsg(sm, false);
                        App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
                    }
                    catch (Exception ex)
                    {
                        //                        Debug.WriteLine("PostStatus:: postStatus_Callback, Exception : " + ex.StackTrace);
                    }
                });
            }
            else
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(AppResources.Please_Try_Again_Txt, "Status Not Posted", MessageBoxButton.OK);
                    //                    postStatusIcon.IsEnabled = true;
                });
            }
        }

    }
}
