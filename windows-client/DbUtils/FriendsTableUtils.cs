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
            NotSet,
            RequestSent,
            RequestRecieved,
            UnfriendedAfterFriend,
            Ignored,
            Friends
        }

        public static string FRIENDS_DIRECTORY = "FRIENDS";
        private static object readWriteLock = new object();


        public static void addFriendStatus(string msisdn, FriendStatusEnum friendStatus)
        {
            if (friendStatus > FriendStatusEnum.NotSet)
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
                            if (store.FileExists(fileName))
                            {
                                FriendStatusEnum friendStatusDb = FriendStatusEnum.NotSet;
                                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                                {
                                    using (var reader = new BinaryReader(file))
                                    {
                                        friendStatusDb = (FriendStatusEnum)reader.ReadByte();
                                    }
                                }
                                if ((friendStatusDb == FriendStatusEnum.RequestSent && friendStatus == FriendStatusEnum.RequestRecieved) ||
                                    (friendStatusDb == FriendStatusEnum.RequestRecieved && friendStatus == FriendStatusEnum.RequestSent))
                                {
                                    friendStatus = FriendStatusEnum.Friends;
                                }
                                store.DeleteFile(fileName);
                                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                                {
                                    using (var writer = new BinaryWriter(file))
                                    {
                                        writer.Seek(0, SeekOrigin.Begin);
                                        writer.Write((byte)friendStatus);
                                    }
                                }
                            }
                            else
                            {
                                using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                                {
                                    using (var writer = new BinaryWriter(file))
                                    {
                                        writer.Seek(0, SeekOrigin.Begin);
                                        writer.Write((byte)friendStatus);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);
                    }
                }
            }
        }
        public static FriendStatusEnum GetFriendStatus(string msisdn)
        {
            FriendStatusEnum friendStatus = FriendStatusEnum.NotSet;
            lock (readWriteLock)
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
            return friendStatus;
        }

        public static void deleteFriend(string msisdn)
        {
            lock (readWriteLock)
            {
                string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteFile(fileName);
                }
            }
        }

        public static void deleteAllFriends()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    store.DeleteDirectory(FRIENDS_DIRECTORY);
                }
            }
        }
    }
}
