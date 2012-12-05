﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using windows_client.DbUtils;
using windows_client.Model;

namespace windows_client.Misc
{
    class GroupManager
    {
        private string GROUP_DIR = "groups_dir";
        private IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();

        private Dictionary<string, List<GroupParticipant>> groupCache = null;

        public Dictionary<string, List<GroupParticipant>> GroupCache
        {
            get
            {
                return groupCache;
            }
            set
            {
                if (value != groupCache)
                    groupCache = value;
            }
        }

        private static volatile GroupManager instance = new GroupManager();

        private GroupManager()
        {
            groupCache = new Dictionary<string, List<GroupParticipant>>();
            if (!isoStore.DirectoryExists(GROUP_DIR))
                isoStore.CreateDirectory(GROUP_DIR);
        }
        
        public static GroupManager Instance
        {
            get
            {
                return instance;
            }
        }

        public GroupParticipant getGroupParticipant(string defaultName, string msisdn, string grpId)
        {
            if (grpId == null)
                return null;

            if (groupCache.ContainsKey(grpId))
            {
                List<GroupParticipant> l = groupCache[grpId];
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].Msisdn == msisdn)
                    {
                        return l[i];
                    }
                }
            }
            ContactInfo cInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);
            GroupParticipant gp = new GroupParticipant(grpId, cInfo != null ? cInfo.Name : string.IsNullOrWhiteSpace(defaultName) ? msisdn : defaultName, msisdn, cInfo != null ? cInfo.OnHike : true);
            if (groupCache.ContainsKey(grpId))
            {
                groupCache[grpId].Add(gp);
                return gp;
            }

            List<GroupParticipant> ll = new List<GroupParticipant>();
            ll.Add(gp);
            groupCache.Add(grpId, ll);
            return gp;
        }

        public void SaveGroupCache(string grpId)
        {
            string grp = grpId.Replace(":", "_");
            string fileName = GROUP_DIR + "\\" + grp;
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                try
                {
                    if (store.FileExists(fileName))
                        store.DeleteFile(fileName);
                }
                catch { }
                try
                {
                    using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            writer.Seek(0, SeekOrigin.Begin);
                            writer.Write(groupCache[grpId].Count);
                            for (int i = 0; i < groupCache[grpId].Count; i++)
                            {
                                GroupParticipant item = groupCache[grpId][i];
                                item.Write(writer);
                            }
                            writer.Flush();
                            writer.Close();
                        }
                        file.Close();
                        file.Dispose();
                    }
                }
                catch { }
            }
        }

        public void SaveGroupCache()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                foreach (string grpId in groupCache.Keys)
                {
                    string grp = grpId.Replace(":", "_");
                    string fileName = GROUP_DIR + "\\" + grp;

                    try
                    {
                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                    catch { }
                    try
                    {
                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(groupCache[grpId].Count);
                                for (int i = 0; i < groupCache[grpId].Count; i++)
                                {
                                    GroupParticipant item = groupCache[grpId][i];
                                    item.Write(writer);
                                }
                                writer.Flush();
                                writer.Close();
                            }
                            file.Close();
                            file.Dispose();
                        }
                    }
                    catch { }
                }
            }
        }

        public List<GroupParticipant> GetParticipantList(string grpId)
        {
            if (groupCache.ContainsKey(grpId))
                return groupCache[grpId];
            else // load from file
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    List<GroupParticipant> gpList = null;
                    string grp = grpId.Replace(":", "_");
                    string fileName = GROUP_DIR + "\\" + grp;
                    if (!store.FileExists(fileName))
                        return null;
                    else
                    {
                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                int count = 0;
                                try
                                {
                                    count = reader.ReadInt32();
                                }
                                catch { }
                                if (count > 0)
                                {
                                    gpList = new List<GroupParticipant>(count);
                                    for (int i = 0; i < count; i++)
                                    {
                                        GroupParticipant item = new GroupParticipant();
                                        try
                                        {
                                            item.Read(reader);
                                        }
                                        catch
                                        {
                                            item = null;
                                        }
                                    }
                                }
                                reader.Close();
                            }
                            try
                            {
                                file.Close();
                                file.Dispose();
                            }
                            catch { }
                        }
                        if (gpList != null)
                            groupCache[grpId] = gpList;
                        return gpList;
                    }
                }
            }
        }

        public void LoadGroupParticipants(string grpId)
        {
            if (groupCache.ContainsKey(grpId))
                return;

            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                List<GroupParticipant> gpList = null;
                string grp = grpId.Replace(":", "_");
                string fileName = GROUP_DIR + "\\" + grp;
                if (!store.FileExists(fileName))
                    return;
                else
                {
                    using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;
                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch { }
                            if (count > 0)
                            {
                                gpList = new List<GroupParticipant>(count);
                                for (int i = 0; i < count; i++)
                                {
                                    GroupParticipant item = new GroupParticipant();
                                    try
                                    {
                                        item.Read(reader);
                                        gpList.Add(item);
                                    }
                                    catch
                                    {
                                        item = null;
                                    }
                                }
                            }
                            reader.Close();
                        }
                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch { }
                    }
                    if (gpList != null)
                        groupCache[grpId] = gpList;
                }
            }
        }

        public void LoadGroupCache()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(GROUP_DIR + "\\*");
                if (files == null || files.Length == 0)
                    return;
                for (int i = 0; i < files.Length; i++)
                {
                    string grpId = files[i].Replace("_", ":");
                    if (groupCache.ContainsKey(grpId))
                        continue;
                    string fileName = GROUP_DIR + "\\" + files[i];
                    List<GroupParticipant> gpList = null;

                    using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var reader = new BinaryReader(file))
                        {
                            int count = 0;
                            try
                            {
                                count = reader.ReadInt32();
                            }
                            catch { }
                            if (count > 0)
                            {
                                gpList = new List<GroupParticipant>(count);
                                for (int j = 0; j < count; j++)
                                {
                                    GroupParticipant item = new GroupParticipant();
                                    try
                                    {
                                        item.Read(reader);
                                        gpList.Add(item);
                                    }
                                    catch
                                    {
                                        item = null;
                                    }
                                }
                            }
                            reader.Close();
                        }
                        try
                        {
                            file.Close();
                            file.Dispose();
                        }
                        catch { }
                    }
                    if (gpList != null)
                        groupCache[grpId] = gpList;

                }
            }
        }

        public string defaultGroupName(string grpId)
        {
            List<GroupParticipant> groupParticipants = null;
            groupCache.TryGetValue(grpId, out groupParticipants);
            if (groupParticipants == null || groupParticipants.Count == 0) // this should not happen as at this point cache should be populated
                return "GROUP";
            List<GroupParticipant> activeMembers = GetActiveGroupParticiants(grpId);
            if (activeMembers == null || groupParticipants.Count == 0)
                return "GROUP";
            switch (activeMembers.Count)
            {
                case 1:
                    return activeMembers[0].FirstName;
                case 2:
                    return activeMembers[0].FirstName + " and "
                    + activeMembers[1].FirstName;
                default:
                    return activeMembers[0].FirstName + " and "
                    + (activeMembers.Count - 1) + " others";
            }
        }

        public List<GroupParticipant> GetActiveGroupParticiants(string groupId)
        {
            if (!groupCache.ContainsKey(groupId) || groupCache[groupId] == null)
                return null;
            List<GroupParticipant> activeGroupMembers = new List<GroupParticipant>(groupCache[groupId].Count);
            for (int i = 0; i < groupCache[groupId].Count; i++)
            {
                if (!groupCache[groupId][i].HasLeft)
                    activeGroupMembers.Add(groupCache[groupId][i]);
            }
            return activeGroupMembers;
        }

        public void DeleteAllGroups()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string[] files = store.GetFileNames(GROUP_DIR + "\\*");
                for (int i = 0; i < files.Length; i++)
                    store.DeleteFile(GROUP_DIR + "\\" + files[i]);
            }
        }

        /// <summary>
        /// Deletes group cache file
        /// </summary>
        /// <param name="grpId"></param>
        public void DeleteGroup(string grpId)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string grp = grpId.Replace(":","_");
                store.DeleteFile(GROUP_DIR + "\\" + grp);
            }
        }
    }
}