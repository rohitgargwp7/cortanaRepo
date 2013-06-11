﻿using System;
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
using windows_client.ViewModel;

namespace windows_client.Controls
{
    public partial class StickerPivot : UserControl
    {
        private int _pivotIndex;
        private string _category;
        public StickerPivot(EventHandler<System.Windows.Input.GestureEventArgs> stickerTap, EventHandler<ItemRealizationEventArgs> stickerItemsRealized,
            ObservableCollection<Sticker> listStickers, int pivotIndex, string category)
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
            _category = category;
        }

        //call from ui thread
        public void ShowStickers()
        {
            llsStickerCategory.Visibility = Visibility.Visible;
            stLoading.Visibility = Visibility.Collapsed;
            stNoStickers.Visibility = Visibility.Collapsed;
            stRetry.Visibility = Visibility.Collapsed;
        }
        //call from ui thread
        public void ShowLoadingStickers()
        {
            llsStickerCategory.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Visible;
            stNoStickers.Visibility = Visibility.Collapsed;
            stRetry.Visibility = Visibility.Collapsed;
        }
        //call from ui thread
        public void ShowNoStickers()
        {
            llsStickerCategory.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Collapsed;
            stNoStickers.Visibility = Visibility.Visible;
            stRetry.Visibility = Visibility.Collapsed;
        }

        public void ShowHidMoreProgreesBar(bool show)
        {
            if (show)
                moreProgressBar.Visibility = Visibility.Visible;
            else
                moreProgressBar.Visibility = Visibility.Collapsed;
        }

        public void ShowDownloadFailed()
        {
            llsStickerCategory.Visibility = Visibility.Collapsed;
            stLoading.Visibility = Visibility.Collapsed;
            stNoStickers.Visibility = Visibility.Collapsed;
            stRetry.Visibility = Visibility.Visible;
            if (llsStickerCategory.ItemsSource != null && llsStickerCategory.ItemsSource.Count > 0)
                btnClose.Visibility = Visibility.Visible;
            else
                btnClose.Visibility = Visibility.Collapsed;

        }
        private void RetryStickersTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (llsStickerCategory.ItemsSource != null && llsStickerCategory.ItemsSource.Count > 0)
            {
                ShowStickers();
                ShowHidMoreProgreesBar(true);
            }
            else
                ShowLoadingStickers();

            StickerCategory stickerCategory;
            if (App.newChatThreadPage != null && (stickerCategory=HikeViewModel.stickerHelper.GetStickersByCategory(_category))!=null)
            {
                App.newChatThreadPage.PostRequestForBatchStickers(stickerCategory);
            }
        }

        public int PivotItemIndex
        {
            get
            {
                return _pivotIndex;
            }
        }

        private void btnClose_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            stRetry.Visibility = Visibility.Collapsed;
            llsStickerCategory.Visibility = Visibility.Visible;
        }
    }
}
