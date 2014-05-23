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
using windows_client.ViewModel;
using System.Windows.Media;
using System.Collections;
using System.Windows.Controls.Primitives;
using windows_client.Languages;

namespace windows_client.Controls
{
    public partial class StickerPivotItem : UserControl
    {
        private int _pivotIndex;
        private string _category;
        public StickerPivotItem(int pivotIndex, string category)
        {
            InitializeComponent();
            llsStickerCategory.Tap += Stickers_Tap;
            //  llsStickerCategory.ItemsSource = listStickers;item source will be set when that particular category would be tapped
            _pivotIndex = pivotIndex;
            _category = category;
        }

        public void SetLlsSource(ObservableCollection<Sticker> listStickers)
        {
            llsStickerCategory.ItemsSource = listStickers;
        }
        public void SetLlsSourceList(List<Sticker> listStickers)
        {
            llsStickerCategory.ItemsSource = listStickers;
        }
        private void Stickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            LongListSelector llsStickerCategory = (sender as LongListSelector);
            Sticker sticker = llsStickerCategory.SelectedItem as Sticker;
            llsStickerCategory.SelectedItem = null;
            if (sticker == null)
                return;
            if (App.newChatThreadPage != null)
            {
                App.newChatThreadPage.SendSticker(sticker);
            }
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
            if (_category == StickerHelper.CATEGORY_RECENT)
                txtNoSticker.Text = AppResources.RecentSticker_Default_Txt;
            else
                txtNoSticker.Text = AppResources.No_Stickers_Downloaded_Txt;

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
            if (App.newChatThreadPage != null && (stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_category)) != null)
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

        ScrollBar vScrollBar = null;
        private void vScrollBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            vScrollBar = sender as ScrollBar;
            if (vScrollBar != null)
            {
                if ((vScrollBar.Maximum - vScrollBar.Value) < 100)
                {
                    StickerCategory stickerCategory;
                    //if download message is shown that means user has not yet requested download
                    if (App.newChatThreadPage != null && (stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_category)) != null && !stickerCategory.ShowDownloadMessage && stickerCategory.HasMoreStickers && !stickerCategory.IsDownLoading)
                    {
                        if (llsStickerCategory.ItemsSource != null && llsStickerCategory.ItemsSource.Count > 0)
                        {
                            ShowStickers();
                            ShowHidMoreProgreesBar(true);
                        }
                        else
                            ShowLoadingStickers();
                        App.newChatThreadPage.PostRequestForBatchStickers(stickerCategory);
                    }
                }
            }
        }
    }
}
