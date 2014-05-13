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
using System.Net.Sockets;
using System.IO;
using finalmqtt.Msg;
using System.Text;
using System.Collections.Generic;
using mqtttest.Client;
using Microsoft.Phone.Reactive;
using System.Threading;
using System.Diagnostics;

namespace finalmqtt.Client
{
    public class MqttConnection
    {
        short nextMessageId = 1;
        public short getNextMessageId()
        {
            short rc = nextMessageId;
            nextMessageId++;
            if (nextMessageId == 0)
            {
                nextMessageId = 1;
            }
            return rc;
        }
        private Listener mqttListener;
        public Listener MqttListener
        {
            set
            {
                mqttListener = value;
            }
        }
        private Socket _socket;
        const int MAX_BUFFER_SIZE = 1024 * 40;
        const int socketReadBufferSize = 1024 * 10;
        const int RECURSIVE_PING_INTERVAL = 50;//seconds
        const int PING_CALLBACK_WAIT_TIME = 20;//seconds
        private byte[] bufferForSocketReads;
        private String id;
        private List<byte> combinedMessageBytes = new List<byte>();
        //        private volatile bool connected;
        private volatile bool connackReceived;

        private int _lastReadTime;//long is not atomic
        private int _lastWriteTime;

        const int MaxSecondsPerHour = 3600;

        private Object msgMapLockObj = new object();
        private Object scheduleActionMapLockObj = new object();
        private IDisposable pingFailureAction;
        private IDisposable pingScheduleAction;
        private Dictionary<short, Callback> msgCallbacksMap = new Dictionary<short, Callback>();
        private Dictionary<short, IDisposable> scheduledActionsMap = new Dictionary<short, IDisposable>();

        private void MsgCallbacksMapRemove(short messageId)
        {
            lock (msgMapLockObj)
            {
                msgCallbacksMap.Remove(messageId);
            }
        }

        private void MsgCallbacksMapAdd(short messagId, Callback cb)
        {
            lock (msgMapLockObj)
            {
                msgCallbacksMap[messagId] = cb;
            }
        }

        private Callback MsgCallBacksMapGetValue(short messageId)
        {
            lock (msgMapLockObj)
            {
                Callback cb;
                msgCallbacksMap.TryGetValue(messageId, out cb);
                return cb;
            }
        }

