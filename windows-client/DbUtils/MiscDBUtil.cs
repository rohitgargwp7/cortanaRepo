using windows_client.Model;
using System.Linq;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System;

namespace windows_client.DbUtils
{
    public class MiscDBUtil
    {
        private static object lockObj = new object();
        public static readonly string THUMBNAILS = "THUMBNAILS";
        private static MySerializer ser = new MySerializer();

        public static void clearDatabase()
        {
            #region DELETE CONVS,CHAT MSGS, GROUPS, GROUP MEMBERS
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                //context.conversations.DeleteAllOnSubmit<ConversationListObject>(context.GetTable<ConversationListObject>());
                ConversationTableUtils.deleteAllConversations();
                context.messages.DeleteAllOnSubmit<ConvMessage>(context.GetTable<ConvMessage>());
                context.groupInfo.DeleteAllOnSubmit<GroupInfo>(context.GetTable<GroupInfo>());
                context.groupMembers.DeleteAllOnSubmit<GroupMembers>(context.GetTable<GroupMembers>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }

                catch (ChangeConflictException e)
                {
                    Debug.WriteLine(e.Message);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges);
                    }
                }

                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);

            }
            #endregion
            #region DELETE USERS, BLOCKLIST, THUMBNAILS
            using (HikeUsersDb context = new HikeUsersDb(App.UsersDBConnectionstring))
            {
                context.blockedUsersTable.DeleteAllOnSubmit<Blocked>(context.GetTable<Blocked>());
                context.users.DeleteAllOnSubmit<ContactInfo>(context.GetTable<ContactInfo>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }

                catch (ChangeConflictException e)
                {
                    Debug.WriteLine(e.Message);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges);
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

                catch (ChangeConflictException e)
                {
                    Debug.WriteLine(e.Message);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges);
                    }
                }

                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
            #endregion
        }

        public static void saveAvatarImage(string msisdn, byte[] imageBytes)
        {
            string FileName = THUMBNAILS + "\\" + msisdn;
            lock (lockObj)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
                {
                    using (FileStream stream = new IsolatedStorageFileStream(FileName, FileMode.Create, FileAccess.Write, store))
                    {
                        stream.Write(imageBytes, 0, imageBytes.Length);
                    }
                }
            }
        }

        public static byte[] getThumbNailForMsisdn(string msisdn)
        {
            byte[] data = null;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (store.FileExists(THUMBNAILS + "\\" + msisdn)) // Check if file exists
                {
                    using (IsolatedStorageFileStream isfs = store.OpenFile(THUMBNAILS + "\\" + msisdn, FileMode.Open, FileAccess.Read))
                    {
                        data = new byte[isfs.Length];
                        // Read the entire file and then close it
                        isfs.Read(data, 0, data.Length);
                        isfs.Close();
                    }
                }
            }
            return data;
        }

        #region FILE TRANSFER UTILS

        public static void saveAttachmentObject(Attachment obj, string msisdn, long messageId)
        {
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
            string attachmentObjectPath = HikeConstants.FILES_ATTACHMENT + "/" + msisdn + "/" + Convert.ToString(messageId);
            string attachmentFileBytes = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn + "/" + Convert.ToString(messageId);
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                store.DeleteFile(attachmentObjectPath);
                store.DeleteFile(attachmentFileBytes);
            }
        }

        public static void deleteMsisdnData(string msisdn)
        {
            string[] attachmentPaths = new string[2];
            attachmentPaths[0] = HikeConstants.FILES_ATTACHMENT + "/" + msisdn;
            attachmentPaths[1] = HikeConstants.FILES_BYTE_LOCATION + "/" + msisdn;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach(string attachmentPath in attachmentPaths)
                {
                    string[] fileNames = store.GetFileNames(attachmentPath + "/*");
                    foreach (string fileName in fileNames)
                    {
                        store.DeleteFile(attachmentPath + "/" + fileName);
                    }
                }
            }
        }

        public static void deleteAllAttachmentData()
        {
            string[] attachmentPaths = new string[2];
            attachmentPaths[0] = HikeConstants.FILES_ATTACHMENT;
            attachmentPaths[1] = HikeConstants.FILES_BYTE_LOCATION;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach(string attachmentPath in attachmentPaths)
                {
                    string[] directoryNames = store.GetDirectoryNames(attachmentPath + "/*");
                    foreach (string directoryName in directoryNames)
                    {
                        string[] fileNames = store.GetFileNames(attachmentPath + "/" + directoryName + "/*");
                        foreach (string fileName in fileNames)
                        {
                            store.DeleteFile(attachmentPath + "/" + directoryName + "/" + fileName);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
