using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CommonLibrary.Model;
using CommonLibrary.DbUtils;
using CommonLibrary.utils;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using CommonLibrary.Misc;
using CommonLibrary.Languages;
using CommonLibrary.ViewModel;
using CommonLibrary.utils.ServerTips;
using CommonLibrary.Constants;
using CommonLibrary.Lib;
using CommonLibrary.Utils;

namespace CommonLibrary
{
    public class NetworkManager
    {
        /* message read by recipient */
        public static readonly string MESSAGE_READ = "mr";

        public static readonly string BULK_MESSAGES = "bm";

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
        public static readonly string MULTIPLE_INVITE = "mi";

        public static readonly string ICON = "ic";

        public static readonly string SERVER_TIMESTAMP = "sts";
        public static readonly string LAST_SEEN = "ls";

        public static readonly string REQUEST_DISPLAY_PIC = "rdp";

        public static readonly string STICKER = "stk";

        public static readonly string ACTION = "action";

        public static readonly string ICON_REMOVE = "icr";

        public static readonly string TIPS_POPUP = "popup";
        private static readonly string TIPS_HEADER = "h";
        private static readonly string TIPS_BODY = "b";
        private static readonly string TIPS_ID = "i";

        public static bool turnOffNetworkManager = true;

        private static volatile NetworkManager instance;
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private object lockObj = new object();

        public enum GroupChatState
        {
            ALREADY_ADDED_TO_GROUP, NEW_GROUP, ADD_MEMBER, DUPLICATE, KICKEDOUT_USER_ADDED
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
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse, Exception : " + ex.StackTrace);
                return;
            }
            string type = null;
            try
            {
                type = (string)jsonObj[ServerJsonKeys.TYPE];
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse type, Exception : " + ex.StackTrace);
                return;
            }
            string msisdn = null;
            try
            {
                msisdn = (string)jsonObj[ServerJsonKeys.FROM];
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse from, Exception : " + ex.StackTrace);
                return;
            }
            #region BULK MESSAGE
            if (BULK_MESSAGES == type)
            {
                ProcessBulkPacket(jsonObj);
            }
            #endregion
            #region MESSAGE
            else if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    bool isPush = true;
                    JToken pushJToken;
                    var jData = (JObject)jsonObj[ServerJsonKeys.DATA];
                    if (jData.TryGetValue(ServerJsonKeys.PUSH, out pushJToken))
                        isPush = (Boolean)pushJToken;

                    ConvMessage convMessage = null;
                    try
                    {
                        convMessage = new ConvMessage(jsonObj);

                        if (Utility.IsGroupConversation(convMessage.Msisdn))
                            GroupManager.Instance.LoadGroupParticipants(convMessage.Msisdn);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  MESSAGE convmessage, Exception : " + ex.StackTrace);
                        return;
                    }

                    convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(convMessage, false);

                    if (obj == null)
                        return;

                    if (convMessage.FileAttachment != null && (convMessage.FileAttachment.ContentType.Contains(FTBasedConstants.CONTACT)
                        || convMessage.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION)))
                    {
                        convMessage.FileAttachment.FileState = Attachment.AttachmentState.COMPLETED;
                    }

