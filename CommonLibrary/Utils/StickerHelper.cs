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
using CommonLibrary.Misc;
using CommonLibrary.Model.Sticker;
using CommonLibrary.ViewModel;
using CommonLibrary.Lib;

namespace CommonLibrary.utils
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
                    string metadataFile = STICKERS_DIR + "\\" + LOW_RESOLUTION_DIR + "\\" + category + "\\" + METADATA;
                    StickerCategory stickerCategory = new StickerCategory(category);
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
                    stickerCategory.OverlayText = GetOverLayText(category);
                    stickerCategory.OverlayBackgroundColorString = GetOverLayColor(category);
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

            StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);

            if (stickerCategory != null)
                stickerCategory.ListStickers.Remove(new StickerObj(category, stickerId, null, false));

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
                foreach (string stickerId in listStickerIds)
                    stickerCategory.ListStickers.Remove(new StickerObj(category, stickerId, null, false));
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

            if (HikeViewModel.StickerHelper != null && HikeViewModel.StickerHelper.GetStickersByCategory(category) != null)
            {
                StickerCategory stickerCategory = HikeViewModel.StickerHelper.GetStickersByCategory(category);
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

        static string GetOverLayColor(string _category)
        {
            switch (_category)
            {
                case StickerHelper.CATEGORY_HUMANOID:
                    return "#008bd3";
                case StickerHelper.CATEGORY_EXPRESSIONS:
                    return "#00a470";
                case StickerHelper.CATEGORY_DOGGY:
                    return "#9d5c2c";
                case StickerHelper.CATEGORY_KITTY:
                    return "#267be1";
                case StickerHelper.CATEGORY_BOLLYWOOD:
                    return "#d59022";
                case StickerHelper.CATEGORY_TROLL:
                    return "#349d26";
                case StickerHelper.CATEGORY_HUMANOID2:
                    return "#c63070";
                case StickerHelper.CATEGORY_AVATARS:
                    return "#b9181d";
                case StickerHelper.CATEGORY_INDIANS:
                    return "#6238b7";
                case StickerHelper.CATEGORY_JELLY:
                    return "#663129";
                case StickerHelper.CATEGORY_SPORTS:
                    return "#a77a11";
                case StickerHelper.CATEGORY_SMILEY_EXPRESSIONS:
                    return "#3a2533";
                case StickerHelper.CATEGORY_LOVE:
                    return "#d83a59";
            }
            return string.Empty;
        }

        static string GetOverLayText(string _category)
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
            }
            return string.Empty;
        }
    }
}

