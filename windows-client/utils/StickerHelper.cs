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
using windows_client.Misc;
using windows_client.ViewModel;

namespace windows_client.utils
{

    public class StickerHelper
    {
        public const string CATEGORY_RECENT = "recent";
        public const string CATEGORY_HUMANOID = "humanoid";
        public const string CATEGORY_DOGGY = "doggy";
        public const string CATEGORY_KITTY = "kitty";
        public const string CATEGORY_EXPRESSIONS = "expressions";
        public const string CATEGORY_BOLLYWOOD = "bollywood";
        public const string CATEGORY_TROLL = "rageface";

        public const string _stickerWVGAPath = "/View/images/stickers/WVGA/{0}/{1}";
        public const string _sticker720path = "/View/images/stickers/720p/{0}/{1}";
        public const string _stickerWXGApath = "/View/images/stickers/WXGA/{0}/{1}";

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

        public static string[] arrayDefaultDoggyStickers = new string[]
        {
            "001_hi.png",
            "002_thumbsup.png",
            "003_drooling.png",
            "004_devilsmile.png",
            "005_sorry.png",
            "006_urgh.png",
            "007_confused.png",
            "008_dreaming.png"
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

                    InitialiseDefaultStickers(CATEGORY_DOGGY, arrayDefaultDoggyStickers);

                    List<StickerCategory> listStickerCategories = StickerCategory.ReadAllStickerCategories();
                    foreach (StickerCategory sc in listStickerCategories)
                    {
                        if (_dictStickersCategories.ContainsKey(sc.Category))
                        {
                            StickerCategory stickerCategory = _dictStickersCategories[sc.Category];
                            foreach (Sticker sticker in sc.ListStickers)
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
                Sticker sticker = new Sticker(category1Stickers.Category, arrayDefaultStickers[i], null, false);
                category1Stickers.ListStickers.Add(sticker);
            }
            if (_dictStickersCategories.ContainsKey(category1Stickers.Category))
            {
                StickerCategory stickerCategory = _dictStickersCategories[category1Stickers.Category];
                foreach (Sticker sticker in category1Stickers.ListStickers)
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
            StickerCategory.CreateCategory(CATEGORY_HUMANOID);
            StickerCategory.CreateCategory(CATEGORY_DOGGY);
            StickerCategory.CreateCategory(CATEGORY_KITTY);
            StickerCategory.CreateCategory(CATEGORY_EXPRESSIONS);
            StickerCategory.CreateCategory(CATEGORY_BOLLYWOOD);
            StickerCategory.CreateCategory(CATEGORY_TROLL);
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
            if ((category == StickerHelper.CATEGORY_DOGGY && StickerHelper.arrayDefaultDoggyStickers.Contains(stickerId))
                || (category == StickerHelper.CATEGORY_HUMANOID && StickerHelper.arrayDefaultHumanoidStickers.Contains(stickerId)))
            {
                string url;
                if (Utils.CurrentResolution == Utils.Resolutions.WXGA)
                    url = _stickerWXGApath;
                else if (Utils.CurrentResolution == Utils.Resolutions.WVGA)
                    url = StickerHelper._stickerWVGAPath;
                else
                    url = StickerHelper._sticker720path;
                image.UriSource = new Uri(string.Format(url, category, stickerId), UriKind.Relative);
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
                    string fileName = StickerCategory.STICKERS_DIR + "\\" + (isHighres ? StickerCategory.HIGH_RESOLUTION_DIR : StickerCategory.LOW_RESOLUTION_DIR) + "\\" + category + "\\" + stickerId;
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
    }

    public class Sticker : IBinarySerializable
    {
        private string _id;
        private string _category;
        private bool _isHighRes;
        private byte[] _stickerImageBytes;
        public Sticker(string category, string id, byte[] stickerImageBytes, bool isHighRes)
        {
            this._category = category;
            this._id = id;
            this._stickerImageBytes = stickerImageBytes;
            this._isHighRes = isHighRes;
        }


        public string Id
        {
            get
            {
                return _id;
            }
        }

        public string Category
        {
            get
            {
                return _category;
            }
        }
        public bool IsStickerDownloaded
        {
            get;
            set;
        }

        public byte[] StickerImageBytes
        {
            get
            {
                return _stickerImageBytes;
            }
            set
            {
                _stickerImageBytes = value;
            }
        }

        public BitmapImage StickerImage
        {
            get
            {
                if (_isHighRes)
                {
                    BitmapImage _stickerImage = App.newChatThreadPage.lruStickerCache.GetObject(_category + "_" + Id);
                    if (_stickerImage == null)
                    {
                        _stickerImage = new BitmapImage();
                        HikeViewModel.stickerHelper.GetSticker(_stickerImage, _category, _id, _stickerImageBytes, true);
                    }

                    return _stickerImage;
                }
                else
                {
                    BitmapImage _stickerImage = HikeViewModel.stickerHelper.lruStickers.GetObject(_category + "_" + Id);
                    if (_stickerImage == null)
                    {
                        _stickerImage = new BitmapImage();
                        HikeViewModel.stickerHelper.GetSticker(_stickerImage, _category, _id, _stickerImageBytes, false);
                    }

                    return _stickerImage;
                }
            }
        }

        public void Write(BinaryWriter writer)
        {
        }
        public void Read(BinaryReader reader)
        {
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Sticker))
                return false;

            Sticker compareTo = obj as Sticker;
            if (_id == compareTo._id && _category == compareTo._category && _isHighRes == compareTo._isHighRes)
                return true;
            return base.Equals(obj);
        }
    }

    public class RecentStickerHelper
    {
        public const string RECENTS_FILE = "recents";
        private const int maxStickersCount = 30;
        private static object readWriteLock = new object();
        public List<Sticker> listRecentStickers;
        public RecentStickerHelper()
        {
            listRecentStickers = new List<Sticker>();
        }

        public void LoadSticker()
        {
            lock (readWriteLock)
            {
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                {
                    string fileName = StickerCategory.STICKERS_DIR + "\\" + RECENTS_FILE;
                    if (store.FileExists(fileName))
                    {
                        using (var file = store.OpenFile(fileName, FileMode.Open, FileAccess.Read))
                        {
                            using (var reader = new BinaryReader(file))
                            {
                                int total = reader.ReadInt32();
                                for (int i = 0; i < total; i++)
                                {
                                    int count = reader.ReadInt32();
                                    string stickerId = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    count = reader.ReadInt32();
                                    string category = Encoding.UTF8.GetString(reader.ReadBytes(count), 0, count);
                                    listRecentStickers.Add(new Sticker(category, stickerId, null, false));
                                }

                            }
                        }
                    }

                }
            }
        }

        public void AddSticker(Sticker currentSticker)
        {
            if (currentSticker == null)
                return;
            lock (readWriteLock)
            {
                if (!listRecentStickers.Remove(currentSticker))
                    ShrinkToSize();
                listRecentStickers.Insert(0, currentSticker);
                UpdateRecentsFile();
            }
        }

        public async Task UpdateRecentsFile()
        {
            try
            {
                await Task.Delay(1);

                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) 
                {
                    if (!store.DirectoryExists(StickerCategory.STICKERS_DIR))
                    {
                        store.CreateDirectory(StickerCategory.STICKERS_DIR);
                    }
                    string fileName = StickerCategory.STICKERS_DIR + "\\" + RECENTS_FILE;

                    try
                    {
                        if (store.FileExists(fileName))
                            store.DeleteFile(fileName);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile : DeletingFile , Exception : " + ex.StackTrace);
                    }
                    try
                    {
                        using (var file = store.OpenFile(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                        {
                            using (BinaryWriter writer = new BinaryWriter(file))
                            {
                                writer.Seek(0, SeekOrigin.Begin);
                                writer.Write(listRecentStickers.Count);
                                foreach (Sticker sticker in listRecentStickers)
                                {
                                    writer.WriteStringBytes(sticker.Id);
                                    writer.WriteStringBytes(sticker.Category);
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
                        Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile : WritingFile , Exception : " + ex.StackTrace);
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("RecentStickerHelper :: UpdateRecentsFile , Exception : " + ex.StackTrace);
            }
        }
        private void ShrinkToSize()
        {
            if (this.listRecentStickers.Count > (maxStickersCount - 1))
            {
                listRecentStickers.RemoveAt(listRecentStickers.Count - 1);
            }
        }


        public static void DeleteSticker(string categoryId, List<string> listStickers)
        {
            RecentStickerHelper recentSticker;
            if (HikeViewModel.stickerHelper == null || HikeViewModel.stickerHelper.recentStickerHelper == null)
            {
                recentSticker = new RecentStickerHelper();
                recentSticker.LoadSticker();
            }
            else
                recentSticker = HikeViewModel.stickerHelper.recentStickerHelper;
            bool isStickerInRecent = false;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    foreach (string stickerId in listStickers)
                    {
                        if (recentSticker.listRecentStickers.Remove(new Sticker(categoryId, stickerId, null, false)))
                            isStickerInRecent = true;
                    }
                    if (isStickerInRecent)
                        recentSticker.UpdateRecentsFile();
                });
        }

        public static void DeleteCategory(string category)
        {
            RecentStickerHelper recentSticker;
            if (HikeViewModel.stickerHelper == null || HikeViewModel.stickerHelper.recentStickerHelper == null)
            {
                recentSticker = new RecentStickerHelper();
                recentSticker.LoadSticker();
            }
            else
                recentSticker = HikeViewModel.stickerHelper.recentStickerHelper;
            bool isStickerInRecent = false;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    for (int i = recentSticker.listRecentStickers.Count - 1; i >= 0; i--)
                    {
                        if (recentSticker.listRecentStickers[i].Category == category)
                        {
                            recentSticker.listRecentStickers.RemoveAt(i);
                            isStickerInRecent = true;
                        }
                    }
                    if (isStickerInRecent)
                        recentSticker.UpdateRecentsFile();
                });

        }

        public static void DeleteRecents()
        {
            lock (readWriteLock)
            {
                try
                {
                    using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication())
                    {

                        try
                        {
                            string filename = StickerCategory.STICKERS_DIR + "\\" + RECENTS_FILE;

                            if (store.FileExists(filename))
                                store.DeleteFile(filename);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("RecentStickerHelper :: DeleteRecents :Delete FIle: Exception :{0} , StackTrace:{1} ", ex.Message, ex.StackTrace);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("RecentStickerHelper :: DeleteRecents , Exception :{0} , StackTrace:{1} ", ex.Message, ex.StackTrace);
                }
            }
        }

    }
}

