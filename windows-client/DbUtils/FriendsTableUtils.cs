﻿using System;
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
            ALREADY_FRIENDS//added state just to ignore add friends after two way friends
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
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (!store.DirectoryExists(FRIENDS_DIRECTORY))
                        {
                            store.CreateDirectory(FRIENDS_DIRECTORY);
                        }
                        long ts = 0, lsts=0;
                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            if (file.Length > 0)
                            {
                                FriendStatusEnum friendStatusDb = FriendStatusEnum.NOT_SET;
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        friendStatusDb = (FriendStatusEnum)(byte)reader.ReadByte();
                                        //ignore add friend after becoming two way friends
                                        if (friendStatusDb == FriendStatusEnum.FRIENDS && friendStatus == FriendStatusEnum.REQUEST_RECIEVED)
                                            return FriendStatusEnum.ALREADY_FRIENDS;
                                    }
                                    catch (Exception e)
                                    {
                                        Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus : Reading status, Exception : " + e.StackTrace);
                                    }
                                    try
                                    {
                                        ts = reader.ReadInt64();
                                    }
                                    catch (Exception e)
                                    {
                                        Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus : Reading timestamp, Exception : " + e.StackTrace);
                                    }
                                    try
                                    {
                                        lsts = reader.ReadInt64();
                                    }
                                    catch (Exception e)
                                    {
                                        Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus : Reading last Seen timestamp, Exception : " + e.StackTrace);
                                    } 
                                }

                                //not now and remove from friends are totally same state now
                                if ((friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_YOU && friendStatus == FriendStatusEnum.UNFRIENDED_BY_HIM) ||
                                    (friendStatusDb == FriendStatusEnum.UNFRIENDED_BY_HIM && friendStatus == FriendStatusEnum.UNFRIENDED_BY_YOU) ||
                                    (friendStatusDb == FriendStatusEnum.REQUEST_SENT && friendStatus == FriendStatusEnum.UNFRIENDED_BY_YOU) ||
                                    (friendStatusDb == FriendStatusEnum.REQUEST_RECIEVED && friendStatus == FriendStatusEnum.UNFRIENDED_BY_HIM))
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
                                writer.Write(lsts);
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
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus, Exception : " + ex.StackTrace);
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
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
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
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return friendStatus;
        }

        public static FriendStatusEnum GetFriendInfo(string msisdn, out long ts)
        {
            ts = 0;
            long lsts = 0;
            FriendStatusEnum friendStatus = FriendStatusEnum.NOT_SET;
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    friendStatus = (FriendStatusEnum)reader.ReadByte();
                                    try
                                    {
                                        ts = reader.ReadInt64();
                                    }
                                    catch
                                    {
                                        ts = 0;
                                    }
                                    try
                                    {
                                        lsts = reader.ReadInt64();
                                    }
                                    catch
                                    {
                                        lsts = 0;
                                    }
                                    
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
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

        public static long GetFriendLastSeenTSFromFile(string msisdn)
        {
            long lsts = 0;
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    try
                                    {
                                        reader.ReadByte();
                                    }
                                    catch { }
                                    
                                    try
                                    {
                                        reader.ReadInt64();
                                    }
                                    catch { }

                                    lsts = reader.ReadInt64();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
                }
            }
            return lsts;
        }

        public static void SetFriendLastSeenTSToFile(string msisdn, long newLSTimeStamp)
        {
            lock (readWriteLock)
            {
                try
                {
                    long joinTime = 0;
                    FriendStatusEnum fStatus = FriendStatusEnum.NOT_SET;
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (!store.DirectoryExists(FRIENDS_DIRECTORY))
                            store.CreateDirectory(FRIENDS_DIRECTORY);

                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.ReadWrite))
                            {
                                if (file.Length > 0)
                                {
                                    using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                    {
                                        fStatus = (FriendStatusEnum)reader.ReadByte();

                                        try
                                        {
                                            joinTime = reader.ReadInt64();
                                        }
                                        catch { }
                                    }
                                }
                             
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    writer.Seek(0, SeekOrigin.Begin);
                                    writer.Write((byte)fStatus);
                                    writer.Write(joinTime);
                                    writer.Write(newLSTimeStamp);
                                    writer.Flush();
                                    writer.Close();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendTimeStamp :SetFriendTimeStamp, Exception : " + ex.StackTrace);
                }
            }
        }

        public static void UpdateOldFilesWithDefaultLastSeen()
        {
            lock (readWriteLock)
            {
                try
                {
                    long joinTime = 0, lastSeenTS = 0;
                    FriendStatusEnum fStatus = FriendStatusEnum.NOT_SET;

                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(FRIENDS_DIRECTORY))
                            return;

                        var fileNames = store.GetFileNames(FRIENDS_DIRECTORY + "\\*");

                        foreach (var fileName in fileNames)
                        {
                            if (store.FileExists(fileName))
                            {
                                using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.ReadWrite))
                                {
                                    if (file.Length > 0)
                                    {
                                        using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                        {
                                            try
                                            {
                                                fStatus = (FriendStatusEnum)reader.ReadByte();
                                            }
                                            catch
                                            {
                                                fStatus = FriendStatusEnum.NOT_SET;
                                            } 
                                            
                                            try
                                            {
                                                joinTime = reader.ReadInt64();
                                            }
                                            catch
                                            {
                                                joinTime = 0;
                                            }
                                        }
                                    }

                                    using (BinaryWriter writer = new BinaryWriter(file))
                                    {
                                        writer.Seek(0, SeekOrigin.Begin);
                                        writer.Write((byte)fStatus);
                                        writer.Write(joinTime);
                                        writer.Write(lastSeenTS);
                                        writer.Flush();
                                        writer.Close();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: UpdateFriendTimeStamp :UpdateFriendTimeStamp, Exception : " + ex.StackTrace);
                }
            }
        }

        public static long GetFriendOnHIke(string msisdn)
        {
            long ts = 0;
            lock (readWriteLock)
            {
                try
                {
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    FriendStatusEnum friendStatus = (FriendStatusEnum)reader.ReadByte();

                                    try
                                    {
                                        ts = reader.ReadInt64();
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: GetFriendStatus :GetFriendStatus, Exception : " + ex.StackTrace);
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
                    long lsts=0;
                    string fileName = FRIENDS_DIRECTORY + "\\" + msisdn;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                    {
                        if (!store.DirectoryExists(FRIENDS_DIRECTORY))
                        {
                            store.CreateDirectory(FRIENDS_DIRECTORY);
                        }
                        using (var file = store.OpenFile(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                        {
                            if (file.Length > 0)
                            {
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        friendStatusDb = (FriendStatusEnum)(byte)reader.ReadByte();
                                        try
                                        {
                                            reader.ReadInt64();
                                        }
                                        catch { }
                                        try
                                        {
                                            lsts = reader.ReadInt64();
                                        }
                                        catch { }
                                    }
                                    catch (Exception e)
                                    {
                                        Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus : Reading status, Exception : " + e.StackTrace);
                                    }
                                }
                            }


                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write((byte)friendStatusDb);
                                writer.Write(ts);
                                writer.Write(lsts);
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
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: SetFriendStatus, Exception : " + ex.StackTrace);
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
                                    Logging.LogWriter.Instance.WriteToLog(string.Format("Exception while deleting all friendsDb, FileName :{0} ", fileName));
                                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: DeleteAllFriends : Individual Files, Exception : " + ex.StackTrace);
                                }
                            }
                    }
                }
                catch (Exception ex)
                {
                    Logging.LogWriter.Instance.WriteToLog("FriendsTableUtils :: DeleteAllFriends : DeleteAllFriends, Exception : " + ex.StackTrace);
                }
            }
        }
    }
}
