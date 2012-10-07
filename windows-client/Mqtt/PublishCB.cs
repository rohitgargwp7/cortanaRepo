using System;
using mqtttest.Client;
using windows_client.Model;
using windows_client.DbUtils;
using Newtonsoft.Json.Linq;

namespace windows_client.Mqtt
{
    public class PublishCB : Callback
    {
        private HikePacket packet;
        bool called;
        private HikeMqttManager hikeMqttManager;

        public PublishCB(HikePacket packet, HikeMqttManager hikeMqttManager)
        {
            this.packet = packet;
            this.called = false;
            this.hikeMqttManager = hikeMqttManager;
        }

        public void onSuccess()
        {
            if (packet != null)
            {
                MqttDBUtils.removeSentMessage(packet.MessageId);
                if (packet.MessageId > 0) // represents ack for message that is recieved by server
                {
                    JObject obj = new JObject();
                    obj[HikeConstants.TYPE] = NetworkManager.SERVER_REPORT;
                    obj[HikeConstants.DATA] = Convert.ToString(packet.MessageId);
                    NetworkManager.Instance.onMessage(obj.ToString());
                }
            }
        }

        public void onFailure(Exception value)
        {
            hikeMqttManager.ping();
            MqttDBUtils.addSentMessage(packet);

            if (packet != null)
            {
                if (packet.MessageId > 0) // represents ack for message that is recieved by server
                {
                    JObject obj = new JObject();
                    obj[HikeConstants.DATA] = Convert.ToString(packet.MessageId);
                    App.HikePubSubInstance.publish(HikePubSub.WS_RECEIVED, obj.ToString());
                }
            }
        }
    }
}
