using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace windows_client.utils
{
    public class StickerHelper
    {
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
        private Dictionary<string, Sticker[]> dictStickers;
        public Dictionary<string, BitmapImage> dictStickerImages;


        public void InitialiseStickers()
        {
            if (!_isInitialised)
            {
                dictStickers = new Dictionary<string, Sticker[]>();
                dictStickerImages = new Dictionary<string, BitmapImage>();
                Sticker[] category1Stickers = new Sticker[stickers.Length];
                for (int i = 0; i < stickers.Length; i++)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                    bitmap.UriSource = new Uri(stickers[i], UriKind.Relative);
                    Sticker s1 = new Sticker("category1", i + 1, bitmap);
                    category1Stickers[i] = s1;
                    dictStickerImages[s1.StickerId] = bitmap;
                }
                dictStickers["category1"] = category1Stickers;
                //read stickers from file too
                _isInitialised = true;
            }
        }


        public Sticker[] GetStickersByCategory(string category)
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
            this._stickerId = string.Format("{0}{1}", category, id);
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
