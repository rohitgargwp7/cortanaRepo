using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using windows_client.utils;
using Microsoft.Phone.Tasks;
using System.Windows.Resources;
using System.Diagnostics;
using windows_client.Languages;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using Windows.Storage;
using Windows.Storage.FileProperties;
using windows_client.Model;

namespace windows_client.View
{
    public partial class PreviewVideo : PhoneApplicationPage
    {
        public VideoItem _videoShared;
        public PreviewVideo()
        {
            InitializeComponent();

            this.ApplicationBar = new ApplicationBar();
            ApplicationBar.IsVisible = true;
            ApplicationBar.Opacity = 0.5;
            ApplicationBar.BackgroundColor = Colors.Black;
            ApplicationBar.ForegroundColor = Colors.White;

            ApplicationBarIconButton shareVideo = new ApplicationBarIconButton();
            shareVideo.Text = AppResources.Share_Txt;
            shareVideo.IconUri = new Uri("/View/images/AppBar/icon_send_video.png", UriKind.RelativeOrAbsolute); ;
            shareVideo.Click += shareVideo_Click;
            this.ApplicationBar.Buttons.Add(shareVideo);

            _videoShared = (VideoItem)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED];
            thumbnailImage.Source = _videoShared.ThumbnailImage;
            VideoDurationText.Text = TimeUtils.GetDurationInHourMinFromMilliseconds(_videoShared.Duration);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
            base.OnNavigatedFrom(e);
        }

        void shareVideo_Click(object sender, EventArgs e)
        {
            if (_videoShared.Size > HikeConstants.FILE_MAX_SIZE)
            {
                MessageBox.Show(AppResources.CT_FileSizeExceed_Text, AppResources.CT_FileSizeExceed_Caption_Text, MessageBoxButton.OK);
                PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
                
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
            else
            {
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);

            if (!NavigationService.CanGoBack)
                e.Cancel = true; 

            base.OnBackKeyPress(e);
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            App.ViewModel.PauseBackgroundAudio();
            Utils.PlayFileInMediaPlayer(_videoShared.FilePath);
        }
    }
}