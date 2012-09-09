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
            mPubSub.addListener(HikePubSub.DELETE_ACCOUNT, this);
            mPubSub.addListener(HikePubSub.GROUP_LEFT, this);
            mPubSub.addListener(HikePubSub.BLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.addListener(HikePubSub.DELETE_CONVERSATION,this);
            mPubSub.addListener(HikePubSub.DELETE_ALL_CONVERSATIONS, this);
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.removeListener(HikePubSub.DELETE_ACCOUNT, this);
            mPubSub.removeListener(HikePubSub.GROUP_LEFT, this);
            mPubSub.removeListener(HikePubSub.BLOCK_GROUPOWNER, this);
            mPubSub.removeListener(HikePubSub.UNBLOCK_GROUPOWNER, this);
            mPubSub.removeListener(HikePubSub.DELETE_CONVERSATION,this);
            mPubSub.removeListener(HikePubSub.DELETE_ALL_CONVERSATIONS, this);
        }


        public void uploadFileCallback(JObject obj, ConvMessage convMessage)
        {
            string response = obj.ToString();
            if (obj != null)
            {
                JObject data = obj[HikeConstants.FILE_RESPONSE_DATA].ToObject<JObject>();
                string fileKey = data[HikeConstants.FILE_KEY].ToString();
                string fileName = data[HikeConstants.FILE_NAME].ToString();
                string contentType = data[HikeConstants.FILE_CONTENT_TYPE].ToString();

                //DO NOT Update message text in db. We sent the below line, but we save only filename as message.
                convMessage.Message = "I sent you a file. To view go to " + HikeConstants.FILE_TRANSFER_BASE_URL + "/" + fileKey;
                
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                convMessage.FileAttachment.FileKey = fileKey;
                convMessage.FileAttachment.ContentType = contentType;

                MessagesTableUtils.updateMsgStatus(convMessage.MessageId,(int) ConvMessage.State.SENT_UNCONFIRMED);
                MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);

                //TODO add fileAttachment object in map
//                attachments.Add(convMessage.MessageId, convMessage.FileAttachment);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));
            }
        }


        public void onEventReceived(string type, object obj)
        {

            #region MESSAGE_SENT
            if (HikePubSub.MESSAGE_SENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];

                bool isNewGroup = false;
                if (vals[1] is bool)
                {
                    isNewGroup = (bool)vals[1];
                }
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage, isNewGroup);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if(App.ViewModel.MessageListPageCollection.Contains(convObj))
                    {
                        App.ViewModel.MessageListPageCollection.Remove(convObj);
                    }
                    App.ViewModel.MessageListPageCollection.Insert(0, convObj);
                });
                if (vals.Length == 2)
                {
                    if (vals[1] is bool)
                    {
                        if (!isNewGroup)
                            mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));
                    }
                    else if (vals[1] is string)
                    {
                        string sourceFilePath = vals[1] as string;
                        string destinationFilePath = HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" + convMessage.MessageId;
                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                        MiscDBUtil.copyFileInIsolatedStorage(sourceFilePath, destinationFilePath);
                        mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));
                    }
                }
                else if (vals.Length == 4)
                {
                    byte[] thumbnail = vals[1] as byte[];
                    byte[] largeImage = vals[2] as byte[];
                    SentChatBubble chatbubble = vals[3] as SentChatBubble;

                    //  MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    convMessage.FileAttachment.FileState = Attachment.AttachmentState.STARTED;
                    AccountUtils.postUploadPhotoFunction finalCallbackForUploadFile = new AccountUtils.postUploadPhotoFunction(uploadFileCallback);

                    MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_THUMBNAILS + "/" + convMessage.Msisdn + "/" +
                            Convert.ToString(convMessage.MessageId), thumbnail);

                    MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                            Convert.ToString(convMessage.MessageId), largeImage);
                    AccountUtils.uploadFile(largeImage, finalCallbackForUploadFile, convMessage, chatbubble);

                }
            }
            #endregion
            #region MESSAGE_RECEIVED_READ
            else if (HikePubSub.MESSAGE_RECEIVED_READ == type)  // represents event when a msg is read by this user
            {
                long[] ids = (long[])obj;
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
                string msisdn = (string)obj;
                UsersTableUtils.block(msisdn);
                JObject blockObj = blockUnblockSerialize("b", msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, blockObj);
            }
            #endregion
            #region UNBLOCK_USER
            else if (HikePubSub.UNBLOCK_USER == type)
            {
                string msisdn = (string)obj;
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

                byte[] thumbnailBytes = (byte[])vals[1];
                byte[] fileBytes = (byte[])vals[2];
                if (Utils.isGroupConversation(msisdn))
                {
                    string grpId = msisdn.Replace(":", "_");
                    MiscDBUtil.saveAvatarImage(grpId, thumbnailBytes);
                }
                else
                {
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC + "_small", thumbnailBytes);
                    MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, fileBytes);
                }
            }
            #endregion
            #region DELETE ACCOUNT
            else if (HikePubSub.DELETE_ACCOUNT == type)
            {
                removeListeners();
                MiscDBUtil.clearDatabase();
                mPubSub.publish(HikePubSub.ACCOUNT_DELETED, null);
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
                MessagesTableUtils.deleteAllMessagesForMsisdn(groupId);
                GroupTableUtils.deleteGroupWithId(groupId);
                GroupTableUtils.deleteGroupMembersWithId(groupId);
            }
            #endregion
            #region BLOCK GROUP OWNER
            else if (HikePubSub.BLOCK_GROUPOWNER == type)
            {
                object[] vals = (object[])obj;
                string groupId = (string)vals[0];
                string groupOwner = (string)vals[1];
                UsersTableUtils.block(groupId);
                JObject blockObj = blockUnblockSerialize("b", groupOwner);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, blockObj);
            }
            #endregion
            #region UNBLOCK GROUP OWNER
            else if (HikePubSub.UNBLOCK_GROUPOWNER == type)
            {
                object[] vals = (object[])obj;
                string groupId = (string)vals[0];
                string groupOwner = (string)vals[1];
                UsersTableUtils.unblock(groupId);
                JObject unblockObj = blockUnblockSerialize("ub", groupOwner);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, unblockObj);
            }
            else if (HikePubSub.DELETE_CONVERSATION == type)
            {
                string convMsisdn = (string)obj;
                if (Utils.isGroupConversation(convMsisdn)) // if Group Conversation delete groups too
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.WorkerSupportsCancellation = true;
                    bw.DoWork += new DoWorkEventHandler(deleteGroupsAsync);
                    bw.RunWorkerAsync(convMsisdn);
                }
                MessagesTableUtils.deleteAllMessagesForMsisdn(convMsisdn); //removed all chat messages for this msisdn
                ConversationTableUtils.deleteConversation(convMsisdn); // removed entry from conversation table
            }
            else if (HikePubSub.DELETE_ALL_CONVERSATIONS == type)
            {
                MessagesTableUtils.deleteAllMessages();
                ConversationTableUtils.deleteAllConversations();
                foreach (string convMsisdn in ConversationsList.ConvMap.Keys)
                {
                    if (Utils.isGroupConversation(convMsisdn))
                    {
                        JObject jObj = new JObject();
                        jObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE;
                        jObj[HikeConstants.TO] = convMsisdn;
                        App.MqttManagerInstance.mqttPublishToServer(jObj);
                    }
                }

                BackgroundWorker bw = new BackgroundWorker();
                bw.WorkerSupportsCancellation = true;
                bw.DoWork += new DoWorkEventHandler(deleteAllGroupsAsync);
                bw.RunWorkerAsync();
                mPubSub.publish(HikePubSub.DELETED_ALL_CONVERSATIONS, null);
            }
            #endregion
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
            string msisdn = MessagesTableUtils.updateAllMsgStatus(ids, status);
            ConversationTableUtils.updateLastMsgStatus(msisdn,status);
        }

        private void deleteGroupsAsync(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            string msisdn = (string)e.Argument;
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                GroupTableUtils.deleteGroupMembersWithId(msisdn);
                GroupTableUtils.deleteGroupWithId(msisdn);
            }
        }

        private void deleteAllGroupsAsync(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;           
            if ((worker.CancellationPending == true))
            {
                e.Cancel = true;
            }
            else
            {
                GroupTableUtils.deleteAllGroupMembers();
                GroupTableUtils.deleteAllGroups();
            }
        }
    }
}
