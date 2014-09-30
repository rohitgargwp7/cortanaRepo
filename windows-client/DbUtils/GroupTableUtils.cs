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
using System.Collections.Generic;
using windows_client.Model;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace windows_client.DbUtils
{
    public class GroupTableUtils
    {

        public static void addGroupInfo(GroupInfo gi)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupInfo.InsertOnSubmit(gi);
                context.SubmitChanges();
            }
        }

        public static bool updateGroupName(string groupId, string groupName)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (cObj == null)
                    return;
                cObj.GroupAlive = true;
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static bool IsGroupAlive(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (cObj == null)
                    return false;
                return cObj.GroupAlive;
            }
        }

        public static List<GroupInfo> GetAllGroups()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                return DbCompiledQueries.GetAllGroups(context).ToList();
            }
        }

        public static GroupInfo getGroupInfoForId(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                return cObj;
            }
        }

        public static List<GroupInfo> getAllGroupInfo()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                return DbCompiledQueries.GetAllGroupInfo(context).ToList();
            }
        }

        public static void UpdateReadBy(string groupId, long lastMessageId, JArray readByArray)
        {
            if (string.IsNullOrEmpty(groupId) || readByArray == null || readByArray.Count == 0)
                return;

            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                GroupInfo gi = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                if (gi == null || gi.LastReadMessageId > lastMessageId)
                    return;

                if (gi.LastReadMessageId == lastMessageId)
                {
                    for (int i = 0; i < readByArray.Count; i++)
                    {
                        if (!gi.ReadByArray.Contains(readByArray[i]))
                            gi.ReadByArray.Add(readByArray[i]);
                    }
                }
                else
                {
                    gi.LastReadMessageId = lastMessageId;
                    gi.ReadByArray = readByArray;
                }
                gi.ReadByInfo = gi.ReadByArray.ToString();
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void UpdateReadBy(string groupId, long lastMessageId, string readBy)
        {
            if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(readBy))
                return;

            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
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

        public static void deleteGroupWithId(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupInfo.DeleteAllOnSubmit<GroupInfo>(DbCompiledQueries.GetGroupInfoForID(context, groupId));
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void deleteAllGroups()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupInfo.DeleteAllOnSubmit<GroupInfo>(context.GetTable<GroupInfo>());
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }
    }
}
