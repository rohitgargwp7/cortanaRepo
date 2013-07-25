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

namespace windows_client.Controls
{
    public partial class StickerPivotItem : UserControl
    {
        private int _pivotIndex;
        private string _category;
        public StickerPivotItem(ObservableCollection<Sticker> listStickers, int pivotIndex, string category)
        {
            InitializeComponent();
            llsStickerCategory.Tap += Stickers_Tap;
            llsStickerCategory.ItemsSource = listStickers;
            _pivotIndex = pivotIndex;
            _category = category;
        }

        private void Stickers_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ListBox llsStickerCategory = (sender as ListBox);
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
            if (llsStickerCategory.Items != null && llsStickerCategory.Items.Count > 0)
                btnClose.Visibility = Visibility.Visible;
            else
                btnClose.Visibility = Visibility.Collapsed;

        }
        private void RetryStickersTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (llsStickerCategory.Items != null && llsStickerCategory.Items.Count > 0)
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

        private void vgroup_CurrentStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState.Name == "CompressionBottom")
            {
                StickerCategory stickerCategory;
                if (App.newChatThreadPage != null && (stickerCategory = HikeViewModel.stickerHelper.GetStickersByCategory(_category)) != null && stickerCategory.HasMoreStickers && !stickerCategory.ShowDownloadMessage && !stickerCategory.IsDownLoading)
                {
                    if (llsStickerCategory.ItemsSource != null && llsStickerCategory.Items.Count > 0)
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
