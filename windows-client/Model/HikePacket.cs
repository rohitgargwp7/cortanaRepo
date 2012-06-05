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

namespace windows_client.Model
{
    public class HikePacket
    {
        private bool _retry;
        public bool Retry
        {
            get 
            {
                return _retry;
            }
            set 
            {
                _retry = value;
            }
        }

        private byte[] _message;
        public byte[] Message
        {
            get 
            {
                return _message;
            }
        }
        
        
        private long _messageId;
        public long MessageId
        {
            get 
            {
                return _messageId;
            }
        }

        private long _timeStamp;
        public long TimeStamp
        {
            get 
            {
                return _timeStamp;
            }
        }


        public HikePacket(byte[] message, long msgId)
        {
            this._message = message;
            this._messageId = msgId;
            this._retry = true;
        }

    }
}
