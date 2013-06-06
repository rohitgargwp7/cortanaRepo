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
        public const string CATEGORY_1 = "kitty";
        public const string CATEGORY_2 = "expressions";
        public const string CATEGORY_3 = "rageFaces";
        public const string CATEGORY_4 = "doggy";
        public const string CATEGORY_5 = "bollywood";

        public const string _stickerWVGAPath = "/View/images/stickers/WVGA/{0}";
        public const string _sticker720path = "/View/images/stickers/720p/{0}";
        public const string _stickerWXGApath = "/View/images/stickers/WXGA/{0}";
        private string[] stickers = new string[]
        {
        "1_awww.png",
        "2_talktohand.png",
        "3_wink.png",
        "4_hugs.png",
        "5_woohoo.png",
        "6_sshh.png",
        "7_pheww.png",
        "8_crying.png"
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
                    StickerCategory category1Stickers = new StickerCategory(CATEGORY_1, false);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            for (int i = 0; i < stickers.Length; i++)
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

                                bitmap.UriSource = new Uri(string.Format(url, stickers[i]), UriKind.Relative);
                                Sticker sticker = new Sticker(category1Stickers.Category, stickers[i], bitmap);
                                category1Stickers.ListStickers.Add(sticker);
                            }
                            _dictStickersCategories[category1Stickers.Category] = category1Stickers;
                        });
                    List<StickerCategory> listStickerCategories = StickerCategory.ReadAllStickerCategories();
                    foreach (StickerCategory sc in listStickerCategories)
                    {
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
            StickerCategory.CreateCategory(CATEGORY_2);
            StickerCategory.CreateCategory(CATEGORY_3);
            StickerCategory.CreateCategory(CATEGORY_4);
            StickerCategory.CreateCategory(CATEGORY_5);
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

