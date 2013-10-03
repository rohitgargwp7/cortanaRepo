using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using windows_client.ViewModel;

namespace windows_client.utils
{

    public class StickerHelper
    {
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
                    _dictStickersCategories = new Dictionary<string, StickerCategory>();

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
                        }
                        _dictStickersCategories[sc.Category] = sc;
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
        public void GetSticker(ref BitmapImage image, string category, string stickerId, byte[] stickerImageBytes, bool isHighres)
        {
            if (string.IsNullOrEmpty(stickerId) || string.IsNullOrEmpty(category))
                return;
            if (stickerImageBytes != null && stickerImageBytes.Length > 0)
            {
                image = UI_Utils.Instance.createImageFromBytes(stickerImageBytes);
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
                using (IsolatedStorageFile store = IsolatedStorageFile.GetUserStoreForApplication()) // grab the storage
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
                                image = UI_Utils.Instance.createImageFromBytes(imageBytes);
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

    public class Sticker
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
                        HikeViewModel.stickerHelper.GetSticker(ref _stickerImage, _category, _id, _stickerImageBytes, true);
                    }

                    return _stickerImage;
                }
                else
                {
                    BitmapImage _stickerImage = HikeViewModel.stickerHelper.lruStickers.GetObject(_category + "_" + Id);
                    if (_stickerImage == null)
                    {
                        _stickerImage = new BitmapImage();
                        HikeViewModel.stickerHelper.GetSticker(ref _stickerImage, _category, _id, _stickerImageBytes, false);
                    }

                    return _stickerImage;
                }
            }
        }
    }

}

