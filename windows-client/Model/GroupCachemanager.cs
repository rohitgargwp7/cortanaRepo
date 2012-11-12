using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace windows_client.Model
{
    class GroupCachemanager
    {
        private static volatile GroupCachemanager instance = new GroupCachemanager();

        private GroupCachemanager()
        {
            groupCache = new Dictionary<string, List<GroupParticipant>>();
        }

        public static GroupCachemanager GROUP_CACHE_MANAGER_INSTANCE
        {
            get
            {
                return instance;
            }
        }

        private static Dictionary<string, List<GroupParticipant>> groupCache = null;
        private Dictionary<string, List<GroupParticipant>> GroupCache
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
    }
}
