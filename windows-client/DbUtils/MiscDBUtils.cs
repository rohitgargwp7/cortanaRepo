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
    public class MiscDBUtils
    {
        #region MqttPersistence
        public static List<HikeMqttPersistence> getAllSentMessages()
        {
            Func<HikeDataContext, IQueryable<HikeMqttPersistence>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<HikeMqttPersistence>>
            ((HikeDataContext hdc) =>
                from o in hdc.mqttMessages
                select o);
            return q(App.HikeDataContext).Count<HikeMqttPersistence>() == 0 ? null :
                q(App.HikeDataContext).ToList<HikeMqttPersistence>();
        }

        public static void addSentMessage(HikePacket packet)
        {
            HikeMqttPersistence mqttMessage = new HikeMqttPersistence(packet.MessageId, packet.Message, packet.TimeStamp);
            App.HikeDataContext.mqttMessages.InsertOnSubmit(mqttMessage);
            App.HikeDataContext.SubmitChanges();
            //TODO update observable list
        }

        public void removeSentMessage(long msgId)
        {
            Func<HikeDataContext, long, IQueryable<HikeMqttPersistence>> q =
            CompiledQuery.Compile<HikeDataContext, long, IQueryable<HikeMqttPersistence>>
            ((HikeDataContext hdc, long id) =>
                from o in hdc.mqttMessages
                where o.MessageId == id
                select o);
            App.HikeDataContext.mqttMessages.DeleteAllOnSubmit<HikeMqttPersistence>(q(App.HikeDataContext, msgId));
            App.HikeDataContext.SubmitChanges();
        }
        #endregion


    }
}
