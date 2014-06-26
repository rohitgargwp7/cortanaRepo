using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace finalmqtt.Client
{
    class SocketEventArguemntsMessageId : SocketAsyncEventArgs
    {
        List<short> _listMessageId;
        short _messageId;

        public List<short> MessageIdList
        {
            get
            {
                return _listMessageId;
            }
            set
            {
                _listMessageId = value;
            }
        }

        public short MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                _messageId = value;
            }
        }

    }
}
