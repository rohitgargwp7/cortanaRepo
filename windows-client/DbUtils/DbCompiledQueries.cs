using System;
using windows_client.Model;
using System.Linq;
using System.Data.Linq;

namespace windows_client.DbUtils
{
    public class DbCompiledQueries
    {
        public static HikeChatsDb chatsDbContext = new HikeChatsDb(App.MsgsDBConnectionstring);
        public static HikeUsersDb usersDbContext = new HikeUsersDb(App.UsersDBConnectionstring);

        #region GroupTable Queries

        public static Func<HikeChatsDb, string, string, IQueryable<GroupMembers>> GetGroupMembers
        {
            get
            {
                Func<HikeChatsDb, string,string,IQueryable<GroupMembers>> q =
                     CompiledQuery.Compile<HikeChatsDb, string, string, IQueryable<GroupMembers>>
                     ((HikeChatsDb hdc, string grpId,string msisdn) =>
                         from o in hdc.groupMembers
                         where o.GroupId == grpId && o.Msisdn == msisdn
                         select o);
                return q;
            }
        }

        public static Func<HikeChatsDb,string ,IQueryable<GroupMembers>> GetGroupMembersForGroupID
        {
            get
            {
                Func<HikeChatsDb,string ,IQueryable<GroupMembers>> q =
                     CompiledQuery.Compile<HikeChatsDb, string, IQueryable<GroupMembers>>
                     ((HikeChatsDb hdc,string grpId) =>
                         from o in hdc.groupMembers
                         where o.GroupId == grpId 
                         select o);
                return q;
            }
        }

        public static Func<HikeChatsDb, string, IQueryable<GroupMembers>> GetActiveGroupMembersForGroupID
        {
            get
            {
                Func<HikeChatsDb, string, IQueryable<GroupMembers>> q =
                     CompiledQuery.Compile<HikeChatsDb, string, IQueryable<GroupMembers>>
                     ((HikeChatsDb hdc, string grpId) =>
                         from o in hdc.groupMembers
                         where o.GroupId == grpId && o.HasLeft == false
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
                        select o);
                return q;
            }
        }

        public static Func<HikeMqttPersistenceDb, long, IQueryable<HikePacket>> GetMqttMsgForMsgId
        {
            get
            {
                Func<HikeMqttPersistenceDb, long, IQueryable<HikePacket>> q =
                   CompiledQuery.Compile<HikeMqttPersistenceDb, long, IQueryable<HikePacket>>
                   ((HikeMqttPersistenceDb hdc, long id) =>
                       from o in hdc.mqttMessages
                       where o.MessageId == id
                       select o);
                return q;
            }
        }

        #endregion

    }
}
