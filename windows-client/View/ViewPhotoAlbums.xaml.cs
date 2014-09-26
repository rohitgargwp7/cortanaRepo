using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.Model;
using Microsoft.Xna.Framework.Media;
using windows_client.utils;
using System.Windows.Media;
using System.Diagnostics;
using System.Threading.Tasks;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class ViewPhotoAlbums : PhoneApplicationPage
    {
        ApplicationBarIconButton picturesUpload;
        ApplicationBarIconButton multipleSelect;
        bool isSingleListSelected = true;
        bool isAllPicturesLaoded = false;
        List<PhotoItem> listPic = null;

        public ViewPhotoAlbums()
        {
            InitializeComponent();
            ApplicationBar appbar = new ApplicationBar();
            appbar.IsVisible = false;
            appbar.Opacity = 0.5;
            appbar.BackgroundColor = Colors.Black;
            appbar.ForegroundColor = Colors.White;
            this.ApplicationBar = appbar;

            picturesUpload = new ApplicationBarIconButton();
            picturesUpload.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.RelativeOrAbsolute);
            picturesUpload.Text = AppResources.AppBar_Done_Btn;
            picturesUpload.Click += OnPicturesUploadClick;

            multipleSelect = new ApplicationBarIconButton();
            multipleSelect.IconUri = new Uri("/View/images/AppBar/appbar.select.png", UriKind.RelativeOrAbsolute);
            multipleSelect.Text = AppResources.ViewAlbum_AppBar_Select_text;
            multipleSelect.Click += multipleSelect_Click;

            appbar.Buttons.Add(multipleSelect);

            txtMaxImageSelected.Text = string.Format(AppResources.ViewAlbums_MaxImageSelection, HikeConstants.MAX_IMAGES_SHARE);
        }

        #region Albums
        private async Task BindAlbums()
        {
            await Task.Delay(1);
            llsAlbums.ItemsSource = GetAlbums(out listPic);
            //create a delay so that it hides after ui render
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAlbums.Visibility = Visibility.Collapsed;
            });
        }

        public List<PhotoAlbum> GetAlbums(out List<PhotoItem> listPicture)
        {
            Dictionary<string, PhotoAlbum> albumList = new Dictionary<string, PhotoAlbum>();
            listPicture = new List<PhotoItem>();

            try
            {
                MediaLibrary lib = new MediaLibrary();
                //lib.pictures without ordering was throwing unexpected error exception on non debugger mode
                foreach (Picture pic in lib.Pictures.OrderBy(x => x.Date))
                {
                    PhotoItem imageData = new PhotoItem(pic)
                    {
                        Title = pic.Name,
                        TimeStamp = pic.Date
                    };
                    PhotoAlbum albumObj;
                    if (!albumList.TryGetValue(pic.Album.Name, out albumObj))
                    {
                        albumObj = new PhotoAlbum(pic.Album.Name);
                        albumList.Add(pic.Album.Name, albumObj);
                    }
                    albumObj.Add(imageData);
                    albumObj.AlbumPicture = pic;
                    listPicture.Add(imageData);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception::ViewPhotoAlbums:GetAlbums," + ex.Message + "---" + ex.StackTrace);
            }
            return albumList.Values.ToList();
        }

        private void Albums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PhotoAlbum album = llsAlbums.SelectedItem as PhotoAlbum;
            if (album == null)
                return;
            albumNameTxt.Text = album.AlbumName.ToLower();
            llsAlbums.SelectedItem = null;
            ToggleView(false);
            llsPhotos.ItemsSource = null;
            shellProgressPhotos.Visibility = Visibility.Visible;
            BindAlbumPhotos(album);
        }

        private async Task BindAlbumPhotos(PhotoAlbum album)
        {
            await Task.Delay(1);
            llsPhotos.ItemsSource = GroupedPhotos(album);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressPhotos.Visibility = Visibility.Collapsed;
            });
        }
        #endregion ALBUMS

        #region Photos

        private async Task BindPhotos()
        {
            await Task.Delay(1);
            llsAllPhotos.ItemsSource = GroupedPhotos(listPic);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAllPhotos.Visibility = Visibility.Collapsed;
            });
        }

        void OnPicturesUploadClick(object sender, EventArgs e)
        {
            List<PhotoItem> listSelectedItems = new List<PhotoItem>();
            LongListMultiSelector lls = isSingleListSelected ? llsAllPhotos : llsPhotos;
            foreach (PhotoItem picture in lls.SelectedItems)
            {
                listSelectedItems.Add(picture);
            }
            PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.MULTIPLE_IMAGES] = listSelectedItems;

            NavigationService.Navigate(new Uri("/View/PreviewImages.xaml", UriKind.RelativeOrAbsolute));
        }

        public List<KeyedList<string, PhotoItem>> GroupedPhotos(List<PhotoItem> album)
        {
            if (album == null || album.Count == 0)
                return null;
            var groupedPhotos =
                from photo in album
                orderby photo.TimeStamp descending
                group photo by photo.TimeStamp.ToString("y") into photosByMonth
                select new KeyedList<string, PhotoItem>(photosByMonth);
            return new List<KeyedList<string, PhotoItem>>(groupedPhotos);
        }

        public class KeyedList<TKey, TItem> : List<TItem>
        {
            public TKey Key { protected set; get; }

            public KeyedList(TKey key, IEnumerable<TItem> items)
                : base(items)
            {
                Key = key;
            }

            public KeyedList(IGrouping<TKey, TItem> grouping)
                : base(grouping)
            {
                Key = grouping.Key;
            }
        }
        #endregion

        #region helper functions
        public void ToggleView(bool showAlbum)
        {
            if (showAlbum)
            {
                gridAlbums.Visibility = Visibility.Visible;
                gridPhotos.Visibility = Visibility.Collapsed;
                isSingleListSelected = true;
            }
            else
            {
                gridAlbums.Visibility = Visibility.Collapsed;
                gridPhotos.Visibility = Visibility.Visible;

                isSingleListSelected = false;
            }
            this.ApplicationBar.IsVisible = !showAlbum;

        }

        private void ToggleAppBarIcons(bool enableMultiselect)
        {
            try
            {
                if (enableMultiselect)
                {
                    this.ApplicationBar.Buttons.RemoveAt(0);
                    picturesUpload.IsEnabled = false;
                    this.ApplicationBar.Buttons.Add(picturesUpload);
                    if (isSingleListSelected)
                    {
                        pivotAlbums.IsLocked = true;
                        llsAllPhotos.EnforceIsSelectionEnabled = true;
                    }
                    else
                        llsPhotos.EnforceIsSelectionEnabled = true;
                }
                else
                {
                    this.ApplicationBar.Buttons.RemoveAt(0);
                    this.ApplicationBar.Buttons.Add(multipleSelect);

                    if (isSingleListSelected)
                    {
                        pivotAlbums.IsLocked = false;
                        llsAllPhotos.EnforceIsSelectionEnabled = false;
                    }
                    else
                        llsPhotos.EnforceIsSelectionEnabled = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                //llsAllPhotos.EnforceIsSelectionEnabled = true; throwing exception internally
            }
        }
        #endregion

        #region event handlers

        void multipleSelect_Click(object sender, EventArgs e)
        {
            ToggleAppBarIcons(true);
        }

        private void llsPhotos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListMultiSelector lls = isSingleListSelected ? llsAllPhotos : llsPhotos;
            picturesUpload.IsEnabled = lls.SelectedItems.Count > 0;
            if (lls.SelectedItems.Count > HikeConstants.MAX_IMAGES_SHARE)
            {
                lls.SelectedItems.RemoveAt(HikeConstants.MAX_IMAGES_SHARE);
                if (MaxLabelsp.Visibility == Visibility.Collapsed)
                    StoryBoard0.Begin();
            }
            else
            {
                StoryBoard0.Stop();
            }
        }

        private void pivotAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ApplicationBar.IsVisible = pivotAlbums.SelectedIndex == 1;
            if (pivotAlbums.SelectedIndex == 1 && !isAllPicturesLaoded)
            {
                shellProgressAllPhotos.Visibility = Visibility.Visible;
                BindPhotos();
                isAllPicturesLaoded = true;
            }
        }

        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;
            if (fe != null)
            {
                PhotoItem picture = fe.DataContext as PhotoItem;
                PhoneApplicationService.Current.State[HikeConstants.NavigationKeys.MULTIPLE_IMAGES] = new List<PhotoItem>() { picture };
                NavigationService.Navigate(new Uri("/View/PreviewImages.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        #endregion

        #region Page Functions

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SystemTray.IsVisible = false;

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || HikeInstantiation.IsTombstoneLaunch)
            {
                BindAlbums();
            }

            Object obj;
            //clear multiselect list and update selected items so that if user deleted some items it can be updated
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.NavigationKeys.MULTIPLE_IMAGES, out obj))
            {
                List<PhotoItem> listSelectedItems = (List<PhotoItem>)obj;
                LongListMultiSelector lls = isSingleListSelected ? llsAllPhotos : llsPhotos;
                ToggleAppBarIcons(true);
                lls.SelectedItems.Clear();
                foreach (PhotoItem pic in listSelectedItems)
                {
                    var container = lls.ContainerFromItem(pic) as LongListMultiSelectorItem;
                    if (container != null)
                        container.IsSelected = true;
                }
                PhoneApplicationService.Current.State.Remove(HikeConstants.NavigationKeys.MULTIPLE_IMAGES);
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            HikeInstantiation.ViewModel.ClearMFtImageCache();
            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            //particular album is selected
            if (gridPhotos.Visibility == Visibility.Visible)
            {
                //disable multiselect for album images list
                if (this.ApplicationBar.Buttons.Contains(picturesUpload))
                    ToggleAppBarIcons(false);
                //go back to pivot view 
                else
                    ToggleView(true);
                e.Cancel = true;
            }
            else if (pivotAlbums.SelectedIndex == 1 && this.ApplicationBar.Buttons.Contains(picturesUpload))
            {
                //disable multiselect for all images list
                ToggleAppBarIcons(false);
                e.Cancel = true;
            }
            else
                //go back and cleat thumbnail cache
                HikeInstantiation.ViewModel.ClearMFtImageCache();
            base.OnBackKeyPress(e);
        }

        #endregion
    }
}