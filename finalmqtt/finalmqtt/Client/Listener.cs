﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace finalmqtt.Client
{
    public interface Listener
    {
        void onConnected();
        void onDisconnected();
        void onPublish(String topic, byte[] body);
        //void onFailure();
        //void onSuccess();
    }
}
