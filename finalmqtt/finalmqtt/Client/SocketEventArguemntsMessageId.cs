using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace finalmqtt.Client
{
    /// <summary>
    /// Used to pass data between socket events , Inherits SocketAsyncEventArgs
    /// </summary>
    class SocketEventArguemntsMessageId : SocketAsyncEventArgs
    {
        object _messageData;


        public object MessageData
        {
            get
            {
                return _messageData;
            }
            set
            {
                _messageData = value;
            }
        }

    }
}
