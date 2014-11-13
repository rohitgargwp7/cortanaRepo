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

namespace finalmqtt.Msg
{
    public class Header
    {
        public MessageType type;
        public bool retain;
        public QoS qos = QoS.AT_MOST_ONCE;
        public bool dup;

        public Header(MessageType type, bool retain, QoS qos, bool dup)
        {
            this.type = type;
            this.retain = retain;
            this.qos = qos;
            this.dup = dup;
        }

        public Header(byte flags)
        {
            retain = (flags & 1) > 0;
            qos = (QoS)((Convert.ToInt32(flags) & 0x6) >> 1);
            dup = (flags & 8) > 0;
            type = (MessageType)((Convert.ToInt32(flags) >> 4) & 0xF);
        }

        public MessageType getType()
        {
            return type;
        }

        public byte encode()
        {
            byte b = 0;
            b = (byte)((int)type << 4);
            b |= (byte)(retain ? 1 : 0);
            b |= (byte)((int)qos << 1);
            b |= (byte)(dup ? 8 : 0);
            return b;
        }

        public override String ToString()
        {
            return "Header [type=" + type + ", retain=" + retain + ", qos="
                    + qos + ", dup=" + dup + "]";
        }
    }

}
