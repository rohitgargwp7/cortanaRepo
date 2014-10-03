﻿using System;
using windows_client.Model;
using System.Linq;
using System.Data.Linq;
using windows_client.utils;

namespace windows_client.DbUtils
{
    public class DbCompiledQueries
    {
        public static HikeChatsDb chatsDbContext = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring);
        public static HikeUsersDb usersDbContext = new HikeUsersDb(HikeConstants.DBStrings.UsersDBConnectionstring);

        #region GroupTable Queries

        public static Func<HikeChatsDb, IQueryable<GroupInfo>> GetAllGroups
        {
            get
            {
                Func<HikeChatsDb, IQueryable<GroupInfo>> q =
                     CompiledQuery.Compile<HikeChatsDb, IQueryable<GroupInfo>>
                     ((HikeChatsDb hdc) =>
                         from o in hdc.groupInfo
                         select o);
                return q;
            }
        }

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

        public static Func<HikeChatsDb, IQueryable<GroupInfo>> GetAllGroupInfo
        {
            get
            {
                Func<HikeChatsDb, IQueryable<GroupInfo>> q =
                     CompiledQuery.Compile<HikeChatsDb, IQueryable<GroupInfo>>
                     ((HikeChatsDb hdc) =>
                         from o in hdc.groupInfo
                         where o.GroupAlive == true
                         select o);
                return q;
            }
        }

        #endregion

        #region UsersTable Queries

        public static Func<HikeUsersDb, IQueryable<ContactInfo>> GetAllContacts
        {
            get
            {
                Func<HikeUsersDb, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeUsersDb, IQueryable<ContactInfo>>
                     ((HikeUsersDb hdc) =>
                         from o in hdc.users
                         select o);
                return q;
            }
        }

