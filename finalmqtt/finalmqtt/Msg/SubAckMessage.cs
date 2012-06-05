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
using System.Collections.Generic;
using System.IO;
using finalmqtt.Client;

namespace finalmqtt.Msg
{
    public class SubAckMessage : RetryableMessage
    {
        private List<QoS> grantedQoSs;

        public SubAckMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        public SubAckMessage(short messageId, List<QoS> qoSs, MqttConnection conn)
            : base(MessageType.SUBACK, conn)
        {
            this.messageId = messageId;
            this.grantedQoSs = qoSs;
        }

        public SubAckMessage(MqttConnection conn)
            : base(MessageType.SUBACK, conn)
        {
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {
            base.readMessage(input, msgLength);
            int pos = 2;
            while (pos < msgLength)
            {
                QoS qos = (QoS)(input.readByte());
                addQoS(qos);
                pos++;
            }
        }

        protected override void writeMessage()
        {
            base.writeMessage();
            if (grantedQoSs != null)
            {
                byte[] bts = new byte[grantedQoSs.Count];
                for (int i = 0; i < grantedQoSs.Count; i++)
                {
                    bts[i] = (byte)grantedQoSs[i];
                }
//                output.Write(bts, 0, bts.Length);
                messageData.AddRange(bts);
            }
        }

        private void addQoS(QoS qos)
        {
            if (grantedQoSs == null)
            {
                grantedQoSs = new List<QoS>();
            }
            grantedQoSs.Add(qos);
        }

        public List<QoS> getGrantedQoSs()
        {
            return grantedQoSs;
        }

        protected override int messageLength()
        {
            int length = 2;
            if (grantedQoSs != null)
            {
                length += grantedQoSs.Count;
            }
            return length;
        }


        public override String ToString()
        {
            StringBuilder strBuff = new StringBuilder();
            strBuff.Append("SubAckMessage [");
            strBuff.Append("messageId: " + getMessageId());
            strBuff.Append("qos: " + getGrantedQoSs());
            strBuff.Append("]");
            return strBuff.ToString();
        }

    }
}
