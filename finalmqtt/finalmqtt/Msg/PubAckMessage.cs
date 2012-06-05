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
using finalmqtt.Client;

namespace finalmqtt.Msg
{
    public class PubAckMessage:RetryableMessage
    {
        public PubAckMessage(int messageId, MqttConnection conn)
            : base(MessageType.PUBACK, conn)
    {
        setMessageId(messageId);
    }

        public PubAckMessage(Header header, MqttConnection conn)
            : base(header, conn)
    {
    }

    public override String ToString()
    {
        StringBuilder strBuff = new StringBuilder();
        strBuff.Append("PubAckMessage [");
        strBuff.Append("messageId: " + getMessageId()+"]");
        return strBuff.ToString();
    }

    }
}
