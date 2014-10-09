using CommonLibrary.Constants;
using Newtonsoft.Json.Linq;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using CommonLibrary.DbUtils;
using CommonLibrary.Languages;
using CommonLibrary.Model;

namespace CommonLibrary.utils
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

        public bool IsTwoWayFriend(string msisdn)
        {
            return FriendsTableUtils.GetFriendStatus(msisdn) == FriendsTableUtils.FriendStatusEnum.FRIENDS;
        }
    }
}
