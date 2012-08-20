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
using System.Collections.Generic;
using System.Linq;

namespace windows_client.DbUtils
{
    public class GroupTableUtils
    {
       /// <summary>
       /// Adds a list of participants to the group
       /// </summary>
       /// <param name="memberList"></param>
        public static void addGroupMembers(List<GroupMembers> memberList)
        {
            if(memberList == null || memberList.Count == 0)
                return;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupMembers.InsertAllOnSubmit(memberList);
                context.SubmitChanges();
            }
        }

        public static void addGroupInfo(GroupInfo gi)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupInfo.InsertOnSubmit(gi);
                context.SubmitChanges();
            }
        }

        public static List<GroupMembers> getActiveGroupMembers(string grpId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                IQueryable<GroupMembers> q =  DbCompiledQueries.GetActiveGroupMembersForGroupID(context, grpId);
                return q.ToList();
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

        public static GroupInfo getGroupInfoForId(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                GroupInfo cObj = DbCompiledQueries.GetGroupInfoForID(context, groupId).FirstOrDefault();
                return cObj;
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

        public static void deleteGroupMembersWithId(string groupId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupMembers.DeleteAllOnSubmit<GroupMembers>(DbCompiledQueries.GetGroupMembersForGroupID(context, groupId));
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void removeParticipantFromGroup(string groupId, string msisdn)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                var q = from o in context.groupMembers where o.GroupId == groupId && o.Msisdn == msisdn select o;
                context.groupMembers.DeleteAllOnSubmit<GroupMembers>(q);
                MessagesTableUtils.SubmitWithConflictResolve(context);
            }
        }

        public static void deleteAllGroupMembers()
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupMembers.DeleteAllOnSubmit<GroupMembers>(context.GetTable<GroupMembers>());
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
