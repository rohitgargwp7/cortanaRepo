using System.ComponentModel;
using CommonLibrary.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Phone.Reactive;
using System;
using CommonLibrary.DbUtils;
using System.Diagnostics;
using CommonLibrary.utils;
using System.Windows.Media;
using CommonLibrary.Constants;
using CommonLibrary.Lib;
using CommonLibrary.Misc;

namespace CommonLibrary.ViewModel
{
    public class HikeViewModel
    {
        // this key will track number of conversations in the app. If this does not match the convs at load time , simply move to backup plan.
        public static string NUMBER_OF_CONVERSATIONS = "NoConvs";
        public static string NUMBER_OF_FAVS = "NoFavs";

        static StickerHelper _stickerHelper;
        public static StickerHelper StickerHelper
        {
            get
            {
                if (_stickerHelper == null)
                    _stickerHelper = new StickerHelper();

                return _stickerHelper;
            }
            private set
            {
                _stickerHelper = value;
            }
        }

        private Dictionary<string, ConversationListObject> _pendingReq = null;

        private Dictionary<string, ConversationListObject> _convMap;
        public Dictionary<string, ConversationListObject> ConvMap
        {
            get
            {
                return _convMap;
            }
            set
            {
                if (value != _convMap)
                    _convMap = value;
            }
        }

        private List<ConversationListObject> _favList = null;
        public List<ConversationListObject> FavList
        {
            get
            {
                return _favList;
            }
        }

        private object readWriteLock = new object();

        private bool isBlockedSetLoaded = false;

        private HashSet<string> _blockedHashSet = null;
        public HashSet<string> BlockedHashset
        {
            get
            {
                // optimization to avoid lock acquire again and again
                if (isBlockedSetLoaded)
                    return _blockedHashSet;

                lock (readWriteLock)
                {
                    if (_blockedHashSet == null)
                    {
                        _blockedHashSet = new HashSet<string>();
                        List<Blocked> blockList = UsersTableUtils.getBlockList();
                        
                        if (blockList != null)
                        {
                            for (int i = 0; i < blockList.Count; i++)
                                _blockedHashSet.Add(blockList[i].Msisdn);
                        }
                        
                        isBlockedSetLoaded = true;
                    }

                    return _blockedHashSet;
                }
            }
        }

        /// <summary>
        /// use this function to clear blocklist rather than BlockedHashset.clear()
        /// </summary>
        public void ClearBLockedHashSet()
        {
            lock (readWriteLock)
            {
                if (_blockedHashSet != null)
                {
                    _blockedHashSet.Clear();
                    _blockedHashSet = null;
                }

                isBlockedSetLoaded = false;
            }
        }
      
        public bool IsPendingListLoaded
        {
            get;
            set;
        }

        public Dictionary<string, ConversationListObject> PendingRequests
        {
            get
            {
                if (IsPendingListLoaded)
                    return _pendingReq;
                LoadPendingRequests();
                return _pendingReq;
            }
        }

        public void LoadPendingRequests()
        {
            try
            {
                MiscDBUtil.LoadPendingRequests(_pendingReq);
            }
            catch (Exception e)
            {
                Debug.WriteLine("HikeViewModel :: LoadPendingRequests : Exception : " + e.StackTrace);
            }
        }

        public HikeViewModel(List<ConversationListObject> convList)
        {
            _convMap = new Dictionary<string, ConversationListObject>();

            for (int i = 0; i < convList.Count; i++)
            {
                ConversationListObject convListObj = convList[i];
                _convMap[convListObj.Msisdn] = convListObj;
            }

            LoadViewModelObjects();
        }

        public HikeViewModel()
        {
            _convMap = new Dictionary<string, ConversationListObject>();

            LoadViewModelObjects();
        }

        private void LoadViewModelObjects()
        {
            _pendingReq = new Dictionary<string, ConversationListObject>();

            _favList = new List<ConversationListObject>();

            MiscDBUtil.LoadFavourites(_favList, _convMap);
            int count = 0;
            HikeInstantiation.AppSettings.TryGetValue<int>(HikeViewModel.NUMBER_OF_FAVS, out count);

            if (count != _favList.Count) // values are not loaded, move to backup plan
            {
                _favList.Clear();
                MiscDBUtil.LoadFavouritesFromIndividualFiles(_favList, _convMap);
            }

            MiscDBUtil.LoadPendingUploadPicRequests();

            ChatBackgroundHelper.Instance.Instantiate();

            if (HikeInstantiation.AppSettings.Contains(AppSettingsKeys.HIDDEN_MODE_ACTIVATED))
                IsHiddenModeActive = true;
        }

        public bool Isfavourite(string mContactNumber)
        {
            if (_favList.Count == 0)
                return false;

            for (int i = 0; i < _favList.Count; i++)
            {
                if (_favList[i].Msisdn == mContactNumber)
                    return true;
            }
            
            return false;
        }

        public ConversationListObject GetFav(string mContactNumber)
        {
            if (_favList.Count == 0)
                return null;

            for (int i = 0; i < _favList.Count; i++)
            {
                if (_favList[i].Msisdn == mContactNumber)
                    return _favList[i];
            }
            
            return null;
        }

        public bool IsPending(string ms)
        {
            if (!IsPendingListLoaded)
                MiscDBUtil.LoadPendingRequests(_pendingReq);

            if (_pendingReq == null)
                return false;
        
            if (_pendingReq.ContainsKey(ms))
                return true;
            
            return false;
        }

        public ConversationListObject GetPending(string mContactNumber)
        {
            if (_pendingReq.Count == 0)
                return null;

            if (_pendingReq.ContainsKey(mContactNumber))
                return _pendingReq[mContactNumber];
            
            return null;
        }

        private Dictionary<string, ContactInfo> _contactsCache = new Dictionary<string, ContactInfo>();
        public Dictionary<string, ContactInfo> ContactsCache
        {
            get
            {
                return _contactsCache;
            }
        }

        /// <summary>
        /// Check if hidden mode is active. True means hidden chats are visible.
        /// </summary>
        public Boolean IsHiddenModeActive
        {
            get;
            private set;
        }

        #region NEW GROUP PIC

        public List<GroupPic> PicUploadList = new List<GroupPic>();

        public void AddGroupPicForUpload(string id)
        {
            if (PicUploadList.Count == 10)
            {
                DeleteImageForMsisdn(id);
                return;
            }

            PicUploadList.Add(new GroupPic(id));
            MiscDBUtil.SavePendingUploadPicRequests();
        }

        public void DeleteImageForMsisdn(string id)
        {
            MiscDBUtil.DeleteImageForMsisdn(id);
        }

        #endregion
    }
}
