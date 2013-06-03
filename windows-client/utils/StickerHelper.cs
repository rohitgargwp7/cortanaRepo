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
        public static readonly string CATEGORY_1 = "category1";
        public static readonly string CATEGORY_2 = "Expressions";
        public static readonly string CATEGORY_3 = "Expressions";
        public static readonly string CATEGORY_4 = "Expressions";
        public static readonly string CATEGORY_5 = "Expressions";
        private static readonly int cate1Stickers = 13;
        private string[,] stickers = new string[,]
        {
        {"100.png",                 "/View/images/stickers/100.png"     }        ,
        {"200.png",              "/View/images/stickers/200.png"  }        ,
        {"xoxo.png",            "/View/images/stickers/xoxo.png"   }     ,
        {"GM.png",                   "/View/images/stickers/GM.png"      }       ,
        {"GTG.png",                "/View/images/stickers/GTG.png"       }       ,
        {"Kitty.png",              "/View/images/stickers/Kitty.png"      }      ,
        {"Kitty1.png",             "/View/images/stickers/Kitty1.png"     }      ,
        {"Kitty2.png",             "/View/images/stickers/Kitty2.png"      }     ,
        {"Kitty4.png",             "/View/images/stickers/Kitty4.png"      }     ,
        {"LMAO.png",               "/View/images/stickers/LMAO.png"       }      ,
        {"LOL.png",               "/View/images/stickers/LOL.png"         }      ,
        {"OMG.png",                 "/View/images/stickers/OMG.png"        }     ,
        {"yum.png",                  "/View/images/stickers/yum.png"         }    
        };

        private bool _isInitialised;
        private Dictionary<string, StickerCategory> dictStickers;

        //call from background
        public void InitialiseLowResStickers()
        {
            try
            {
                if (!_isInitialised)
                {
                    dictStickers = new Dictionary<string, StickerCategory>();
                    StickerCategory category1Stickers = new StickerCategory(CATEGORY_1, false);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            for (int i = 0; i < cate1Stickers; i++)
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.CreateOptions = BitmapCreateOptions.BackgroundCreation;
                                bitmap.UriSource = new Uri(stickers[i, 1], UriKind.Relative);
                                Sticker sticker = new Sticker(category1Stickers.Category, stickers[i, 0], bitmap);
                                category1Stickers.ListStickers.Add(sticker);
                            }
                            dictStickers[category1Stickers.Category] = category1Stickers;
                        });
                    StickerCategory category2Stickers = new StickerCategory(CATEGORY_2);
                    category2Stickers.CreateFromFile();
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

            if (dictStickers.ContainsKey(category))
            {
                return dictStickers[category];
            }

            return null;
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
