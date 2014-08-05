using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO;
using WindowsPhoneRuntimeComponent1;
using windows_client.Model.Video;
using System.Diagnostics;
using System.Windows.Resources;
using windows_client.utils;
using System.Threading.Tasks;
using windows_client.Model;

namespace windows_client.View
{
    public partial class ViewVideos : PhoneApplicationPage
    {
        List<VideoClass> listAllVideos = null;
        public ViewVideos()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
                BindAlbums();
            }
        }

        private void llsVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoClass selectedVideo = llsVideos.SelectedItem as VideoClass;
            if (selectedVideo == null)
                return;
            llsVideos.SelectedItem = null;

            StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri(selectedVideo.FilePath, UriKind.Relative));
            byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_THUMB_SHARED] = selectedVideo.Thumbnail;
            NavigationService.GoBack();

        }

        #region Albums
        public async Task BindAlbums()
        {
            await Task.Delay(1);
            llsAlbums.ItemsSource = GetAlbums();
            //create a delay so that it hides after ui render
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAlbums.Visibility = Visibility.Collapsed;
            });
        }

        public List<VideoAlbumClass> GetAlbums()
        {
            Dictionary<string, VideoAlbumClass> videoAlbumList = new Dictionary<string, VideoAlbumClass>();
            listAllVideos = new List<VideoClass>();

            try
            {
                ushort totalVideos = WindowsPhoneRuntimeComponent.GetVideoCount();

                for (int i = 0; i < totalVideos; i++)
                {
                    string filePath = string.Empty;
                    string fileName = string.Empty;
                    string albumName = string.Empty;//todo:Fetch;
                    Byte[] thumbBytes = WindowsPhoneRuntimeComponent.myfunc((byte)i, out filePath, out fileName,out albumName);
                    albumName = "test";
                    VideoClass video = new VideoClass(fileName, filePath, thumbBytes)
                    {
                        TimeStamp = DateTime.Now//todo:Change
                    };
                    VideoAlbumClass albumObj;
                    if (!videoAlbumList.TryGetValue(albumName, out albumObj))
                    {
                        albumObj = new VideoAlbumClass(albumName, thumbBytes);
                        videoAlbumList.Add(albumName, albumObj);
                    }
                    albumObj.Add(video);
                    listAllVideos.Add(video);
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception::ViewPhotoAlbums:GetAlbums," + ex.Message + "---" + ex.StackTrace);
            }
            return videoAlbumList.Values.ToList();
        }

        private void Albums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PhotoAlbumClass album = llsAlbums.SelectedItem as PhotoAlbumClass;
            if (album == null)
                return;
            //albumNameTxt.Text = album.AlbumName.ToLower();
            //llsAlbums.SelectedItem = null;
            //ToggleView(false);
            //llsPhotos.ItemsSource = null;
            //shellProgressPhotos.Visibility = Visibility.Visible;
            //BindAlbumPhotos(album);
        }

        private async Task BindAlbumPhotos(PhotoAlbumClass album)
        {
            await Task.Delay(1);
            //llsVideos.ItemsSource = GroupedPhotos(album);
            ////create a delay so that it doesnot pause abruptly
            //Dispatcher.BeginInvoke(() =>
            //{
            //    shellProgressPhotos.Visibility = Visibility.Collapsed;
            //});
        }
        #endregion ALBUMS

        #region Videos

        public async Task BindVideos()
        {
            await Task.Delay(1);
            llsVideos.ItemsSource = GroupedPhotos(listAllVideos);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAllPhotos.Visibility = Visibility.Collapsed;
            });
        }


        public List<KeyedList<string, VideoClass>> GroupedPhotos(List<VideoClass> listVideos)
        {
            if (listVideos == null || listVideos.Count == 0)
                return null;
            var groupedPhotos =
                from photo in listVideos
                orderby photo.TimeStamp descending
                group photo by photo.TimeStamp.ToString("y") into photosByMonth
                select new KeyedList<string, VideoClass>(photosByMonth);
            return new List<KeyedList<string, VideoClass>>(groupedPhotos);
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

        private void pivotAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //this.ApplicationBar.IsVisible = pivotAlbums.SelectedIndex == 1;
            if (pivotAlbums.SelectedIndex == 1)
            {
                shellProgressAllPhotos.Visibility = Visibility.Visible;
                BindVideos();
            }
        }
    }
}