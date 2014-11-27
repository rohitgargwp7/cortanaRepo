using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading;
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

        //regional stickers
        public const string CATEGORY_MUMBAI = "mumbai";
        public const string CATEGORY_DELHI = "delhi";
        public const string CATEGORY_GUJARAT = "gujarat";
        public const string CATEGORY_BANGALORE = "bangalore";
        public const string CATEGORY_HYDERABAD = "hyderabad";
        public const string CATEGORY_BHOPAL = "bhopal";
        public const string CATEGORY_CHENNAI = "chennai";
        public const string CATEGORY_KERALA = "kerala";
        public const string CATEGORY_KOLKATA = "kolkata";
        public const string CATEGORY_BIHAR = "bihar";
        public const string CATEGORY_GUWAHATI = "guwahati";

        //File constants
        public const string STICKERS_DIR = "stickers";
        public const string HIGH_RESOLUTION_DIR = "highres";
        public const string LOW_RESOLUTION_DIR = "lowres";
        public const string METADATA = "metadata";

        public const string _stickerWVGAPath = "/View/images/stickers/WVGA/{0}/{1}";
        private static object readWriteLock = new object();

        public LruCache<string, BitmapImage> lruStickers = new LruCache<string, BitmapImage>(20, 0);
        public RecentStickerHelper RecentStickerHelper;

        public static string[] ArrayDefaultHumanoidStickers = new string[]
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

        public static string[] ArrayDefaultExpressionStickers = new string[]
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
        public Dictionary<string, StickerCategory> DictStickersCategories
        {
            get
            {
                if (_dictStickersCategories == null)
                    _dictStickersCategories = new Dictionary<string, StickerCategory>();

                return _dictStickersCategories;
            }
        }

        //call from background
        public void InitialiseLowResStickers()
        {
            try
            {
                if (!_isInitialised)
                {
                    if (RecentStickerHelper == null)
                    {
                        RecentStickerHelper = new RecentStickerHelper();
                        RecentStickerHelper.LoadRecentStickers();
                    }

                    StickerCategory stickerCategoryRecent = new StickerCategory(CATEGORY_RECENT);
                    stickerCategoryRecent.HasMoreStickers = false;
                    stickerCategoryRecent.HasNewStickers = false;
                    stickerCategoryRecent.ShowDownloadMessage = false;
                    //stickerCategoryRecent.ListStickers = recentStickerHelper.listRecentStickers;

                    DictStickersCategories[CATEGORY_RECENT] = stickerCategoryRecent;

                    InitialiseDefaultStickers(CATEGORY_HUMANOID, ArrayDefaultHumanoidStickers);

                    InitialiseDefaultStickers(CATEGORY_EXPRESSIONS, ArrayDefaultExpressionStickers);

                    List<StickerCategory> listStickerCategories = ReadAllStickerCategories();
                    foreach (StickerCategory sc in listStickerCategories)
                    {
                        if (DictStickersCategories.ContainsKey(sc.Category))
                        {
                            StickerCategory stickerCategory = DictStickersCategories[sc.Category];
                            foreach (StickerObj sticker in sc.ListStickers)
                            {
                                stickerCategory.ListStickers.Add(sticker);
                            }
                            sc.ListStickers = stickerCategory.ListStickers;
                        } DictStickersCategories[sc.Category] = sc;
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
            if (DictStickersCategories.ContainsKey(category1Stickers.Category))
            {
                StickerCategory stickerCategory = DictStickersCategories[category1Stickers.Category];
                foreach (StickerObj sticker in category1Stickers.ListStickers)
                {
                    stickerCategory.ListStickers.Add(sticker);
                }
                DictStickersCategories[category1Stickers.Category] = stickerCategory;
            }
            else
                DictStickersCategories[category1Stickers.Category] = category1Stickers;

        }

        public StickerCategory GetStickersByCategory(string category)
        {
            if (String.IsNullOrEmpty(category))
                return null;

            if (DictStickersCategories.ContainsKey(category))
            {
                return DictStickersCategories[category];
            }

            return null;
        }

        public void CreateDefaultCategories()
        {
            CreateCategory(CATEGORY_HUMANOID);
            CreateCategory(CATEGORY_INDIANS);
            CreateCategory(CATEGORY_EXPRESSIONS);
            CreateCategory(CATEGORY_LOVE);
            CreateCategory(CATEGORY_BOLLYWOOD);
            CreateCategory(CATEGORY_TROLL);
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
            if ((category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.ArrayDefaultExpressionStickers.Contains(stickerId))
                || (category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.ArrayDefaultHumanoidStickers.Contains(stickerId)))
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

            BitmapImage bmp = HikeViewModel.StickerHelper.lruStickers.GetObject(category + "_" + stickerId);
            if (bmp != null)
                return true;
            if ((category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.ArrayDefaultExpressionStickers.Contains(stickerId))
                || (category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.ArrayDefaultHumanoidStickers.Contains(stickerId)))
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
                                    store.DeleteDirectory(folder);
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
                                    store.DeleteDirectory(folder);
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

            if ((sticker.Category == StickerHelper.CATEGORY_EXPRESSIONS && StickerHelper.ArrayDefaultExpressionStickers.Contains(sticker.Id))
                || (sticker.Category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.ArrayDefaultHumanoidStickers.Contains(sticker.Id)))
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

        public async void CreateCategory(string category, bool isVisible = true)
        {
            await Task.Run(() =>
                {
                    StickerCategory stickerCategory;
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
                            string metadataFile = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category + "\\" + METADATA;
                            stickerCategory = new StickerCategory(category);
                            if (store.FileExists(metadataFile))
                            {
                                using (var file = store.OpenFile(metadataFile, FileMode.Open, FileAccess.Read))
                                {
                                    using (var reader = new BinaryReader(file))
                                    {
                                        try
                                        {
                                            stickerCategory.Read(reader);
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.WriteLine("Exception in reading sticker file,message:" + ex.Message);
                                        }
                                    }
                                }
                            }
                            stickerCategory.IsVisbile = isVisible;
                            using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                            {
                                using (BinaryWriter writer = new BinaryWriter(file))
                                {
                                    stickerCategory.Write(writer);
                                    writer.Flush();
                                    writer.Close();
                                }
                            }
                            if (!DictStickersCategories.ContainsKey(category))
                                DictStickersCategories[category] = stickerCategory;
                        }
                    }
                });
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
                                                        stickerCategory.Read(reader);
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

        public static void UpdateRemovedCategory(string category)
        {
            StickerCategory stickerCategory;
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
                    string metadataFile = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category + "\\" + METADATA;
                    stickerCategory = new StickerCategory(category);
                    stickerCategory.IsRemoved = true;
                    using (var file = store.OpenFile(metadataFile, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                    {
                        using (BinaryWriter writer = new BinaryWriter(file))
                        {
                            stickerCategory.Write(writer);
                            writer.Flush();
                            writer.Close();
                        }
                    }
                }
            }
        }

        public static void DeleteSticker(string category, string stickerId)
        {
            if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(stickerId))
                return;

            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);

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

            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);

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
        /// <param name="category">Category to be deleted</param>
        /// <param name="listStickerIds">list stickers not to be deleted</param>
        public static void DeleteLowResCategory(string category, List<string> listStickerIds)
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
                            {
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

            if (HikeViewModel.StickerHelper.GetStickersByCategory(category) != null)
            {
                StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);
                stickerCategory.HasMoreStickers = hasMoreStickers;
                stickerCategory.HasNewStickers = hasNewMessages;
                stickerCategory.IsRemoved = false;
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
                            StickerCategory stickerCategory = new StickerCategory(category);
                            if (file.Length > 0)
                            {
                                using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                {
                                    try
                                    {
                                        stickerCategory.Read(reader);
                                    }
                                    catch
                                    {
                                    }
                                }
                            }
                            stickerCategory.HasMoreStickers = hasMoreStickers;
                            stickerCategory.HasNewStickers = hasNewMessages;
                            stickerCategory.IsRemoved = false;
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                stickerCategory.Write(writer);
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

        public async void UpdateVisibility(string category, bool isVisible)
        {
            if (string.IsNullOrEmpty(category))
                return;

            await Task.Run(() =>
                {
                    if (HikeViewModel.StickerHelper != null && HikeViewModel.StickerHelper.GetStickersByCategory(category) != null)
                    {
                        StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);
                        stickerCategory.IsVisbile = isVisible;
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
                                StickerCategory stickerCategory = new StickerCategory(category);
                                using (var file = store.OpenFile(metadataFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                                {
                                    if (file.Length > 0)
                                    {
                                        using (var reader = new BinaryReader(file, Encoding.UTF8, true))
                                        {
                                            try
                                            {
                                                stickerCategory.Read(reader);
                                            }
                                            catch
                                            {
                                            }
                                        }
                                    }
                                    stickerCategory.IsVisbile = isVisible;
                                    using (BinaryWriter writer = new BinaryWriter(file))
                                    {
                                        writer.Seek(0, SeekOrigin.Begin);
                                        stickerCategory.Write(writer);
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
                });
        }
        public static string GetOverLayColor(string _category)
        {
            switch (_category)
            {
                case StickerHelper.CATEGORY_HUMANOID:
                    return "#06a4e0";
                case StickerHelper.CATEGORY_EXPRESSIONS:
                    return "#fedc4d";
                case StickerHelper.CATEGORY_DOGGY:
                    return "#fedc4d";
                case StickerHelper.CATEGORY_KITTY:
                    return "#fedc4d";
                case StickerHelper.CATEGORY_BOLLYWOOD:
                    return "#f47a67";
                case StickerHelper.CATEGORY_TROLL:
                    return "#292824";
                case StickerHelper.CATEGORY_HUMANOID2:
                    return "#f8b0b7";
                case StickerHelper.CATEGORY_AVATARS:
                    return "#292824";
                case StickerHelper.CATEGORY_INDIANS:
                    return "#9ce5bb";
                case StickerHelper.CATEGORY_JELLY:
                    return "#663129";
                case StickerHelper.CATEGORY_SPORTS:
                    return "#adad18";
                case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                    return "#df3657";
                case StickerHelper.CATEGORY_LOVE:
                    return "#f988aa";
                case StickerHelper.CATEGORY_DELHI:
                    return "#8acda7";
                case StickerHelper.CATEGORY_MUMBAI:
                    return "#f87840";
                case StickerHelper.CATEGORY_GUJARAT:
                    return "#47211b";
                case StickerHelper.CATEGORY_BANGALORE:
                    return "#25aaa0";
                case StickerHelper.CATEGORY_HYDERABAD:
                    return "#abc533";
                case StickerHelper.CATEGORY_BHOPAL:
                    return "#ef7802";
                case StickerHelper.CATEGORY_CHENNAI:
                    return "#c9a083";
                case StickerHelper.CATEGORY_KERALA:
                    return "#e19c5b";
                case StickerHelper.CATEGORY_KOLKATA:
                    return "#06a4e0";
                case StickerHelper.CATEGORY_BIHAR:
                    return "#292824";
                case StickerHelper.CATEGORY_GUWAHATI:
                    return "#03b7a7";
            }
            return string.Empty;
        }

        public static string GetOverLayText(string _category)
        {
            switch (_category)
            {
                case StickerHelper.CATEGORY_HUMANOID:
                    return "We are Hikins";
                case StickerHelper.CATEGORY_EXPRESSIONS:
                    return "Express it all";
                case StickerHelper.CATEGORY_DOGGY:
                    return "Adorable Snuggles";
                case StickerHelper.CATEGORY_KITTY:
                    return "Meow, I'm Miley!";
                case StickerHelper.CATEGORY_BOLLYWOOD:
                    return "Bollywood Masala";
                case StickerHelper.CATEGORY_TROLL:
                    return "Rage Face";
                case StickerHelper.CATEGORY_HUMANOID2:
                    return "You & I";
                case StickerHelper.CATEGORY_AVATARS:
                    return "Superhero Avatars";
                case StickerHelper.CATEGORY_INDIANS:
                    return "Things Indians Say";
                case StickerHelper.CATEGORY_JELLY:
                    return "Wicked Jellies";
                case StickerHelper.CATEGORY_SPORTS:
                    return "Sports Maniacs";
                case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                    return "Wacky Smileys";
                case StickerHelper.CATEGORY_LOVE:
                    return "Love You Forever";
                case StickerHelper.CATEGORY_DELHI:
                    return "Saddi Dilli";
                case StickerHelper.CATEGORY_MUMBAI:
                    return "Aamchi Mumbai";
                case StickerHelper.CATEGORY_GUJARAT:
                    return "Aapno Gujarat";
                case StickerHelper.CATEGORY_BANGALORE:
                    return "Namma Kannada";
                case StickerHelper.CATEGORY_HYDERABAD:
                    return "Telugu Tamasha";
                case StickerHelper.CATEGORY_BHOPAL:
                    return "Apna MP";
                case StickerHelper.CATEGORY_CHENNAI:
                    return "Sooper Tamilian";
                case StickerHelper.CATEGORY_KERALA:
                    return "Ende Keralam";
                case StickerHelper.CATEGORY_KOLKATA:
                    return "Bong Connection";
                case StickerHelper.CATEGORY_BIHAR:
                    return "Hamaar Bihar";
                case StickerHelper.CATEGORY_GUWAHATI:
                    return "Ami Axomiya";
            }
            return string.Empty;
        }

        public static void GetStickerCategoryPreference()
        {
            JArray array = new JArray();
            array.Add(StickerHelper.CATEGORY_DELHI);
            array.Add(StickerHelper.CATEGORY_MUMBAI);
            array.Add(StickerHelper.CATEGORY_GUJARAT);
            array.Add(StickerHelper.CATEGORY_BANGALORE);
            array.Add(StickerHelper.CATEGORY_HYDERABAD);
            array.Add(StickerHelper.CATEGORY_BHOPAL);
            array.Add(StickerHelper.CATEGORY_CHENNAI);
            array.Add(StickerHelper.CATEGORY_KERALA);
            array.Add(StickerHelper.CATEGORY_KOLKATA);
            array.Add(StickerHelper.CATEGORY_BIHAR);
            array.Add(StickerHelper.CATEGORY_GUWAHATI);

            JObject obj = new JObject();
            obj.Add(HikeConstants.Stickers.CATEGORY_IDS_COLLECTION, array);
            AccountUtils.GetStickerCategoryData(obj, new AccountUtils.postResponseFunction(StickerCategoryCallBack));
        }

        public static void StickerCategoryCallBack(JObject json)
        {
            try
            {
                if (json != null && HikeConstants.OK == (string)json[HikeConstants.STAT])
                {
                    JArray jarray = (JArray)json[HikeConstants.Stickers.DATA];
                    List<string> listCategories = new List<string>();
                    for (int i = 0; i < jarray.Count; i++)
                    {
                        JObject categoryJobj = (JObject)jarray[i];
                        JToken jtoken;
                        if (categoryJobj.TryGetValue(HikeConstants.Stickers.VISIBILITY, out jtoken) && (int)jtoken == 1)
                        {
                            string category = (string)categoryJobj[HikeConstants.Stickers.CATEGORY_ID];
                            HikeViewModel.StickerHelper.CreateCategory(category);
                            listCategories.Add(category);
                        }
                    }
                    App.WriteToIsoStorageSettings(HikeConstants.AppSettings.PREFERRED_STICKER_CATEGORY, listCategories.Count > 0 ? listCategories : null);
                    if (App.newChatThreadPage != null)
                        App.newChatThreadPage.UpdateCategoryOrder(GetStickerCategoryOrder());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("StickerHelper::StickerCategoryCallBack,ExMessage:{0},ExStackTrace:{1}", ex.Message, ex.StackTrace);
            }
        }

        public static List<string> GetStickerCategoryOrder()
        {
            List<string> listCategories = new List<string>();
            StickerCategory stickerCategory;
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_RECENT)) != null)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_HUMANOID)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            List<string> listRegionalCategory;
            if (App.appSettings.TryGetValue(HikeConstants.AppSettings.PREFERRED_STICKER_CATEGORY, out listRegionalCategory) && listRegionalCategory != null)
            {
                foreach (string category in listRegionalCategory)
                {
                    if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                    {
                        listCategories.Add(stickerCategory.Category);
                    }
                }
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_INDIANS)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_EXPRESSIONS)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_LOVE)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_BOLLYWOOD)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_TROLL)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_MUMBAI))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_MUMBAI);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_DELHI))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_DELHI);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_GUJARAT))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_GUJARAT);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }

            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_HYDERABAD))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_HYDERABAD);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }

            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_CHENNAI))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_CHENNAI);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BIHAR))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_BIHAR);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BANGALORE))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_BANGALORE);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }

            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_KOLKATA))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_KOLKATA);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BHOPAL))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_BHOPAL);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            } if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_KERALA))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_KERALA);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_GUWAHATI))
            {
                stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(CATEGORY_GUWAHATI);
                if (stickerCategory != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
                {
                    listCategories.Add(stickerCategory.Category);
                }
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_DOGGY)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }

            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_SPORTS)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_HUMANOID2)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_AVATARS)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_SMILEY_EXPRESSIONS)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_KITTY)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }
            if ((stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_JELLY)) != null && stickerCategory.IsVisbile && !stickerCategory.IsRemoved)
            {
                listCategories.Add(stickerCategory.Category);
            }

            return listCategories;
        }

        public static ObservableCollection<StickerCategory> GetAllStickerGategories()
        {
            ObservableCollection<StickerCategory> listCategories = new ObservableCollection<StickerCategory>();
            AddCategory(listCategories, CATEGORY_HUMANOID);
            List<string> listRegionalCategory;
            if (App.appSettings.TryGetValue(HikeConstants.AppSettings.PREFERRED_STICKER_CATEGORY, out listRegionalCategory) && listRegionalCategory != null)
            {
                foreach (string category in listRegionalCategory)
                {
                    AddCategory(listCategories, category);
                }
            }
            AddCategory(listCategories, CATEGORY_INDIANS);
            AddCategory(listCategories, CATEGORY_EXPRESSIONS);
            AddCategory(listCategories, CATEGORY_LOVE);
            AddCategory(listCategories, CATEGORY_BOLLYWOOD);
            AddCategory(listCategories, CATEGORY_TROLL);


            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_MUMBAI))
            {
                AddCategory(listCategories, CATEGORY_MUMBAI);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_DELHI))
            {
                AddCategory(listCategories, CATEGORY_DELHI);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_GUJARAT))
            {
                AddCategory(listCategories, CATEGORY_GUJARAT);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_HYDERABAD))
            {
                AddCategory(listCategories, CATEGORY_HYDERABAD);
            }

            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_CHENNAI))
            {
                AddCategory(listCategories, CATEGORY_CHENNAI);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BIHAR))
            {
                AddCategory(listCategories, CATEGORY_BIHAR);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BANGALORE))
            {
                AddCategory(listCategories, CATEGORY_BANGALORE);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_KOLKATA))
            {
                AddCategory(listCategories, CATEGORY_KOLKATA);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_BHOPAL))
            {
                AddCategory(listCategories, CATEGORY_BHOPAL);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_KERALA))
            {
                AddCategory(listCategories, CATEGORY_KERALA);
            }
            if (listRegionalCategory == null || !listRegionalCategory.Contains(CATEGORY_GUWAHATI))
            {
                AddCategory(listCategories, CATEGORY_GUWAHATI);
            }
            AddCategory(listCategories, CATEGORY_DOGGY);
            AddCategory(listCategories, CATEGORY_SPORTS);
            AddCategory(listCategories, CATEGORY_HUMANOID2);
            AddCategory(listCategories, CATEGORY_AVATARS);
            AddCategory(listCategories, CATEGORY_SMILEY_EXPRESSIONS);
            AddCategory(listCategories, CATEGORY_KITTY);
            AddCategory(listCategories, CATEGORY_JELLY);

            return listCategories;
        }

        private static void AddCategory(ObservableCollection<StickerCategory> listCategories, string category)
        {
            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);
            if (stickerCategory == null || !stickerCategory.IsRemoved)
            {
                if (stickerCategory == null)
                {
                    stickerCategory = new StickerCategory(category);
                }
                listCategories.Add(stickerCategory);
            }
        }

        public static int GetStickersCount(string category)
        {
            switch (category)
            {
                case StickerHelper.CATEGORY_HUMANOID:
                    return 49;
                case StickerHelper.CATEGORY_EXPRESSIONS:
                    return 59;
                case StickerHelper.CATEGORY_DOGGY:
                    return 30;
                case StickerHelper.CATEGORY_KITTY:
                    return 17;
                case StickerHelper.CATEGORY_BOLLYWOOD:
                    return 50;
                case StickerHelper.CATEGORY_TROLL:
                    return 50;
                case StickerHelper.CATEGORY_HUMANOID2:
                    return 25;
                case StickerHelper.CATEGORY_AVATARS:
                    return 29;
                case StickerHelper.CATEGORY_INDIANS:
                    return 56;
                case StickerHelper.CATEGORY_JELLY:
                    return 12;
                case StickerHelper.CATEGORY_SPORTS:
                    return 29;
                case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                    return 26;
                case StickerHelper.CATEGORY_LOVE:
                    return 30;
                case StickerHelper.CATEGORY_DELHI:
                    return 12;
                case StickerHelper.CATEGORY_MUMBAI:
                    return 12;
                case StickerHelper.CATEGORY_GUJARAT:
                    return 12;
                case StickerHelper.CATEGORY_BANGALORE:
                    return 12;
                case StickerHelper.CATEGORY_HYDERABAD:
                    return 12;
                case StickerHelper.CATEGORY_BHOPAL:
                    return 12;
                case StickerHelper.CATEGORY_CHENNAI:
                    return 12;
                case StickerHelper.CATEGORY_KERALA:
                    return 12;
                case StickerHelper.CATEGORY_KOLKATA:
                    return 12;
                case StickerHelper.CATEGORY_BIHAR:
                    return 12;
                case StickerHelper.CATEGORY_GUWAHATI:
                    return 12;
            }
            return 0;

        }
    }
}

