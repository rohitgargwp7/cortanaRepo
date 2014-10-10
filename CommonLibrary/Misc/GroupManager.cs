using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using CommonLibrary.DbUtils;
using CommonLibrary.Languages;
using CommonLibrary.Model;

namespace CommonLibrary.Misc
{
    class GroupManager
    {
        private string GROUP_DIR = "groups_dir";
        private object readWriteLock = new object();
        private IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication();

        private Dictionary<string, List<GroupParticipant>> groupParticpantsCache = null;

        public Dictionary<string, List<GroupParticipant>> GroupParticpantsCache
        {
            get
            {
                return groupParticpantsCache;
            }
            set
            {
                if (value != groupParticpantsCache)
                    groupParticpantsCache = value;
            }
        }

        private static volatile GroupManager instance = new GroupManager();

        private GroupManager()
        {
            groupParticpantsCache = new Dictionary<string, List<GroupParticipant>>();
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

        /// <summary>
        /// Get group participant
        /// </summary>
        /// <param name="defaultName">default name for the participant, null if want to use db name</param>
        /// <param name="msisdn">msisdn of the participant</param>
        /// <param name="grpId">group id</param>
        /// <returns>group participant</returns>
        public GroupParticipant GetGroupParticipant(string defaultName, string msisdn, string grpId)
        {
            if (grpId == null)
                return null;

            if (groupParticpantsCache.ContainsKey(grpId))
            {
                List<GroupParticipant> l = groupParticpantsCache[grpId];
                for (int i = 0; i < l.Count; i++)
                {
                    if (l[i].Msisdn == msisdn)
                    {
                        return l[i];
                    }
                }
            }

            ContactInfo cInfo = null;
            bool isInAdressBook = false;

            if (HikeInstantiation.ViewModel.ContactsCache.TryGetValue(msisdn, out cInfo) && cInfo.Name != null)
                isInAdressBook = true;
            else
            {
                cInfo = UsersTableUtils.getContactInfoFromMSISDN(msisdn);

                if (cInfo != null)
                {
                    isInAdressBook = true;
                    HikeInstantiation.ViewModel.ContactsCache[msisdn] = cInfo;
                }
            }

            GroupParticipant gp = new GroupParticipant(grpId, cInfo != null ? cInfo.Name : string.IsNullOrWhiteSpace(defaultName) ? msisdn : defaultName, msisdn, cInfo != null ? cInfo.OnHike : true);
            gp.IsInAddressBook = isInAdressBook;

            if (gp.Msisdn == HikeInstantiation.MSISDN)
                return gp;

            if (groupParticpantsCache.ContainsKey(grpId))
            {
                groupParticpantsCache[grpId].Add(gp);
                SaveGroupParticpantsCache();
                return gp;
            }

            List<GroupParticipant> ll = new List<GroupParticipant>();
            ll.Add(gp);
            groupParticpantsCache.Add(grpId, ll);
            SaveGroupParticpantsCache();
            return gp;
        }

        public void SaveGroupParticpantsCache(string grpId)
        {
            lock (readWriteLock)
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
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GroupManager :: SaveGroupParticpantsCache : delete file , Exception : " + ex.StackTrace);
                    }
                    try
                    {
                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(groupParticpantsCache[grpId].Count);
                                for (int i = 0; i < groupParticpantsCache[grpId].Count; i++)
                                {
                                    GroupParticipant item = groupParticpantsCache[grpId][i];
                                    item.Write(writer);
                                }
                                writer.Flush();
                                writer.Close();
                            }
                            file.Close();
                            file.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GroupManager :: SaveGroupParticpantsCache : write file , Exception : " + ex.StackTrace);
                    }
                }
            }
        }

        public void SaveGroupParticpantsCache()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    foreach (string grpId in groupParticpantsCache.Keys)
                    {
                        string grp = grpId.Replace(":", "_");
                        string fileName = GROUP_DIR + "\\" + grp;

                        try
                        {
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("GroupManager :: SaveGroupParticpantsCache : delete file , Exception : " + ex.StackTrace);
                        }
                        try
                        {
                            using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    writer.Seek(0, SeekOrigin.Begin);
                                    writer.Write(groupParticpantsCache[grpId].Count);
                                    for (int i = 0; i < groupParticpantsCache[grpId].Count; i++)
                                    {
                                        GroupParticipant item = groupParticpantsCache[grpId][i];
                                        item.Write(writer);
                                    }
                                    writer.Flush();
                                    writer.Close();
                                }
                                file.Close();
                                file.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("GroupManager :: SaveGroupParticpantsCache : write file , Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        public List<GroupParticipant> GetParticipantList(string grpId)
        {
            if (groupParticpantsCache.ContainsKey(grpId))
                return groupParticpantsCache[grpId];
            else // load from file
            {
                lock (readWriteLock)
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
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("GroupManager :: GetParticipantList : read count, Exception : " + ex.StackTrace);
                                    }
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
                                            catch (Exception ex)
                                            {
                                                Debug.WriteLine("GroupManager :: GetParticipantList : read group participant, Exception : " + ex.StackTrace);
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
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("GroupManager :: GetParticipantList : dispose file , Exception : " + ex.StackTrace);
                                }
                            }
                            if (gpList != null)
                                groupParticpantsCache[grpId] = gpList;
                            return gpList;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Load individual group cache to save memory (Lazy loading)
        /// </summary>
        /// <param name="grpId"></param>
        public void LoadGroupParticipants(string grpId)
        {
            if (groupParticpantsCache.ContainsKey(grpId))
                return;

            lock (readWriteLock)
            {
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
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("GroupManager :: LoadGroupParticipants : read count, Exception : " + ex.StackTrace);
                                }
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
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine("GroupManager :: LoadGroupParticipants : read item, Exception : " + ex.StackTrace);
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
                            catch (Exception ex)
                            {
                                Debug.WriteLine("GroupManager :: LoadGroupParticipants : dispose file, Exception : " + ex.StackTrace);
                            }
                        }
                        if (gpList != null)
                            groupParticpantsCache[grpId] = gpList;
                    }
                }
            }
        }

        /// <summary>
        /// Load entire group cache to perform operation on all groups
        /// </summary>
        public void LoadGroupParticpantsCache()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string[] files = store.GetFileNames(GROUP_DIR + "\\*");
                    if (files == null || files.Length == 0)
                        return;
                    for (int i = 0; i < files.Length; i++)
                    {
                        var index = files[i].LastIndexOf("_");
                        StringBuilder sb = new StringBuilder(files[i]);
                        sb[index] = ':';
                        string grpId = sb.ToString();

                        if (groupParticpantsCache.ContainsKey(grpId)) // if this group is already loaded ignore
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
                                catch (Exception ex)
                                {
                                    Debug.WriteLine("GroupManager :: LoadGroupParticpantsCache : read count, Exception : " + ex.StackTrace);
                                }
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
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine("GroupManager :: LoadGroupParticpantsCache : read item, Exception : " + ex.StackTrace);
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
                            catch (Exception ex)
                            {
                                Debug.WriteLine("GroupManager :: LoadGroupParticpantsCache : dispose file, Exception : " + ex.StackTrace);
                            }
                        }
                        if (gpList != null)
                            groupParticpantsCache[grpId] = gpList;

                    }
                }
            }
        }

        public string defaultGroupName(string grpId)
        {
            List<GroupParticipant> groupParticipants = null;
            groupParticpantsCache.TryGetValue(grpId, out groupParticipants);
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
                    return activeMembers[0].FirstName + AppResources.And_txt
                    + activeMembers[1].FirstName;
                default:
                    return string.Format(AppResources.NamingConvention_Txt, activeMembers[0].FirstName, activeMembers.Count - 1);
            }
        }

        public List<GroupParticipant> GetActiveGroupParticiants(string groupId)
        {
            if (!groupParticpantsCache.ContainsKey(groupId) || groupParticpantsCache[groupId] == null)
                return null;

            List<GroupParticipant> activeGroupMembers = new List<GroupParticipant>(groupParticpantsCache[groupId].Count);
            for (int i = 0; i < groupParticpantsCache[groupId].Count; i++)
            {
                if (!groupParticpantsCache[groupId][i].HasLeft)
                    activeGroupMembers.Add(groupParticpantsCache[groupId][i]);
            }
            return activeGroupMembers;
        }

        public void DeleteAllGroups()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string[] files = store.GetFileNames(GROUP_DIR + "\\*");
                    for (int i = 0; i < files.Length; i++)
                    {
                        try
                        {
                            store.DeleteFile(GROUP_DIR + "\\" + files[i]);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("GroupManager :: DeleteAllGroups :DeleteAllGroups, Exception : " + ex.StackTrace);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Deletes group cache file
        /// </summary>
        /// <param name="grpId"></param>
        public void DeleteGroup(string grpId)
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (groupParticpantsCache != null)
                        groupParticpantsCache.Remove(grpId);
                    string grp = grpId.Replace(":", "_");
                    store.DeleteFile(GROUP_DIR + "\\" + grp);
                }
            }
        }
    }
}
