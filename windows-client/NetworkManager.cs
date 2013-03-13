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
using Microsoft.Phone.Notification;
using System.Text;
using windows_client.Misc;
using windows_client.View;
using System.Collections.ObjectModel;
using windows_client.Languages;
using System.Windows.Threading;
using windows_client.ViewModel;
using windows_client.Controls.StatusUpdate;
using Microsoft.Phone.Controls;

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
        private object lockObj = new object();
        public enum GroupChatState
        {
            ALREADY_ADDED_TO_GROUP, NEW_GROUP, ADD_MEMBER, DUPLICATE, KICKEDOUT_USER_ADDED
        }
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
                        if (Utils.isGroupConversation(convMessage.Msisdn))
                            GroupManager.Instance.LoadGroupParticipants(convMessage.Msisdn);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Exception in parsing json : " + e.StackTrace);
                        return;
                    }
                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(convMessage, false);

                    if (convMessage.FileAttachment != null && convMessage.FileAttachment.ContentType.Contains(HikeConstants.CONTACT))
                        convMessage.FileAttachment.FileState = Attachment.AttachmentState.COMPLETED;

                    if (convMessage.FileAttachment != null && (convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION)))
                    {
                        byte[] locationBytes = (new System.Text.UTF8Encoding()).GetBytes(convMessage.MetaDataString);
                        MiscDBUtil.storeFileInIsolatedStorage(HikeConstants.FILES_BYTE_LOCATION + "/" + convMessage.Msisdn + "/" +
                    Convert.ToString(convMessage.MessageId), locationBytes);
                    }

                    if (obj == null)
                        return;
                    if (convMessage.FileAttachment != null)
                    {
                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    }
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
            #endregion
            #region START_TYPING
            else if (START_TYPING == type) /* Start Typing event received*/
            {
                string sentTo = "";
                try
                {
                    sentTo = (string)jsonObj[HikeConstants.TO];
                }
                catch (Exception e)
                {
                }

                object[] vals = new object[2];
                vals[0] = msisdn;
                vals[1] = sentTo;
                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, vals);
                return;
            }
            #endregion
            #region END_TYPING
            else if (END_TYPING == type) /* End Typing event received */
            {
                string sentTo = "";
                try
                {
                    sentTo = (string)jsonObj[HikeConstants.TO];
                }
                catch (Exception e)
                {
                }

                object[] vals = new object[2];
                vals[0] = msisdn;
                vals[1] = sentTo;
                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.END_TYPING_CONVERSATION, vals);
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
                    Debug.WriteLine("NETWORK MANAGER:: Received report for Message Id " + msgID);
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
                if (msgIds == null || msgIds.Count == 0)
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
                // update contacts cache
                if (App.ViewModel.ContactsCache.ContainsKey(uMsisdn))
                    App.ViewModel.ContactsCache[uMsisdn].OnHike = joined;
                GroupManager.Instance.LoadGroupCache();
                if (joined)
                {
                    // if user is in contact list then only show the joined msg
                    ContactInfo c = UsersTableUtils.getContactInfoFromMSISDN(uMsisdn);
                    bool isUserInContactList = c != null ? true : false;
                    if (isUserInContactList && c.OnHike) // if user exists and is already on hike , do nothing
                        return;

                    // if user does not exists we dont know about his onhike status , so we need to process
                    ProcessUoUjMsgs(jsonObj, false, isUserInContactList);
                }
                // if user has left mark him as non hike user in group cache
                else if (GroupManager.Instance.GroupCache != null)
                {
                    foreach (string key in GroupManager.Instance.GroupCache.Keys)
                    {
                        bool shouldSave = false;
                        List<GroupParticipant> l = GroupManager.Instance.GroupCache[key];
                        for (int i = 0; i < l.Count; i++)
                        {
                            if (l[i].Msisdn == uMsisdn)
                            {
                                l[i].IsOnHike = false;
                                shouldSave = true;
                            }
                        }
                        if (shouldSave)
                            GroupManager.Instance.SaveGroupCache(key);
                    }
                }
                UsersTableUtils.updateOnHikeStatus(uMsisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(uMsisdn, joined);
                this.pubSub.publish(joined ? HikePubSub.USER_JOINED : HikePubSub.USER_LEFT, uMsisdn);
            }
            #endregion
            #region ICON
            else if (ICON == type)
            {
                // donot do anything if its a GC as it will be handled in DP packet
                if (Utils.isGroupConversation(msisdn))
                    return;

                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                string iconBase64 = temp.ToString();
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);

                Stopwatch st = Stopwatch.StartNew();
                MiscDBUtil.saveAvatarImage(msisdn, imageBytes, true);
                st.Stop();
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            App.ViewModel.ConvMap[msisdn].Avatar = imageBytes;
                            this.pubSub.publish(HikePubSub.UPDATE_UI, msisdn);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Network Manager : Exception in ICON :: " + ex.StackTrace);
                        }
                    });
                }
                else
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (msisdn == null)
                            return;
                        ConversationListObject c = App.ViewModel.GetFav(msisdn);
                        if (c != null) // for favourites
                        {
                            c.Avatar = imageBytes;
                        }
                        c = App.ViewModel.GetPending(msisdn);
                        if (c != null) // for pending requests
                        {
                            c.Avatar = imageBytes;
                        }
                    });
                }
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
                string totalCreditsPerMonth = "0";
                try
                {
                    totalCreditsPerMonth = data[HikeConstants.TOTAL_CREDITS_PER_MONTH].ToString();
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
                    Debug.WriteLine("NETWORK MANAGER : Received account info json : {0}", jsonObj.ToString());
                    KeyValuePair<string, JToken> kv;
                    IEnumerator<KeyValuePair<string, JToken>> keyVals = data.GetEnumerator();
                    while (keyVals.MoveNext())
                    {
                        try
                        {
                            kv = keyVals.Current;
                            Debug.WriteLine("AI :: Key : " + kv.Key);
                            JToken valTok = kv.Value;
                            object oj = valTok.ToObject<object>();
                            if (kv.Key == HikeConstants.ACCOUNT)
                            {
                                JObject acntValObj = (JObject)oj;
                                KeyValuePair<string, JToken> kkvv;
                                IEnumerator<KeyValuePair<string, JToken>> kkeyVvals = acntValObj.GetEnumerator();
                                while (kkeyVvals.MoveNext())
                                {
                                    try
                                    {
                                        kkvv = kkeyVvals.Current;
                                        Debug.WriteLine("AI :: Key : " + kkvv.Key);

                                        #region FAVOURITES
                                        if (kkvv.Key == HikeConstants.FAVORITES)
                                        {
                                            JObject favJSON = kkvv.Value.ToObject<JObject>();
                                            if (favJSON != null)
                                            {
                                                Deployment.Current.Dispatcher.BeginInvoke(() =>
                                                {
                                                    string name = null;
                                                    bool thrAreFavs = false;
                                                    KeyValuePair<string, JToken> fkkvv;
                                                    IEnumerator<KeyValuePair<string, JToken>> kVals = favJSON.GetEnumerator();
                                                    while (kVals.MoveNext())
                                                    {
                                                        bool isFav = true; // true for fav , false for pending
                                                        fkkvv = kVals.Current; // kkvv contains favourites MSISDN
                                                        JObject pendingJSON = fkkvv.Value.ToObject<JObject>();
                                                        JToken pToken;
                                                        if (pendingJSON.TryGetValue(HikeConstants.PENDING, out pToken))
                                                            isFav = false;
                                                        if (pendingJSON.TryGetValue(HikeConstants.NAME, out pToken))
                                                            name = pToken.ToString();
                                                        Debug.WriteLine("Fav request, Msisdn : {0} ; isFav : {1}", fkkvv.Key, isFav);
                                                        LoadFavAndPending(isFav, fkkvv.Key, name); // true for favs
                                                        thrAreFavs = true;
                                                    }
                                                    if (thrAreFavs)
                                                        this.pubSub.publish(HikePubSub.ADD_REMOVE_FAV, null);
                                                });
                                            }
                                        }

                                        #endregion
                                        #region FACEBOOK AND TWITTER
                                        if (kkvv.Key == HikeConstants.ACCOUNTS)
                                        {
                                            JObject socialObj = kkvv.Value.ToObject<JObject>();
                                            if (socialObj != null)
                                            {
                                                JToken socialJToken;
                                                socialObj.TryGetValue(HikeConstants.TWITTER, out socialJToken);
                                                if (socialJToken != null) // twitter is present in JSON
                                                {
                                                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.TWITTER_TOKEN, (string)(socialJToken as JObject)["id"]);
                                                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.TWITTER_TOKEN_SECRET, (string)(socialJToken as JObject)["token"]);
                                                    App.WriteToIsoStorageSettings(HikeConstants.TW_LOGGED_IN, true);
                                                }
                                                socialJToken = null;
                                                socialObj.TryGetValue(HikeConstants.FACEBOOK, out socialJToken);
                                                if (socialJToken != null) // facebook is present in JSON
                                                {
                                                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.FB_USER_ID, (string)(socialJToken as JObject)["id"]);
                                                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.FB_ACCESS_TOKEN, (string)(socialJToken as JObject)["token"]);
                                                    App.WriteToIsoStorageSettings(HikeConstants.FB_LOGGED_IN, true);
                                                }
                                            }

                                        }

                                        #endregion
                                        #region REWARDS
                                        if (kkvv.Key == HikeConstants.REWARDS_TOKEN)
                                        {
                                            App.WriteToIsoStorageSettings(HikeConstants.REWARDS_TOKEN, kkvv.Value.ToString());
                                        }
                                        // whenever this key will come toggle the show rewards thing
                                        if (kkvv.Key == HikeConstants.SHOW_REWARDS)
                                        {
                                            App.WriteToIsoStorageSettings(HikeConstants.SHOW_REWARDS, kkvv.Value.ToObject<bool>());
                                            pubSub.publish(HikePubSub.REWARDS_TOGGLE, null);
                                        }

                                        if (kkvv.Key == HikeConstants.MqttMessageTypes.REWARDS)
                                        {
                                            JObject ttObj = kkvv.Value.ToObject<JObject>();
                                            if (ttObj != null)
                                            {
                                                int rew_val = (int)ttObj[HikeConstants.REWARDS_VALUE];
                                                App.WriteToIsoStorageSettings(HikeConstants.REWARDS_VALUE, rew_val);
                                                pubSub.publish(HikePubSub.REWARDS_CHANGED, rew_val);
                                            }
                                        }

                                        #endregion
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
                                }

                                // save only for Twitter , FB
                                //App.WriteToIsoStorageSettings(kv.Key, (oj as JObject).ToString(Newtonsoft.Json.Formatting.None));
                            }// save only tc , invite_token
                            else if (kv.Key == HikeConstants.INVITE_TOKEN || kv.Key == HikeConstants.TOTAL_CREDITS_PER_MONTH)
                            {
                                string val = oj.ToString();
                                Debug.WriteLine("AI :: Value : " + val);

                                if (kv.Key == HikeConstants.INVITE_TOKEN || kv.Key == HikeConstants.TOTAL_CREDITS_PER_MONTH)
                                    App.WriteToIsoStorageSettings(kv.Key, val);
                            }
                        }
                        catch { }
                    }

                    JToken it = data[HikeConstants.TOTAL_CREDITS_PER_MONTH];
                    if (it != null)
                    {
                        string tc = it.ToString().Trim();
                        Debug.WriteLine("Account Info :: TOTAL_CREDITS_PER_MONTH : " + tc);
                        this.pubSub.publish(HikePubSub.INVITEE_NUM_CHANGED, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Account Info Json Exception " + e.StackTrace);
                    return;
                }

            }
            #endregion
            #region ACCOUNT CONFIG
            else if (HikeConstants.MqttMessageTypes.ACCOUNT_CONFIG == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    Debug.WriteLine("NETWORK MANAGER : Received account info json : {0}", jsonObj.ToString());

                    JToken rew;
                    if (data.TryGetValue(HikeConstants.REWARDS_TOKEN, out rew))
                        App.WriteToIsoStorageSettings(HikeConstants.REWARDS_TOKEN, rew.ToString());
                    rew = null;
                    if (data.TryGetValue(HikeConstants.SHOW_REWARDS, out rew))
                    {
                        App.WriteToIsoStorageSettings(HikeConstants.SHOW_REWARDS, rew.ToObject<bool>());
                        pubSub.publish(HikePubSub.REWARDS_TOGGLE, null);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            #endregion
            #region USER_OPT_IN
            else if (HikeConstants.MqttMessageTypes.USER_OPT_IN == type)
            {
                // {"t":"uo", "d":{"msisdn":"", "credits":10}}
                ProcessUoUjMsgs(jsonObj, true, true);
            }
            #endregion
            #region GROUP CHAT RELATED

            #region GROUP_CHAT_JOIN
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                jsonObj[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN_NEW;
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
                GroupManager.Instance.LoadGroupParticipants(grpId);
                ConvMessage convMessage = null;
                List<GroupParticipant> dndList = new List<GroupParticipant>(1);
                GroupChatState gcState = AddGroupmembers(arr, grpId, dndList);

                #region NEW GROUP
                if (gcState == GroupChatState.NEW_GROUP) // this group is created by someone else
                {
                    // 1. create new msg for new GC
                    // 2. create DND msg also
                    try
                    {
                        convMessage = new ConvMessage(jsonObj, false, false); // this will be normal DND msg
                        List<GroupParticipant> dndMembersList = GetDNDMembers(grpId);
                        if (dndMembersList != null && dndMembersList.Count > 0)
                        {
                            string dndMsg = GetDndMsg(dndMembersList);
                            convMessage.Message += ";" + dndMsg;
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
                #endregion
                #region ALREADY ADDED TO GROUP
                else if (gcState == GroupChatState.ALREADY_ADDED_TO_GROUP)
                {
                    // update JSON in the metadata .....
                    if (dndList.Count > 0) // there are people who are in dnd , show their msg
                    {
                        JObject o = new JObject();
                        o[HikeConstants.TYPE] = HikeConstants.MqttMessageTypes.DND_USER_IN_GROUP;
                        convMessage = new ConvMessage(); // this will be normal DND msg
                        convMessage.Msisdn = grpId;
                        convMessage.MetaDataString = o.ToString(Formatting.None);
                        convMessage.Message = GetDndMsg(dndList);
                        convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                        convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.DND_USER;
                        convMessage.Timestamp = TimeUtils.getCurrentTimeStamp();
                    }
                    else
                        return;
                }
                #endregion
                #region DUPLICATE GCJ
                else if (gcState == GroupChatState.DUPLICATE)
                {
                    return;
                }
                #endregion
                #region KICKEDOUT USER ADDED
                else if (gcState == GroupChatState.KICKEDOUT_USER_ADDED)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!App.IS_MARKETPLACE) // remove this later , this is only for QA
                            MessageBox.Show("GCJ came after adding knocked user!!");
                    });
                    GroupTableUtils.SetGroupAlive(grpId);
                    convMessage = new ConvMessage(jsonObj, false, false); // this will be normal GCJ msg
                    this.pubSub.publish(HikePubSub.GROUP_ALIVE, grpId);
                }
                #endregion
                #region ADD NEW MEMBERS TO EXISTING GROUP
                else // new members are added to group
                {
                    try
                    {
                        convMessage = new ConvMessage(jsonObj, false, true); // this will be normal DND msg
                        List<GroupParticipant> dndMembersList = GetDNDMembers(grpId);
                        if (dndMembersList != null && dndMembersList.Count > 0)
                        {
                            string dndMsg = GetDndMsg(dndMembersList);
                            convMessage.Message += ";" + dndMsg;
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
                #endregion

                ConversationListObject obj = MessagesTableUtils.addGroupChatMessage(convMessage, jsonObj);
                if (obj == null)
                    return;
                GroupManager.Instance.SaveGroupCache(grpId);
                //App.WriteToIsoStorageSettings(App.GROUPS_CACHE, GroupManager.Instance.GroupCache);
                Debug.WriteLine("NetworkManager", "Group is new");

                object[] vals = new object[2];
                vals[0] = convMessage;
                vals[1] = obj;

                this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                this.pubSub.publish(HikePubSub.PARTICIPANT_JOINED_GROUP, jsonObj);
            }
            #endregion
            #region GROUP_CHAT_NAME CHANGE
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_NAME == type) //Group chat name change
            {
                try
                {
                    string groupName = (string)jsonObj[HikeConstants.DATA];
                    string groupId = (string)jsonObj[HikeConstants.TO];
                    if (msisdn == App.MSISDN) // if I changed the name ignore
                        return;
                    bool groupExist = ConversationTableUtils.updateGroupName(groupId, groupName);
                    if (!groupExist)
                        return;
                    ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE, jsonObj);
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                    if (obj == null)
                        return;
                    object[] vals = new object[2];
                    vals[0] = cm;
                    vals[1] = obj;

                    bool goAhead = GroupTableUtils.updateGroupName(groupId, groupName);
                    if (goAhead)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            App.ViewModel.ConvMap[groupId].ContactName = groupName;
                            this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                            this.pubSub.publish(HikePubSub.GROUP_NAME_CHANGED, groupId);
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCN packet : " + e.StackTrace);
                }
            }
            #endregion
            #region GROUP DISPLAY PIC CHANGE
            else if (HikeConstants.MqttMessageTypes.GROUP_DISPLAY_PIC == type)
            {
                string groupId = (string)jsonObj[HikeConstants.TO];
                string from = (string)jsonObj[HikeConstants.FROM];
                if (from == App.MSISDN) // if you changed the pic simply ignore
                    return;
                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                string iconBase64 = temp.ToString();
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED, jsonObj);
                ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                if (obj == null)
                    return;
                MiscDBUtil.saveAvatarImage(groupId, imageBytes, true);
                if (App.ViewModel.ConvMap.ContainsKey(groupId))
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        try
                        {
                            App.ViewModel.ConvMap[groupId].Avatar = imageBytes;
                            object[] oa = new object[2];
                            oa[0] = cm;
                            oa[1] = obj;
                            this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, oa);
                            this.pubSub.publish(HikePubSub.UPDATE_GRP_PIC, groupId);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Network Manager : Exception in ICON :: " + ex.StackTrace);
                        }
                    });
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
                    GroupManager.Instance.LoadGroupParticipants(groupId);
                    GroupParticipant gp = GroupManager.Instance.getGroupParticipant(null, fromMsisdn, groupId);
                    if (gp.HasLeft)
                        return;

                    ConvMessage convMsg = new ConvMessage(jsonObj, false, false);
                    GroupManager.Instance.SaveGroupCache(groupId);
                    ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMsg, false); // grp name will change inside this
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
                        ConvMessage convMessage = new ConvMessage(jsonObj, false, false);
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
            #region INTERNATIONAL USER
            else if (HikeConstants.MqttMessageTypes.BLOCK_INTERNATIONAL_USER == type)
            {
                ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.INTERNATIONAL_USER, jsonObj);
                cm.Msisdn = msisdn;
                ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                if (obj == null)
                    return;
                object[] vals = new object[2];
                vals[0] = cm;
                vals[1] = obj;
                pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
            }
            #endregion
            #region ADD FAVOURITES
            else if (HikeConstants.MqttMessageTypes.ADD_FAVOURITE == type)
            {
                try
                {
                    string ms = (string)jsonObj[HikeConstants.FROM];
                    if (ms == null)
                        return;
                    FriendsTableUtils.SetFriendStatus(ms, FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED);
                    if (App.ViewModel.Isfavourite(ms)) // already favourite
                        return;
                    if (App.ViewModel.IsPending(ms))
                        return;

                    try
                    {

                        ConversationListObject favObj;
                        if (App.ViewModel.ConvMap.ContainsKey(ms))
                            favObj = App.ViewModel.ConvMap[ms];
                        else
                        {
                            ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                            string name = null;
                            if (ci == null)
                            {
                                JToken data;
                                if (jsonObj.TryGetValue(HikeConstants.DATA, out data))
                                {
                                    JToken n;
                                    JObject dobj = data.ToObject<JObject>();
                                    if (dobj.TryGetValue(HikeConstants.NAME, out n))
                                        name = n.ToString();
                                }
                            }
                            else
                                name = ci.Name;
                            favObj = new ConversationListObject(ms, name, ci != null ? ci.OnHike : true, ci != null ? MiscDBUtil.getThumbNailForMsisdn(ms) : null);
                        }
                        // this will ensure there will be one pending request for a particular msisdn
                        App.ViewModel.PendingRequests[ms] = favObj;
                        MiscDBUtil.SavePendingRequests();
                        this.pubSub.publish(HikePubSub.ADD_TO_PENDING, favObj);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("Network Manager : Exception in ADD FAVORITES :: " + e.StackTrace);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in ADD TO FAVS : " + e.StackTrace);
                }
            }
            #endregion
            #region POSTPONE FRIEND REQUEST
            else if (HikeConstants.MqttMessageTypes.POSTPONE_FRIEND_REQUEST == type)
            {
                try
                {
                    FriendsTableUtils.DeleteFriend(msisdn);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in PostPone from FAVS : " + e.StackTrace);
                }
            }
            #endregion
            #region REMOVE FAVOURITES
            else if (HikeConstants.MqttMessageTypes.REMOVE_FAVOURITE == type)
            {
                try
                {
                    FriendsTableUtils.FriendStatusEnum fs = FriendsTableUtils.GetFriendStatus(msisdn);
                    if (fs == FriendsTableUtils.FriendStatusEnum.FRIENDS)
                        FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                    else
                        FriendsTableUtils.DeleteFriend(msisdn);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in Remove from Friends: " + e.StackTrace);
                }
            }
            #endregion
            #region REWARDS VALUE CHANGED
            else if (HikeConstants.MqttMessageTypes.REWARDS == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    int rewards_val = (int)data[HikeConstants.REWARDS_VALUE];
                    App.WriteToIsoStorageSettings(HikeConstants.REWARDS_VALUE, rewards_val);
                    pubSub.publish(HikePubSub.REWARDS_CHANGED, rewards_val);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Netwok Manager :: Exception in REWARDS : " + e.StackTrace);
                }
            }
            #endregion
            #region STATUS UPDATE
            else if (HikeConstants.MqttMessageTypes.STATUS_UPDATE == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    StatusMessage sm = null;
                    JToken val;
                    string iconBase64 = null;
                    if (data.TryGetValue(HikeConstants.THUMBNAIL, out val) && val != null)
                        iconBase64 = val.ToString();

                    val = null;

                    #region HANDLE PROFILE PIC UPDATE
                    if (data.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                    {
                        string id = null;
                        JToken idToken;
                        if (data.TryGetValue(HikeConstants.STATUS_ID, out idToken))
                            id = idToken.ToString();

                        sm = new StatusMessage(msisdn, id, StatusMessage.StatusType.PROFILE_PIC_UPDATE, id, TimeUtils.getCurrentTimeStamp(), -1);

                        idToken = null;
                        if (iconBase64 != null)
                        {
                            byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                            StatusMsgsTable.InsertStatusMsg(sm);
                            MiscDBUtil.saveProfileImages(msisdn, imageBytes, sm.ServerId);
                            jsonObj[HikeConstants.PROFILE_PIC_ID] = sm.ServerId;
                        }
                    }
                    #endregion

                    #region HANDLE TEXT UPDATE
                    else if (data.TryGetValue(HikeConstants.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        string id = null;
                        JToken idToken;
                        if (data.TryGetValue(HikeConstants.STATUS_ID, out idToken) && idToken != null)
                            id = idToken.ToString();

                        idToken = null;
                        if (data.TryGetValue(HikeConstants.MOOD, out idToken) && idToken != null && string.IsNullOrEmpty(idToken.ToString()))
                            sm = new StatusMessage(msisdn, val.ToString(), StatusMessage.StatusType.TEXT_UPDATE, id, TimeUtils.getCurrentTimeStamp(), -1, idToken.ToString(), true);
                        else
                            sm = new StatusMessage(msisdn, val.ToString(), StatusMessage.StatusType.TEXT_UPDATE, id, TimeUtils.getCurrentTimeStamp(), -1);

                        StatusMsgsTable.InsertStatusMsg(sm);
                    }
                    #endregion

                    ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.STATUS_UPDATE, jsonObj);
                    cm.Msisdn = msisdn;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);

                    // if conversation  with this user exists then only show him status updates on chat thread and conversation screen
                    if (obj != null)
                    {
                        object[] vals = new object[2];
                        vals[0] = cm;
                        vals[1] = null; // always send null as we dont want any activity on conversation page
                        pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                        sm.MsgId = cm.MessageId;
                        StatusMsgsTable.UpdateMsgId(sm);
                    }
                    pubSub.publish(HikePubSub.STATUS_RECEIVED, sm);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in REWARDS : " + e.StackTrace);
                }
            }
            #endregion
            #region DELETE STATUS
            else if (HikeConstants.MqttMessageTypes.DELETE_STATUS_UPDATE == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    string id = (string)data[HikeConstants.STATUS_ID];
                    long msgId = StatusMsgsTable.DeleteStatusMsg(id);
                    if(msgId > 0) // delete only if msgId is greater than 0
                        MessagesTableUtils.deleteMessage(msgId);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception in DELETE STATUS : " + e.StackTrace);
                }
            }
            #endregion
            #region OTHER
            else
            {
                //logger.Info("WebSocketPublisher", "Unknown Type:" + type);
            }
            #endregion
        }

        private void LoadFavAndPending(bool isFav, string msisdn, string name)
        {
            if (msisdn == null)
                return;

            if (isFav)
            {
                if (App.ViewModel.Isfavourite(msisdn))
                    return;
                ConversationListObject favObj = null;
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    favObj = App.ViewModel.ConvMap[msisdn];
                }
                else
                {
                    // here no need to call cache
                    ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    favObj = new ConversationListObject(msisdn, ci != null ? ci.Name : null, ci != null ? ci.OnHike : true, ci != null ? MiscDBUtil.getThumbNailForMsisdn(msisdn) : null);
                }
                App.ViewModel.FavList.Add(favObj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(favObj);
                int count = 0;
                App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
            }
            else // pending case
            {
                if (App.ViewModel.IsPending(msisdn))
                    return;
                ConversationListObject favObj = null;
                if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                {
                    favObj = App.ViewModel.ConvMap[msisdn];
                }
                else
                {
                    // no need to call cache here
                    ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    favObj = new ConversationListObject(msisdn, ci != null ? ci.Name : null, ci != null ? ci.OnHike : true, ci != null ? MiscDBUtil.getThumbNailForMsisdn(msisdn) : null);
                }
                App.ViewModel.PendingRequests[favObj.Msisdn] = favObj;
                MiscDBUtil.SavePendingRequests();
            }
        }

        private List<GroupParticipant> GetDNDMembers(string grpId)
        {
            List<GroupParticipant> members = GroupManager.Instance.GetParticipantList(grpId);
            List<GroupParticipant> output = null;

            for (int i = 0; i < members.Count; i++)
            {
                if (!members[i].IsOnHike && members[i].IsDND && !members[i].IsUsed)
                {
                    if (output == null)
                        output = new List<GroupParticipant>();
                    output.Add(members[i]);
                    members[i].IsUsed = true;
                }
            }
            return output;
        }

        private string GetDndMsg(List<GroupParticipant> dndMembersList)
        {
            StringBuilder msgText = new StringBuilder();
            if (dndMembersList.Count == 1)
                msgText.Append(dndMembersList[0].FirstName);
            else if (dndMembersList.Count == 2)
                msgText.Append(dndMembersList[0].FirstName + AppResources.And_txt + dndMembersList[1].FirstName);
            else
            {
                for (int i = 0; i < dndMembersList.Count; i++)
                {
                    msgText.Append(dndMembersList[i].FirstName);
                    if (i == dndMembersList.Count - 2)
                        msgText.Append(AppResources.And_txt);
                    else if (i < dndMembersList.Count - 2)
                        msgText.Append(",");
                }
            }
            return string.Format(AppResources.WAITING_TO_JOIN, msgText.ToString());
        }

        private void ProcessUoUjMsgs(JObject jsonObj, bool isOptInMsg, bool isUserInContactList)
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

            if (isUserInContactList)
            {
                if (!isOptInMsg || App.ViewModel.ConvMap.ContainsKey(ms)) // if this is UJ or conversation has this msisdn go in
                {
                    object[] vals = null;
                    ConvMessage cm = null;
                    if (isOptInMsg)
                        cm = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);
                    else
                        cm = new ConvMessage(ConvMessage.ParticipantInfoState.USER_JOINED, jsonObj);
                    cm.Msisdn = ms;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                    if (obj == null)
                    {
                        GroupManager.Instance.SaveGroupCache(cm.Msisdn);
                        //App.WriteToIsoStorageSettings(App.GROUPS_CACHE, GroupManager.Instance.GroupCache);
                        return;
                    }
                    if (credits <= 0)
                        vals = new object[2];
                    else                    // this shows that we have to show credits msg as this user got credits.
                    {
                        string text = string.Format(AppResources.CREDITS_EARNED, credits);
                        JObject o = new JObject();
                        o.Add("t", "credits_gained");
                        ConvMessage cmCredits = new ConvMessage(ConvMessage.ParticipantInfoState.CREDITS_GAINED, o);
                        cmCredits.Message = text;
                        cmCredits.Msisdn = ms;
                        obj = MessagesTableUtils.addChatMessage(cmCredits, false);
                        vals = new object[3];
                        vals[2] = cmCredits;
                    }

                    vals[0] = cm;
                    vals[1] = obj;

                    pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                }
            }
            // UPDATE group cache
            foreach (string key in GroupManager.Instance.GroupCache.Keys)
            {
                List<GroupParticipant> l = GroupManager.Instance.GroupCache[key];
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].Msisdn == ms) // if this msisdn exists in group
                    {
                        ConvMessage convMsg = null;
                        if (!isOptInMsg) // represents UJ event
                        {
                            if (l[i].IsOnHike)  // if this user is already on hike
                                continue;
                            l[i].IsOnHike = true;
                            if (!GroupTableUtils.IsGroupAlive(key)) // if group is dead simply dont do anything
                                continue;
                            convMsg = new ConvMessage(ConvMessage.ParticipantInfoState.USER_JOINED, jsonObj);
                        }
                        else
                            convMsg = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);

                        object[] values = null;
                        convMsg.Msisdn = key;
                        convMsg.Message = ms;
                        ConversationListObject co = MessagesTableUtils.addChatMessage(convMsg, false);
                        if (co == null)
                        {
                            GroupManager.Instance.SaveGroupCache();
                            return;
                        }
                        if (credits > 0)                    // this shows that we have to show credits msg as this user got credits.
                        {
                            string text = string.Format(AppResources.CREDITS_EARNED, credits);
                            JObject o = new JObject();
                            o.Add("t", "credits_gained");
                            ConvMessage cmCredits = new ConvMessage(ConvMessage.ParticipantInfoState.CREDITS_GAINED, o);
                            cmCredits.Message = text;
                            cmCredits.Msisdn = key;
                            co = MessagesTableUtils.addChatMessage(cmCredits, false);
                            if (co == null)
                            {
                                GroupManager.Instance.SaveGroupCache();
                                return;
                            }
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
            GroupManager.Instance.SaveGroupCache();
        }

        #region OLD ADD GROUPMEMBERS LOGIC
        /// <summary>
        /// This function will return 
        ///  -- > true , if new users are added to GC
        ///  -- > false , if GCJ is come to notify DND status
        //private bool AddGroupmembers(JArray arr, string grpId)
        //{
        //    if (App.ViewModel.ConvMap.ContainsKey(grpId))
        //    {
        //        List<GroupParticipant> l = null;
        //        GroupManager.Instance.GroupCache.TryGetValue(grpId, out l);
        //        if (l == null)
        //            return true;

        //        bool saveCache = false;
        //        bool output = true;
        //        for (int i = 0; i < arr.Count; i++)
        //        {
        //            JObject o = (JObject)arr[i];
        //            bool onhike = (bool)o["onhike"];
        //            bool dnd = (bool)o["dnd"];
        //            string ms = (string)o["msisdn"];
        //            for (int k = 0; k < l.Count; k++)
        //            {
        //                if (l[k].Msisdn == ms)
        //                {
        //                    output = false;
        //                    if (!l[k].IsOnHike && onhike) // this is the case where client thinks that a given user is not on hike but actually he is on hike
        //                    {
        //                        l[k].IsOnHike = onhike;
        //                        saveCache = true;
        //                        UsersTableUtils.updateOnHikeStatus(ms, true);
        //                    }

        //                    if (l[k].IsDND != dnd)
        //                    {
        //                        l[k].IsDND = dnd;
        //                        saveCache = true;
        //                    }

        //                    if (!onhike) // is any user is not on hike, first msg logic will be there
        //                    {
        //                        l[k].IsOnHike = onhike;
        //                        saveCache = true;
        //                    }

        //                    if (l[k].HasLeft)
        //                    {
        //                        l[k].HasLeft = false;
        //                        saveCache = true;
        //                        output = true;
        //                    }
        //                    break;
        //                }
        //            }
        //        }
        //        if (saveCache)
        //            App.WriteToIsoStorageSettings(App.GROUPS_CACHE, GroupManager.Instance.GroupCache);
        //        return output;
        //    }
        //    else
        //        return true;

        //}

        #endregion

        /*
         * This function performs 3 roles
         * 1. Same GCJ is received by user who created group
         * 2. New GCJ is received
         * 3. User is added to group.
         */
        private GroupChatState AddGroupmembers(JArray arr, string grpId, List<GroupParticipant> dndList)
        {
            if (!App.ViewModel.ConvMap.ContainsKey(grpId)) // if its a new group always return true
                return GroupChatState.NEW_GROUP;
            else
            {
                // now check if its same gcj packet created by owner or its different gcj packet
                List<GroupParticipant> l = GroupManager.Instance.GetParticipantList(grpId);
                if (l == null || l.Count == 0)
                    return GroupChatState.NEW_GROUP;

                GroupInfo gi = GroupTableUtils.getGroupInfoForId(grpId);
                if (gi != null && !gi.GroupAlive)
                    return GroupChatState.KICKEDOUT_USER_ADDED;

                GroupChatState output = GroupChatState.DUPLICATE;
                Dictionary<string, GroupParticipant> gpMap = GetGroupParticipantMap(l);
                for (int i = 0; i < arr.Count; i++)
                {
                    JObject o = (JObject)arr[i];
                    string ms = (string)o["msisdn"];
                    GroupParticipant gp = null;

                    if (!gpMap.TryGetValue(ms, out gp) || gp == null || gp.HasLeft)     // this shows this member is not in the list and is added externally
                        return GroupChatState.ADD_MEMBER;

                    else if (!gp.IsUsed)
                    {
                        bool onhike = (bool)o["onhike"];
                        bool dnd = (bool)o["dnd"];
                        gp.IsUsed = true;
                        gp.IsOnHike = onhike;
                        gp.IsDND = dnd;
                        if (!onhike && dnd) // this member is in dnd so add to dndList and show notification msg
                        {
                            gp.IsDND = true;
                            dndList.Add(gp);
                        }
                        output = GroupChatState.ALREADY_ADDED_TO_GROUP;
                    }
                    else
                        output = GroupChatState.DUPLICATE;
                }
                return output;
            }
        }

        private Dictionary<string, GroupParticipant> GetGroupParticipantMap(List<GroupParticipant> groupParticipantList)
        {
            Dictionary<string, GroupParticipant> map = new Dictionary<string, GroupParticipant>(groupParticipantList.Count);
            for (int i = 0; i < groupParticipantList.Count; i++)
                map[groupParticipantList[i].Msisdn] = groupParticipantList[i];
            return map;
        }

        private void updateDB(string fromUser, long msgID, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateMsgStatus(fromUser, msgID, status);
            ConversationTableUtils.updateLastMsgStatus(msgID, msisdn, status); // update conversationObj, null is already checked in the function
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED : {0}", msec);
        }

        private void updateDbBatch(string fromUser, long[] ids, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateAllMsgStatus(fromUser, ids, status);
            ConversationTableUtils.updateLastMsgStatus(ids[ids.Length - 1], msisdn, status);
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED READ : {0}", msec);
        }
    }
}
