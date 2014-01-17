﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Media;
using System.Text;

namespace windows_client.View
{
    public partial class MultipleImageSelect : PhoneApplicationPage
    {
        ApplicationBarIconButton picturesUpload;

        public MultipleImageSelect()
        {
            InitializeComponent();
            PhotoHubLLS.ItemsSource = GroupedPhotos;


            ApplicationBar appbar = new ApplicationBar();
            appbar.IsVisible = true;
            this.ApplicationBar = appbar;

            picturesUpload = new ApplicationBarIconButton();
            picturesUpload.IconUri = new Uri("/ApplicationBar.Upload.png", UriKind.RelativeOrAbsolute);
            picturesUpload.Text = "upload";
            picturesUpload.Click += OnPicturesUploadClick;

            ApplicationBar.Buttons.Add(picturesUpload);
        }


        /// <summary>
        /// Simulates upload of the pictures
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void OnPicturesUploadClick(object sender, EventArgs e)
        {
            App.ViewModel.MultiplePhotos = new List<Picture>();
            foreach (Photo picture in PhotoHubLLS.SelectedItems)
            {
                App.ViewModel.MultiplePhotos.Add(picture.Pic);
            }
            NavigationService.GoBack();
        }


        List<KeyedList<string, Photo>> _groupedPhotos;
        public List<KeyedList<string, Photo>> GroupedPhotos
        {
            get
            {
                if (_groupedPhotos == null)
                {
                    var photos = GetPhotos();

                    var groupedPhotos =
                        from photo in photos
                        orderby photo.TimeStamp descending
                        group photo by photo.TimeStamp.ToString("y") into photosByMonth
                        select new KeyedList<string, Photo>(photosByMonth);
                    _groupedPhotos = new List<KeyedList<string, Photo>>(groupedPhotos);
                }
                return _groupedPhotos;
            }
        }

        public List<Photo> GetPhotos()
        {
            List<Photo> imageList = new List<Photo>();
            MediaLibrary lib = new MediaLibrary();
            foreach (Picture pic in lib.Pictures)
            {
                Photo imageData = new Photo(pic)
                {
                    Title = pic.Name,
                    TimeStamp = pic.Date
                };

                imageList.Add(imageData);
            }

            return imageList;
        }

        public class Photo
        {
            Picture _pic;
            BitmapImage _thumbnail;
            BitmapImage _image;
            public BitmapImage Thumbnail
            {
                get
                {
                    if (_thumbnail == null)
                    {
                        _thumbnail = new BitmapImage();
                        if (_pic != null)
                            _thumbnail.SetSource(_pic.GetThumbnail());
                    }
                    return _thumbnail;
                }
            }
            public BitmapImage ImageSource
            {
                get
                {
                    if (_image == null)
                    {
                        _image = new BitmapImage();
                        if (_pic != null)
                            _image.SetSource(_pic.GetImage());
                    }
                    return _thumbnail;
                }
            }
            public DateTime TimeStamp { get; set; }
            public string Title { get; set; }
            public Picture Pic
            {
                get
                {
                    return _pic;
                }
            }
            public Photo(Picture pic)
            {
                _pic = pic;
            }

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

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            base.OnBackKeyPress(e);
            if (PhotoHubLLS.ItemsSource != null)
            {
                PhotoHubLLS.ItemsSource.Clear();
                PhotoHubLLS.ItemsSource = null;
            }
            if (_groupedPhotos != null)
            {
                _groupedPhotos.Clear();
                _groupedPhotos = null;
            }
        }
    }
}