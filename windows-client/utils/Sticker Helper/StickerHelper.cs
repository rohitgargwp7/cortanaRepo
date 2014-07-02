using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using windows_client.Misc;
using windows_client.Model.Sticker;
using windows_client.utils.Sticker_Helper;
using windows_client.ViewModel;

namespace windows_client.utils
{

    public class StickerHelper
    {
        public const string CATEGORY_RECENT = "recent";
        public const string CATEGORY_HUMANOID = "humanoid";
        public const string CATEGORY_HUMANOID2 = "humanoid2";
        public const string CATEGORY_DOGGY = "doggy";
        public const string CATEGORY_KITTY = "kitty";
        public const string CATEGORY_EXPRESSIONS = "expressions";
        public const string CATEGORY_SMILEY_EXPRESSIONS = "smileyexpressions";
        public const string CATEGORY_BOLLYWOOD = "bollywood";
        public const string CATEGORY_TROLL = "rageface";
        public const string CATEGORY_AVATARS = "avatars";
        public const string CATEGORY_INDIANS = "indian";
        public const string CATEGORY_JELLY = "jelly";
        public const string CATEGORY_SPORTS = "sports";
        public const string CATEGORY_LOVE = "love";
        public const string CATEGORY_ANGRY = "angry";

        //File constants
        public const string STICKERS_DIR = "stickers";
        public const string HIGH_RESOLUTION_DIR = "highres";
        public const string LOW_RESOLUTION_DIR = "lowres";
        public const string METADATA = "metadata";

        public const string _stickerWVGAPath = "/View/images/stickers/WVGA/{0}/{1}";
        private static object readWriteLock = new object();

        public LruCache<string, BitmapImage> lruStickers = new LruCache<string, BitmapImage>(20, 0);
        public RecentStickerHelper recentStickerHelper;
        public static string[] arrayDefaultHumanoidStickers = new string[]
        {
            "001_love1.png",
            "002_love2.png",
            "003_teasing.png",
            "004_rofl.png",
            "005_bored.png",
            "006_angry.png",
            "007_strangle.png",
            "008_shocked.png",
            "009_hurray.png",
            "010_yawning.png"
        
        };
        public static string[] arrayDefaultExpressionStickers = new string[]
        {
           "001_gn.png",
           "002_lol.png",
           "003_rofl.png",
           "004_lmao.png",
           "005_omg.png",
           "006_brb.png",
           "007_gtg.png",
           "008_xoxo.png"
        };
        private bool _isInitialised;
        private Dictionary<string, StickerCategory> _dictStickersCategories;

