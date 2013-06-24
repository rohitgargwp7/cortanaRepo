using System.IO;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Windows;
using System.Windows.Navigation;
using System;
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
            mPubSub.addListener(HikePubSub.ATTACHMENT_RESEND, this);
            mPubSub.addListener(HikePubSub.ATTACHMENT_SENT, this);
            mPubSub.addListener(HikePubSub.FORWARD_ATTACHMENT, this);
            mPubSub.addListener(HikePubSub.SAVE_STATUS_IN_DB, this);
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
            mPubSub.removeListener(HikePubSub.ATTACHMENT_RESEND, this);
            mPubSub.removeListener(HikePubSub.ATTACHMENT_SENT, this);
            mPubSub.removeListener(HikePubSub.FORWARD_ATTACHMENT, this);
            mPubSub.removeListener(HikePubSub.SAVE_STATUS_IN_DB, this);
        }

        public void uploadFileCallback(JObject obj, ConvMessage convMessage)
        {
            if (obj != null && convMessage.FileAttachment.FileState != Attachment.AttachmentState.CANCELED
                && convMessage.FileAttachment.FileState != Attachment.AttachmentState.FAILED_OR_NOT_STARTED)
            {
                JObject data = obj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                string fileKey = data[HikeConstants.FILE_KEY].ToString();
                string fileName = data[HikeConstants.FILE_NAME].ToString();
                string contentType = data[HikeConstants.FILE_CONTENT_TYPE].ToString();

                convMessage.ProgressBarValue = 100;
                //DO NOT Update message text in db. We sent the below line, but we save content type as message.
                //here message status should be updated in db, as on event listener message state should be unknown

                if (contentType.Contains(HikeConstants.IMAGE))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Photo_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileKey;
                }
                else if (contentType.Contains(HikeConstants.AUDIO))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Voice_msg_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileKey;
                }
                else if (contentType.Contains(HikeConstants.LOCATION))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Location_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileKey;
                }
                else if (contentType.Contains(HikeConstants.VIDEO))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.Video_Txt) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileKey;
                }
                else if (contentType.Contains(HikeConstants.CT_CONTACT))
                {
                    convMessage.Message = String.Format(AppResources.FILES_MESSAGE_PREFIX, AppResources.ContactTransfer_Text) + HikeConstants.FILE_TRANSFER_BASE_URL +
                        "/" + fileKey;
                }
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                convMessage.FileAttachment.FileKey = fileKey;
                convMessage.FileAttachment.ContentType = contentType;
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));
                convMessage.SetAttachmentState(Attachment.AttachmentState.COMPLETED);
                MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
            }
            else
            {
                convMessage.MessageStatus = ConvMessage.State.SENT_FAILED;
                convMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
            }
        }

        //call this from UI thread
        private void addSentMessageToMsgMap(ConvMessage conMessage)
        {
            NewChatThread currentPage = App.newChatThreadPage;
            if (currentPage != null && conMessage != null)
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
                if (obj is object[])
                {
                    object[] vals = (object[])obj;
                    convMessage = (ConvMessage)vals[0];
                    isNewGroup = (bool)vals[1];
                }
                else
                {
                    convMessage = (ConvMessage)obj;
                    isNewGroup = false;
                }
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, isNewGroup);
                if (convObj == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    addSentMessageToMsgMap(convMessage);

                    if (App.ViewModel.MessageListPageCollection.Contains(convObj))//cannot use convMap here because object has pushed to map but not to ui
                    {
                        App.ViewModel.MessageListPageCollection.Remove(convObj);
                    }

                    App.ViewModel.MessageListPageCollection.Insert(0, convObj);

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

                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, false);
                convMessage.MessageId = convMessage.MessageId;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    addSentMessageToMsgMap(convMessage);

                    if (App.ViewModel.MessageListPageCollection.Contains(convObj))//cannot use convMap here because object has pushed to map but not to ui
                    {
                        App.ViewModel.MessageListPageCollection.Remove(convObj);
                    }

                    App.ViewModel.MessageListPageCollection.Insert(0, convObj);
                    MessagesTableUtils.addUploadingOrDownloadingMessage(convMessage.MessageId, convMessage);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.STARTED);

                    byte[] fileBytes;
                    MiscDBUtil.readFileFromIsolatedStorage(sourceFilePath, out fileBytes);
                    AccountUtils.postUploadPhotoFunction finalCallbackForUploadFile = new AccountUtils.postUploadPhotoFunction(uploadFileCallback);
                    if (!convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                                Convert.ToString(convMessage.MessageId), fileBytes);
                    AccountUtils.uploadFile(fileBytes, finalCallbackForUploadFile, convMessage);
                });
            }
            #endregion
            #region ATTACHMENT_SEND
            else if (HikePubSub.ATTACHMENT_SENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                byte[] fileBytes = (byte[])vals[1];

                //In case of sending attachments, here message state should be unknown instead of sent_unconfirmed
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, false);

                // in case of db failure convObj returned will be null
                if (convObj == null)
                    return;

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    addSentMessageToMsgMap(convMessage);
                    if (App.ViewModel.MessageListPageCollection.Contains(convObj))
                    {
                        App.ViewModel.MessageListPageCollection.Remove(convObj);
                    }
                    App.ViewModel.MessageListPageCollection.Insert(0, convObj);
                    //send attachment message (new attachment - upload case)
                    MessagesTableUtils.addUploadingOrDownloadingMessage(convMessage.MessageId, convMessage);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.FAILED_OR_NOT_STARTED);
                    MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    convMessage.SetAttachmentState(Attachment.AttachmentState.STARTED);

                    AccountUtils.postUploadPhotoFunction finalCallbackForUploadFile = new AccountUtils.postUploadPhotoFunction(uploadFileCallback);
                    if (!convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                        MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                                Convert.ToString(convMessage.MessageId), fileBytes);
                    AccountUtils.uploadFile(fileBytes, finalCallbackForUploadFile, convMessage);
                });
            }
            #endregion
            #region ATTACHMENT_RESEND_OR_FORWARD
            else if (HikePubSub.ATTACHMENT_RESEND == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                byte[] fileBytes;
                if (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                    fileBytes = Encoding.UTF8.GetBytes(convMessage.MetaDataString);
                else
                    MiscDBUtil.readFileFromIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                                Convert.ToString(convMessage.MessageId), out fileBytes);
                AccountUtils.postUploadPhotoFunction finalCallbackForUploadFile = new AccountUtils.postUploadPhotoFunction(uploadFileCallback);
                AccountUtils.uploadFile(fileBytes, finalCallbackForUploadFile, convMessage);
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
                if (delConv)
                {
                    // delete the conversation from DB.
                    ConversationTableUtils.deleteConversation(c.Msisdn);
                    //ConversationTableUtils.saveConvObjectList();
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
                byte[] thumbnailBytes = (byte[])vals[2];
                if (Utils.isGroupConversation(msisdn))
                {
                    byte[] fullViewBytes = (byte[])vals[1];
                    string grpId = msisdn.Replace(":", "_");
                    MiscDBUtil.saveAvatarImage(grpId + HikeConstants.FULL_VIEW_IMAGE_PREFIX, fullViewBytes, false);
                    MiscDBUtil.saveAvatarImage(grpId, thumbnailBytes, false);
                }
                else
                {
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
            }
            #endregion
            else if (type == HikePubSub.SAVE_STATUS_IN_DB)
            {
                StatusMessage sm = obj as StatusMessage;
                StatusMsgsTable.InsertStatusMsg(sm, false);
            }
        }

        private JObject blockUnblockSerialize(string type, string msisdn)
        {
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] = type;
            obj[HikeConstants.DATA] = msisdn;
            return obj;
        }

        private void updateDbBatch(long[] ids, int status)
        {
            string msisdn = MessagesTableUtils.updateAllMsgStatus(null, ids, status);
            ConversationTableUtils.updateLastMsgStatus(ids[ids.Length - 1], msisdn, status);
        }
    }
}
