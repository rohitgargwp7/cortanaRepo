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
        private HikeMqttManager hikeMqttManager;
        private int qos;
        private bool removeFromDBOnReceivingAck;

        public PublishCB(HikePacket packet, HikeMqttManager hikeMqttManager, int qos, bool removeFromDBOnReceivingAck)
        {
            this.packet = packet;
            this.hikeMqttManager = hikeMqttManager;
            this.qos = qos;
            this.removeFromDBOnReceivingAck = removeFromDBOnReceivingAck;
        }

        public void onSuccess()
        {
            if (packet != null)
            {
                if (qos > 0 && removeFromDBOnReceivingAck)
                {
                    MqttDBUtils.removeSentMessage(packet.Timestamp);
                }
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
            if (packet != null)
            {
                if (qos > 0)
                {
                    MqttDBUtils.addSentMessage(packet);
                }
            }
        }
    }
}
