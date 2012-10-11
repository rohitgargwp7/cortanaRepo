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
            string type = null;
            try
            {
                type = (string)jsonObj[HikeConstants.TYPE];
            }
            catch
            {
                return;
            }
            string msisdn = null;
            try
            {
                msisdn = (string)jsonObj[HikeConstants.FROM];
            }
            catch (Exception e)
            {
            }

            #region MESSAGE
            if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    ConvMessage convMessage = null;
                    try
                    {
                        convMessage = new ConvMessage(jsonObj);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception in parsing json : " + e.StackTrace);
                        return;
                    }
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
                        JObject oj = ConvMessage.ProcessGCLogic(obj.Msisdn);
                        if (oj != null)
                        {
                            ConvMessage cm;
                            try
                            {
                                cm = new ConvMessage(oj, true);
                                vals = new object[3];
                                vals[2] = cm;
                                MessagesTableUtils.addChatMessage(cm, false);
                            }
                            catch (Exception e)
                            {
                                vals = new object[2];
                                cm = null;
                                Debug.WriteLine("NETWORK MANAGER :: Problem while parsing json and creating ConvMessage object.");
                            }
                        }
                        else
                        {
                            vals = new object[2];
                        }
                        obj.IsFirstMsg = false;
                        ConversationTableUtils.updateConversation(obj);
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
                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, msisdn);
                return;
            }
            #endregion
            #region END_TYPING
            else if (END_TYPING == type) /* End Typing event received */
            {
                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.END_TYPING_CONVERSATION, msisdn);
                return;
            }
            #endregion
            #region SMS_CREDITS
            else if (SMS_CREDITS == type) /* SMS CREDITS */
            {
                try
                {
                    int sms_credits = Int32.Parse((string)jsonObj[HikeConstants.DATA]);
                    App.WriteToIsoStorageSettings(App.SMS_SETTING, sms_credits);
                    this.pubSub.publish(HikePubSub.SMS_CREDIT_CHANGED, sms_credits);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing sms_credits : " + e.StackTrace);
                }
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
                    Debug.WriteLine("NETWORK MANAGER:: Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                    return;
                }
                this.pubSub.publish(HikePubSub.SERVER_RECEIVED_MSG, msgID);
                updateDB(null, msgID, (int)ConvMessage.State.SENT_CONFIRMED);
            }
            #endregion
            #region DELIVERY_REPORT
            else if (DELIVERY_REPORT == type) // this handles the case when msg with msgId is recieved by the recipient but is unread
            {
                string id = (string)jsonObj[HikeConstants.DATA];
                JToken msisdnToken = null;
                string msisdnToCheck = null;
                long msgID;
                try
                {
                    msgID = Int64.Parse(id);
                    jsonObj.TryGetValue(HikeConstants.TO, out msisdnToken);
                    if (msisdnToken != null)
                        msisdnToCheck = msisdnToken.ToString();
                    else
                        msisdnToCheck = msisdn;
                }
                catch (FormatException e)
                {
                    Debug.WriteLine("NETWORK MANAGER:: Exception occured while parsing msgId. Exception : " + e);
                    msgID = -1;
                    return;
                }

                object[] vals = new object[2];
                vals[0] = msgID;
                vals[1] = msisdnToCheck;
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED, vals);
                updateDB(msisdnToCheck, msgID, (int)ConvMessage.State.SENT_DELIVERED);
            }
            #endregion
            #region MESSAGE_READ
            else if (MESSAGE_READ == type) // Message read by recipient
            {
                JArray msgIds = null;
                JToken msisdnToken = null;
                string msisdnToCheck = null;
                
                try
                {
                    msgIds = (JArray)jsonObj["d"];
                    jsonObj.TryGetValue(HikeConstants.TO, out msisdnToken);
                    if (msisdnToken != null)
                        msisdnToCheck = msisdnToken.ToString();
                    else
                        msisdnToCheck = msisdn;
                }
                catch
                {
                    return;
                }
                if (msgIds == null)
                {
                    Debug.WriteLine("NETWORK MANAGER", "Update Error : Message id Array is empty or null . Check problem");
                    return;
                }

                long[] ids = new long[msgIds.Count];
                for (int i = 0; i < ids.Length; i++)
                {
                    ids[i] = Int64.Parse(msgIds[i].ToString());
                }
                object[] vals = new object[2];
                vals[0] = ids;
                vals[1] = msisdnToCheck;
                updateDbBatch(msisdnToCheck, ids, (int)ConvMessage.State.SENT_DELIVERED_READ);
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED_READ, vals);
            }
            #endregion
            #region USER_JOINED USER_LEFT
            else if ((USER_JOINED == type) || (USER_LEFT == type))
            {
                JObject o = null;
                string uMsisdn = null;
                try
                {
                    o = (JObject)jsonObj[HikeConstants.DATA];
                    uMsisdn = (string)o[HikeConstants.MSISDN];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing UJ/UL Json : " + e.StackTrace);
                    return;
                }
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
                try
                {
                    int invited = (int)data[HikeConstants.ALL_INVITEE];
                    App.WriteToIsoStorageSettings(App.INVITED, invited);
                }
                catch
                {
                }
                try
                {
                    int invited_joined = (int)data[HikeConstants.ALL_INVITEE_JOINED];
                    App.WriteToIsoStorageSettings(App.INVITED_JOINED, invited_joined);
                }
                catch
                {
                }
                string totalCreditsPerMonth = null;
                try
                {
                    totalCreditsPerMonth = (string)data[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                }
                catch { }

                if (!String.IsNullOrEmpty(totalCreditsPerMonth) && Int32.Parse(totalCreditsPerMonth) > 0)
                {
                    App.WriteToIsoStorageSettings(HikeConstants.TOTAL_CREDITS_PER_MONTH, totalCreditsPerMonth);
                    this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
                }

            }
            #endregion
            #region ACCOUNT_INFO
            else if (HikeConstants.MqttMessageTypes.ACCOUNT_INFO == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    Debug.WriteLine("NETWORK MANAGER : Received account info json : {0}",jsonObj.ToString());
                    KeyValuePair<string, JToken> kv;
                    IEnumerator<KeyValuePair<string, JToken>> keyVals = data.GetEnumerator();
                    while (keyVals.MoveNext())
                    {
                        kv = keyVals.Current;
                        Debug.WriteLine("AI :: Key : " + kv.Key);
                        string val = kv.Value.ToObject<string>();
                        Debug.WriteLine("AI :: Value : " + val);
                        App.WriteToIsoStorageSettings(kv.Key, val);
                    }

                    JToken it = data[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                    if (it != null)
                    {
                        string tc = it.ToString().Trim();
                        Debug.WriteLine("Account Info :: TOTAL_CREDITS_PER_MONTH : " + tc);
                        this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
                    }
                }
                catch(Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Account Info Json Exception "+e.StackTrace);
                    return;
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
            #region GROUP CHAT RELATED

            #region GROUP_CHAT_JOIN
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                JArray arr = null;
                try
                {
                    arr = (JArray)jsonObj[HikeConstants.DATA];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCJ packet : " + e.StackTrace);
                }
                if (arr == null || !arr.HasValues)
                    return;

                string grpId = null;
                try
                {
                    grpId = jsonObj[HikeConstants.TO].ToString();
                }
                catch
                {
                    return;
                }

                if (!AddGroupmembers(arr, grpId)) // is gcj to add new members or to give DND info
                    return;

                ConvMessage convMessage = null;
                try
                {
                    convMessage = new ConvMessage(jsonObj, false);
                }
                catch
                {
                    return;
                }
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
                try
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
                catch(Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCN packet : "+e.StackTrace);
                }
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
                try
                {
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
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCL packet : " + e.StackTrace);
                }
            }
            #endregion
            #region GROUP_CHAT_END
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_END == type) //Group chat end
            {
                try
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
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCE packet : " + e.StackTrace);
                }
            }
            #endregion

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
            if (ms == null)
                return;
            /* Process UO for 1-1 chat*/

            if (!isOptInMsg || ConversationsList.ConvMap.ContainsKey(ms)) // if this is UJ or conversation has this msisdn go in
            {
                object[] vals = null;
                ConvMessage cm = new ConvMessage();
                cm.MetaDataString = jsonObj.ToString(Newtonsoft.Json.Formatting.None);
                cm.Timestamp = TimeUtils.getCurrentTimeStamp();
                cm.Msisdn = ms;
                cm.MessageId = -1;
                cm.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                if (isOptInMsg)
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
            }
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

                bool removeFirstMsgLogic = false;
                bool firstMsgLogic = false;
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
                            output = false;
                            if (!l[k].IsOnHike && onhike) // this is the case where client thinks that a given user is not on hike but actually he is on hike
                            {
                                removeFirstMsgLogic = true;
                                l[k].IsOnHike = onhike;
                                saveCache = true;
                                UsersTableUtils.updateOnHikeStatus(ms, true);
                            }

                            if (l[k].IsDND != dnd)
                            {
                                l[k].IsDND = dnd;
                                saveCache = true;
                            }

                            if (!onhike) // is any user is not on hike, first msg logic will be there
                            {
                                firstMsgLogic = true;
                                l[k].IsOnHike = onhike;
                                saveCache = true;
                            }

                            if (l[k].HasLeft)
                            {
                                l[k].HasLeft = false;
                                saveCache = true;
                                output = true;
                            }
                            break;
                        }
                    }
                }
                if (!firstMsgLogic && removeFirstMsgLogic) // this turn off first msg logic
                {
                    ConversationListObject co = null;
                    ConversationsList.ConvMap.TryGetValue(grpId, out co);
                    if (co != null)
                    {
                        co.IsFirstMsg = false;
                        ConversationTableUtils.updateConversation(co);
                        if (App.newChatThreadPage != null && App.newChatThreadPage.mContactNumber == grpId)
                            App.newChatThreadPage.IsFirstMsg = false;
                    }
                }
                if (saveCache)
                    App.WriteToIsoStorageSettings(App.GROUPS_CACHE, Utils.GroupCache);
                return output;
            }
            else
                return true;

        }

        private void updateDB(string fromUser, long msgID, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateMsgStatus(fromUser, msgID, status);
            ConversationTableUtils.updateLastMsgStatus(msgID,msisdn, status); // update conversationObj, null is already checked in the function
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED : {0}", msec);
        }

        private void updateDbBatch(string fromUser, long[] ids, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateAllMsgStatus(fromUser, ids, status);
            ConversationTableUtils.updateLastMsgStatus(ids[ids.Length-1],msisdn, status);
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED READ : {0}", msec);
        }
    }
}
