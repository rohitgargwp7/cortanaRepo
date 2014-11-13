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
using System.Collections.Generic;

namespace finalmqtt.Msg
{
    public class ConnAckMessage : Message
    {
        public enum ConnectionStatus
        {
            ACCEPTED = 0,
            UNACCEPTABLE_PROTOCOL_VERSION,
            IDENTIFIER_REJECTED,
            SERVER_UNAVAILABLE,
            BAD_USERNAME_OR_PASSWORD,
            NOT_AUTHORIZED
        }

        private ConnectionStatus status;

        public ConnAckMessage(ConnectionStatus status, MqttConnection conn) :
            base(MessageType.CONNACK, conn)
        {
            this.status = status;
        }

        public ConnAckMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {
            if (msgLength != 2)
            {
                //            throw new IllegalStateException("Message Length must be 2 for CONNACK. Current value: " + msgLength);
            }
            // Ignore first byte
            input.readByte();
            int result = input.readByte();
            switch (result)
            {
                case 0:
                    status = ConnectionStatus.ACCEPTED;
                    break;
                case 1:
                    status = ConnectionStatus.UNACCEPTABLE_PROTOCOL_VERSION;
                    break;
                case 2:
                    status = ConnectionStatus.IDENTIFIER_REJECTED;
                    break;
                case 3:
                    status = ConnectionStatus.SERVER_UNAVAILABLE;
                    break;
                case 4:
                    status = ConnectionStatus.BAD_USERNAME_OR_PASSWORD;
                    break;
                case 5:
                    status = ConnectionStatus.NOT_AUTHORIZED;
                    break;
                default:
                    throw new NotSupportedException("Unsupported CONNACK code: " + result);
            }
        }

        protected override void writeMessage()
        {
            messageData.Add(0);
            //        output.WriteByte(0);
            switch (status)
            {
                case ConnectionStatus.ACCEPTED:
                    messageData.Add(0);
                    //                output.WriteByte(0);
                    break;
                case ConnectionStatus.UNACCEPTABLE_PROTOCOL_VERSION:
                    messageData.Add(1);
                    //                output.WriteByte(1);
                    break;
                case ConnectionStatus.IDENTIFIER_REJECTED:
                    messageData.Add(2);
                    //output.WriteByte(2);
                    break;
                case ConnectionStatus.SERVER_UNAVAILABLE:
                    messageData.Add(3);
                    //                output.WriteByte(3);
                    break;
                case ConnectionStatus.BAD_USERNAME_OR_PASSWORD:
                    messageData.Add(4);
                    //                output.WriteByte(4);
                    break;
                case ConnectionStatus.NOT_AUTHORIZED:
                    messageData.Add(5);
                    //output.WriteByte(5);
                    break;
                default:
                    messageData.Add(10);
                    //output.WriteByte(10);
                    break;
            }

        }

        public ConnectionStatus getStatus()
        {
            return status;
        }

        protected override int messageLength()
        {
            return 2;
        }

        public override void setDup(bool dup)
        {
            throw new NotSupportedException("CONNACK messages don't use the DUP flag.");
        }

        public override void setRetained(bool retain)
        {
            throw new NotSupportedException("CONNACK messages don't use the RETAIN flag.");
        }

        public override void setQos(QoS qos)
        {
            throw new NotSupportedException("CONNACK messages don't use the QoS flag.");
        }


        public override String ToString()
        {
            StringBuilder strBuff = new StringBuilder();
            strBuff.Append("ConnAckMessage [");
            strBuff.Append("status:" + getStatus() + "]");
            return strBuff.ToString();
        }

    }
}
