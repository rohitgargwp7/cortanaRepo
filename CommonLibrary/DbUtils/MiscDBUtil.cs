using CommonLibrary.Model;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System;
using CommonLibrary.Misc;
using System.Threading.Tasks;
using CommonLibrary.Constants;
using Newtonsoft.Json.Linq;
using CommonLibrary.Utils;

namespace CommonLibrary.DbUtils
{
    public class MiscDBUtil
    {
        private static object lockObj = new object();
        private static object favReadWriteLock = new object();
        private static object pendingReadWriteLock = new object();
        private static object pendingProfilePicReadWriteLock = new object();
        private static object profilePicLock = new object();
        private static object attachmentLock = new object();

        public static string FAVOURITES_FILE = "favFile";
        public static string MISC_DIR = "Misc_Dir";
        public static string THUMBNAILS = "THUMBNAILS";
        public static string PROFILE_PICS = "PROFILE_PICS";
        public static string STATUS_UPDATE_LARGE = "STATUS_FULL_IMAGE";

        public static string PENDING_REQ_FILE = "pendingReqFile";
        public static string PENDING_PROFILE_PIC_REQ_FILE = "pendingProfilePicReqFile";

        /// <summary>
        /// This function is used to store profile pics (small) so that same can be used in timelines
        /// </summary>
        /// <param name="msisdn"></param>
        /// <param name="imageBytes"></param>
        /// <param name="isUpdated"></param>
        public static void saveProfileImages(string msisdn, byte[] imageBytes, string serverId)
        {
            if (imageBytes == null)
                return;
            serverId = serverId.Replace(":", "_");
            string FileName = PROFILE_PICS + "\\" + msisdn + "\\" + serverId;
            lock (profilePicLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(PROFILE_PICS))
                            store.CreateDirectory(PROFILE_PICS);
                        if (!store.DirectoryExists(PROFILE_PICS + "\\" + msisdn))
                            store.CreateDirectory(PROFILE_PICS + "\\" + msisdn);
                        using (FileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, store))
                        {
                            stream.Write(imageBytes, 0, imageBytes.Length);
                            stream.Flush();
                            stream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: saveProfileImages :saveProfileImages, Exception : " + ex.StackTrace);
                }
            }
        }

        public static void saveAvatarImage(string msisdn, byte[] imageBytes, bool isUpdated)
        {
            if (msisdn == HikeInstantiation.MSISDN)
                msisdn = HikeConstants.MY_PROFILE_PIC;

            if (imageBytes == null)
                return;

            msisdn = msisdn.Replace(":", "_");
            string FileName = THUMBNAILS + "\\" + msisdn;

            lock (lockObj)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (isUpdated && store.FileExists(FileName + FTBasedConstants.FULL_VIEW_IMAGE_PREFIX))
                            store.DeleteFile(FileName + FTBasedConstants.FULL_VIEW_IMAGE_PREFIX);

                        using (FileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, store))
                        {
                            stream.Write(imageBytes, 0, imageBytes.Length);
                            stream.Flush();
                            stream.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: saveAvatarImage : saveAvatarImage, Exception : " + ex.StackTrace);
                }
            }
        }

        public static byte[] getThumbNailForMsisdn(string msisdn)
        {
            if (msisdn == HikeInstantiation.MSISDN)
                msisdn = HikeConstants.MY_PROFILE_PIC;

            msisdn = msisdn.Replace(":", "_");
            byte[] data = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(THUMBNAILS + "\\" + msisdn)) // Check if file exists
                    {
                        using (IsolatedStorageFileStream isfs = store.OpenFile(THUMBNAILS + "\\" + msisdn, FileMode.Open, FileAccess.Read))
                        {
                            data = new byte[isfs.Length];
                            isfs.Read(data, 0, data.Length);
                            isfs.Close();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: getThumbNailForMsisdn : getThumbNailForMsisdn, Exception : " + ex.StackTrace);
                }
            }
            return data;
        }

        public async static void DeleteImageForMsisdn(string msisdn)
        {
            await Task.Delay(1);

            if (msisdn == HikeInstantiation.MSISDN)
                msisdn = HikeConstants.MY_PROFILE_PIC;

            msisdn = msisdn.Replace(":", "_");

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(THUMBNAILS + "\\" + msisdn))
                    store.DeleteFile(THUMBNAILS + "\\" + msisdn);

                if (store.FileExists(THUMBNAILS + "\\" + msisdn + FTBasedConstants.FULL_VIEW_IMAGE_PREFIX))
                    store.DeleteFile(THUMBNAILS + "\\" + msisdn + FTBasedConstants.FULL_VIEW_IMAGE_PREFIX);
            }
        }

        #region FILE TRANSFER UTILS

        public static void saveAttachmentObject(Attachment obj, string msisdn, long messageId)
        {
            lock (attachmentLock)
            {
                try
                {
                    msisdn = msisdn.Replace(":", "_");
                    string fileDirectory = FTBasedConstants.FILES_ATTACHMENT + "/" + msisdn;
                    string fileName = fileDirectory + "/" + messageId;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(fileDirectory))
                        {
                            store.CreateDirectory(fileDirectory);
                        }
                        if (store.FileExists(fileName))
                        {
                            store.DeleteFile(fileName);
                        }
                        using (var file = store.OpenFile(fileName, FileMode.Create, FileAccess.Write))
                        {
                            using (var writer = new BinaryWriter(file))
                            {
                                obj.Write(writer);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: saveAttachmentObject : saveAttachmentObject, Exception : " + ex.StackTrace);
                }
            }
        }

        #endregion

        #region FAVOURITES

        public static void LoadFavouritesFromIndividualFiles(List<ConversationListObject> favList, Dictionary<string, ConversationListObject> _convmap)
        {
            lock (favReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists("FAVS"))
                    {
                        store.CreateDirectory("FAVS");
                        return;
                    }
                    string[] files = store.GetFileNames("FAVS\\*");
                    foreach (string fname in files)
                    {
                        using (var file = store.OpenFile("FAVS\\" + fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                ConversationListObject item = new ConversationListObject();
                                try
                                {
                                    item.ReadFavOrPending(reader);

                                    if (_convmap.ContainsKey(item.Msisdn)) // if this item is in convList, just mark IsFav to true
                                        favList.Add(_convmap[item.Msisdn]);
                                    else
                                        favList.Add(item);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("MiscDbUtil :: LoadFavouritesFromIndividualFiles : reading file, Exception : " + ex.StackTrace);
                                }
                                reader.Close();
                            }
                            try
                            {
                                file.Close();
                                file.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("MiscDbUtil :: LoadFavouritesFromIndividualFiles : disposing file, Exception : " + ex.StackTrace);
                            }
                        }
                    }
                }
            }
        }

        public static void LoadFavourites(List<ConversationListObject> favList, Dictionary<string, ConversationListObject> _convmap)
        {
            lock (favReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(MISC_DIR))
                    {
                        store.CreateDirectory(MISC_DIR);
                        return;
                    }
                    string fname = MISC_DIR + "\\" + FAVOURITES_FILE;
                    if (!store.FileExists(fname))
                        return;
                    using (var file = store.OpenFile(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;
                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("MiscDbUtil :: LoadFavourites : count reading, Exception : " + ex.StackTrace);
                            }
                            if (count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    ConversationListObject item = new ConversationListObject();

                                    try
                                    {
                                        item.ReadFavOrPending(reader);

                                        if (_convmap.ContainsKey(item.Msisdn)) // if this item is in convList, just mark IsFav to true
                                            favList.Add(_convmap[item.Msisdn]);
                                        else
                                            favList.Add(item);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("MiscDbUtil :: LoadFavourites : file reading, Exception : " + ex.StackTrace);
                                    }
                                }
                            }
                            reader.Close();
                        }
                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("MiscDbUtil :: LoadFavourites : disposing file, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        public static void SaveFavourites()
        {
            lock (favReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string fName = MISC_DIR + "\\" + FAVOURITES_FILE;

                    using (var file = store.OpenFile(fName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        if (!store.DirectoryExists(MISC_DIR))
                        {
                            store.CreateDirectory(MISC_DIR);
                        }

                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            writer.Write(HikeInstantiation.ViewModel.FavList.Count);
                            for (int i = 0; i < HikeInstantiation.ViewModel.FavList.Count; i++)
                            {
                                ConversationListObject item = HikeInstantiation.ViewModel.FavList[i];
                                item.WriteFavOrPending(writer);
                            }
                            writer.Flush();
                            writer.Close();
                        }
                        file.Close();
                        file.Dispose();
                    }
                }
            }
        }

        public static void SaveFavourites(ConversationListObject obj) // this is to save individual file
        {
            if (obj == null)
                return;
            lock (favReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists("FAVS"))
                        store.CreateDirectory("FAVS");

                    string fName = "FAVS" + "\\" + obj.Msisdn.Replace(":", "_"); // ttoohis will handle GC 
                    using (var file = store.OpenFile(fName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            obj.WriteFavOrPending(writer);
                            writer.Flush();
                            writer.Close();
                        }
                        file.Close();
                        file.Dispose();
                    }
                }
            }
        }

        public static void DeleteFavourite(string msisdn)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    store.DeleteFile("FAVS\\" + msisdn.Replace(":", "_"));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: DeleteFavourite : DeleteFavourite, Exception : " + ex.StackTrace);
                }
            }
        }

        #endregion

        #region PENDING REQUESTS

        public static void LoadPendingRequests(Dictionary<string, ConversationListObject> _pendingReq)
        {
            lock (pendingReadWriteLock)
            {
                if (HikeInstantiation.ViewModel.IsPendingListLoaded)
                    return;
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(MISC_DIR))
                    {
                        store.CreateDirectory(MISC_DIR);
                        return;
                    }
                    string fname = MISC_DIR + "\\" + PENDING_REQ_FILE;
                    if (!store.FileExists(fname))
                        return;
                    using (var file = store.OpenFile(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;
                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("MiscDbUtil :: LoadPendingRequests : read count, Exception : " + ex.StackTrace);
                            }
                            if (count > 0)
                            {
                                for (int i = 0; i < count; i++)
                                {
                                    ConversationListObject item = new ConversationListObject();
                                    try
                                    {
                                        item.ReadFavOrPending(reader);
                                        if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(item.Msisdn))
                                            _pendingReq[item.Msisdn] = HikeInstantiation.ViewModel.ConvMap[item.Msisdn];
                                        else
                                        {
                                            _pendingReq[item.Msisdn] = item;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("MiscDbUtil :: LoadPendingRequests : read file, Exception : " + ex.StackTrace);
                                    }
                                }
                            }

                            reader.Close();
                        }

                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("MiscDbUtil :: LoadPendingRequests : dispose file, Exception : " + ex.StackTrace);
                        }
                    }
                }

                HikeInstantiation.ViewModel.IsPendingListLoaded = true;
            }
        }

        public static void SavePendingRequests()
        {
            lock (pendingReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(MISC_DIR))
                    {
                        store.CreateDirectory(MISC_DIR);
                    }
                    string fName = MISC_DIR + "\\" + PENDING_REQ_FILE;
                    using (var file = store.OpenFile(fName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            writer.Write(HikeInstantiation.ViewModel.PendingRequests.Count);
                            foreach (string ms in HikeInstantiation.ViewModel.PendingRequests.Keys)
                            {
                                ConversationListObject item = HikeInstantiation.ViewModel.PendingRequests[ms];
                                item.WriteFavOrPending(writer);
                            }
                            writer.Flush();
                            writer.Close();
                        }
                        file.Close();
                        file.Dispose();
                    }
                }
            }
        }

        public static void SavePendingUploadPicRequests()
        {
            lock (pendingProfilePicReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(MISC_DIR))
                        store.CreateDirectory(MISC_DIR);

                    string fName = MISC_DIR + "\\" + PENDING_PROFILE_PIC_REQ_FILE;

                    if (HikeInstantiation.ViewModel.PicUploadList.Count == 0)
                    {
                        store.DeleteFile(fName);
                        return;
                    }

                    using (var file = store.OpenFile(fName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            writer.Write(HikeInstantiation.ViewModel.PicUploadList.Count);

                            foreach (var ms in HikeInstantiation.ViewModel.PicUploadList)
                            {
                                ms.Write(writer);
                            }

                            writer.Flush();
                            writer.Close();
                        }

                        file.Close();
                        file.Dispose();
                    }
                }
            }
        }

        public static async void LoadPendingUploadPicRequests()
        {
            await Task.Delay(1);

            lock (pendingProfilePicReadWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(MISC_DIR))
                        return;

                    string fname = MISC_DIR + "\\" + PENDING_PROFILE_PIC_REQ_FILE;

                    if (!store.FileExists(fname))
                        return;

                    using (var file = store.OpenFile(fname, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;

                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("MiscDbUtil :: LoadPendingUploadPicRequests : read count, Exception : " + ex.StackTrace);
                            }

                            if (count > 0)
                            {
                                GroupPic group = null;
                                for (int i = 0; i < count; i++)
                                {
                                    try
                                    {
                                        group = new GroupPic();
                                        group.Read(reader);
                                        HikeInstantiation.ViewModel.PicUploadList.Add(group);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("MiscDbUtil :: LoadPendingUploadPicRequests : read file, Exception : " + ex.StackTrace);
                                    }
                                }
                            }

                            reader.Close();
                        }

                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("MiscDbUtil :: LoadPendingUploadPicRequests : dispose file, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        #endregion

        #region MESSAGE STATUS CHANGE

        /// <summary>
        /// Mark single msg as Sent Confirmed, Sent Socket Written and Sent Delivered
        /// </summary>
        /// <param name="fromUser"></param>
        /// <param name="msgID"></param>
        /// <param name="status"></param>
        public static void UpdateDBsMessageStatus(string fromUser, long msgID, int status)
        {
            string msisdn = MessagesTableUtils.updateMsgStatus(fromUser, msgID, status);
            ConversationTableUtils.updateLastMsgStatus(msgID, msisdn, status); // update conversationObj, null is already checked in the function
        }

        /// <summary>
        /// Update delivered status of all messages less than msg id 
        /// </summary>
        /// <param name="msisdn"></param>
        /// <param name="msgID"></param>
        /// <param name="status"></param>
        public static IList<long> UpdateBulkMessageDBsDeliveredStatus(string msisdn, long msgID)
        {
            IList<long> listUpdatedMsgIds = MessagesTableUtils.updateBulkMsgDeliveredStatus(msisdn, msgID);
            ConversationTableUtils.updateLastMsgStatus(msgID, msisdn, (int)ConvMessage.State.SENT_DELIVERED); // update conversationObj, null is already checked in the function
            return listUpdatedMsgIds;
        }

        /// <summary>
        /// Update read status of all messages less than msg id 
        /// </summary>
        /// <param name="msisdn"></param>
        /// <param name="msgID"></param>
        /// <param name="status"></param>
        public static IList<long> UpdateBulkMessageDBsReadStatus(string msisdn, long msgID, long lastReadMessageId, JArray readByArray)
        {
            IList<long> listUpdatedMsgIds = MessagesTableUtils.updateBulkMsgReadStatus(msisdn, msgID);
            ConversationTableUtils.updateLastMsgStatus(msgID, msisdn, (int)ConvMessage.State.SENT_DELIVERED_READ); // update conversationObj, null is already checked in the function
            if (Utility.IsGroupConversation(msisdn))
                GroupTableUtils.UpdateReadBy(msisdn, lastReadMessageId, readByArray);
            return listUpdatedMsgIds;
        }

        #endregion
    }
}
