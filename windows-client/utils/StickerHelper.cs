using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

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
                            foreach (Sticker sticker in stickerCategory.ListStickers)
                            {
                                sc.ListStickers.Add(sticker);
                            }
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
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                for (int i = 0; i < arrayDefaultStickers.Length; i++)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                    string url;
                    if (Utils.CurrentResolution == Utils.Resolutions.WXGA)
                        url = _stickerWXGApath;
                    else if (Utils.CurrentResolution == Utils.Resolutions.WVGA)
                        url = _stickerWVGAPath;
                    else
                        url = _sticker720path;

                    bitmap.UriSource = new Uri(string.Format(url, category, arrayDefaultStickers[i]), UriKind.Relative);
                    Sticker sticker = new Sticker(category1Stickers.Category, arrayDefaultStickers[i], bitmap);
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

            });
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
    }

    public class Sticker
    {
        private string _id;
        private string _category;
        private BitmapImage _stickerImage;

        public Sticker(string category, string id, BitmapImage stickerImage)
        {
            this._category = category;
            this._id = id;
            this._stickerImage = stickerImage;
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

        public BitmapImage StickerImage
        {
            get
            {
                return _stickerImage;
            }
            set
            {
                _stickerImage = value;
            }
        }
    }

}

