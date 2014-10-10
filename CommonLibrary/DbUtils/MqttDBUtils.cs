﻿using CommonLibrary.Model;
using System.Collections.Generic;
using System.Linq;
using System.Data.Linq;
using System.Diagnostics;
using System;
using Microsoft.Phone.Data.Linq;
using CommonLibrary.utils;
using CommonLibrary.Constants;

namespace CommonLibrary.DbUtils
{
    public class MqttDBUtils
    {
        public const int MqttDb_Latest_Version = 1;

        private static object lockObj = new object();

        /// <summary>
        /// Retrives all messages thet were unsent previously, and are required to send when connection re-establishes
        /// deletes pending messages from db after reading
        /// </summary>
        /// <returns></returns>
        public static List<HikePacket> getAllSentMessages()
        {
            try
            {

                List<HikePacket> res;
                using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(HikeConstants.DBStrings.MqttDBConnectionstring))
                {
                    res = DbCompiledQueries.GetAllSentMessages(context).ToList<HikePacket>();
                    //context.mqttMessages.DeleteAllOnSubmit(context.mqttMessages);
                    //context.SubmitChanges();
                }
                return (res == null || res.Count() == 0) ? null : res;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("MqttDbUtil :: getAllSentMessages : getAllSentMessages, Exception : " + ex.StackTrace);
                return null;
            }
        }

        public static void addSentMessage(HikePacket packet)
        {
            if (packet.Message != null && packet.Message.Length < 8000)
            {
                lock (lockObj)
                {
                    try
                    {
                        HikePacket mqttMessage = new HikePacket(packet.MessageId, packet.Message, packet.Timestamp);
                        using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(HikeConstants.DBStrings.MqttDBConnectionstring))
                        {
                            context.mqttMessages.InsertOnSubmit(mqttMessage);
                            context.SubmitChanges();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("MqttDbUtil :: addSentMessage : addSentMessage, Exception : " + ex.StackTrace);
                    }
                }
            }
            //TODO update observable list
        }

        public static void removeSentMessage(long timestamp)
        {
            lock (lockObj)
            {
                using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(HikeConstants.DBStrings.MqttDBConnectionstring))
                {
                    List<HikePacket> entriesToDelete = DbCompiledQueries.GetMqttMsgForTimestamp(context, timestamp).ToList();
                    if (entriesToDelete == null || entriesToDelete.Count == 0)
                        return;
                    context.mqttMessages.DeleteAllOnSubmit<HikePacket>(entriesToDelete);
                    try
                    {
                        context.SubmitChanges(ConflictMode.ContinueOnConflict);
                        Debug.WriteLine("Removed unsent packet with timestamp :: " + timestamp);
                    }
                    catch (ChangeConflictException e)
                    {
                        Debug.WriteLine("MqttDbUtil :: removeSentMessage : removeSentMessage, Exception : " + e.StackTrace);
                        Debug.WriteLine("Failed to remove unsent packet with timestamp :: " + timestamp);
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

        public static void MqttDbUpdateToLatestVersion()
        {
            using (HikeMqttPersistenceDb context = new HikeMqttPersistenceDb(HikeConstants.DBStrings.MqttDBConnectionstring))
            {
                DatabaseSchemaUpdater schemaUpdater = context.CreateDatabaseSchemaUpdater();
                // get current database schema version
                // if not changed the version is 0 by default
                int version = schemaUpdater.DatabaseSchemaVersion;

                // if current version of database schema is old
                if (version == 0)
                {
                    // add Address column to the table corresponding to the Person class
                    // IMPORTANT: update database schema version before calling Execute
                    schemaUpdater.DatabaseSchemaVersion = MqttDb_Latest_Version;
                    try
                    {
                        // execute changes to database schema
                        schemaUpdater.Execute();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
    }
}
