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
using mqtttest.Client;
using windows_client.Model;
using windows_client.DbUtils;

namespace windows_client.Mqtt
{
    public class PublishCB : Callback
    {
        private HikePacket packet;
        bool called;
        public PublishCB(HikePacket packet)
        {
            this.packet = packet;
            this.called = false;
        }

        public void onSuccess()
        {
            //set message status
        }


        public void onFailure(Exception value)
        {
            HikeDbUtils.addSentMessage(packet);
        }

    }
}
