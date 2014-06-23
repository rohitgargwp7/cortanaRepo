using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace finalmqtt.Client
{
    class SocketEventArguemntsMessageId : SocketAsyncEventArgs
    {
        List<short> _messageId=new List<short>();


        public List<short> MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                _messageId=value;
            }
        }


    }
}
