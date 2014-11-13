using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace finalmqtt.Client
{
    public class OnSocketWriteEventArgs : EventArgs
    {
        public OnSocketWriteEventArgs(long messageId)
        {
            MessageId = messageId;
        }

        public long MessageId { get; private set; }
    }
}
