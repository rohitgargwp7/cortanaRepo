using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using windows_client.Model;

namespace windows_client.DbUtils
{
    public class FriendsTableUtils
    {
        public enum FriendStatusEnum : byte
        {
            NOT_SET,
            UNFRIENDED_BY_HIM,
            REQUEST_SENT,
            REQUEST_RECIEVED,
            UNFRIENDED_BY_YOU,
            IGNORED,
            FRIENDS,
            NOT_FRIENDS //  this is just to signify that there is no 2 way friendship
        }

        public static string FRIENDS_DIRECTORY = "FRIENDS";
        private static object readWriteLock = new object();


        public static FriendStatusEnum SetFriendStatus(string msisdn, FriendStatusEnum friendStatus)
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
                        long ts = 0;
                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite,FileShare.ReadWrite))
                        {
                            if (file.Length > 0)
                            {
                                FriendStatusEnum friendStatusDb = FriendStatusEnum.NOT_SET;
                                using (var reader = new BinaryReader(file,Encoding.UTF8,true))
                                {
                                    try
                                    {
                                        friendStatusDb = (FriendStatusEnum)(byte)reader.ReadByte();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("FriendsTableUtils :: SetFriendStatus : Reading status, Exception : " + e.StackTrace);
                                    }
                                    try
                                    {
                                        ts = reader.ReadInt64();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("FriendsTableUtils :: SetFriendStatus : Reading timestamp, Exception : " + e.StackTrace);
                                    }
                                }

                                if (friendStatus == FriendStatusEnum.UNFRIENDED_BY_HIM && friendStatusDb != FriendsTableUtils.FriendStatusEnum.FRIENDS)                                  
                                {
                                    friendStatus = FriendStatusEnum.NOT_SET;
                                }
                                else if (friendStatus == FriendStatusEnum.UNFRIENDED_BY_YOU && friendStatusDb != FriendsTableUtils.FriendStatusEnum.FRIENDS)
                                {
                                    friendStatus = FriendStatusEnum.NOT_SET;
                                }

                                else if ((friendStatusDb == FriendStatusEnum.REQUEST_SENT && friendStatus == FriendStatusEnum.REQUEST_RECIEVED) ||
                                    (friendStatusDb == FriendStatusEnum.REQUEST_RECIEVED && friendStatus == FriendStatusEnum.REQUEST_SENT) ||
                                    (friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_YOU && friendStatus == FriendStatusEnum.REQUEST_SENT) ||
                                    (friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_HIM && friendStatus == FriendStatusEnum.REQUEST_RECIEVED))
                                {
                                    friendStatus = FriendStatusEnum.FRIENDS;
                                }
                            }
                            
                           
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write((byte)friendStatus);
                                writer.Write(ts);
                                writer.Flush();
                                writer.Close();
                            }
                            if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                                App.ViewModel.ContactsCache[msisdn].FriendStatus = friendStatus;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: SetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return friendStatus;
        }

        private static FriendStatusEnum GetFriendStatusFromFile(string msisdn)
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
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return friendStatus;
        }

        public static FriendStatusEnum GetFriendInfo(string msisdn,out long ts)
        {
            ts = 0;
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
                                    ts = reader.ReadInt64();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return friendStatus;
        }

        public static FriendStatusEnum GetFriendStatus(string msisdn)
        {
            if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
            {
                ContactInfo ci = App.ViewModel.ContactsCache[msisdn];
                if (ci.FriendStatus == FriendsTableUtils.FriendStatusEnum.NOT_SET)
                    ci.FriendStatus = FriendsTableUtils.GetFriendStatusFromFile(msisdn);
                return ci.FriendStatus;
            }
            else
                return GetFriendStatusFromFile(msisdn);
        }

        public static long GetFriendOnHIke(string msisdn)
        {
            long ts = 0;
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
                                    FriendStatusEnum friendStatus = (FriendStatusEnum)reader.ReadByte();
                                    ts = reader.ReadInt64();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return ts;
        }

        public static void SetJoiningTime(string msisdn, long ts)
        {
            lock (readWriteLock)
            {
                try
                {
                    FriendStatusEnum friendStatusDb = FriendStatusEnum.NOT_SET;
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
                                using (var reader = new BinaryReader(file))
                                {
                                    try
                                    {
                                        friendStatusDb = (FriendStatusEnum)(byte)reader.ReadByte();
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("FriendsTableUtils :: SetFriendStatus : Reading status, Exception : " + e.StackTrace);
                                    }
                                }
                            }


                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write((byte)friendStatusDb);
                                writer.Write(ts);
                                writer.Flush();
                                writer.Close();
                            }
                            if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                                App.ViewModel.ContactsCache[msisdn].FriendStatus = friendStatusDb;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: SetFriendStatus, Exception : " + ex.StackTrace);
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
                            {
                                try
                                {
                                    store.DeleteFile(FRIENDS_DIRECTORY + "\\" + fileName);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("Exception while deleting all friendsDb, FileName :{0} ", fileName);
                                    Debug.WriteLine("FriendsTableUtils :: DeleteAllFriends : Individual Files, Exception : " + ex.StackTrace);
                                }
                            }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("FriendsTableUtils :: DeleteAllFriends : DeleteAllFriends, Exception : " + ex.StackTrace);
                }
            }
        }
    }
}
