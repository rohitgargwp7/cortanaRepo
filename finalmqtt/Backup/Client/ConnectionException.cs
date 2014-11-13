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

namespace mqtttest.Client
{
    public class ConnectionException : Exception
    {
        private ConnAckMessage.ConnectionStatus code;

        public ConnectionException(String message, ConnAckMessage.ConnectionStatus code)
            : base(message)
        {
            this.code = code;
        }

        public ConnAckMessage.ConnectionStatus getCode()
        {
            return code;
        }


    }
}
