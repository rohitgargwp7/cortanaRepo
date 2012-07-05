using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using windows_client;
using windows_client.Model;
using Newtonsoft.Json.Linq;
using windows_client.converters;
using System.IO;
using System.Windows.Media.Imaging;

namespace windows_client.DbUtils
{
    public class DbConversationListener : HikePubSub.Listener
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private HikePubSub mPubSub;

        /* Register all the listeners*/
        public DbConversationListener()
        {
            mPubSub = App.HikePubSubInstance;
            registerListeners();           
        }

        private void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_FROM_SENDER, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.addListener(HikePubSub.USER_JOINED, this);
            mPubSub.addListener(HikePubSub.USER_LEFT, this);
            mPubSub.addListener(HikePubSub.ICON_CHANGED, this);
            mPubSub.addListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
        }

        private void removeListeners()
        {
           mPubSub.removeListener(HikePubSub.MESSAGE_SENT, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_FROM_SENDER, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
           mPubSub.removeListener(HikePubSub.SERVER_RECEIVED_MSG, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_DELETED, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_FAILED, this);
           mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
           mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
           mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
           mPubSub.removeListener(HikePubSub.SMS_CREDIT_CHANGED, this);
           mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
           mPubSub.removeListener(HikePubSub.USER_JOINED, this);
           mPubSub.removeListener(HikePubSub.USER_LEFT, this);
           mPubSub.removeListener(HikePubSub.ICON_CHANGED, this);
           mPubSub.removeListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
        }

        public void onEventReceived(string type, object obj)
        {
            #region MESSAGE_SENT
            if (HikePubSub.MESSAGE_SENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                bool isNewConv = (bool)vals[1];
                MessagesTableUtils.addChatMessage(convMessage, isNewConv);
                logger.Info("DBCONVERSATION LISTENER", "Sending Message : " + convMessage.Message + " ; to : " + convMessage.Msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize());
            }
            #endregion
            #region MESSAGE_RECEIVED_FROM_SENDER
            else if (HikePubSub.MESSAGE_RECEIVED_FROM_SENDER == type)  // represents event when a client receive msg from other client through server.
            {
                ConvMessage convMessage = (ConvMessage)obj;
                convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                MessagesTableUtils.addChatMessage(convMessage);
                logger.Info("DBCONVERSATION LISTENER", "Receiver received Message : " + convMessage.Message + " ; Receiver Msg ID : " + convMessage.MessageId + "	; Mapped msgID : " + convMessage.MappedMessageId);
                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED, convMessage);
            }
            #endregion
            #region MESSAGE_RECEIVED_READ
            else if (HikePubSub.MESSAGE_RECEIVED_READ == type)  // represents event when a msg is read by this user
            {
                long[] ids = (long[])obj;
                updateDbBatch(ids, (int)ConvMessage.State.RECEIVED_READ);
            }
            #endregion
            #region SERVER_RECEIVED_MSG
            else if (HikePubSub.SERVER_RECEIVED_MSG == type)  // server got msg from client 1 and sent back received msg receipt
            {
                logger.Info("DBCONVERSATION LISTENER", "(Sender) Message sent confirmed for msgID -> " + (long)obj);
                updateDB(obj, (int)ConvMessage.State.SENT_CONFIRMED);
            }
            #endregion
            #region MESSAGE_DELIVERED
            else if (HikePubSub.MESSAGE_DELIVERED == type)  // server got msg from client 1 and sent back received msg receipt
            {
                logger.Info("DBCONVERSATION LISTENER", "Msg delivered to receiver for msgID -> " + (long)obj);
                updateDB(obj, (int)ConvMessage.State.SENT_DELIVERED);
            }
            #endregion
            #region MESSAGE_DELIVERED_READ
            else if (HikePubSub.MESSAGE_DELIVERED_READ == type)  // server got msg from client 1 and sent back received msg receipt
            {
                long[] ids = (long[])obj;
                logger.Info("DBCONVERSATION LISTENER", "Message delivered read for ids " + ids);
                updateDbBatch(ids, (int)ConvMessage.State.SENT_DELIVERED_READ);
            }
            #endregion
            #region MESSAGE_DELETED
            else if (HikePubSub.MESSAGE_DELETED == type)
            {
                long msgId = (long)obj;
                MessagesTableUtils.deleteMessage(msgId);
                // TODO :: persistence.removeMessage(msgId);
            }
            #endregion
            #region MESSAGE_FAILED
            else if (HikePubSub.MESSAGE_FAILED == type)  // server got msg from client 1 and sent back received msg receipt
            {
                updateDB(obj, (int)ConvMessage.State.SENT_FAILED);
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
            #region SMS_CREDIT_CHANGED
            else if (HikePubSub.SMS_CREDIT_CHANGED == type)  // server got msg from client 1 and sent back received msg receipt
            {
                /* Save credits to isolated storage */
                int credits = (int)obj;
                App.appSettings[App.SMS_SETTING] = credits;
                App.appSettings.Save();
            }
            #endregion
            #region USER_JOINED/LEFT
            else if (HikePubSub.USER_JOINED == type || HikePubSub.USER_LEFT == type)
            {
                string msisdn = (string)obj;
                bool joined = HikePubSub.USER_JOINED == type;
                UsersTableUtils.updateOnHikeStatus(msisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(msisdn, joined);
            }
            #endregion
            #region ICON_CHANGED
            else if (HikePubSub.ICON_CHANGED == type)
            {
                object[] data = (object[])obj;
                string msisdn = (string)data[0];
                byte[] imageBytes = (byte[])data[1];
                MiscDBUtil.addOrUpdateIcon(msisdn, imageBytes);
                ImageConverter.updateImageInCache(msisdn, imageBytes);
                mPubSub.publish(HikePubSub.UPDATE_UI, msisdn);
            }
            #endregion 
            #region ADD_OR_UPDATE_PROFILE
            else if (HikePubSub.ADD_OR_UPDATE_PROFILE == type)
            { 
                object[] vals = (object[])obj;
                string msisdn = (string)vals[0];
                MemoryStream msSmallImage = (MemoryStream)vals[1];
                MemoryStream msLargeImage = (MemoryStream)vals[2];
                MiscDBUtil.addOrUpdateProfileIcon(msisdn, msSmallImage.ToArray());
                MiscDBUtil.addOrUpdateProfileIcon(msisdn + "::large", msLargeImage.ToArray());
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

        private void updateDB(object obj, int status)
        {
            long msgID = (long)obj;
            MessagesTableUtils.updateMsgStatus(msgID, status);
        }

        private void updateDbBatch(long[] ids, int status)
        {
            MessagesTableUtils.updateAllMsgStatus(ids, status);
        }
    }
}
