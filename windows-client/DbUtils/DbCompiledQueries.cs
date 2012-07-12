﻿using System;
using windows_client.Model;
using System.Linq;
using System.Data.Linq;

namespace windows_client.DbUtils
{
    public static class DbCompiledQueries
    {
        #region UsersTable Queries

        public static Func<HikeDataContext, IQueryable<ContactInfo>> GetAllContacts
        {
            get
            {
                Func<HikeDataContext, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
                     ((HikeDataContext hdc) =>
                         from o in hdc.users
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<ContactInfo>> GetContactFromName
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
                   CompiledQuery.Compile((HikeDataContext hdc, string chars) =>
                       from o in hdc.users
                       where o.Name.Contains(chars) || o.PhoneNo.Contains(chars)
                       select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<ContactInfo>> GetContactFromMsisdn
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
                    CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
                    ((HikeDataContext hdc, string m) =>
                        from o in hdc.users
                        where o.Msisdn == m
                        select o);
                return q;
            }
        }

        public static Func<HikeDataContext, IQueryable<Blocked>> GetBlockList
        {
            get
            {
                Func<HikeDataContext, IQueryable<Blocked>> q =
                     CompiledQuery.Compile<HikeDataContext, IQueryable<Blocked>>
                     ((HikeDataContext hdc) =>
                         from o in hdc.blockedUsersTable
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<ContactInfo>> UpdateOnhikeStatus
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
                     ((HikeDataContext hdc, string ms) =>
                         from o in hdc.users
                         where o.Msisdn == ms
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<Blocked>> GetBlockedUserForMsisdn
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<Blocked>> q =
                     CompiledQuery.Compile<HikeDataContext, string, IQueryable<Blocked>>
                     ((HikeDataContext hdc, string ms) =>
                         from o in hdc.blockedUsersTable
                         where o.Msisdn == ms
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<ContactInfo>> GetUsersWithGivenId
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<ContactInfo>> q =
                     CompiledQuery.Compile<HikeDataContext, string, IQueryable<ContactInfo>>
                     ((HikeDataContext hdc, string i) =>
                         from o in hdc.users
                         where o.Id == i
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, IQueryable<ContactInfo>> GetContactsForOnhikeStatus
        {
            get
            {
                Func<HikeDataContext, IQueryable<ContactInfo>> q =
                    CompiledQuery.Compile<HikeDataContext, IQueryable<ContactInfo>>
                    ((HikeDataContext hdc) =>
                        from o in hdc.users
                        where o.OnHike == false
                        orderby o.Name
                        select o);
                return q;
            }
        }

        #endregion

        #region MessagesTable Queries

        public static Func<HikeDataContext, string, IQueryable<ConvMessage>> GetMessagesForMsisdn
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<ConvMessage>> q =
                CompiledQuery.Compile<HikeDataContext, string, IQueryable<ConvMessage>>
                ((HikeDataContext hdc, string myMsisdn) =>
                    from o in hdc.messages
                    where o.Msisdn == myMsisdn
                    select o);
                return q;
            }
        }
        public static Func<HikeDataContext, long, IQueryable<ConvMessage>> GetMessagesForMsgId
        {
            get
            {
                Func<HikeDataContext, long, IQueryable<ConvMessage>> q =
                     CompiledQuery.Compile<HikeDataContext, long, IQueryable<ConvMessage>>
                     ((HikeDataContext hdc, long id) =>
                         from o in hdc.messages
                         where o.MessageId == id
                         select o);
                return q;
            }
        }

        public static Func<HikeDataContext, IQueryable<ConvMessage>> GetAllMessages
        {
            get
            {
                Func<HikeDataContext, IQueryable<ConvMessage>> q =
                    CompiledQuery.Compile<HikeDataContext, IQueryable<ConvMessage>>
                    ((HikeDataContext hdc) =>
                        from o in hdc.messages
                        select o);
                return q;
            }
        }

        #endregion

        #region ConversationTable Queries

        public static Func<HikeDataContext, IQueryable<Conversation>> GetAllConversations
        {
            get
            {
                Func<HikeDataContext, IQueryable<Conversation>> q =
                    CompiledQuery.Compile<HikeDataContext, IQueryable<Conversation>>
                    ((HikeDataContext hdc) =>
                        from o in hdc.conversations
                        select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<Conversation>> GetConvForMsisdn
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<Conversation>> q =
                    CompiledQuery.Compile<HikeDataContext, string, IQueryable<Conversation>>
                    ((HikeDataContext hdc, string _msisdn) =>
                        from o in hdc.conversations
                        where o.Msisdn == _msisdn
                        select o);
                return q;
            }
        }

        #endregion

        #region MqttTable Queries

        public static Func<HikeDataContext, IQueryable<HikePacket>> GetAllSentMessages
        {
            get
            {
                Func<HikeDataContext, IQueryable<HikePacket>> q =
                    CompiledQuery.Compile<HikeDataContext, IQueryable<HikePacket>>
                    ((HikeDataContext hdc) =>
                        from o in hdc.mqttMessages
                        select o);
                return q;
            }
        }

        public static Func<HikeDataContext, long, IQueryable<HikePacket>> GetMqttMsgForMsgId
        {
            get
            {
                Func<HikeDataContext, long, IQueryable<HikePacket>> q =
                   CompiledQuery.Compile<HikeDataContext, long, IQueryable<HikePacket>>
                   ((HikeDataContext hdc, long id) =>
                       from o in hdc.mqttMessages
                       where o.MessageId == id
                       select o);
                return q;
            }
        }

        #endregion

        #region MiscTable Queries

        public static Func<HikeDataContext, IQueryable<Thumbnails>> GetAllIcons
        {
            get
            {
                Func<HikeDataContext, IQueryable<Thumbnails>> q =
                  CompiledQuery.Compile<HikeDataContext, IQueryable<Thumbnails>>
                  ((HikeDataContext hdc) =>
                      from o in hdc.thumbnails
                      select o);
                return q;
            }
        }

        public static Func<HikeDataContext, string, IQueryable<Thumbnails>> GetIconForMsisdn
        {
            get
            {
                Func<HikeDataContext, string, IQueryable<Thumbnails>> q =
                   CompiledQuery.Compile<HikeDataContext, string, IQueryable<Thumbnails>>
                   ((HikeDataContext hdc, string m) =>
                       from o in hdc.thumbnails
                       where o.Msisdn == m
                       select o);
                return q;
            }
        }

        #endregion
    }
}
