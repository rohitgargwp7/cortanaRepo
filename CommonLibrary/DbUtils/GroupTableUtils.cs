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
using System.Collections.Generic;
using CommonLibrary.Model;
using System.Linq;
using CommonLibrary.utils;
using Newtonsoft.Json.Linq;
using CommonLibrary.Constants;

namespace CommonLibrary.DbUtils
{
    public class GroupTableUtils
    {
        public static void addGroupInfo(GroupInfo gi)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                context.groupInfo.InsertOnSubmit(gi);
                context.SubmitChanges();
            }
        }

        public static bool updateGroupName(string groupId, string groupName)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (cObj == null)
                    return false;
                cObj.GroupName = groupName;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
            return true;
        }

        public static bool UpdateGroupOwner(string groupId, string groupOwner)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo gi = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (gi == null)
                    return false;
                if (gi.GroupOwner == groupOwner)
                    return false;
                gi.GroupOwner = groupOwner;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }

            return true;
        }

        public static bool SetGroupDead(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (cObj == null)
                    return false;
                cObj.GroupAlive = false;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
            return true;
        }

        public static void SetGroupAlive(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (cObj == null)
                    return;
                cObj.GroupAlive = true;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static GroupInfo getGroupInfoForId(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                return cObj;
            }
        }

        /// <summary>
        /// Update read by and last read message id for particular group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="lastReadMessageId"></param>
        /// <param name="readByArray"></param>
        public static void UpdateReadBy(string groupId, long lastReadMessageId, JArray readByArray)
        {
            if (string.IsNullOrEmpty(groupId) || readByArray == null || readByArray.Count == 0)
                return;

            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo gi = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (gi == null || gi.LastReadMessageId > lastReadMessageId)
                    return;

                if (gi.LastReadMessageId == lastReadMessageId)
                {
                    for (int i = 0; i < readByArray.Count; i++)
                    {
                        if (!gi.ReadByArray.Contains(readByArray[i]))
                            gi.ReadByArray.Add(readByArray[i]);
                    }
                }
                else
                {
                    gi.LastReadMessageId = lastReadMessageId;
                    gi.ReadByArray = readByArray;
                }
                gi.ReadByInfo = gi.ReadByArray.ToString();
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        /// <summary>
        /// Update read by and last read message id for particular group
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="lastMessageId"></param>
        /// <param name="readBy"></param>
        public static void UpdateReadBy(string groupId, long lastMessageId, string readBy)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(readBy))
                return;

            using (HikeChatsDb context = new HikeChatsDb(HikeConstants.DBStrings.MsgsDBConnectionstring))
            {
                GroupInfo gi = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (gi == null || gi.LastReadMessageId > lastMessageId)
                    return;

                if (gi.LastReadMessageId == lastMessageId)
                {
                    if (!gi.ReadByArray.Contains(readBy))
                        gi.ReadByArray.Add(readBy);
                }
                else
                {
                    gi.LastReadMessageId = lastMessageId;
                    gi.ReadByArray = new JArray() { readBy };
                }
                gi.ReadByInfo = gi.ReadByArray.ToString();
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }
    }
}
