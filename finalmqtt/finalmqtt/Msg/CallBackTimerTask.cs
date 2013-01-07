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
using finalmqtt.Client;

namespace finalmqtt.Msg
{
    public class CallBackTimerTask
    {
        private short messageId;
        private Callback cb;
        private MqttConnection.onAckFailedDelegate onAckFailed;

        public CallBackTimerTask(MqttConnection.onAckFailedDelegate onAckFailed, short messageId, Callback cb)
        {
            this.onAckFailed = onAckFailed;
            this.messageId = messageId;
            this.cb = cb;
        }

        public void HandleTimerTask()
        {
            Debug.WriteLine("CALLBACK TIMER:: For message ID - " + messageId);
            Callback cb = onAckFailed(messageId);
            if (cb != null)
                cb.onFailure(new TimeoutException("Couldn't get Ack for retryable Message id=" + messageId));
        }
    }
}
