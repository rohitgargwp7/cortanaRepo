using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.utils
{

    public class StickerHelper
    {
        public const string CATEGORY_1 = "category1";
        public const string CATEGORY_2 = "Expressions";
        public const string CATEGORY_3 = "Expressions";
        public const string CATEGORY_4 = "Expressions";
        public const string CATEGORY_5 = "Expressions";
        private string[] stickers = new string[]
        {
            "/View/images/stickers/BRB.png", 
            "/View/images/stickers/Callme.png",
            "/View/images/stickers/Gift-box.png",
            "/View/images/stickers/GM.png",
            "/View/images/stickers/GTG.png",
            "/View/images/stickers/Kitty.png",
            "/View/images/stickers/Kitty1.png",
            "/View/images/stickers/Kitty2.png",
            "/View/images/stickers/Kitty4.png",
            "/View/images/stickers/LMAO.png",
            "/View/images/stickers/LOL.png",
            "/View/images/stickers/OMG.png",
            "/View/images/stickers/yum.png"
        };
        private bool _isInitialised;
        private Dictionary<string, StickerCategory> dictStickers;
        public Dictionary<string, BitmapImage> dictStickerImages;


        public void InitialiseStickers()
        {
            try
            {
                if (!_isInitialised)
                {
                    dictStickers = new Dictionary<string, StickerCategory>();
                    dictStickerImages = new Dictionary<string, BitmapImage>();
                    StickerCategory category1Stickers = new StickerCategory(CATEGORY_1, false);
                    for (int i = 0; i < stickers.Length; i++)
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                        bitmap.UriSource = new Uri(stickers[i], UriKind.Relative);
                        Sticker sticker = new Sticker("category1", i + 1, bitmap);
                        category1Stickers.dictStickers[sticker.Id] = sticker;
                        dictStickerImages[sticker.StickerId] = bitmap;
                    }
                    dictStickers[category1Stickers.Category] = category1Stickers;

                    StickerCategory category2Stickers = new StickerCategory(CATEGORY_2);
                    category2Stickers.CreateFromFile();
                    foreach (int id in category2Stickers.dictStickers.Keys)
                    {
                        Sticker sticker = category2Stickers.dictStickers[id];
                        dictStickerImages[sticker.StickerId] = sticker.StickerImage;
                    }
                    dictStickers[category2Stickers.Category] = category2Stickers;

                    //do same for all categories
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
            if (!_isInitialised)
            {
                InitialiseStickers();
            }
            if (dictStickers.ContainsKey(category))
            {
                return dictStickers[category];
            }

            return null;
        }

        public BitmapImage GetStickerImageById(string stickerId)
        {
            BitmapImage stickerImage;
            if (_isInitialised)
            {
                InitialiseStickers();
            }
            if (!string.IsNullOrEmpty(stickerId) && dictStickerImages.TryGetValue(stickerId, out stickerImage))
            {
                return stickerImage;
            }
            return null;
        }
    }

    public class Sticker
    {
        private int _id;
        private string _category;
        private string _stickerId;
        private BitmapImage _stickerImage;

        public Sticker(string category, int id, BitmapImage stickerImage)
        {
            this._category = category;
            this._id = id;
            this._stickerId = string.Format("{0}_{1}", category, id);
            this._stickerImage = stickerImage;
        }

        public string StickerId
        {
            get
            {
                return _stickerId;
            }
        }

        public int Id
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
        }
    }

}
