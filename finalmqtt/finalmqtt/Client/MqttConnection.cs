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
                        Debug.WriteLine(string.Format("MqttConnection::MsgCallBackMapClear:Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));
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
                        Debug.WriteLine(string.Format("MqttConnection::ScheduledActionsMapClear:Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));
                    }
                }
                scheduledActionsMap.Clear();
            }
        }

        private IScheduler scheduler = Scheduler.NewThread;

        private String username;
        private String password;
        private Callback connectCallback;

        private MessageStream input;

        public delegate Callback onAckFailedDelegate(short messageId);

        public MqttConnection(String id, String username, String password, Callback cb, Listener listener)
        {
            this.bufferForSocketReads = new byte[socketReadBufferSize];
            this.id = id;
            this.input = new MessageStream(MAX_BUFFER_SIZE);
            this.mqttListener = listener;
            this.username = username;
            this.password = password;
            this.connectCallback = cb;
        }

        /// <summary>
        /// Initiates connect request to server.
        /// </summary>
        public void connect(String host, int port)
        {
            DnsEndPoint hostEntry = new DnsEndPoint(host, port);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.NoDelay = true;
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onSocketConnected);
            if (!_socket.ConnectAsync(socketEventArg))
                ProcessSocketConnected(socketEventArg);
        }

        /// <summary>
        /// AsyncCallback of socket connection. Is called when response of socket connection is received. 
        /// It sends a connect message and then starts reading from socket.  
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void onSocketConnected(object s, SocketAsyncEventArgs e)
        {
            ProcessSocketConnected(e);
        }

        /// <summary>
        /// Process callback of socket connection
        /// </summary>
        /// <param name="e">Socket event arguement</param>
        private void ProcessSocketConnected(SocketAsyncEventArgs e)
        {
            //connected = _socket.Connected;
            if (e.SocketError != SocketError.Success)
            {
                connectCallback.onFailure(new Exception(e.SocketError.ToString()));
                return;
            }
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

                    if (!_socket.ReceiveAsync(socketEventArg))
                        ProcessDataRead(socketEventArg);
                }
                catch (Exception e)
                {
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
            ProcessDataRead(e);
        }

        /// <summary>
        /// Process callback of data read from socket
        /// </summary>
        /// <param name="e"></param>
        private void ProcessDataRead(SocketAsyncEventArgs e)
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
                    disconnect();
                    return;
                }
                readMessagesFromBuffer();
                read();
            }
            catch (Exception)
            {
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

        /// <summary>
        /// Async Callback for succesful data sent
        /// </summary>
        /// <param name="s"></param>
        /// <param name="e"></param>
        private void onDataSent(object s, SocketAsyncEventArgs e)
        {
            OnDataSentSuccess(e as SocketEventArguemntsMessageId);
        }

        private void OnDataSentSuccess(SocketEventArguemntsMessageId e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.MessageId > 0)
                {
                    ProcessAckForMessage(e.MessageId);
                }
                else
                {
                    List<short> listMessageId = e.MessageIdList;
                    if (listMessageId != null && listMessageId.Count > 0)
                    {
                        foreach (short messageId in listMessageId)
                        {
                            ProcessAckForMessage(messageId);
                        }
                    }
                }
                _lastWriteTime = GetCurrentSeconds();
            }
        }

        private void ProcessAckForMessage(short messageId)
        {
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
                    MsgCallbacksMapRemove(messageId);
                    cb.onSuccess();
                }
            }
        }

        /// <summary>
        /// sends raw bytes of data through socket
        /// </summary>
        /// <param name="data"></param>
        public void sendMessage(byte[] data, short messageId)
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {
                    SocketEventArguemntsMessageId socketEventArg = new SocketEventArguemntsMessageId();
                    socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                    socketEventArg.UserToken = null;
                    socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onDataSent);
                    socketEventArg.SetBuffer(data, 0, data.Length);
                    socketEventArg.MessageId = messageId;
                    if (!_socket.SendAsync(socketEventArg))
                        OnDataSentSuccess(socketEventArg);
                }
            }
            catch
            {
                disconnect();
            }
        }

        /// <summary>
        /// sends raw bytes of data through socket
        /// </summary>
        /// <param name="data"></param>
        public void sendMessage(byte[] data, List<short> messageIdList)
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {
                    SocketEventArguemntsMessageId socketEventArg = new SocketEventArguemntsMessageId();
                    socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                    socketEventArg.UserToken = null;
                    socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onDataSent);
                    socketEventArg.SetBuffer(data, 0, data.Length);
                    socketEventArg.MessageIdList = messageIdList;
                    if (!_socket.SendAsync(socketEventArg))
                        OnDataSentSuccess(socketEventArg);
                }
            }
            catch
            {
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
                List<short> listMessageIds = new List<short>();
                for (int i = 0; i < msg.Length; i++)
                {
                    Message messsage = msg[i];
                    listMessageIds.Add((short)(messsage is RetryableMessage ? ((RetryableMessage)messsage).getMessageId() : 0));
                    combinedMessageBytes.AddRange(messsage.messageContent());
                }
                sendMessage(combinedMessageBytes.ToArray(), listMessageIds);
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
            Debug.WriteLine("RECURSIVE PING CALLED, Time:" + DateTime.Now);
            if (this.mqttListener != null && _socket != null)
            {
                int lastActivityTime = Math.Min(_lastReadTime, _lastWriteTime);

                int currentTime = GetCurrentSeconds();

                if (lastActivityTime > currentTime)
                    lastActivityTime -= MaxSecondsPerHour;
                int timeDiff = currentTime - lastActivityTime;

                if (timeDiff >= RECURSIVE_PING_INTERVAL)
                {
                    Debug.WriteLine("PING CALLED AND SCHEDULED, Time:" + DateTime.Now);
                    ping();
                }

                var nextPingTime = timeDiff < RECURSIVE_PING_INTERVAL ? RECURSIVE_PING_INTERVAL - timeDiff : RECURSIVE_PING_INTERVAL;
                pingScheduleAction = scheduler.Schedule(recursivePingSchedule, TimeSpan.FromSeconds(nextPingTime));
                Debug.WriteLine("NEXT PING TIME:" + DateTime.Now.AddSeconds(nextPingTime));
            }
        }

        private void onPingFailure()
        {
            int currentTime = GetCurrentSeconds();
            if (_lastReadTime > currentTime)
                _lastReadTime -= MaxSecondsPerHour;

            if ((currentTime - _lastReadTime) > PING_CALLBACK_WAIT_TIME)
            {
                Debug.WriteLine("On Ping Failure Called,Time:" + DateTime.Now);
                disconnect();
                pingFailureAction = null;
            }
        }

        #endregion

        public void publish(String topic, byte[] message, QoS qos, Callback cb) //throws IOException 
        {
            PublishMessage msg = new PublishMessage(topic, message, qos, this);
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
        }

        public void publish(String topic, byte[][] message, QoS qos, Callback[] cb) //throws IOException 
        {
            PublishMessage[] messagesToPublish = new PublishMessage[cb.Length];
            for (int i = 0; i < message.Length; i++)
            {
                messagesToPublish[i] = new PublishMessage(topic, message[i], qos, this);
                messagesToPublish[i].setMessageId(getNextMessageId());
            }
            sendCallbackMessage(messagesToPublish, cb);
        }

        public void subscribe(String topic, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(topic, QoS.AT_MOST_ONCE, this);
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
        }
        public void subscribe(String topic, QoS qos, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(topic, qos, this);
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
        }
        public void subscribe(List<String> listTopics, List<QoS> listQos, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(listTopics, listQos, this);
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
        }
        public void unsubscribe(String topic, Callback cb) //throws IOException 
        {
            UnsubscribeMessage msg = new UnsubscribeMessage(topic, this);
            sendCallbackMessage(msg, cb);
        }

        public void disconnect() //throws IOException 
        {
            try
            {
                Debug.WriteLine("DISCONNECT CALLED");

                ClearPageResources();

                if (mqttListener != null)
                {
                    mqttListener.onDisconnected();
                }
            }
            //to make sure if there is any exception in clearing page resources, app should work fine 
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("MqttConnection::disconnect :Exception:{0}, StackTrace:{1}", ex.Message, ex.StackTrace));

                if (_socket != null)
                {
                    if (_socket.Connected)//if not connected it throws exception that its not connected
                        _socket.Shutdown(SocketShutdown.Both);
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
                ProcessAckForMessage(messageId);
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
                sendAcknowledement(msg);
                if (mqttListener != null)
                    mqttListener.onPublish(msg.getTopic(), msg.getData());
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MqttConnection :: handleMessage : Exception -" + ex.StackTrace);
            }
        }

        protected void handleMessage(PingRespMessage msg)
        {
            if (pingFailureAction != null)
            {
                Debug.WriteLine("Ping Response recieved");
                pingFailureAction.Dispose();
                pingFailureAction = null;
            }
        }
        protected void handleMessage(PubAckMessage msg)
        {
        }
        protected void handleMessage(SubAckMessage msg)
        {
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
            Debug.WriteLine("CLEARING PAGE RESOURCES");
            ScheduledActionsMapClear();
            MsgCallbacksMapClear();
            if (pingScheduleAction != null)
            {
                Debug.WriteLine("PING DISPOSED");

                pingScheduleAction.Dispose();
                pingScheduleAction = null;
            }
            if (_socket != null)
            {
                //For connection-oriented protocols, it is recommended that you call Shutdown before calling the Close method. 
                //http://msdn.microsoft.com/en-us/library/wahsac9k(v=vs.110).aspx
                if (_socket.Connected)//if not connected it throws exception that its not connected
                    _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socket = null;
            }

            if (input != null)
            {
                input.ClearStream();
            }
        }

        private int GetCurrentSeconds()
        {
            return DateTime.Now.Minute * 60 + DateTime.Now.Second;
        }

    }
}

