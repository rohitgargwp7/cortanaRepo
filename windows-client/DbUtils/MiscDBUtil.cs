using windows_client.Model;
using System.Linq;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System;
using windows_client.Misc;
using System.Collections.ObjectModel;
using windows_client.ViewModel;
using windows_client.utils;

namespace windows_client.DbUtils
{
    public class MiscDBUtil
    {
        private static object lockObj = new object();
        private static object favReadWriteLock = new object();
        private static object pendingReadWriteLock = new object();
        private static object profilePicLock = new object();
        private static object statusImageLock = new object();

        public static string FAVOURITES_FILE = "favFile";
        public static string MISC_DIR = "Misc_Dir";
        public static string THUMBNAILS = "THUMBNAILS";
        public static string PROFILE_PICS = "PROFILE_PICS";
        public static string STATUS_UPDATE_LARGE = "STATUS_FULL_IMAGE";

        public static string PENDING_REQ_FILE = "pendingReqFile";

        public static void clearDatabase()
        {
            #region DELETE CONVS,CHAT MSGS, GROUPS, GROUP MEMBERS,THUMBNAILS,SAVED PIC UPDATES, STATUS MSGS , LAST STATUS

            ConversationTableUtils.deleteAllConversations();
            DeleteAllThumbnails();
            DeleteAllAttachmentData();
            DeleteAllPicUpdates();
            DeleteAllLargeStatusImages();
            UI_Utils.Instance.BitmapImageCache.Clear();
            StatusMsgsTable.DeleteAllStatusMsgs();
            StatusMsgsTable.DeleteLastStatusFile();
            StatusMsgsTable.DeleteUnreadCountFile();
            GroupManager.Instance.DeleteAllGroups();
            FriendsTableUtils.DeleteAllFriends();
            MessagesTableUtils.DeleteAllLongMessages();
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.groupInfo.DeleteAllOnSubmit<GroupInfo>(context.GetTable<GroupInfo>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }
                catch (ChangeConflictException ex)
                {
                    Debug.WriteLine("MiscDbUtil :: clearDatabase : submitChangesChat, Exception : " + ex.StackTrace);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges); // second client changes will be submitted.
                    }
                }

                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);

            }
            #endregion
            #region DELETE USERS, BLOCKLIST

            App.ViewModel.BlockedHashset.Clear();
            App.ViewModel.ContactsCache.Clear();

            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }

                catch (ChangeConflictException ex)
                {
                    Debug.WriteLine("MiscDbUtil :: clearDatabase : submitChangesUSers , blocklists, Exception : " + ex.StackTrace);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges); // second client changes will be submitted.
                    }
                }

                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);

            }
            #endregion
            #region DELETE MQTTPERSISTED MESSAGES
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
            {
                context.mqttMessages.DeleteAllOnSubmit<HikePacket>(context.GetTable<HikePacket>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }

                catch (ChangeConflictException ex)
                {
                    Debug.WriteLine("MiscDbUtil :: clearDatabase :  DELETE MQTTPERSISTED MESSAGES , Exception : " + ex.StackTrace);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges); // second client changes will be submitted.
                    }
                }

                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            #endregion
            #region DELETE FAVOURITES AND PENDING REQUESTS AND PROTIPS 
            DeleteFavourites();
            DeletePendingRequests();
            ProTipHelper.Instance.ClearProTips();
            App.WriteToIsoStorageSettings(App.PRO_TIP_COUNT, 2); // reset value of protip count for next new user
            #endregion
            #region DELETE CATEGORIES
            StickerCategory.DeleteAllCategories();
            #endregion
        }

        #region STATUS UPDATES

        public static void saveStatusImage(string msisdn, string serverId, byte[] imageBytes)
        {
            msisdn = msisdn.Replace(":", "_");
            serverId = serverId.Replace(":", "_");
            string fullFilePath = STATUS_UPDATE_LARGE + "/" + msisdn + "/" + serverId;
            storeFileInIsolatedStorage(fullFilePath, imageBytes);
        }

        public static byte[] GetProfilePicUpdateForID(string msisdn, string serverId)
        {
            serverId = serverId.Replace(":", "_");
            string filePath = PROFILE_PICS + "/" + msisdn + "/" + serverId;
            byte[] data = null;
            lock (profilePicLock)
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (myIsolatedStorage.FileExists(filePath))
                    {
                        using (IsolatedStorageFileStream stream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                        {
                            data = new byte[stream.Length];
                            stream.Read(data, 0, data.Length);
                            stream.Close();
                        }
                    }
                }
                return data;
            }
        }

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
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
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

        public static void getStatusUpdateImage(string msisdn, string serverId, out byte[] imageBytes, out bool isThumbnail)
        {
            lock (statusImageLock)
            {
                isThumbnail = false;
                msisdn = msisdn.Replace(":", "_");
                serverId = serverId.Replace(":", "_");
                string fullFilePath = STATUS_UPDATE_LARGE + "/" + msisdn + "/" + serverId;
                readFileFromIsolatedStorage(fullFilePath, out imageBytes);
                if (imageBytes == null || imageBytes.Length == 0)
                {
                    isThumbnail = true;
                    string thumbnailFilePath = PROFILE_PICS + "/" + msisdn + "/" + serverId;
                    readFileFromIsolatedStorage(thumbnailFilePath, out imageBytes);
                }
            }
        }

        public static void DeleteAllLargeStatusImages()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!store.DirectoryExists(STATUS_UPDATE_LARGE))
                    return;
                string[] dirs = store.GetFileNames(STATUS_UPDATE_LARGE + "\\*");
                foreach (string dir in dirs)
                {
                    string[] files = store.GetFileNames(STATUS_UPDATE_LARGE + "\\" + dir + "\\*");

                    foreach (string file in files)
                    {
                        try
                        {
                            store.DeleteFile(STATUS_UPDATE_LARGE + "\\" + dir + "\\" + file);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("File {0} does not exist.", STATUS_UPDATE_LARGE + "\\" + dir + "\\" + file);
                            Debug.WriteLine("MiscDbUtil :: DeleteAllLargeStatusImages : DeleteAllLargeStatusImages, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        public static void DeleteAllPicUpdates()
        {
            lock (profilePicLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(PROFILE_PICS))
                        return;
                    string[] dirs = store.GetFileNames(PROFILE_PICS + "\\*");
                    foreach (string dir in dirs)
                    {
                        string[] files = store.GetFileNames(PROFILE_PICS + "\\" + dir + "\\*");

                        foreach (string file in files)
                        {
                            try
                            {
                                store.DeleteFile(PROFILE_PICS + "\\" + dir + "\\" + file);
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("File {0} does not exist.", PROFILE_PICS + "\\" + dir + "\\" + file);
                                Debug.WriteLine("MiscDbUtil :: DeleteAllPicUpdates : DeleteAllPicUpdates, Exception : " + ex.StackTrace);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        public static void saveAvatarImage(string msisdn, byte[] imageBytes, bool isUpdated)
        {
                if (imageBytes == null)
                    return;
                msisdn = msisdn.Replace(":", "_");
                string FileName = THUMBNAILS + "\\" + msisdn;
                lock (lockObj)
                {
                    try
                    {
                        using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                        {
                            if (isUpdated && store.FileExists(FileName + HikeConstants.FULL_VIEW_IMAGE_PREFIX))
                            {
                                store.DeleteFile(FileName + HikeConstants.FULL_VIEW_IMAGE_PREFIX);
                            }
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

        public static bool hasCustomProfileImage(string msisdn)
        {
            msisdn = msisdn.Replace(":", "_");
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(THUMBNAILS + "\\" + msisdn)) // Check if file exists
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: hasCustomProfileImage : hasCustomProfileImage, Exception : " + ex.StackTrace);
                }
            }
            return false;
        }

        public static byte[] getThumbNailForMsisdn(string msisdn)
        {
            if (msisdn == App.MSISDN)
            {
                msisdn = HikeConstants.MY_PROFILE_PIC;
            }
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

        public static void DeleteAllThumbnails()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(THUMBNAILS + "\\*");
                foreach (string fileName in files)
                {
                    try
                    {
                        store.DeleteFile(THUMBNAILS + "\\" + fileName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("File {0} does not exist.", THUMBNAILS + "\\" + fileName);
                        Debug.WriteLine("MiscDbUtil :: DeleteAllThumbnails : DeleteAllThumbnails, Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        #region FILE TRANSFER UTILS

        public static void saveAttachmentObject(Attachment obj, string msisdn, long messageId)
        {
            msisdn = msisdn.Replace(":", "_");
            string fileDirectory = HikeConstants.FILES_ATTACHMENT + "/" + msisdn;
            string fileName = fileDirectory + "/" + messageId;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
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

        public static Dictionary<long, Attachment> getAllFileAttachment(string msisdn)
        {
            if (msisdn == null) // this is imp as explicit handling of null is required to check exception
                return null;
            msisdn = msisdn.Replace(":", "_");
            string fileDirectory = HikeConstants.FILES_ATTACHMENT + "/" + msisdn;
            Dictionary<long, Attachment> msgIdAttachmentMap = new Dictionary<long, Attachment>();
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.DirectoryExists(fileDirectory))
                {
                    string[] msgIds = store.GetFileNames(fileDirectory + "/*");
                    foreach (string msgId in msgIds)
                    {
                        using (var file = store.OpenFile(fileDirectory + "/" + msgId, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                Attachment attachment = new Attachment();
                                attachment.Read(reader);
                                long messageId = Int64.Parse(msgId);
                                if (attachment.FileState == Attachment.AttachmentState.FAILED_OR_NOT_STARTED && MessagesTableUtils.isUploadingOrDownloadingMessage(messageId))
                                {
                                    attachment.FileState = Attachment.AttachmentState.STARTED;
                                }
                                msgIdAttachmentMap.Add(Int64.Parse(msgId), attachment);
                            }
                        }
                    }
                }
                return msgIdAttachmentMap;
            }
        }

        public static void readFileFromIsolatedStorage(string filePath, out byte[] imageBytes)
        {
            filePath = filePath.Replace(":", "_");
            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (myIsolatedStorage.FileExists(filePath))
                {
                    using (IsolatedStorageFileStream fileStream = myIsolatedStorage.OpenFile(filePath, FileMode.Open, FileAccess.Read))
                    {
                        imageBytes = new byte[fileStream.Length];
                        // Read the entire file and then close it
                        fileStream.Read(imageBytes, 0, imageBytes.Length);
                        fileStream.Close();
                    }
                }
                else
                {
                    imageBytes = null;
                }
            }
        }

        public static void storeFileInIsolatedStorage(string filePath, byte[] imagebytes)
        {
            filePath = filePath.Replace(":", "_");
            string fileDirectory = filePath.Substring(0, filePath.LastIndexOf("/"));
            if (imagebytes != null)
            {
                using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!myIsolatedStorage.DirectoryExists(fileDirectory))
                    {
                        myIsolatedStorage.CreateDirectory(fileDirectory);
                    }

                    if (myIsolatedStorage.FileExists(filePath))
                    {
                        myIsolatedStorage.DeleteFile(filePath);
                    }

                    using (IsolatedStorageFileStream fileStream = new IsolatedStorageFileStream(filePath, FileMode.Create, myIsolatedStorage))
                    {
                        using (BinaryWriter writer = new BinaryWriter(fileStream))
                        {
                            writer.Write(imagebytes, 0, imagebytes.Length);
                        }
                    }
                }
            }
        }

        public static void copyFileInIsolatedStorage(string sourceFilePath, string destinationFilePath)
        {
            sourceFilePath = sourceFilePath.Replace(":", "_");
            destinationFilePath = destinationFilePath.Replace(":", "_");
            string sourceFileDirectory = sourceFilePath.Substring(0, sourceFilePath.LastIndexOf("/"));
            string destinationFileDirectory = destinationFilePath.Substring(0, destinationFilePath.LastIndexOf("/"));

            using (IsolatedStorageFile myIsolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!myIsolatedStorage.DirectoryExists(sourceFileDirectory))
                {
                    return;
                }
                if (!myIsolatedStorage.DirectoryExists(destinationFileDirectory))
                {
                    myIsolatedStorage.CreateDirectory(destinationFileDirectory);
                }
                myIsolatedStorage.CopyFile(sourceFilePath, destinationFilePath);
            }
        }

        public static void deleteMessageData(string msisdn, long messageId)
        {
            msisdn = msisdn.Replace(":", "_");
            string attachmentObjectPath = HikeConstants.FILES_ATTACHMENT + "/" + msisdn + "/" + Convert.ToString(messageId);
            string attachmentFileBytes = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messageId);
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(attachmentObjectPath))
                    store.DeleteFile(attachmentObjectPath);
                if (store.FileExists(attachmentFileBytes))
                    store.DeleteFile(attachmentFileBytes);
            }
        }

        public static void deleteMsisdnData(string msisdn)
        {
            msisdn = msisdn.Replace(":", "_");
            string[] attachmentPaths = new string[2];
            attachmentPaths[0] = HikeConstants.FILES_ATTACHMENT + "/" + msisdn;
            attachmentPaths[1] = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (string attachmentPath in attachmentPaths)
                {
                    if (store.DirectoryExists(attachmentPath))
                    {
                        string[] fileNames = store.GetFileNames(attachmentPath + "/*");
                        foreach (string fileName in fileNames)
                        {
                            store.DeleteFile(attachmentPath + "/" + fileName);
                        }
                    }
                }
            }
        }

        public static void DeleteAllAttachmentData()
        {
            string[] attachmentPaths = new string[2];
            attachmentPaths[0] = HikeConstants.FILES_ATTACHMENT;
            attachmentPaths[1] = HikeConstants.FILES_BYTE_LOCATION;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (string attachmentPath in attachmentPaths)
                {
                    if (store.DirectoryExists(attachmentPath))
                    {
                        string[] directoryNames = store.GetDirectoryNames(attachmentPath + "/*");
                        foreach (string directoryName in directoryNames)
                        {
                            string escapedDirectoryName = directoryName.Replace(":", "_");
                            string[] fileNames = store.GetFileNames(attachmentPath + "/" + escapedDirectoryName + "/*");
                            foreach (string fileName in fileNames)
                            {
                                store.DeleteFile(attachmentPath + "/" + escapedDirectoryName + "/" + fileName);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region FAVOURITES

        public static void LoadFavouritesFromIndividualFiles(ObservableCollection<ConversationListObject> favList, Dictionary<string, ConversationListObject> _convmap)
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
                                    {
                                        favList.Add(_convmap[item.Msisdn]);
                                        _convmap[item.Msisdn].IsFav = true;
                                    }
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

        public static void LoadFavourites(ObservableCollection<ConversationListObject> favList, Dictionary<string, ConversationListObject> _convmap)
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
                                        {
                                            favList.Add(_convmap[item.Msisdn]);
                                            _convmap[item.Msisdn].IsFav = true;
                                        }
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
                            writer.Write(App.ViewModel.FavList.Count);
                            for (int i = 0; i < App.ViewModel.FavList.Count; i++)
                            {
                                ConversationListObject item = App.ViewModel.FavList[i];
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

        public static void DeleteFavourites()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    store.DeleteFile(MISC_DIR + "\\" + FAVOURITES_FILE);
                    string[] fileName = store.GetFileNames("FAVS\\*");
                    foreach (string file in fileName)
                    {
                        store.DeleteFile("FAVS\\" + file);
                    }
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, 0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: DeleteFavourites :DeleteFavourites Exception : " + ex.StackTrace);
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
                if (App.ViewModel.IsPendingListLoaded)
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
                                        if (App.ViewModel.ConvMap.ContainsKey(item.Msisdn))
                                            _pendingReq[item.Msisdn] = App.ViewModel.ConvMap[item.Msisdn];
                                        else
                                        {
                                            item.Avatar = MiscDBUtil.getThumbNailForMsisdn(item.Msisdn);
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
                App.ViewModel.IsPendingListLoaded = true;
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
                            writer.Write(App.ViewModel.PendingRequests.Count);
                            foreach (string ms in App.ViewModel.PendingRequests.Keys)
                            {
                                ConversationListObject item = App.ViewModel.PendingRequests[ms];
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

        public static void DeletePendingRequests()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    store.DeleteFile(MISC_DIR + "\\" + PENDING_REQ_FILE);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("MiscDbUtil :: DeletePendingRequests : DeletePendingRequests, Exception : " + ex.StackTrace);
                }
            }
        }

        #endregion
    }
}
