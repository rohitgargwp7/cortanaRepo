using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using windows_client.Model;
using System.Windows.Media;
using System.Collections.ObjectModel;
using windows_client.Languages;
using System.Threading.Tasks;

namespace windows_client.View
{
    public partial class PreviewImages : PhoneApplicationPage
    {
        ObservableCollection<PhotoItem> _listPic;
        ApplicationBarIconButton _picturesUpload;
        ApplicationBarIconButton _deleteIcon;

        /// <summary>
        /// total number of images selected by user for preview
        /// </summary>
        int _totalCount = 0;

        /// <summary>
        /// Variable to identify if SML page needs to be displayed depending upon Media Settings
        /// </summary>
        bool _showQualityPage;

        public PreviewImages()
        {
            InitializeComponent();
            InitialiseAppBar();

            _listPic = new ObservableCollection<PhotoItem>();
            shellProgressPhotos.Visibility = Visibility.Visible;
            Loaded += PreviewImages_Loaded;
        }

        void PreviewImages_Loaded(object sender, RoutedEventArgs e)
        {
            BindPivotPhotos();
        }

        #region Page Functions

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            base.OnRemovedFromJournal(e);

            //clear thumbnail cache as it is not required now
            App.ViewModel.ClearMFtImageCache();
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

        #endregion

        #region Helper Functions

        private void InitialiseAppBar()
        {
            ApplicationBar = new ApplicationBar()
            {
                ForegroundColor = (Color)App.Current.Resources["AppBarBlackForegroundColor"],
                BackgroundColor = (Color)App.Current.Resources["AppBarBlackBackgroundColor"],
            };
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 0.5;

            //Add "Image Quality" selection button if user media setting is "ask every time" else add "send" button
            _picturesUpload = new ApplicationBarIconButton();
            if (App.appSettings.Contains(HikeConstants.SET_IMAGE_QUALITY))
            {
                _showQualityPage = false;                
                _picturesUpload.IconUri = new Uri("/View/images/AppBar/icon_send.png", UriKind.RelativeOrAbsolute);
                _picturesUpload.Text = AppResources.Send_Txt;                
            }
            else
            {
                _showQualityPage = true;
                _picturesUpload.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.RelativeOrAbsolute);
                _picturesUpload.Text = AppResources.imageQuality_txt;
            }
            _picturesUpload.Click += OnPicturesUploadClick;
            ApplicationBar.Buttons.Add(_picturesUpload);

            _deleteIcon = new ApplicationBarIconButton();
            _deleteIcon.IconUri = new Uri("/View/images/AppBar/appbar.delete.png", UriKind.RelativeOrAbsolute);
            _deleteIcon.Text = AppResources.Delete_Txt;
            _deleteIcon.Click += deleteIcon_Click;

            ApplicationBar.Buttons.Add(_deleteIcon);
        }

