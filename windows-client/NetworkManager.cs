using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using windows_client.View;

namespace windows_client
{
    public class NetworkManager
    {
        /* message read by recipient */
        public static readonly string MESSAGE_READ = "mr";

        public static readonly string MESSAGE = "m";

        public static readonly string SMS_CREDITS = "sc";

        public static readonly string DELIVERY_REPORT = "dr";

        public static readonly string SERVER_REPORT = "sr";

        public static readonly string USER_JOINED = "uj";

        public static readonly string USER_LEFT = "ul";

        public static readonly string START_TYPING = "st";

        public static readonly string END_TYPING = "et";

        public static readonly string INVITE_INFO = "ii";

        public static readonly string INVITE = "i";

        public static readonly string ICON = "ic";

        public static bool turnOffNetworkManager = true;

        private HikePubSub pubSub;

        private static volatile NetworkManager instance;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton

        private NetworkManager()
        {
            pubSub = App.HikePubSubInstance;
        }

        public static NetworkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new NetworkManager();
                    }
                }

                return instance;
            }
        }

        public void onMessage(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            while (turnOffNetworkManager)
            {
                Thread.Sleep(500);
            }
            JObject jsonObj = null;
            try
            {
                jsonObj = JObject.Parse(msg);
            }
            catch (JsonReaderException e)
            {
                //logger.Info("WebSocketPublisher", "Invalid JSON message: " + msg +", Exception : "+e);
                return;
            }
            string type = (string)jsonObj[HikeConstants.TYPE];
            string msisdn = (string)jsonObj[HikeConstants.FROM];

            if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    ConvMessage convMessage = new ConvMessage(jsonObj);
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(convMessage,false);
                    object[] vals = new object[2];
                    vals[0] = convMessage;
                    vals[1] = obj;
                    pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                }
                catch (Exception e)
                {
                    //logger.Info("NETWORK MANAGER", "Invalid JSON", e);
                }
            }
            else if (START_TYPING == type) /* Start Typing event received*/
            {
                this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, msisdn);
            }
            else if (END_TYPING == type) /* End Typing event received */
            {
                this.pubSub.publish(HikePubSub.END_TYPING_CONVERSATION, msisdn);
            }
            else if (SMS_CREDITS == type) /* SMS CREDITS */
            {
                int sms_credits = Int32.Parse((string)jsonObj[HikeConstants.DATA]);
                App.appSettings[App.SMS_SETTING] = sms_credits;
                App.appSettings.Save();
                this.pubSub.publish(HikePubSub.SMS_CREDIT_CHANGED, sms_credits);
            }
            else if (SERVER_REPORT == type) /* Represents Server has received the msg you sent */
            {
                string id = (string)jsonObj[HikeConstants.DATA];
                long msgID;
                try
                {
                    msgID = long.Parse(id);
                }
                catch (FormatException e)
                {
                    //logger.Info("NETWORK MANAGER", "Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                }
                updateDB(msgID, (int)ConvMessage.State.SENT_CONFIRMED);
                this.pubSub.publish(HikePubSub.SERVER_RECEIVED_MSG, msgID);
            }
            else if (DELIVERY_REPORT == type) // this handles the case when msg with msgId is recieved by the recipient but is unread
            {
                string id = (string)jsonObj[HikeConstants.DATA];
                long msgID;
                try
                {
                    msgID = Int64.Parse(id);
                }
                catch (FormatException e)
                {
                    //logger.Info("NETWORK MANAGER", "Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                }
                //logger.Info("NETWORK MANAGER", "Delivery report received for msgid : " + msgID + "	;	REPORT : DELIVERED");
                updateDB(msgID, (int)ConvMessage.State.SENT_DELIVERED);
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED, msgID);
            }
            else if (MESSAGE_READ == type) // Message read by recipient
            {
                JArray msgIds = (JArray)jsonObj["d"];
                if (msgIds == null)
                {
                    //logger.Info("NETWORK MANAGER", "Update Error : Message id Array is empty or null . Check problem");
                    return;
                }

                long[] ids = new long[msgIds.Count];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = Int64.Parse(msgIds[i].ToString());
                }
                //logger.Info("NETWORK MANAGER", "Delivery report received : " + "	;	REPORT : DELIVERED READ");
                updateDbBatch(ids, (int)ConvMessage.State.SENT_DELIVERED_READ);
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED_READ, ids);
            }
            else if ((USER_JOINED == type) || (USER_LEFT == type))
            {
                string uMsisdn = (string)jsonObj[HikeConstants.DATA];
                bool joined = USER_JOINED == type;
                UsersTableUtils.updateOnHikeStatus(uMsisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(uMsisdn, joined);
                this.pubSub.publish(joined ? HikePubSub.USER_JOINED : HikePubSub.USER_LEFT, uMsisdn);
            }
            else if (ICON == type)
            {
                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                string iconBase64 = temp.ToString();
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);

                MiscDBUtil.addOrUpdateIcon(msisdn, imageBytes);
                ConversationTableUtils.updateImage(msisdn, imageBytes);
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.UI_UtilsInstance.updateImageInCache(msisdn, imageBytes);
                });
                this.pubSub.publish(HikePubSub.UPDATE_UI, msisdn);
            }
            else if (INVITE_INFO == type)
            {
                JObject data;
                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                data = temp.ToObject<JObject>();
                int invited = (int)data[HikeConstants.ALL_INVITEE];
                int invited_joined = (int)data[HikeConstants.ALL_INVITEE_JOINED];
                String totalCreditsPerMonth = (string)data[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                App.appSettings[App.INVITED] = invited;
                App.appSettings[App.INVITED_JOINED] = invited_joined;

                if (!String.IsNullOrEmpty(totalCreditsPerMonth) && Int32.Parse(totalCreditsPerMonth) > 0)
                {
                    App.appSettings[App.TOTAL_CREDITS_PER_MONTH] = totalCreditsPerMonth;
                }
                App.appSettings.Save();
                this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
            }

            #region GROUP CHAT RELATED

            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                JArray arr = (JArray)jsonObj[HikeConstants.DATA];
                if (arr == null || !arr.HasValues)
                {
                    return;
                }
                ConvMessage convMessage = new ConvMessage(jsonObj, false);
                convMessage.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                ConversationListObject obj = MessagesTableUtils.addGroupChatMessage(convMessage, jsonObj);
                if (obj == null)
                    return;
                Debug.WriteLine("NetworkManager", "Group is new");

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = obj;

                this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
            }
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_NAME == type) //Group chat name change
            {
                string groupName = (string)jsonObj[HikeConstants.DATA];
                string groupId = (string)jsonObj[HikeConstants.TO];

                object[] vals = new object[2];
                vals[0] = groupId;
                vals[1] = groupName;
                ConversationTableUtils.updateGroupName(groupId, groupName);
                bool goAhead = GroupTableUtils.updateGroupName(groupId, groupName);
                if (goAhead)
                    this.pubSub.publish(HikePubSub.GROUP_NAME_CHANGED, vals);

            }
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE == type) //Group chat leave
            {
                /*
                     * 1. Update Conversation list name if groupName is not set.
                     * 2. Update DB.
                     * 3. Notify GroupInfo page (if opened)
                     * 4. Notify Chat Thread page if opened.
                     */

                string groupId = (string)jsonObj[HikeConstants.TO];
                string fromMsisdn = (string)jsonObj[HikeConstants.FROM];

                ConvMessage convMsg = new ConvMessage(jsonObj, false);
                ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMsg,false);
                GroupTableUtils.removeParticipantFromGroup(groupId, fromMsisdn);
                GroupInfo gi = GroupTableUtils.getGroupInfoForId(groupId);
                if (gi == null)
                    return;
                if (string.IsNullOrEmpty(gi.GroupName)) // no group name is set
                {
                    List<GroupMembers> existingMembers = GroupTableUtils.getActiveGroupMembers(groupId);
                    cObj.ContactName = Utils.defaultGroupName(existingMembers);
                }

               
                object[] vals = new object[2];
                vals[0] = convMsg;
                vals[1] = cObj;
                this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                this.pubSub.publish(HikePubSub.PARTICIPANT_LEFT_GROUP, jsonObj);
            }

            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_END == type) //Group chat end
            {
                string groupId = (string)jsonObj[HikeConstants.TO];
                bool goAhead = GroupTableUtils.SetGroupDead(groupId);
                if (goAhead)
                {
                    ConvMessage convMessage = new ConvMessage(jsonObj, false);
                    ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMessage,false);
                    if (cObj == null)
                        return;
                    object[] vals = new object[2];
                    vals[0] = convMessage;
                    vals[1] = cObj;
                    this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                    this.pubSub.publish(HikePubSub.GROUP_END, groupId);
                }
            }
            #endregion

            else if (HikeConstants.MqttMessageTypes.ACCOUNT_INFO == type)
            {
                //TODO
            }
            else
            {
                //logger.Info("WebSocketPublisher", "Unknown Type:" + type);
            }
        }

        private void updateDB(long msgID, int status)
        {
            MessagesTableUtils.updateMsgStatus(msgID, status);
        }

        private void updateDbBatch(long[] ids, int status)
        {
            MessagesTableUtils.updateAllMsgStatus(ids, status);
        }
    }
}
