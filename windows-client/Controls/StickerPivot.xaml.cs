using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.ObjectModel;
using windows_client.utils;

namespace windows_client.Controls
{
    public partial class StickerPivot : UserControl
    {
        private int _pivotIndex;

        public StickerPivot(EventHandler<System.Windows.Input.GestureEventArgs> stickerTap, EventHandler<ItemRealizationEventArgs> stickerItemsRealized,
            ObservableCollection<Sticker> listStickers,int pivotIndex)
        {
            InitializeComponent();
            if (stickerTap != null)
            {
                llsStickerCategory.Tap += stickerTap;
            }
            if (stickerItemsRealized != null)
            {
                llsStickerCategory.ItemRealized += stickerItemsRealized;
            }
            llsStickerCategory.ItemsSource = listStickers;
            _pivotIndex = pivotIndex;
        }

        //call from ui thread
        public void ShowStickers()
        {
            llsStickerCategory.Visibility = Visibility.Visible;
            stLoading.Visibility = Visibility.Collapsed;
            stNoStickers.Visibility = Visibility.Collapsed;
        }
        //call from ui thread
        public void ShowLoadingStickers()
        {
            llsStickerCategory.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Visible;
            stNoStickers.Visibility = Visibility.Collapsed;
        }
        //call from ui thread
        public void ShowNoStickers()
        {
            llsStickerCategory.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Collapsed;
            stNoStickers.Visibility = Visibility.Visible;
        }

        public int PivotItemIndex
        {
            get
            {
                return _pivotIndex;
            }
        }
    }
}