        public void BindPivotPhotos()
        {
            bool defaultSelected = true;
            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.MULTIPLE_IMAGES, out obj))
            {
                List<PhotoItem> listSelectedPic = (List<PhotoItem>)obj;
                foreach (PhotoItem photo in listSelectedPic)
                {
                    _listPic.Add(photo);
                    PivotItem pvt = GetPivotItem(photo);
                    pivotPhotos.Items.Add(pvt);
                    //marking first object as selected on page load and others as unselected as same object is used on to and fro, it wont get unselected itself
                    if (defaultSelected)
                    {
                        photo.IsSelected = true;
                        defaultSelected = false;
                    }
                    else
                        photo.IsSelected = false;
                }

                _totalCount = _listPic.Count;
                if (_listPic.Count < HikeConstants.MAX_IMAGES_SHARE)
                {
                    PhotoItem photo = new PhotoItem(null);//to show add new image
                    photo.AddMoreImage = true;
                    _listPic.Add(photo);
                }
            }
            headerText.Text = string.Format(AppResources.PreviewImages_Header_txt, 1, _totalCount);
            lbThumbnails.ItemsSource = _listPic;
            lbThumbnails.SelectedIndex = 0;
            pivotPhotos.SelectedIndex = 0;

            //create a delay so that it gets hidden after ui render
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                shellProgressPhotos.Visibility = Visibility.Collapsed;
            });

        }

        public PivotItem GetPivotItem(PhotoItem photo)
        {
            PivotItem pvtItem = new PivotItem();

            Grid grid = new Grid();
            grid.Margin = new Thickness(0);
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;

            try
            {
                Image img = new Image();
                img.Source = photo.ImageSource;
                img.VerticalAlignment = VerticalAlignment.Center;
                img.HorizontalAlignment = HorizontalAlignment.Center;
                img.Stretch = Stretch.None;
                img.Margin = new Thickness(0);

                grid.Children.Add(img);
                pvtItem.Content = grid;
                pvtItem.HorizontalAlignment = HorizontalAlignment.Stretch;
                pvtItem.VerticalAlignment = VerticalAlignment.Stretch;
                pvtItem.Margin = pvtItem.Padding = new Thickness(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show (AppResources.CT_ImageNotOpenable_Text, AppResources.Something_Wrong_Txt, MessageBoxButton.OK);

                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }

            return pvtItem;
        }

        #endregion

        #region Event Handlers

        void deleteIcon_Click(object sender, EventArgs e)
        {
            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.MULTIPLE_IMAGES, out obj))
            {
                //update counter so that header text can be updated
                _totalCount--;
                List<PhotoItem> listSelectedPic = (List<PhotoItem>)obj;
                int index = lbThumbnails.SelectedIndex;
                listSelectedPic.RemoveAt(index);
                pivotPhotos.Items.RemoveAt(index);
                _listPic.RemoveAt(index);

                //if zeroith image is deleted then selected image should be next one else previous one 
                //it will call selection change event and other ui elements would be updated
                lbThumbnails.SelectedIndex = index > 0 ? index - 1 : 0;

                if (pivotPhotos.Items.Count == 0 && NavigationService.CanGoBack)
                    NavigationService.GoBack();

                //if add more doesnot exist previously then add it to list
                if (!_listPic[_listPic.Count - 1].AddMoreImage)
                {
                    PhotoItem photo = new PhotoItem(null);//to show add new image
                    photo.AddMoreImage = true;
                    _listPic.Add(photo);
                }
            }
        }

        private void OnPicturesUploadClick(object sender, EventArgs e)
        {            
            if (_showQualityPage)
            {
                NavigationService.Navigate(new Uri("/View/SetImageQuality.xaml", UriKind.RelativeOrAbsolute));
            }
            else
            {
                //previous page is viewalbums page and then new chatthread page
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        private void lbThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbThumbnails.SelectedIndex < 0)
                return;

            //on tapping add more go back to previous page to add more image
            if (_listPic[lbThumbnails.SelectedIndex].AddMoreImage)
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                return;
            }

            if (pivotPhotos.SelectedIndex != lbThumbnails.SelectedIndex || (pivotPhotos.SelectedIndex == 0 && lbThumbnails.SelectedIndex == 0))
            {
                headerText.Text = string.Format(AppResources.PreviewImages_Header_txt, lbThumbnails.SelectedIndex + 1, _totalCount);

                _listPic[pivotPhotos.SelectedIndex].IsSelected = false;
                PhotoItem photo = _listPic[lbThumbnails.SelectedIndex];
                photo.IsSelected = true;
                lbThumbnails.ScrollIntoView(photo);
                pivotPhotos.SelectedIndex = lbThumbnails.SelectedIndex;
            }
        }

        private void pivotPhotos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotPhotos.SelectedIndex < 0 || lbThumbnails.SelectedIndex < 0)
                return;

            if (pivotPhotos.SelectedIndex != lbThumbnails.SelectedIndex || (pivotPhotos.SelectedIndex == 0 && lbThumbnails.SelectedIndex == 0))
            {
                headerText.Text = string.Format(AppResources.PreviewImages_Header_txt, pivotPhotos.SelectedIndex + 1, _totalCount);

                _listPic[lbThumbnails.SelectedIndex].IsSelected = false;
                PhotoItem photo = _listPic[pivotPhotos.SelectedIndex];
                photo.IsSelected = true;
                lbThumbnails.ScrollIntoView(photo);
                lbThumbnails.SelectedIndex = pivotPhotos.SelectedIndex;
            }
        }

        #endregion
    }
}