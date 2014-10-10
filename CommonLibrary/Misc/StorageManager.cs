using CommonLibrary.Constants;
using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonLibrary.Misc
{
    public class StorageManager
    {
        private static object syncRoot = new Object(); // this object is used to take lock while creating singleton
        private static StorageManager instance = null;

        public static StorageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new StorageManager();
                        }
                    }
                }
                return instance;
            }
        }

        public long GetAvailableMemory()
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return store.AvailableFreeSpace;
            }
        }

        public bool IsDeviceMemorySufficient(int size)
        {
            using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                return store.AvailableFreeSpace > size + FTBasedConstants.APP_MIN_FREE_SIZE;
            }
        }
    }
}
