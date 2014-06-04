﻿using System;
using System.Threading;
using System.Collections.Generic;
using windows_client.utils;
using System.Diagnostics;

namespace windows_client
{
    public class HikePubSub
    {
        public class Operation
        {
            public readonly string type;
            public readonly object payload;

            public Operation(string type, Object o)
            {
                this.type = type;
                this.payload = o;
            }
        }
        public interface Listener
        {
            void onEventReceived(string type, object obj);
        }

        private static readonly Operation DONE_OPERATION = null; /* TODO this can't be null */

        /* broadcast when the sender sends the message (click the send button in chat thread view) */
        public static readonly string MESSAGE_SENT = "messagesent";

        public static readonly string FORWARD_ATTACHMENT = "forwardAttachment";

        public static readonly string ATTACHMENT_SENT = "attachmentSent";

        public static readonly string MESSAGE_DELIVERED = "messageDelivered"; // represents that msg is delivered to receiver but is not read.

        public static readonly string MESSAGE_DELIVERED_READ = "messageDeliveredRead"; // represents that msg is delivered to receiver and is read by the same.

        public static readonly string WS_CLOSE = "ws_close";

        public static readonly string WS_RECEIVED = "ws_received";

        public static readonly string WS_OPEN = "ws_open";

        //	public static readonly string WS_SEND = "ws_send";

        public static readonly string NEW_CONVERSATION = "newconv";

        /* Broadcast after we've received a message and written it to our DB.
         * Status is RECEIVED_UNREAD */
        public static readonly string MESSAGE_RECEIVED = "messagereceived";

        public static readonly string STATUS_RECEIVED = "statusReceived";

        public static readonly string SAVE_STATUS_IN_DB = "svStatusDb";

        public static readonly string STATUS_DELETED = "statusDeleted";

        public static readonly string FRIEND_RELATIONSHIP_CHANGE = "friendRelation";

        public static readonly string NEW_ACTIVITY = "new_activity";

        public static readonly string TYPING_CONVERSATION = "typingconv";

        public static readonly string TOKEN_CREATED = "tokencreated";

        /* sms credits have been modified */
        public static readonly string SMS_CREDIT_CHANGED = "smscredits";

        /* broadcast when the server receives the message and replies with a confirmation */
        public static readonly string SERVER_RECEIVED_MSG = "serverReceivedMsg";

        /* broadcast when a message is received from the sender but before it's been written our DB*/
        public static readonly string MESSAGE_RECEIVED_FROM_SENDER = "messageReceivedFromSender";

        /* broadcast when a message is received from the sender abd is being read now */
        public static readonly string MESSAGE_RECEIVED_READ = "messageReceivedRead";

        /* publishes a message via mqtt to the server */
        public static readonly string MQTT_PUBLISH = "serviceSend";

        /* publishes a message via mqtt to the server with QoS 0*/
        public static readonly string MQTT_PUBLISH_LOW = "serviceSendLow";

        /* published when a message is deleted */
        public static readonly string MESSAGE_DELETED = "messageDeleted";

        public static readonly string MESSAGE_FAILED = "messageFailed";

        public static readonly string CONNECTION_STATUS = "connStatus";

        public static readonly string BLOCK_USER = "blockUser";

        public static readonly string UNBLOCK_USER = "unblockUser";

        public static readonly string UNBLOCK_GROUPOWNER = "unblockGroupOwner";

        public static readonly string BLOCK_GROUPOWNER = "blockGroupOwner";

        public static readonly string ICON_CHANGED = "iconChanged";

        public static readonly string USER_JOINED = "userJoined";

        public static readonly string USER_LEFT = "userLeft";

        public static readonly string SEND_NEW_MSG = "sendNewMsg";

        public static readonly string UPDATE_PROFILE_ICON = "updateIcon";

        public static string ADD_OR_UPDATE_PROFILE = "addOrUpdateProfile";

        public static readonly string ACCOUNT_DELETED = "accountDeleted";

        public static readonly string INVITEE_NUM_CHANGED = "inviteeNoChanged";

