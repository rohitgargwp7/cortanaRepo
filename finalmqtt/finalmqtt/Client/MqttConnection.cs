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
        private byte[] bufferForSocketReads;
        private readonly String id;
        private volatile bool stopped;
        //        private volatile bool connected;
        private volatile bool connackReceived;
        private volatile bool disconnectSent = false;

        private Dictionary<short, Callback> map = new Dictionary<short, Callback>();
        private IScheduler scheduler = Scheduler.NewThread;

        private String host;
        private int port;
        private String username;
        private String password;
        private Callback connectCallback;

        private MessageStream input;
        private List<byte> pendingOutput;

        public MqttConnection(String id, String host, int port, String username, String password, Callback cb, Listener listener)
        {
            this.id = id;
            this.stopped = true;
            //            this.connected = false;
            this.input = new MessageStream(MAX_BUFFER_SIZE);
            this.mqttListener = listener;
            this.pendingOutput = new List<byte>();
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
            this.connectCallback = cb;
            this.bufferForSocketReads = new byte[socketReadBufferSize];
        }

        public MqttConnection(String id, String host, int port, String username, String password, Callback connectCallback)
            : this(id, host, port, username, password, connectCallback, null)
        {
        }
        /// <summary>
        /// Initiates connect request to server.
        /// </summary>
        public void connect()
        {
            stopped = false;
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
                    _socket.ReceiveAsync(socketEventArg);
                }
                catch (Exception e)
                {
                    if (_socket != null)
                    {
                        _socket.Close();
                        _socket = null;
                    }
                    mqttListener.onDisconnected();
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
                }
                else
                {
                    if (mqttListener != null)
                    {
                        if (_socket != null)
                        {
                            _socket.Close();
                            _socket = null;
                        }
                        mqttListener.onDisconnected();
                    }
                }
                readMessagesFromBuffer();
                read();
            }
            catch (Exception)
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                mqttListener.onDisconnected();
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
            if (disconnectSent)
            {
                if (_socket != null)
                    _socket.Close();
                disconnectSent = false;
            }
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
            }
            catch
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                mqttListener.onDisconnected();
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
        /// <param name="cb">Callback to be called in case of error</param>
        public void sendCallbackMessage(Message msg, Callback cb)
        {
            if (!_socket.Connected || !connackReceived)
            {
                cb.onFailure(null);
                return;
            }
            try
            {
                msg.write();

            }
            catch (ObjectDisposedException ode)
            {
                cb.onFailure(ode);
            }
            catch (SocketException se)
            {
                cb.onFailure(se);
            }

            if (msg is RetryableMessage)
            {
                short messageId = ((RetryableMessage)msg).getMessageId();
                if (messageId != 0)
                {
                    map.Add(messageId, cb);
                    Action callbackMessageAction = (new CallBackTimerTask(map, messageId, cb)).HandleTimerTask;
                    scheduler.Schedule(callbackMessageAction, TimeSpan.FromSeconds(10));
                }
            }
        }


        public bool ping(Callback cb)// throws IOException
        {
            PingReqMessage msg = new PingReqMessage(this);
            sendCallbackMessage(msg, cb);
            return true;
        }
        public void publish(String topic, byte[] message, QoS qos, Callback cb) //throws IOException 
        {
            PublishMessage msg = new PublishMessage(topic, message, qos, this);
            msg.setMessageId(getNextMessageId());
            sendCallbackMessage(msg, cb);
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

        public void unsubscribe(String topic, Callback cb) //throws IOException 
        {
            UnsubscribeMessage msg = new UnsubscribeMessage(topic, this);
            sendCallbackMessage(msg, cb);
        }

        public void disconnect(Callback cb) //throws IOException 
        {
            stopped = true;
            if (_socket.Connected)
            {
                DisconnectMessage msg = new DisconnectMessage(this);
                foreach (KeyValuePair<short, Callback> kvp in map)
                {
                    kvp.Value.onFailure(new TimeoutException("Couldn't get Ack for retryable Message id=" + kvp.Key));
                }
                map.Clear();
                disconnectSent = true;
                sendCallbackMessage(msg, cb);
            }
            //if (_socket != null)
            //    _socket.Close();
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
                    if (map.ContainsKey(messageId))
                    {
                        map.TryGetValue(messageId, out cb);
                        map.Remove(messageId);
                        if (cb != null)
                        {
                            cb.onSuccess();
                        }
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
            }
            if (mqttListener != null)
                mqttListener.onConnected();
            connectCallback.onSuccess();
        }

        protected void handleMessage(PublishMessage msg)
        {
            sendAcknowledement(msg);
            if (mqttListener != null)
                mqttListener.onPublish(msg.getTopic(), msg.getData());
        }

        protected void handleMessage(PingRespMessage msg)
        {

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
    }
}
