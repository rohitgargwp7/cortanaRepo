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
        }

        void shareVideo_Click(object sender, EventArgs e)
        {
            StreamResourceInfo streamInfo = Application.GetResourceStream(new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative));
            byte[] videoBytes = AccountUtils.StreamToByteArray(streamInfo.Stream);
            PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED] = videoBytes;

            if (NavigationService.CanGoBack)
                NavigationService.RemoveBackEntry();
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }

        private void ContentPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
             MediaPlayerLauncher mediaPlayerLauncher = new MediaPlayerLauncher();
                mediaPlayerLauncher.Media = new Uri((string)PhoneApplicationService.Current.State[HikeConstants.VIDEO_SHARED], UriKind.Relative);
                mediaPlayerLauncher.Location = MediaLocationType.Data;
                mediaPlayerLauncher.Controls = MediaPlaybackControls.Pause | MediaPlaybackControls.Stop;
                mediaPlayerLauncher.Orientation = MediaPlayerOrientation.Landscape;
                try
                {
                    mediaPlayerLauncher.Show();
                }
                catch (Exception ex)
                {
                }

        }
    }
}