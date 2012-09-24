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
using System.IO.IsolatedStorage;
using System.Windows.Resources;
using System.IO;
using windows_client.View;
using Microsoft.Phone.Shell;

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

        private static long totalTime = 0;
        private static int numberOfImages = 0;

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

            #region MESSAGE
            if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    ConvMessage convMessage = new ConvMessage(jsonObj);
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(convMessage, false);
                    
                    if (obj == null)
                        return;
                    if (convMessage.FileAttachment != null)
                    {
                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    }
                    object[] vals = null;

                    if (obj.IsFirstMsg) // case when grp is created and you have to show invited etc msg
                    {
                        vals = new object[3];
                        JObject oj = ConvMessage.ProcessGCLogic(obj.Msisdn);
                        ConvMessage cm = new ConvMessage(oj, true);
                        MessagesTableUtils.addChatMessage(cm, false);
                        obj.IsFirstMsg = false;
                        ConversationTableUtils.updateConversation(obj);
                        vals[2] = cm;
                    }
                    else
                        vals = new object[2];

                    vals[0] = convMessage;
                    vals[1] = obj;

                    pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                }
                catch (Exception e)
                {
                    //logger.Info("NETWORK MANAGER", "Invalid JSON", e);
                }
            }
            #endregion
            #region START_TYPING
            else if (START_TYPING == type) /* Start Typing event received*/
            {
                this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, msisdn);
            }
            #endregion
            #region END_TYPING
            else if (END_TYPING == type) /* End Typing event received */
            {
                this.pubSub.publish(HikePubSub.END_TYPING_CONVERSATION, msisdn);
            }
            #endregion
            #region SMS_CREDITS
            else if (SMS_CREDITS == type) /* SMS CREDITS */
            {
                int sms_credits = Int32.Parse((string)jsonObj[HikeConstants.DATA]);
                App.WriteToIsoStorageSettings(App.SMS_SETTING, sms_credits);
                this.pubSub.publish(HikePubSub.SMS_CREDIT_CHANGED, sms_credits);
            }
            #endregion
            #region SERVER_REPORT
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
                this.pubSub.publish(HikePubSub.SERVER_RECEIVED_MSG, msgID);
                updateDB(msgID, (int)ConvMessage.State.SENT_CONFIRMED);
            }
            #endregion
            #region DELIVERY_REPORT
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
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED, msgID);
                updateDB(msgID, (int)ConvMessage.State.SENT_DELIVERED);
            }
            #endregion
            #region MESSAGE_READ
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
            #endregion
            #region USER_JOINED USER_LEFT
            else if ((USER_JOINED == type) || (USER_LEFT == type))
            {
                JObject o = (JObject)jsonObj[HikeConstants.DATA];
                string uMsisdn = (string)o[HikeConstants.MSISDN];
                bool joined = USER_JOINED == type;
                if (joined)
                {
                    // if user is in contact list then only show the joined msg
                    bool isUserInContactList = UsersTableUtils.getContactInfoFromMSISDN(uMsisdn) != null ? true : false;
                    if (isUserInContactList)
                        ProcessUoUjMsgs(jsonObj, false);
                }
                UsersTableUtils.updateOnHikeStatus(uMsisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(uMsisdn, joined);
                this.pubSub.publish(joined ? HikePubSub.USER_JOINED : HikePubSub.USER_LEFT, uMsisdn);
            }
            #endregion
            #region ICON
            else if (ICON == type)
            {
                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                string iconBase64 = temp.ToString();
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                object[] vals = new object[2];
                vals[0] = msisdn;
                vals[1] = imageBytes;

                this.pubSub.publish(HikePubSub.UPDATE_UI, vals);
                Stopwatch st = Stopwatch.StartNew();
                if (Utils.isGroupConversation(msisdn))
                {
                    // ':' is not supported in Isolated Storage so replacing it with '_'
                    string grpId = msisdn.Replace(":", "_");
                    MiscDBUtil.saveAvatarImage(grpId, imageBytes);
                }
                else
                    MiscDBUtil.saveAvatarImage(msisdn, imageBytes);
                st.Stop();
                long msec = st.ElapsedMilliseconds;
                Debug.WriteLine("Time to save image for msisdn {0} : {1}", msisdn, msec);
            }
            #endregion
            #region INVITE_INFO
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
                App.WriteToIsoStorageSettings(App.INVITED,invited);
                App.WriteToIsoStorageSettings(App.INVITED_JOINED, invited_joined);

                if (!String.IsNullOrEmpty(totalCreditsPerMonth) && Int32.Parse(totalCreditsPerMonth) > 0)
                {
                    App.WriteToIsoStorageSettings(App.TOTAL_CREDITS_PER_MONTH, totalCreditsPerMonth);
                }
                this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
            }
            #endregion
            #region GROUP CHAT RELATED

            #region GROUP_CHAT_JOIN
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                JArray arr = (JArray)jsonObj[HikeConstants.DATA];
                if (arr == null || !arr.HasValues)
                    return;

                string grpId = jsonObj[HikeConstants.TO].ToString();

                if (!AddGroupmembers(arr, grpId)) // is gcj to add new members or to give DND info
                    return;

                ConvMessage convMessage = new ConvMessage(jsonObj, false);
                // till here Group Cache is already made.
                convMessage.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                ConversationListObject obj = MessagesTableUtils.addGroupChatMessage(convMessage, jsonObj);
                if (obj == null)
                    return;
                Debug.WriteLine("NetworkManager", "Group is new");

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = obj;

                this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                this.pubSub.publish(HikePubSub.PARTICIPANT_JOINED_GROUP, jsonObj);
            }
            #endregion
            #region GROUP_CHAT_NAME
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_NAME == type) //Group chat name change
            {
                string groupName = (string)jsonObj[HikeConstants.DATA];
                string groupId = (string)jsonObj[HikeConstants.TO];

                bool groupExist = ConversationTableUtils.updateGroupName(groupId, groupName);
                if (!groupExist)
                    return;
                object[] vals = new object[2];
                vals[0] = groupId;
                vals[1] = groupName;

                bool goAhead = GroupTableUtils.updateGroupName(groupId, groupName);
                if (goAhead)
                    this.pubSub.publish(HikePubSub.GROUP_NAME_CHANGED, vals);

            }
            #endregion
            #region GROUP_CHAT_LEAVE
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_LEAVE == type) //Group chat leave
            {
                /*
                * 1. Update Conversation list name if groupName is not set.
                * 2. Update DB.
                * 3. Notify GroupInfo page (if opened)
                * 4. Notify Chat Thread page if opened.
                */

                string groupId = (string)jsonObj[HikeConstants.TO];
                string fromMsisdn = (string)jsonObj[HikeConstants.DATA];
                GroupParticipant gp = Utils.getGroupParticipant(null, fromMsisdn, groupId);
                if (gp.HasLeft)
                    return;
                
                ConvMessage convMsg = new ConvMessage(jsonObj, false);
                ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMsg, false);
                if (cObj == null)
                    return;
                
                object[] vals = new object[2];
                vals[0] = convMsg;
                vals[1] = cObj;
                this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                this.pubSub.publish(HikePubSub.PARTICIPANT_LEFT_GROUP, convMsg);
            }
            #endregion
            #region GROUP_CHAT_END
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_END == type) //Group chat end
            {
                string groupId = (string)jsonObj[HikeConstants.TO];
                bool goAhead = GroupTableUtils.SetGroupDead(groupId);
                if (goAhead)
                {
                    ConvMessage convMessage = new ConvMessage(jsonObj, false);
                    ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMessage, false);
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

            #endregion
            #region ACCOUNT_INFO
            else if (HikeConstants.MqttMessageTypes.ACCOUNT_INFO == type)
            {
                JObject data = (JObject)jsonObj[HikeConstants.DATA];
                KeyValuePair<string, JToken> kv;
                IEnumerator<KeyValuePair<string, JToken>> keyVals = data.GetEnumerator();
                while (keyVals.MoveNext())
                {
                    kv = keyVals.Current;
                    string val = kv.Value.ToObject<string>();
                    App.WriteToIsoStorageSettings(kv.Key, val);
                }

                JToken it = data[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                if (it != null)
                {
                    this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
                }
            }
            #endregion
            #region USER_OPT_IN
            else if (HikeConstants.MqttMessageTypes.USER_OPT_IN == type)
            {
                // {"t":"uo", "d":{"msisdn":"", "credits":10}}
                ProcessUoUjMsgs(jsonObj, true);
            }
            #endregion
            #region OTHER
            else
            {
                //logger.Info("WebSocketPublisher", "Unknown Type:" + type);
            }
            #endregion
        }

        private void ProcessUoUjMsgs(JObject jsonObj, bool isOptInMsg)
        {
            int credits = 0;
           
            string ms = null;
            try
            {
                JObject data = (JObject)jsonObj[HikeConstants.DATA];
                ms = (string)data[HikeConstants.MSISDN];
                try
                {
                    credits = (int)data["credits"];
                }
                catch
                {
                    credits = 0;
                }
            }
            catch (Exception e)
            {
                ms = null;
            }
            if(ms == null)
                return;
            /* Process UO for 1-1 chat*/
            if (!ConversationsList.ConvMap.ContainsKey(ms))
                return;

            object[] vals = null;
            ConvMessage cm = new ConvMessage();
            cm.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
            cm.Timestamp = TimeUtils.getCurrentTimeStamp();
            cm.Msisdn = ms;
            cm.MessageId = -1;
            cm.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
            if(isOptInMsg)
                cm.GrpParticipantState = ConvMessage.ParticipantInfoState.USER_OPT_IN;
            else
                cm.GrpParticipantState = ConvMessage.ParticipantInfoState.USER_JOINED;
            ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);

            if (credits <= 0)
                vals = new object[2];
            else                    // this shows that we have to show credits msg as this user got credits.
            {
                string text = string.Format(HikeConstants.CREDITS_EARNED, credits);
                JObject o = new JObject();
                o.Add("t", "credits_gained");
                ConvMessage cmCredits = new ConvMessage();
                cmCredits.MetaDataString = o.ToString(Newtonsoft.Json.Formatting.None);
                cmCredits.Message = text;
                cmCredits.Timestamp = TimeUtils.getCurrentTimeStamp();
                cmCredits.Msisdn = ms;
                cmCredits.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                cmCredits.GrpParticipantState = ConvMessage.ParticipantInfoState.CREDITS_GAINED;
                obj = MessagesTableUtils.addChatMessage(cmCredits, false);

                vals = new object[3];
                vals[2] = cmCredits;
            }

            vals[0] = cm;
            vals[1] = obj;
            pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);

            // UPDATE group cache
            foreach (string key in Utils.GroupCache.Keys)
            {
                List<GroupParticipant> l = Utils.GroupCache[key];
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].Msisdn == ms) // if this msisdn exists in group
                    {
                        object[] values = null;
                        ConvMessage convMsg = new ConvMessage();
                        convMsg.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                        convMsg.Timestamp = TimeUtils.getCurrentTimeStamp();
                        convMsg.MessageId = -1;
                        convMsg.Msisdn = key;
                        convMsg.Message = ms;
                        convMsg.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                        convMsg.GrpParticipantState = ConvMessage.ParticipantInfoState.USER_OPT_IN;
                        ConversationListObject co = MessagesTableUtils.addChatMessage(convMsg, false);

                        if (credits > 0)                    // this shows that we have to show credits msg as this user got credits.
                        {
                            string text = string.Format(HikeConstants.CREDITS_EARNED, credits);
                            JObject o = new JObject();
                            o.Add("t", "credits_gained");
                            ConvMessage cmCredits = new ConvMessage();
                            cmCredits.MetaDataString = o.ToString(Newtonsoft.Json.Formatting.None);
                            cmCredits.MessageId = -1;
                            cmCredits.Message = text;
                            cmCredits.Timestamp = TimeUtils.getCurrentTimeStamp();
                            cmCredits.Msisdn = key;
                            cmCredits.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                            cmCredits.GrpParticipantState = ConvMessage.ParticipantInfoState.CREDITS_GAINED;
                            co = MessagesTableUtils.addChatMessage(cmCredits, false);
                            values = new object[3];
                            values[2] = cmCredits;
                        }
                        else
                            values = new object[2];

                        values[0] = convMsg;
                        values[1] = co;
                        pubSub.publish(HikePubSub.MESSAGE_RECEIVED, values);
                        l[i].HasOptIn = true;
                        break;
                    }
                }
            }
            App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
        }
        /// <summary>
        /// This function will return 
        ///  -- > true , if new users are added to GC
        ///  -- > false , if GCJ is come to notify DND status
        /// 
        /// </summary>
        /// <param name="arr"></param>
        /// <param name="grpId"></param>
        /// <returns></returns>
        private bool AddGroupmembers(JArray arr, string grpId)
        {
            if (ConversationsList.ConvMap.ContainsKey(grpId))
            {
                List<GroupParticipant> l = null;
                Utils.GroupCache.TryGetValue(grpId, out l);
                if (l == null)
                    return true;
                bool saveCache = false;
                bool output = true;
                for (int i = 0; i < arr.Count; i++)
                {
                    JObject o = (JObject)arr[i];
                    bool onhike = (bool)o["onhike"];
                    bool dnd = (bool)o["dnd"];
                    string ms = (string)o["msisdn"];
                    for (int k = 0; k < l.Count; k++)
                    {
                        if (l[k].Msisdn == ms)
                        {
                            if (l[k].HasLeft)
                            {
                                l[k].HasLeft = false;
                                App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
                                return true;
                            }
                            l[k].IsDND = dnd;
                            l[k].IsOnHike = onhike;
                            output = false;
                            break;
                        }
                    }
                }
                if (!output)
                    App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
                return output;
            }
            else
                return true;

        }

        private void updateDB(long msgID, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateMsgStatus(msgID, status); // update covmsg
            ConversationTableUtils.updateLastMsgStatus(msisdn,status); // update conversationObj
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED : {0}", msec);
        }

        private void updateDbBatch(long[] ids, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateAllMsgStatus(ids, status);
            ConversationTableUtils.updateLastMsgStatus(msisdn,status);
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED READ : {0}", msec);
        }
    }
}
