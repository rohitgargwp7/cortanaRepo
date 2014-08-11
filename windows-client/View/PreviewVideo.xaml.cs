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

namespace windows_client.View
{
    public partial class PreviewVideo : PhoneApplicationPage
    {
        public PreviewVideo()
        {
            InitializeComponent();
            this.ApplicationBar = new ApplicationBar();

            ApplicationBarIconButton shareVideo = new ApplicationBarIconButton();
            shareVideo.Text = "share";
            shareVideo.IconUri = new Uri("/View/images/AppBar/icon_tick.png", UriKind.RelativeOrAbsolute); ;
            shareVideo.Click += shareVideo_Click;

            this.ApplicationBar.Buttons.Add(shareVideo);

            thumbnailImage.Source = UI_Utils.Instance.createImageFromBytes((byte[])PhoneApplicationService.Current.State[HikeConstants.VIDEO_THUMB_SHARED]);
            VideoDurationText.Text = TimeUtils.GetProperTimeFromMilliseconds((int)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED_DURATION]);
        }

        void shareVideo_Click(object sender, EventArgs e)
        {
            double videoSize = (int)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED_SIZE];
            if (videoSize > HikeConstants.FILE_MAX_SIZE)
            {
                //int MaxFileSizeInMB = HikeConstants.FILE_MAX_SIZE / (1024 * 1024 * 10);
                MessageBox.Show(AppResources.CT_FileSizeExceed_Text, AppResources.CT_FileSizeExceed_Caption_Text, MessageBoxButton.OK);
                //return;
                //var result = MessageBox.Show("Your video is larger than " + MaxFileSizeInMB + "MB. Send first " + MaxFileSizeInMB + "MB.", "Video Size Exceeds Maximum Limit", MessageBoxButton.OKCancel);
                //if (result == MessageBoxResult.OK)
                //{
                //    try
                //    {
                //        StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
                //        byte[] videoBytes = AccountUtils.InitialBytesStreamToByteArray(streamInfo.Stream, HikeConstants.FILE_MAX_SIZE/(2*5));
                //        PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
                //        Debug.WriteLine(videoBytes.Length);
                //    }
                //    catch (Exception ex)
                //    {
                //        Debug.WriteLine(ex.Message);
                //    }
                //    if (NavigationService.CanGoBack)
                //        NavigationService.RemoveBackEntry();
                //    if (NavigationService.CanGoBack)
                //        NavigationService.GoBack();
                //}
                //else
                //{
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_THUMB_SHARED);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED_DURATION);
                    PhoneApplicationService.Current.State.Remove(HikeConstants.VIDEO_SHARED_SIZE);
                    //if (NavigationService.CanGoBack)
                    //    NavigationService.RemoveBackEntry();
                    NavigationService.Navigate(new Uri("/View/ViewVideos.xaml", UriKind.Relative));
               // }
            }
            else
            {
                //try
                //{
                //    StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
                //    byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
                //    PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //}
                if (NavigationService.CanGoBack)
                    NavigationService.RemoveBackEntry();
                if (NavigationService.CanGoBack)
                    NavigationService.GoBack();
            }
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Utils.PlayFileInMediaPlayer((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED]);
                //MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                //mediaPlayerLauncher.Media = new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative);
                //mediaPlayerLauncher.Location = MediaLocationType.Data;
                //mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                //mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                //try
                //{
                //    mediaPlayerLauncher.Show();
                //}
                //catch (Exception ex)
                //{
                //}
        }
    }
}