using System.IO;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Windows;
using System.Windows.Navigation;
using System;
using System.Linq;
using System.Data.Linq;
using System.ComponentModel;
using windows_client.utils;
using windows_client.View;
using windows_client.Controls;
using System.Threading;
using windows_client.Misc;
using windows_client.Languages;
using Microsoft.Phone.Controls;
using System.Text;
using windows_client.FileTransfers;
using System.Diagnostics;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Net.NetworkInformation;

namespace windows_client.DbUtils
{
    public class DbConversationListener : HikePubSub.Listener
    {
        private HikePubSub mPubSub;

        /* Register all the listeners*/
        public DbConversationListener()
        {
            mPubSub = App.HikePubSubInstance;
            registerListeners();
        }

        public void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.addListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.addListener(HikePubSub.GROUP_LEFT, this);
            mPubSub.addListener(HikePubSub.BLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.DELETE_CONVERSATION, this);
            mPubSub.addListener(HikePubSub.ATTACHMENT_SENT, this);
            mPubSub.addListener(HikePubSub.FORWARD_ATTACHMENT, this);
            mPubSub.addListener(HikePubSub.SAVE_STATUS_IN_DB, this);
            mPubSub.addListener(HikePubSub.FILE_STATE_CHANGED, this);
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.removeListener(HikePubSub.GROUP_LEFT, this);
            mPubSub.removeListener(HikePubSub.BLOCK_GROUPOWNER, this);
            mPubSub.removeListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.removeListener(HikePubSub.DELETE_CONVERSATION, this);
            mPubSub.removeListener(HikePubSub.ATTACHMENT_SENT, this);
            mPubSub.removeListener(HikePubSub.FORWARD_ATTACHMENT, this);
            mPubSub.removeListener(HikePubSub.SAVE_STATUS_IN_DB, this);
            mPubSub.removeListener(HikePubSub.FILE_STATE_CHANGED, this);
        }