        private void MsgCallbacksMapClear()
        {
            lock (msgMapLockObj)
            {
                foreach (KeyValuePair<short, Callback> kvp in msgCallbacksMap)
                {
                    try
                    {
                        kvp.Value.onFailure(new TimeoutException("Couldn't get Ack for retryable Message id=" + kvp.Key));
                    }
                    catch (Exception ex)
                    {
                        MQttLogging.LogWriter.Instance.WriteToLog(string.Format("MqttConnection::MsgCallBackMapClear:Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));
                    }
                }
                msgCallbacksMap.Clear();
            }
        }

        private void ScheduledActionsMapRemove(short messageId)
        {
            lock (scheduleActionMapLockObj)
            {
                scheduledActionsMap.Remove(messageId);
            }
        }

        private void ScheduledActionsMapAdd(short messagId, IDisposable scheduledAction)
        {
            lock (scheduleActionMapLockObj)
            {
                scheduledActionsMap[messagId] = scheduledAction;
            }
        }

        private IDisposable ScheduledActionsMapGetValue(short messageId)
        {
            lock (scheduleActionMapLockObj)
            {
                IDisposable scheduledAction;
                scheduledActionsMap.TryGetValue(messageId, out scheduledAction);
                return scheduledAction;
            }
        }

        private void ScheduledActionsMapClear()
        {
            lock (scheduleActionMapLockObj)
            {
                foreach (IDisposable action in scheduledActionsMap.Values)
                {
                    try
                    {
                        action.Dispose();
                    }
                    catch (Exception ex)
                    {
                        MQttLogging.LogWriter.Instance.WriteToLog(string.Format("MqttConnection::ScheduledActionsMapClear:Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));
                    }
                }
                scheduledActionsMap.Clear();
            }
        }

        private IScheduler scheduler = Scheduler.NewThread;

        private String host;
        private int port;
        private String username;
        private String password;
        private Callback connectCallback;

        private MessageStream input;

        public delegate Callback onAckFailedDelegate(short messageId);

        public MqttConnection(String id, String host, int port, String username, String password, Callback cb, Listener listener)
        {
            this.bufferForSocketReads = new byte[socketReadBufferSize];
            this.id = id;
            this.input = new MessageStream(MAX_BUFFER_SIZE);
            this.mqttListener = listener;
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
            this.connectCallback = cb;
        }

        DateTime dt;
        /// <summary>
        /// Initiates connect request to server.
        /// </summary>
        public void connect()
        {
            dt = DateTime.Now;
            DnsEndPoint hostEntry = new DnsEndPoint(host, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onSocketConnected);
            _socket.ConnectAsync(socketEventArg);
        }

        /// <summary>
        /// AsyncCallback of socket connection. Is called when response of socket connection is received. 
        /// It sends a connect message and then starts reading from socket.  
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void onSocketConnected(object s, SocketAsyncEventArgs e)
        {
            Double timeTaken = (DateTime.Now - dt).TotalSeconds;
            firstmessage = true;
            //connected = _socket.Connected;
            if (e.SocketError != SocketError.Success)
            {
                connectCallback.onFailure(new Exception(e.SocketError.ToString()));

                MQttLogging.LogWriter.Instance.WriteToLog("MQTT socket connection FAILED, time taken :" + timeTaken + " secs");
                return;
            }
            MQttLogging.LogWriter.Instance.WriteToLog("MQTT socket connection SUCCESS, time taken :" + timeTaken + " secs");
            ConnectMessage msg = new ConnectMessage(id, false, (byte)60, this);
            if (username != null)
                msg.setCredentials(username, password);
            msg.write();
            read();
        }
        /// <summary>
        /// It intiates read request from the socket connected with a buffer where data from socket would be copied.
        /// </summary>
        private void read()
        {
            if (_socket != null)
            {
                try
                {
                    SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                    socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                    socketEventArg.SetBuffer(bufferForSocketReads, 0, socketReadBufferSize);
                    socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onReadCompleted);
                    _socket.ReceiveAsync(socketEventArg);
                }
                catch (Exception e)
                {
                    MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT::READ Error, Exception:" + e.Message);
                    if (_socket != null)
                    {
                        _socket.Close();
                        _socket = null;
                    }
                    disconnect();
                }
            }
        }
        /// <summary>
        /// asynccallback for reading socket. Is called when the data from socket is received.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void onReadCompleted(object s, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    input.writeBytes(e.Buffer, e.Offset, e.BytesTransferred);
                    _lastReadTime = GetCurrentSeconds();
                }
                else
                {
                    if (_socket != null)
                    {
                        _socket.Close();
                        _socket = null;
                    }
                    MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT::onReadCompleted, Error:" + e.SocketError);

                    disconnect();
                    return;
                }
                readMessagesFromBuffer();
                read();
            }
            catch (Exception ex)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT::onReadCompleted, Exception:" + ex.Message);

                disconnect();
            }

        }

        /// <summary>
        /// Reads messages in while loop from buffer (written by onReadCompleted). If a message is incomplete in buffer,
        /// exception occurs which is ignored
        /// </summary>
        private void readMessagesFromBuffer()
        {
            while (input.containsCompleteMessage())
            {
                Message message = readMessage();
                handleMessage(message);
            }
        }

        private void onDataSent(object s, SocketAsyncEventArgs e)
        {
            _lastWriteTime = GetCurrentSeconds();
        }

        /// <summary>
        /// sends raw bytes of data through socket
        /// </summary>
        /// <param name="data"></param>
        public void sendMessage(byte[] data)
        {
            try
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onDataSent);
                socketEventArg.SetBuffer(data, 0, data.Length);
                _socket.SendAsync(socketEventArg);

