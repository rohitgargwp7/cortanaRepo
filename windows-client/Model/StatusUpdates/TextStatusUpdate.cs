using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.Languages;
using windows_client.utils;

namespace windows_client.Model
{
    public class TextStatus : BaseStatusUpdate
    {
        public TextStatus(string userName, BitmapImage userImage, StatusMessage sm, bool isShowOnTimeline)
            : base(userName, userImage, sm.Msisdn, sm.ServerId, isShowOnTimeline)
        {
            if (sm.Status_Type == StatusMessage.StatusType.IS_NOW_FRIEND)
            {
                string firstName = Utils.GetFirstName(userName);

                if (isShowOnTimeline)
                    Text = string.Format(AppResources.ConfimFriendTimeline_Txt, firstName);
                else
                    Text = string.Format(AppResources.ConfimFriendUserProfile_Txt, firstName);
            }
            else
                Text = sm.Message;

            Timestamp = sm.Timestamp;
            IsUnread = sm.IsUnread;
            IsShowOnTimeline = isShowOnTimeline;

            if(sm.MoodId > 0)
                MoodId = sm.MoodId;
        }

        private int _moodId;
        public int MoodId
        {
            get
            {
                return _moodId;
            }
            set
            {
                if (value != _moodId)
                {
                    _moodId = value;
                    NotifyPropertyChanged("UserImage");
                }
            }
        }

        public override BitmapImage UserImage
        {
            get
            {
                if (MoodId > 0)
                {
                    if (IsShowOnTimeline)
                    {
                        if (MoodsInitialiser.Instance.IsValidMoodId(MoodId))
                            return MoodsInitialiser.Instance.GetMoodImageForMoodId(MoodId);
                        else
                            return base.UserImage;
                    }
                    else
                        return MoodsInitialiser.Instance.GetMoodImageForMoodId(MoodId);
                }
                else if (!IsShowOnTimeline)
                    return UI_Utils.Instance.TextStatusImage;
                else return base.UserImage;
            }
        }

        public override bool IsUnread
        {
            get
            {
                return base.IsUnread;
            }
            set
            {
                if (value != base.IsUnread)
                {
                    base.IsUnread = value;
                    NotifyPropertyChanged("StatusTextForeground");
                }
            }
        }

        public SolidColorBrush StatusTextForeground
        {
            get
            {
                if (IsShowOnTimeline)
                {
                    if (IsUnread != true) //read status
                        return UI_Utils.Instance.StatusTextForeground;
                    else
                        return (SolidColorBrush)App.Current.Resources["HikeBlueHeader"];
                }
                else
                    return UI_Utils.Instance.StatusTextForeground;
            }
        }
    }
}
