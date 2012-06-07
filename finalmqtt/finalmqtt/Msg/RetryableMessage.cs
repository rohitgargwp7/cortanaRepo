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
using System.IO;
using finalmqtt.Client;
using mqtttest.Utils;
using System.Collections.Generic;

namespace finalmqtt.Msg
{
    public class RetryableMessage : Message
    {
        protected short messageId;

        public RetryableMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        public RetryableMessage(MessageType type, MqttConnection conn)
            : base(type, conn)
        {
        }

        protected override int messageLength()
        {
            return 2;
        }

        protected override void writeMessage()
        {
            int id = getMessageId();
            messageData.AddRange(FormatUtil.toMQttString(id));
            //        WriteToStream((ushort)id);
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {
            byte msb = input.readByte();
            byte lsb = input.readByte();
            int id = msb >> 8;
            id += lsb;
            setMessageId(id);

            //            setMessageId((short)(input.readByte() >> 8 + input.readByte()));
        }

        public void setMessageId(int messageId)
        {
            this.messageId = (short)messageId;
        }

        public short getMessageId()
        {
            if (messageId == -1)
            {
                messageId = NextId;
            }
            return messageId;
        }

    }
}