        //call from background
        public void InitialiseLowResStickers()
        {
            try
            {
                if (!_isInitialised)
                {
                    if (recentStickerHelper == null)
                    {
                        recentStickerHelper = new RecentStickerHelper();
                        recentStickerHelper.LoadSticker();
                    }

                    _dictStickersCategories = new Dictionary<string, StickerCategory>();

                    StickerCategory stickerCategoryRecent = new StickerCategory(CATEGORY_RECENT);
                    stickerCategoryRecent.HasMoreStickers = false;
                    stickerCategoryRecent.HasNewStickers = false;
                    stickerCategoryRecent.ShowDownloadMessage = false;
                    //stickerCategoryRecent.ListStickers = recentStickerHelper.listRecentStickers;

                    _dictStickersCategories[CATEGORY_RECENT] = stickerCategoryRecent;

                    InitialiseDefaultStickers(CATEGORY_HUMANOID, arrayDefaultHumanoidStickers);

                    InitialiseDefaultStickers(CATEGORY_EXPRESSIONS, arrayDefaultExpressionStickers);

                    List<StickerCategory> listStickerCategories = ReadAllStickerCategories();
                    foreach (StickerCategory sc in listStickerCategories)
                    {
                        if (_dictStickersCategories.ContainsKey(sc.Category))
                        {
                            StickerCategory stickerCategory = _dictStickersCategories[sc.Category];
                            foreach (StickerObj sticker in sc.ListStickers)
                            {
                                stickerCategory.ListStickers.Add(sticker);
                            }
                            sc.ListStickers = stickerCategory.ListStickers;
                        } _dictStickersCategories[sc.Category] = sc;
                    }

                    _isInitialised = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerHelper::InitialiseStickers, Exception:" + ex.Message);
            }
        }

        private void InitialiseDefaultStickers(string category, string[] arrayDefaultStickers)
        {
            StickerCategory category1Stickers = new StickerCategory(category, true);
            for (int i = 0; i < arrayDefaultStickers.Length; i++)
            {
                StickerObj sticker = new StickerObj(category1Stickers.Category, arrayDefaultStickers[i], null, false);
                category1Stickers.ListStickers.Add(sticker);
            }
            if (_dictStickersCategories.ContainsKey(category1Stickers.Category))
            {
                StickerCategory stickerCategory = _dictStickersCategories[category1Stickers.Category];
                foreach (StickerObj sticker in category1Stickers.ListStickers)
                {
                    stickerCategory.ListStickers.Add(sticker);
                }
                _dictStickersCategories[category1Stickers.Category] = stickerCategory;
            }
            else
                _dictStickersCategories[category1Stickers.Category] = category1Stickers;

        }

        public StickerCategory GetStickersByCategory(string category)
        {
            if (String.IsNullOrEmpty(category))
                return null;

            if (_dictStickersCategories.ContainsKey(category))
            {
                return _dictStickersCategories[category];
            }

            return null;
        }

        public Dictionary<string, StickerCategory> DictStickersCategories
        {
            get
            {
                return _dictStickersCategories;
            }
        }

        public static void CreateDefaultCategories()
        {
            StickerHelper.CreateCategory(CATEGORY_HUMANOID);
            StickerHelper.CreateCategory(CATEGORY_DOGGY);
            StickerHelper.CreateCategory(CATEGORY_KITTY);
            StickerHelper.CreateCategory(CATEGORY_EXPRESSIONS);
            StickerHelper.CreateCategory(CATEGORY_BOLLYWOOD);
            StickerHelper.CreateCategory(CATEGORY_TROLL);
            StickerHelper.CreateCategory(CATEGORY_AVATARS);
            StickerHelper.CreateCategory(CATEGORY_INDIANS);
            StickerHelper.CreateCategory(CATEGORY_JELLY);
            StickerHelper.CreateCategory(CATEGORY_SPORTS);
            StickerHelper.CreateCategory(CATEGORY_HUMANOID2);
            StickerHelper.CreateCategory(CATEGORY_SMILEY_EXPRESSIONS);
            StickerHelper.CreateCategory(CATEGORY_LOVE);
        }

        /// <summary>
        /// Get sticker if present on client
        /// </summary>
        /// <param name="image"></param>
        /// <param name="category"></param>
        /// <param name="stickerId"></param>
        /// <param name="stickerImageBytes">send null if no bytes available</param>
        /// <param name="isHighres"></param>
        public async Task GetSticker(BitmapImage image, string category, string stickerId, byte[] stickerImageBytes, bool isHighres)
        {
            if (!isHighres)
                await Task.Delay(1);
            if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(category))
                return;
            if (stickerImageBytes != null && stickerImageBytes.Length > 0)
            {
                UI_Utils.Instance.createImageFromBytes(stickerImageBytes, image);
                if (isHighres)
                    App.newChatThreadPage.lruStickerCache.AddObject(category + "_" + stickerId, image);
                else
                    lruStickers.AddObject(category + "_" + stickerId, image);
                return;
            }
            if ((category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.arrayDefaultExpressionStickers.Contains(stickerId))
                || (category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.arrayDefaultHumanoidStickers.Contains(stickerId)))
            {
                image.UriSource = new Uri(string.Format(StickerHelper._stickerWVGAPath, category, stickerId), UriKind.Relative);
                if (isHighres)
                    App.newChatThreadPage.lruStickerCache.AddObject(category + "_" + stickerId, image);
                else
                    lruStickers.AddObject(category + "_" + stickerId, image);
                return;
            }

            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string fileName = STICKERS_DIR + "\\" + (isHighres ? HIGH_RESOLUTION_DIR : LOW_RESOLUTION_DIR) + "\\" + category + "\\" + stickerId;
                    if (store.FileExists(fileName))
                    {
                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                int imageBytesCount = reader.ReadInt32();
                                Byte[] imageBytes = reader.ReadBytes(imageBytesCount);
                                UI_Utils.Instance.createImageFromBytes(imageBytes, image);
                                if (isHighres)
                                    App.newChatThreadPage.lruStickerCache.AddObject(category + "_" + stickerId, image);
                                else
                                    lruStickers.AddObject(category + "_" + stickerId, image);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
            }
            return;
        }

        /// <summary>
        /// to check is sticker hidden from pallete
        /// </summary>
        /// <param name="category"></param>
        /// <param name="stickerId"></param>
        /// <returns></returns>
        public bool CheckLowResStickerExists(string category, string stickerId)
        {
            if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(category))
                return false;

