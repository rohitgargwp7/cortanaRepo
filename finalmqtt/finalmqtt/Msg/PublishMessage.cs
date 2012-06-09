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
    public class PublishMessage : RetryableMessage
    {

        private String topic;
        private byte[] data;

        public PublishMessage(String topic, String msg, MqttConnection conn)
            : this(topic, Encoding.UTF8.GetBytes(msg), QoS.AT_MOST_ONCE, conn)
        {
        }

        public PublishMessage(String topic, String msg, QoS qos, MqttConnection conn)
            : this(topic, Encoding.UTF8.GetBytes(msg), qos, conn)
        {
        }

        public PublishMessage(String topic, byte[] data, QoS qos, MqttConnection conn)
            : base(MessageType.PUBLISH, conn)
        {
            this.topic = topic;
            this.data = data;
            setQos(qos);
        }

        public PublishMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        protected override int messageLength()
        {
            int length = FormatUtil.toMQttString(topic).Length;
            length += (getQos() == QoS.AT_MOST_ONCE) ? 0 : 2;
            length += data.Length;
            return length;
        }

        protected override void writeMessage()
        {
            messageData.AddRange(FormatUtil.toMQttString(topic));
            if (getQos() != QoS.AT_MOST_ONCE)
            {
                base.writeMessage();
            }
            messageData.AddRange(data);
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {
            int pos = 0;
            topic = ReadStringFromStream(input);

            pos += FormatUtil.toMQttString(topic).Length;
            if (getQos() != QoS.AT_MOST_ONCE)
            {
                base.readMessage(input, msgLength);
                pos += 2;
            }
            int payloadSize = (msgLength - pos);
            data = input.readBytes(payloadSize);
        }

        public String getTopic()
        {
            return topic;
        }

        public byte[] getData()
        {
            return data;
        }

        public String getDataAsString()
        {
            return Encoding.UTF8.GetString(data, 0, data.Length);
        }

        public override String ToString()
        {
            StringBuilder strBuff = new StringBuilder();
            strBuff.Append("PublishMessage [");
            strBuff.Append("topic: " + getTopic() + ",");
            strBuff.Append("messageId: " + getMessageId() + ",");
            strBuff.Append("data: " + getDataAsString() + "]");
            return strBuff.ToString();
        }
    }
}


