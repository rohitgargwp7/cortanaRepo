using System;
using CommonLibrary.Model;
using System.Linq;
using System.Data.Linq;

namespace CommonLibrary.DbUtils
{
    public class DbCompiledQueries
    {
        public static Func<HikeChatsDb, string, IQueryable<GroupInfo>> GetGroupInfoForID
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<GroupInfo>> q =
                     CompiledQuery.Compile<HikeChatsDb, string, IQueryable<GroupInfo>>
                     ((HikeChatsDb hdc, string grpId) =>
                         from o in hdc.groupInfo
                         where o.GroupId == grpId
                         select o);
                return q;
            }
        }

        #region UsersTable Queries

        public static Func<HikeUsersDb, string, IQueryable<ContactInfo>> GetContactFromMsisdn
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<ContactInfo>> q =
                    CompiledQuery.Compile<HikeUsersDb, string, IQueryable<ContactInfo>>
                    ((HikeUsersDb hdc, string m) =>
                        from o in hdc.users
                        where o.Msisdn == m
                        select o);
                return q;
            }
        }

        public static Func<HikeUsersDb, IQueryable<Blocked>> GetBlockList
        {
            get
            {
                Func<HikeUsersDb, IQueryable<Blocked>> q =
                     CompiledQuery.Compile<HikeUsersDb, IQueryable<Blocked>>
                     ((HikeUsersDb hdc) =>
                         from o in hdc.blockedUsersTable
                         select o);
                return q;
            }
        }

        public static Func<HikeUsersDb, string, IQueryable<ContactInfo>> GetUsersWithGivenId
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeUsersDb, string, IQueryable<ContactInfo>>
                     ((HikeUsersDb hdc, string i) =>
                         from o in hdc.users
                         where o.Id == i
                         select o);
                return q;
            }
        }

        #endregion

        #region MessagesTable Queries

        public static Func<HikeChatsDb, string, IQueryable<ConvMessage>> GetMessagesForMsisdn
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<ConvMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, IQueryable<ConvMessage>>
                ((HikeChatsDb hdc, string myMsisdn) =>
                    from o in hdc.messages
                    where o.Msisdn == myMsisdn
                    orderby o.MessageId
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, string, long, string, IQueryable<ConvMessage>> GetMessageForMappedMsgIdMsisdn
        {
            get
            {
                Func<HikeChatsDb, string, long, string, IQueryable<ConvMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, long, string, IQueryable<ConvMessage>>
                ((HikeChatsDb hdc, string myMsisdn, long mappedMsgId, string msg) =>
                    from o in hdc.messages
                    where o.Msisdn == myMsisdn && o.MappedMessageId == mappedMsgId && o.Message == msg
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, long, IQueryable<ConvMessage>> GetMessagesForMsgId
        {
            get
            {
                Func<HikeChatsDb, long, IQueryable<ConvMessage>> q =
                     CompiledQuery.Compile<HikeChatsDb, long, IQueryable<ConvMessage>>
                     ((HikeChatsDb hdc, long id) =>
                         from o in hdc.messages
                         where o.MessageId == id
                         select o);
                return q;
            }
        }

        /// <summary>
        /// Get all messages which are sent but not delivered for msisdn for maximum msg id
        /// </summary>
        public static Func<HikeChatsDb, long, string, IQueryable<ConvMessage>> GetUndeliveredMessagesForMsisdn
        {
            get
            {
                Func<HikeChatsDb, long, string, IQueryable<ConvMessage>> q =
                     CompiledQuery.Compile<HikeChatsDb, long, string, IQueryable<ConvMessage>>
                     ((HikeChatsDb hdc, long id, string msisdn) =>
                         from o in hdc.messages
                         where o.MessageId <= id && o.Msisdn == msisdn && (o.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE || o.MessageStatus == ConvMessage.State.SENT_CONFIRMED || o.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED)
                         select o);
                return q;
            }
        }

        /// <summary>
        /// Get all messages which are sent or delivered but nor read for msisdn for maximum msg id
        /// </summary>
        public static Func<HikeChatsDb, long, string, IQueryable<ConvMessage>> GetDeliveredUnreadMessagesForMsisdn
        {
            get
            {
                Func<HikeChatsDb, long, string, IQueryable<ConvMessage>> q =
                     CompiledQuery.Compile<HikeChatsDb, long, string, IQueryable<ConvMessage>>
                     ((HikeChatsDb hdc, long id, string msisdn) =>
                         from o in hdc.messages
                         where o.MessageId <= id && o.Msisdn == msisdn && (o.MessageStatus == ConvMessage.State.SENT_SOCKET_WRITE || o.MessageStatus == ConvMessage.State.SENT_CONFIRMED || o.MessageStatus == ConvMessage.State.SENT_DELIVERED || o.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_CONFIRMED || o.MessageStatus == ConvMessage.State.FORCE_SMS_SENT_DELIVERED)
                         select o);
                return q;
            }
        }

        #endregion

        #region STATUS UPDATES

        public static Func<HikeChatsDb, long, IQueryable<StatusMessage>> GetStatusMsgForStatusId
        {
            get
            {
                Func<HikeChatsDb, long, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, long, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, long Id) =>
                    from o in hdc.statusMessage
                    where o.StatusId == Id
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, string, IQueryable<StatusMessage>> GetStatusMsgForServerId
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, string Id) =>
                    from o in hdc.statusMessage
                    where o.ServerId == Id
                    select o);
                return q;
            }
        }

        #endregion

        #region MqttTable Queries

        public static Func<HikeMqttPersistenceDb, IQueryable<HikePacket>> GetAllSentMessages
        {
            get
            {
                Func<HikeMqttPersistenceDb, IQueryable<HikePacket>> q =
                    CompiledQuery.Compile<HikeMqttPersistenceDb, IQueryable<HikePacket>>
                    ((HikeMqttPersistenceDb hdc) =>
                        from o in hdc.mqttMessages
                        orderby o.Timestamp
                        select o);
                return q;
            }
        }

        public static Func<HikeMqttPersistenceDb, long, IQueryable<HikePacket>> GetMqttMsgForTimestamp
        {
            get
            {
                Func<HikeMqttPersistenceDb, long, IQueryable<HikePacket>> q =
                   CompiledQuery.Compile<HikeMqttPersistenceDb, long, IQueryable<HikePacket>>
                   ((HikeMqttPersistenceDb hdc, long ts) =>
                       from o in hdc.mqttMessages
                       where o.Timestamp == ts
                       select o);
                return q;
            }
        }

        #endregion
    }
}
