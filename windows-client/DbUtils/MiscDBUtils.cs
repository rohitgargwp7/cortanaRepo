﻿using System;
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
        public static List<HikePacket> getAllSentMessages()
        {
            Func<HikeDataContext, IQueryable<HikePacket>> q =
            CompiledQuery.Compile<HikeDataContext, IQueryable<HikePacket>>
            ((HikeDataContext hdc) =>
                from o in hdc.mqttMessages
                select o);
            return q(App.HikeDataContext).Count<HikePacket>() == 0 ? null :
                q(App.HikeDataContext).ToList<HikePacket>();
        }

        public static void addSentMessage(HikePacket packet)
        {
            HikePacket mqttMessage = new HikePacket(packet.MessageId, packet.Message, packet.Timestamp);
            App.HikeDataContext.mqttMessages.InsertOnSubmit(mqttMessage);
            App.HikeDataContext.SubmitChanges();
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
            App.HikeDataContext.mqttMessages.DeleteAllOnSubmit<HikePacket>(q(App.HikeDataContext, msgId));
            App.HikeDataContext.SubmitChanges();
        }
        #endregion


    }
}