            BitmapImage bmp = HikeViewModel.stickerHelper.lruStickers.GetObject(category + "_" + stickerId);
            if (bmp != null)
                return true;
            if ((category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.arrayDefaultExpressionStickers.Contains(stickerId))
                || (category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.arrayDefaultHumanoidStickers.Contains(stickerId)))
                return true;

            try
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string fileName = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category + "\\" + stickerId;
                    return store.FileExists(fileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerHelper::CheckLowResStickerExists, Exception:" + ex.Message);
            }
            return false;
        }

        public static void DeleteAllCategories()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string categoryFolder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR;
                        string[] folders = store.GetDirectoryNames(categoryFolder + "\\*");
                        if (folders != null)
                            foreach (string category in folders)
                            {
                                string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                                if (store.DirectoryExists(folder))
                                {
                                    string[] files = store.GetFileNames(folder + "\\*");
                                    if (files != null)
                                        foreach (string stickerId in files)
                                        {
                                            string fileName = folder + "\\" + stickerId;
                                            if (store.FileExists(fileName))
                                                store.DeleteFile(fileName);
                                        }
                                }
                            }
                        folders = store.GetDirectoryNames(categoryFolder + "\\*");
                        if (folders != null)
                        {
                            foreach (string category in folders)
                            {
                                string folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                                if (store.DirectoryExists(folder))
                                {
                                    string[] files = store.GetFileNames(folder + "\\*");
                                    if (files != null)
                                        foreach (string stickerId in files)
                                        {
                                            string fileName = folder + "\\" + stickerId;
                                            if (store.FileExists(fileName))
                                                store.DeleteFile(fileName);
                                        }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static BitmapImage GetHighResolutionSticker(StickerObj sticker)
        {
            if (sticker == null || string.IsNullOrEmpty(sticker.Id) || string.IsNullOrEmpty(sticker.Category))
                return null;

            if ((sticker.Category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.arrayDefaultExpressionStickers.Contains(sticker.Id))
                || (sticker.Category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.arrayDefaultHumanoidStickers.Contains(sticker.Id)))
            {
                return new BitmapImage(new Uri(string.Format(StickerHelper._stickerWVGAPath, sticker.Category, sticker.Id), UriKind.Relative));
            }

            try
            {
                lock (readWriteLock)
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string fileName = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + sticker.Category + "\\" + sticker.Id;
                        if (store.FileExists(fileName))
                        {
                            using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                            {
                                using (var reader = new BinaryReader(file))
                                {
                                    int imageBytesCount = reader.ReadInt32();
                                    sticker.StickerImageBytes = reader.ReadBytes(imageBytesCount);
                                    return UI_Utils.Instance.createImageFromBytes(sticker.StickerImageBytes);
                                }
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
            }
            return null;
        }

        public static void CreateCategory(string category)
        {
            lock (readWriteLock)
            {
                string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (!store.DirectoryExists(STICKERS_DIR))
                    {
                        store.CreateDirectory(STICKERS_DIR);
                    }
                    if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR))
                    {
                        store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR);
                    }
                    if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category))
                    {
                        store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category);
                    }
                }

            }
        }

        public static List<StickerCategory> ReadAllStickerCategories()
        {
            List<StickerCategory> listStickerCategory = new List<StickerCategory>();
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR;
                        string[] folders = store.GetDirectoryNames(folder + "\\*");
                        if (folders != null)
                            foreach (string category in folders)
                            {
                                StickerCategory stickerCategory = new StickerCategory(category);
                                string[] files1 = store.GetFileNames(folder + "\\" + category + "\\*");
                                IEnumerable<string> files = files1.OrderBy(x => x);
                                if (files != null)

                                    foreach (string stickerId in files)
                                    {
                                        string fileName = folder + "\\" + category + "\\" + stickerId;
                                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                                        {
                                            using (var reader = new BinaryReader(file))
                                            {
                                                try
                                                {
                                                    if (stickerId == METADATA)
                                                    {
                                                        stickerCategory.HasMoreStickers = reader.ReadBoolean();
                                                        stickerCategory.ShowDownloadMessage = reader.ReadBoolean();
                                                        stickerCategory.HasNewStickers = reader.ReadBoolean();
                                                    }
                                                    else
                                                    {
                                                        stickerCategory.ListStickers.Add(new StickerObj(category, stickerId, null, false));
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    Debug.WriteLine("Exception in reading sticker file,message:" + ex.Message);
                                                }
                                            }
                                        }
                                    }
                                listStickerCategory.Add(stickerCategory);
                            }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::CreateFromFile, Exception:" + ex.Message);
                }
                return listStickerCategory;
            }
        }

        public static void DeleteCategory(string category)
        {
            if (string.IsNullOrEmpty(category))
                return;
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string[] files = store.GetFileNames(folder + "\\*");
                            if (files != null)
                                foreach (string stickerId in files)
                                {
                                    string fileName = folder + "\\" + stickerId;
                                    if (store.FileExists(fileName))
                                        store.DeleteFile(fileName);
                                }
                            store.DeleteDirectory(folder);
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string[] files = store.GetFileNames(folder + "\\*");
                            if (files != null)
                                foreach (string stickerId in files)
                                {
                                    string fileName = folder + "\\" + stickerId;
                                    if (store.FileExists(fileName))
                                        store.DeleteFile(fileName);
                                }
                            store.DeleteDirectory(folder);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static void DeleteSticker(string category, string stickerId)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(stickerId))
                return;

            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);
            if (stickerCategory != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                   {
                       stickerCategory.ListStickers.Remove(new StickerObj(category, stickerId, null, false));
                   });
            }
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string fileName = folder + "\\" + stickerId;
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string fileName = folder + "\\" + stickerId;
                            if (store.FileExists(fileName))
                                store.DeleteFile(fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }

        public static void DeleteSticker(string category, List<string> listStickerIds)
        {
            if (string.IsNullOrEmpty(category) || listStickerIds == null || listStickerIds.Count == 0)
                return;
            StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);
            if (stickerCategory != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (string stickerId in listStickerIds)
                    {
                        stickerCategory.ListStickers.Remove(new StickerObj(category, stickerId, null, false));
                    }
                });
            }
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            foreach (string stickerId in listStickerIds)
                            {
                                string fileName = folder + "\\" + stickerId;
                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }
                        }

                        folder = STICKERS_DIR + "\\" + HIGH_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            foreach (string stickerId in listStickerIds)
                            {
                                string fileName = folder + "\\" + stickerId;
                                if (store.FileExists(fileName))
                                    store.DeleteFile(fileName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }


        /// <summary>
        /// Deletes low res stickers except list of sticker passed
        /// </summary>
        /// <param name="category"></param>
        /// <param name="listStickerIds">list stickers not to be deleted</param>
        public static void DeleteLowResCategory(string category, List<string> listStickerIds)
        {
            if (string.IsNullOrEmpty(category) || listStickerIds == null || listStickerIds.Count == 0)
                return;
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;

                        if (store.DirectoryExists(folder))
                        {
                            string[] files = store.GetFileNames(folder + "\\*");
                            if (files != null)
                                foreach (string stickerId in files)
                                {
                                    if (listStickerIds != null && listStickerIds.Contains(stickerId))
                                        continue;
                                    string fileName = folder + "\\" + stickerId;
                                    if (store.FileExists(fileName))
                                        store.DeleteFile(fileName);
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::DeleteCategory, Exception:" + ex.Message);
                }
            }
        }
        public static void UpdateHasMoreMessages(string category, bool hasMoreStickers, bool hasNewMessages)
        {
            if (string.IsNullOrEmpty(category))
                return;

            if (HikeViewModel.stickerHelper != null && HikeViewModel.stickerHelper.GetStickersByCategory(category) != null)
            {
                StickerCategory stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(category);
                stickerCategory.HasMoreStickers = hasMoreStickers;
                stickerCategory.HasNewStickers = hasNewMessages;
            }
            lock (readWriteLock)
            {
                try
                {
                    string folder = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category;
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {
                        if (!store.DirectoryExists(STICKERS_DIR))
                        {
                            store.CreateDirectory(STICKERS_DIR);
                        }
                        if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR))
                        {
                            store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR);
                        }
                        if (!store.DirectoryExists(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category))
                        {
                            store.CreateDirectory(STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category);
                        }
                        string metadataFile = folder + "\\" + METADATA;
                        using (var file = store.OpenFile(metadataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                        {
                            bool showDownloadMessage = true;
                            if (file.Length > 0)
                            {
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        reader.ReadBoolean();
                                        showDownloadMessage = reader.ReadBoolean();

                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(hasMoreStickers);
                                writer.Write(showDownloadMessage);
                                writer.Write(hasNewMessages);
                                writer.Flush();
                                writer.Close();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("StickerCategory::UpdateHasMoreMessages, Exception:" + ex.Message);
                }
            }
        }

    }
}

