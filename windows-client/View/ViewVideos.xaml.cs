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
using windows_client.Model;
using System.Diagnostics;
using System.Windows.Resources;
using windows_client.utils;
using System.Threading.Tasks;
using Microsoft.Phone.Tasks;
using windows_client.Languages;

namespace windows_client.View
{
    public partial class ViewVideos : PhoneApplicationPage
    {
        List<VideoItem> _listAllVideos = null;

        public ViewVideos()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);

            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.New || HikeInstantiation.IS_TOMBSTONED)
            {
                shellProgressAlbums.IsIndeterminate = true;
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

        /// <summary>
        /// Function to bind fetched video albums to LongListContainer
        /// </summary>
        /// <returns></returns>
        public async Task BindAlbums()
        {
            await Task.Delay(1);
            llsAlbums.ItemsSource = GetAlbums();
            //create a delay so that it hides after ui render
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAlbums.IsIndeterminate = false;
            });
        }

        /// <summary>
        /// Create video albums from video files. Function creates video album from filepath and adds respective videos in it.
        /// </summary>
        /// <returns></returns>
        public List<VideoAlbum> GetAlbums()
        {
            Dictionary<string, VideoAlbum> videoAlbumMap = new Dictionary<string, VideoAlbum>();
            _listAllVideos = new List<VideoItem>();

            try
            {
                FetchPreRecordedVideos preRecordedVideos = new FetchPreRecordedVideos();
                ushort totalVideos = preRecordedVideos.GetVideoCount();
                VideoItem video;
                string albumName;

                for (int index = 0; index < totalVideos; index++)
                {
                    video = GetVideoFile(preRecordedVideos, index);
                    
                    if (video == null)
                        continue;

                    VideoAlbum albumObj;
                    try
                    {
                        albumName = video.FilePath.Substring(0, video.FilePath.LastIndexOf("\\"));
                        albumName = albumName.Substring(albumName.LastIndexOf("\\") + 1);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ViewVideos :: GetAlbums : Setting album name , Exception : " + ex.StackTrace);
                        albumName = AppResources.Default_Video_Album_Txt;
                    }
                    
                    if (!videoAlbumMap.TryGetValue(albumName, out albumObj))
                    {
                        albumObj = new VideoAlbum(albumName);
                        videoAlbumMap.Add(albumName, albumObj);
                    }

                    albumObj.Add(video);
                    _listAllVideos.Add(video);
                }

                preRecordedVideos.ClearData();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ViewVideos :: GetAlbums , Exception : " + ex.StackTrace);
            }

            GenerateThumbnailForAlbumsFromRecentVideo(videoAlbumMap);
            return videoAlbumMap.Values.ToList();
        }

        /// <summary>
        /// Generate thumbnail for album tiles with latest video of that album
        /// </summary>
        /// <param name="videoAlbumMap"></param>
        void GenerateThumbnailForAlbumsFromRecentVideo(Dictionary<string, VideoAlbum> videoAlbumMap)
        {
            foreach (var album in videoAlbumMap)
            {
                DateTime maxTillnow = new DateTime(0);
                VideoItem selectedVideo = null;

                foreach (var video in album.Value)
                {
                    if (maxTillnow < video.TimeStamp && video.ThumbnailBytes != null)
                    {
                        maxTillnow = video.TimeStamp;
                        selectedVideo = video;
                    }
                }

                if (selectedVideo != null && selectedVideo.ThumbnailBytes != null)
                    album.Value.ThumbBytes = selectedVideo.ThumbnailBytes;
            }
        }

        /// <summary>
        /// Function to call when a user taps on a album in long list selector
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Albums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VideoAlbum album = llsAlbums.SelectedItem as VideoAlbum;

            if (album == null)
                return;

            albumNameTxt.Text = album.AlbumName.ToLower();
            llsAlbums.SelectedItem = null;
            ToggleView(false);
            llsVideos.ItemsSource = null;
            shellProgressVideos.IsIndeterminate = true;
            BindAlbumVideos(album);
        }

        /// <summary>
        /// Binding videos to longList Selector when a user taps on a album
        /// </summary>
        /// <param name="album"></param>
        /// <returns></returns>
        private async Task BindAlbumVideos(VideoAlbum album)
        {
            await Task.Delay(1);
            llsVideos.ItemsSource = GroupedVideos(album);

            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressVideos.IsIndeterminate = false;
            });
        }

        #endregion ALBUMS

        #region Videos

        /// <summary>
        /// Binding all videos to LongList selector when user swipe to all video section
        /// </summary>
        /// <returns></returns>
        public async Task BindVideos()
        {
            await Task.Delay(1);
            llsAllVideos.ItemsSource = GroupedVideos(_listAllVideos);
            //create a delay so that it doesnot pause abruptly
            Dispatcher.BeginInvoke(() =>
            {
                shellProgressAllVideos.IsIndeterminate = false;
            });
        }

        /// <summary>
        /// Group videos according to their creation month
        /// </summary>
        /// <param name="listVideos"></param>
        /// <returns></returns>
        public List<KeyedList<string, VideoItem>> GroupedVideos(List<VideoItem> listVideos)
        {
            if (listVideos == null || listVideos.Count == 0)
                return null;

            var groupedPhotos =
                from video in listVideos
                orderby video.TimeStamp descending
                group video by video.TimeStamp.ToString("y") into videosByMonth
                select new KeyedList<string, VideoItem>(videosByMonth);

            return new List<KeyedList<string, VideoItem>>(groupedPhotos);
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

        /// <summary>
        /// Get video file from Video file list created by RPAL calls
        /// </summary>
        /// <param name="preRecordedVideos">RPAL object which contains video list pointer</param>
        /// <param name="index">index of the file in the list </param>
        /// <returns></returns>
        public VideoItem GetVideoFile(FetchPreRecordedVideos preRecordedVideos, int index)
        {
            string filePath = string.Empty;
            int videoDuration;
            double date;
            Byte[] videoThumbBytes;
            VideoItem video = null;

            try
            {
                videoThumbBytes = preRecordedVideos.GetVideoInfo((byte)index, out filePath, out date, out videoDuration);
                video = new VideoItem(filePath, videoThumbBytes, videoDuration);
                DateTime dob = new DateTime(Convert.ToInt64(date), DateTimeKind.Utc);
                video.TimeStamp = dob.AddYears(HikeConstants.STARTING_BASE_YEAR);//file time is ticks starting from jan 1 1601 so adding 1600 years
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PreviewVideo :: GetVideoFile , Exception : " + ex.StackTrace);
            }

            return video;
        }

        #endregion

        #region helper functions

        /// <summary>
        /// Function to set visibility for all album grids or grids for a particular video album
        /// </summary>
        /// <param name="showAlbum"></param>
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

        /// <summary>
        /// Fuction to call when a user taps on a video
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void llsVideos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LongListSelector lls = sender as LongListSelector;
            VideoItem selectedVideo = lls.SelectedItem as VideoItem;

            if (selectedVideo == null)
                return;

            lls.SelectedItem = null;
            int size;

            try
            {
                StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri(selectedVideo.FilePath, UriKind.Relative));
                size = (int)streamInfo.Stream.Length;
                streamInfo.Stream.Dispose();
                if (size <= 0)
                {
                    MessageBox.Show(AppResources.CT_FileNotOpenable_Text, AppResources.CT_FileNotSupported_Caption_Text, MessageBoxButton.OK);
                    return;
                }
                selectedVideo.Size = size;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PreviewVideo :: llsVideos_SelectionChanged , Exception : " + ex.StackTrace);
                MessageBox.Show(AppResources.CT_FileNotOpenable_Text, AppResources.CT_FileNotSupported_Caption_Text, MessageBoxButton.OK);
                return;
            }

            PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = selectedVideo;
            NavigationService.Navigate(new Uri("/View/PreviewVideo.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Function to call when user swipe between all video and album view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pivotAlbums_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pivotAlbums.SelectedIndex == 1)
            {
                shellProgressAllVideos.IsIndeterminate = true;
                BindVideos();
            }
        }
    }
}