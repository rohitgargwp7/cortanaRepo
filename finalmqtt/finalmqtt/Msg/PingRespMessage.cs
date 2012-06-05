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
using finalmqtt.Client;

namespace finalmqtt.Msg
{
    public class PingRespMessage : Message
    {
        public PingRespMessage(MqttConnection conn)
            : base(MessageType.PINGRESP, conn)
        {
        }

        public PingRespMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }

        public override void setDup(bool dup)
        {
            //            throw new UnsupportedOperationException("PINGREQ message does not support the DUP flag");
        }

        public override void setQos(QoS qos)
        {
            //        throw new UnsupportedOperationException("PINGREQ message does not support the QoS flag");
        }

        public override void setRetained(bool retain)
        {
            //          throw new UnsupportedOperationException("PINGREQ message does not support the RETAIN flag");
        }


    }
}
