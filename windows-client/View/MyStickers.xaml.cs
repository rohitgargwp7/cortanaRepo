using System;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model.Sticker;
using System.Collections.ObjectModel;
using windows_client.utils;
using windows_client.ViewModel;

namespace windows_client.View
{
    public partial class MyStickers : PhoneApplicationPage
    {
        public MyStickers()
        {
            InitializeComponent();
            llsCategories.ItemsSource = StickerHelper.GetAllStickerGategories();
        }
        bool _isSelectionChanged=false;
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (App.newChatThreadPage != null && _isSelectionChanged)
                App.newChatThreadPage.UpdateCategoryOrder(StickerHelper.GetStickerCategoryOrder());
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            StickerCategory stickerCategory = ((CheckBox)sender).DataContext as StickerCategory;
            if (HikeViewModel.StickerHelper.GetStickersByCategory(stickerCategory.Category) != null)
                HikeViewModel.StickerHelper.UpdateVisibility(stickerCategory.Category, true);
            else
                HikeViewModel.StickerHelper.CreateCategory(stickerCategory.Category, true);
            _isSelectionChanged = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            StickerCategory stickerCategory = ((CheckBox)sender).DataContext as StickerCategory;
            HikeViewModel.StickerHelper.UpdateVisibility(stickerCategory.Category, false);
            _isSelectionChanged = true;
        }

        private void CheckBox_Loaded(object sender, RoutedEventArgs e)
        {
            CheckBox cbx = sender as CheckBox;
            cbx.Checked += CheckBox_Checked;
            cbx.Unchecked += CheckBox_Unchecked;
        }
    }
}