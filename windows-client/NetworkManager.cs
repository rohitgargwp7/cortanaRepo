﻿using System;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using windows_client.Model;
using windows_client.DbUtils;
using windows_client.utils;
using System.Windows;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using windows_client.Misc;
using windows_client.Languages;
using windows_client.ViewModel;
using Microsoft.Phone.Shell;
using windows_client.utils.Sticker_Helper;

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
        public static readonly string MULTIPLE_INVITE = "mi";

        public static readonly string ICON = "ic";

        public static readonly string SERVER_TIMESTAMP = "sts";
        public static readonly string LAST_SEEN = "ls";

        public static readonly string REQUEST_DISPLAY_PIC = "rdp";

        public static readonly string STICKER = "stk";

        public static readonly string ACTION = "action";

        public static readonly string ICON_REMOVE = "icr";

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
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse, Exception : " + ex.StackTrace);
                return;
            }
            string type = null;
            try
            {
                type = (string)jsonObj[HikeConstants.TYPE];
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse type, Exception : " + ex.StackTrace);
                return;
            }
            string msisdn = null;
            try
            {
                msisdn = (string)jsonObj[HikeConstants.FROM];
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine("NetworkManager ::  onMessage : json Parse from, Exception : " + ex.StackTrace);
                return;
            }

            #region MESSAGE
            if (MESSAGE == type)  // this represents msg from another client through tornado(python) server.
            {
                try
                {
                    bool isPush = true;
                    JToken pushJToken;
                    var jData = (JObject)jsonObj[HikeConstants.DATA];
                    if (jData.TryGetValue(HikeConstants.PUSH, out pushJToken))
                        isPush = (Boolean)pushJToken;

                    ConvMessage convMessage = null;
                    try
                    {
                        convMessage = new ConvMessage(jsonObj);
                        if (Utils.isGroupConversation(convMessage.Msisdn))
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

                    if (convMessage.FileAttachment != null && (convMessage.FileAttachment.ContentType.Contains(HikeConstants.CONTACT)
                        || convMessage.FileAttachment.ContentType.Contains(HikeConstants.LOCATION)))
                    {
                        convMessage.FileAttachment.FileState = Attachment.AttachmentState.COMPLETED;
                    }
                    else if (convMessage.FileAttachment != null && !App.appSettings.Contains(App.AUTO_DOWNLOAD_SETTING))
                    {
                        FileTransfers.FileTransferManager.Instance.DownloadFile(convMessage.Msisdn, convMessage.MessageId.ToString(), convMessage.FileAttachment.FileKey, convMessage.FileAttachment.ContentType, convMessage.FileAttachment.FileSize);
                    }

                    if (convMessage.FileAttachment != null)
                    {
                        MiscDBUtil.saveAttachmentObject(convMessage.FileAttachment, convMessage.Msisdn, convMessage.MessageId);
                    }
                    object[] vals = new object[3];

                    vals[0] = convMessage;
                    vals[1] = obj;
                    vals[2] = isPush;
                    pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
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
                string grpId = "";
                try
                {
                    grpId = (string)jsonObj[HikeConstants.TO];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  REQUEST_DISPLAY_PIC, Exception : " + ex.StackTrace);
                }

                App.ViewModel.AddGroupPicForUpload(grpId);
            }
            #endregion
            #region START_TYPING
            else if (START_TYPING == type) /* Start Typing event received*/
            {
                string sentTo = "";
                try
                {
                    // If not null then this is group id
                    sentTo = (string)jsonObj[HikeConstants.TO];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  START_TYPING, Exception : " + ex.StackTrace);
                }

                var number = String.IsNullOrEmpty(sentTo) ? msisdn : sentTo;

                if (App.ViewModel.ConvMap != null && App.ViewModel.ConvMap.ContainsKey(number)
                    && App.ViewModel.ConvMap[number].IsHidden && !App.ViewModel.IsHiddenModeActive)
                    return;

                object[] vals = new object[2];
                vals[0] = msisdn;
                vals[1] = sentTo;
                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.TYPING_CONVERSATION, vals);
                return;
            }
            #endregion
            #region LAST_SEEN
            else if (LAST_SEEN == type) /* Last Seen received */
            {
                long lastSeen = 0;

                try
                {
                    var data = jsonObj[HikeConstants.DATA];
                    lastSeen = (long)data[HikeConstants.LASTSEEN];

                    if (lastSeen > 0)
                    {
                        long timedifference;
                        if (App.appSettings.TryGetValue(HikeConstants.AppSettings.TIME_DIFF_EPOCH, out timedifference))
                            lastSeen = lastSeen - timedifference;
                    }

                    if (lastSeen == -1)
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, 0);
                    else if (lastSeen == 0)
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, TimeUtils.getCurrentTimeStamp());
                    else
                        FriendsTableUtils.SetFriendLastSeenTSToFile(msisdn, lastSeen);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  Last Seen :  TimeStamp, Exception : " + ex.StackTrace);
                }

                object[] vals = new object[2];
                vals[0] = msisdn;
                vals[1] = lastSeen;

                if (msisdn != null)
                    this.pubSub.publish(HikePubSub.LAST_SEEN, vals);

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
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  SMS_CREDITS, Exception : " + ex.StackTrace);
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
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  SERVER_REPORT, Exception : " + ex.StackTrace);
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
                    Debug.WriteLine("Network Manager:: Delivery Report, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), e.StackTrace);
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
                {
                    ids[i] = Int64.Parse(msgIds[i].ToString());
                }
                object[] vals = new object[3];
                vals[0] = ids;
                vals[1] = msisdnToCheck;
                vals[2] = msisdn;
                updateDbBatch(msisdnToCheck, ids, (int)ConvMessage.State.SENT_DELIVERED_READ, msisdn);
                this.pubSub.publish(HikePubSub.MESSAGE_DELIVERED_READ, vals);
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
                    o = (JObject)jsonObj[HikeConstants.DATA];
                    uMsisdn = (string)o[HikeConstants.MSISDN];
                    serverTimestamp = (long)jsonObj[HikeConstants.TIMESTAMP];
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  USER_JOINED USER_LEFT, Exception : " + ex.StackTrace);
                    return;
                }
                bool joined = USER_JOINED == type;
                bool isRejoin = false;
                JToken subtype;
                if (jsonObj.TryGetValue(HikeConstants.SUB_TYPE, out subtype))
                {
                    isRejoin = HikeConstants.SUBTYPE_REJOIN == (string)subtype;
                }
                // update contacts cache
                if (App.ViewModel.ContactsCache.ContainsKey(uMsisdn))
                    App.ViewModel.ContactsCache[uMsisdn].OnHike = joined;
                GroupManager.Instance.LoadGroupCache();
                if (joined)
                {
                    long lastTimeStamp;
                    if (App.appSettings.TryGetValue(HikeConstants.AppSettings.LAST_USER_JOIN_TIMESTAMP, out lastTimeStamp) && lastTimeStamp >= serverTimestamp)
                        return;
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.LAST_USER_JOIN_TIMESTAMP, serverTimestamp);
                    // if user is in contact list then only show the joined msg
                    ContactInfo c = UsersTableUtils.getContactInfoFromMSISDN(uMsisdn);

                    // if user does not exists we dont know about his onhike status , so we need to process
                    ProcessUoUjMsgs(jsonObj, false, c != null, isRejoin);
                }
                // if user has left, mark him as non hike user in group cache
                else
                {
                    //remove image if stored.
                    if (App.ViewModel.ConvMap.ContainsKey(uMsisdn))
                    {
                        if (App.ViewModel.ConvMap[uMsisdn].Avatar != null)
                        {
                            App.ViewModel.ConvMap[uMsisdn].Avatar = null;
                            this.pubSub.publish(HikePubSub.UPDATE_PROFILE_ICON, uMsisdn);
                        }
                    }

                    MiscDBUtil.DeleteImageForMsisdn(uMsisdn);

                    if (GroupManager.Instance.GroupCache != null)
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
                }
                UsersTableUtils.updateOnHikeStatus(uMsisdn, joined);
                ConversationTableUtils.updateOnHikeStatus(uMsisdn, joined);
                JToken jt;
                long ts = 0;
                if (joined && jsonObj.TryGetValue(HikeConstants.TIMESTAMP, out jt))
                    ts = jt.ToObject<long>();
                FriendsTableUtils.SetJoiningTime(uMsisdn, ts);
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
                    try
                    {
                        App.ViewModel.ConvMap[msisdn].Avatar = imageBytes;
                        this.pubSub.publish(HikePubSub.UPDATE_PROFILE_ICON, msisdn);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  ICON , Exception : " + ex.StackTrace);
                    }
                }
                else // update fav and contact section
                {
                    if (msisdn == null)
                        return;
                    ConversationListObject c = App.ViewModel.GetFav(msisdn);
                    if (c != null) // for favourites
                    {
                        c.Avatar = imageBytes;
                    }
                    else
                    {
                        c = App.ViewModel.GetPending(msisdn);
                        if (c != null) // for pending requests
                        {
                            c.Avatar = imageBytes;
                        }
                    }
                }
                if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                {
                    UI_Utils.Instance.BitmapImageCache.Remove(msisdn);
                    // this is done to notify that image is changed so load new one.
                    App.ViewModel.ContactsCache[msisdn].Avatar = null;
                }
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.UpdateUserImageInStatus(msisdn);
                });
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
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }
                try
                {
                    int invited_joined = (int)data[HikeConstants.ALL_INVITEE_JOINED];
                    App.WriteToIsoStorageSettings(App.INVITED_JOINED, invited_joined);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }
                string totalCreditsPerMonth = "0";
                try
                {
                    totalCreditsPerMonth = data[HikeConstants.TOTAL_CREDITS_PER_MONTH].ToString();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  INVITE_INFO , Exception : " + ex.StackTrace);
                }

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
                    JToken jtoken;
                    if (data.TryGetValue(HikeConstants.SHOW_FREE_INVITES, out jtoken) && (bool)jtoken)
                    {
                        App.appSettings[HikeConstants.SHOW_POPUP] = null;//to show it is free sms pop up.
                    }
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
                                                    bool thrAreFavs = false, isFav;
                                                    KeyValuePair<string, JToken> fkkvv;
                                                    IEnumerator<KeyValuePair<string, JToken>> kVals = favJSON.GetEnumerator();
                                                    while (kVals.MoveNext()) // this will iterate throught the list
                                                    {
                                                        isFav = true; // true for fav , false for pending
                                                        fkkvv = kVals.Current; // kkvv contains favourites MSISDN

                                                        if (App.ViewModel.BlockedHashset.Contains(fkkvv.Key)) // if this user is blocked ignore him
                                                            continue;

                                                        JObject pendingJSON = fkkvv.Value.ToObject<JObject>();
                                                        JToken pToken;
                                                        if (pendingJSON.TryGetValue(HikeConstants.REQUEST_PENDING, out pToken))
                                                        {
                                                            bool rp = false;
                                                            thrAreFavs = true;
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

                                                            if (App.ViewModel.ConvMap.ContainsKey(fkkvv.Key))
                                                                App.ViewModel.ConvMap[fkkvv.Key].IsFav = true;

                                                            if (rp)
                                                                FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.REQUEST_SENT);
                                                            else
                                                                FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                                                        }
                                                        else if (pendingJSON.TryGetValue(HikeConstants.PENDING, out pToken) && pToken != null)
                                                        {
                                                            if (pToken.ToObject<bool>() == true) // pending is true
                                                            {
                                                                isFav = false;
                                                                FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED);

                                                                ConversationListObject favObj;
                                                                if (App.ViewModel.ConvMap.ContainsKey(fkkvv.Key))
                                                                    favObj = App.ViewModel.ConvMap[fkkvv.Key];
                                                                else
                                                                {
                                                                    ContactInfo ci = UsersTableUtils.getContactInfoFromMSISDN(fkkvv.Key);
                                                                    if (ci != null)
                                                                        name = ci.Name;

                                                                    favObj = new ConversationListObject(fkkvv.Key, name, ci != null ? ci.OnHike : true, ci != null ? MiscDBUtil.getThumbNailForMsisdn(fkkvv.Key) : null);
                                                                }

                                                                this.pubSub.publish(HikePubSub.ADD_TO_PENDING, favObj);
                                                            }
                                                            else // pending is false
                                                            {
                                                                // in this case friend state should be ignored
                                                                FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_YOU);
                                                                continue;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            thrAreFavs = true;

                                                            if (App.ViewModel.ConvMap.ContainsKey(fkkvv.Key))
                                                                App.ViewModel.ConvMap[fkkvv.Key].IsFav = true;

                                                            FriendsTableUtils.SetFriendStatus(fkkvv.Key, FriendsTableUtils.FriendStatusEnum.FRIENDS);
                                                        }

                                                        Debug.WriteLine("Fav request, Msisdn : {0} ; isFav : {1}", fkkvv.Key, isFav);
                                                        LoadFavAndPending(isFav, fkkvv.Key); // true for favs
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
                                        if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian dont show rewards
                                        {
                                            if (kkvv.Key == HikeConstants.REWARDS_TOKEN)
                                            {
                                                App.WriteToIsoStorageSettings(HikeConstants.REWARDS_TOKEN, kkvv.Value.ToString());
                                            }
                                            // whenever this key will come toggle the show rewards thing
                                            if (kkvv.Key == HikeConstants.SHOW_REWARDS)
                                            {
                                                App.WriteToIsoStorageSettings(HikeConstants.SHOW_REWARDS, kkvv.Value.ToObject<bool>());
                                                pubSub.publish(HikePubSub.REWARDS_TOGGLE, true);
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
                                        }
                                        #endregion
                                        #region Profile Pic

                                        if (kkvv.Key == HikeConstants.ICON)
                                        {
                                            JToken iconToken = kkvv.Value.ToObject<JToken>();
                                            if (iconToken != null)
                                            {
                                                byte[] imageBytes = System.Convert.FromBase64String(iconToken.ToString());
                                                MiscDBUtil.saveAvatarImage(HikeConstants.MY_PROFILE_PIC, imageBytes, true);
                                                object[] vals = new object[3];
                                                vals[0] = App.MSISDN;
                                                vals[1] = null;
                                                vals[2] = imageBytes;
                                                App.HikePubSubInstance.publish(HikePubSub.ADD_OR_UPDATE_PROFILE, vals);
                                            }
                                        }

                                        #endregion
                                        #region LAST SEEN SEETING

                                        if (kkvv.Key == HikeConstants.LASTSEENONOFF)
                                        {
                                            try
                                            {
                                                var val = kkvv.Value.ToString();

                                                if (String.IsNullOrEmpty(val) || Convert.ToBoolean(val))
                                                {
                                                    App.appSettings.Remove(App.LAST_SEEN_SEETING);
                                                    App.appSettings.Save();
                                                }
                                                else
                                                    App.WriteToIsoStorageSettings(App.LAST_SEEN_SEETING, false);
                                            }
                                            catch { }
                                        }

                                        #endregion

                                        #region CHAT BACKGROUNDS

                                        else if (kkvv.Key == HikeConstants.CHAT_BACKGROUND_ARRAY)
                                        {
                                            bool isUpdated = false;

                                            var val = kkvv.Value;
                                            foreach (var obj in val)
                                            {
                                                JObject jObj = (JObject)obj;

                                                var id = (string)jObj[HikeConstants.MSISDN];
                                                bool hasCustomBg = false;
                                                JToken custom;
                                                if (jObj.TryGetValue(HikeConstants.HAS_CUSTOM_BACKGROUND, out custom))
                                                    hasCustomBg = (bool)custom;

                                                if (!hasCustomBg && ChatBackgroundHelper.Instance.UpdateChatBgMap(id, (string)jObj[HikeConstants.BACKGROUND_ID], TimeUtils.getCurrentTimeStamp(), false))
                                                {
                                                    isUpdated = true;

                                                    if (App.newChatThreadPage != null && App.newChatThreadPage.mContactNumber == id)
                                                        pubSub.publish(HikePubSub.CHAT_BACKGROUND_REC, id);
                                                }
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
                                            {
                                                App.WriteToIsoStorageSettings(App.DISPLAYPIC_FAV_ONLY, true);
                                            }
                                        }
                                        #endregion

                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT_INFO , Exception : " + ex.StackTrace);
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
                        catch (Exception ex)
                        {
                            Debug.WriteLine("NetworkManager ::  onMessage :  ACCOUNT_INFO , Exception : " + ex.StackTrace);
                        }
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
                    #region rewards zone
                    JToken rew;
                    if (App.MSISDN.Contains(HikeConstants.INDIA_COUNTRY_CODE))//for non indian dont show rewards
                    {
                        if (data.TryGetValue(HikeConstants.REWARDS_TOKEN, out rew))
                            App.WriteToIsoStorageSettings(HikeConstants.REWARDS_TOKEN, rew.ToString());
                        rew = null;
                        if (data.TryGetValue(HikeConstants.SHOW_REWARDS, out rew))
                        {
                            App.WriteToIsoStorageSettings(HikeConstants.SHOW_REWARDS, rew.ToObject<bool>());
                            pubSub.publish(HikePubSub.REWARDS_TOGGLE, true);
                        }
                    }
                    #endregion
                    #region batch push zone
                    JToken pushStatus;
                    if (data.TryGetValue(HikeConstants.ENABLE_PUSH_BATCH_SU, out pushStatus))
                    {
                        try
                        {
                            JArray jArray = (JArray)pushStatus;
                            if (jArray != null)
                            {
                                if (jArray.Count > 1)
                                {
                                    App.appSettings[App.STATUS_UPDATE_FIRST_SETTING] = (byte)jArray[0];
                                    App.WriteToIsoStorageSettings(App.STATUS_UPDATE_SECOND_SETTING, (byte)jArray[1]);
                                }
                                else if (jArray.Count == 1)
                                {
                                    App.WriteToIsoStorageSettings(App.STATUS_UPDATE_FIRST_SETTING, (byte)jArray[0]);
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
                    if (data.TryGetValue(App.HIDE_CRICKET_MOODS, out rew))
                    {
                        //we are keeping state for hide because by default moods are ON. If server never sends this packet, no
                        //appsetting would ever be stored
                        bool showMoods = rew.ToObject<bool>();
                        App.WriteToIsoStorageSettings(App.HIDE_CRICKET_MOODS, !showMoods);
                    }
                    #endregion
                    #region Invite pop up
                    JToken jtokenMessageId;
                    if (data.TryGetValue(HikeConstants.MESSAGE_ID, out jtokenMessageId))
                    {
                        JToken jtokenShowFreeInvites;
                        string previousId;
                        if ((!App.appSettings.TryGetValue(HikeConstants.INVITE_POPUP_UNIQUEID, out previousId) || previousId != ((string)jtokenMessageId)) && data.TryGetValue(HikeConstants.SHOW_FREE_INVITES, out jtokenShowFreeInvites))
                        {
                            App.WriteToIsoStorageSettings(HikeConstants.INVITE_POPUP_UNIQUEID, (string)jtokenMessageId);
                            bool showInvite = (bool)jtokenShowFreeInvites;

                            if (showInvite)
                            {
                                JToken jtoken;
                                Object[] popupDataobj = new object[2];
                                //add title to zero place;
                                popupDataobj[0] = data.TryGetValue(HikeConstants.FREE_INVITE_POPUP_TITLE, out jtoken) ? (string)jtoken : null;
                                //add text to first place;
                                popupDataobj[1] = data.TryGetValue(HikeConstants.FREE_INVITE_POPUP_TEXT, out jtoken) ? (string)jtoken : null;
                                App.appSettings[HikeConstants.SHOW_POPUP] = popupDataobj;
                            }
                        }
                    }
                    #endregion
                    #region REFRESH IP LIST
                    JToken iplist;
                    if (data.TryGetValue(HikeConstants.IP_KEY, out iplist))
                    {
                        try
                        {
                            JArray jArray = (JArray)iplist;
                            if (jArray != null && jArray.Count > 0)
                            {
                                string[] ips = new string[jArray.Count];

                                for (int i = 0; i < jArray.Count; i++)
                                {
                                    ips[i] = (string)jArray[i];
                                }

                                App.WriteToIsoStorageSettings(App.IP_LIST, ips);
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
            else if (HikeConstants.MqttMessageTypes.USER_OPT_IN == type)
            {
                // {"t":"uo", "d":{"msisdn":"", "credits":10}}
                ProcessUoUjMsgs(jsonObj, true, true, false);
            }
            #endregion
            #region GROUP CHAT RELATED

            #region GROUP_CHAT_JOIN
            else if (HikeConstants.MqttMessageTypes.GROUP_CHAT_JOIN == type) //Group chat join
            {
                string groupName = string.Empty;
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
                catch (Exception ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage :  GROUP_CHAT_JOIN , Exception : " + ex.StackTrace);
                }
                GroupManager.Instance.LoadGroupParticipants(grpId);
                ConvMessage convMessage = null;
                List<GroupParticipant> dndList = new List<GroupParticipant>(1);
                GroupChatState gcState = AddGroupmembers(arr, grpId, dndList);

                #region META DATA CHAT BACKGROUND

                JObject metaData = (JObject)jsonObj[HikeConstants.METADATA];
                if (metaData != null)
                {
                    #region chat background
                    try
                    {
                        JObject chatBg = (JObject)metaData[HikeConstants.MqttMessageTypes.CHAT_BACKGROUNDS];
                        if (chatBg != null)
                        {
                            bool hasCustomBg = false;
                            JToken custom;
                            if (chatBg.TryGetValue(HikeConstants.HAS_CUSTOM_BACKGROUND, out custom))
                                hasCustomBg = (bool)custom;

                            if (!hasCustomBg && ChatBackgroundHelper.Instance.UpdateChatBgMap(grpId, (string)chatBg[HikeConstants.BACKGROUND_ID], TimeUtils.getCurrentTimeStamp()))
                                pubSub.publish(HikePubSub.CHAT_BACKGROUND_REC, grpId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("NetworkManager ::  onMessage :  GROUP_CHAT_JOIN with chat background, Exception : " + ex.StackTrace);
                    }

                    #endregion

                    #region GROUP NAME

                    JToken gName;
                    //pubsub for gcn is not raised
                    if (metaData.TryGetValue(HikeConstants.NAME, out gName))
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
                    {
                        GroupManager.Instance.SaveGroupCache(grpId);
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
                    groupName = groupName.Trim();
                    string groupId = (string)jsonObj[HikeConstants.TO];
                    //no self check as server will send packet of group name change if changed by self
                    //we need to use this in case of self name change and unlink account
                    ConversationListObject cObj;
                    if (App.ViewModel.ConvMap.TryGetValue(groupId, out cObj))
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
                ConversationListObject cObj;
                if (!App.ViewModel.ConvMap.TryGetValue(groupId, out cObj))
                    return;//if group doesn't exist return
                JToken temp;
                jsonObj.TryGetValue(HikeConstants.DATA, out temp);
                if (temp == null)
                    return;
                string iconBase64 = temp.ToString();
                
                //check if same image is set
                if (cObj.Avatar != null)
                {
                    string previousImage = System.Convert.ToBase64String(cObj.Avatar);
                    if (previousImage.Length > 4 && iconBase64.Length > 4 &&
                        previousImage.Substring(0, 5) == iconBase64.Substring(0, 5) &&
                        previousImage.Substring(previousImage.Length - 5) == iconBase64.Substring(iconBase64.Length - 5))
                    {
                        return;
                    }
                }

                GroupManager.Instance.LoadGroupParticipants(groupId);

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
                    GroupParticipant gp = GroupManager.Instance.GetGroupParticipant(null, fromMsisdn, groupId);
                    if (gp == null || gp.HasLeft)
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

                        //explicitly set IsGroupAlive false to prevent db hit
                        cObj.IsGroupAlive = false;

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

            #region GROUP_OWNER_CHANGED
            else if (HikeConstants.MqttMessageTypes.GROUP_OWNER_CHANGED == type) //Group chat end
            {
                try
                {
                    string groupId = (string)jsonObj[HikeConstants.TO];

                    if (!App.ViewModel.ConvMap.ContainsKey(groupId))//group doesn't exists
                        return;

                    JObject data = (JObject)jsonObj[HikeConstants.DATA];

                    JToken jtoken;
                    if (data.TryGetValue(HikeConstants.MqttMessageTypes.MSISDN_KEYWORD, out jtoken))
                    {
                        string newOwner = (string)jtoken;

                        if (string.IsNullOrEmpty(newOwner))
                            return;

                        if (GroupTableUtils.UpdateGroupOwner(groupId, newOwner))
                        {
                            Object[] objArray = new object[] { groupId, newOwner };
                            App.ViewModel.GroupOwnerChanged(objArray);
                        }
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
                    // if user is blocked simply ignore the request.
                    if (App.ViewModel.BlockedHashset.Contains(msisdn))
                        return;
                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.REQUEST_RECIEVED);
                    if (friendStatus == FriendsTableUtils.FriendStatusEnum.ALREADY_FRIENDS)
                        return;

                    if (friendStatus == FriendsTableUtils.FriendStatusEnum.FRIENDS)
                    {
                        StatusMessage sm = new StatusMessage(msisdn, String.Empty, StatusMessage.StatusType.IS_NOW_FRIEND, null, TimeUtils.getCurrentTimeStamp(), -1, false);
                        App.HikePubSubInstance.publish(HikePubSub.SAVE_STATUS_IN_DB, sm);
                        App.HikePubSubInstance.publish(HikePubSub.STATUS_RECEIVED, sm);
                    }
                    App.HikePubSubInstance.publish(HikePubSub.FRIEND_RELATIONSHIP_CHANGE, new Object[] { msisdn, friendStatus });
                    if (App.ViewModel.Isfavourite(msisdn)) // already favourite
                        return;
                    if (App.ViewModel.IsPending(msisdn))
                        return;

                    try
                    {
                        ConversationListObject favObj;
                        if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                            favObj = App.ViewModel.ConvMap[msisdn];
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
                            favObj = new ConversationListObject(msisdn, name, ci != null ? ci.OnHike : true, ci != null ? MiscDBUtil.getThumbNailForMsisdn(msisdn) : null);
                        }
                        // this will ensure there will be one pending request for a particular msisdn
                        App.ViewModel.PendingRequests[msisdn] = favObj;
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
                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                    App.HikePubSubInstance.publish(HikePubSub.FRIEND_RELATIONSHIP_CHANGE, new Object[] { msisdn, friendStatus });
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
                    // if user is blocked ignore his requests
                    if (App.ViewModel.BlockedHashset.Contains(msisdn))
                        return;

                    FriendsTableUtils.FriendStatusEnum friendStatus = FriendsTableUtils.SetFriendStatus(msisdn, FriendsTableUtils.FriendStatusEnum.UNFRIENDED_BY_HIM);
                    App.HikePubSubInstance.publish(HikePubSub.FRIEND_RELATIONSHIP_CHANGE, new Object[] { msisdn, friendStatus });
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
                // if this user is already blocked simply ignore his status
                if (App.ViewModel.BlockedHashset.Contains(msisdn))
                    return;

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
                    long ts = 0;

                    if (jsonObj.TryGetValue(HikeConstants.TIMESTAMP, out val) && val != null)
                    {
                        ts = val.ToObject<long>();
                        long tsCorrection;

                        if (App.appSettings.TryGetValue(HikeConstants.AppSettings.TIME_DIFF_EPOCH, out tsCorrection))
                            ts -= tsCorrection;
                    }

                    val = null;
                    string id = null;
                    JToken idToken;

                    if (data.TryGetValue(HikeConstants.STATUS_ID, out idToken))
                        id = idToken.ToString();
                    #region HANDLE PROFILE PIC UPDATE
                    if (data.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                    {
                        sm = new StatusMessage(msisdn, id, StatusMessage.StatusType.PROFILE_PIC_UPDATE, id, ts,
                            StatusUpdateHelper.Instance.IsTwoWayFriend(msisdn), -1, -1, 0, true);
                        idToken = null;
                        if (iconBase64 != null)
                        {
                            byte[] imageBytes = System.Convert.FromBase64String(iconBase64);
                            if (!StatusMsgsTable.InsertStatusMsg(sm, true))//will return false if status already exists
                                return;
                            MiscDBUtil.saveProfileImages(msisdn, imageBytes, sm.ServerId);
                            jsonObj[HikeConstants.PROFILE_PIC_ID] = sm.ServerId;
                            UI_Utils.Instance.BitmapImageCache.Remove(msisdn);
                        }
                    }
                    #endregion

                    #region HANDLE TEXT UPDATE
                    else if (data.TryGetValue(HikeConstants.TEXT_UPDATE_MSG, out val) && val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                    {
                        int moodId = -1;
                        int tod = 0;
                        if (data[HikeConstants.MOOD] != null)
                        {
                            string moodId_String = data[HikeConstants.MOOD].ToString();
                            if (!string.IsNullOrEmpty(moodId_String))
                            {
                                int.TryParse(moodId_String, out moodId);
                                moodId = MoodsInitialiser.GetRecieverMoodId(moodId);
                                try
                                {
                                    if (moodId > 0 && data[HikeConstants.TIME_OF_DAY] != null && !String.IsNullOrWhiteSpace(data[HikeConstants.TIME_OF_DAY].ToString()))
                                        tod = data[HikeConstants.TIME_OF_DAY].ToObject<int>();
                                
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
                            return;
                    }
                    #endregion

                    ConvMessage cm = new ConvMessage(ConvMessage.ParticipantInfoState.STATUS_UPDATE, jsonObj, ts);
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
                    Debug.WriteLine("Network Manager :: Exception in STATUS UPDATES : " + e.StackTrace);
                }
            }
            #endregion
            #region DELETE STATUS
            else if (HikeConstants.MqttMessageTypes.DELETE_STATUS_UPDATE == type)
            {
                JObject data = null;
                try
                {
                    if (App.ViewModel.BlockedHashset.Contains(msisdn)) // if this user is blocked simply ignore him 
                        return;
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    string id = (string)data[HikeConstants.STATUS_ID];
                    long msgId = StatusMsgsTable.DeleteStatusMsg(id);
                    if (msgId > 0) // delete only if msgId is greater than 0
                    {
                        MessagesTableUtils.deleteMessage(msgId);
                        // if conversation from this user exists
                        if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                        {
                            ConversationListObject co = App.ViewModel.ConvMap[msisdn];
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
                                        if (cm.FileAttachment.ContentType.Contains(HikeConstants.IMAGE))
                                            co.LastMessage = AppResources.Image_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(HikeConstants.AUDIO))
                                            co.LastMessage = AppResources.Audio_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(HikeConstants.VIDEO))
                                            co.LastMessage = AppResources.Video_Txt;
                                        else if (cm.FileAttachment.ContentType.Contains(HikeConstants.CT_CONTACT))
                                            co.LastMessage = AppResources.ContactTransfer_Text;
                                        else if (cm.FileAttachment.ContentType.Contains(HikeConstants.LOCATION))
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
                                            {
                                                co.LastMessage = AppResources.Nudge;
                                            }
                                            // STATUS UPDATE
                                            else if (cm.MetaDataString.Contains(HikeConstants.MqttMessageTypes.STATUS_UPDATE))
                                            {
                                                JObject jdata = null;
                                                try
                                                {
                                                    jdata = JObject.Parse(cm.MetaDataString);
                                                }
                                                catch (Exception e)
                                                {
                                                }
                                                if (jdata != null)
                                                {
                                                    JToken val;
                                                    JObject ddata = jdata[HikeConstants.DATA] as JObject;
                                                    // profile pic update
                                                    if (ddata.TryGetValue(HikeConstants.PROFILE_UPDATE, out val) && true == (bool)val)
                                                        co.LastMessage = "\"" + AppResources.Update_Profile_Pic_txt + "\"";
                                                    else // status , mood update
                                                        co.LastMessage = "\"" + cm.Message + "\"";
                                                }
                                            }
                                            else // NOTIFICATION AND NORMAL MSGS
                                            {
                                                co.LastMessage = cm.Message;
                                            }
                                        }
                                    }

                                }
                                else // there are no msgs left remove the conversation from db and map
                                {
                                    ConversationTableUtils.deleteConversation(msisdn);
                                    pubSub.publish(HikePubSub.DELETE_STATUS_AND_CONV, App.ViewModel.ConvMap[msisdn]);
                                    App.ViewModel.ConvMap.Remove(msisdn);
                                }
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
                long timediff = (long)jsonObj[HikeConstants.TIMESTAMP] - TimeUtils.getCurrentTimeStamp();
                App.WriteToIsoStorageSettings(HikeConstants.AppSettings.TIME_DIFF_EPOCH, timediff);
                //todo:place this setting in some different file as will be written again and agian
            }
            #endregion
            #region STICKER
            else if (type == STICKER)
            {
                try
                {
                    string subType = (string)jsonObj[HikeConstants.SUB_TYPE];
                    JObject jsonData = (JObject)jsonObj[HikeConstants.DATA];

                    //do same for category as well as subcategory
                    if (subType == HikeConstants.ADD_STICKER || subType == HikeConstants.ADD_CATEGORY)
                    {
                        string category = (string)jsonData[HikeConstants.CATEGORY_ID];
                        StickerHelper.UpdateHasMoreMessages(category, true, true);

                        //reset in app tip for "New Stickers"
                        App.ViewModel.ResetInAppTip(1);
                    }
                    else if (subType == HikeConstants.REMOVE_STICKER)
                    {
                        string category = (string)jsonData[HikeConstants.CATEGORY_ID];
                        JArray jarray = (JArray)jsonData["stIds"];
                        List<string> listStickers = new List<string>();
                        for (int i = 0; i < jarray.Count; i++)
                        {
                            listStickers.Add((string)jarray[i]);
                        }
                        StickerHelper.DeleteSticker(category, listStickers);
                        RecentStickerHelper.DeleteSticker(category, listStickers);

                    }
                    else if (subType == HikeConstants.REMOVE_CATEGORY)
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

            else if (HikeConstants.MqttMessageTypes.PRO_TIPS == type)
            {
                JObject data = null;

                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    var id = (string)data[HikeConstants.PRO_TIP_ID];
                    var header = (string)data[HikeConstants.PRO_TIP_HEADER];
                    var text = (string)data[HikeConstants.PRO_TIP_TEXT];

                    var imageUrl = "";
                    try
                    {
                        imageUrl = (string)data[HikeConstants.PRO_TIP_IMAGE];
                    }
                    catch
                    {
                        imageUrl = "";
                    }

                    var base64Image = "";
                    try
                    {
                        base64Image = (string)data[HikeConstants.THUMBNAIL];
                    }
                    catch
                    {
                        base64Image = "";
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
            else if (HikeConstants.MqttMessageTypes.CHAT_BACKGROUNDS == type)
            {
                try
                {
                    ConvMessage cm;
                    var ts = (long)jsonObj[HikeConstants.TIMESTAMP];
                    if (ts > 0)
                    {
                        long timedifference;
                        if (App.appSettings.TryGetValue(HikeConstants.AppSettings.TIME_DIFF_EPOCH, out timedifference))
                            ts = ts - timedifference;
                    }

                    var to = (string)jsonObj[HikeConstants.TO];

                    if (!String.IsNullOrEmpty(to) && Utils.isGroupConversation(to))
                        GroupManager.Instance.LoadGroupParticipants(to);

                    if (!String.IsNullOrEmpty(to) && Utils.isGroupConversation(to) && !GroupManager.Instance.GroupCache.ContainsKey(to))
                    {
                        Debug.WriteLine("OnMesage: Chat backgrounds: Group not found - {0}", to);
                        return;
                    }

                    var sender = !String.IsNullOrEmpty(to) ? to : msisdn;

                    var data = (JObject)jsonObj[HikeConstants.DATA];
                    var bgId = (string)data[HikeConstants.BACKGROUND_ID];

                    ChatThemeData bg = null;
                    if (ChatBackgroundHelper.Instance.ChatBgMap.TryGetValue(sender, out bg))
                    {
                        if (bg.Timestamp > ts || bg.BackgroundId == bgId)
                            return;
                    }

                    bool hasCustomBg = false;
                    JToken custom;
                    if (data.TryGetValue(HikeConstants.HAS_CUSTOM_BACKGROUND, out custom))
                        hasCustomBg = (bool)custom;

                    if (!hasCustomBg && ChatBackgroundHelper.Instance.BackgroundIDExists(bgId))
                    {
                        if (!String.IsNullOrEmpty(to) && GroupManager.Instance.GroupCache.ContainsKey(to))
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

                    ConversationListObject obj = MessagesTableUtils.addChatMessage(cm, false, null, sender);

                    if (hasCustomBg || !ChatBackgroundHelper.Instance.BackgroundIDExists(bgId))
                        cm.GrpParticipantState = ConvMessage.ParticipantInfoState.NO_INFO;

                    if (obj != null)
                    {
                        bool isPush = true;
                        JToken pushJToken;
                        if (data.TryGetValue(HikeConstants.PUSH, out pushJToken))
                            isPush = (Boolean)pushJToken;

                        object[] vals;
                        vals = new object[3];
                        vals[0] = cm;
                        vals[1] = obj;
                        vals[2] = isPush;

                        this.pubSub.publish(HikePubSub.MESSAGE_RECEIVED, vals);
                    }

                    if (!hasCustomBg && ChatBackgroundHelper.Instance.UpdateChatBgMap(sender, bgId, ts))
                    {
                        pubSub.publish(HikePubSub.CHAT_BACKGROUND_REC, sender);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: Chat Background, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }
            }
            #endregion
            #region App Update

            else if (HikeConstants.MqttMessageTypes.APP_UPDATE == type)
            {
                JObject data = null;

                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    var devType = (string)data[HikeConstants.DEVICE_TYPE_KEY];

                    if (devType != "windows")
                        return;

                    var version = (string)data[HikeConstants.VERSION];

                    if (Utils.compareVersion(version, App.CURRENT_VERSION) <= 0)
                        return;

                    bool isCritical = false;
                    try
                    {
                        isCritical = (bool)data[HikeConstants.CRITICAL];
                    }
                    catch
                    {
                        isCritical = false;
                    }

                    var message = "";
                    try
                    {
                        message = (string)data[HikeConstants.TEXT_UPDATE_MSG];
                    }
                    catch
                    {
                        message = isCritical ? AppResources.CRITICAL_UPDATE_TEXT : AppResources.NORMAL_UPDATE_TEXT;
                    }

                    JObject obj = new JObject();
                    obj.Add(HikeConstants.CRITICAL, isCritical);
                    obj.Add(HikeConstants.TEXT_UPDATE_MSG, message);
                    obj.Add(HikeConstants.VERSION, version);
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.NEW_UPDATE_AVAILABLE, obj.ToString(Newtonsoft.Json.Formatting.None));

                    pubSub.publish(HikePubSub.APP_UPDATE_AVAILABLE, null); // no need of any arguments
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: APP UPDATE, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }
            }

            #endregion
            #region ACTION
            else if (type == ACTION)
            {
                JObject data = null;

                try
                {
                    data = (JObject)jsonObj[HikeConstants.DATA];
                    bool isRegisterPush = (bool)data[HikeConstants.PUSH];

                    if (isRegisterPush)
                        PushHelper.Instance.registerPushnotifications(true);

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Network Manager:: ACTION, Json : {0} Exception : {1}", jsonObj.ToString(Formatting.None), ex.StackTrace);
                }

            }
            #endregion
            #region IC REMOVE
            else if (type == ICON_REMOVE)
            {
                try
                {
                    MiscDBUtil.DeleteImageForMsisdn(msisdn);
                    UI_Utils.Instance.BitmapImageCache.Remove(msisdn);
                    
                    if (App.ViewModel.ConvMap.ContainsKey(msisdn))
                    {
                        App.ViewModel.ConvMap[msisdn].Avatar = null;
                        this.pubSub.publish(HikePubSub.UPDATE_PROFILE_ICON, msisdn);
                    }

                    ConversationListObject c = App.ViewModel.GetFav(msisdn);
                    
                    if (c != null) // for favourites
                    {
                        c.Avatar = null;
                    }
                    else
                    {
                        c = App.ViewModel.GetPending(msisdn);
                        if (c != null) // for pending requests
                        {
                            c.Avatar = null;
                        }
                    }

                    if (App.ViewModel.ContactsCache.ContainsKey(msisdn))
                    {
                        // this is done to notify that image is changed so load new one.
                        App.ViewModel.ContactsCache[msisdn].Avatar = null;
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            App.ViewModel.UpdateUserImageInStatus(msisdn);
                        });
                }
                catch (JsonReaderException ex)
                {
                    Debug.WriteLine("NetworkManager ::  onMessage : Icon Remove Handling, Exception : " + ex.Message);
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

        private void LoadFavAndPending(bool isFav, string msisdn)
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
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.FavList.Add(favObj);
                    MiscDBUtil.SaveFavourites();
                    MiscDBUtil.SaveFavourites(favObj);
                    int count = 0;
                    App.appSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);
                    App.WriteToIsoStorageSettings(HikeViewModel.NUMBER_OF_FAVS, count + 1);
                });
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

        private void ProcessUoUjMsgs(JObject jsonObj, bool isOptInMsg, bool isUserInContactList, bool isRejoin)
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
                if (!isOptInMsg || App.ViewModel.ConvMap.ContainsKey(ms)) // if this is UJ or conversation has this msisdn go in
                {
                    object[] vals = null;
                    ConvMessage cm = null;
                    if (isOptInMsg)
                        cm = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);
                    else
                        cm = new ConvMessage(isRejoin ? ConvMessage.ParticipantInfoState.USER_REJOINED : ConvMessage.ParticipantInfoState.USER_JOINED, jsonObj);
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
                GroupParticipant gp = l.Find(x => x.Msisdn == ms);
                if (gp != null)
                {
                    if (isOptInMsg)
                    {
                        ConvMessage convMsg = new ConvMessage(ConvMessage.ParticipantInfoState.USER_OPT_IN, jsonObj);

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
                    }
                    else
                        gp.IsOnHike = true;
                    gp.HasOptIn = true;
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

        /// <summary>
        /// Mark single msg as Sent Confirmed and Sent Delivered
        /// </summary>
        /// <param name="fromUser"></param>
        /// <param name="msgID"></param>
        /// <param name="status"></param>
        public static void updateDB(string fromUser, long msgID, int status)
        {
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateMsgStatus(fromUser, msgID, status);
            ConversationTableUtils.updateLastMsgStatus(msgID, msisdn, status); // update conversationObj, null is already checked in the function
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED : {0}", msec);
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
            Stopwatch st = Stopwatch.StartNew();
            string msisdn = MessagesTableUtils.updateAllMsgStatus(fromUser, ids, status, sender);
            if (msisdn == null)
            {
                string idsString = string.Empty;
                foreach (long id in ids)
                {
                    idsString = string.Format("{0}, {1}", idsString, id.ToString());
                }
                Debug.WriteLine("NetworkManager :: UpdateDbBatch : msisdn null for user:{0} ,ids:{1}, status:{2}", fromUser, idsString, status);
                return;
            }
            // To update conversation object , we have to check if ids [] contains last msg id
            if (App.ViewModel.ConvMap.ContainsKey(msisdn))
            {
                if (ContainsLastMsgId(ids, App.ViewModel.ConvMap[msisdn]))
                    ConversationTableUtils.updateLastMsgStatus(App.ViewModel.ConvMap[msisdn].LastMsgId, msisdn, status);
            }
            st.Stop();
            long msec = st.ElapsedMilliseconds;
            Debug.WriteLine("Time to update msg status DELIVERED READ : {0}", msec);
        }

        private bool ContainsLastMsgId(long[] ids, ConversationListObject co)
        {
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == co.LastMsgId)
                    return true;
            }
            return false;
        }
    }
}