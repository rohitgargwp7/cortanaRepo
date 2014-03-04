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
        bool _isFirstLoad = true;
        bool isAllPicturesLaoded = false;
        List<PhotoClass> listPic = null;

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
            picturesUpload.IconUri = new Uri("/View/images/icon_tick.png", UriKind.RelativeOrAbsolute);
            picturesUpload.Text = AppResources.Done_Txt;
            picturesUpload.Click += OnPicturesUploadClick;

            multipleSelect = new ApplicationBarIconButton();
            multipleSelect.IconUri = new Uri("/View/images/appbar.select.png", UriKind.RelativeOrAbsolute);
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

        public List<AlbumClass> GetAlbums(out List<PhotoClass> listPicture)
        {
            List<AlbumClass> albumList = new List<AlbumClass>();
            listPicture = new List<PhotoClass>();
            MediaLibrary lib = new MediaLibrary();

            foreach (PictureAlbum picAlbum in lib.RootPictureAlbum.Albums)
            {
                AlbumClass albumObj = new AlbumClass(picAlbum.Name);
                Picture lastPic = null;
                foreach (Picture pic in picAlbum.Pictures)
                {
                    PhotoClass imageData = new PhotoClass(pic)
                    {
                        Title = pic.Name,
                        TimeStamp = pic.Date
                    };
                    lastPic = pic;
                    albumObj.Add(imageData);
                    listPicture.Add(imageData);
                }
                albumObj.AlbumPicture = lastPic;
                if (albumObj.Count > 0)
                    albumList.Add(albumObj);
            }
            return albumList;
        }

        private void Albums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AlbumClass album = llsAlbums.SelectedItem as AlbumClass;
            if (album == null)
                return;
            albumNameTxt.Text = album.AlbumName.ToLower();
            llsAlbums.SelectedItem = null;
            ToggleView(false);
            llsPhotos.ItemsSource = null;
            shellProgressPhotos.Visibility = Visibility.Visible;
            BindAlbumPhotos(album);
        }

        private async Task BindAlbumPhotos(AlbumClass album)
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
            List<PhotoClass> listSelectedItems = new List<PhotoClass>();
            LongListMultiSelector lls = isSingleListSelected ? llsAllPhotos : llsPhotos;
            foreach (PhotoClass picture in lls.SelectedItems)
            {
                listSelectedItems.Add(picture);
            }
            PhoneApplicationService.Current.State[HikeConstants.MULTIPLE_IMAGES] = listSelectedItems;

            NavigationService.Navigate(new Uri("/View/PreviewImages.xaml", UriKind.RelativeOrAbsolute));
        }

        public List<KeyedList<string, PhotoClass>> GroupedPhotos(List<PhotoClass> album)
        {
            if (album == null || album.Count == 0)
                return null;
            var groupedPhotos =
                from photo in album
                orderby photo.TimeStamp descending
                group photo by photo.TimeStamp.ToString("y") into photosByMonth
                select new KeyedList<string, PhotoClass>(photosByMonth);
            return new List<KeyedList<string, PhotoClass>>(groupedPhotos);
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

        private void ToggleAppBarIcons(bool showUpload)
        {
            try
            {
                if (showUpload)
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
                PhotoClass picture = fe.DataContext as PhotoClass;
                PhoneApplicationService.Current.State[HikeConstants.MULTIPLE_IMAGES] = new List<PhotoClass>() { picture };
                NavigationService.Navigate(new Uri("/View/PreviewImages.xaml", UriKind.RelativeOrAbsolute));
            }
        }

        #endregion

        #region Page Functions

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SystemTray.IsVisible = false;

            if (_isFirstLoad)
            {
                BindAlbums();
                _isFirstLoad = false;
            }

            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.MULTIPLE_IMAGES, out obj))
            {
                List<PhotoClass> listSelectedItems = (List<PhotoClass>)obj;
                LongListMultiSelector lls = isSingleListSelected ? llsAllPhotos : llsPhotos;
                ToggleAppBarIcons(true);
                lls.SelectedItems.Clear();
                foreach (PhotoClass pic in listSelectedItems)
                {
                    var container = lls.ContainerFromItem(pic) as LongListMultiSelectorItem;
                    if (container != null)
                        container.IsSelected = true;
                }
                PhoneApplicationService.Current.State.Remove(HikeConstants.MULTIPLE_IMAGES);
            }
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            App.ViewModel.ClearMFtImageCache();
            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (gridPhotos.Visibility == Visibility.Visible)
            {
                if (this.ApplicationBar.Buttons.Contains(picturesUpload))
                    ToggleAppBarIcons(false);
                else
                    ToggleView(true);
                e.Cancel = true;
            }
            else if (pivotAlbums.SelectedIndex == 1 && this.ApplicationBar.Buttons.Contains(picturesUpload))
            {
                ToggleAppBarIcons(false);
                e.Cancel = true;
            }
            else
                App.ViewModel.ClearMFtImageCache();
            base.OnBackKeyPress(e);
        }

        #endregion
    }
}