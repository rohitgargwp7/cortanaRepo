﻿using System;
using System.Threading;
using System.Collections.Generic;
using windows_client.utils;

namespace windows_client
{
    public class HikePubSub
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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

        public static readonly string NEW_ACTIVITY = "new_activity";

        public static readonly string END_TYPING_CONVERSATION = "endtypingconv";

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

        public static readonly string MSG_READ = "msgRead";

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

        public static readonly string ICON_CHANGED = "iconChanged";

        public static readonly string USER_JOINED = "userJoined";

        public static readonly string USER_LEFT = "userLeft";

        public static readonly string SEND_NEW_MSG = "sendNewMsg";

        public static readonly string UPDATE_UI = "udpateUI";

        public static string ADD_OR_UPDATE_PROFILE = "addOrUpdateProfile";

        private readonly Thread mThread;

        private readonly BlockingQueue mQueue;

        private Dictionary<string, List<Listener>> listeners;

        public HikePubSub()
        {
            listeners = new Dictionary<string, List<Listener>>();
            mQueue = new BlockingQueue(2000);
            try
            {
                mThread = new Thread(new ThreadStart(startPubSub));
                mThread.Start();
            }
            catch(ThreadStartException e)
            {
                // do something here
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
            listeners.TryGetValue(type , out l);
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
                catch (Exception e)
                {
                    logger.Info("PubSub", "exception while running", e);
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
                    //list = listeners[type];
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
