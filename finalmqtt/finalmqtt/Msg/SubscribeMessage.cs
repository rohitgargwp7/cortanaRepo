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
using System.Collections.Generic;
using System.IO;
using finalmqtt.Client;
using mqtttest.Utils;

namespace finalmqtt.Msg
{
    public class SubscribeMessage : RetryableMessage
    {
        private List<String> topics = new List<String>();
        private List<QoS> topicQoSs = new List<QoS>();

        public SubscribeMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        public SubscribeMessage(String topic, QoS topicQos, MqttConnection conn)
            : base(MessageType.SUBSCRIBE, conn)
        {
            setQos(QoS.AT_LEAST_ONCE);
            topics.Add(topic);
            topicQoSs.Add(topicQos);
        }
        public SubscribeMessage(List<String> topics, List<QoS> topicQoSs, MqttConnection conn)
            : base(MessageType.SUBSCRIBE, conn)
        {
            setQos(QoS.AT_LEAST_ONCE);
            this.topics = topics;
            this.topicQoSs = topicQoSs;
        }

        protected override int messageLength()
        {
            int length = 2; // message id length
            foreach (String topic in topics)
            {
                length += FormatUtil.toMQttString(topic).Length;
                length += 1; // topic QoS
            }
            return length;
        }

        protected override void writeMessage()
        {
            base.writeMessage();

            for (int i = 0; i < topics.Count; i++)
            {
               
                messageData.AddRange(FormatUtil.toMQttString(topics[i]));
//                WriteToStream(topics[i]);
//                output.WriteByte((byte)topicQoSs[i]);
                messageData.Add((byte)topicQoSs[i]);
            }
        }

        protected override void readMessage(MessageStream input, int msgLength)
        {
            base.readMessage(input, msgLength);
            while (input.Size() > 0)
            {
                String topic = ReadStringFromStream(input);
                //System.out.println("Topic : " + topic);
                topics.Add(topic);
                topicQoSs.Add((QoS)input.readByte());
            }
        }

        public override void setQos(QoS qos)
        {
            if (qos != QoS.AT_LEAST_ONCE)
            {
                //throw new IllegalArgumentException(
                //        "SUBSCRIBE is always using QoS-level AT LEAST ONCE. Requested level: "
                //                + qos);
            }
            base.setQos(qos);
        }

        public override void setDup(bool dup)
        {
            if (dup == true)
            {
                //            throw new IllegalArgumentException("SUBSCRIBE can't set the DUP flag.");
            }
            base.setDup(dup);
        }

        public override void setRetained(bool retain)
        {
            //        throw new UnsupportedOperationException("SUBSCRIBE messages don't use the RETAIN flag");
        }

        //    public String getTopic() {
        //        return topics.get(0);
        //    }

        public List<String> getTopics()
        {
            return topics;
        }

        public List<QoS> getTopicQoSs()
        {
            return topicQoSs;
        }


    }
}
