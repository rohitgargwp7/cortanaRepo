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
using RPALApiComponent;
using windows_client.Model.Video;
using System.Diagnostics;
using System.Windows.Resources;
using windows_client.utils;
using System.Threading.Tasks;
using windows_client.Model;
using Microsoft.Phone.Tasks;
using windows_client.Languages;

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
            PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
          
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || App.IS_TOMBSTONED)
            {
                BindAlbums();
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (gridVideos.Visibility == Visibility.Visible)
            {
                ToggleView(true);
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
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
                FetchPreRecordedVideos wrt = new FetchPreRecordedVideos();
                ushort totalVideos = wrt.GetVideoCount();
                
                if (totalVideos > 0)
                {
                    for (int i = 0; i < totalVideos; i++)
                    {
                        string filePath = string.Empty;
                        string albumName = string.Empty;
                        int videoSize;
                        int videoDuration;
                        double date;

                        Byte[] thumbBytes = wrt.GetVideoInfo((byte)i, out filePath, out date,out videoDuration,out videoSize);
                        try
                        {
                            albumName = filePath.Substring(0, filePath.LastIndexOf("\\"));
                            albumName = albumName.Substring(albumName.LastIndexOf("\\") + 1);
                        }
                        catch
                        {
                            albumName = AppResources.Default_Video_Album_Txt;
                        }
                        VideoClass video = new VideoClass(filePath, thumbBytes, videoDuration, videoSize);
                        DateTime dob = new DateTime(Convert.ToInt64(date), DateTimeKind.Utc);
                        video.TimeStamp = dob.AddYears(1600);//file time is ticks starting from jan 1 1601 so adding 1600 years
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
                wrt.ClearData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception::ViewPhotoAlbums:GetAlbums," + ex.Message + "---" + ex.StackTrace);
            }
            return videoAlbumList.Values.ToList();
        }

        private void Albums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoAlbumClass album = llsAlbums.SelectedItem as VideoAlbumClass;
            if (album == null)
                return;
            albumNameTxt.Text = album.AlbumName.ToLower();
            llsAlbums.SelectedItem = null;
            ToggleView(false);
            llsVideos.ItemsSource = null;
            shellProgressVideos.Visibility = Visibility.Visible;
            BindAlbumVideos(album);
        }

        private async Task BindAlbumVideos(VideoAlbumClass album)
        {
            await Task.Delay(1);
            llsVideos.ItemsSource = GroupedVideos(album);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressVideos.Visibility = Visibility.Collapsed;
            });
        }
        #endregion ALBUMS

        #region Videos

        public async Task BindVideos()
        {
            await Task.Delay(1);
            llsAllVideos.ItemsSource = GroupedVideos(listAllVideos);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAllVideos.Visibility = Visibility.Collapsed;
            });
        }


        public List<KeyedList<string, VideoClass>> GroupedVideos(List<VideoClass> listVideos)
        {
            if (listVideos == null || listVideos.Count == 0)
                return null;
            var groupedPhotos =
                from video in listVideos
                orderby video.TimeStamp descending
                group video by video.TimeStamp.ToString("y") into videosByMonth
                select new KeyedList<string, VideoClass>(videosByMonth);
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

        #region helper functions
        public void ToggleView(bool showAlbum)
        {
            if (showAlbum)
            {
                gridAlbums.Visibility = Visibility.Visible;
                gridVideos.Visibility = Visibility.Collapsed;
            }
            else
            {
                gridAlbums.Visibility = Visibility.Collapsed;
                gridVideos.Visibility = Visibility.Visible;
            }
        }

        #endregion

        private void llsVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector lls = sender as LongListSelector;
            VideoClass selectedVideo = lls.SelectedItem as VideoClass;
            if (selectedVideo == null)
                return;
            lls.SelectedItem = null;

            PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = selectedVideo;

            NavigationService.Navigate(new Uri("/View/PreviewVideo.xaml", UriKind.Relative));
        }

        private void pivotAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //this.ApplicationBar.IsVisible = pivotAlbums.SelectedIndex == 1;
            if (pivotAlbums.SelectedIndex == 1)
            {
                shellProgressAllVideos.Visibility = Visibility.Visible;
                BindVideos();
            }
        }
    }
}