                    if (convMessage.FileAttachment != null)
                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  MESSAGE , Exception : " + ex.StackTrace);
                    return;
                }
            }
            #endregion
            #region REQUEST_DISPLAY_PIC
            else if (REQUEST_DISPLAY_PIC == type)
            {
                string grpId = String.Empty;
                try
                {
                    grpId = (string)jsonObj[ServerJsonKeys.TO];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  REQUEST_DISPLAY_PIC, Exception : " + ex.StackTrace);
                }

                HikeInstantiation.ViewModel.AddGroupPicForUpload(grpId);
            }
            #endregion
            #region LAST_SEEN
            else if (LAST_SEEN == type) /* Last Seen received */
            {
                long lastSeen = 0;

                try
                {
                    var data = jsonObj[ServerJsonKeys.DATA];
                    lastSeen = (long)data[ServerJsonKeys.LASTSEEN];

                    if (lastSeen > 0)
                    {
                        long timedifference;
                        if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.TIME_DIFF_EPOCH, out timedifference))
                            lastSeen = lastSeen - timedifference;
                    }

                    if (lastSeen == -1)
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, 0);
                    else if (lastSeen == 0)
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, TimeUtils.GetCurrentTimeStamp());
                    else
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, lastSeen);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  Last Seen :  TimeStamp, Exception : " + ex.StackTrace);
                }

                return;
            }
            #endregion
            #region SMS_CREDITS
            else if (SMS_CREDITS == type) /* SMS CREDITS */
            {
                try
                {
                    int sms_credits = Int32.Parse((string)jsonObj[ServerJsonKeys.DATA]);
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.SMS_SETTING, sms_credits);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  SMS_CREDITS, Exception : " + ex.StackTrace);
                }
            }
            #endregion
            #region SERVER_REPORT
            else if (SERVER_REPORT == type) /* Represents Server has received the msg you sent */
            {
                string id = (string)jsonObj[ServerJsonKeys.DATA];
                long msgID;

                try
                {
                    msgID = long.Parse(id);
                    Debug.WriteLine("NETWORK MANAGER:: Received report for Message Id " + msgID);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  SERVER_REPORT, Exception : " + ex.StackTrace);
                    msgID = -1;
                    return;
                }

                MiscDBUtil.UpdateDBsMessageStatus(null, msgID, (int)ConvMessage.State.SENT_CONFIRMED);
            }
            #endregion
            #region DELIVERY_REPORT
            else if (DELIVERY_REPORT == type) // this handles the case when msg with msgId is recieved by the recipient but is unread
            {
                string id = (string)jsonObj[ServerJsonKeys.DATA];
                JToken msisdnToken = null;
                string msisdnToCheck = null;
                long msgID;

                try
                {
                    msgID = Int64.Parse(id);
                    jsonObj.TryGetValue(ServerJsonKeys.TO, out msisdnToken);
                    msisdnToCheck = msisdnToken != null ? msisdnToken.ToString() : msisdn;
                }
                catch (FormatException e)
                {
                    Debug.WriteLine("Network Manager:: Delivery Report, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), e.StackTrace);
                    msgID = -1;
                    return;
                }

                MiscDBUtil.UpdateDBsMessageStatus(msisdnToCheck, msgID, (int)ConvMessage.State.SENT_DELIVERED);
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
                    jsonObj.TryGetValue(ServerJsonKeys.TO, out msisdnToken);
                    if (msisdnToken != null)
                        msisdnToCheck = msisdnToken.ToString();
                    else
                        msisdnToCheck = msisdn;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  MESSAGE_READ, Exception : " + ex.StackTrace);
                    return;
                }

                if (msgIds == null || msgIds.Count == 0)
                {
                    Debug.WriteLine("NETWORK MANAGER", "Update Error : Message id Array is empty or null . Check problem");
                    return;
                }

                long[] ids = new long[msgIds.Count];

                for (int i = 0; i < ids.Length; i++)
                    ids[i] = Int64.Parse(msgIds[i].ToString());

                updateDbBatch(msisdnToCheck, ids, (int)ConvMessage.State.SENT_DELIVERED_READ, msisdn);
            }
            #endregion
            #region USER_JOINED USER_LEFT
            else if ((USER_JOINED == type) || (USER_LEFT == type))
            {
                JObject o = null;
                string uMsisdn = null;
                long serverTimestamp = 0;

                try
                {
                    o = (JObject)jsonObj[ServerJsonKeys.DATA];
                    uMsisdn = (string)o[ServerJsonKeys.MSISDN];
                    serverTimestamp = (long)jsonObj[ServerJsonKeys.TIMESTAMP];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  USER_JOINED USER_LEFT, Exception : " + ex.StackTrace);
                    return;
                }

                bool joined = USER_JOINED == type;
                bool isRejoin = false;
                JToken subtype;

                if (jsonObj.TryGetValue(ServerJsonKeys.SUB_TYPE, out subtype))
                    isRejoin = ServerJsonKeys.SUBTYPE_REJOIN == (string)subtype;
                
                // update contacts cache
                if (HikeInstantiation.ViewModel.ContactsCache.ContainsKey(uMsisdn))
                    HikeInstantiation.ViewModel.ContactsCache[uMsisdn].OnHike = joined;
                
                GroupManager.Instance.LoadGroupParticpantsCache();
                
                if (joined)
                {
                    long lastTimeStamp;
                    
                    if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.LAST_USER_JOIN_TIMESTAMP, out lastTimeStamp) && lastTimeStamp >= serverTimestamp)
                        return;

                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.LAST_USER_JOIN_TIMESTAMP, serverTimestamp);

                    // if user is in contact list then only show the joined msg
                    ContactInfo c = UsersTableUtils.getContactInfoFromMSISDN(uMsisdn);

                    // if user does not exists we dont know about his onhike status , so we need to process
                    ProcessUoUjMsgs(jsonObj, false, c != null, isRejoin);
                }
                // if user has left, mark him as non hike user in group cache
                else
                {
                    MiscDBUtil.DeleteImageForMsisdn(uMsisdn);

                    if (GroupManager.Instance.GroupParticpantsCache != null)
                    {
                        foreach (string key in GroupManager.Instance.GroupParticpantsCache.Keys)
                        {
                            bool shouldSave = false;
                            List<GroupParticipant> l = GroupManager.Instance.GroupParticpantsCache[key];
                            for (int i = 0; i < l.Count; i++)
                            {
                                if (l[i].Msisdn == uMsisdn)
                                {
                                    l[i].IsOnHike = false;
                                    shouldSave = true;
                                }
                            }

                            if (shouldSave)
                                GroupManager.Instance.SaveGroupParticpantsCache(key);
                        }
                    }
                }

                UsersTableUtils.updateOnHikeStatus(uMsisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(uMsisdn, joined);

                JToken jt;
                long ts = 0;
                
                if (joined && jsonObj.TryGetValue(ServerJsonKeys.TIMESTAMP, out jt))
                    ts = jt.ToObject<long>();
                
                FriendsTableUtils.SetJoiningTime(uMsisdn, ts);
            }
            #endregion
            #region ICON
            else if (ICON == type)
            {
                // donot do anything if its a GC as it will be handled in DP packet
                if (Utility.IsGroupConversation(msisdn))
                    return;

                JToken temp;
                jsonObj.TryGetValue(ServerJsonKeys.DATA, out temp);
                
                if (temp == null)
                    return;

                string iconBase64 = temp.ToString();
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);

                MiscDBUtil.saveAvatarImage(msisdn, imageBytes, true);
            }
            #endregion
            #region INVITE_INFO
            else if (INVITE_INFO == type)
            {
                JObject data;
                JToken temp;
                jsonObj.TryGetValue(ServerJsonKeys.DATA, out temp);

                if (temp == null)
                    return;
                
                data = temp.ToObject<JObject>();
                
                try
                {
                    int invited = (int)data[ServerJsonKeys.ALL_INVITEE];
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.INVITED, invited);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }

                try
                {
                    int invited_joined = (int)data[ServerJsonKeys.ALL_INVITEE_JOINED];
                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.INVITED_JOINED, invited_joined);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }

                string totalCreditsPerMonth = "0";
                
                try
                {
                    totalCreditsPerMonth = data[ServerJsonKeys.TOTAL_CREDITS_PER_MONTH].ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }

                if (!String.IsNullOrEmpty(totalCreditsPerMonth) && Int32.Parse(totalCreditsPerMonth) > 0)
                    HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.TOTAL_CREDITS_PER_MONTH, totalCreditsPerMonth);
            }
            #endregion
            #region ACCOUNT_INFO
            else if (ServerJsonKeys.MqttMessageTypes.ACCOUNT_INFO == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    Debug.WriteLine("NETWORK MANAGER : Received account info json : {0}", jsonObj.ToString());
                    JToken jtoken;
                    
                    if (data.TryGetValue(AppSettingsKeys.SHOW_FREE_INVITES, out jtoken) && (bool)jtoken)
                        HikeInstantiation.AppSettings[AppSettingsKeys.SHOW_POPUP] = null;//to show it is free sms pop up.
                    
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

                            if (kv.Key == ServerJsonKeys.ACCOUNT)
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

                                        if (kkvv.Key == ServerJsonKeys.FAVORITES)
                                        {
                                            JObject favJSON = kkvv.Value.ToObject<JObject>();
                                            if (favJSON != null)
                                            {
                                                string name = null;
                                                bool isFav;
                                                KeyValuePair<string, JToken> fkkvv;
                                                IEnumerator<KeyValuePair<string, JToken>> kVals = favJSON.GetEnumerator();

                                                while (kVals.MoveNext()) // this will iterate throught the list
                                                {
                                                    isFav = true; // true for fav , false for pending
                                                    fkkvv = kVals.Current; // kkvv contains favourites MSISDN

                                                    if (HikeInstantiation.ViewModel.BlockedHashset.Contains(fkkvv.Key)) // if this user is blocked ignore him
                                                        continue;

                                                    JObject pendingJSON = fkkvv.Value.ToObject<JObject>();
                                                    JToken pToken;

                                                    if (pendingJSON.TryGetValue(ServerJsonKeys.REQUEST_PENDING, out pToken))
                                                    {
                                                        bool rp = false;

                                                        if (pToken != null)
                                                        {
                                                            try
                                                            {
                                                                object o = pToken.ToObject<object>();

                                                                if (o is bool)
                                                                    rp = (bool)o;
                                                            }
                                                            catch { }
                                                        }

                                                        if (rp)
                                                            FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                                                        else
                                                            FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                                                    }
                                                    else if (pendingJSON.TryGetValue(ServerJsonKeys.PENDING, out pToken) && pToken != null)
                                                    {
                                                        if (pToken.ToObject<bool>() == true) // pending is true
                                                        {
                                                            isFav = false;
                                                            FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED);
                                                        }
                                                        else // pending is false
                                                        {
                                                            // in this case friend state should be ignored
                                                            FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
                                                            continue;
                                                        }
                                                    }
                                                    else
                                                        FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.FRIENDS);

                                                    Debug.WriteLine("Fav request, Msisdn : {0} ; isFav : {1}", fkkvv.Key, isFav);
                                                    LoadFavAndPending(isFav, fkkvv.Key); // true for favs
                                                }
                                            }
                                        }

                                        #endregion

                                        #region FACEBOOK AND TWITTER
                                        
                                        if (kkvv.Key == ServerJsonKeys.ACCOUNTS)
                                        {
                                            JObject socialObj = kkvv.Value.ToObject<JObject>();

                                            if (socialObj != null)
                                            {
                                                JToken socialJToken;
                                                socialObj.TryGetValue(ServerJsonKeys.TWITTER, out socialJToken);

                                                if (socialJToken != null) // twitter is present in JSON
                                                {
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.TWITTER_TOKEN, (string)(socialJToken as JObject)["id"]);
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.TWITTER_TOKEN_SECRET, (string)(socialJToken as JObject)["token"]);
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.TW_LOGGED_IN, true);
                                                }
                                                
                                                socialJToken = null;
                                                socialObj.TryGetValue(ServerJsonKeys.FACEBOOK, out socialJToken);

                                                if (socialJToken != null) // facebook is present in JSON
                                                {
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.FB_USER_ID, (string)(socialJToken as JObject)["id"]);
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.FB_ACCESS_TOKEN, (string)(socialJToken as JObject)["token"]);
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.FB_LOGGED_IN, true);
                                                }
                                            }
                                        }

                                        #endregion

                                        #region REWARDS

                                        if (HikeInstantiation.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian dont show rewards
                                        {
                                            if (kkvv.Key == ServerJsonKeys.REWARDS_TOKEN)
                                                HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.REWARDS_TOKEN, kkvv.Value.ToString());
                                            
                                            // whenever this key will come toggle the show rewards thing
                                            if (kkvv.Key == ServerJsonKeys.SHOW_REWARDS)
                                                HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.SHOW_REWARDS, kkvv.Value.ToObject<bool>());

                                            if (kkvv.Key == ServerJsonKeys.MqttMessageTypes.REWARDS)
                                            {
                                                JObject ttObj = kkvv.Value.ToObject<JObject>();

                                                if (ttObj != null)
                                                {
                                                    int rew_val = (int)ttObj[ServerJsonKeys.REWARDS_VALUE];
                                                    HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.REWARDS_VALUE, rew_val);
                                                }
                                            }
                                        }

                                        #endregion

                                        #region Profile Pic

                                        if (kkvv.Key == ServerJsonKeys.ICON)
                                        {
                                            JToken iconToken = kkvv.Value.ToObject<JToken>();
                                            if (iconToken != null)
                                            {
                                                byte[] imageBytes = System.Convert.FromBase64String(iconToken.ToString());
                                                MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, imageBytes, true);
                                            }
                                        }

                                        #endregion

                                        #region LAST SEEN SEETING

                                        if (kkvv.Key == ServerJsonKeys.LASTSEENONOFF)
                                        {
                                            try
                                            {
                                                var val = kkvv.Value.ToString();

                                                if (String.IsNullOrEmpty(val) || Convert.ToBoolean(val))
                                                {
                                                    HikeInstantiation.AppSettings.Remove(AppSettingsKeys.LAST_SEEN_SEETING);
                                                    HikeInstantiation.AppSettings.Save();
                                                }
                                                else
                                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.LAST_SEEN_SEETING, false);
                                            }
                                            catch { }
                                        }

                                        #endregion

                                        #region CHAT BACKGROUNDS

                                        else if (kkvv.Key == ServerJsonKeys.CHAT_BACKGROUND_ARRAY)
                                        {
                                            bool isUpdated = false;

                                            var val = kkvv.Value;

                                            foreach (var obj in val)
                                            {
                                                JObject jObj = (JObject)obj;

                                                var id = (string)jObj[ServerJsonKeys.MSISDN];
                                                bool hasCustomBg = false;
                                                JToken custom;

                                                if (jObj.TryGetValue(ServerJsonKeys.HAS_CUSTOM_BACKGROUND, out custom))
                                                    hasCustomBg = (bool)custom;

                                                if (!hasCustomBg && ChatBackgroundHelper.Instance.UpdateChatBgMap(id, (string)jObj[ServerJsonKeys.BACKGROUND_ID], TimeUtils.GetCurrentTimeStamp(), false))
                                                    isUpdated = true;
                                            }

                                            if (isUpdated)
                                                ChatBackgroundHelper.Instance.SaveChatBgMapToFile();
                                        }

                                        #endregion

                                        #region DP PRIVACY SETTING
                                      
                                        else if (kkvv.Key == HikeConstants.AVATAR)
                                        {
                                            int value = (int)kkvv.Value;
                                            if (value == 2)
                                                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.DISPLAY_PIC_FAV_ONLY, true);
                                        }

                                        #endregion
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT_INFO , Exception : " + ex.StackTrace);
                                    }
                                }

                                // save only for Twitter , FB
                                //HikeInstantiation.WriteToIsoStorageSettings(kv.Key, (oj as JObject).ToString(Newtonsoft.Json.Formatting.None));
                            }// save only tc , invite_token
                            else if (kv.Key == ServerJsonKeys.INVITE_TOKEN || kv.Key == ServerJsonKeys.TOTAL_CREDITS_PER_MONTH)
                            {
                                string val = oj.ToString();
                                Debug.WriteLine("AI :: Value : " + val);

                                if (kv.Key == ServerJsonKeys.INVITE_TOKEN || kv.Key == ServerJsonKeys.TOTAL_CREDITS_PER_MONTH)
                                    HikeInstantiation.WriteToIsoStorageSettings(kv.Key, val);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT_INFO , Exception : " + ex.StackTrace);
                        }
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
            else if (ServerJsonKeys.MqttMessageTypes.ACCOUNT_CONFIG == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    Debug.WriteLine("NETWORK MANAGER : Received account info json : {0}", jsonObj.ToString());
                    
                    #region rewards zone

                    JToken rew;

                    if (HikeInstantiation.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian dont show rewards
                    {
                        if (data.TryGetValue(ServerJsonKeys.REWARDS_TOKEN, out rew))
                            HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.REWARDS_TOKEN, rew.ToString());

                        rew = null;
                     
                        if (data.TryGetValue(ServerJsonKeys.SHOW_REWARDS, out rew))
                            HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.SHOW_REWARDS, rew.ToObject<bool>());
                    }

                    #endregion

                    #region batch push zone
                    
                    JToken pushStatus;

                    if (data.TryGetValue(ServerJsonKeys.ENABLE_PUSH_BATCH_SU, out pushStatus))
                    {
                        try
                        {
                            JArray jArray = (JArray)pushStatus;
                            if (jArray != null)
                            {
                                if (jArray.Count > 1)
                                {
                                    HikeInstantiation.AppSettings[AppSettingsKeys.STATUS_UPDATE_FIRST_SETTING] = (byte)jArray[0];
                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.STATUS_UPDATE_SECOND_SETTING, (byte)jArray[1]);
                                }
                                else if (jArray.Count == 1)
                                {
                                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.STATUS_UPDATE_FIRST_SETTING, (byte)jArray[0]);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT CONFIG, enable push notification, Exception : " + ex.StackTrace);
                        }
                    }

                    #endregion

                    #region moods zone

                    if (data.TryGetValue(AppSettingsKeys.HIDE_CRICKET_MOODS, out rew))
                    {
                        //we are keeping state for hide because by default moods are ON. If server never sends this packet, no
                        //appsetting would ever be stored
                        bool showMoods = rew.ToObject<bool>();
                        HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.HIDE_CRICKET_MOODS, !showMoods);
                    }

                    #endregion
                    
                    #region Invite pop up
                    
                    JToken jtokenMessageId;

                    if (data.TryGetValue(ServerJsonKeys.MESSAGE_ID, out jtokenMessageId))
                    {
                        JToken jtokenShowFreeInvites;
                        string previousId;
                        if ((!HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.INVITE_POPUP_UNIQUEID, out previousId) || previousId != ((string)jtokenMessageId)) && data.TryGetValue(AppSettingsKeys.SHOW_FREE_INVITES, out jtokenShowFreeInvites))
                        {
                            HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.INVITE_POPUP_UNIQUEID, (string)jtokenMessageId);
                            bool showInvite = (bool)jtokenShowFreeInvites;

                            if (showInvite)
                            {
                                JToken jtoken;
                                Object[] popupDataobj = new object[2];
                                //add title to zero place;
                                popupDataobj[0] = data.TryGetValue(ServerJsonKeys.FREE_INVITE_POPUP_TITLE, out jtoken) ? (string)jtoken : null;
                                //add text to first place;
                                popupDataobj[1] = data.TryGetValue(ServerJsonKeys.FREE_INVITE_POPUP_TEXT, out jtoken) ? (string)jtoken : null;
                                HikeInstantiation.AppSettings[AppSettingsKeys.SHOW_POPUP] = popupDataobj;
                            }
                        }
                    }

                    #endregion

                    #region REFRESH IP LIST

                    JToken iplist;
                    
                    if (data.TryGetValue(ServerJsonKeys.IP_KEY, out iplist))
                    {
                        try
                        {
                            JArray jArray = (JArray)iplist;

                            if (jArray != null && jArray.Count > 0)
                            {
                                string[] ips = new string[jArray.Count];

                                for (int i = 0; i < jArray.Count; i++)
                                    ips[i] = (string)jArray[i];

                                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.IP_LIST, ips);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT CONFIG, List IPs, Exception : " + ex.StackTrace);
                        }
                    }

                    #endregion
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT CONFIG , Exception : " + ex.StackTrace);
                }

            }
            #endregion
            #region USER_OPT_IN
            else if (ServerJsonKeys.MqttMessageTypes.USER_OPT_IN == type)
            {
                ProcessUoUjMsgs(jsonObj, true, true, false);
            }
            #endregion
            #region GROUP CHAT RELATED

            #region GROUP_CHAT_JOIN
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                string groupName = string.Empty;
                jsonObj[ServerJsonKeys.TYPE] = ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_JOIN_NEW;
                
                JArray arr = null;
                try
                {
                    arr = (JArray)jsonObj[ServerJsonKeys.DATA];
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
                    grpId = jsonObj[ServerJsonKeys.TO].ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  GROUP_CHAT_JOIN , Exception : " + ex.StackTrace);
                }
                
                GroupManager.Instance.LoadGroupParticipants(grpId);
                ConvMessage convMessage = null;
                List<GroupParticipant> dndList = new List<GroupParticipant>(1);
                GroupChatState gcState = AddGroupmembers(arr, grpId, dndList);

                #region META DATA CHAT BACKGROUND

                JObject metaData = (JObject)jsonObj[ServerJsonKeys.METADATA];
                if (metaData != null)
                {
                    #region chat background
                    try
                    {
                        JObject chatBg = (JObject)metaData[ServerJsonKeys.MqttMessageTypes.CHAT_BACKGROUNDS];
                        if (chatBg != null)
                        {
                            bool hasCustomBg = false;
                            JToken custom;
                            if (chatBg.TryGetValue(ServerJsonKeys.HAS_CUSTOM_BACKGROUND, out custom))
                                hasCustomBg = (bool)custom;

                            if (!hasCustomBg)
                                ChatBackgroundHelper.Instance.UpdateChatBgMap(grpId, (string)chatBg[ServerJsonKeys.BACKGROUND_ID], TimeUtils.GetCurrentTimeStamp());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  GROUP_CHAT_JOIN with chat background, Exception : " + ex.StackTrace);
                    }

                    #endregion

                    #region GROUP NAME

                    JToken gName;

                    if (metaData.TryGetValue(ServerJsonKeys.NAME, out gName))
                        groupName = gName.ToString().Trim();

                    #endregion
                }
                #endregion

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
                            convMessage.Message = convMessage.Message.Replace(";", "") + ";" + dndMsg.Replace(";", "");// as while displaying MEMBERS_JOINED in CT we split on ; for dnd message
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  NEW GROUP , Exception : " + ex.StackTrace);
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
                        o[ServerJsonKeys.TYPE] = ServerJsonKeys.MqttMessageTypes.DND_USER_IN_GROUP;
                        convMessage = new ConvMessage(); // this will be normal DND msg
                        convMessage.Msisdn = grpId;
                        convMessage.MetaDataString = o.ToString(Formatting.None);
                        convMessage.Message = GetDndMsg(dndList);
                        convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                        convMessage.GrpParticipantState = ConvMessage.ParticipantInfoState.DND_USER;
                        convMessage.Timestamp = TimeUtils.GetCurrentTimeStamp();
                    }
                    else
                    {
                        GroupManager.Instance.SaveGroupParticpantsCache(grpId);
                        return;
                    }
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
                    GroupTableUtils.SetGroupAlive(grpId);
                    convMessage = new ConvMessage(jsonObj, false, false); // this will be normal GCJ msg
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
                            convMessage.Message = convMessage.Message.Replace(";", "") + ";" + dndMsg.Replace(";", "");// as while displaying MEMBERS_JOINED in CT we split on ; for dnd message
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
                #endregion

                ConversationListObject obj = MessagesTableUtils.addGroupChatMessage(convMessage, jsonObj, groupName);
                
                if (obj == null)
                    return;

                GroupManager.Instance.SaveGroupParticpantsCache(grpId);
                Debug.WriteLine("NetworkManager", "Group is new");
            }
            #endregion
            #region GROUP_CHAT_NAME CHANGE
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_NAME == type) //Group chat name change
            {
                try
                {
                    string groupName = (string)jsonObj[ServerJsonKeys.DATA];
                    groupName = groupName.Trim();
                    string groupId = (string)jsonObj[ServerJsonKeys.TO];

                    //no self check as server will send packet of group name change if changed by self
                    //we need to use this in case of self name change and unlink account
                    ConversationListObject cObj;
                    if (HikeInstantiation.ViewModel.ConvMap.TryGetValue(groupId, out cObj))
                    {
                        if (cObj.ContactName == groupName || string.IsNullOrEmpty(groupName))//group name is same as previous or empty
                            return;
                    }
                    else
                        return;//group doesn't exists

                    GroupManager.Instance.LoadGroupParticipants(groupId);
                    ConversationTableUtils.updateGroupName(groupId, groupName);
                    ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.GROUP_NAME_CHANGE, jsonObj);
                    
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                    if (obj == null)
                        return;
                    
                    bool goAhead = GroupTableUtils.updateGroupName(groupId, groupName);

                    if (goAhead)
                        HikeInstantiation.ViewModel.ConvMap[groupId].ContactName = groupName;
                    
                    ConversationTableUtils.saveConvObjectList();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCN packet : " + e.StackTrace);
                }
            }
            #endregion
            #region GROUP DISPLAY PIC CHANGE
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_DISPLAY_PIC == type)
            {
                string groupId = (string)jsonObj[ServerJsonKeys.TO];
                string from = (string)jsonObj[ServerJsonKeys.FROM];
                ConversationListObject cObj;

                if (!HikeInstantiation.ViewModel.ConvMap.TryGetValue(groupId, out cObj))
                    return;//if group doesn't exist return
                
                JToken temp;
                jsonObj.TryGetValue(ServerJsonKeys.DATA, out temp);
                
                if (temp == null)
                    return;
                
                string iconBase64 = temp.ToString();

                GroupManager.Instance.LoadGroupParticipants(groupId);
                byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.GROUP_PIC_CHANGED, jsonObj);
                ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                
                if (obj == null)
                    return;

                MiscDBUtil.saveAvatarImage(groupId, imageBytes, true);
            }
            #endregion
            #region GROUP_CHAT_LEAVE
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_LEAVE == type) //Group chat leave
            {
                /*
                * 1. Update Conversation list name if groupName is not set.
                * 2. Update DB.
                * 3. Notify GroupInfo page (if opened)
                * 4. Notify Chat Thread page if opened.
                */
                try
                {
                    string groupId = (string)jsonObj[ServerJsonKeys.TO];
                    string fromMsisdn = (string)jsonObj[ServerJsonKeys.DATA];
                    GroupManager.Instance.LoadGroupParticipants(groupId);
                    GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, fromMsisdn, groupId);
                    
                    if (gp == null || gp.HasLeft)
                        return;

                    ConvMessage convMsg = new ConvMessage(jsonObj, false, false);
                    GroupManager.Instance.SaveGroupParticpantsCache(groupId);
                    ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMsg, false); // grp name will change inside this
                    
                    if (cObj == null)
                        return;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCL packet : " + e.StackTrace);
                }
            }
            #endregion
            #region GROUP_CHAT_END
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_CHAT_END == type) //Group chat end
            {
                try
                {
                    string groupId = (string)jsonObj[ServerJsonKeys.TO];
                    bool goAhead = GroupTableUtils.SetGroupDead(groupId);

                    if (goAhead)
                    {
                        ConvMessage convMessage = new ConvMessage(jsonObj, false, false);
                        ConversationListObject cObj = MessagesTableUtils.addChatMessage(convMessage, false);
                        if (cObj == null)
                            return;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GCE packet : " + e.StackTrace);
                }
            }
            #endregion

            #region GROUP_OWNER_CHANGED
            else if (ServerJsonKeys.MqttMessageTypes.GROUP_OWNER_CHANGED == type) //Group chat end
            {
                try
                {
                    string groupId = (string)jsonObj[ServerJsonKeys.TO];

                    if (!HikeInstantiation.ViewModel.ConvMap.ContainsKey(groupId))//group doesn't exists
                        return;

                    JObject data = (JObject)jsonObj[ServerJsonKeys.DATA];

                    JToken jtoken;

                    if (data.TryGetValue(ServerJsonKeys.MqttMessageTypes.MSISDN_KEYWORD, out jtoken))
                    {
                        string newOwner = (string)jtoken;

                        if (string.IsNullOrEmpty(newOwner))
                            return;

                        GroupTableUtils.UpdateGroupOwner(groupId, newOwner);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception while parsing GOC packet : " + e.StackTrace);
                }
            }
            #endregion

            #endregion
            #region INTERNATIONAL USER
            else if (ServerJsonKeys.MqttMessageTypes.BLOCK_INTERNATIONAL_USER == type)
            {
                ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.INTERNATIONAL_USER, jsonObj);
                cm.Msisdn = msisdn;
                ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                
                if (obj == null)
                    return;
            }
            #endregion
            #region ADD FAVOURITES
            else if (ServerJsonKeys.MqttMessageTypes.ADD_FAVOURITE == type)
            {
                try
                {
                    // if user is blocked simply ignore the request.
                    if (HikeInstantiation.ViewModel.BlockedHashset.Contains(msisdn))
                        return;

                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED);

                    if (friendStatus == FriendsTableUtils.FriendStatusEnum.ALREADY_FRIENDS)
                        return;

                    if (HikeInstantiation.ViewModel.IsPending(msisdn))
                        return;

                    try
                    {
                        ConversationListObject favObj;

                        if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                            favObj = HikeInstantiation.ViewModel.ConvMap[msisdn];
                        else
                        {
                            ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                            string name = null;

                            if (ci == null)
                            {
                                JToken data;
                                if (jsonObj.TryGetValue(ServerJsonKeys.DATA, out data))
                                {
                                    JToken n;
                                    JObject dobj = data.ToObject<JObject>();
                                    if (dobj.TryGetValue(ServerJsonKeys.NAME, out n))
                                        name = n.ToString();
                                }
                            }
                            else
                                name = ci.Name;

                            favObj = new ConversationListObject(msisdn, name, ci != null ? ci.OnHike : true);
                        }

                        // this will ensure there will be one pending request for a particular msisdn
                        HikeInstantiation.ViewModel.PendingRequests[msisdn] = favObj;
                        MiscDBUtil.SavePendingRequests();
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
            else if (ServerJsonKeys.MqttMessageTypes.POSTPONE_FRIEND_REQUEST == type)
            {
                try
                {
                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in PostPone from FAVS : " + e.StackTrace);
                }
            }
            #endregion
            #region REMOVE FAVOURITES
            else if (ServerJsonKeys.MqttMessageTypes.REMOVE_FAVOURITE == type)
            {
                try
                {
                    // if user is blocked ignore his requests
                    if (HikeInstantiation.ViewModel.BlockedHashset.Contains(msisdn))
                        return;

                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Network Manager :: Exception in Remove from Friends: " + e.StackTrace);
                }
            }
            #endregion
            #region REWARDS VALUE CHANGED
            else if (ServerJsonKeys.MqttMessageTypes.REWARDS == type)
            {
                JObject data = null;
                try
                {
                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    int rewards_val = (int)data[ServerJsonKeys.REWARDS_VALUE];
                    HikeInstantiation.WriteToIsoStorageSettings(ServerJsonKeys.REWARDS_VALUE, rewards_val);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Netwok Manager :: Exception in REWARDS : " + e.StackTrace);
                }
            }
            #endregion
            #region STATUS UPDATE
            else if (ServerJsonKeys.MqttMessageTypes.STATUS_UPDATE == type)
            {
                StatusMessage sm = null;
                ConvMessage cm = ProcessStatusUpdate(msisdn, jsonObj, out sm);

                if (cm != null)
                {
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                    
                    // if conversation  with this user exists then only show him status updates on chat thread and conversation screen
                    if (obj != null)
                    {
                        sm.MsgId = cm.MessageId;
                        StatusMsgsTable.UpdateMsgId(sm);
                    }
                }
            }
            #endregion
            #region DELETE STATUS
            else if (ServerJsonKeys.MqttMessageTypes.DELETE_STATUS_UPDATE == type)
            {
                JObject data = null;

                try
                {
                    if (HikeInstantiation.ViewModel.BlockedHashset.Contains(msisdn)) // if this user is blocked simply ignore him 
                        return;

                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    string id = (string)data[ServerJsonKeys.STATUS_ID];
                    long msgId = StatusMsgsTable.DeleteStatusMsg(id);

                    if (msgId > 0) // delete only if msgId is greater than 0
                    {
                        MessagesTableUtils.deleteMessage(msgId);

                        // if conversation from this user exists
                        if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                        {
                            ConversationListObject co = HikeInstantiation.ViewModel.ConvMap[msisdn];

                            // if last msg is status update and its of same id which is about to get deleted, then only proceed
                            if (co.IsLastMsgStatusUpdate && co.LastMsgId == msgId)
                            {
                                ConvMessage cm = MessagesTableUtils.getLastMessageForMsisdn(msisdn);

                                if (cm != null)
                                {
                                    co.LastMessage = cm.Message;
                                    co.LastMsgId = cm.MessageId;
                                    co.MessageStatus = cm.MessageStatus;

                                    if (cm.FileAttachment != null)
                                    {
                                        if (cm.FileAttachment.ContentType.Contains(FTBasedConstants.IMAGE))
                                            co.LastMessage = AppResources.Image_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(FTBasedConstants.AUDIO))
                                            co.LastMessage = AppResources.Audio_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(FTBasedConstants.VIDEO))
                                            co.LastMessage = AppResources.Video_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(FTBasedConstants.CT_CONTACT))
                                            co.LastMessage = AppResources.ContactTransfer_Text;
                                        else if (cm.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION))
                                            co.LastMessage = AppResources.Location_Txt;
                                        else
                                            co.LastMessage = AppResources.UnknownFile_txt;

                                        co.TimeStamp = cm.Timestamp;
                                    }
                                    else // check here nudge , notification , status update
                                    {
                                        // if metadata string 
                                        if (!string.IsNullOrEmpty(cm.MetaDataString))
                                        {
                                            // NUDGE
                                            if (cm.MetaDataString.Contains("poke"))
                                                co.LastMessage = AppResources.Nudge;

                                            // STATUS UPDATE
                                            else if (cm.MetaDataString.Contains(ServerJsonKeys.MqttMessageTypes.STATUS_UPDATE))
                                            {
                                                JObject jdata = null;

                                                try
                                                {
                                                    jdata = JObject.Parse(cm.MetaDataString);
                                                }
                                                catch (Exception e) { }

                                                if (jdata != null)
                                                {
                                                    JToken val;
                                                    JObject ddata = jdata[ServerJsonKeys.DATA] as JObject;

                                                    // profile pic update
                                                    if (ddata.TryGetValue(ServerJsonKeys.PROFILE_UPDATE, out val) && true == (bool)val)
                                                        co.LastMessage = "\"" + AppResources.Update_Profile_Pic_txt + "\"";
                                                    else // status , mood update
                                                        co.LastMessage = "\"" + cm.Message + "\"";
                                                }
                                            }
                                            else // NOTIFICATION AND NORMAL MSGS
                                                co.LastMessage = cm.Message;
                                        }
                                    }
                                }
                                else // there are no msgs left remove the conversation from db and map
                                {
                                    ConversationTableUtils.deleteConversation(msisdn);
                                    HikeInstantiation.ViewModel.ConvMap.Remove(msisdn);
                                }

                                ConversationTableUtils.saveConvObjectList();
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception in DELETE STATUS : " + e.StackTrace);
                }
            }
            #endregion
            #region SERVER TIMESTAMP
            else if (type == SERVER_TIMESTAMP)
            {
                long timediff = (long)jsonObj[ServerJsonKeys.TIMESTAMP] - TimeUtils.GetCurrentTimeStamp();
                HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.TIME_DIFF_EPOCH, timediff);
            }
            #endregion
            #region STICKER
            else if (type == STICKER)
            {
                try
                {
                    string subType = (string)jsonObj[ServerJsonKeys.SUB_TYPE];
                    JObject jsonData = (JObject)jsonObj[ServerJsonKeys.DATA];

                    //do same for category as well as subcategory
                    if (subType == ServerJsonKeys.ADD_STICKER || subType == ServerJsonKeys.ADD_CATEGORY)
                    {
                        string category = (string)jsonData[HikeConstants.CATEGORY_ID];
                        StickerHelper.UpdateHasMoreMessages(category, true, true);
                    }
                    else if (subType == ServerJsonKeys.REMOVE_STICKER)
                    {
                        string category = (string)jsonData[HikeConstants.CATEGORY_ID];
                        JArray jarray = (JArray)jsonData["stIds"];
                        List<string> listStickers = new List<string>();

                        for (int i = 0; i < jarray.Count; i++)
                            listStickers.Add((string)jarray[i]);

                        StickerHelper.DeleteSticker(category, listStickers);
                        RecentStickerHelper.DeleteSticker(category, listStickers);
                    }
                    else if (subType == ServerJsonKeys.REMOVE_CATEGORY)
                    {
                        string category = (string)jsonData[HikeConstants.CATEGORY_ID];
                        StickerHelper.DeleteCategory(category);
                        RecentStickerHelper.DeleteCategory(category);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception in ADD Sticker: " + e.StackTrace);
                }
            }
            #endregion
            #region Pro Tips

            else if (ServerJsonKeys.MqttMessageTypes.PRO_TIPS == type)
            {
                JObject data = null;

                try
                {
                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    var id = (string)data[ServerJsonKeys.PRO_TIP_ID];
                    var header = (string)data[ServerJsonKeys.PRO_TIP_HEADER];
                    var text = (string)data[ServerJsonKeys.PRO_TIP_TEXT];

                    var imageUrl = String.Empty;
                    try
                    {
                        imageUrl = (string)data[ServerJsonKeys.PRO_TIP_IMAGE];
                    }
                    catch
                    {
                        imageUrl = String.Empty;
                    }

                    var base64Image = String.Empty;
                    try
                    {
                        base64Image = (string)data[ServerJsonKeys.THUMBNAIL];
                    }
                    catch
                    {
                        base64Image = String.Empty;
                    }

                    ProTipHelper.Instance.AddProTip(id, header, text, imageUrl, base64Image);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: ProTip, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }
            }

            #endregion
            #region CHAT BACKGROUND
            else if (ServerJsonKeys.MqttMessageTypes.CHAT_BACKGROUNDS == type)
            {
                try
                {
                    ConvMessage cm;
                    var ts = (long)jsonObj[ServerJsonKeys.TIMESTAMP];
                    if (ts > 0)
                    {
                        long timedifference;
                        if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.TIME_DIFF_EPOCH, out timedifference))
                            ts = ts - timedifference;
                    }

                    var to = (string)jsonObj[ServerJsonKeys.TO];

                    if (!String.IsNullOrEmpty(to) && Utility.IsGroupConversation(to))
                        GroupManager.Instance.LoadGroupParticipants(to);

                    if (!String.IsNullOrEmpty(to) && Utility.IsGroupConversation(to) && !GroupManager.Instance.GroupParticpantsCache.ContainsKey(to))
                    {
                        Debug.WriteLine("OnMesage: Chat backgrounds: Group not found - {0}", to);
                        return;
                    }

                    var sender = !String.IsNullOrEmpty(to) ? to : msisdn;

                    var data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    var bgId = (string)data[ServerJsonKeys.BACKGROUND_ID];

                    ChatThemeData bg = null;
                    if (ChatBackgroundHelper.Instance.ChatBgMap.TryGetValue(sender, out bg))
                    {
                        if (bg.Timestamp > ts || bg.BackgroundId == bgId)
                            return;
                    }

                    bool hasCustomBg = false;
                    JToken custom;
                    if (data.TryGetValue(ServerJsonKeys.HAS_CUSTOM_BACKGROUND, out custom))
                        hasCustomBg = (bool)custom;

                    if (!hasCustomBg && ChatBackgroundHelper.Instance.BackgroundIDExists(bgId))
                    {
                        if (!String.IsNullOrEmpty(to) && GroupManager.Instance.GroupParticpantsCache.ContainsKey(to))
                        {
                            //if group chat, message text will be set in the constructor else it will be updated by MessagesTableUtils.addChatMessage
                            cm = new ConvMessage(ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED, jsonObj, ts);
                        }
                        else
                        {
                            cm = new ConvMessage(String.Empty, msisdn, ts, ConvMessage.State.RECEIVED_UNREAD);
                            cm.GrpParticipantState = ConvMessage.ParticipantInfoState.CHAT_BACKGROUND_CHANGED;
                        }

                        cm.MetaDataString = "{\"t\":\"cbg\"}";
                    }
                    else
                        return;

                    if (hasCustomBg || !ChatBackgroundHelper.Instance.BackgroundIDExists(bgId))
                        cm.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;
                    
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false, sender);

                    if (!hasCustomBg)
                        ChatBackgroundHelper.Instance.UpdateChatBgMap(sender, bgId, ts);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: Chat Background, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }
            }
            #endregion
            #region App Update

            else if (ServerJsonKeys.MqttMessageTypes.APP_UPDATE == type)
            {
                JObject data = null;

                try
                {
                    data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    var devType = (string)data[ServerJsonKeys.DEVICE_TYPE_KEY];

                    if (devType != "windows")
                        return;

                    var version = (string)data[ServerJsonKeys.VERSION];

                    if (Utility.CompareVersion(version, HikeInstantiation.CurrentVersion) <= 0)
                        return;

                    bool isCritical = false;
                    try
                    {
                        isCritical = (bool)data[ServerJsonKeys.CRITICAL];
                    }
                    catch
                    {
                        isCritical = false;
                    }

                    var message = String.Empty;
                    try
                    {
                        message = (string)data[ServerJsonKeys.TEXT_UPDATE_MSG];
                    }
                    catch
                    {
                        message = isCritical ? AppResources.CRITICAL_UPDATE_TEXT : AppResources.NORMAL_UPDATE_TEXT;
                    }

                    JObject obj = new JObject();
                    obj.Add(ServerJsonKeys.CRITICAL, isCritical);
                    obj.Add(ServerJsonKeys.TEXT_UPDATE_MSG, message);
                    obj.Add(ServerJsonKeys.VERSION, version);

                    HikeInstantiation.WriteToIsoStorageSettings(AppSettingsKeys.NEW_UPDATE_AVAILABLE, obj.ToString(Newtonsoft.Json.Formatting.None));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: APP UPDATE, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }
            }

            #endregion
            #region IC REMOVE
            else if (type == ICON_REMOVE)
            {
                MiscDBUtil.DeleteImageForMsisdn(msisdn);
            }
            #endregion
            #region Server Tips
            else if (TIPS_POPUP == type)
            {
                try
                {
                    JToken subtype = jsonObj[ServerJsonKeys.SUB_TYPE];
                    JObject data = (JObject)jsonObj[ServerJsonKeys.DATA];
                    JToken headertext;

                    if (!data.TryGetValue(TIPS_HEADER, out headertext))
                        headertext = String.Empty;

                    JToken bodyText;

                    if (!data.TryGetValue(TIPS_BODY, out bodyText))
                        bodyText = String.Empty;

                    TipManager.Instance.AddTip((string)subtype, (string)headertext, (string)bodyText, (string)data[TIPS_ID]);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NetworkManager :: OnMessage : TipsException " + e.StackTrace);
                }
            }
            #endregion
        }

        /// <summary>
        /// Process bulk packet
        /// </summary>
        /// <param name="jsonObj"></param>
        private void ProcessBulkPacket(JObject jsonObj)
        {
            //try
            //{
            var jData = (JObject)jsonObj[ServerJsonKeys.DATA];
            JArray msgs = (JArray)jData[ServerJsonKeys.MESSAGES];

            Dictionary<string, MsisdnBulkData> dictBulkData = new Dictionary<string, MsisdnBulkData>();
        
            for (int i = 0; i < msgs.Count; i++)
                ProcessBulkIndividualMsg((JObject)msgs[i], dictBulkData);

            if (dictBulkData.Count > 0)
            {
                foreach (MsisdnBulkData msisdnBulkData in dictBulkData.Values)
                {
                    if (msisdnBulkData.ListMessages.Count > 0)
                    {
                        MessagesTableUtils.FilterDuplicateMessage(msisdnBulkData.ListMessages);

                        //after filtering it may be zero
                        if (msisdnBulkData.ListMessages.Count > 0)
                        {
                            bool success = MessagesTableUtils.BulkInsertMessage(msisdnBulkData.ListMessages);
                            if (success)
                            {
                                ConversationListObject obj;
                                bool updateConversation = false;
                                foreach (ConvMessage convMessage in msisdnBulkData.ListMessages)
                                {
                                    if (convMessage.StatusUpdateObj != null)
                                    {
                                        convMessage.StatusUpdateObj.MsgId = convMessage.MessageId;
                                        StatusMsgsTable.UpdateMsgId(convMessage.StatusUpdateObj);
                                    }
                                    else if (convMessage.GrpParticipantState == ConvMessage.ParticipantInfoState.PIN_MESSAGE)
                                    {
                                        if (HikeInstantiation.ViewModel.ConvMap.TryGetValue(convMessage.Msisdn, out obj))
                                        {
                                            MessagesTableUtils.ProcessConversationMetadata(convMessage, obj);
                                            updateConversation = true;
                                        }
                                    }
                                    else
                                    {
                                        if (convMessage.FileAttachment != null && (convMessage.FileAttachment.ContentType.Contains(FTBasedConstants.CONTACT)
                      || convMessage.FileAttachment.ContentType.Contains(FTBasedConstants.LOCATION)))
                                        {
                                            convMessage.FileAttachment.FileState = Attachment.AttachmentState.COMPLETED;
                                        }

                                        if (convMessage.FileAttachment != null)
                                            MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                                    }
                                }
                                
                                ConvMessage lastMessage = msisdnBulkData.ListMessages[msisdnBulkData.ListMessages.Count - 1];
                                obj = MessagesTableUtils.UpdateConversationList(lastMessage, false);

                                if (obj == null)
                                    continue;

                                if (msisdnBulkData.ListMessages.Count > 1)
                                {
                                    obj.UnreadCounter += msisdnBulkData.ListMessages.Count - 1;// -1 because 1 count is already incremented by adding last message
                                    updateConversation = true;
                                }

                                if (updateConversation)
                                    ConversationTableUtils.updateConversation(obj);
                            }

                        }
                    }

                    if (msisdnBulkData.LastDeliveredMsgId > 0)
                        MiscDBUtil.UpdateBulkMessageDBsDeliveredStatus(msisdnBulkData.Msisdn, msisdnBulkData.LastDeliveredMsgId);

                    if (msisdnBulkData.LastReadMsgId > 0)
                        MiscDBUtil.UpdateBulkMessageDBsReadStatus(msisdnBulkData.Msisdn, msisdnBulkData.LastReadMsgId, msisdnBulkData.LastReadMsgId, msisdnBulkData.ReadByArray);
                }
            }
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("NetworkManager::OnMessage:BulkMessages,Exception:{0},StackTrace:{1}", ex.Message, ex.StackTrace);
            //}
        }

        /// <summary>
        /// Processes individual packets in bulk packets
        /// </summary>
        /// <param name="jsonObj"></param>
        /// <param name="dictBulkData"></param>
        private void ProcessBulkIndividualMsg(JObject jsonObj, Dictionary<string, MsisdnBulkData> dictBulkData)
        {

            if (jsonObj == null)
                return;
            try
            {
                string type = (string)jsonObj[ServerJsonKeys.TYPE];

                if (type == MESSAGE)
                {
                    try
                    {
                        ConvMessage convMessage = new ConvMessage(jsonObj);
                        if (Utility.IsGroupConversation(convMessage.Msisdn))
                            GroupManager.Instance.LoadGroupParticipants(convMessage.Msisdn);

                        convMessage.MessageStatus = ConvMessage.State.RECEIVED_UNREAD;
                        MessagesTableUtils.UpdateConvMessageText(convMessage, string.Empty);

                        MsisdnBulkData msisdnBulkData;
                        if (!dictBulkData.TryGetValue(convMessage.Msisdn, out msisdnBulkData))
                        {
                            msisdnBulkData = new MsisdnBulkData(convMessage.Msisdn);
                            dictBulkData[convMessage.Msisdn] = msisdnBulkData;
                        }
                        msisdnBulkData.ListMessages.Add(convMessage);

                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  MESSAGE convmessage, Exception : " + ex.StackTrace);
                        return;
                    }

                }
                #region DELIVERY_REPORT
                else if (DELIVERY_REPORT == type) // this handles the case when msg with msgId is recieved by the recipient but is unread
                {

                    try
                    {
                        string id = (string)jsonObj[ServerJsonKeys.DATA];
                        string msisdn;
                        JToken msisdnToken;
                        jsonObj.TryGetValue(ServerJsonKeys.TO, out msisdnToken);
                        if (msisdnToken != null)
                            msisdn = msisdnToken.ToString();
                        else
                            msisdn = (string)jsonObj[ServerJsonKeys.FROM];
                        long msgID = Int64.Parse(id);
                        MsisdnBulkData msisdnBulkData;
                        if (!dictBulkData.TryGetValue(msisdn, out msisdnBulkData))
                        {
                            msisdnBulkData = new MsisdnBulkData(msisdn);
                            dictBulkData[msisdn] = msisdnBulkData;
                        }
                        msisdnBulkData.LastDeliveredMsgId = msgID > msisdnBulkData.LastDeliveredMsgId ? msgID : msisdnBulkData.LastDeliveredMsgId;
                    }
                    catch (FormatException e)
                    {
                        Debug.WriteLine("Network Manager:: Delivery Report, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), e.StackTrace);
                        return;
                    }
                }
                #endregion
                #region MESSAGE_READ
                else if (MESSAGE_READ == type) // Message read by recipient
                {
                    JArray msgIds = null;

                    try
                    {
                        msgIds = (JArray)jsonObj["d"];
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  MESSAGE_READ, Exception : " + ex.StackTrace);
                        return;
                    }
                    if (msgIds == null || msgIds.Count == 0)
                    {
                        Debug.WriteLine("NETWORK MANAGER", "Update Error : Message id Array is empty or null . Check problem");
                        return;
                    }

                    string readBy = (string)jsonObj[ServerJsonKeys.FROM];
                    string msisdn;
                    JToken msisdnToken;
                    jsonObj.TryGetValue(ServerJsonKeys.TO, out msisdnToken);
                    if (msisdnToken != null)
                        msisdn = msisdnToken.ToString();
                    else
                        msisdn = readBy;

                    MsisdnBulkData msisdnBulkData;
                    if (!dictBulkData.TryGetValue(msisdn, out msisdnBulkData))
                    {
                        msisdnBulkData = new MsisdnBulkData(msisdn);
                        dictBulkData[msisdn] = msisdnBulkData;
                    }
                    for (int i = 0; i < msgIds.Count; i++)
                    {
                        long msgID = Int64.Parse(msgIds[i].ToString());
                        if (msgID > msisdnBulkData.LastReadMsgId)
                        {
                            msisdnBulkData.LastReadMsgId = msgID;
                            if (Utility.IsGroupConversation(msisdn))
                                msisdnBulkData.ReadByArray = new JArray() { readBy };//if new msg id is greater than existing msg id then create new readby array
                        }
                        else if (msgID == msisdnBulkData.LastReadMsgId && Utility.IsGroupConversation(msisdn))
                        {
                            if (!msisdnBulkData.ReadByArray.Contains(readBy))
                                msisdnBulkData.ReadByArray.Add(readBy);
                        }
                    }

                }
                #endregion
                #region STATUS UPDATE
                else if (ServerJsonKeys.MqttMessageTypes.STATUS_UPDATE == type)
                {
                    string msisdn = (string)jsonObj[ServerJsonKeys.FROM];
                    StatusMessage sm = null;
                    ConvMessage cm = ProcessStatusUpdate(msisdn, jsonObj, out sm);
                    if (cm != null)
                    {
                        cm.StatusUpdateObj = sm;//to update after getting message id after storing in db
                        MsisdnBulkData msisdnBulkData;
                        if (!dictBulkData.TryGetValue(cm.Msisdn, out msisdnBulkData))
                        {
                            msisdnBulkData = new MsisdnBulkData(cm.Msisdn);
                            dictBulkData[cm.Msisdn] = msisdnBulkData;
                        }
                        msisdnBulkData.ListMessages.Add(cm);
                    }
                }
                #endregion
                else
                {
                    onMessage(jsonObj.ToString(Newtonsoft.Json.Formatting.None));
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse type, Exception : " + ex.StackTrace);
                return;
            }
            return;
        }

        private ConvMessage ProcessStatusUpdate(string msisdn, JObject jsonObj, out StatusMessage sm)
        {
            sm = null;
            // if this user is already blocked simply ignore his status
            if (HikeInstantiation.ViewModel.BlockedHashset.Contains(msisdn))
                return null;

            JObject data = null;

            try
            {
                data = (JObject)jsonObj[ServerJsonKeys.DATA];
                JToken val;
                string iconBase64 = null;

                if (data.TryGetValue(ServerJsonKeys.THUMBNAIL, out val) && val != null)
                    iconBase64 = val.ToString();

                val = null;
                long ts = 0;

                if (jsonObj.TryGetValue(ServerJsonKeys.TIMESTAMP, out val) && val != null)
                {
                    ts = val.ToObject<long>();
                    long tsCorrection;

                    if (HikeInstantiation.AppSettings.TryGetValue(AppSettingsKeys.TIME_DIFF_EPOCH, out tsCorrection))
                        ts -= tsCorrection;
                }

                val = null;
                string id = null;
                JToken idToken;

                if (data.TryGetValue(ServerJsonKeys.STATUS_ID, out idToken))
                    id = idToken.ToString();

                #region HANDLE PROFILE PIC UPDATE

                if (data.TryGetValue(ServerJsonKeys.PROFILE_UPDATE, out val) && true == (bool)val)
                {
                    sm = new StatusMessage(msisdn, id, StatusMessage.StatusType.PROFILE_PIC_UPDATE, id, ts,
                        StatusUpdateHelper.Instance.IsTwoWayFriend(msisdn), -1, -1, 0, true);
                    idToken = null;

                    if (iconBase64 != null)
                    {
                        byte[] imageBytes = System.Convert.FromBase64String(iconBase64);

                        if (!StatusMsgsTable.InsertStatusMsg(sm, true))//will return false if status already exists
                            return null;
                        
                        MiscDBUtil.saveProfileImages(msisdn, imageBytes, sm.ServerId);
                        jsonObj[ServerJsonKeys.PROFILE_PIC_ID] = sm.ServerId;
                    }
                }

                #endregion

                #region HANDLE TEXT UPDATE

                else if (data.TryGetValue(ServerJsonKeys.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                {
                    int moodId = -1;
                    int tod = 0;

                    if (data[ServerJsonKeys.MOOD] != null)
                    {
                        string moodId_String = data[ServerJsonKeys.MOOD].ToString();

                        if (!string.IsNullOrEmpty(moodId_String))
                        {
                            int.TryParse(moodId_String, out moodId);
                            moodId = Utility.GetRecieverMoodId(moodId);

                            try
                            {
                                if (moodId > 0 && data[ServerJsonKeys.TIME_OF_DAY] != null && !String.IsNullOrWhiteSpace(data[ServerJsonKeys.TIME_OF_DAY].ToString()))
                                    tod = data[ServerJsonKeys.TIME_OF_DAY].ToObject<int>();
                            }
                            catch (Exception ex)
                            {
                                tod = 0;
                                Debug.WriteLine("NetworkManager :: Exception in TextStatus Updates : " + ex.StackTrace);
                            }
                        }
                    }

                    sm = new StatusMessage(msisdn, val.ToString(), StatusMessage.StatusType.TEXT_UPDATE, id, ts,
                        StatusUpdateHelper.Instance.IsTwoWayFriend(msisdn), -1, moodId, tod, true);
                    
                    if (!StatusMsgsTable.InsertStatusMsg(sm, true))//will return false if status already exists
                        return null;
                }

                #endregion

                ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.STATUS_UPDATE, jsonObj, ts);
                cm.Msisdn = msisdn;
                return cm;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Network Manager :: Exception in STATUS UPDATES : " + e.StackTrace);
            }

            return null;
        }

        private void LoadFavAndPending(bool isFav, string msisdn)
        {
            if (msisdn == null)
                return;

            if (isFav)
            {
                if (HikeInstantiation.ViewModel.Isfavourite(msisdn))
                    return;

                ConversationListObject favObj = null;

                if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                    favObj = HikeInstantiation.ViewModel.ConvMap[msisdn];
                else
                {
                    // here no need to call cache
                    ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    favObj = new ConversationListObject(msisdn, ci != null ? ci.Name : null, ci != null ? ci.OnHike : true);
                }

                HikeInstantiation.ViewModel.FavList.Add(favObj);
                MiscDBUtil.SaveFavourites();
                MiscDBUtil.SaveFavourites(favObj);
                int count = 0;
                HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                HikeInstantiation.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
            }
            else // pending case
            {
                if (HikeInstantiation.ViewModel.IsPending(msisdn))
                    return;

                ConversationListObject favObj = null;

                if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(msisdn))
                    favObj = HikeInstantiation.ViewModel.ConvMap[msisdn];
                else
                {
                    // no need to call cache here
                    ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
                    favObj = new ConversationListObject(msisdn, ci != null ? ci.Name : null, ci != null ? ci.OnHike : true);
                }

                HikeInstantiation.ViewModel.PendingRequests[favObj.Msisdn] = favObj;
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

        private void ProcessUoUjMsgs(JObject jsonObj, bool isOptInMsg, bool isUserInContactList, bool isRejoin)
        {
            int credits = 0;

            string ms = null;
            try
            {
                JObject data = (JObject)jsonObj[ServerJsonKeys.DATA];
                ms = (string)data[ServerJsonKeys.MSISDN];
                try
                {
                    credits = (int)data["credits"];
                }
                catch (Exception e)
                {
                    Debug.WriteLine("NETWORK MANAGER :: Exception in ProcessUoUjMsgs : " + e.StackTrace);
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
                if (!isOptInMsg || HikeInstantiation.ViewModel.ConvMap.ContainsKey(ms)) // if this is UJ or conversation has this msisdn go in
                {
                    ConvMessage cm = null;
                    
                    if (isOptInMsg)
                        cm = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);
                    else
                        cm = new ConvMessage(isRejoin ? ConvMessage.ParticipantInfoState.USER_REJOINED : ConvMessage.ParticipantInfoState.USER_JOINED, jsonObj);
                    
                    cm.Msisdn = ms;
                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false);
                    if (obj == null)
                    {
                        GroupManager.Instance.SaveGroupParticpantsCache(cm.Msisdn);
                        return;
                    }

                    if (credits > 0)
                    {
                        string text = string.Format(AppResources.CREDITS_EARNED, credits);
                        JObject o = new JObject();
                        o.Add("t", "credits_gained");

                        ConvMessage cmCredits = new ConvMessage(ConvMessage.ParticipantInfoState.CREDITS_GAINED, o);
                        cmCredits.Message = text;
                        cmCredits.Msisdn = ms;

                        obj = MessagesTableUtils.addChatMessage(cmCredits, false);
                    }
                }
            }

            // UPDATE group cache
            foreach (string key in GroupManager.Instance.GroupParticpantsCache.Keys)
            {
                List<GroupParticipant> l = GroupManager.Instance.GroupParticpantsCache[key];
                GroupParticipant gp = l.Find(x => x.Msisdn == ms);

                if (gp != null)
                {
                    if (isOptInMsg)
                    {
                        ConvMessage convMsg = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);

                        convMsg.Msisdn = key;
                        convMsg.Message = ms;
                        ConversationListObject co = MessagesTableUtils.addChatMessage(convMsg, false);
                        if (co == null)
                        {
                            GroupManager.Instance.SaveGroupParticpantsCache();
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
                                GroupManager.Instance.SaveGroupParticpantsCache();
                                return;
                            }
                        }
                    }
                    else
                        gp.IsOnHike = true;

                    gp.HasOptIn = true;
                }
            }

            GroupManager.Instance.SaveGroupParticpantsCache();
        }

        /*
         * This function performs 3 roles
         * 1. Same GCJ is received by user who created group
         * 2. New GCJ is received
         * 3. User is added to group.
         */
        private GroupChatState AddGroupmembers(JArray arr, string grpId, List<GroupParticipant> dndList)
        {
            if (!HikeInstantiation.ViewModel.ConvMap.ContainsKey(grpId)) // if its a new group always return true
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

        /// <summary>
        /// Update message db with status sent delivered read for set of messages
        /// </summary>
        /// <param name="fromUser"></param>
        /// <param name="ids"></param>
        /// <param name="status"></param>
        /// <param name="sender"></param>
        private void updateDbBatch(string fromUser, long[] ids, int status, string sender)
        {
            if (ids == null || ids.Length == 0)
                return;
            string msisdn = MessagesTableUtils.updateAllMsgStatus(fromUser, ids, status);//msisdn would be null for multiple read by

            // To update conversation object , we have to check if ids [] contains last msg id
            if (HikeInstantiation.ViewModel.ConvMap.ContainsKey(fromUser))
            {
                ConversationListObject co = HikeInstantiation.ViewModel.ConvMap[fromUser];
                bool containsMessageId = false;
                long maxReadId = 0;

                for (int i = 0; i < ids.Length; i++)
                {
                    if (ids[i] > maxReadId)
                        maxReadId = ids[i];

                    if (co.LastMsgId == ids[i])
                        containsMessageId = true;
                }

                if (containsMessageId)
                    ConversationTableUtils.updateLastMsgStatus(co.LastMsgId, msisdn, status);//if msisdn null then conversastionlistObj is alreadyUpdated

                if (Utility.IsGroupConversation(fromUser))
                    GroupTableUtils.UpdateReadBy(fromUser, maxReadId, sender);
            }
        }
    }
}