        public static Func<HikeUsersDb, IQueryable<ContactInfo>> GetAllHikeContactsOrdered
        {
            get
            {
                Func<HikeUsersDb, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeUsersDb, IQueryable<ContactInfo>>
                     ((HikeUsersDb hdc) =>
                         from o in hdc.users
                         where o.OnHike == true
                         orderby o.Name descending
                         select o);
                return q;
            }
        }
        public static Func<HikeUsersDb, IQueryable<ContactInfo>> GetAllHikeContacts
        {
            get
            {
                Func<HikeUsersDb, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeUsersDb, IQueryable<ContactInfo>>
                     ((HikeUsersDb hdc) =>
                         from o in hdc.users
                         where o.OnHike == true
                         select o);
                return q;
            }
        }
        public static Func<HikeUsersDb, string, IQueryable<ContactInfo>> GetContactFromName
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<ContactInfo>> q =
                   CompiledQuery.Compile((HikeUsersDb hdc, string chars) =>
                       from o in hdc.users
                       where o.Name.Contains(chars) || o.PhoneNo.Contains(chars)
                       select o);
                return q;
            }
        }

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

        public static Func<HikeUsersDb, string, IQueryable<ContactInfo>> GetHikeContactFromMsisdn
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<ContactInfo>> q =
                    CompiledQuery.Compile<HikeUsersDb, string, IQueryable<ContactInfo>>
                    ((HikeUsersDb hdc, string m) =>
                        from o in hdc.users
                        where o.Msisdn == m && o.OnHike == true
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

        public static Func<HikeUsersDb, string, IQueryable<ContactInfo>> UpdateOnhikeStatus
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeUsersDb, string, IQueryable<ContactInfo>>
                     ((HikeUsersDb hdc, string ms) =>
                         from o in hdc.users
                         where o.Msisdn == ms
                         select o);
                return q;
            }
        }

        public static Func<HikeUsersDb, string, IQueryable<Blocked>> GetBlockedUserForMsisdn
        {
            get
            {
                Func<HikeUsersDb, string, IQueryable<Blocked>> q =
                     CompiledQuery.Compile<HikeUsersDb, string, IQueryable<Blocked>>
                     ((HikeUsersDb hdc, string ms) =>
                         from o in hdc.blockedUsersTable
                         where o.Msisdn == ms
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

        public static Func<HikeUsersDb, IQueryable<ContactInfo>> GetContactsForOnhikeStatus
        {
            get
            {
                Func<HikeUsersDb, IQueryable<ContactInfo>> q =
                    CompiledQuery.Compile<HikeUsersDb, IQueryable<ContactInfo>>
                    ((HikeUsersDb hdc) =>
                        from o in hdc.users
                        where o.OnHike == false
                        orderby o.Name
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

        /// <summary>
        /// Db Call for retrieving pin history of any Group parts by parts
        /// </summary>
        public static Func<HikeChatsDb, string, long, int, IQueryable<ConvMessage>> GetPinMessagesForMsisdnForPaging
        {
            get
            {
                Func<HikeChatsDb, string, long, int, IQueryable<ConvMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, long, int, IQueryable<ConvMessage>>
                ((HikeChatsDb hdc, string myMsisdn,long lastmessageId,int count) =>
                    (from o in hdc.messages
                    where o.Msisdn == myMsisdn && o.MetaDataString.Contains("\"pin\":1") && o.MessageId < lastmessageId
                    orderby o.MessageId descending
                    select o).Take(count));
                return q;
            }
        }

        public static Func<HikeChatsDb, string, IQueryable<ConvMessage>> GetLastMessageForMsisdn
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

        public static Func<HikeChatsDb, string, long, int, IQueryable<ConvMessage>> GetMessagesForMsisdnForPaging
        {
            get
            {
                Func<HikeChatsDb, string, long, int, IQueryable<ConvMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, long, int, IQueryable<ConvMessage>>
                ((HikeChatsDb hdc, string myMsisdn, long lastMessageId, int count) =>
                    (from o in hdc.messages
                     where o.Msisdn == myMsisdn && o.MessageId <= lastMessageId
                     orderby o.MessageId descending
                     select o).Take(count));
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

        public static Func<HikeChatsDb, IQueryable<ConvMessage>> GetAllMessages
        {
            get
            {
                Func<HikeChatsDb, IQueryable<ConvMessage>> q =
                    CompiledQuery.Compile<HikeChatsDb, IQueryable<ConvMessage>>
                    ((HikeChatsDb hdc) =>
                        from o in hdc.messages
                        select o);
                return q;
            }
        }

        #endregion

        #region STATUS UPDATES

        public static Func<HikeChatsDb, string, IQueryable<StatusMessage>> GetStatusMsgsForMsisdn
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, string myMsisdn) =>
                    from o in hdc.statusMessage
                    where o.Msisdn == myMsisdn
                    orderby o.StatusId descending
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, string, long, int, IQueryable<StatusMessage>> GetPaginatedStatusMsgsForMsisdn
        {
            get
            {
                Func<HikeChatsDb, string, long, int, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, long, int, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, string myMsisdn, long lastStatusId, int count) =>
                    (from o in hdc.statusMessage
                     where o.Msisdn == myMsisdn && o.StatusId <= lastStatusId
                     orderby o.StatusId descending
                     select o).Take(count));
                return q;
            }
        }
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

        public static Func<HikeChatsDb, string, IQueryable<StatusMessage>> GetFirstTextStatusUpdate
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, string msisdn) =>
                    from o in hdc.statusMessage
                    where o.Status_Type == StatusMessage.StatusType.TEXT_UPDATE && o.Msisdn == msisdn
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, string, int, IQueryable<StatusMessage>> GetUnReadStatusMsgsForMsisdn
        {
            get
            {
                Func<HikeChatsDb, string, int, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, string, int, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, string myMsisdn, int count) =>
                    from o in hdc.statusMessage.Take(count)
                    orderby o.StatusId descending
                    where o.Msisdn == myMsisdn
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, IQueryable<StatusMessage>> GetAllStatusMsgsForTimeline
        {
            get
            {
                Func<HikeChatsDb, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc) =>
                    from o in hdc.statusMessage
                    where o.ShowOnTimeline == true
                    orderby o.StatusId descending
                    select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, long, int, IQueryable<StatusMessage>> GetPaginatedStatusMsgsForTimeline
        {
            get
            {
                Func<HikeChatsDb, long, int, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, long, int, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, long lastStatusId, int count) =>
                   (from o in hdc.statusMessage
                    where o.ShowOnTimeline == true && o.StatusId <= lastStatusId
                    orderby o.StatusId descending
                    select o).Take(count));
                return q;
            }
        }

        public static Func<HikeChatsDb, int, IQueryable<StatusMessage>> GetUnReadStatusMsgs
        {
            get
            {
                Func<HikeChatsDb, int, IQueryable<StatusMessage>> q =
                CompiledQuery.Compile<HikeChatsDb, int, IQueryable<StatusMessage>>
                ((HikeChatsDb hdc, int count) =>
                    from o in hdc.statusMessage.Take(count)
                    orderby o.StatusId descending
                    select o);
                return q;
            }
        }

        #endregion

        #region ConversationTable Queries

        //public static Func<HikeChatsDb, IQueryable<ConversationListObject>> GetAllConversations
        //{
        //    get
        //    {
        //        Func<HikeChatsDb, IQueryable<ConversationListObject>> q =
        //            CompiledQuery.Compile<HikeChatsDb, IQueryable<ConversationListObject>>
        //            ((HikeChatsDb hdc) =>
        //                from o in hdc.conversations
        //                select o);
        //        return q;
        //    }
        //}

        //public static Func<HikeChatsDb, string, IQueryable<ConversationListObject>> GetConvForMsisdn
        //{
        //    get
        //    {
        //        Func<HikeChatsDb, string, IQueryable<ConversationListObject>> q =
        //            CompiledQuery.Compile<HikeChatsDb, string, IQueryable<ConversationListObject>>
        //            ((HikeChatsDb hdc, string _msisdn) =>
        //                from o in hdc.conversations
        //                where o.Msisdn == _msisdn
        //                select o);
        //        return q;
        //    }
        //}

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
