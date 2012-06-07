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
        const int MAX_BUFFER_SIZE = 4096*2;
        private readonly String id;
        private volatile bool stopped;
        private volatile bool connected;
        private volatile bool connackReceived;

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
            this.connected = false;
            this.input = new MessageStream(MAX_BUFFER_SIZE);
            this.mqttListener = listener;
            this.pendingOutput = new List<byte>();
            this.host = host;
            this.port = port;
            this.username = username;
            this.password = password;
            this.connectCallback = cb;
        }

        public MqttConnection(String id, String host, int port, String username, String password, Callback connectCallback)
            : this(id, host, port, username, password, connectCallback ,null)
        {
        }

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

        private void onSocketConnected(object s, SocketAsyncEventArgs e)
        {
            connected = _socket.Connected;
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

        private void read()
        {
            if (_socket != null)
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.SetBuffer(new Byte[MAX_BUFFER_SIZE], 0, MAX_BUFFER_SIZE);
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onReadCompleted);
                _socket.ReceiveAsync(socketEventArg);
            }
        }

        private void onReadCompleted(object s, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                input.writeBytes(e.Buffer, e.Offset, e.BytesTransferred);
            }
            else
            {
                if(mqttListener!=null)
                    mqttListener.onDisconnected();
            }
            if (input.Size() > 0)
            {
                Message message = readMessage();
                handleMessage(message);
            }
            read();
        }

        private void onDataSent(object s, SocketAsyncEventArgs e)
        {

        }

        public void sendMessage(byte[] data)
        {
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
            socketEventArg.UserToken = null;
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(onDataSent);
            socketEventArg.SetBuffer(data, 0, data.Length);
            _socket.SendAsync(socketEventArg);
            
        }

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
            msg.read(input);
            return msg;
        }

        public void sendCallbackMessage(Message msg, Callback cb)
        {
            if (!connected || !connackReceived)
            {
                cb.onFailure(null);
                return;
            }
            msg.write();
            if (msg is RetryableMessage)
            {
                //short messageId = ((RetryableMessage)msg).getMessageId();
                //map.Add(messageId, cb);
                //Action callbackMessageAction = (new CallBackTimerTask(map, messageId, cb)).HandleTimerTask;
                //scheduler.Schedule(callbackMessageAction, TimeSpan.FromSeconds(15));
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
            sendCallbackMessage(msg, cb);
        }
        public void subscribe(String topic, QoS qos, Callback cb) //throws IOException 
        {
            SubscribeMessage msg = new SubscribeMessage(topic, qos, this);
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
            if (connected)
            {
                DisconnectMessage msg = new DisconnectMessage(this);
                foreach (KeyValuePair<short,Callback> kvp in map)
                {
                    kvp.Value.onFailure(new TimeoutException("Couldn't get Ack for retryable Message id=" + kvp.Key));
                }
                map.Clear();
                sendCallbackMessage(msg, cb);
            }
            if (_socket != null)
                _socket.Close();
        }

        public void writeMessage(Message message) //throws IOException
        {
        }


        private void handleMessage(Message msg)
        {
            if (msg == null)
            {
                return;
            }
            if (msg is RetryableMessage)
            {
                short messageId = ((RetryableMessage)msg).getMessageId();
                map.Remove(messageId);
            }

            switch (msg.getType())
            {
                case MessageType.CONNACK:
                    handleMessage((ConnAckMessage)msg);
                    break;
                case MessageType.PUBLISH:
                    handleMessage((PublishMessage) msg);
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
