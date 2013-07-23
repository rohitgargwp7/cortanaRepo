using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using windows_client.Controls;
using windows_client.ViewModel;

namespace windows_client.utils
{
    class StickerPivotHelper
    {
        bool isInitialised = false;
        private static StickerPivotHelper _instance;

        private Pivot _stickerPivot;
        public Dictionary<string, StickerPivotItem> dictStickersPivot = new Dictionary<string, StickerPivotItem>();
        public Dictionary<int, string> dictPivotCategory = new Dictionary<int, string>();
        Thickness zeroThickness = new Thickness(0, 0, 0, 0);
        Thickness newCategoryThickness = new Thickness(0, 1, 0, 0);

        private StickerPivotHelper()
        {
        }

        public static StickerPivotHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new StickerPivotHelper();
                }
                return _instance;
            }
        }
        public Pivot StickerPivot
        {
            get
            {
                return _stickerPivot;
            }
        }
        public void InitialiseStickerPivot()
        {
            if (!isInitialised)
            {
                _stickerPivot = new Pivot();
                StickerCategory stickerCategory;
                int pivotIndex = 0;
                //done thos way to maintain order of insertion
                if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_DOGGY)) != null)
                {
                    CreateStickerPivotItem(stickerCategory.Category, stickerCategory.ListStickers, pivotIndex);
                    dictPivotCategory[pivotIndex] = StickerHelper.CATEGORY_DOGGY;
                    pivotIndex++;
                }

                if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_KITTY)) != null)
                {
                    CreateStickerPivotItem(stickerCategory.Category, stickerCategory.ListStickers, pivotIndex);
                    dictPivotCategory[pivotIndex] = StickerHelper.CATEGORY_KITTY;
                    pivotIndex++;
                }

                if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_EXPRESSIONS)) != null)
                {
                    CreateStickerPivotItem(stickerCategory.Category, stickerCategory.ListStickers, pivotIndex);
                    dictPivotCategory[pivotIndex] = StickerHelper.CATEGORY_EXPRESSIONS;
                    pivotIndex++;
                }

                if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_BOLLYWOOD)) != null
                    && Utils.IsBollywoodVisible)
                {
                    CreateStickerPivotItem(stickerCategory.Category, stickerCategory.ListStickers, pivotIndex);
                    dictPivotCategory[pivotIndex] = StickerHelper.CATEGORY_BOLLYWOOD;
                    pivotIndex++;
                }
                if ((stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(StickerHelper.CATEGORY_TROLL)) != null)
                {
                    CreateStickerPivotItem(stickerCategory.Category, stickerCategory.ListStickers, pivotIndex);
                    dictPivotCategory[pivotIndex] = StickerHelper.CATEGORY_TROLL;
                }
                isInitialised = true;
            }
        }

        private void CreateStickerPivotItem(string category, ObservableCollection<Sticker> listSticker, int pivotIndex)
        {
            PivotItem pvt = new PivotItem();
            pvt.Margin = zeroThickness;
            pvt.BorderThickness = zeroThickness;
            pvt.Padding = zeroThickness;
            StickerPivotItem stickerPivot = new StickerPivotItem( listSticker, pivotIndex, category);
            StickerPivotHelper.Instance.dictStickersPivot[category] = stickerPivot;
            pvt.Content = stickerPivot;
            _stickerPivot.Items.Add(pvt);
        }

    }
}
