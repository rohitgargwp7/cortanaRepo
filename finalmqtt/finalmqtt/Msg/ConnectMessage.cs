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
using System.Text;
using System.IO;
using finalmqtt.Client;
using mqtttest.Utils;
using System.Collections.Generic;


namespace finalmqtt.Msg
{
    public class ConnectMessage : Message
    {
        private static readonly String PROTOCOL_ID = "MQIsdp";
        private static readonly byte PROTOCOL_VERSION = 3;
        private static int CONNECT_HEADER_SIZE = 12;

        private String clientId;
        private short keepAlive;
        private String username;
        private String password;
        private bool cleanSession;
        private String willTopic;
        private String will;
        private QoS willQoS = QoS.AT_MOST_ONCE;
        private bool retainWill = false;

        public ConnectMessage(String clientId, bool cleanSession, short keepAlive, MqttConnection conn)
            : base(MessageType.CONNECT, conn)
        {
            if (clientId == null || clientId.Length > 23)
            {
                throw new ArgumentException(
                        "Client id cannot be null and must be at most 23 characters long: "
                                + clientId);
            }
            //System.out.println("Client Id : " + clientId);
            this.clientId = clientId;
            this.cleanSession = cleanSession;
            this.keepAlive = keepAlive;
        }

        public ConnectMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        protected override int messageLength()
        {
            int payloadSize = FormatUtil.toMQttString(clientId).Length;
            payloadSize += FormatUtil.toMQttString(willTopic).Length;
            payloadSize += FormatUtil.toMQttString(will).Length;
            payloadSize += FormatUtil.toMQttString(username).Length;
            payloadSize += FormatUtil.toMQttString(password).Length;
            return payloadSize + CONNECT_HEADER_SIZE;
        }

        protected override void writeMessage()
        {
//            WriteToStream(PROTOCOL_ID);
            messageData.AddRange(FormatUtil.toMQttString(PROTOCOL_ID));

            messageData.Add(PROTOCOL_VERSION);

            int flags = cleanSession ? 2 : 0;
            flags |= (will == null) ? 0 : 0x04;
            flags |= (Convert.ToInt32(willQoS) << 3);
            flags |= retainWill ? 0x20 : 0;
            flags |= (password == null) ? 0 : 0x40;
            flags |= (username == null) ? 0 : 0x80;
            messageData.Add((byte)flags);
//            WriteToStream((ushort)keepAlive);
            messageData.AddRange(FormatUtil.toMQttString(keepAlive));

            messageData.AddRange(FormatUtil.toMQttString(clientId));
//            WriteToStream(clientId);
            
            if (will != null)
            {
                messageData.AddRange(FormatUtil.toMQttString(willTopic));
                messageData.AddRange(FormatUtil.toMQttString(will));
                //WriteToStream(willTopic);
                //WriteToStream(will);
            }
            if (username != null)
            {
                messageData.AddRange(FormatUtil.toMQttString(username));
                //WriteToStream(username);
            }
            if (password != null)
            {
                messageData.AddRange(FormatUtil.toMQttString(password));
                //WriteToStream(password);
            }
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {

            ReadStringFromStream(input);
            input.readByte();
            byte flags = input.readByte(); //flags
            keepAlive = (short)ReadUshortFromStream(input);
            clientId = ReadStringFromStream(input);

            if (((flags & 0x04) != 0))
            {
                willTopic = ReadStringFromStream(input);
                will = ReadStringFromStream(input);
            }

            username = ((flags & 0x80) == 0) ? null : ReadStringFromStream(input);
            password = ((flags & 0x40) == 0) ? null : ReadStringFromStream(input);

            retainWill = ((flags & 0x20) > 0);
            willQoS = (QoS)(Convert.ToUInt16(flags & 0x18) >> 3);

        }

        public void setCredentials(String username, String password)
        {
            this.username = username;
            this.password = password;

        }

        public void setWill(String willTopic, String will)
        {
            this.willTopic = willTopic;
            this.will = will;
        }

        public void setWill(String willTopic, String will, QoS willQoS,
                            bool retainWill)
        {
            this.willTopic = willTopic;
            this.will = will;
            this.willQoS = willQoS;
            this.retainWill = retainWill;

        }

        public String getClientId()
        {
            return clientId;
        }

        public bool isCleanSession()
        {
            return cleanSession;
        }

        public int getKeepAlive()
        {
            return keepAlive;
        }

        public String getUsername()
        {
            return username;
        }

        public String getPassword()
        {
            return password;
        }

        public override void setDup(bool dup)
        {
            //        throw new UnsupportedOperationException("CONNECT messages don't use the DUP flag.");
        }

        public override void setRetained(bool retain)
        {
            //        throw new UnsupportedOperationException("CONNECT messages don't use the RETAIN flag.");
        }

        public override void setQos(QoS qos)
        {
            //        throw new UnsupportedOperationException("CONNECT messages don't use the QoS flags.");
        }


        public override String ToString()
        {
            StringBuilder strBuff = new StringBuilder();
            strBuff.Append("ConnectMessage [");
            strBuff.Append("clientId:" + clientId + "]");
            return strBuff.ToString();
        }
    }
}
