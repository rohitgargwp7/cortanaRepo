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
using System.Collections.Generic;
using mqtttest.Client;
using System.Diagnostics;

namespace finalmqtt.Msg
{
    public class CallBackTimerTask
    {
        private Dictionary<short, Callback> map = new Dictionary<short, Callback>();
        private short messageId;
        private Callback cb;

        public CallBackTimerTask(Dictionary<short, Callback> map, short messageId, Callback cb)
        {
            this.map = map;
            this.messageId = messageId;
            this.cb = cb;
        }

        public void HandleTimerTask()
        {
            Debug.WriteLine("CALLBACK TIMER:: For message ID - " + messageId);

            if (map.ContainsKey(messageId))
            {
                map.Remove(messageId);
                if(cb!=null)
                    cb.onFailure(new TimeoutException("Couldn't get Ack for retryable Message id=" + messageId));
            }
        }
    }
}