                var str = Encoding.UTF8.GetString(data, 0, data.Length);
                MQttLogging.LogWriter.Instance.WriteToLog("MSG SENT:: " + str);

            }
            catch (Exception ex)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }

                MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT::sendMessage, Exception:" + ex.Message);

                disconnect();
            }
        }

        /// <summary>
        /// reads message from messagestream buffer. It reads first byte of flags, which is passed further to restore buffer
        /// if buffer does not  contains complete message as of now. An exception is thrown which is ignored by caller
        /// </summary>
        /// <returns></returns>
        public Message readMessage()
        {
            byte flags = input.readByte();
            Header header = new Header(flags);
            Message msg = null;
            MessageType type = header.getType();
            switch (type)
            {
                case MessageType.CONNACK:
                    msg = new ConnAckMessage(header, this);
                    break;
                case MessageType.PUBLISH:
                    msg = new PublishMessage(header, this);
                    break;
                case MessageType.PUBACK:
                    msg = new PubAckMessage(header, this);
                    break;
                case MessageType.PUBREC:
                    msg = new PubRecMessage(header, this);
                    break;
                case MessageType.PUBREL:
                    msg = new PubRelMessage(header, this);
                    break;
                case MessageType.SUBACK:
                    msg = new SubAckMessage(header, this);
                    break;
                case MessageType.SUBSCRIBE:
                    msg = new SubscribeMessage(header, this);
                    break;
                case MessageType.UNSUBACK:
                    msg = new UnsubAckMessage(header, this);
                    break;
                case MessageType.PINGRESP:
                    msg = new PingRespMessage(header, this);
                    break;
                case MessageType.PINGREQ:
                    msg = new PingReqMessage(header, this);
                    break;
                case MessageType.DISCONNECT:
                    msg = new DisconnectMessage(header, this);
                    break;
                case MessageType.CONNECT:
                    msg = new ConnectMessage(header, this);
                    break;
                default:
                    throw new NotSupportedException("No support for deserializing " + header.getType() + " messages");
            }
            try
            {
                msg.read(input);
            }
            catch (IndexOutOfRangeException outOfRange)
            {
                throw outOfRange;
            }
            return msg;
        }
        bool firstmessage = false;
        /// <summary>
        /// Writes message to socket. If message is of retryable type, its id and callback are inserted in a map.
        /// If ack is not received in 5 seconds, then callback's onFailure is called
        /// </summary>
        /// <param name="msg">Message to be written (or sent)</param>
        /// <param name="scheduledAction">Callback to be called in case of error</param>
        public void sendCallbackMessage(Message msg, Callback cb)
        {
            if (_socket == null || !_socket.Connected || !connackReceived || msg == null)
            {
                if (cb != null)
                    cb.onFailure(null);
                return;
            }
            try
            {
                msg.write();
                if (msg is RetryableMessage && cb != null)
                {
                    short messageId = ((RetryableMessage)msg).getMessageId();
                    if (messageId != 0)
                    {
                        MsgCallbacksMapAdd(messageId, cb);
                        Action callbackMessageAction = (new CallBackTimerTask(new onAckFailedDelegate(onReceivingAck), messageId, cb)).HandleTimerTask;
                        IDisposable scheduledAction = scheduler.Schedule(callbackMessageAction, TimeSpan.FromSeconds(10));
                        ScheduledActionsMapAdd(messageId, scheduledAction);
                    }
                }
            }
            catch (ObjectDisposedException ode)
            {
                if (cb != null)
                    cb.onFailure(ode);
            }
            catch (SocketException se)
            {
                if (cb != null)
                    cb.onFailure(se);
            }

        }

        public Callback onReceivingAck(short messageId)
        {
            Callback cb = MsgCallBacksMapGetValue(messageId);
            if (cb != null)
            {
                MQttLogging.LogWriter.Instance.WriteToLog("Ack Not recieved for message id:" + messageId);
                MsgCallbacksMapRemove(messageId);
            }
            ScheduledActionsMapRemove(messageId);
            return cb;
        }

        public void sendCallbackMessage(Message[] msg, Callback[] cb)
        {
            if (_socket == null || !_socket.Connected || !connackReceived || msg == null || cb == null)
            {
                if (cb != null)
                {
                    for (int i = 0; i < cb.Length; i++)
                    {
                        if (cb[i] != null)
                            cb[i].onFailure(null);
                    }
                }
                return;
            }
            try
            {
                for (int i = 0; i < msg.Length; i++)
                {
                    combinedMessageBytes.AddRange(msg[i].messageContent());
                }
                sendMessage(combinedMessageBytes.ToArray());
                combinedMessageBytes.Clear();
            }
            catch (ObjectDisposedException ode)
            {
                for (int i = 0; i < cb.Length; i++)
                {
                    if (cb[i] != null)
                        cb[i].onFailure(ode);
                }
            }
            catch (SocketException se)
            {
                for (int i = 0; i < cb.Length; i++)
                {
                    if (cb[i] != null)
                        cb[i].onFailure(se);
                }
            }

            for (int i = 0; i < msg.Length; i++)
            {
                if (msg[i] is RetryableMessage && cb[i] != null)
                {
                    short messageId = ((RetryableMessage)msg[i]).getMessageId();
                    if (messageId != 0)
                    {
                        MsgCallbacksMapAdd(messageId, cb[i]);
                    }
                }
            }
        }

        #region PING

        public void ping()// throws IOException
        {
            PingReqMessage msg = new PingReqMessage(this);
            sendCallbackMessage(msg, null);
            pingFailureAction = scheduler.Schedule(onPingFailure, TimeSpan.FromSeconds(PING_CALLBACK_WAIT_TIME));
        }

        private void recursivePingSchedule()
        {
            MQttLogging.LogWriter.Instance.WriteToLog("RECURSIVE PING CALLED, Time:" + DateTime.Now);
            if (this.mqttListener != null && _socket != null)
            {
                int lastActivityTime = Math.Min(_lastReadTime, _lastWriteTime);

                int currentTime = GetCurrentSeconds();

                if (lastActivityTime > currentTime)
                    lastActivityTime -= MaxSecondsPerHour;
                int timeDiff = currentTime - lastActivityTime;

                if (timeDiff >= RECURSIVE_PING_INTERVAL)
                {
                    MQttLogging.LogWriter.Instance.WriteToLog("PING CALLED AND SCHEDULED, Time:" + DateTime.Now);
                    ping();
                }

                var nextPingTime = timeDiff < RECURSIVE_PING_INTERVAL ? RECURSIVE_PING_INTERVAL - timeDiff : RECURSIVE_PING_INTERVAL;
                pingScheduleAction = scheduler.Schedule(recursivePingSchedule, TimeSpan.FromSeconds(nextPingTime));
                MQttLogging.LogWriter.Instance.WriteToLog("NEXT PING TIME:" + DateTime.Now.AddSeconds(nextPingTime));
            }
        }

        private void onPingFailure()
        {
            int currentTime = GetCurrentSeconds();
            if (_lastReadTime > currentTime)
                _lastReadTime -= MaxSecondsPerHour;

            if ((currentTime - _lastReadTime) > PING_CALLBACK_WAIT_TIME)
            {
                MQttLogging.LogWriter.Instance.WriteToLog("On Ping Failure Called,Time:" + DateTime.Now);
                MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT::PingFailure");

                disconnect();
                pingFailureAction = null;
            }
        }

        #endregion

        public void publish(String topic, byte[] message, QoS qos, Callback cb) //throws IOException 
        {

            PublishMessage msg = new PublishMessage(topic, message, qos, this);
            if (msg is PublishMessage && firstmessage)
            {
                listSubscribe[((PublishMessage)msg).getMessageId()] = DateTime.Now;
                firstmessage = false;
            }
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
        }

        public void publish(String topic, byte[][] message, QoS qos, Callback[] cb) //throws IOException 
        {

            PublishMessage[] messagesToPublish = new PublishMessage[cb.Length];
            for (int i = 0; i < message.Length; i++)
            {
                messagesToPublish[i] = new PublishMessage(topic, message[i], qos, this);
                if (messagesToPublish[i] is PublishMessage && firstmessage)
                {
                    listSubscribe[((PublishMessage)messagesToPublish[i]).getMessageId()] = DateTime.Now;
                    firstmessage = false;
                }
                messagesToPublish[i].setMessageId(getNextMessageId());
            }
            sendCallbackMessage(messagesToPublish, cb);
        }
        Dictionary<short, DateTime> listSubscribe = new Dictionary<short, DateTime>();
        public void subscribe(String topic, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(topic, QoS.AT_MOST_ONCE, this);
            short messageid = getNextMessageId();
            msg.setMessageId(messageid);

            sendCallbackMessage(msg, cb);
        }
        public void subscribe(String topic, QoS qos, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(topic, qos, this);
            short messageid = getNextMessageId();
            msg.setMessageId(messageid);
            sendCallbackMessage(msg, cb);
        }
        public void subscribe(List<String> listTopics, List<QoS> listQos, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(listTopics, listQos, this);
            short messageid = getNextMessageId();
            msg.setMessageId(messageid);
            sendCallbackMessage(msg, cb);
        }
        public void unsubscribe(String topic, Callback cb) //throws IOException 
        {
            UnsubscribeMessage msg = new UnsubscribeMessage(topic, this);
            sendCallbackMessage(msg, cb);
        }

        public void disconnect() //throws IOException 
        {
            MQttLogging.LogWriter.Instance.WriteToLog("DISCONNECT CALLED");
            try
            {

                ClearPageResources();

                if (mqttListener != null)
                {
                    mqttListener.onDisconnected();
                }
            }
            //to make sure if there is any exception in clearing page resources, app should work fine 
            catch (Exception ex)
            {
                MQttLogging.LogWriter.Instance.WriteToLog(string.Format("MqttConnection::disconnect :Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));

                if (_socket != null)
                {
                    _socket.Dispose();
                    _socket.Close();
                    _socket = null;
                }

                if (mqttListener != null)
                {
                    mqttListener.onDisconnected();
                }
            }
        }

        /// <summary>
        /// it handles messages read from socket. If it exists in map (i.e. it is a response message from server) then its
        /// entry is removed from map, so that its failure callback is not called
        /// </summary>
        /// <param name="msg"></param>
        private void handleMessage(Message msg)
        {
            if (msg == null)
            {
                return;
            }
            if (msg is RetryableMessage)
            {
                short messageId = ((RetryableMessage)msg).getMessageId();
                if (messageId != 0)
                {
                    Callback cb;
                    IDisposable scheduledAction;
                    cb = MsgCallBacksMapGetValue(messageId);
                    if (cb != null)
                    {
                        scheduledAction = ScheduledActionsMapGetValue(messageId);
                        if (scheduledAction != null)
                        {
                            ScheduledActionsMapRemove(messageId);
                            scheduledAction.Dispose();
                        }
                        MQttLogging.LogWriter.Instance.WriteToLog("MQTTCONNECTION:: Disposing action for message ID - " + messageId);
                        MsgCallbacksMapRemove(messageId);
                        cb.onSuccess();
                        MQttLogging.LogWriter.Instance.WriteToLog("Success called for message ID:" + messageId);
                    }
                }
            }
            switch (msg.getType())
            {
                case MessageType.CONNACK:
                    handleMessage((ConnAckMessage)msg);
                    break;
                case MessageType.PUBLISH:
                    handleMessage((PublishMessage)msg);
                    break;
                case MessageType.PINGRESP:
                    handleMessage((PingRespMessage)msg);
                    break;
                case MessageType.PUBACK:
                    handleMessage((PubAckMessage)msg);
                    PubAckMessage puback = (PubAckMessage)msg;
                    MQttLogging.LogWriter.Instance.WriteToLog("PubAck recieved for message ID:" + puback.getMessageId());
                    break;
                case MessageType.SUBACK:
                    handleMessage((SubAckMessage)msg);
                    break;
                default:
                    break;
            }
        }

        protected void handleMessage(ConnAckMessage msg)
        {
            connackReceived = true;
            if (msg.getStatus() != ConnAckMessage.ConnectionStatus.ACCEPTED)
            {
                connectCallback.onFailure(new ConnectionException("Unable to connect to server", msg.getStatus()));
                return;
            }
            if (mqttListener != null)
                mqttListener.onConnected();
            connectCallback.onSuccess();
            if (pingScheduleAction == null)
                pingScheduleAction = scheduler.Schedule(recursivePingSchedule, TimeSpan.FromSeconds(RECURSIVE_PING_INTERVAL));

        }

        protected void handleMessage(PublishMessage msg)
        {
            try
            {
                DateTime dt1;

                if (listSubscribe.TryGetValue(msg.getMessageId(), out dt1))
                {
                    Double timeTaken = (DateTime.Now - dt1).TotalSeconds;
                    MQttLogging.LogWriter.Instance.WriteToLog("Ack for first message published, time taken :" + timeTaken + " secs");
                    listSubscribe.Remove(msg.getMessageId());
                }

                sendAcknowledement(msg);
                if (mqttListener != null)
                    mqttListener.onPublish(msg.getTopic(), msg.getData());
            }
            catch (Exception ex)
            {
                MQttLogging.LogWriter.Instance.WriteToLog("MqttConnection :: handleMessage : Exception -" + ex.StackTrace);
            }
        }

        protected void handleMessage(PingRespMessage msg)
        {
            if (pingFailureAction != null)
            {
                MQttLogging.LogWriter.Instance.WriteToLog("Ping Response recieved");
                pingFailureAction.Dispose();
                pingFailureAction = null;
            }
        }
        protected void handleMessage(PubAckMessage msg)
        {
        }
        protected void handleMessage(SubAckMessage msg)
        {
            DateTime dt1;

            if (listSubscribe.TryGetValue(msg.getMessageId(), out dt1))
            {
                Double timeTaken = (dt1 - DateTime.Now).TotalSeconds;
                MQttLogging.LogWriter.Instance.WriteToLog("AUTO SUBSCRIPTION, time taken :" + timeTaken + " secs");
            }
            else
            {
                MQttLogging.LogWriter.Instance.WriteToLog("AUTO SUBSCRIPTION, key not found for messageid:" + msg.getMessageId());
            }
        }

        /// <summary>
        /// sends acknowledgement of every publish message recieved. If server does not receive ack, then it keeps on sending
        /// publish messages
        /// </summary>
        /// <param name="msg"></param>
        protected void sendAcknowledement(RetryableMessage msg)
        {
            short messageId = msg.getMessageId();
            if (msg.getQos() == QoS.AT_LEAST_ONCE || msg.getQos() == QoS.EXACTLY_ONCE)
            {
                Message puback = new PubAckMessage(messageId, this);
                puback.write();
            }
        }

        private void ClearPageResources()
        {

            MQttLogging.LogWriter.Instance.WriteToLog("CLEARING PAGE RESOURCES");
            ScheduledActionsMapClear();
            MsgCallbacksMapClear();
            if (pingScheduleAction != null)
            {
                MQttLogging.LogWriter.Instance.WriteToLog("PING DISPOSED");

                pingScheduleAction.Dispose();
                pingScheduleAction = null;
            }
            if (_socket != null)
            {
                _socket.Dispose();
                _socket.Close();
                _socket = null;
            }
        }

        private int GetCurrentSeconds()
        {
            return DateTime.Now.Hour * 60 + DateTime.Now.Second;
        }

    }
}

