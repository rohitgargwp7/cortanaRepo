using System;
using windows_client.Model;
using System.Linq;
using System.Collections.Generic;
using windows_client.utils;
using windows_client.View;
using System.Data.Linq;

namespace windows_client.DbUtils
{
    public class ConversationTableUtils
    {
        /* This function gets all the conversations shown on the message list page*/
        public static List<ConversationListObject> getAllConversations()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                var q = from o in context.conversations orderby o.TimeStamp descending select o;
                return q.ToList();
            }           
        }

        public static void updateImage(string msisdn,byte [] image)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConversationListObject obj = DbCompiledQueries.GetConvForMsisdn(context, msisdn).FirstOrDefault<ConversationListObject>();
                if (obj != null)
                {
                    obj.Avatar = image;
                    MessagesTableUtils.SubmitWithConflictResolve(context);
                }
            } 
        }

        public static ConversationListObject addGroupConversation(ConvMessage convMessage,string groupName)
        {
             /*
             * Msisdn : GroupId
             * Contactname : GroupOwner
             */
            ConversationListObject obj = new ConversationListObject(convMessage.Msisdn, groupName, convMessage.Message,
                true, convMessage.Timestamp, null, convMessage.MessageStatus);

            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.InsertOnSubmit(obj);
                context.SubmitChanges();
            }
            return obj;
        }

        public static ConversationListObject addConversation(ConvMessage convMessage)
        {
            ContactInfo contactInfo = UsersTableUtils.getContactInfoFromMSISDN(convMessage.Msisdn);
            Thumbnails thumbnail = MiscDBUtil.getThumbNailForMSisdn(convMessage.Msisdn);
            ConversationListObject obj = new ConversationListObject(convMessage.Msisdn, contactInfo == null ? null : contactInfo.Name, convMessage.Message,
                contactInfo == null ? !convMessage.IsSms : contactInfo.OnHike, convMessage.Timestamp, thumbnail==null?null:thumbnail.Avatar,convMessage.MessageStatus);
            
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {              
                    context.conversations.InsertOnSubmit(obj);
                    context.SubmitChanges();
            }
            return obj;
        }

        public static void deleteAllConversations()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<ConversationListObject>(context.GetTable<ConversationListObject>());
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void deleteConversation(string msisdn)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.conversations.DeleteAllOnSubmit<ConversationListObject>(DbCompiledQueries.GetConvForMsisdn(context, msisdn));
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void updateOnHikeStatus(string msisdn, bool joined)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                List<ConversationListObject> res = DbCompiledQueries.GetConvForMsisdn(context, msisdn).ToList<ConversationListObject>();
                if (res == null || res.Count<ConversationListObject>() == 0)
                    return;
                for (int i = 0; i < res.Count;i++ )
                {
                    ConversationListObject conv = res[i];
                    conv.IsOnhike = (bool)joined;
                }
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void updateConversation(ConversationListObject obj)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, obj.Msisdn).FirstOrDefault();
                cObj.MessageStatus =  obj.MessageStatus;
                cObj.LastMessage = obj.LastMessage;
                cObj.TimeStamp = obj.TimeStamp;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }
        public static void updateGroupName(string grpId,string groupName)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, grpId).FirstOrDefault();
                cObj.ContactName = groupName;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }
        internal static void updateConversation(List<ContactInfo> cn)
        {
            bool shouldSubmit = false;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                for (int i = 0; i < cn.Count; i++)
                {
                    if (ConversationsList.ConvMap.ContainsKey(cn[i].Msisdn))
                    {
                        ConversationListObject obj = ConversationsList.ConvMap[cn[i].Msisdn]; //update UI
                        obj.ContactName = cn[i].Name;

                        ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, obj.Msisdn).FirstOrDefault();
                        if (cObj.ContactName != cn[i].Name)
                        {
                            cObj.ContactName = cn[i].Name;
                            shouldSubmit = true;
                        }
                    }
                }
                if (shouldSubmit)
                {
                    MessagesTableUtils.SubmitWithConflictResolve(context);
                }
            }
        }

        public static void updateLastMsgStatus(string msisdn, int status)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                ConversationListObject cObj = DbCompiledQueries.GetConvForMsisdn(context, msisdn).FirstOrDefault<ConversationListObject>();
                cObj.MessageStatus = (ConvMessage.State)status;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }
    }
}
