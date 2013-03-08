using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace windows_client.DbUtils
{
    class FriendsTableUtils
    {
        public enum FriendStatusEnum : byte
        {
            NOT_SET,
            UNFRIENDED_BY_HIM,
            REQUEST_SENT,
            REQUEST_RECIEVED,
            UNFRIENDED_BY_YOU,
            IGNORED,
            FRIENDS
        }

        public static string FRIENDS_DIRECTORY = "FRIENDS";
        private static object readWriteLock = new object();


        public static void SetFriendStatus(string msisdn, FriendStatusEnum friendStatus)
        {
            if (friendStatus > FriendStatusEnum.NOT_SET)
            {
                lock (readWriteLock)
                {
                    try
                    {
                        string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                        {
                            if (!store.DirectoryExists(FRIENDS_DIRECTORY))
                            {
                                store.CreateDirectory(FRIENDS_DIRECTORY);
                            }

                            using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                            {
                                if (file.Length > 0)
                                {
                                    FriendStatusEnum friendStatusDb = (FriendStatusEnum)(byte)file.ReadByte();
                                    if ((friendStatusDb == FriendStatusEnum.REQUEST_SENT && friendStatus == FriendStatusEnum.REQUEST_RECIEVED) ||
                                        (friendStatusDb == FriendStatusEnum.REQUEST_RECIEVED && friendStatus == FriendStatusEnum.REQUEST_SENT) ||
                                        (friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_YOU && friendStatus == FriendStatusEnum.REQUEST_SENT) ||
                                        (friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_HIM && friendStatus == FriendStatusEnum.REQUEST_RECIEVED))
                                    {
                                        friendStatus = FriendStatusEnum.FRIENDS;
                                    }
                                }
                                file.Seek(0, SeekOrigin.Begin);
                                file.WriteByte((byte)friendStatus);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception while settin friend status,{0}", e.StackTrace);
                    }
                }
            }
        }

        public static FriendStatusEnum GetFriendStatus(string msisdn)
        {
            FriendStatusEnum friendStatus = FriendStatusEnum.NOT_SET;
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                    {
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    friendStatus = (FriendStatusEnum)reader.ReadByte();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception while getting friend status,{0}", e.StackTrace);
                }
            }
            return friendStatus;
        }

        public static void DeleteFriend(string msisdn)
        {
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        store.DeleteFile(fileName);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception while deleting friend status,{0}", e.StackTrace);
                }
            }
        }

        public static void DeleteAllFriends()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string[] files = store.GetFileNames(FRIENDS_DIRECTORY + "\\*");
                        if (files != null)
                            foreach (string fileName in files)
                                store.DeleteFile(FRIENDS_DIRECTORY + "\\" + fileName);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception while deleting all friendsDb,{0}", e.StackTrace);
                }
            }
        }
    }
}
