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
    public class PubCompMessage : RetryableMessage
    {
        public PubCompMessage(int messageId, MqttConnection conn)
            : base(MessageType.PUBCOMP, conn)
        {
            setMessageId(messageId);
        }

        public PubCompMessage(Header header, MqttConnection conn)
            : base(header, conn)
        {
        }
    }
}