        public static readonly string GROUP_LEFT = "groupLeft";

        public static readonly string GROUP_END = "groupEnd";

        public static readonly string GROUP_ALIVE = "groupAlive";

        public static readonly string GROUP_NAME_CHANGED = "groupNameChanged";

        public static readonly string PARTICIPANT_JOINED_GROUP = "participantJoinedGroup";

        public static readonly string PARTICIPANT_LEFT_GROUP = "participantLeftGroup";

        public static readonly string DELETE_CONVERSATION = "deleteConversation";

        public static readonly string DELETE_STATUS_AND_CONV = "delConvStatus";

        public static readonly string UPDATE_ACCOUNT_NAME = "updateAccountName";

        public static readonly string INVITE_TOKEN_ADDED = "inviteTokenAdded";

        public static readonly string LAST_SEEN = "lastSeen";

        public static readonly string APP_UPDATE_AVAILABLE = "appUpdateAvailable";

        // TODO : USE ADDREM FROM FAV instead
        public static readonly string ADD_REMOVE_FAV = "addRemFP";
        public static readonly string REMOVE_FRIENDS = "remFriends";
        public static readonly string ADD_FRIENDS = "addFriends";

        public static readonly string ADD_TO_PENDING = "addToPending";

        public static readonly string REWARDS_TOGGLE = "rew_tog";

        public static readonly string REWARDS_CHANGED = "rew_changed";

        public static readonly string BAD_USER_PASS = "badUserPass";

        public static readonly string UPDATE_GRP_PIC = "up_grp_pic";
        public static readonly string PRO_TIPS_REC = "proTipRec";
        public static readonly string CHAT_BACKGROUND_REC = "chatBgRec";

        public static readonly string CONTACT_ADDED = "contact_added";
        public static readonly string ADDRESSBOOK_UPDATED= "adbook_updated";

        public static readonly string FILE_STATE_CHANGED = "fileStateChanged";

        private readonly Thread mThread;

        private readonly BlockingQueue mQueue;

        private Dictionary<string, List<Listener>> listeners;


        public HikePubSub()
        {
            listeners = new Dictionary<string, List<Listener>>();
            mQueue = new BlockingQueue(100);
            try
            {
                mThread = new Thread(new ThreadStart(startPubSub));
                mThread.Name = "PUBSUB THREAD";
                mThread.Start();
            }
            catch (Exception ex)
            {
                // do something here
                Debug.WriteLine("HIkePubSub :: HikePubSub() : thread start, Exception : " + ex.StackTrace);
            }
        }

        public void addListener(string type, Listener listener)
        {
            lock (listeners) // enter synchronization here i.e only one thread can enter this part
            {
                List<Listener> list;
                listeners.TryGetValue(type, out list);
                if (list == null)
                {
                    list = new List<Listener>();
                    listeners[type] = list;
                }
                if (!list.Contains(listener))
                    list.Add(listener);
            }
        }

        public bool publish(string type, object o)
        {

            if (!listeners.ContainsKey(type))
            {
                return false;
            }
            mQueue.Enqueue(new Operation(type, o));
            return true;
        }

        public void removeListener(string type, Listener listener)
        {
            List<Listener> l;
            listeners.TryGetValue(type, out l);
            if (l != null)
            {
                l.Remove(listener);
            }
        }

        private void startPubSub()
        {
            Operation op = null;
            while (true)
            {
                try
                {
                    op = (Operation)mQueue.Dequeue();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("HIkePubSub :: startPubSub : startPubSub , Exception : " + ex.StackTrace);
                    continue;
                }

                if (op == DONE_OPERATION)
                {
                    break;
                }
                if (op == null)
                    continue;
                string type = op.type;
                object o = op.payload;

                List<Listener> list;

                lock (listeners)  // seems not required here
                {
                    listeners.TryGetValue(type, out list);
                }
                if (list == null)
                {
                    continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    list[i].onEventReceived(type, o);
                    Thread.Sleep(10);
                }
            }
        }
    }
}
