using windows_client.Model;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Diagnostics;

namespace windows_client.DbUtils
{
    public class MqttDBUtils
    {
        private static object lockObj = new object();

        #region MqttPersistence
        /// <summary>
        /// Retrives all messages thet were unsent previously, and are required to send when connection re-establishes
        /// deletes pending messages from db after reading
        /// </summary>
        /// <returns></returns>
        public static List<HikePacket> getAllSentMessages()
        {
            List<HikePacket> res;
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
            {
                res = DbCompiledQueries.GetAllSentMessages(context).ToList<HikePacket>();
                //context.mqttMessages.DeleteAllOnSubmit(context.mqttMessages);
                //context.SubmitChanges();
            }
            return (res==null || res.Count() == 0)?null:res;
        }

        public static void addSentMessage(HikePacket packet)
        {
            lock (lockObj)
            {

                HikePacket mqttMessage = new HikePacket(packet.MessageId, packet.Message, packet.Timestamp);
                using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
                {
                    context.mqttMessages.InsertOnSubmit(mqttMessage);
                    context.SubmitChanges();
                }
            }
            //TODO update observable list
        }

        public static void removeSentMessage(long msgId)
        {
            lock (lockObj)
            {
                using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
                {
                    context.mqttMessages.DeleteAllOnSubmit<HikePacket>(DbCompiledQueries.GetMqttMsgForMsgId(context, msgId));
                    try
                    {
                        context.SubmitChanges(ConflictMode.ContinueOnConflict);
                    }

                    catch (ChangeConflictException e)
                    {
                        Debug.WriteLine(e.Message);
                        // Automerge database values for members that client
                        // has not modified.
                        foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                        {
                            occ.Resolve(RefreshMode.KeepChanges);
                        }
                    }
                    // Submit succeeds on second try.
                    context.SubmitChanges(ConflictMode.FailOnFirstConflict);
                }
            }
        }

        public static void deleteAllUnsentMessages()
        {
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(App.MqttDBConnectionstring))
            {
                context.mqttMessages.DeleteAllOnSubmit<HikePacket>(context.GetTable<HikePacket>());
                try
                {
                    context.SubmitChanges(ConflictMode.ContinueOnConflict);
                }

                catch (ChangeConflictException e)
                {
                    Debug.WriteLine(e.Message);
                    // Automerge database values for members that client
                    // has not modified.
                    foreach (ObjectChangeConflict occ in context.ChangeConflicts)
                    {
                        occ.Resolve(RefreshMode.KeepChanges);
                    }
                }
                // Submit succeeds on second try.
                context.SubmitChanges(ConflictMode.FailOnFirstConflict);
            }
        }

        #endregion
    }
}
