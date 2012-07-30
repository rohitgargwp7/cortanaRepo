using System.IO;
using Newtonsoft.Json.Linq;
using windows_client.Model;
using System.Windows;
using System.Windows.Navigation;
using System;

namespace windows_client.DbUtils
{
    public class DbConversationListener : HikePubSub.Listener
    {
        private HikePubSub mPubSub;

        /* Register all the listeners*/
        public DbConversationListener()
        {
            mPubSub = App.HikePubSubInstance;
            //registerListeners();
        }

        public void registerListeners()
        {
            mPubSub.addListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.addListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.addListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.addListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.addListener(HikePubSub.BLOCK_USER, this);
            mPubSub.addListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.addListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.addListener(HikePubSub.DELETE_ACCOUNT, this);
        }

        private void removeListeners()
        {
            mPubSub.removeListener(HikePubSub.MESSAGE_SENT, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_RECEIVED_READ, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_DELETED, this);
            mPubSub.removeListener(HikePubSub.MESSAGE_FAILED, this);
            mPubSub.removeListener(HikePubSub.BLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.UNBLOCK_USER, this);
            mPubSub.removeListener(HikePubSub.ADD_OR_UPDATE_PROFILE, this);
            mPubSub.removeListener(HikePubSub.DELETE_ACCOUNT, this);
        }

        public void onEventReceived(string type, object obj)
        {

            #region MESSAGE_SENT
            if (HikePubSub.MESSAGE_SENT == type)
            {
                ConvMessage convMessage = (ConvMessage)obj;
                convMessage.MessageStatus = ConvMessage.State.SENT_UNCONFIRMED;
                ConversationListObject convObj = MessagesTableUtils.addChatMessage(convMessage);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if(App.ViewModel.MessageListPageCollection.Contains(convObj))
                    {
                        App.ViewModel.MessageListPageCollection.Remove(convObj);
                    }
                    App.ViewModel.MessageListPageCollection.Insert(0, convObj);
                });
                mPubSub.publish(HikePubSub.MQTT_PUBLISH, convMessage.serialize(true));
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

                bool delConv = (bool)o[2];
                if (delConv)
                {
                    string msisdn = (string)o[1];
                    // delete the conversation from DB.
                    ConversationTableUtils.deleteConversation(msisdn);
                }

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
            #region DELETE ACCOUNT
            else if (HikePubSub.DELETE_ACCOUNT == type)
            {
                removeListeners();
                MiscDBUtil.clearDatabase();
                mPubSub.publish(HikePubSub.ACCOUNT_DELETED, null);
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
