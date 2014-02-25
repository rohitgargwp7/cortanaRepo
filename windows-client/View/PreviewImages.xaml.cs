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

namespace windows_client.View
{
    public partial class PreviewImages : PhoneApplicationPage
    {
        ObservableCollection<PhotoClass> listPic;
        ApplicationBarIconButton picturesUpload;
        ApplicationBarIconButton deleteIcon;
        int totalCount = 0;
        public PreviewImages()
        {
            InitializeComponent();

            ApplicationBar appbar = new ApplicationBar();
            appbar.IsVisible = true;
            appbar.Opacity = 0.5;
            this.ApplicationBar = appbar;

            picturesUpload = new ApplicationBarIconButton();
            picturesUpload.IconUri = new Uri("/View/images/appbar.send.png", UriKind.RelativeOrAbsolute);
            picturesUpload.Text = "upload";
            picturesUpload.Click += OnPicturesUploadClick;

            ApplicationBar.Buttons.Add(picturesUpload);

            deleteIcon = new ApplicationBarIconButton();
            deleteIcon.IconUri = new Uri("/View/images/appbar.delete.png", UriKind.RelativeOrAbsolute);
            deleteIcon.Text = "delete";
            deleteIcon.Click += deleteIcon_Click;

            ApplicationBar.Buttons.Add(deleteIcon);


            listPic = new ObservableCollection<PhotoClass>();
            bool defaultSelected = true;
            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.MULTIPLE_IMAGES, out obj))
            {
                List<PhotoClass> listSelectedPic = (List<PhotoClass>)obj;
                foreach (PhotoClass photo in listSelectedPic)
                {
                    listPic.Add(photo);
                    PivotItem pvt = GetPivotItem(photo);
                    pivotPhotos.Items.Add(pvt);
                    if (defaultSelected)
                    {
                        photo.IsSelected = true;
                        defaultSelected = false;
                    }
                    else
                        photo.IsSelected = false;


                }
                totalCount = listPic.Count;
                if (listPic.Count < HikeConstants.MAX_IMAGES_SHARE)
                {
                    PhotoClass photo = new PhotoClass(null);//to show add new image
                    photo.AddMoreImage = true;
                    listPic.Add(photo);
                }
            }
            headerText.Text = string.Format("preview 1 of {0}", totalCount);
            lbThumbnails.ItemsSource = listPic;
            lbThumbnails.SelectedIndex = 0;
            pivotPhotos.SelectedIndex = 0;
        }

        void deleteIcon_Click(object sender, EventArgs e)
        {
            Object obj;
            if (PhoneApplicationService.Current.State.TryGetValue(HikeConstants.MULTIPLE_IMAGES, out obj))
            {
                totalCount--;
                List<PhotoClass> listSelectedPic = (List<PhotoClass>)obj;
                int index = lbThumbnails.SelectedIndex;
                listSelectedPic.RemoveAt(index);
                pivotPhotos.Items.RemoveAt(index);
                listPic.RemoveAt(index);

                lbThumbnails.SelectedIndex = index > 0 ? index - 1 : 0;

                if (pivotPhotos.Items.Count == 0 && NavigationService.CanGoBack)
                    NavigationService.GoBack();

                if (!listPic[listPic.Count - 1].AddMoreImage)
                {
                    PhotoClass photo = new PhotoClass(null);//to show add new image
                    photo.AddMoreImage = true;
                    listPic.Add(photo);
                }
            }
        }

        private void OnPicturesUploadClick(object sender, EventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }


        public PivotItem GetPivotItem(PhotoClass photo)
        {
            PivotItem pvtItem = new PivotItem();

            Grid grid = new Grid();
            grid.Margin = new Thickness(0);
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;

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
            return pvtItem;
        }

        private void lbThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbThumbnails.SelectedIndex < 0)
                return;
            if (listPic[lbThumbnails.SelectedIndex].AddMoreImage)
            {
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
                return;
            }
            if (pivotPhotos.SelectedIndex != lbThumbnails.SelectedIndex || (pivotPhotos.SelectedIndex == 0 && lbThumbnails.SelectedIndex == 0))
            {
                headerText.Text = string.Format("preview {0} of {1}", lbThumbnails.SelectedIndex + 1, totalCount);

                listPic[pivotPhotos.SelectedIndex].IsSelected = false;
                PhotoClass photo = listPic[lbThumbnails.SelectedIndex];
                photo.IsSelected = true;
                lbThumbnails.ScrollIntoView(photo);
                pivotPhotos.SelectedIndex = lbThumbnails.SelectedIndex;
            }
        }

        private void pivotPhotos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotPhotos.SelectedIndex < 0)
                return;
            if (pivotPhotos.SelectedIndex != lbThumbnails.SelectedIndex || (pivotPhotos.SelectedIndex == 0 && lbThumbnails.SelectedIndex == 0))
            {
                headerText.Text = string.Format("preview {0} of {1}", pivotPhotos.SelectedIndex + 1, totalCount);

                listPic[lbThumbnails.SelectedIndex].IsSelected = false;
                PhotoClass photo = listPic[pivotPhotos.SelectedIndex];
                photo.IsSelected = true;
                lbThumbnails.ScrollIntoView(photo);
                lbThumbnails.SelectedIndex = pivotPhotos.SelectedIndex;
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
        }

    }
}