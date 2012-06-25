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
using windows_client.Model;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;

namespace windows_client.DbUtils
{
    public class MqttDBUtils
    {
        #region MqttPersistence
        /// <summary>
        /// Retrives all messages thet were unsent previously, and are required to send when connection re-establishes
        /// deletes pending messages from db after reading
        /// </summary>
        /// <returns></returns>
        public static List<HikePacket> getAllSentMessages()
        {
            Func<HikeDataContext, IQueryable<HikePacket>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<HikePacket>>
            ((HikeDataContext hdc) =>
                from o in hdc.mqttMessages
                select o);
            List<HikePacket> unsentMessages = q(App.HikeDataContextInstance).Count<HikePacket>() == 0 ? null :
                q(App.HikeDataContextInstance).ToList<HikePacket>();
            
            App.HikeDataContextInstance.mqttMessages.DeleteAllOnSubmit(App.HikeDataContextInstance.mqttMessages);
            App.HikeDataContextInstance.SubmitChanges();

            return unsentMessages;
        }

        public static void addSentMessage(HikePacket packet)
        {
            HikePacket mqttMessage = new HikePacket(packet.MessageId, packet.Message, packet.Timestamp);
            App.HikeDataContextInstance.mqttMessages.InsertOnSubmit(mqttMessage);
            App.HikeDataContextInstance.SubmitChanges();
            //TODO update observable list
        }

        public void removeSentMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<HikePacket>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<HikePacket>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.mqttMessages
                where o.MessageId == id
                select o);
            App.HikeDataContextInstance.mqttMessages.DeleteAllOnSubmit<HikePacket>(q(App.HikeDataContextInstance, msgId));
            App.HikeDataContextInstance.SubmitChanges();
        }
        #endregion


    }
}
