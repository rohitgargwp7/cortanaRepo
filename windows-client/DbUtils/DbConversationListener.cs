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
            mPubSub.addListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.addListener(HikePubSub.SMS_CREDIT_CHANGED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_FROM_SENDER, this);
            mPubSub.addListener(HikePubSub.SERVER_RECEIVED_MSG, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELIVERED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
        }

        public void onEventReceived(string type, object obj)
        {
            if (HikePubSub.MESSAGE_SENT == type)
            {
                object[] vals = (object[])obj;
                ConvMessage convMessage = (ConvMessage)vals[0];
                bool isNewConv = (bool)vals[1];
                MessagesTableUtils.addChatMessage(convMessage,isNewConv);
                logger.Info("DBCONVERSATION LISTENER", "Sending Message : " + convMessage.Message + " ; to : " + convMessage.Msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize());
            }
            else if (HikePubSub.MESSAGE_RECEIVED_FROM_SENDER == type)  // represents event when a client receive msg from other client through server.
            {
                ConvMessage convMessage = (ConvMessage)obj;
                MessagesTableUtils.addChatMessage(convMessage);
                logger.Info("DBCONVERSATION LISTENER", "Receiver received Message : " + convMessage.Message + " ; Receiver Msg ID : " + convMessage.MessageId + "	; Mapped msgID : " + convMessage.MappedMessageId);
                mPubSub.publish(HikePubSub.MESSAGE_RECEIVED,convMessage);
            }
            else if (HikePubSub.SERVER_RECEIVED_MSG == type)  // server got msg from client 1 and sent back received msg receipt
            {
                logger.Info("DBCONVERSATION LISTENER", "(Sender) Message sent confirmed for msgID -> " + (long)obj);
                updateDB(obj, (int)ConvMessage.State.SENT_CONFIRMED);
            }
            else if (HikePubSub.MESSAGE_DELIVERED == type)  // server got msg from client 1 and sent back received msg receipt
            {
                logger.Info("DBCONVERSATION LISTENER", "Msg delivered to receiver for msgID -> " + (long)obj);
                updateDB(obj, (int)ConvMessage.State.SENT_DELIVERED);
            }
            else if (HikePubSub.MESSAGE_DELIVERED_READ == type)  // server got msg from client 1 and sent back received msg receipt
            {
                long[] ids = (long[])obj;
                logger.Info("DBCONVERSATION LISTENER", "Message delivered read for ids " + ids);
                //TODO :: updateDbBatch(ids, (int)ConvMessage.State.SENT_DELIVERED_READ);
            }
            else if (HikePubSub.MESSAGE_DELETED == type)
            {
                long msgId = (long)obj;
                ConversationTableUtils.deleteMessage(msgId);
                // TODO :: persistence.removeMessage(msgId);
            }
            else if (HikePubSub.MESSAGE_FAILED == type)  // server got msg from client 1 and sent back received msg receipt
            {
                updateDB(obj, (int)ConvMessage.State.SENT_FAILED);
            }
            else if (HikePubSub.BLOCK_USER == type)
            {
                string msisdn = (string)obj;
                UsersTableUtils.block(msisdn);
                JObject blockObj = blockUnblockSerialize("b", msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, blockObj);
            }
            else if (HikePubSub.UNBLOCK_USER == type)
            {
                string msisdn = (string)obj;
                UsersTableUtils.unblock(msisdn);
                JObject unblockObj = blockUnblockSerialize("ub", msisdn);
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, unblockObj);
            }
        }

        private JObject blockUnblockSerialize(string type, string msisdn)
        {
            JObject obj = new JObject();
            obj[HikeConstants.TYPE] =  type;
            obj[HikeConstants.DATA] = msisdn;           
            return obj;
        }

        private void updateDB(object obj, int status)
        {
            long msgID = (long)obj;
            /* TODO we should lookup the convid for this user, since otherwise one could set mess with the state for other conversations */
            MessagesTableUtils.updateMsgStatus(msgID, status);
        }
    }
}
