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
       /// <param name="participantList"></param>
        public static void addGroupParticipants(List<GroupMembers> participantList)
        {
            if(participantList == null || participantList.Count == 0)
                return;
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                context.groupMembers.InsertAllOnSubmit(participantList);
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

        public static List<GroupMembers> getGroupMembers(string grpId)
        {
            using (HikeChatsDb context = new HikeChatsDb(App.MsgsDBConnectionstring))
            {
                IQueryable<GroupMembers> q =  DbCompiledQueries.GetActiveGroupMembersForGroupID(context, grpId);
                return q.ToList();
            }
            
        }
    }
}
