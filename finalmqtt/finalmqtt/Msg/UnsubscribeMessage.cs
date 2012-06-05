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
    public class UnsubscribeMessage : RetryableMessage
    {
            private List<String> topics = new List<String>();

            public UnsubscribeMessage(String topic, MqttConnection conn)
                : base(MessageType.UNSUBSCRIBE, conn)
    {
        setQos(QoS.AT_LEAST_ONCE);
        topics.Add(topic);
    }

            public UnsubscribeMessage(List<String> topics, MqttConnection conn)
                : base(MessageType.UNSUBSCRIBE, conn)
    {
        setQos(QoS.AT_LEAST_ONCE);
        this.topics=topics;
    }

    protected override int messageLength()
    {
        int length = 2; // message id length
        foreach (String topic in topics)
        {
            length += FormatUtil.toMQttString(topic).Length;
        }
        return length;
    }

    protected override void writeMessage()
    {
        base.writeMessage();
        foreach (String topic in topics)
        {
            messageData.AddRange(FormatUtil.toMQttString(topic));

            //WriteToStream(topic);
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
  //          throw new IllegalArgumentException("SUBSCRIBE can't set the DUP flag.");
        }
        base.setDup(dup);
    }

    public override void setRetained(bool retain)
    {
//        throw new UnsupportedOperationException("SUBSCRIBE messages don't use the RETAIN flag");
    }

    public List<String> getTopics()
    {
        return topics;
    }


    }
}
