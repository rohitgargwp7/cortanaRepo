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
using System.Text;
using System.Collections.Generic;
using finalmqtt.Client;
using mqtttest.Client;

namespace finalmqtt.Msg
{
    public class Message
    {

        protected List<byte> messageData;
        protected MqttConnection mqttConnection;

        private static short _nextId = 1;
        public static short NextId
        {
            get
            {
                return _nextId++;
            }
        }

        public Header header;


        public Message(MessageType type, MqttConnection mqttConnection)
        {
            header = new Header(type, false, QoS.AT_MOST_ONCE, false);
            this.messageData = new List<byte>();
            this.mqttConnection = mqttConnection;
        }

        public Message(Header header, MqttConnection conn)
        {
            this.header = header;
            this.messageData = new List<byte>();
            this.mqttConnection = conn;
        }

        /// <summary>
        /// composes a message object from messagestream. throws exception if the message is incomplete in buffer as of now.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="flags">flags to be restored in message stream in case message is incomplete as of now</param>
        public void read(MessageStream input)
        {
            int msgLength = input.readMsgLength();
            readMessage(input, msgLength);
        }

        public void write()
        {
            messageData.Add(header.encode());
            writeMsgLength();
            writeMessage();
            byte[] data = messageData.ToArray();
            try
            {
                mqttConnection.sendMessage(data);
            }
            catch 
            {
                throw;
            } 
        }


        private void writeMsgLength()
        {
            int msgLength = messageLength();
            int val = msgLength;
            int pos = 1;
            do
            {
                byte b = (byte)(val & 0x7F);
                val >>= 7;
                if (val > 0)
                {
                    b |= 0x80;
                }
                pos++;
                messageData.Add(b);
            } while (val > 0);
        }

        protected virtual int messageLength()
        {
            return 0;
        }

        protected virtual void writeMessage()
        {
        }

        protected virtual void readMessage(MessageStream input, int msgLength)
        {

        }


        public virtual void setRetained(bool retain)
        {
            header.retain = retain;
        }

        public bool isRetained()
        {
            return header.retain;
        }

        public virtual void setQos(QoS qos)
        {
            header.qos = qos;
        }

        public QoS getQos()
        {
            return header.qos;
        }

        public virtual void setDup(bool dup)
        {
            header.dup = dup;
        }

        public virtual bool isDup()
        {
            return header.dup;
        }

        public MessageType getType()
        {
            return header.type;
        }


        public override String ToString()
        {
            StringBuilder strBuff = new StringBuilder();
            strBuff.Append("Message [");
            strBuff.Append("type: " + getType() + "]");
            return strBuff.ToString();
        }

        protected ushort ReadUshortFromStream(MessageStream input)
        {
            return (ushort)((input.readByte() << 8) + input.readByte());
        }

        protected string ReadStringFromStream(MessageStream input)
        {
            ushort len = ReadUshortFromStream(input);
            byte[] data = input.readBytes(len);
            UTF8Encoding enc = new UTF8Encoding();
            return enc.GetString(data, 0, data.Length);
        }
    }
}