        /// <summary>
        /// call this from UI thread for outgoing messages. These messages will be added to msgmap on new chat thread page
        /// </summary>
        /// <param name="conMessage"></param>
        private void addSentMessageToMsgMap(ConvMessage conMessage)
        {
            NewChatThread currentPage = App.newChatThreadPage;
            if (currentPage != null && conMessage != null && currentPage.mContactNumber == conMessage.Msisdn)
            {
                currentPage.OutgoingMsgsMap[conMessage.MessageId] = conMessage;
            }
        }

        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_SENT
            if (HikePubSub.MESSAGE_SENT == type)
            {
                ConvMessage convMessage;
                bool isNewGroup;
                byte[] imageBytes = null;

                if (obj is object[])
                {
                    object[] vals = (object[])obj;
                    convMessage = (ConvMessage)vals[0];
                    isNewGroup = (bool)vals[1];
                    imageBytes = MiscDBUtil.getThumbNailForMsisdn(convMessage.Msisdn);
                }
                else
                {
                    convMessage = (ConvMessage)obj;
                    isNewGroup = false;
                }

                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, isNewGroup, imageBytes);
                if (convObj == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateConvListForSentMessage(convMessage, convObj);

                    if (!isNewGroup)
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(convMessage.IsSms ? false : true));
                });
                //if (!isNewGroup)
                //    mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(convMessage.IsSms ? false : true));
            }
            #endregion
            #region FORWARD_ATTACHMENT
            else if (HikePubSub.FORWARD_ATTACHMENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                string sourceFilePath = (string)vals[1];

                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, false);
                convMessage.MessageId = convMessage.MessageId;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateConvListForSentMessage(convMessage, convObj);

                    byte[] fileBytes;
                    if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT) || convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
                        fileBytes = Encoding.UTF8.GetBytes(convMessage.MetaDataString);
                    else
                        MiscDBUtil.readFileFromIsolatedStorage(sourceFilePath, out fileBytes);

                    if (fileBytes == null)
                        return;

                    MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn.Replace(":", "_") + "/" + Convert.ToString(convMessage.MessageId), fileBytes);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.NOT_STARTED);
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);

                    if (FileTransferManager.Instance.IsTransferPossible())
                        FileTransfers.FileTransferManager.Instance.UploadFile(convMessage.Msisdn, convMessage.MessageId.ToString(), convMessage.FileAttachment.FileName, convMessage.FileAttachment.ContentType, fileBytes.Length);
                    else
                        MessageBox.Show(AppResources.FT_MaxFiles_Txt, AppResources.FileTransfer_LimitReached, MessageBoxButton.OK);
                });
            }
            #endregion
            #region ATTACHMENT_SEND
            else if (HikePubSub.ATTACHMENT_SENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                byte[] fileBytes = (byte[])vals[1];

                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, false);

                // in case of db failure convObj returned will be null
                if (convObj == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateConvListForSentMessage(convMessage, convObj);
                    //send attachment message (new attachment - upload case)

                    MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn.Replace(":", "_") + "/" + Convert.ToString(convMessage.MessageId), fileBytes);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.NOT_STARTED);
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);

                    if (FileTransferManager.Instance.IsTransferPossible())
                    {
                        if (!NetworkInterface.GetIsNetworkAvailable())
                            MessageBox.Show(AppResources.FileTransfer_NetworkError, AppResources.NetworkError_TryAgain, MessageBoxButton.OK);

                        FileTransfers.FileTransferManager.Instance.UploadFile(convMessage.Msisdn, convMessage.MessageId.ToString(), convMessage.FileAttachment.FileName, convMessage.FileAttachment.ContentType, fileBytes.Length);
                    }
                    else
                        MessageBox.Show(AppResources.FT_MaxFiles_Txt, AppResources.FileTransfer_LimitReached, MessageBoxButton.OK);
                });
            }
            #endregion
            #region MESSAGE_RECEIVED_READ
            else if (HikePubSub.MESSAGE_RECEIVED_READ == type)  // represents event when a msg is read by this user
            {
                long[] ids = (long[])obj;
                if (ids == null || ids.Length == 0)
                    return;
                updateDbBatch(ids, (int)ConvMessage.State.RECEIVED_READ);
            }
            #endregion
            #region MESSAGE_DELETED
            else if (HikePubSub.MESSAGE_DELETED == type)
            {
                object[] o = (object[])obj;

                long msgId = (long)o[0];
                MessagesTableUtils.deleteMessage(msgId); // delete msg with given msgId from messages table

                ConversationListObject c = (ConversationListObject)o[1];
                bool delConv = (bool)o[2];
                //remove attachment if present
                MiscDBUtil.deleteMessageData(c.Msisdn, msgId);

                if (delConv)
                {
                    // delete the conversation from DB.
                    ConversationTableUtils.deleteConversation(c.Msisdn);
                    //ConversationTableUtils.saveConvObjectList();
                    MessagesTableUtils.DeleteLongMessages(c.Msisdn);

                }
                else
                {
                    //update conversation
                    ConversationTableUtils.updateConversation(c);
                }
                // TODO :: persistence.removeMessage(msgId);
            }
            #endregion
            #region BLOCK_USER
            else if (HikePubSub.BLOCK_USER == type)
            {
                string msisdn;
                if (obj is ContactInfo)
                    msisdn = (obj as ContactInfo).Msisdn;
                else
                    msisdn = (string)obj;
                UsersTableUtils.block(msisdn);
                JObject blockObj = blockUnblockSerialize("b", msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, blockObj);
            }
            #endregion
            #region UNBLOCK_USER
            else if (HikePubSub.UNBLOCK_USER == type)
            {
                string msisdn;
                if (obj is ContactInfo)
                    msisdn = (obj as ContactInfo).Msisdn;
                else
                    msisdn = (string)obj;
                UsersTableUtils.unblock(msisdn);
                JObject unblockObj = blockUnblockSerialize("ub", msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, unblockObj);
            }
            #endregion
            #region ADD_OR_UPDATE_PROFILE
            else if (HikePubSub.ADD_OR_UPDATE_PROFILE == type)
            {
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                byte[] fullViewBytes = (byte[])vals[1];
                byte[] thumbnailBytes = (byte[])vals[2];
                if (Utils.isGroupConversation(msisdn))
                {
                    string grpId = msisdn.Replace(":", "_");
                    MiscDBUtil.saveLargeImage(grpId, fullViewBytes);
                    MiscDBUtil.saveAvatarImage(grpId, thumbnailBytes, false);
                }
                else
                {
                    MiscDBUtil.saveLargeImage(HikeConstants.MY_PROFILE_PIC, fullViewBytes);
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, thumbnailBytes, false);
                }
            }
            #endregion
            #region GROUP LEFT
            else if (HikePubSub.GROUP_LEFT == type)
            {
                /*
                 * 1. Delete conversation with this groupId
                 * 2. Delete ConvMessages
                 * 3. Delete GroupInfo
                 * 4. Delete GroupMembers
                 */
                string groupId = (string)obj;
                ConversationTableUtils.deleteConversation(groupId);
                //ConversationTableUtils.saveConvObjectList();
                MiscDBUtil.DeleteImageForMsisdn(groupId);
                MessagesTableUtils.deleteAllMessagesForMsisdn(groupId);
                GroupTableUtils.deleteGroupWithId(groupId);
                GroupManager.Instance.GroupCache.Remove(groupId);
                GroupManager.Instance.DeleteGroup(groupId);
            }
            #endregion
            #region BLOCK GROUP OWNER
            else if (HikePubSub.BLOCK_GROUPOWNER == type)
            {
                string groupOwner = (string)obj;
                UsersTableUtils.block(groupOwner);
                JObject blockObj = blockUnblockSerialize("b", groupOwner);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, blockObj);
            }
            #endregion
            #region UNBLOCK GROUP OWNER
            else if (HikePubSub.UNBLOCK_GROUPOWNER == type)
            {
                string groupOwner = (string)obj;
                UsersTableUtils.unblock(groupOwner);
                JObject unblockObj = blockUnblockSerialize("ub", groupOwner);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, unblockObj);
            }
            #endregion
            #region DELETE CONVERSATION
            else if (HikePubSub.DELETE_CONVERSATION == type)
            {
                string convMsisdn = (string)obj;
                if (Utils.isGroupConversation(convMsisdn)) // if Group Conversation delete groups too
                {
                    GroupTableUtils.deleteGroupWithId(convMsisdn); // remove entry from Group Table
                    GroupManager.Instance.GroupCache.Remove(convMsisdn);
                    GroupManager.Instance.DeleteGroup(convMsisdn); // delete the group file
                }
                MessagesTableUtils.deleteAllMessagesForMsisdn(convMsisdn); //removed all chat messages for this msisdn
                ConversationTableUtils.deleteConversation(convMsisdn); // removed entry from conversation table
                //ConversationTableUtils.saveConvObjectList();
                MiscDBUtil.deleteMsisdnData(convMsisdn);
                MessagesTableUtils.DeleteLongMessages(convMsisdn);
            }
            #endregion
            else if (type == HikePubSub.SAVE_STATUS_IN_DB)
            {
                StatusMessage sm = obj as StatusMessage;
                StatusMsgsTable.InsertStatusMsg(sm, false);
            }
            else if (type == HikePubSub.FILE_STATE_CHANGED)
            {
                var fInfo = obj as FileInfoBase;

                using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring + ";Max Buffer Size = 1024"))
                {
                    var id = Convert.ToInt64(fInfo.MessageId);
                    ConvMessage convMessage = DbCompiledQueries.GetMessagesForMsgId(context, id).FirstOrDefault<ConvMessage>();

                    if (convMessage == null)
                    {
                        FileTransferManager.Instance.TaskMap.Remove(fInfo.MessageId);
                        fInfo.Delete();
                        return;
                    }

                    try
                    {
                        var attachment = MiscDBUtil.getFileAttachment(fInfo.Msisdn, fInfo.MessageId);
                        if (attachment == null)
                        {
                            FileTransferManager.Instance.TaskMap.Remove(fInfo.MessageId);
                            fInfo.Delete();
                            return;
                        }

                        convMessage.FileAttachment = attachment;

                        Attachment.AttachmentState state = Attachment.AttachmentState.FAILED;

                        if (fInfo.FileState == FileTransferState.CANCELED)
                        {
                            state = Attachment.AttachmentState.CANCELED;

                            if (convMessage.IsSent)
                                convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                        }
                        else if (fInfo.FileState == FileTransferState.COMPLETED)
                        {
                            state = Attachment.AttachmentState.COMPLETED;

                            if (fInfo is FileUploader)
                                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                        }
                        else if (fInfo.FileState == FileTransferState.PAUSED)
                            state = Attachment.AttachmentState.PAUSED;
                        else if (fInfo.FileState == FileTransferState.MANUAL_PAUSED)
                            state = Attachment.AttachmentState.MANUAL_PAUSED;
                        else if (fInfo.FileState == FileTransferState.FAILED)
                        {
                            state = Attachment.AttachmentState.FAILED;

                            if (fInfo is FileUploader)
                                convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                        }
                        else if (fInfo.FileState == FileTransferState.STARTED)
                            state = Attachment.AttachmentState.STARTED;
                        else if (fInfo.FileState == FileTransferState.NOT_STARTED)
                            state = Attachment.AttachmentState.NOT_STARTED;
                        else if (fInfo.FileState == FileTransferState.DOES_NOT_EXIST)
                            state = Attachment.AttachmentState.FAILED;

                        if (fInfo.FileState == FileTransferState.COMPLETED)
                            convMessage.ProgressBarValue = 100;

                        convMessage.SetAttachmentState(state);

                        if (fInfo is FileDownloader)
                        {
                            if (fInfo.FileState == FileTransferState.COMPLETED && FileTransferManager.Instance.TaskMap.ContainsKey(fInfo.MessageId))
                            {
                                if (!App.appSettings.Contains(App.AUTO_SAVE_MEDIA) && (fInfo.ContentType.Contains(HikeConstants.VIDEO) || fInfo.ContentType.Contains(HikeConstants.IMAGE)))
                                {
                                    string targetFileName = fInfo.MessageId + "_" + TimeUtils.getCurrentTimeStamp();

                                    if (fInfo.ContentType.Contains(HikeConstants.VIDEO))
                                        targetFileName = targetFileName + ".mp4";
                                    else if (fInfo.ContentType.Contains(HikeConstants.IMAGE))
                                        targetFileName = targetFileName + ".jpg";

                                    string sourceFile = HikeConstants.FILES_BYTE_LOCATION + "/" + fInfo.Msisdn.Replace(":", "_") + "/" + fInfo.MessageId;
                                    string absoluteFilePath = Utils.GetAbsolutePath(sourceFile);
                                    Utils.StoreFileInHikeDirectory(absoluteFilePath, targetFileName);
                                }
                                FileTransferManager.Instance.TaskMap.Remove(fInfo.MessageId);
                                fInfo.Delete();
                            }
                        }
                        else
                        {
                            if (fInfo.FileState == FileTransferState.COMPLETED)
                            {
                                JObject data = (fInfo as FileUploader).SuccessObj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                                var fileKey = data[HikeConstants.FILE_KEY].ToString();

                                //send the content type which is sent by server
                                fInfo.ContentType = data[HikeConstants.FILE_CONTENT_TYPE].ToString();

                                int fileSize = 0;
                                JToken fs;
                                if (data.TryGetValue(HikeConstants.FILE_SIZE, out fs))
                                    fileSize = Convert.ToInt32(fs.ToString());

                                if (fInfo.ContentType.Contains(HikeConstants.IMAGE))
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }
                                else if (fInfo.ContentType.Contains(HikeConstants.AUDIO))
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }
                                else if (fInfo.ContentType.Contains(HikeConstants.VIDEO))
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }
                                else if (fInfo.ContentType.Contains(HikeConstants.CT_CONTACT))
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.ContactTransfer_Text) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }
                                else if (fInfo.ContentType.Contains(HikeConstants.LOCATION))
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Location_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }
                                else
                                {
                                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.UnknownFile_txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                                        "/" + fileKey;
                                }

                                convMessage.FileAttachment.ContentType = fInfo.ContentType;
                                convMessage.FileAttachment.FileSize = fileSize;
                                convMessage.FileAttachment.FileKey = fileKey;
                                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;

                                App.HikePubSubInstance.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));

                                FileTransferManager.Instance.TaskMap.Remove(fInfo.MessageId);
                                fInfo.Delete();
                            }
                            else if (fInfo.FileState == FileTransferState.FAILED)
                            {
                                FileTransferManager.Instance.TaskMap.Remove(fInfo.MessageId);
                                convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                                MiscDBUtil.UpdateDBsMessageStatus(null, convMessage.MessageId, (int)ConvMessage.State.SENT_FAILED);
                            }
                        }

                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("DbConversationListener :: FILE_STATE_CHANGED : FILE_STATE_CHANGED, Exception : " + e.StackTrace);
                    }
                }
            }
        }

        private void UpdateConvListForSentMessage(ConvMessage convMessage, ConversationListObject convObj)
        {
            if (convObj == null)
                return;
            addSentMessageToMsgMap(convMessage);

            int index = App.ViewModel.MessageListPageCollection.IndexOf(convObj);
            //cannot use convMap here because object has pushed to map but not to ui
            if (index < 0)//not present in convmap
            {
                App.ViewModel.MessageListPageCollection.Insert(0, convObj);
            }
            else if (index > 0)
            {
                App.ViewModel.MessageListPageCollection.RemoveAt(index);
                App.ViewModel.MessageListPageCollection.Insert(0, convObj);
            }//if already at zero, do nothing

            App.ViewModel.IsConversationUpdated = true;
        }

        private JObject blockUnblockSerialize(string type, string msisdn)
        {
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = type;
            obj[HikeConstants.DATA] = msisdn;
            return obj;
        }

        /// <summary>
        /// Update messag db with messages as Received Read
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="status"></param>
        private void updateDbBatch(long[] ids, int status)
        {
            string msisdn = MessagesTableUtils.updateAllMsgStatus(null, ids, status, null);
            ConversationTableUtils.updateLastMsgStatus(ids[ids.Length - 1], msisdn, status);
        }
    }
}
