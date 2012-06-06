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
using finalmqtt.Msg;

namespace windows_client.Mqtt
{
    public class Topic
    {
        private readonly String _name;
        private readonly QoS _qos;

        public String Name
        {
            get 
            {
                return _name;
            }
        }

        public QoS qos
        {
            get
            {
                return _qos;
            }
        }

        public Topic(String topicName, QoS ackMode)
        {
            this._name = topicName.ToLower();
            this._qos = ackMode;
        }


        public bool isAutoAck()
        {
            return (_qos == QoS.AT_MOST_ONCE);
        }

        public override String ToString()
        {
            return base.ToString() + ":" + Name + "-" + qos;
        }


        public override bool Equals(Object o)
        {
            if (this == o) return true;
            if (!(o is Topic)) return false;

            Topic topic = (Topic)o;

            if (qos != topic.qos) return false;
            if (_name != null ? (String.Compare(_name, topic._name, StringComparison.CurrentCultureIgnoreCase) != 0) : topic._name != null) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = _name != null ? _name.GetHashCode() : 0;
            result = 31 * result + (int)qos;
            return result;
        }

    }
}
